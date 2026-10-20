using Kworship.Domain;
namespace Kworship.Application;

public sealed class RevisionConflictException(string message) : Exception(message);
public sealed record Song(Guid Id, int Revision, string Title, string Lyrics);
public sealed record ServiceSchedule(Guid Id, int Revision, string Title, IReadOnlyList<Guid> SongIds);
public interface ILibraryStore
{
    Song SaveSong(Song song, int expectedRevision);
    Song? GetSong(Guid id);
    IReadOnlyList<Song> ListSongs();
    ServiceSchedule SaveSchedule(ServiceSchedule schedule, int expectedRevision);
    ServiceSchedule? GetSchedule(Guid id);
    IReadOnlyList<ServiceSchedule> ListSchedules();
}
public interface IPresentationOutput { void Present(PresentationState state); }
public interface IClock { DateTimeOffset UtcNow { get; } }
public sealed class SystemClock : IClock { public DateTimeOffset UtcNow => DateTimeOffset.UtcNow; }
public static class SongPresentation
{
    public static PresentationItem Create(Song song)
    {
        var blocks = System.Text.RegularExpressions.Regex.Split(song.Lyrics.Replace("\r\n", "\n"), @"\n[ \t]*\n+")
            .Select(block => block.Trim()).Where(block => block.Length > 0).ToArray();
        var slides = blocks.Select((block, index) =>
        {
            var lines = block.Split('\n');
            var labeled = lines[0].StartsWith('[') && lines[0].EndsWith(']') && lines.Length > 1;
            var label = labeled ? lines[0][1..^1] : $"Slide {index + 1}";
            var text = labeled ? string.Join('\n', lines.Skip(1)).Trim() : block;
            var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"{song.Id}:{song.Revision}:{index}"));
            return new Slide(new Guid(hash.AsSpan(0, 16)), label, text);
        });
        return new(song.Id, song.Revision, song.Title, slides);
    }
}
public sealed class PresentationSession(IPresentationOutput output)
{
    public PresentationState State { get; private set; } = new();
    public void Dispatch(PresentationCommand command)
    {
        var next = PresentationReducer.Apply(State, command);
        // Output failures leave the application state unchanged. Native adapters must swap atomically.
        output.Present(next);
        State = next;
    }
}
