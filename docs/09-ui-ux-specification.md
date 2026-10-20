# Proposed interface specification

Original design, not a reconstruction of an observed EasyWorship layout. This is a detailed textual layout specification; high-fidelity rendered mockups remain pending review and reference materials.

## Workspace
At 1440 x 900 logical pixels: top command bar 56 px; left library 280 px; schedule column 280 px; remaining workspace has equal Preview and Live panels separated by a 16 px gutter; bottom status bar 28 px. Panels use splitters with minimum widths and collapse to tabs at small sizes. Store dimensions per user and clamp restored positions to available screens.

Dark palette: canvas #11151D, panels #1B2230, primary text #F3F5F7, secondary #B4BFCE, preview accent #63B3FF, live accent #FF6B76, focus #FFD166. Check actual contrast and color-vision variants before approval. Use system-accessible fonts, 14–16 px controls, 24 px headings, scalable UI and visible keyboard focus. Color always accompanies a text label/icon.

| Component | Purpose / interaction | State and dependency |
|---|---|---|
| Library tabs | Songs, Scripture, Media, Presentations, Themes | retained query per tab; loading/empty/error states |
| Search | type query; keyboard traverse results; explicit add or preview | cancellable query; no automatic take |
| Schedule | ordered items, duration/status, reorder and context actions | selected vs live badges distinct; persistence errors visible |
| Preview | upcoming content and slide thumbnails; edit/open | blue PREVIEW label; local selection never changes live |
| Live | congregation composition and visible-version status | red LIVE label; render acknowledgement or disconnected state |
| Take control | explicit commit with item title | enabled only when prepared; accessible name includes destination |
| Output toolbar | Black, Clear text, Freeze, display assignment | persistent pressed state; visible audio-continuing indicator |
| Song editor | metadata, section list/body, arrangement, theme and preview | dirty/save/error states; optimistic conflict dialog |
| Scripture picker | translation, book/chapter/verse, range, search | missing content and rights metadata; no silent fallback |
| Media browser | thumbnails, type, duration, rights/missing badges | import progress, retry/relink; playback failure details |
| Theme editor | font, alignment, safe area, background, scripture style | bounded preview; overflow validation |
| Settings | output identities, audio, recovery, theme, backup paths | validate before applying; reversible output test |
| Status bar | save health, output state, current time | nonblocking diagnostics; persistent critical errors |

Proposed shortcuts (not reference findings): Ctrl+F focuses search; Ctrl+S saves current editor/schedule; Enter previews selected result; Ctrl+Enter takes prepared preview; live next/previous only when presentation controls own focus. Text editing must never trigger navigation or black accidentally. Black/clear/freeze shortcuts require explicit mapping review and displayed labels.

Drag/drop offers insertion marker and undo, validates type, and never implicitly takes an item. Context menus expose the same actions as buttons. Destructive content deletion requires impact details. Dialogs return focus to their invoker; output windows never show modals. Full keyboard operation and screen-reader names are acceptance criteria.

## Reference interface map (separate from proposed design)
S01 documents a Schedule Area, bottom collapsible Resource Area, Preview Area and Live Area. Resources expose five library tabs and two or three columns depending on selected view, with Add Item, Action Menu and thumbnail/list controls. Preview holds selection and isolated navigation; double-click takes. Live holds current output and navigation, with no direct editing. Song editor provides Words/Slides, Masters and Inspector. Theme and presentation editors depend on media resources. S05 describes Edit > Options output/Foldback settings. Exact modal geometry, resize limits, focus order and persistent layout behavior remain unobserved.

The original [workspace mockup](workspace-mockup.svg) illustrates the proposed visual hierarchy at 1440×900. It is a static design artifact, not a functioning app or copied reference screenshot.
