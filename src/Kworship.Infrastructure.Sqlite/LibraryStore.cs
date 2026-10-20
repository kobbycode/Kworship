using System.Security.Cryptography;
using System.Text;
using Kworship.Application;
using Microsoft.Data.Sqlite;

namespace Kworship.Infrastructure.Sqlite;

public sealed class LibraryStore : ILibraryStore
{
    private readonly string path;
    private const string Schema = """
        CREATE TABLE songs(id TEXT PRIMARY KEY, revision INTEGER NOT NULL CHECK(revision > 0),
          title TEXT NOT NULL, lyrics TEXT NOT NULL);
        CREATE TABLE schedules(id TEXT PRIMARY KEY, revision INTEGER NOT NULL CHECK(revision > 0), title TEXT NOT NULL);
        CREATE TABLE schedule_items(schedule_id TEXT NOT NULL REFERENCES schedules(id) ON DELETE CASCADE,
          ordinal INTEGER NOT NULL, song_id TEXT NOT NULL REFERENCES songs(id) ON DELETE RESTRICT,
          PRIMARY KEY(schedule_id, ordinal));
        """;
    private static string Checksum => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Schema)));
    public LibraryStore(string databasePath)
    {
        path = Path.GetFullPath(databasePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var db = Open();
        using var tx = db.BeginTransaction();
        Execute(db, tx, "CREATE TABLE IF NOT EXISTS schema_migrations(version INTEGER PRIMARY KEY, checksum TEXT NOT NULL)");
        using var query = Command(db, tx, "SELECT version, checksum FROM schema_migrations");
        using (var reader = query.ExecuteReader())
        {
            if (reader.Read())
            {
                if (reader.GetInt32(0) != 1 || reader.GetString(1) != Checksum || reader.Read())
                    throw new InvalidDataException("Unsupported or modified database schema.");
                tx.Commit(); return;
            }
        }
        Execute(db, tx, Schema);
        Execute(db, tx, "INSERT INTO schema_migrations VALUES(1,$hash)", ("$hash", Checksum));
        tx.Commit();
    }
    private SqliteConnection Open()
    {
        var db = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path,
            ForeignKeys = true, Pooling = false, DefaultTimeout = 5 }.ToString());
        db.Open();
        using var command = db.CreateCommand();
        command.CommandText = "PRAGMA synchronous=FULL;"; command.ExecuteNonQuery();
        return db;
    }
    private static SqliteCommand Command(SqliteConnection db, SqliteTransaction? tx, string sql, params (string, object)[] values)
    {
        var command = db.CreateCommand(); command.Transaction = tx; command.CommandText = sql;
        foreach (var (key, value) in values) command.Parameters.AddWithValue(key, value);
        return command;
    }
    private static int Execute(SqliteConnection db, SqliteTransaction? tx, string sql, params (string, object)[] values)
    { using var command = Command(db, tx, sql, values); return command.ExecuteNonQuery(); }
    private static void Validate(Guid id, string title, int expected)
    {
        if (id == Guid.Empty || expected < 0) throw new ArgumentException("Invalid ID or revision.");
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
    }
    public Song SaveSong(Song song, int expectedRevision)
    {
        Validate(song.Id, song.Title, expectedRevision); ArgumentException.ThrowIfNullOrWhiteSpace(song.Lyrics);
        using var db = Open(); using var tx = db.BeginTransaction();
        var sql = expectedRevision == 0
          ? "INSERT OR IGNORE INTO songs VALUES($id,1,$title,$lyrics)"
          : "UPDATE songs SET revision=revision+1,title=$title,lyrics=$lyrics WHERE id=$id AND revision=$expected";
        if (Execute(db, tx, sql, ("$id", song.Id.ToString()), ("$title", song.Title), ("$lyrics", song.Lyrics), ("$expected", expectedRevision)) != 1)
            throw new RevisionConflictException("Song changed since it was loaded. Reload before saving.");
        tx.Commit(); return song with { Revision = expectedRevision + 1 };
    }
    public Song? GetSong(Guid id)
    {
        using var db = Open();
        using var command = Command(db, null, "SELECT id,revision,title,lyrics FROM songs WHERE id=$id", ("$id", id.ToString()));
        using var reader = command.ExecuteReader();
        return reader.Read() ? new(Guid.Parse(reader.GetString(0)), reader.GetInt32(1), reader.GetString(2), reader.GetString(3)) : null;
    }
    public IReadOnlyList<Song> ListSongs()
    {
        using var db = Open(); using var command = Command(db, null, "SELECT id,revision,title,lyrics FROM songs ORDER BY title COLLATE NOCASE,id");
        using var reader = command.ExecuteReader(); var songs = new List<Song>();
        while (reader.Read()) songs.Add(new(Guid.Parse(reader.GetString(0)), reader.GetInt32(1), reader.GetString(2), reader.GetString(3)));
        return songs;
    }
    public ServiceSchedule SaveSchedule(ServiceSchedule schedule, int expectedRevision)
    {
        Validate(schedule.Id, schedule.Title, expectedRevision);
        // Materialize caller data before opening a transaction; later caller edits cannot change the saved order.
        var ids = schedule.SongIds.ToArray();
        using var db = Open(); using var tx = db.BeginTransaction();
        var sql = expectedRevision == 0 ? "INSERT OR IGNORE INTO schedules VALUES($id,1,$title)"
            : "UPDATE schedules SET revision=revision+1,title=$title WHERE id=$id AND revision=$expected";
        if (Execute(db, tx, sql, ("$id", schedule.Id.ToString()), ("$title", schedule.Title), ("$expected", expectedRevision)) != 1)
            throw new RevisionConflictException("Schedule changed since it was loaded.");
        Execute(db, tx, "DELETE FROM schedule_items WHERE schedule_id=$id", ("$id", schedule.Id.ToString()));
        for (var i = 0; i < ids.Length; i++)
            Execute(db, tx, "INSERT INTO schedule_items VALUES($id,$ordinal,$song)", ("$id", schedule.Id.ToString()), ("$ordinal", i), ("$song", ids[i].ToString()));
        tx.Commit(); return schedule with { Revision = expectedRevision + 1, SongIds = ids };
    }
    public ServiceSchedule? GetSchedule(Guid id)
    {
        using var db = Open(); using var tx = db.BeginTransaction();
        int revision; string title;
        using (var command = Command(db, tx, "SELECT revision,title FROM schedules WHERE id=$id", ("$id", id.ToString())))
        using (var reader = command.ExecuteReader())
        { if (!reader.Read()) return null; revision = reader.GetInt32(0); title = reader.GetString(1); }
        using var items = Command(db, tx, "SELECT song_id FROM schedule_items WHERE schedule_id=$id ORDER BY ordinal", ("$id", id.ToString()));
        using var rows = items.ExecuteReader(); var ids = new List<Guid>();
        while (rows.Read()) ids.Add(Guid.Parse(rows.GetString(0)));
        return new(id, revision, title, ids.AsReadOnly());
    }
    public IReadOnlyList<ServiceSchedule> ListSchedules()
    {
        using var db = Open();
        using var command = Command(db, null, """
            SELECT s.id,s.revision,s.title,i.song_id FROM schedules s
            LEFT JOIN schedule_items i ON s.id=i.schedule_id
            ORDER BY s.title COLLATE NOCASE,s.id,i.ordinal
            """);
        using var reader = command.ExecuteReader();
        var result = new List<ServiceSchedule>();
        Guid? currentId = null; int revision = 0; string title = ""; var ids = new List<Guid>();
        while (reader.Read())
        {
            var id = Guid.Parse(reader.GetString(0));
            if (currentId != id)
            {
                if (currentId is { } previous) result.Add(new(previous, revision, title, ids.ToArray()));
                currentId = id; revision = reader.GetInt32(1); title = reader.GetString(2); ids.Clear();
            }
            if (!reader.IsDBNull(3)) ids.Add(Guid.Parse(reader.GetString(3)));
        }
        if (currentId is { } last) result.Add(new(last, revision, title, ids.ToArray()));
        return result;
    }
    public void Backup(string destination)
    {
        var full = Path.GetFullPath(destination);
        if (File.Exists(full)) throw new IOException("Backup destination already exists; choose a new file.");
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        using var source = Open();
        using var target = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = full, Pooling = false }.ToString());
        target.Open(); source.BackupDatabase(target); Verify(target);
    }
    public static LibraryStore RestoreToNewDatabase(string backup, string destination)
    {
        if (!File.Exists(backup)) throw new FileNotFoundException("Backup not found.", backup);
        if (File.Exists(destination)) throw new IOException("Restore requires a new destination; existing data was preserved.");
        using var source = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path.GetFullPath(backup), Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString());
        source.Open(); Verify(source);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destination))!);
        using (var target = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path.GetFullPath(destination), Pooling = false }.ToString()))
        { target.Open(); source.BackupDatabase(target); }
        return new LibraryStore(destination);
    }
    private static void Verify(SqliteConnection db)
    {
        using var check = Command(db, null, "PRAGMA integrity_check");
        if ((string?)check.ExecuteScalar() != "ok") throw new InvalidDataException("Database integrity check failed.");
        using var keys = Command(db, null, "PRAGMA foreign_key_check");
        using var reader = keys.ExecuteReader();
        if (reader.Read()) throw new InvalidDataException("Database contains broken references.");
        reader.Close();
        using var schema = Command(db, null, "SELECT version, checksum FROM schema_migrations");
        using var versions = schema.ExecuteReader();
        if (!versions.Read() || versions.GetInt32(0) != 1 || versions.GetString(1) != Checksum || versions.Read())
            throw new InvalidDataException("Backup is not a supported Kworship library.");
    }
}
