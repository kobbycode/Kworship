using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using Kworship.Application;
using Kworship.Domain;
using Kworship.Infrastructure.Sqlite;
using Kworship.Platform.Windows;
using Microsoft.Win32;
namespace Kworship.Desktop;

public partial class MainWindow : Window
{
    private readonly WorkspaceViewModel workspace;
    private readonly LibraryStore store;
    private readonly WorkspaceStateFile recoveryFile;
    private readonly string workspaceDirectory;
    private readonly TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal Task WhenReady => ready.Task;
    private readonly DispatcherTimer saveTimer = new() { Interval = TimeSpan.FromMilliseconds(700) };
    private TextOutputWindow? congregation, stage;
    private Task<bool> pendingSave = Task.FromResult(true);
    private bool initialized, allowClose, closing;
    private Point dragStart;
    private int dragIndex = -1;
    private readonly Guid dragToken = Guid.NewGuid();
    private sealed record ServiceDrag(Guid Token, int Index);
    public MainWindow(LibraryStore store, string? workspaceDirectory = null)
    {
        InitializeComponent(); this.store = store;
        this.workspaceDirectory = workspaceDirectory ?? App.DataRoot;
        recoveryFile = new(Path.Combine(this.workspaceDirectory, "workspace.json"));
        workspace = new(store, new OutputRouter(this)); DataContext = workspace;
        workspace.PropertyChanged += WorkspaceChanged;
        saveTimer.Tick += SaveDraftTick;
        SizeChanged += (_, _) => QueueRecovery();
        Loaded += Initialize;
        UpdateActions();
        Closing += CloseWorkspace;
        Closed += (_, _) => { congregation?.Close(); stage?.Close(); };
    }
    private async void Initialize(object sender, RoutedEventArgs e)
    {
        WorkspaceRecovery? recovery = null;
        try { recovery = await Task.Run(recoveryFile.Load); }
        catch (Exception ex)
        {
            App.Log("recovery_read_failed",ex);
            // Keep the exact invalid file available before writing any new recovery state.
            try
            {
                var path = Path.Combine(workspaceDirectory, "workspace.json");
                File.Copy(path, path + ".unreadable-" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            }
            catch (Exception copyError) { App.Log("recovery_preservation_failed",copyError); workspace.ReportError("Recovery file could not be read or preserved. Workspace recovery is disabled for this session."); await workspace.InitializeAsync(); ready.TrySetResult(); return; }
        }
        {
            var layout = recovery ?? new WorkspaceRecovery();
            Width = FiniteClamp(layout.Width, 1120, Math.Max(1120,SystemParameters.WorkArea.Width),1440);
            Height = FiniteClamp(layout.Height, 720, Math.Max(720,SystemParameters.WorkArea.Height),900);
            LibraryColumn.Width = new(FiniteClamp(layout.LibraryWidth,200,340,250));
            ScheduleColumn.Width = new(FiniteClamp(layout.ScheduleWidth,230,360,270));
            var ratio = FiniteClamp(layout.PreviewHeightRatio,0.35,0.75,0.55);
            PreviewRow.Height = new(ratio,GridUnitType.Star); EditorRow.Height = new(1-ratio,GridUnitType.Star);
            if (LibraryColumn.Width.Value + ScheduleColumn.Width.Value > Width - 608)
            { LibraryColumn.Width = new(200); ScheduleColumn.Width = new(230); }
        }
        await workspace.InitializeAsync(recovery);
        initialized = true; UpdateActions(); ready.TrySetResult();
    }
    private static double FiniteClamp(double value, double min, double max, double fallback) => double.IsFinite(value) ? Math.Clamp(value,min,max) : fallback;
    private void WorkspaceChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Library mutations run off the dispatcher; disable editable fields while a write is in flight.
        EditorPanel.IsEnabled = !workspace.IsBusy && !workspace.HasRecovery;
        ServicePanel.IsEnabled = !workspace.IsBusy && !workspace.HasRecovery;
        UpdateActions();
        if (e.PropertyName is nameof(WorkspaceViewModel.SongTitle) or nameof(WorkspaceViewModel.Lyrics)
            or nameof(WorkspaceViewModel.IsSongDirty) or nameof(WorkspaceViewModel.IsScheduleDirty)
            or nameof(WorkspaceViewModel.HasRecovery) or nameof(WorkspaceViewModel.CurrentScheduleId)) QueueRecovery();
    }
    private void LayoutChanged(object sender, DragCompletedEventArgs e) => QueueRecovery();
    private void QueueRecovery() { if (!initialized || allowClose) return; saveTimer.Stop(); saveTimer.Start(); }
    private WorkspaceRecovery Capture()
    {
        var bounds = WindowState == WindowState.Normal ? new Rect(Left,Top,ActualWidth,ActualHeight) : RestoreBounds;
        return workspace.CaptureRecovery() with { Width = bounds.Width, Height = bounds.Height,
            LibraryWidth = LibraryColumn.ActualWidth, ScheduleWidth = ScheduleColumn.ActualWidth,
            PreviewHeightRatio = PreviewRow.ActualHeight / Math.Max(1, PreviewRow.ActualHeight+EditorRow.ActualHeight) };
    }
    private async void SaveDraftTick(object? sender, EventArgs e)
    {
        saveTimer.Stop();
        await pendingSave;
        pendingSave = Persist(Capture());
        await pendingSave;
    }
    private async Task<bool> Persist(WorkspaceRecovery state)
    {
        try { await Task.Run(() => recoveryFile.Save(state)); return true; }
        catch (Exception ex) { App.Log("recovery_write_failed",ex); workspace.ReportError("Could not save workspace recovery. Save your songs and service before closing."); return false; }
    }
    private async void CloseWorkspace(object? sender, CancelEventArgs e)
    {
        if (allowClose) return;
        e.Cancel = true;
        if (closing || workspace.IsBusy) { workspace.ReportError("Wait for the current library operation before closing."); return; }
        if (!initialized && (workspace.IsSongDirty || workspace.IsScheduleDirty))
        { workspace.ReportError("Draft recovery is unavailable. Save or discard your edits before closing."); return; }
        if ((workspace.IsSongDirty || workspace.IsScheduleDirty) && MessageBox.Show(this,
            "Keep your unsaved work as recovery drafts and close? Drafts will be offered next time you open Kworship.",
            "Close Kworship",MessageBoxButton.OKCancel,MessageBoxImage.Question) != MessageBoxResult.OK) return;
        closing = true; IsEnabled = false;
        saveTimer.Stop();
        await pendingSave;
        if (initialized && !await Persist(Capture()) && (workspace.IsSongDirty || workspace.IsScheduleDirty || workspace.HasRecovery))
        { closing = false; IsEnabled = true; workspace.ReportError("Could not preserve drafts. Save or discard them before closing."); return; }
        allowClose = true; Close();
    }
    private void OpenOutput(object sender, RoutedEventArgs e)
    {
        if (congregation is null) { congregation = new(); congregation.Closed += (_,_) => congregation = null; congregation.Show(); }
        congregation.Present(workspace.State); congregation.Activate();
    }
    private void OpenStage(object sender, RoutedEventArgs e)
    {
        if (stage is null) { stage = new(true); stage.Closed += (_,_) => stage = null; stage.Show(); }
        stage.Present(workspace.State); stage.Activate();
    }
    private sealed class OutputRouter(MainWindow owner) : IPresentationOutput
    { public void Present(PresentationState state) { owner.congregation?.Present(state); owner.stage?.Present(state); } }
    private static ListBoxItem? Container(DependencyObject? element)
    {
        while (element is not null && element is not ListBoxItem)
            element = element is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(element) : LogicalTreeHelper.GetParent(element);
        return element as ListBoxItem;
    }
    private void StartServiceDrag(object sender, MouseButtonEventArgs e)
    {
        dragStart = e.GetPosition(ServiceItems);
        var row = Container(e.OriginalSource as DependencyObject);
        dragIndex = row is null ? -1 : ServiceItems.ItemContainerGenerator.IndexFromContainer(row);
    }
    private void DragServiceItem(object sender, MouseEventArgs e)
    {
        if (workspace.IsBusy || e.LeftButton != MouseButtonState.Pressed || dragIndex < 0) return;
        var now = e.GetPosition(ServiceItems);
        if (Math.Abs(now.X-dragStart.X) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs(now.Y-dragStart.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        var sourceIndex = dragIndex; dragIndex = -1;
        DragDrop.DoDragDrop(ServiceItems,new DataObject(typeof(ServiceDrag),new ServiceDrag(dragToken,sourceIndex)),DragDropEffects.Move);
    }
    private void ServiceDragOver(object sender, DragEventArgs e)
    {
        e.Effects = !workspace.IsBusy && e.Data.GetData(typeof(ServiceDrag)) is ServiceDrag data && data.Token == dragToken ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }
    private void DropServiceItem(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(typeof(ServiceDrag)) is not ServiceDrag data || data.Token != dragToken) return;
        var row = Container(e.OriginalSource as DependencyObject);
        var insertion = row is null ? workspace.Entries.Count : ServiceItems.ItemContainerGenerator.IndexFromContainer(row) + (e.GetPosition(row).Y > row.ActualHeight/2 ? 1 : 0);
        workspace.ReorderEntry(data.Index,insertion); e.Handled = true;
    }
    private void UpdateActions()
    {
        if (workspace is null) return;
        SaveSongButton.IsEnabled = workspace.CanSaveSong;
        EditSongButton.IsEnabled = Songs.SelectedItem is Song && !workspace.IsBusy && !workspace.HasRecovery;
        PreviewSelectedButton.IsEnabled = Songs.SelectedItem is Song;
        AddSongButton.IsEnabled = Songs.SelectedItem is Song && !workspace.IsBusy && !workspace.HasRecovery;
        TakeButton.IsEnabled = workspace.State.Preview is not null;
        PreviewPreviousButton.IsEnabled = workspace.State.Preview is {Index: >0};
        PreviewNextButton.IsEnabled = workspace.State.Preview is { } preview && preview.Index+1 < preview.Item.Slides.Length;
        LivePreviousButton.IsEnabled = workspace.State.Live is {Index: >0};
        LiveNextButton.IsEnabled = workspace.State.Live is { } live && live.Index+1 < live.Item.Slides.Length;
        BlackButton.IsEnabled = ClearButton.IsEnabled = FreezeButton.IsEnabled = workspace.State.Live is not null;
        OpenServiceButton.IsEnabled = SavedSchedules.SelectedItem is ServiceSchedule;
        SaveServiceButton.IsEnabled = !string.IsNullOrWhiteSpace(workspace.ScheduleTitle) && !workspace.IsBusy;
        PreviewServiceButton.IsEnabled = workspace.SelectedScheduleIndex >= 0;
    }
    private void SongSelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateActions();
    private void SavedServiceSelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateActions();
    private void ServiceSelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateActions();
    private void NewSong(object sender, RoutedEventArgs e) { if (workspace.NewSong()) SongTitleInput.Focus(); }
    private void EditSelected(object sender, RoutedEventArgs e) => workspace.EditSong(Songs.SelectedItem as Song);
    private async void SaveSong(object sender, RoutedEventArgs e)
    {
        await workspace.SaveSongAsync();
        if (!workspace.IsSongDirty && workspace.EditingSongId is { } id)
        {
            workspace.Query = "";
            Songs.SelectedItem = workspace.Songs.FirstOrDefault(song => song.Id == id);
            workspace.ReportError("Song saved and selected. Click Preview selected, then Take. Switch Black off to show it on the congregation output.");
        }
    }
    private void DiscardSong(object sender, RoutedEventArgs e) => workspace.DiscardSongEdits();
    private void PreviewLibrary(object sender, RoutedEventArgs e) => workspace.PreviewSong(Songs.SelectedItem as Song);
    private void PreviewLibraryDoubleClick(object sender, MouseButtonEventArgs e) => workspace.PreviewSong(Songs.SelectedItem as Song);
    private void AddToService(object sender, RoutedEventArgs e) => workspace.AddSong(Songs.SelectedItem as Song);
    private void PreviewService(object sender, RoutedEventArgs e) => workspace.PreviewEntry();
    private void PreviewServiceDoubleClick(object sender, MouseButtonEventArgs e) => workspace.PreviewEntry();
    private void NewSchedule(object sender, RoutedEventArgs e) => workspace.NewSchedule();
    private async void OpenSchedule(object sender, RoutedEventArgs e) => await workspace.OpenScheduleAsync(SavedSchedules.SelectedItem as ServiceSchedule);
    private async void SaveSchedule(object sender, RoutedEventArgs e)
    {
        await workspace.SaveScheduleAsync();
        if (!workspace.IsScheduleDirty) SavedSchedules.SelectedItem = workspace.Schedules.FirstOrDefault(item=>item.Id==workspace.CurrentScheduleId);
    }
    private async void DuplicateSchedule(object sender, RoutedEventArgs e) => await workspace.DuplicateScheduleAsync();
    private void DiscardSchedule(object sender, RoutedEventArgs e) => workspace.DiscardScheduleEdits();
    private void MoveUp(object sender, RoutedEventArgs e) => workspace.MoveEntry(-1);
    private void MoveDown(object sender, RoutedEventArgs e) => workspace.MoveEntry(1);
    private void RemoveEntry(object sender, RoutedEventArgs e) => workspace.RemoveEntry();
    private void Recover(object sender, RoutedEventArgs e) => workspace.RecoverDrafts();
    private void DismissRecovery(object sender, RoutedEventArgs e) => workspace.DismissRecovery();
    private void PreviewPrevious(object sender, RoutedEventArgs e) => workspace.Dispatch(new MovePreview(-1));
    private void PreviewNext(object sender, RoutedEventArgs e) => workspace.Dispatch(new MovePreview(1));
    private void Take(object sender, RoutedEventArgs e) => workspace.Dispatch(new TakePreview());
    private void LivePrevious(object sender, RoutedEventArgs e) => workspace.Dispatch(new MoveLive(-1));
    private void LiveNext(object sender, RoutedEventArgs e) => workspace.Dispatch(new MoveLive(1));
    private void Black(object sender, RoutedEventArgs e) => workspace.Dispatch(new SetBlack(!workspace.State.IsBlack));
    private void Clear(object sender, RoutedEventArgs e) => workspace.Dispatch(new SetClearText(!workspace.State.IsTextClear));
    private void Freeze(object sender, RoutedEventArgs e) => workspace.Dispatch(new SetFreeze(!workspace.State.IsFrozen));
    private void FocusSearch(object sender, ExecutedRoutedEventArgs e) { SearchBox.Focus(); SearchBox.SelectAll(); }
    private async void SaveFocused(object sender, ExecutedRoutedEventArgs e)
    { if (EditorPanel.IsKeyboardFocusWithin) await workspace.SaveSongAsync(); else await workspace.SaveScheduleAsync(); }
    private async void BackupLibrary(object sender, RoutedEventArgs e)
    {
        if (workspace.IsBusy) return;
        var dialog = new SaveFileDialog { FileName = "Kworship-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".db", Filter = "Kworship database backup (*.db)|*.db", OverwritePrompt = true };
        if (dialog.ShowDialog(this) != true) return;
        try { await Task.Run(() => store.Backup(dialog.FileName)); workspace.ReportError("Database backup verified. Unsaved drafts are separate; save them before backing up."); }
        catch (Exception ex) { App.Log("backup_failed",ex); workspace.ReportError(ex is IOException ? ex.Message : "Could not create a verified database backup."); }
    }
}
