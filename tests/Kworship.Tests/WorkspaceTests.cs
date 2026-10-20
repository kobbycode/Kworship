using Kworship.Application;
using Kworship.Domain;
using Kworship.Infrastructure.Sqlite;
using Xunit;
namespace Kworship.Tests;

public sealed class WorkspaceTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(),"kworship-workspace-" + Guid.NewGuid());
    private LibraryStore Store => new(Path.Combine(directory,"library.db"));
    private Song AddSong(string title, string lyrics = "[Verse 1]\nFirst line\n\n[Chorus]\nSecond line") => Store.SaveSong(new(Guid.NewGuid(),0,title,lyrics),0);
    private async Task<WorkspaceViewModel> Open(WorkspaceRecovery? recovery = null, IPresentationOutput? output = null)
    { var vm = new WorkspaceViewModel(Store, output ?? new RecordingOutput()); await vm.InitializeAsync(recovery); return vm; }
    [Fact] public async Task SearchFindsLyricsCaseInsensitivelyWithoutChangingLive()
    {
        var a=AddSong("Morning", "Café LIGHT"); AddSong("Evening", "Peace"); var vm=await Open();
        vm.PreviewSong(a); vm.Dispatch(new TakePreview()); var live=vm.State.Live;
        vm.Query="light"; Assert.Single(vm.Songs); Assert.Equal(a.Id,vm.Songs[0].Id); Assert.Same(live,vm.State.Live);
        vm.Query="EVEN"; Assert.Equal("Evening",Assert.Single(vm.Songs).Title);
    }
    [Fact] public async Task UnsavedSongBlocksReplacementUntilExplicitDiscard()
    {
        var a=AddSong("A");var b=AddSong("B");var vm=await Open();
        Assert.True(vm.EditSong(a));vm.Lyrics="Unsaved original words";
        Assert.False(vm.EditSong(b));Assert.False(vm.NewSong());Assert.Equal("A",vm.SongTitle);Assert.True(vm.IsSongDirty);
        vm.DiscardSongEdits();Assert.True(vm.EditSong(b));Assert.Equal("B",vm.SongTitle);
    }
    [Fact] public async Task SavedEditsDoNotReplaceLiveSnapshot()
    {
        var a=AddSong("A");var vm=await Open();vm.PreviewSong(a);vm.Dispatch(new TakePreview());vm.Dispatch(new SetBlack(false));
        vm.EditSong(a);vm.Lyrics="Edited words";await vm.SaveSongAsync();
        Assert.False(vm.IsSongDirty);Assert.Equal("First line",vm.LiveText);Assert.Equal("Edited words",Store.GetSong(a.Id)!.Lyrics);
        vm.PreviewSong(Store.GetSong(a.Id));Assert.Equal("First line",vm.LiveText);
        vm.Dispatch(new TakePreview());Assert.Equal("Edited words",vm.LiveText);
    }
    [Fact] public async Task FailedSongSaveKeepsDraftAndDisplaysConflict()
    {
        var a=AddSong("A");var vm=await Open();vm.EditSong(a);vm.Lyrics="My draft";
        Store.SaveSong(a with { Title="Other operator" },1);await vm.SaveSongAsync();
        Assert.True(vm.IsSongDirty);Assert.Equal("My draft",vm.Lyrics);Assert.Contains("changed",vm.Status);
        Assert.Equal("Other operator",Store.GetSong(a.Id)!.Title);
    }
    [Fact] public async Task ServiceReorderRemoveAndSaveSurviveRestartWithoutInterruptingLive()
    {
        var a=AddSong("A");var b=AddSong("B");var vm=await Open();vm.PreviewSong(a);vm.Dispatch(new TakePreview());var live=vm.State.Live;
        vm.ScheduleTitle="Sunday";vm.AddSong(a);vm.AddSong(b);vm.AddSong(a);
        vm.ReorderEntry(2,0);vm.SelectedScheduleIndex=1;vm.RemoveEntry();
        await vm.SaveScheduleAsync();Assert.False(vm.IsScheduleDirty);Assert.Same(live,vm.State.Live);
        var reopened=await Open(vm.CaptureRecovery());Assert.Equal("Sunday",reopened.ScheduleTitle);
        Assert.Equal(new[]{a.Id,b.Id},reopened.Entries.Select(e=>e.SongId));Assert.True(reopened.State.IsBlack);Assert.Null(reopened.State.Live);
    }
    [Fact] public async Task DuplicateSavesIndependentServiceWithWorkingOrder()
    {
        var a=AddSong("A");var b=AddSong("B");var vm=await Open();vm.ScheduleTitle="Sunday";vm.AddSong(a);await vm.SaveScheduleAsync();var original=vm.CurrentScheduleId;
        vm.AddSong(b);await vm.DuplicateScheduleAsync();Assert.NotEqual(original,vm.CurrentScheduleId);
        Assert.Equal(new[]{a.Id},Store.GetSchedule(original)!.SongIds);
        Assert.Equal(new[]{a.Id,b.Id},Store.GetSchedule(vm.CurrentScheduleId)!.SongIds);Assert.Equal(2,vm.Schedules.Count);
    }
    [Fact] public async Task UnsavedServiceCannotBeOverwrittenByNewOrOpen()
    {
        var saved=Store.SaveSchedule(new(Guid.NewGuid(),0,"Saved",[]),0);var vm=await Open();vm.ScheduleTitle="My unsaved service";
        Assert.False(vm.NewSchedule());await vm.OpenScheduleAsync(saved);Assert.Equal("My unsaved service",vm.ScheduleTitle);
        vm.DiscardScheduleEdits();await vm.OpenScheduleAsync(saved);Assert.Equal("Saved",vm.ScheduleTitle);
    }
    [Fact] public async Task RecoveryDoesNotActivateLiveAndPersistsPendingDraftUntilDecision()
    {
        var a=AddSong("A");var vm=await Open();vm.EditSong(a);vm.Lyrics="Recovered lyric";vm.ScheduleTitle="Recovered service";vm.AddSong(a);
        var recovery=vm.CaptureRecovery();var reopened=await Open(recovery);
        Assert.True(reopened.HasRecovery);Assert.Null(reopened.State.Live);Assert.True(reopened.State.IsBlack);
        Assert.Equal("Recovered lyric",reopened.CaptureRecovery().Song!.Lyrics);
        reopened.RecoverDrafts();Assert.Equal("Recovered lyric",reopened.Lyrics);Assert.True(reopened.IsSongDirty);
        Assert.Equal("Recovered service",reopened.ScheduleTitle);Assert.Single(reopened.Entries);Assert.Null(reopened.State.Live);
        await reopened.SaveSongAsync();await reopened.SaveScheduleAsync();Assert.False(reopened.IsSongDirty);Assert.False(reopened.IsScheduleDirty);
    }
    [Fact] public async Task RecoveredStaleSongRevisionCannotOverwriteNewerDatabaseContent()
    {
        var a=AddSong("A");var vm=await Open();vm.EditSong(a);vm.Lyrics="Old draft";var recovery=vm.CaptureRecovery();
        Store.SaveSong(a with { Lyrics="Newer database" },1);
        var reopened=await Open(recovery);reopened.RecoverDrafts();await reopened.SaveSongAsync();
        Assert.Equal("Old draft",reopened.Lyrics);Assert.Contains("changed",reopened.Status);Assert.Equal("Newer database",Store.GetSong(a.Id)!.Lyrics);
    }
    [Fact] public async Task DismissRecoveryRemovesPendingDraftAndCannotChangeLive()
    {
        var a=AddSong("A");var vm=await Open(new(Song:new(a.Id,1,"A","Unsaved")));
        vm.DismissRecovery();Assert.False(vm.HasRecovery);Assert.Null(vm.CaptureRecovery().Song);Assert.Null(vm.State.Live);
    }
    [Fact] public async Task MissingRecoveredSongPreventsTakeAndKeepsServiceDraft()
    {
        var vm=await Open(new(Schedule:new(Guid.NewGuid(),0,"Missing",[Guid.NewGuid()])));
        vm.RecoverDrafts();Assert.True(Assert.Single(vm.Entries).IsMissing);
        vm.SelectedScheduleIndex=0;vm.PreviewEntry();Assert.Null(vm.State.Preview);Assert.Contains("missing",vm.Status);
        await vm.SaveScheduleAsync();Assert.True(vm.IsScheduleDirty);Assert.Null(Store.GetSchedule(vm.CurrentScheduleId));
    }
    [Fact] public async Task ScheduleListReturnsFullOrderedContentIncludingEmptyServices()
    {
        var a=AddSong("A");var b=AddSong("B");Store.SaveSchedule(new(Guid.NewGuid(),0,"B service",[a.Id,b.Id,a.Id]),0);
        Store.SaveSchedule(new(Guid.NewGuid(),0,"A service",[]),0);
        var list=Store.ListSchedules();Assert.Equal("A service",list[0].Title);Assert.Empty(list[0].SongIds);
        Assert.Equal(new[]{a.Id,b.Id,a.Id},list[1].SongIds);_ = await Open();
    }
    [Fact] public async Task OutputFailureKeepsPriorPresentationAndDrafts()
    {
        var a=AddSong("A");var output=new RecordingOutput();var vm=await Open(output:output);vm.PreviewSong(a);vm.Dispatch(new TakePreview());var prior=vm.State;
        output.Fail=true;vm.Dispatch(new MoveLive(1));Assert.Same(prior,vm.State);Assert.Contains("failed",vm.Status);
    }
    [Fact] public void SongParserRemovesLabelsAndKeepsSnapshotSlideIdsStable()
    {
        var a=AddSong("A","[Verse 1]\r\nFirst\r\n  \r\n[Chorus]\r\nSecond\r\n\r\n\r\nThird");
        var item=SongPresentation.Create(a);Assert.Equal(3,item.Slides.Length);Assert.Equal("Verse 1",item.Slides[0].Label);Assert.Equal("First",item.Slides[0].Text);
        Assert.Equal(item.Slides.Select(s=>s.Id),SongPresentation.Create(a).Slides.Select(s=>s.Id));
        Assert.NotEqual(item.Slides[0].Id,SongPresentation.Create(a with { Revision=2 }).Slides[0].Id);
    }
    [Theory] [InlineData(0,3,"BCA")] [InlineData(2,0,"CAB")] [InlineData(1,2,"ABC")]
    public async Task DragInsertionHandlesBothDirectionsAndNoOp(int source,int insertion,string order)
    {
        var vm=await Open();foreach(var title in new[]{"A","B","C"})vm.AddSong(AddSong(title));
        // Refresh via a new workspace to include every newly created song in its in-memory library.
        await vm.SaveScheduleAsync();var loaded=await Open(vm.CaptureRecovery());
        loaded.ReorderEntry(source,insertion);Assert.Equal(order,string.Concat(loaded.Entries.Select(e=>e.Title)));
    }
    [Fact] public async Task PendingRecoveryBlocksNewEditsRatherThanOverwritingUnreviewedDraft()
    {
        var song=AddSong("A");var vm=await Open(new(Song:new(song.Id,1,"A","Pending draft")));
        Assert.False(vm.NewSong());Assert.False(vm.EditSong(song));Assert.False(vm.NewSchedule());
        vm.AddSong(song);Assert.Empty(vm.Entries);Assert.Equal("Pending draft",vm.CaptureRecovery().Song!.Lyrics);
        vm.DismissRecovery();Assert.True(vm.EditSong(song));vm.AddSong(song);Assert.Single(vm.Entries);
    }
    private sealed class RecordingOutput : IPresentationOutput
    { public bool Fail { get;set; } public void Present(PresentationState state) { if(Fail)throw new IOException("Device lost"); } }
    public void Dispose() { if(Directory.Exists(directory))Directory.Delete(directory,true); }
}
