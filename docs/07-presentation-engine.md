# Proposed presentation engine

## State model
Preview = selected item revision, slide index and local edits. Live = immutable resolved item snapshot, slide index, composition version and transport. Output flags = black, clearText and freeze. Selected library records are never shared mutable objects with live output. Take resolves and validates all required resources before atomically committing a new live version. Failed preparation keeps the prior live state and displays an operator error.

| Command | Preview | Live / visible output |
|---|---|---|
| Select or preview next | changes | unchanged |
| Take | unchanged | new validated immutable snapshot |
| Live next/previous | unchanged | clamps to item boundary; increments version |
| Edit library/schedule | may refresh selected revision | unchanged until explicit take |
| Black | unchanged | highest priority opaque black composition |
| Clear text | unchanged | hides text layers; preserves background |
| Freeze | unchanged | holds displayed composition; new commits queue as latest state |
| Unfreeze | unchanged | renders latest committed state, subject to black/clear |
| Restart | recover drafts | black until operator explicitly resumes |

Proposed precedence: black > frozen base composition > current base composition; clear-text applies to a non-black base, including frozen text layers. Freeze holds visuals only; audio continues unless explicitly paused. Black does not mute audio. These policies require operator review and clear UI indicators; they are not claims about the reference app. Video freeze must capture a frame while maintaining separate playback ownership.

## Pipeline and resource policy
Command -> reducer -> resolve assets/fonts -> build immutable layout -> prewarm resources -> enqueue bounded render snapshot -> atomic render swap at frame boundary -> acknowledge committed visible version. A take must not show a new lyric with an old background. Stage and congregation derive from the same committed version but use different compositions.

UI never waits synchronously on decode, filesystem or output recovery. Cancellation discards stale preview work. Live commands have priority over thumbnail jobs. Keep at most current, transition-old and prepared-next heavy resources subject to a configurable memory budget; release GPU resources on owning threads. Decoder events carry item/version IDs so late callbacks cannot alter a newer presentation. Audio has exactly one defined owner with explicit crossfade policy.

Transitions blend complete frames, can be disabled, and fall back to a hard cut under resource pressure. Text layout uses safe-area bounds and explicit overflow warnings before take. Test Unicode, line breaks and missing fonts. Recovery re-enumerates displays and recreates device resources; a renderer crash cannot mutate persisted content.
