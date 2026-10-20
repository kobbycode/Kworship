using System.Text.Json;
using Kworship.Application;
using Kworship.Infrastructure.Sqlite;
using Xunit;
namespace Kworship.Tests;
public sealed class RecoveryTests : IDisposable
{
    private readonly string directory=Path.Combine(Path.GetTempPath(),"kworship-recovery-"+Guid.NewGuid());
    private string FilePath=>Path.Combine(directory,"workspace.json");
    [Fact] public void RecoveryRoundTripIncludesUnicodeDraftsAndLayout()
    {
        var file=new WorkspaceStateFile(FilePath);var id=Guid.NewGuid();
        file.Save(new(Song:new(id,3,"Café","Original text 🙏"),Schedule:new(Guid.NewGuid(),2,"Sunday",[id,id]),Width:1300,LibraryWidth:240));
        var loaded=file.Load()!;Assert.Equal("Café",loaded.Song!.Title);Assert.Equal("Original text 🙏",loaded.Song.Lyrics);
        Assert.Equal(new[]{id,id},loaded.Schedule!.SongIds);Assert.Equal(1300,loaded.Width);Assert.Equal(240,loaded.LibraryWidth);
    }
    [Fact] public void ReplacingDraftUsesOneCompleteFileAndLeavesNoTemporaryFiles()
    {
        var file=new WorkspaceStateFile(FilePath);file.Save(new(Song:new(Guid.NewGuid(),0,"First","Old")));
        file.Save(new());Assert.Null(file.Load()!.Song);Assert.Single(Directory.GetFiles(directory));
        Assert.NotNull(JsonSerializer.Deserialize<WorkspaceRecovery>(File.ReadAllText(FilePath)));
    }
    [Fact] public void CorruptOrFutureRecoveryIsNotSilentlyAccepted()
    {
        Directory.CreateDirectory(directory);File.WriteAllText(FilePath,"broken json");
        Assert.Throws<JsonException>(()=>new WorkspaceStateFile(FilePath).Load());Assert.Equal("broken json",File.ReadAllText(FilePath));
        File.WriteAllText(FilePath,"{\"FormatVersion\":99}");Assert.Throws<InvalidDataException>(()=>new WorkspaceStateFile(FilePath).Load());
    }
    [Fact] public void MissingFileIsAnOrdinaryFirstLaunch()=>Assert.Null(new WorkspaceStateFile(FilePath).Load());
    public void Dispose(){if(Directory.Exists(directory))Directory.Delete(directory,true);}
}
