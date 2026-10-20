# Version 0.3.0 — structured songs and display setup

## Scope

This milestone adds a section-based song editor, custom play order, author/copyright credits, and saved congregation/stage display assignments. Scripture and multimedia remain later milestones. The application still presents text.

## Song workflow

Choose **Sections & details** from the song editor. Edit the title, author and copyright/license credit; add verses, choruses or bridges and edit each section. Section labels must be unique when using this editor. The play-order list can reference a section repeatedly; renaming that section updates its existing play-order entries. Remove a section from the play order before deleting it. An empty play order uses section order.

**Apply to song editor** updates the working draft; **Save song** persists it. Cancel leaves the working draft untouched. Unapplied modal edits are not included in crash recovery; apply them first. Saving or arranging a song never mutates an existing live snapshot. Author/copyright credits appear on congregation output and follow its black/clear/freeze state.

Existing raw lyrics remain supported. Labels use `[Verse 1]` and blank lines delimit slides. Custom orders reference labels, case-insensitively, and reject missing or ambiguous labels. Raw edits that rename a referenced label must also update the play order through Sections & details. Search includes author names.

ADR-006: this migration retains the existing canonical lyric representation and stores play order as validated label references. The structured editor holds references to section objects so renames update repeated order entries. Stable independently persisted section UUIDs, rich formatting and external format compatibility remain future schema work; this milestone does not claim those capabilities.

## Library upgrade and recovery

Schema v2 adds author, copyright and arrangement columns with empty defaults. Before upgrading a v1 database, the application creates an SQLite online backup next to it named `library.db.before-v2-<id>.db` and verifies integrity, foreign keys and migration receipts. Schema changes and the new receipt are committed in a single transaction. Failed upgrades retain the pre-upgrade backup and roll back changes.

The first Windows CI run caught a historical line-ending problem: v1 hashed raw SQL strings whose LF/CRLF endings depend on checkout platform. ADR-007: accept only the two known v1 hashes derived from that exact released SQL, and normalize new migration definitions to LF before hashing. Unknown versions or other hashes are rejected. Regression fixtures cover both historical receipts. The SQL contents and validation are not discarded or replaced to ignore corruption.

Backups remain database-only. Metadata and play order are included; unsaved workspace drafts are separate. Restoring a v1 backup into a new destination triggers the same verified upgrade. Older releases cannot open v2: downgrading requires restoring the preserved v1 backup. Do not delete the library to recover from an upgrade error.

## Display setup

**Display setup** enumerates active Windows extended-desktop monitors, shows identity/resolution/primary status, and offers a short Identify overlay. Choose distinct congregation and stage screens, or use windowed mode for one-screen preparation. Missing saved screens are reported; the application does not silently substitute an operator display.

Assignments persist in workspace settings, not by screen enumeration index. Where Windows supplies a monitor device-interface identity it is used; otherwise the adapter falls back to the display name and warns that its identity is weak. Both cases require explicit confirmation before opening assigned fullscreen output, including a primary/operator warning. Confirming congregation output opens it black; the operator reveals it explicitly. Opening a stage output does not black out an already-live congregation surface.

ADR-008: use Windows monitor enumeration, device-interface IDs and physical pixel bounds, with PerMonitorV2 DPI awareness. Assigned windows are borderless and positioned via SetWindowPos. Full production monitor identity rules across hardware/port changes still require a physical device matrix.

On display changes, window messages hide output immediately where delivered, then topology handling blacks congregation output and closes affected outputs while retaining content and assignments. Windowed outputs also suspend because Windows may otherwise relocate them onto the operator desktop. Reconnection never resumes content automatically: reopen, confirm the target, then explicitly reveal congregation content. There is no claim of instantaneous or flicker-free recovery on every GPU/driver; that requires hardware testing.

## Validation and remaining checks

The regression suite includes section order/repetition, rename references, invalid orders, metadata persistence/recovery, retained live revisions, author search, backups, both v1 upgrade receipts, stable display selection, topology changes and assignment persistence.

The Windows UI smoke uses real WPF controls to apply structured edits, save metadata, preview a repeated arrangement, render credits and configure displays. It reads actual Windows monitor identities and checks native output-window pixel bounds. A simulated catalog then exercises disconnect, reconnect, explicit reopen and stage-opening isolation. These topology simulations do not establish physical cable or mixed-DPI behavior. Captured screenshots include the workspace, structured editor and display setup.

Required manual rehearsal: back up a real library; upgrade and inspect existing songs/services; create a repeated song order; identify and assign physical projector/stage displays; test 1080p/4K and unequal DPI; disconnect/reconnect during text output and confirm safe suspension/reopen; rehearse a complete saved song service. Video, scripture, PowerPoint, streaming, timers, automatic typography fitting and installer/signing are not delivered here.

Build run 39388734845 passed on Windows and Linux: 64 regression tests per platform and the expanded Windows UI smoke. The first Windows legacy-migration test failed on receipt line endings; that diagnosis was corrected and both known historical forms were verified by the successful rerun.

Release v0.3.0 is published from tested application commit 0428fa0. [Windows ZIP](https://github.com/kobbycode/Kworship/releases/download/v0.3.0/Kworship-0.3.0-win-x64.zip), [structured editor screenshot](https://github.com/kobbycode/Kworship/releases/download/v0.3.0/structured-song.png), and [display setup screenshot](https://github.com/kobbycode/Kworship/releases/download/v0.3.0/display-setup.png). Public download returned HTTP 200; the Windows-rendered screenshots were inspected.
