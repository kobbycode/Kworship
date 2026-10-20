using Kworship.Application;
using Kworship.Domain;
using Kworship.Infrastructure.Sqlite;
using Microsoft.Data.Sqlite;
using Xunit;
namespace Kworship.Tests;
public sealed class SongMilestoneTests:IDisposable
{
    private readonly string directory=Path.Combine(Path.GetTempPath(),"kworship-songs-"+Guid.NewGuid());
    private string Database=>Path.Combine(directory,"library.db");
    private static Song Fixture()=>new(Guid.NewGuid(),0,"Morning","[Verse 1]\nOne\n\n[Chorus]\nTwo\n\n[Bridge]\nThree","Original author","Original credit","Verse 1, Chorus, Bridge, Chorus");
    [Fact]public void ArrangementRepeatsSectionsAndPreservesCreditsInSnapshot()
    {
        var song=Fixture() with{Revision=1};var item=SongPresentation.Create(song);
        Assert.Equal(new[]{"One","Two","Three","Two"},item.Slides.Select(s=>s.Text));
        Assert.Equal(new[]{"Verse 1","Chorus","Bridge","Chorus"},item.Slides.Select(s=>s.Label));
        Assert.Contains("Original author",item.Credit);Assert.Contains("Original credit",item.Credit);
        Assert.Equal(4,item.Slides.Select(s=>s.Id).Distinct().Count());
    }
    [Theory][InlineData("Missing")][InlineData("Verse 1, Missing")][InlineData(",,,")]
    public void InvalidOrderRejected(string order)=>Assert.Throws<ArgumentException>(()=>SongPresentation.Create(Fixture() with{Revision=1,Arrangement=order}));
    [Fact]public void AmbiguousLabelsOnlyBlockExplicitCustomOrders()
    {
        var song=Fixture() with{Revision=1,Lyrics="[Chorus]\nOne\n\n[Chorus]\nTwo",Arrangement=""};
        Assert.Equal(2,SongPresentation.Create(song).Slides.Length);
        Assert.Throws<ArgumentException>(()=>SongPresentation.Create(song with{Arrangement="Chorus"}));
    }
    [Fact]public void StructuredRenameUpdatesRepeatedOrderReferences()
    {
        var source=Fixture();var editor=new StructuredSongEditor(source);var chorus=editor.Sections[1];chorus.Label="Refrain";
        var result=editor.Build();Assert.Equal("Verse 1, Refrain, Bridge, Refrain",result.Arrangement);
        Assert.Contains("[Refrain]",result.Lyrics);Assert.Equal(source.Author,result.Author);
        Assert.Throws<InvalidOperationException>(()=>editor.RemoveSection(chorus));
        while(editor.Order.Remove(chorus)){}editor.RemoveSection(chorus);Assert.DoesNotContain("Refrain",editor.Build().Arrangement);
    }
    [Fact]public void StructuredEditsAreDetachedUntilApplied()
    {
        var source=Fixture();var editor=new StructuredSongEditor(source);editor.Sections[0].Text="New text";editor.Author="New author";
        Assert.Contains("One",source.Lyrics);Assert.Equal("Original author",source.Author);
        var edited=editor.Build();Assert.Contains("New text",edited.Lyrics);Assert.Equal("New author",edited.Author);
    }
    [Fact]public void StructuredEditorRejectsAmbiguousOrUnserializableLabels()
    {
        var editor=new StructuredSongEditor(Fixture());editor.Sections[1].Label="verse 1";
        Assert.Throws<ArgumentException>(()=>editor.Build());editor.Sections[1].Label="Bad, label";
        Assert.Throws<ArgumentException>(()=>editor.Build());
    }
    [Fact]public void MetadataAndOrderRoundTripAndBackupRestore()
    {
        var store=new LibraryStore(Database);var song=store.SaveSong(Fixture(),0);Assert.Equal(song,new LibraryStore(Database).GetSong(song.Id));
        var backup=Path.Combine(directory,"backup.db");store.Backup(backup);
        var restored=LibraryStore.RestoreToNewDatabase(backup,Path.Combine(directory,"restore.db"));Assert.Equal(song,restored.GetSong(song.Id));
    }
    [Fact]public void InvalidOrderSaveLeavesPreviousRevisionIntact()
    {
        var store=new LibraryStore(Database);var song=store.SaveSong(Fixture(),0);
        Assert.Throws<ArgumentException>(()=>store.SaveSong(song with{Arrangement="Missing"},1));Assert.Equal(song,store.GetSong(song.Id));
    }
    [Fact]public async Task MetadataDraftRecoveryKeepsLiveSnapshotAndSupportsAuthorSearch()
    {
        var store=new LibraryStore(Database);var song=store.SaveSong(Fixture(),0);var vm=new WorkspaceViewModel(store,new Output());await vm.InitializeAsync();vm.EditSong(song);
        vm.PreviewSong(song);vm.Dispatch(new TakePreview());var live=vm.State.Live;vm.Author="Changed author";vm.Arrangement="Chorus, Verse 1";
        var recovery=vm.CaptureRecovery();Assert.Equal("Changed author",recovery.Song!.Author);
        var reopened=new WorkspaceViewModel(store,new Output());await reopened.InitializeAsync(recovery);reopened.RecoverDrafts();Assert.Equal("Chorus, Verse 1",reopened.Arrangement);
        await vm.SaveSongAsync();Assert.Same(live,vm.State.Live);Assert.Contains("Original author",live!.Item.Credit);
        vm.Query="Changed author";Assert.Single(vm.Songs);vm.DiscardSongEdits();Assert.False(vm.IsSongDirty);
    }
    [Theory][InlineData("DB5CEED7A0284AE152FFC670066852BBDCEEDE799930FF58A240A6574669BB45")][InlineData("B577347CEE3E2046D74140ABE1828D1650D1B5E421FBAA53C18CC5EF4145076B")]public void V1UpgradePreservesContentAndCreatesVerifiedPreUpgradeBackup(string legacyReceipt)
    {
        Directory.CreateDirectory(directory);var songId=Guid.NewGuid();var serviceId=Guid.NewGuid();
        using(var db=new SqliteConnection($"Data Source={Database};Pooling=False"))
        {
            db.Open();using var command=db.CreateCommand();command.CommandText=LegacySchema;
            command.ExecuteNonQuery();command.CommandText=$"CREATE TABLE schema_migrations(version INTEGER PRIMARY KEY,checksum TEXT NOT NULL); INSERT INTO schema_migrations VALUES(1,'{legacyReceipt}'); INSERT INTO songs VALUES($song,3,'Legacy song','Legacy lyrics'); INSERT INTO schedules VALUES($service,2,'Legacy service'); INSERT INTO schedule_items VALUES($service,0,$song);";
            command.Parameters.AddWithValue("$song",songId.ToString());command.Parameters.AddWithValue("$service",serviceId.ToString());command.ExecuteNonQuery();
        }
        var store=new LibraryStore(Database);Assert.NotNull(store.MigrationBackupPath);Assert.True(File.Exists(store.MigrationBackupPath));
        var song=store.GetSong(songId)!;Assert.Equal(3,song.Revision);Assert.Equal("Legacy lyrics",song.Lyrics);Assert.Equal("",song.Author);Assert.Equal(new[]{songId},store.GetSchedule(serviceId)!.SongIds);
        using(var backup=new SqliteConnection($"Data Source={store.MigrationBackupPath};Mode=ReadOnly;Pooling=False"))
        {backup.Open();using var command=backup.CreateCommand();command.CommandText="SELECT MAX(version) FROM schema_migrations";Assert.Equal(1L,command.ExecuteScalar());command.CommandText="PRAGMA integrity_check";Assert.Equal("ok",command.ExecuteScalar());}
        Assert.Null(new LibraryStore(Database).MigrationBackupPath);
        var restored=LibraryStore.RestoreToNewDatabase(store.MigrationBackupPath!,Path.Combine(directory,"legacy-restored.db"));Assert.Equal(song,restored.GetSong(songId));
    }
    private const string LegacySchema="""
        CREATE TABLE songs(id TEXT PRIMARY KEY, revision INTEGER NOT NULL CHECK(revision > 0),
          title TEXT NOT NULL, lyrics TEXT NOT NULL);
        CREATE TABLE schedules(id TEXT PRIMARY KEY, revision INTEGER NOT NULL CHECK(revision > 0), title TEXT NOT NULL);
        CREATE TABLE schedule_items(schedule_id TEXT NOT NULL REFERENCES schedules(id) ON DELETE CASCADE,
          ordinal INTEGER NOT NULL, song_id TEXT NOT NULL REFERENCES songs(id) ON DELETE RESTRICT,
          PRIMARY KEY(schedule_id, ordinal));
        """;
    private sealed class Output:IPresentationOutput{public void Present(PresentationState state){}}
    public void Dispose(){if(Directory.Exists(directory))Directory.Delete(directory,true);}
}
