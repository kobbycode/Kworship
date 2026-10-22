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
    private const string Migration2 = """
        ALTER TABLE songs ADD COLUMN author TEXT NOT NULL DEFAULT '';
        ALTER TABLE songs ADD COLUMN copyright TEXT NOT NULL DEFAULT '';
        ALTER TABLE songs ADD COLUMN arrangement TEXT NOT NULL DEFAULT '';
        """;
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value.Replace("\r\n","\n"))));
    // Released v1 builds hashed raw source line endings. Accept only those two known forms.
    private static string LegacyWindowsChecksum => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Schema.Replace("\r\n","\n").Replace("\n","\r\n"))));
    private static string Checksum => Hash(Schema);
    public string? MigrationBackupPath { get; }
    public LibraryStore(string databasePath)
    {
        path = Path.GetFullPath(databasePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var db = Open();
        using (var exists = Command(db,null,"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='schema_migrations'"))
        {
            if (Convert.ToInt32(exists.ExecuteScalar()) != 0 && ReadMigrations(db,null)==1)
            {
                MigrationBackupPath=path+".before-v2-"+Guid.NewGuid().ToString("N")+".db";
                using var backup = new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=MigrationBackupPath,Pooling=false}.ToString());
                backup.Open(); db.BackupDatabase(backup); Verify(backup);
            }
        }
        using var tx = db.BeginTransaction();
        Execute(db,tx,"CREATE TABLE IF NOT EXISTS schema_migrations(version INTEGER PRIMARY KEY, checksum TEXT NOT NULL)");
        var version=ReadMigrations(db,tx);
        if(version==0)
        {
            Execute(db,tx,Schema);
            Execute(db,tx,"INSERT INTO schema_migrations VALUES(1,$hash)",("$hash",Checksum));
        }
        if(version<2)
        {
            Execute(db,tx,Migration2);
            Execute(db,tx,"INSERT INTO schema_migrations VALUES(2,$hash)",("$hash",Hash(Migration2)));
        }
        tx.Commit();
    }
    private static int ReadMigrations(SqliteConnection db,SqliteTransaction? tx)
    {
        using var query=Command(db,tx,"SELECT version,checksum FROM schema_migrations ORDER BY version");
        using var rows=query.ExecuteReader(); var expected=1;
        while(rows.Read())
        {
            var version=rows.GetInt32(0);
            var hash=version switch {1=>Checksum,2=>Hash(Migration2),_=>throw new InvalidDataException("Unsupported database schema version.")};
            if(version!=expected || (rows.GetString(1)!=hash && !(version==1&&rows.GetString(1)==LegacyWindowsChecksum)))throw new InvalidDataException("Unsupported or modified database schema.");
            expected++;
        }
        return expected-1;
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
        _ = SongPresentation.Create(song with { Revision=Math.Max(1,expectedRevision+1) });
        using var db = Open(); using var tx = db.BeginTransaction();
        var sql = expectedRevision == 0
          ? "INSERT OR IGNORE INTO songs(id,revision,title,lyrics,author,copyright,arrangement) VALUES($id,1,$title,$lyrics,$author,$copyright,$arrangement)"
          : "UPDATE songs SET revision=revision+1,title=$title,lyrics=$lyrics,author=$author,copyright=$copyright,arrangement=$arrangement WHERE id=$id AND revision=$expected";
        if (Execute(db, tx, sql, ("$id", song.Id.ToString()), ("$title", song.Title), ("$lyrics", song.Lyrics), ("$author",song.Author), ("$copyright",song.Copyright), ("$arrangement",song.Arrangement), ("$expected", expectedRevision)) != 1)
            throw new RevisionConflictException("Song changed since it was loaded. Reload before saving.");
        tx.Commit(); return song with { Revision = expectedRevision + 1 };
    }
    public HymnImportResult ImportSongs(IReadOnlyList<Song> songs)
    {
        var candidates=songs.ToArray();
        if(candidates.Length is 0 or > HymnImport.MaximumSongs)throw new ArgumentException("Import between 1 and 2,000 hymns.");
        foreach(var song in candidates)
        {
            Validate(song.Id,song.Title,0);
            if(song.Revision!=0)throw new ArgumentException("Import only new songs; existing revisions cannot be replaced.");
            ArgumentException.ThrowIfNullOrWhiteSpace(song.Lyrics);
            _=SongPresentation.Create(song with{Revision=1});
        }
        using var db=Open();using var tx=db.BeginTransaction();
        var known=new HashSet<string>();
        using(var query=Command(db,tx,"SELECT id,revision,title,lyrics,author,copyright,arrangement FROM songs"))
        using(var rows=query.ExecuteReader())
            while(rows.Read())known.Add(HymnImport.Fingerprint(new(Guid.Parse(rows.GetString(0)),rows.GetInt32(1),rows.GetString(2),rows.GetString(3),rows.GetString(4),rows.GetString(5),rows.GetString(6))));
        int added=0,skipped=0;
        foreach(var song in candidates)
        {
            if(!known.Add(HymnImport.Fingerprint(song))){skipped++;continue;}
            // A colliding ID is an error, never an update. Transaction rollback includes prior inserts.
            Execute(db,tx,"INSERT INTO songs(id,revision,title,lyrics,author,copyright,arrangement) VALUES($id,1,$title,$lyrics,$author,$copyright,$arrangement)",
                ("$id",song.Id.ToString()),("$title",song.Title),("$lyrics",song.Lyrics),("$author",song.Author),("$copyright",song.Copyright),("$arrangement",song.Arrangement));
            added++;
        }
        tx.Commit();return new(added,skipped);
    }
    public Song? GetSong(Guid id)
    {
        using var db = Open();
        using var command = Command(db, null, "SELECT id,revision,title,lyrics,author,copyright,arrangement FROM songs WHERE id=$id", ("$id", id.ToString()));
        using var reader = command.ExecuteReader();
        return reader.Read() ? new(Guid.Parse(reader.GetString(0)), reader.GetInt32(1), reader.GetString(2), reader.GetString(3),reader.GetString(4),reader.GetString(5),reader.GetString(6)) : null;
    }
    public IReadOnlyList<Song> ListSongs()
    {
        using var db = Open(); using var command = Command(db, null, "SELECT id,revision,title,lyrics,author,copyright,arrangement FROM songs ORDER BY title COLLATE NOCASE,id");
        using var reader = command.ExecuteReader(); var songs = new List<Song>();
        while (reader.Read()) songs.Add(new(Guid.Parse(reader.GetString(0)), reader.GetInt32(1), reader.GetString(2), reader.GetString(3),reader.GetString(4),reader.GetString(5),reader.GetString(6)));
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
        if(ReadMigrations(db,null)==0)throw new InvalidDataException("Backup has no supported schema migration.");
    }
}
