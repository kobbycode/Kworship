using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Kworship.Application;
using Kworship.Platform.Windows;
namespace Kworship.Desktop;
public partial class DisplaySettingsWindow:Window
{
    private sealed record Choice(string? Id,string Label);
    private readonly IReadOnlyList<DisplayInfo> displays;
    public DisplayAssignments? Result{get;private set;}
    public DisplaySettingsWindow(IReadOnlyList<DisplayInfo> displays,DisplayAssignments assignments)
    {
        InitializeComponent();this.displays=displays;
        var choices=new[]{new Choice(null,"Windowed output — manual placement")}.Concat(displays.Select(display=>new Choice(display.Id,display.Label))).ToArray();
        CongregationDisplay.ItemsSource=choices;StageDisplay.ItemsSource=choices;
        CongregationDisplay.SelectedItem=choices.FirstOrDefault(choice=>choice.Id==assignments.CongregationId)??choices[0];
        StageDisplay.SelectedItem=choices.FirstOrDefault(choice=>choice.Id==assignments.StageId)??choices[0];
        if((assignments.CongregationId is not null&&!displays.Any(d=>d.Id==assignments.CongregationId))||(assignments.StageId is not null&&!displays.Any(d=>d.Id==assignments.StageId)))
            DisplayMessage.Text="A saved display is disconnected. Cancel to keep its assignment, or choose a new display and save.";
    }
    private void SaveAssignments(object sender,RoutedEventArgs e)
    {
        try{var value=new DisplayAssignments((CongregationDisplay.SelectedItem as Choice)?.Id,(StageDisplay.SelectedItem as Choice)?.Id);OutputAssignmentRules.Validate(value,displays);Result=value;DialogResult=true;}
        catch(ArgumentException error){DisplayMessage.Text=error.Message;}
    }
    private void Identify(object sender,RoutedEventArgs e)
    {
        foreach(var display in displays)
        {
            var overlay=new Window{WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,Background=Brushes.Black,ShowInTaskbar=false,Topmost=true,
                Content=new TextBlock{Text=display.Name,Foreground=Brushes.White,FontSize=48,TextAlignment=TextAlignment.Center,VerticalAlignment=VerticalAlignment.Center}};
            overlay.Show();WindowPlacement.Place(overlay,display);
            var timer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(2)};
            timer.Tick+=(_,_)=>{timer.Stop();overlay.Close();};timer.Start();
        }
    }
}
