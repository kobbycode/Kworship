namespace Kworship.Application;

public sealed record SongDraft(Guid Id, int Revision, string Title, string Lyrics);
public sealed record ScheduleDraft(Guid Id, int Revision, string Title, Guid[] SongIds);
public sealed record WorkspaceRecovery(int FormatVersion = 1, SongDraft? Song = null,
    ScheduleDraft? Schedule = null, Guid? LastScheduleId = null,
    double Width = 1440, double Height = 900, double LibraryWidth = 250, double ScheduleWidth = 270, double PreviewHeightRatio = 0.55);
