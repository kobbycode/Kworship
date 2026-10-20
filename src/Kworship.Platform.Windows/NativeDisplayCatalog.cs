using System.Runtime.InteropServices;
using Kworship.Application;
using Microsoft.Win32;
namespace Kworship.Platform.Windows;

public sealed class NativeDisplayCatalog:IDisplayCatalog
{
    public event EventHandler? Changed;
    public NativeDisplayCatalog()=>SystemEvents.DisplaySettingsChanged+=SettingsChanged;
    private void SettingsChanged(object? sender,EventArgs args)=>Changed?.Invoke(this,EventArgs.Empty);
    public void Dispose()=>SystemEvents.DisplaySettingsChanged-=SettingsChanged;
    public IReadOnlyList<DisplayInfo> GetDisplays()
    {
        var result=new List<DisplayInfo>();
        MonitorEnum callback=(IntPtr monitor,IntPtr dc,ref NativeRect bounds,IntPtr data)=>
        {
            var info=new MonitorInfo{Size=Marshal.SizeOf<MonitorInfo>(),Device=""};
            if(!GetMonitorInfo(monitor,ref info))return true;
            var device=new DisplayDevice{Size=Marshal.SizeOf<DisplayDevice>(),Name="",Description="",Id="",Key=""};
            var identified=EnumDisplayDevices(info.Device,0,ref device,1)&&!string.IsNullOrWhiteSpace(device.Id);
            var id=identified?device.Id:info.Device;
            var name=$"Display {result.Count+1}"+(string.IsNullOrWhiteSpace(device.Description)?"":" — "+device.Description);
            result.Add(new(id,name,(info.Flags&1)!=0,info.Bounds.Left,info.Bounds.Top,
                info.Bounds.Right-info.Bounds.Left,info.Bounds.Bottom-info.Bounds.Top,identified));
            return true;
        };
        if(!EnumDisplayMonitors(IntPtr.Zero,IntPtr.Zero,callback,IntPtr.Zero))throw new InvalidOperationException("Windows could not enumerate displays.");
        return result;
    }
    private delegate bool MonitorEnum(IntPtr monitor,IntPtr dc,ref NativeRect bounds,IntPtr data);
    [StructLayout(LayoutKind.Sequential)]private struct NativeRect{public int Left,Top,Right,Bottom;}
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]private struct MonitorInfo
    {public int Size;public NativeRect Bounds,Work;public uint Flags;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)]public string Device;}
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]private struct DisplayDevice
    {public int Size;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)]public string Name;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)]public string Description;public uint Flags;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)]public string Id;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)]public string Key;}
    [DllImport("user32.dll")]private static extern bool EnumDisplayMonitors(IntPtr dc,IntPtr clip,MonitorEnum callback,IntPtr data);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]private static extern bool GetMonitorInfo(IntPtr monitor,ref MonitorInfo info);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]private static extern bool EnumDisplayDevices(string device,uint index,ref DisplayDevice info,uint flags);
}
