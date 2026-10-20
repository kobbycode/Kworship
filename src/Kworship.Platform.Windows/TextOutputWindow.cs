using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Kworship.Application;
using Kworship.Domain;
namespace Kworship.Platform.Windows;

// Text-only output adapter. Physical display assignment and GPU/video rendering are separate milestones.
public sealed class TextOutputWindow : Window, IPresentationOutput
{
    public event EventHandler? TopologyChanged;
    private readonly bool stage;
    private readonly TextBlock text = new() { Foreground = Brushes.White, FontSize = 50,
        TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
    private readonly TextBlock next = new() { Foreground = new SolidColorBrush(Color.FromRgb(175,189,208)), FontSize = 28,
        TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center };
    private readonly TextBlock heading = new() { Foreground = new SolidColorBrush(Color.FromRgb(255,209,102)), FontSize = 22 };
    private readonly TextBlock credit = new(){Foreground=new SolidColorBrush(Color.FromRgb(175,189,208)),FontSize=18,TextAlignment=TextAlignment.Center,Margin=new Thickness(48,8,48,8)};
    private bool fullscreen;
    public TextOutputWindow(bool stage = false)
    {
        this.stage = stage;
        SourceInitialized+=(_,_)=>
        {
            var source=HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            source?.AddHook((IntPtr hwnd,int message,IntPtr wParam,IntPtr lParam,ref bool handled)=>
            {
                if(message==0x007E){text.Text="";heading.Text="";next.Text="";credit.Text="";Hide();TopologyChanged?.Invoke(this,EventArgs.Empty);}
                return IntPtr.Zero;
            });
        };
        Title = stage ? "Kworship · Stage — F11 fullscreen / Esc exit fullscreen" : "Kworship · Congregation — F11 fullscreen / Esc exit fullscreen";
        Width = 960; Height = 540; Background = Brushes.Black;
        var canvas = new Grid { Width = 1280, Height = 720, Background = Brushes.Black, Margin = new Thickness(0) };
        canvas.RowDefinitions.Add(new() { Height = new GridLength(stage ? 80 : 0) });
        canvas.RowDefinitions.Add(new());
        canvas.RowDefinitions.Add(new() { Height = new GridLength(stage ? 180 : 48) });
        heading.Margin = new Thickness(48,24,48,0); canvas.Children.Add(heading);
        text.Margin = new Thickness(64,24,64,24); Grid.SetRow(text,1); canvas.Children.Add(text);
        next.Margin = new Thickness(64,24,64,24); Grid.SetRow(next,2); canvas.Children.Add(next);
        if(!stage){Grid.SetRow(credit,2);canvas.Children.Add(credit);}
        Content = new Viewbox { Stretch = Stretch.Uniform, Child = canvas };
        PreviewKeyDown += (_, args) =>
        {
            if (args.Key == Key.F11) { SetFullscreen(!fullscreen); args.Handled = true; }
            else if (args.Key == Key.Escape && fullscreen) { SetFullscreen(false); args.Handled = true; }
        };
    }
    private void SetFullscreen(bool value)
    {
        fullscreen = value;
        if (value) { WindowState = WindowState.Normal; WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize; WindowState = WindowState.Maximized; }
        else { WindowState = WindowState.Normal; WindowStyle = WindowStyle.SingleBorderWindow; ResizeMode = ResizeMode.CanResize; }
    }
    public void Present(PresentationState state)
    {
        Dispatcher.VerifyAccess();
        if (!stage) { text.Text = state.VisibleText; credit.Text=state.IsTextClear?"":state.Visible?.Item.Credit??""; return; }
        var live = state.Live;
        heading.Text = $"STAGE  /  {live?.Item.Title ?? "Waiting for live content"}  /  {(state.IsBlack ? "CONGREGATION BLACK" : state.IsFrozen ? "CONGREGATION FROZEN" : "LIVE")}";
        text.Text = live?.Slide.Text ?? "";
        next.Text = live is { } cursor && cursor.Index + 1 < cursor.Item.Slides.Length
            ? "NEXT\n" + cursor.Item.Slides[cursor.Index + 1].Text : "END OF ITEM";
    }
}
