using Kworship.Application;
using Kworship.Domain;
using Xunit;
namespace Kworship.Tests;
public sealed class PresentationTests
{
    private static PresentationItem Item(string title = "Original", int revision = 1) => new(Guid.NewGuid(), revision, title,
        [new(Guid.NewGuid(), "Verse", "First"), new(Guid.NewGuid(), "Chorus", "Second")]);
    private static PresentationState Live() => PresentationReducer.Apply(
        PresentationReducer.Apply(new(), new SelectPreview(Item())), new TakePreview());
    [Fact] public void StartupIsBlackAndEmpty()
    { var state = new PresentationState(); Assert.True(state.IsBlack); Assert.Null(state.Live); Assert.Equal("", state.VisibleText); }
    [Fact] public void PreviewSelectionAndNavigationNeverChangeLive()
    {
        var state = Live(); var live = state.Live;
        state = PresentationReducer.Apply(state, new SelectPreview(Item("Upcoming")));
        state = PresentationReducer.Apply(state, new MovePreview(1));
        Assert.Same(live, state.Live); Assert.Equal("Second", state.Preview!.Slide.Text); Assert.Equal(1, state.Version);
    }
    [Fact] public void TakeCommitsSelectedSlideWithoutRemovingBlack()
    {
        var state = PresentationReducer.Apply(new(), new SelectPreview(Item()));
        state = PresentationReducer.Apply(state, new MovePreview(1));
        state = PresentationReducer.Apply(state, new TakePreview());
        Assert.Equal("Second", state.Live!.Slide.Text); Assert.True(state.IsBlack);
        state = PresentationReducer.Apply(state, new SetBlack(false)); Assert.Equal("Second", state.VisibleText);
    }
    [Fact] public void SourceCollectionMutationDoesNotAlterSnapshot()
    {
        var slides = new List<Slide> { new(Guid.NewGuid(), "Verse", "Original") };
        var item = new PresentationItem(Guid.NewGuid(), 1, "Song", slides); slides.Clear();
        Assert.Single(item.Slides); Assert.Equal("Original", item.Slides[0].Text);
    }
    [Theory] [InlineData(int.MinValue,0)] [InlineData(int.MaxValue,1)]
    public void NavigationClampsWithoutOverflow(int delta, int expected)
    { var state = PresentationReducer.Apply(Live(), new MoveLive(delta)); Assert.Equal(expected, state.Live!.Index); }
    [Fact] public void FreezeKeepsVisualUntilReleasedWhileCommitsContinue()
    {
        var state = PresentationReducer.Apply(Live(), new SetBlack(false));
        state = PresentationReducer.Apply(state, new SetFreeze(true));
        state = PresentationReducer.Apply(state, new MoveLive(1));
        Assert.Equal("First", state.VisibleText); Assert.Equal("Second", state.Live!.Slide.Text);
        state = PresentationReducer.Apply(state, new SetFreeze(true)); Assert.Equal("First", state.VisibleText);
        state = PresentationReducer.Apply(state, new SetFreeze(false)); Assert.Equal("Second", state.VisibleText);
    }
    [Fact] public void BlackAndClearOverrideFrozenOutputWithoutDestroyingContent()
    {
        var state = PresentationReducer.Apply(Live(), new SetBlack(false));
        state = PresentationReducer.Apply(state, new SetFreeze(true));
        state = PresentationReducer.Apply(state, new SetClearText(true)); Assert.Equal("", state.VisibleText);
        state = PresentationReducer.Apply(state, new SetBlack(true)); Assert.Null(state.Visible);
        state = PresentationReducer.Apply(state, new SetBlack(false));
        state = PresentationReducer.Apply(state, new SetClearText(false)); Assert.Equal("First", state.VisibleText);
    }
    [Fact] public void TakeWithoutPreviewIsNoOp()
    { var state = new PresentationState(); Assert.Same(state, PresentationReducer.Apply(state, new TakePreview())); }
    [Fact] public void FailedOutputDoesNotCommitSession()
    {
        var session = new PresentationSession(new FailingOutput());
        Assert.Throws<IOException>(() => session.Dispatch(new SelectPreview(Item())));
        Assert.Null(session.State.Preview); Assert.Equal(0, session.State.Version);
    }
    [Fact] public void NewRevisionDoesNotMutateAlreadyLiveItem()
    {
        var state = Live(); var old = state.Live!.Item;
        var next = new PresentationItem(old.Id, 2, "Edited", [new(Guid.NewGuid(), "V1", "Changed")]);
        state = PresentationReducer.Apply(state, new SelectPreview(next));
        Assert.Equal("First", state.Live!.Slide.Text); Assert.Equal(1, state.Live.Item.Revision);
    }
    [Fact] public void EmptyOrDuplicateSlidesRejected()
    {
        Assert.Throws<ArgumentException>(() => new PresentationItem(Guid.NewGuid(), 1, "Song", []));
        var slide = new Slide(Guid.NewGuid(), "V1", "Text");
        Assert.Throws<ArgumentException>(() => new PresentationItem(Guid.NewGuid(), 1, "Song", [slide, slide]));
    }
    private sealed class FailingOutput : IPresentationOutput
    { public void Present(PresentationState state) => throw new IOException("Output unavailable"); }
}
