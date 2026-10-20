using System.Text.Json;
using Kworship.Application;
namespace Kworship.Infrastructure.Sqlite;

// Draft recovery is separate from the canonical database and never activates live output.
public sealed class WorkspaceStateFile(string path)
{
    public WorkspaceRecovery? Load()
    {
        if (!File.Exists(path)) return null;
        var state = JsonSerializer.Deserialize<WorkspaceRecovery>(File.ReadAllText(path));
        if (state is null || state.FormatVersion != 1) throw new InvalidDataException("Unsupported workspace recovery file.");
        if (state.Song is { } song && (song.Id == Guid.Empty || song.Revision < 0 || song.Title is null || song.Lyrics is null || song.Author is null || song.Copyright is null || song.Arrangement is null))
            throw new InvalidDataException("Invalid song recovery data.");
        if (state.Schedule is { } schedule && (schedule.Id == Guid.Empty || schedule.Revision < 0 || schedule.Title is null || schedule.SongIds is null))
            throw new InvalidDataException("Invalid schedule recovery data.");
        return state;
    }
    public void Save(WorkspaceRecovery state)
    {
        var full = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        var temporary = full + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, state);
                stream.Flush(flushToDisk: true);
            }
            File.Move(temporary, full, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
