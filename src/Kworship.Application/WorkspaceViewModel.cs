using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Kworship.Domain;

namespace Kworship.Application;

public sealed record ScheduleEntry(Guid SongId, string Title, bool IsMissing = false);

/// <summary>UI-independent workspace. Call public mutations on the owning UI context.</summary>
public sealed class WorkspaceViewModel : INotifyPropertyChanged
{
    private readonly ILibraryStore store;
    private readonly PresentationSession session;
    private IReadOnlyList<Song> library = [];
    private Song? editingSong;
    private ServiceSchedule currentSchedule = new(Guid.NewGuid(), 0, "Untitled service", []);
    private string query = "", songTitle = "", lyrics = "", scheduleTitle = "Untitled service", status = "Ready. Outputs start black.";
    private Guid newSongId = Guid.NewGuid();
    private int selectedScheduleIndex = -1;
    private bool busy;
    private SongDraft? recoveredSong;
    private ScheduleDraft? recoveredSchedule;
    private ServiceSchedule? recoveryScheduleBase;
    public event PropertyChangedEventHandler? PropertyChanged;
    public ObservableCollection<Song> Songs { get; } = [];
    public ObservableCollection<ServiceSchedule> Schedules { get; } = [];
    public ObservableCollection<ScheduleEntry> Entries { get; } = [];
    public WorkspaceViewModel(ILibraryStore store, IPresentationOutput output)
    { this.store = store; session = new(output); }
    public string Query { get => query; set { query = value; Filter(); Changed(); } }
    public string SongTitle { get => songTitle; set { songTitle = value; Changed(); Changed(nameof(IsSongDirty)); } }
    public string Lyrics { get => lyrics; set { lyrics = value; Changed(); Changed(nameof(IsSongDirty)); } }
    public string ScheduleTitle { get => scheduleTitle; set { scheduleTitle = value; Changed(); Changed(nameof(IsScheduleDirty)); } }
    public string Status { get => status; private set { status = value; Changed(); } }
    public bool IsBusy { get => busy; private set { busy = value; Changed(); } }
    public bool IsSongDirty => SongTitle != (editingSong?.Title ?? "") || Lyrics != (editingSong?.Lyrics ?? "");
    public bool IsScheduleDirty => ScheduleTitle != currentSchedule.Title || !Entries.Select(e => e.SongId).SequenceEqual(currentSchedule.SongIds);
    public bool HasRecovery => recoveredSong is not null || recoveredSchedule is not null;
    public Guid CurrentScheduleId => currentSchedule.Id;
    public string SongSaveLabel => IsSongDirty ? "Unsaved changes" : editingSong is null ? "New song" : $"Saved · revision {editingSong.Revision}";
    public string ScheduleSaveLabel => IsScheduleDirty || currentSchedule.Revision == 0 ? "Unsaved service" : $"Saved · revision {currentSchedule.Revision}";
    public int SelectedScheduleIndex { get => selectedScheduleIndex; set { selectedScheduleIndex = value; Changed(); } }
    public PresentationState State => session.State;
    public string PreviewText => State.Preview?.Slide.Text ?? "Choose a song or service item to preview.";
    public string PreviewTitle => State.Preview?.Item.Title ?? "Nothing selected";
    public string PreviewPosition => State.Preview is { } c ? $"{c.Slide.Label} · {c.Index + 1} / {c.Item.Slides.Length}" : "Preview is independent of live output";
    public string LiveText => State.VisibleText;
    public string LiveTitle => State.Live?.Item.Title ?? "Nothing live";
    public string LivePosition => State.Live is { } c ? $"{c.Slide.Label} · {c.Index + 1} / {c.Item.Slides.Length}" : "Take a preview when you are ready";
    public string BlackLabel => State.IsBlack ? "Black: ON" : "Black";
    public string ClearLabel => State.IsTextClear ? "Clear: ON" : "Clear";
    public string FreezeLabel => State.IsFrozen ? "Freeze: ON" : "Freeze";
    public string OutputStatus => State.IsBlack ? "BLACK SCREEN" : State.IsFrozen ? "FROZEN" : State.IsTextClear ? "TEXT CLEARED" : "LIVE OUTPUT";
    public string NextText => State.Live is { } c && c.Index + 1 < c.Item.Slides.Length ? c.Item.Slides[c.Index + 1].Text : "End of item";
    public IReadOnlyList<Slide> PreviewSlides => State.Preview?.Item.Slides.ToArray() ?? [];
    public int PreviewSlideIndex
    {
        get => State.Preview?.Index ?? -1;
        set { if (value >= 0 && State.Preview is { } c && value != c.Index) Dispatch(new MovePreview(value - c.Index)); }
    }
    private void Changed([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new(name));
        if (name == nameof(IsSongDirty)) PropertyChanged?.Invoke(this, new(nameof(SongSaveLabel)));
        if (name == nameof(IsScheduleDirty)) PropertyChanged?.Invoke(this, new(nameof(ScheduleSaveLabel)));
    }
    private void Filter()
    {
        Songs.Clear();
        foreach (var song in library.Where(s => s.Title.Contains(Query, StringComparison.OrdinalIgnoreCase) || s.Lyrics.Contains(Query, StringComparison.OrdinalIgnoreCase))) Songs.Add(song);
    }
    public async Task InitializeAsync(WorkspaceRecovery? recovery = null)
    {
        // Keep pending drafts even if a database read fails during startup.
        recoveredSong = recovery?.Song; recoveredSchedule = recovery?.Schedule;
        Changed(nameof(HasRecovery));
        await Work(async () =>
        {
            await RefreshLibrary();
            await RefreshSchedules();
            if (recovery?.LastScheduleId is { } id)
            {
                var saved = await Task.Run(() => store.GetSchedule(id));
                if (saved is not null) SetSchedule(saved);
            }
            if (recoveredSchedule is { } draft) recoveryScheduleBase = await Task.Run(() => store.GetSchedule(draft.Id));
            Changed(nameof(HasRecovery));
            if (HasRecovery) Status = "Unsaved work was found. Recover drafts or dismiss them; live output stays black.";
        });
    }
    private async Task RefreshLibrary()
    {
        library = await Task.Run(store.ListSongs); Filter();
        for (int i = 0; i < Entries.Count; i++) Entries[i] = Entry(Entries[i].SongId);
    }
    private async Task RefreshSchedules()
    {
        var schedules = await Task.Run(store.ListSchedules); Schedules.Clear();
        foreach (var schedule in schedules) Schedules.Add(schedule);
    }
    public bool NewSong()
    {
        if (IsBusy || !AllowSongReplacement()) return false;
        editingSong = null; newSongId = Guid.NewGuid(); SongTitle = ""; Lyrics = ""; Changed(nameof(SongSaveLabel));
        Status = "New song. Separate slides with a blank line; optional labels use [Verse 1]."; return true;
    }
    public bool EditSong(Song? song)
    {
        if (IsBusy || song is null || !AllowSongReplacement()) return false;
        editingSong = song; SongTitle = song.Title; Lyrics = song.Lyrics; Status = "Editing song. Live content remains unchanged."; return true;
    }
    private bool AllowSongReplacement()
    { if (!AllowNewEdits()) return false; if (!IsSongDirty) return true; Status = "Save or discard the current song edits before opening another song."; return false; }
    public void DiscardSongEdits()
    { if (IsBusy) return; SongTitle = editingSong?.Title ?? ""; Lyrics = editingSong?.Lyrics ?? ""; Status = "Song edits discarded."; }
    public Task SaveSongAsync() => Work(async () =>
    {
        if (!AllowNewEdits()) return;
        var candidate = new Song(editingSong?.Id ?? newSongId, editingSong?.Revision ?? 0, SongTitle.Trim(), Lyrics);
        _ = SongPresentation.Create(candidate with { Revision = Math.Max(1, candidate.Revision) });
        editingSong = await Task.Run(() => store.SaveSong(candidate, candidate.Revision));
        // Editing is disabled while this write is in flight.
        SongTitle = editingSong.Title; Lyrics = editingSong.Lyrics;
        await RefreshLibrary(); Changed(nameof(IsSongDirty));
        Status = "Song saved locally. Preview and live snapshots were preserved.";
    });
    public void PreviewSong(Song? song)
    { if (song is null) { Status = "Choose a song from the library first."; return; } Try(() => Dispatch(new SelectPreview(SongPresentation.Create(song)))); }
    public void PreviewEntry()
    {
        if (!ValidSelection()) return;
        var song = library.FirstOrDefault(s => s.Id == Entries[SelectedScheduleIndex].SongId);
        if (song is null) { Status = "This song is missing from the library. Live output was preserved."; return; }
        PreviewSong(song);
    }
    public void AddSong(Song? song)
    {
        if (IsBusy || song is null || !AllowNewEdits()) return;
        Entries.Add(Entry(song.Id)); SelectedScheduleIndex = Entries.Count - 1;
        Changed(nameof(IsScheduleDirty)); Status = "Added to the service. Live output was preserved.";
    }
    public void MoveEntry(int delta)
    {
        if (IsBusy || !AllowNewEdits() || !ValidSelection()) return;
        var target = (int)Math.Clamp((long)SelectedScheduleIndex + delta, 0, Entries.Count - 1);
        var entry = Entries[SelectedScheduleIndex]; Entries.RemoveAt(SelectedScheduleIndex); Entries.Insert(target, entry);
        SelectedScheduleIndex = target; Changed(nameof(IsScheduleDirty));
    }
    public void ReorderEntry(int sourceIndex, int insertionIndex)
    {
        if (IsBusy || !AllowNewEdits() || sourceIndex < 0 || sourceIndex >= Entries.Count || insertionIndex < 0 || insertionIndex > Entries.Count) return;
        var entry = Entries[sourceIndex];
        Entries.RemoveAt(sourceIndex);
        var target = sourceIndex < insertionIndex ? insertionIndex - 1 : insertionIndex;
        Entries.Insert(target, entry); SelectedScheduleIndex = target;
        Changed(nameof(IsScheduleDirty)); Status = "Service reordered. Live output was preserved.";
    }
    public void RemoveEntry()
    {
        if (IsBusy || !AllowNewEdits() || !ValidSelection()) return;
        var index = SelectedScheduleIndex; Entries.RemoveAt(index); SelectedScheduleIndex = Math.Min(index, Entries.Count - 1);
        Changed(nameof(IsScheduleDirty)); Status = "Removed from service. Live output was preserved.";
    }
    private bool ValidSelection() => SelectedScheduleIndex >= 0 && SelectedScheduleIndex < Entries.Count;
    private ScheduleEntry Entry(Guid id)
    { var song = library.FirstOrDefault(s => s.Id == id); return new(id, song?.Title ?? "Missing song — relink or remove", song is null); }
    private void SetSchedule(ServiceSchedule schedule)
    {
        currentSchedule = schedule with { SongIds = schedule.SongIds.ToArray() };
        ScheduleTitle = schedule.Title; Entries.Clear();
        foreach (var id in schedule.SongIds) Entries.Add(Entry(id));
        SelectedScheduleIndex = Entries.Count > 0 ? 0 : -1;
        Changed(nameof(CurrentScheduleId)); Changed(nameof(IsScheduleDirty));
    }
    public bool NewSchedule()
    {
        if (IsBusy || !AllowScheduleReplacement()) return false;
        SetSchedule(new(Guid.NewGuid(), 0, "Untitled service", [])); Status = "New service."; return true;
    }
    private bool AllowScheduleReplacement()
    { if (!AllowNewEdits()) return false; if (!IsScheduleDirty) return true; Status = "Save or discard this service before opening another one."; return false; }
    public Task OpenScheduleAsync(ServiceSchedule? schedule) => Work(async () =>
    {
        if (schedule is null || !AllowScheduleReplacement()) return;
        var loaded = await Task.Run(() => store.GetSchedule(schedule.Id)) ?? throw new InvalidOperationException("The service no longer exists.");
        SetSchedule(loaded); Status = "Service opened. Live output was preserved.";
    });
    public Task SaveScheduleAsync() => Work(async () =>
    {
        if (!AllowNewEdits()) return;
        var candidate = currentSchedule with { Title = ScheduleTitle.Trim(), SongIds = Entries.Select(e => e.SongId).ToArray() };
        var saved = await Task.Run(() => store.SaveSchedule(candidate, candidate.Revision));
        SetSchedule(saved); await RefreshSchedules(); Status = "Service saved locally.";
    });
    public Task DuplicateScheduleAsync() => Work(async () =>
    {
        if (!AllowNewEdits()) return;
        // Duplicate the working service, including its current unsaved ordering, without replacing it on failure.
        var copy = new ServiceSchedule(Guid.NewGuid(), 0, ScheduleTitle.Trim() + " (copy)", Entries.Select(e => e.SongId).ToArray());
        var saved = await Task.Run(() => store.SaveSchedule(copy, 0));
        SetSchedule(saved); await RefreshSchedules(); Status = "Service duplicated and saved.";
    });
    public void DiscardScheduleEdits()
    { if (IsBusy) return; SetSchedule(currentSchedule); Status = "Service edits discarded."; }
    public void Dispatch(PresentationCommand command) => Try(() =>
    {
        session.Dispatch(command);
        foreach (var name in new[] { nameof(State), nameof(PreviewText), nameof(PreviewTitle), nameof(PreviewPosition), nameof(LiveText), nameof(LiveTitle), nameof(LivePosition), nameof(OutputStatus), nameof(NextText), nameof(PreviewSlides), nameof(PreviewSlideIndex), nameof(BlackLabel), nameof(ClearLabel), nameof(FreezeLabel) }) Changed(name);
    });
    public void RecoverDrafts()
    {
        if (IsBusy) return;
        if (IsSongDirty || IsScheduleDirty) { Status = "Save or discard current edits before recovering drafts."; return; }
        if (recoveredSong is { } song)
        {
            // Keep the original base revision even if the database changed; a stale save must conflict.
            var baseline = library.FirstOrDefault(s => s.Id == song.Id);
            editingSong = song.Revision == 0 ? null : new(song.Id, song.Revision, baseline?.Title ?? "", baseline?.Lyrics ?? "");
            newSongId = song.Id; SongTitle = song.Title; Lyrics = song.Lyrics;
        }
        if (recoveredSchedule is { } draft)
        {
            SetSchedule(recoveryScheduleBase is { } saved ? saved with { Revision = draft.Revision }
                : new(draft.Id, draft.Revision, "Untitled service", []));
            ScheduleTitle = draft.Title; Entries.Clear();
            foreach (var id in draft.SongIds) Entries.Add(Entry(id)); Changed(nameof(IsScheduleDirty));
        }
        recoveredSong = null; recoveredSchedule = null; Changed(nameof(HasRecovery));
        Status = "Drafts recovered for review. Current live output was preserved.";
    }
    public void DismissRecovery()
    { recoveredSong = null; recoveredSchedule = null; Changed(nameof(HasRecovery)); Status = "Recovered drafts dismissed."; }
    public WorkspaceRecovery CaptureRecovery() => new(Song: recoveredSong ?? (IsSongDirty
        ? new(editingSong?.Id ?? newSongId, editingSong?.Revision ?? 0, SongTitle, Lyrics) : null),
        Schedule: recoveredSchedule ?? (IsScheduleDirty ? new(currentSchedule.Id, currentSchedule.Revision, ScheduleTitle, Entries.Select(e => e.SongId).ToArray()) : null),
        LastScheduleId: currentSchedule.Revision > 0 ? currentSchedule.Id : null);
    private bool AllowNewEdits()
    {
        if (!HasRecovery) return true;
        Status = "Recover or dismiss the pending drafts before starting new edits."; return false;
    }
    public void ReportError(string message) => Status = message;
    private void Try(Action action)
    { try { action(); } catch (Exception ex) { Status = ex is ArgumentException or InvalidOperationException or RevisionConflictException ? ex.Message : "Operation failed. Current live state and edits were preserved."; } }
    private async Task Work(Func<Task> action)
    {
        if (IsBusy) return;
        IsBusy = true;
        try { await action(); }
        catch (Exception ex)
        { Status = ex is ArgumentException or InvalidOperationException or RevisionConflictException ? ex.Message : "Could not complete the library operation. Your edits were preserved."; }
        finally { IsBusy = false; }
    }
}
