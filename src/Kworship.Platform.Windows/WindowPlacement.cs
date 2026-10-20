using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Kworship.Application;
namespace Kworship.Platform.Windows;
public static class WindowPlacement
{
    public static void Place(Window window,DisplayInfo display)
    {
        window.WindowState=WindowState.Normal;window.WindowStyle=WindowStyle.None;window.ResizeMode=ResizeMode.NoResize;
        var handle=new WindowInteropHelper(window).EnsureHandle();
        if(!SetWindowPos(handle,IntPtr.Zero,display.Left,display.Top,display.Width,display.Height,0x0014))
            throw new InvalidOperationException("Windows could not position the output window.");
    }
    [DllImport("user32.dll",SetLastError=true)]private static extern bool SetWindowPos(IntPtr handle,IntPtr after,int x,int y,int width,int height,uint flags);
}
