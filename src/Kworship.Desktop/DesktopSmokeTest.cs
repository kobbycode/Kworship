using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Kworship.Application;
using Kworship.Infrastructure.Sqlite;
using Kworship.Platform.Windows;

namespace Kworship.Desktop;

/// <summary>Runs the real WPF view/bindings/buttons on Windows with disposable data.</summary>
internal static class DesktopSmokeTest
{
    public static async Task<int> RunAsync(App application, string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        var checks = new List<string>();
        var data = Path.Combine(outputDirectory,"fixture-"+Guid.NewGuid().ToString("N"));
        var bindingErrors = new BindingErrorListener();
        PresentationTraceSources.DataBindingSource.Listeners.Add(bindingErrors);
        PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;
        try
        {
            RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
            var store = new LibraryStore(Path.Combine(data,"library.db"));
            using var nativeCatalog=new NativeDisplayCatalog();
            var nativeDisplays=nativeCatalog.GetDisplays();
            if(nativeDisplays.Count==0||nativeDisplays.Select(d=>d.Id).Distinct().Count()!=nativeDisplays.Count||nativeDisplays.Any(d=>d.Width<=0||d.Height<=0))throw new InvalidOperationException("Windows display enumeration returned invalid identities or bounds");
            var simulatedCatalog=new TestDisplayCatalog(nativeDisplays);
            var confirmations=0;
            var window = new MainWindow(store,data,simulatedCatalog,(_,_)=>{confirmations++;return true;});
            application.MainWindow = window;
            window.Show();
            await window.WhenReady.WaitAsync(TimeSpan.FromSeconds(20));
            window.Width=1440; window.Height=900;
            await Pump();
            var model = (WorkspaceViewModel)window.DataContext;
            void Check(bool condition,string message)
            { if(!condition)throw new InvalidOperationException(message); checks.Add(message); }
            Check(BrushColor(window.Background)==Color.FromRgb(16,21,29),"Main window uses the dark background on Windows");
            Check(BrushColor(window.Foreground)==Color.FromRgb(241,245,250),"Main window uses readable foreground text");
            foreach(var name in new[]{"StatusText","PreviewSurface","LiveSurface"})
                Check(BrushColor(Find<TextBlock>(window,name).Foreground)==Color.FromRgb(241,245,250),name+" resolves readable foreground");
            Check(!Find<Button>(window,"TakeButton").IsEnabled,"Take is disabled when no preview exists");
            Check(!Find<Button>(window,"PreviewSelectedButton").IsEnabled,"Preview selected is disabled when the library selection is empty");
            Check(!Find<Button>(window,"SaveSongButton").IsEnabled,"Save is disabled until title and lyrics are entered");
            await Click(window,"NewSongButton");
            Check(FocusManager.GetFocusedElement(window)==Find<TextBox>(window,"SongTitleInput"),"New song moves keyboard focus to the labeled title field");
            Enter(Find<TextBox>(window,"SongTitleInput"),"Morning light — original fixture");
            Enter(Find<TextBox>(window,"LyricsInput"),"[Verse 1]\nWe gather in the morning light\n\n[Chorus]\nTogether we begin again");
            await Pump();
            Check(Find<Button>(window,"SaveSongButton").IsEnabled,"Entering title and lyrics enables Save through real bindings");
            await Click(window,"SaveSongButton");
            await Until(()=>!model.IsBusy && store.ListSongs().Count==1 && Find<ListBox>(window,"Songs").SelectedItem is Song);
            Check(store.ListSongs()[0].Title=="Morning light — original fixture","Save button persists the typed title and lyrics");
            Check(Find<Button>(window,"PreviewSelectedButton").IsEnabled,"Saving automatically selects the song and enables preview");
            await Click(window,"PreviewSelectedButton");
            Check(Find<TextBlock>(window,"PreviewSurface").Text=="We gather in the morning light","Preview button updates the real preview surface");
            Check(model.State.Live is null,"Preview does not take content live");
            await Click(window,"TakeButton");
            Check(model.State.Live is not null && model.State.IsBlack,"Take commits live content with the black-screen safety state intact");
            await Click(window,"BlackButton");
            Check(Find<TextBlock>(window,"LiveSurface").Text=="We gather in the morning light","Black toggle reveals the actual live surface");
            await Click(window,"PreviewNextButton");
            Check(model.PreviewText=="Together we begin again" && model.LiveText=="We gather in the morning light","Preview navigation remains separate from live");
            await Click(window,"OpenOutputButton");
            await Click(window,"OpenStageButton");
            await Pump();
            var outputs=application.Windows.OfType<TextOutputWindow>().ToArray();
            Check(outputs.Length==2,"Both output buttons create real WPF output windows");
            Check(outputs.All(output=>Descendants<TextBlock>(output).Any(t=>t.Text=="We gather in the morning light")),"Both output windows render the committed lyric");
            window.Activate(); await Pump();
            await Click(window,"AddSongButton");
            Enter(Find<TextBox>(window,"ServiceTitleBox"),"Sunday gathering");
            await Click(window,"SaveServiceButton");
            await Until(()=>!model.IsBusy && store.ListSchedules().Count==1);
            Check(store.ListSchedules()[0].SongIds.Count==1,"Service buttons add and persist the selected song");
            Check(!model.IsSongDirty && !model.IsScheduleDirty,"Song and service finish in saved states");
            var combo=Find<ComboBox>(window,"SavedSchedules");
            combo.SelectedIndex=0; await Pump();
            Check(Descendants<TextBlock>(combo).Any(t=>t.Text=="Sunday gathering"),"Saved-service selection renders its title rather than an internal record");
            combo.IsDropDownOpen=true; await Pump();
            Check(BrushColor(combo.Background)==Color.FromRgb(16,25,35),"Saved-service picker uses the dark control palette");
            Check(combo.Template.FindName("PART_Popup",combo) is System.Windows.Controls.Primitives.Popup {IsOpen:true},"Saved-service picker opens its popup");
            combo.IsDropDownOpen=false; await Pump();
            await Click(window,"EditSongButton");
            await Click(window,"StructuredSongButton");
            var structured=application.Windows.OfType<StructuredSongWindow>().Single();
            Enter(Find<TextBox>(structured,"AuthorInput"),"Kworship fixture author");
            Enter(Find<TextBox>(structured,"CopyrightInput"),"Original fixture © 2026");
            var sectionList=Find<ListBox>(structured,"SectionsList");sectionList.SelectedIndex=1;await Pump();
            Enter(Find<TextBox>(structured,"SectionLabelInput"),"Refrain");
            await Click(structured,"AddOrderButton");sectionList.SelectedIndex=0;await Pump();await Click(structured,"AddOrderButton");sectionList.SelectedIndex=1;await Pump();await Click(structured,"AddOrderButton");
            Capture(structured,Path.Combine(outputDirectory,"structured-song.png"));
            var previousLive=model.State.Live;
            await Click(structured,"ApplyStructuredButton");
            Check(model.IsSongDirty&&model.Arrangement=="Refrain, Verse 1, Refrain","Structured dialog applies metadata and repeated custom section order");
            Check(ReferenceEquals(previousLive,model.State.Live),"Structured song editing does not mutate the live snapshot");
            await Click(window,"SaveSongButton");await Until(()=>!model.IsBusy&&!model.IsSongDirty);
            var saved=store.ListSongs().Single();
            Check(saved.Author=="Kworship fixture author"&&saved.Copyright=="Original fixture © 2026","Structured song metadata persists through the real save button");
            await Click(window,"PreviewSelectedButton");
            Check(model.PreviewSlides.Count==3&&model.PreviewSlides[0].Label=="Refrain"&&model.PreviewSlides[2].Label=="Refrain","Preview follows custom song order, including repeated sections");
            await Click(window,"TakeButton");
            Check(application.Windows.OfType<TextOutputWindow>().Any(output=>Descendants<TextBlock>(output).Any(t=>t.Text.Contains("Kworship fixture author"))),"Congregation output includes the saved song credit");
            var bibleFixture = Path.Combine(data,"test-bible.xml");
            File.WriteAllText(bibleFixture,"<XMLBIBLE biblename='Original test translation'><BIBLEBOOK bname='Test book'><CHAPTER cnumber='1'><VERS vnumber='1'>First original scripture fixture.</VERS><VERS vnumber='2'>Second original scripture fixture.</VERS></CHAPTER></BIBLEBOOK></XMLBIBLE>");
            await Click(window,"ScriptureButton");
            var scripture = application.Windows.OfType<ScriptureWindow>().Single();
            Check(Descendants<TextBlock>(Find<ComboBox>(scripture,"Translation")).Any(t=>t.Text=="American Standard Version (1901)"),"Translation picker renders the Bible title rather than an internal file record");
            Find<ComboBox>(scripture,"Book").SelectedItem="John";await Pump();
            Find<ComboBox>(scripture,"Chapter").SelectedItem=3;await Pump();
            Find<ComboBox>(scripture,"FirstVerse").SelectedItem=16;await Pump();
            Check(Find<TextBlock>(scripture,"PassageText").Text.Contains("For God so loved the world"),"Bundled ASV opens offline and presents John 3:16 through the real selectors");
            await scripture.ImportSource(bibleFixture,"Original test content for UI validation");
            await Pump();
            Check(Find<ComboBox>(scripture,"Book").SelectedItem as string == "Test book","Scripture import populates the book and chapter controls");
            Find<ComboBox>(scripture,"LastVerse").SelectedIndex=1; await Pump();
            Check(Find<TextBlock>(scripture,"PassageText").Text.Contains("Second original scripture fixture."),"Scripture range selector previews both selected verses");
            Capture(scripture,Path.Combine(outputDirectory,"scripture.png"));
            var liveBeforeScripture=model.State.Live;
            await Click(scripture,"UsePassageButton");
            await Until(()=>!model.IsBusy&&store.ListSongs().Count==2);
            Check(ReferenceEquals(liveBeforeScripture,model.State.Live),"Saving scripture leaves the live song snapshot unchanged");
            await Click(window,"PreviewSelectedButton");
            Check(model.PreviewSlides.Count==2&&model.PreviewText.Contains("Test book 1:1"),"Saved scripture previews one referenced verse per slide");
            await Click(window,"AddSongButton");await Click(window,"SaveServiceButton");
            await Until(()=>!model.IsBusy&&!model.IsScheduleDirty);
            Check(store.ListSchedules().Single().SongIds.Count==2,"Scripture passage persists alongside a song in a saved service");
            await Click(window,"DisplaySettingsButton");
            var settings=application.Windows.OfType<DisplaySettingsWindow>().Single();
            Find<ComboBox>(settings,"CongregationDisplay").SelectedIndex=1;
            Find<ComboBox>(settings,"StageDisplay").SelectedIndex=0;
            Capture(settings,Path.Combine(outputDirectory,"display-setup.png"));
            await Click(settings,"SaveDisplaysButton");
            Check(window.Assignments.CongregationId==nativeDisplays[0].Id&&window.Assignments.StageId is null,"Display setup saves native monitor identity and independent windowed stage mode");
            Check(model.State.IsBlack&&!application.Windows.OfType<TextOutputWindow>().Any(),"Changing assignments suspends existing outputs safely");
            await Click(window,"OpenOutputButton");
            var assigned=application.Windows.OfType<TextOutputWindow>().Single();
            var target=nativeDisplays[0];
            if(!GetWindowRect(new WindowInteropHelper(assigned).Handle,out var bounds))throw new InvalidOperationException("Cannot read assigned output bounds");
            Check(bounds.Left==target.Left&&bounds.Top==target.Top&&bounds.Right-bounds.Left==target.Width&&bounds.Bottom-bounds.Top==target.Height,"Assigned output occupies the native display pixel bounds");
            Check(confirmations==1&&model.State.IsBlack,"Assigned output requires confirmation and opens black");
            var liveBeforeDisconnect=model.State.Live;
            simulatedCatalog.Set([]);await Until(()=>!application.Windows.OfType<TextOutputWindow>().Any());
            Check(model.State.IsBlack&&ReferenceEquals(liveBeforeDisconnect,model.State.Live),"Simulated disconnect suspends output while retaining the committed live item");
            simulatedCatalog.Set(nativeDisplays);await Pump();
            Check(!application.Windows.OfType<TextOutputWindow>().Any(),"Simulated reconnection never resumes output automatically");
            await Click(window,"OpenOutputButton");
            Check(confirmations==2&&model.State.IsBlack,"Reopening after reconnection requires another confirmation and remains black");
            await Click(window,"BlackButton");
            Check(model.LiveText=="Together we begin again","Operator can explicitly reveal the retained live slide after reconnect");
            window.Activate();await Pump();
            await Click(window,"DisplaySettingsButton");
            var stageSettings=application.Windows.OfType<DisplaySettingsWindow>().Single();
            Find<ComboBox>(stageSettings,"CongregationDisplay").SelectedIndex=0;
            Find<ComboBox>(stageSettings,"StageDisplay").SelectedIndex=1;
            await Click(stageSettings,"SaveDisplaysButton");await Click(window,"OpenOutputButton");await Click(window,"BlackButton");
            var visibleBeforeStage=model.LiveText;
            await Click(window,"OpenStageButton");
            Check(!model.State.IsBlack&&model.LiveText==visibleBeforeStage,"Opening an assigned stage display does not black out congregation output");
            window.Activate();await Pump();
            await Task.Delay(1000);
            Check(new WorkspaceStateFile(Path.Combine(data,"workspace.json")).Load()?.Displays==window.Assignments,"Display assignments persist in workspace recovery");
            Check(bindingErrors.Errors.Count==0,"No WPF data-binding errors during the operator workflow");
            Capture(window,Path.Combine(outputDirectory,"workspace.png"));
            File.WriteAllText(Path.Combine(outputDirectory,"result.json"),JsonSerializer.Serialize(new{passed=true,checks},new JsonSerializerOptions{WriteIndented=true}));
            Console.WriteLine($"Windows desktop smoke passed: {checks.Count} checks");
            return 0;
        }
        catch(Exception error)
        {
            File.WriteAllText(Path.Combine(outputDirectory,"result.json"),JsonSerializer.Serialize(new{passed=false,checks,error=error.ToString(),bindingErrors=bindingErrors.Errors},new JsonSerializerOptions{WriteIndented=true}));
            if(application.MainWindow is { } window) { try { Capture(window,Path.Combine(outputDirectory,"failure.png")); } catch(InvalidOperationException) { } }
            Console.Error.WriteLine("Windows desktop smoke FAILED: "+error.Message);
            return 1;
        }
        finally { PresentationTraceSources.DataBindingSource.Listeners.Remove(bindingErrors); }
    }
    private static Color BrushColor(Brush brush)=>brush is SolidColorBrush solid?solid.Color:throw new InvalidOperationException("Expected a solid theme brush");
    private static T Find<T>(FrameworkElement root,string name)where T:FrameworkElement=>root.FindName(name)as T??throw new InvalidOperationException("Control not found: "+name);
    private static void Enter(TextBox input,string value)
    {
        if(!input.IsEnabled)throw new InvalidOperationException("Editor is disabled: "+input.Name);
        input.SetCurrentValue(TextBox.TextProperty,value);
        input.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
    }
    private static async Task Click(FrameworkElement root,string name)
    {
        var button=Find<Button>(root,name);
        if(!button.IsEnabled)throw new InvalidOperationException("Button is unexpectedly disabled: "+name);
        button.UpdateLayout();
        if(button.InputHitTest(new Point(button.ActualWidth/2,button.ActualHeight/2)) is null)
            throw new InvalidOperationException("Button center cannot receive pointer input: "+name);
        var peer=new ButtonAutomationPeer(button);
        if(peer.GetPattern(PatternInterface.Invoke) is not IInvokeProvider invoke)throw new InvalidOperationException("Button has no accessible invoke action: "+name);
        invoke.Invoke(); await Pump();
    }
    private static async Task Pump()=>await Dispatcher.CurrentDispatcher.InvokeAsync(()=>{},DispatcherPriority.ApplicationIdle);
    private static async Task Until(Func<bool> ready)
    {
        var timer=Stopwatch.StartNew();
        while(!ready()) { if(timer.Elapsed>TimeSpan.FromSeconds(20))throw new TimeoutException("UI operation did not complete");await Task.Delay(40); }
        await Pump();
    }
    private static IEnumerable<T> Descendants<T>(DependencyObject root)where T:DependencyObject
    {
        for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)
        { var child=VisualTreeHelper.GetChild(root,i);if(child is T match)yield return match;foreach(var item in Descendants<T>(child))yield return item; }
    }
    private static void Capture(Window window,string path)
    {
        window.UpdateLayout();
        var bitmap=new RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth),(int)Math.Ceiling(window.ActualHeight),96,96,PixelFormats.Pbgra32);
        bitmap.Render(window);
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream=File.Create(path);encoder.Save(stream);
    }
    [StructLayout(LayoutKind.Sequential)]private struct WindowRect{public int Left,Top,Right,Bottom;}
    [DllImport("user32.dll")]private static extern bool GetWindowRect(IntPtr handle,out WindowRect bounds);
    private sealed class TestDisplayCatalog(IReadOnlyList<DisplayInfo> displays):IDisplayCatalog
    {
        public event EventHandler? Changed;
        public IReadOnlyList<DisplayInfo> GetDisplays()=>displays;
        public void Set(IReadOnlyList<DisplayInfo> value){displays=value;Changed?.Invoke(this,EventArgs.Empty);}
        public void Dispose(){}
    }
    private sealed class BindingErrorListener:TraceListener
    {
        public List<string> Errors{get;}=[];
        public override void Write(string? message){if(!string.IsNullOrWhiteSpace(message))Errors.Add(message);}
        public override void WriteLine(string? message)=>Write(message);
    }
}
