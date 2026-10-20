using System.IO;
using System.Text.Json;
using System.Windows;
using Kworship.Application;
using Kworship.Infrastructure.Sqlite;
namespace Kworship.Desktop;
public partial class App : System.Windows.Application
{
    internal static readonly string DataRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Kworship");
    internal static void Log(string name, Exception? error = null)
    {
        try
        {
            Directory.CreateDirectory(DataRoot);
            var path = Path.Combine(DataRoot, "diagnostics.jsonl");
            if (File.Exists(path) && new FileInfo(path).Length > 2_000_000) File.Move(path, path + ".previous", true);
            File.AppendAllText(path, JsonSerializer.Serialize(new { timestamp = new SystemClock().UtcNow, name, errorType = error?.GetType().Name }) + Environment.NewLine);
        }
        catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args is ["--smoke-test", var directory])
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            RunSmokeTest(directory); return;
        }
        try { Log("startup"); MainWindow = new MainWindow(new LibraryStore(Path.Combine(DataRoot, "library.db"))); MainWindow.Show(); }
        catch (Exception ex) { Log("startup_failed", ex); MessageBox.Show("Kworship could not open its local library. Existing data has been preserved. Check the diagnostics file in LocalAppData/Kworship.", "Kworship"); Shutdown(1); }
    }
    private async void RunSmokeTest(string directory)
    { var result = await DesktopSmokeTest.RunAsync(this, Path.GetFullPath(directory)); Shutdown(result); }
}
