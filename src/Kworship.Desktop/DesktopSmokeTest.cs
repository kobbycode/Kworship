using System.Diagnostics;
using System.IO;
using System.Text.Json;
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
            var window = new MainWindow(store,data);
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
            combo.SelectedIndex=0; combo.IsDropDownOpen=true; await Pump();
            Check(BrushColor(combo.Background)==Color.FromRgb(16,25,35),"Saved-service picker uses the dark control palette");
            Check(combo.Template.FindName("PART_Popup",combo) is System.Windows.Controls.Primitives.Popup {IsOpen:true},"Saved-service picker opens its popup");
            combo.IsDropDownOpen=false; await Pump();
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
    private sealed class BindingErrorListener:TraceListener
    {
        public List<string> Errors{get;}=[];
        public override void Write(string? message){if(!string.IsNullOrWhiteSpace(message))Errors.Add(message);}
        public override void WriteLine(string? message)=>Write(message);
    }
}
