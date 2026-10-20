using System.Collections.Immutable;

namespace Kworship.Domain;

public sealed record Slide(Guid Id, string Label, string Text);
public sealed record PresentationItem
{
    public Guid Id { get; }
    public int Revision { get; }
    public string Title { get; }
    public ImmutableArray<Slide> Slides { get; }
    public PresentationItem(Guid id, int revision, string title, IEnumerable<Slide> slides)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        if (id == Guid.Empty || revision < 1) throw new ArgumentException("A stable ID and positive revision are required.");
        var copy = slides.ToImmutableArray();
        if (copy.IsEmpty || copy.Any(s => s.Id == Guid.Empty || s.Label is null || s.Text is null)
            || copy.Select(s => s.Id).Distinct().Count() != copy.Length)
            throw new ArgumentException("Provide at least one valid slide with unique IDs.");
        Id = id; Revision = revision; Title = title; Slides = copy;
    }
}
public sealed record Cursor(PresentationItem Item, int Index)
{
    public Slide Slide => Item.Slides[Index];
}
public sealed record PresentationState(Cursor? Preview = null, Cursor? Live = null,
    Cursor? Frozen = null, bool IsFrozen = false, bool IsBlack = true,
    bool IsTextClear = false, long Version = 0)
{
    public Cursor? Visible => IsBlack ? null : IsFrozen ? Frozen : Live;
    public string VisibleText => IsTextClear ? "" : Visible?.Slide.Text ?? "";
}
public abstract record PresentationCommand;
public sealed record SelectPreview(PresentationItem Item) : PresentationCommand;
public sealed record MovePreview(int Delta) : PresentationCommand;
public sealed record TakePreview : PresentationCommand;
public sealed record MoveLive(int Delta) : PresentationCommand;
public sealed record SetBlack(bool Value) : PresentationCommand;
public sealed record SetClearText(bool Value) : PresentationCommand;
public sealed record SetFreeze(bool Value) : PresentationCommand;

public static class PresentationReducer
{
    public static PresentationState Apply(PresentationState state, PresentationCommand command) => command switch
    {
        SelectPreview c => state with { Preview = new(c.Item, 0) },
        MovePreview c => state with { Preview = Move(state.Preview, c.Delta) },
        TakePreview => state.Preview is null ? state : state with { Live = state.Preview, Version = state.Version + 1 },
        MoveLive c => state.Live is null ? state : state with { Live = Move(state.Live, c.Delta), Version = state.Version + 1 },
        SetBlack c => state with { IsBlack = c.Value },
        SetClearText c => state with { IsTextClear = c.Value },
        SetFreeze c when c.Value && !state.IsFrozen => state with { IsFrozen = true, Frozen = state.Live },
        SetFreeze c when !c.Value => state with { IsFrozen = false, Frozen = null },
        SetFreeze => state,
        _ => throw new ArgumentOutOfRangeException(nameof(command))
    };
    private static Cursor? Move(Cursor? cursor, int delta) => cursor is null ? null : cursor with
    { Index = (int)Math.Clamp((long)cursor.Index + delta, 0, cursor.Item.Slides.Length - 1) };
}
