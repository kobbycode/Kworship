using Kworship.Application;
using Kworship.Infrastructure.Sqlite;
using Microsoft.Data.Sqlite;
using Xunit;
namespace Kworship.Tests;
public sealed class StorageTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "kworship-tests-" + Guid.NewGuid());
    private string Database => Path.Combine(directory, "library.db");
    private LibraryStore Store() => new(Database);
    private static Song Song(string title = "Original") => new(Guid.NewGuid(), 0, title, "Verse one\n\nChorus — café 🙏");
    [Fact] public void SongUnicodeAndRevisionSurviveReopen()
    {
        var saved = Store().SaveSong(Song(), 0); Assert.Equal(1, saved.Revision);
        Assert.Equal(saved, Store().GetSong(saved.Id));
        var edited = Store().SaveSong(saved with { Title = "Edited" }, 1);
        Assert.Equal(2, edited.Revision); Assert.Equal(edited, Store().GetSong(saved.Id));
    }
    [Fact] public void StaleWriteAndDuplicateCreationPreserveCommittedSong()
    {
        var store = Store(); var saved = store.SaveSong(Song(), 0);
        var edited = store.SaveSong(saved with { Title = "Winner" }, 1);
        Assert.Throws<RevisionConflictException>(() => store.SaveSong(saved with { Title = "Stale" }, 1));
        Assert.Throws<RevisionConflictException>(() => store.SaveSong(saved, 0));
        Assert.Equal(edited, store.GetSong(saved.Id));
    }
    [Fact] public void ScheduleOrderAndRepeatedSongsRoundTrip()
    {
        var store = Store(); var a = store.SaveSong(Song("A"),0); var b = store.SaveSong(Song("B"),0);
        var saved = store.SaveSchedule(new(Guid.NewGuid(),0,"Sunday",[a.Id,b.Id,a.Id]),0);
        var loaded = Store().GetSchedule(saved.Id)!;
        Assert.Equal(1,loaded.Revision); Assert.Equal(saved.SongIds,loaded.SongIds);
    }
    [Fact] public void BrokenReferenceRollsBackScheduleRevisionAndOrder()
    {
        var store = Store(); var song = store.SaveSong(Song(),0);
        var saved = store.SaveSchedule(new(Guid.NewGuid(),0,"Sunday",[song.Id]),0);
        Assert.Throws<SqliteException>(() => store.SaveSchedule(saved with { Title = "Invalid", SongIds = new[] { Guid.NewGuid() } },1));
        var loaded = store.GetSchedule(saved.Id)!;
        Assert.Equal("Sunday",loaded.Title); Assert.Equal(1,loaded.Revision); Assert.Equal(saved.SongIds,loaded.SongIds);
    }
    [Fact] public void NewInvalidScheduleLeavesNoPartialRecord()
    {
        var store = Store(); var id=Guid.NewGuid();
        Assert.Throws<SqliteException>(() => store.SaveSchedule(new(id,0,"Bad",[Guid.NewGuid()]),0));
        Assert.Null(store.GetSchedule(id));
    }
    [Fact] public void StaleScheduleWriteRejected()
    {
        var store = Store(); var saved = store.SaveSchedule(new(Guid.NewGuid(),0,"Sunday",[]),0);
        store.SaveSchedule(saved with { Title = "Updated" },1);
        Assert.Throws<RevisionConflictException>(() => store.SaveSchedule(saved,1));
        Assert.Equal("Updated",store.GetSchedule(saved.Id)!.Title);
    }
    [Fact] public void BackupRestoresPriorSnapshotToNewDatabase()
    {
        var store = Store(); var saved = store.SaveSong(Song(),0);
        var schedule = store.SaveSchedule(new(Guid.NewGuid(),0,"Sunday",[saved.Id]),0);
        var backup=Path.Combine(directory,"backup.db"); store.Backup(backup);
        store.SaveSong(saved with { Title = "Later edit" },1);
        var restored=LibraryStore.RestoreToNewDatabase(backup,Path.Combine(directory,"restored.db"));
        Assert.Equal(saved,restored.GetSong(saved.Id)); Assert.Equal(schedule.SongIds,restored.GetSchedule(schedule.Id)!.SongIds);
        Assert.Equal("Later edit",store.GetSong(saved.Id)!.Title);
        Assert.Throws<IOException>(() => store.Backup(backup));
        Assert.Throws<IOException>(() => LibraryStore.RestoreToNewDatabase(backup,Database));
    }
    [Fact] public void CorruptBackupDoesNotReplaceExistingData()
    {
        var store = Store(); var saved=store.SaveSong(Song(),0);
        var corrupt=Path.Combine(directory,"broken.db"); File.WriteAllText(corrupt,"not a database");
        Assert.Throws<SqliteException>(() => LibraryStore.RestoreToNewDatabase(corrupt,Path.Combine(directory,"restore.db")));
        Assert.Equal(saved,store.GetSong(saved.Id)); Assert.False(File.Exists(Path.Combine(directory,"restore.db")));
    }
    [Fact] public void ChangedMigrationChecksumIsRejected()
    {
        _ = Store(); using(var db = new SqliteConnection($"Data Source={Database};Pooling=False"))
        { db.Open(); using var command=db.CreateCommand(); command.CommandText="UPDATE schema_migrations SET checksum='tampered'"; command.ExecuteNonQuery(); }
        Assert.Throws<InvalidDataException>(() => Store());
    }
    [Fact] public void InvalidSongDoesNotPersist()
    { var store=Store(); Assert.Throws<ArgumentException>(()=>store.SaveSong(Song() with { Title=" " },0)); Assert.Empty(store.ListSongs()); }
    [Fact] public void ForeignDatabaseIsRejectedBeforeRestoreDestinationIsCreated()
    {
        Directory.CreateDirectory(directory);
        var foreign = Path.Combine(directory,"foreign.db");
        using(var db = new SqliteConnection($"Data Source={foreign};Pooling=False"))
        { db.Open(); using var command = db.CreateCommand(); command.CommandText="CREATE TABLE unrelated(id INTEGER)"; command.ExecuteNonQuery(); }
        var destination = Path.Combine(directory,"destination.db");
        Assert.Throws<SqliteException>(()=>LibraryStore.RestoreToNewDatabase(foreign,destination));
        Assert.False(File.Exists(destination));
    }
    [Fact] public void UnsupportedBackupMigrationIsRejectedBeforeCopy()
    {
        var store=Store(); store.SaveSong(Song(),0); var backup=Path.Combine(directory,"backup.db"); store.Backup(backup);
        using(var db=new SqliteConnection($"Data Source={backup};Pooling=False"))
        { db.Open(); using var command=db.CreateCommand(); command.CommandText="UPDATE schema_migrations SET version=99 WHERE version=2"; command.ExecuteNonQuery(); }
        var destination=Path.Combine(directory,"destination.db");
        Assert.Throws<InvalidDataException>(()=>LibraryStore.RestoreToNewDatabase(backup,destination));
        Assert.False(File.Exists(destination));
    }
    public void Dispose() { if (Directory.Exists(directory)) Directory.Delete(directory,true); }
}
