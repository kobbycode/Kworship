# Kworship 0.2.0 — operator workspace milestone

This milestone turns the Phase 2 foundation into a usable text-presentation workflow. It is an incremental development build, not the complete product specified in the master prompt.

## Implemented

- Dark, resizable library/service/preview/live workspace with independent panel splitters. Window dimensions, column widths and preview/editor ratio persist locally and are clamped on restoration.
- Song creation/editing, case-insensitive title/lyric search, save/discard controls, and guards against silently replacing unsaved edits. Blank lines define slides; `[Verse 1]` and `[Chorus]` label slides without appearing in projected lyrics. Slide IDs are deterministic within a saved song revision.
- Service creation, naming, saving, listing/reopening and duplication. Repeated songs are preserved. Drag-and-drop insertion and up/down/remove controls change only the working schedule, never the live snapshot.
- Independent preview selection, slide picker, navigation and explicit take. Black, clear and freeze controls retain visible state labels. Song edits and schedule operations cannot mutate already-live content.
- Separate congregation and stage text windows. Move each window to a screen manually; F11 toggles fullscreen and Escape exits fullscreen. Congregation follows black/clear/freeze. Stage follows committed live content and shows its next slide even when congregation output is black or frozen. Stage never follows unreleased preview selection. This is a deliberate original policy, not an observed EasyWorship guarantee.
- Background SQLite operations keep the UI dispatcher free for presentation commands. Editing is disabled during writes. Revision conflicts preserve the operator's draft.
- Debounced draft recovery and layout persistence in a separate versioned JSON file, with a flushed temporary file followed by replacement. Restart offers Recover/Dismiss; it never resumes live output automatically. Unreviewed drafts block new editing so they cannot be overwritten by a second draft. Invalid recovery files are preserved before replacement; if preservation fails, recovery writes are disabled.
- Database backup through the desktop command. The destination must be new, and backup contents are validated. Backups contain committed songs/services only, not draft recovery or future media assets.
- Ctrl+F for search and Ctrl+S for the focused editor or current service. Arrow keys inside editors are not presentation shortcuts.

## Architecture decisions

ADR-003: the observable workspace model lives in Kworship.Application, without WPF references. It owns working edits and services and uses the existing presentation session for atomic state commit after output delivery. WPF code manages dialogs, output windows, UI-specific selection/drag gestures and debounced persistence. Automated workspace tests exercise real SQLite rather than a fake persistence implementation.

ADR-004: recovery is offered rather than automatically taken live. Canonical content remains in SQLite; recovery stores pending working copies with their original revision. Saving a stale recovered draft conflicts rather than replacing a newer canonical revision. Recovery is local plaintext because it must preserve lyrics; it is not a substitute for backups or a secrecy boundary. Autosave runs after 700 ms of inactivity; abrupt termination before the save completes can lose the latest keystrokes. Power-loss durability of the whole workflow is not yet validated.

ADR-005: this milestone provides text-only output using WPF composition and aspect-preserving scaling. It does not settle the production GPU/media renderer decision. Manual window placement is an explicit temporary workflow; no automatic monitor identity, hot-plug recovery or frame-atomic coordination across physical outputs is claimed.

## Validation

Current Linux-host validation: 46 automated cases passed, zero failed/skipped. Full Release solution cross-build completed with zero warnings/errors. Tests cover the original engine/storage cases plus search, unsaved-edit guards, retained live snapshots, persistent service order and repeats, independent duplication, drag insertion in both directions, recovery and dismissal, stale recovered revisions, missing references, deterministic slide splitting and recovery-file round trips/replacement/version errors.

A self-contained Windows x64 build was produced with .NET runtime 10.0.13 included. Packaging checks required executable/runtime/SQLite/WPF files, the Windows executable header, ZIP CRCs and a SHA-256 receipt. Dependency notices are included. These checks establish package structure, not successful Windows execution.

The Windows CI workflow now includes publish, packaging and artifact upload after build/tests. The source is now pushed to GitHub. Build run 39381603997 passed on Windows and Linux, with 46 passing tests per platform. Release run 39382060566 verified both TRX counters and the Windows-built ZIP checksum before publishing the v0.2.0 development release. No installer/signing step was performed.

## Windows walkthrough — required, not yet executed

1. Extract the complete ZIP and launch `Kworship.exe` on Windows 11 x64. Confirm all panels are visible and keyboard focus/contrast are usable.
2. Create an original song with two labeled sections. Save it; search by a lyric fragment; select it and preview. Open congregation output, take, then switch Black off.
3. Navigate preview while the congregation remains unchanged. Take a different preview slide, navigate live, and verify clear/black/freeze behavior.
4. Open stage output; verify current/upcoming text follows live while congregation black/freeze settings are respected only on congregation composition.
5. Add repeated songs to a named service. Reorder by drag/drop and buttons, remove an item, save, reopen and duplicate. Confirm current live output is unchanged.
6. Change song lyrics without saving, wait at least one second and close/reopen. Recover the draft and confirm no output becomes live. Repeat for service title/order. Confirm saving/discarding or recovery dismissal is explicit.
7. Resize and move splitters, close/reopen, and verify usable restored dimensions. Test fullscreen/escape on manually placed output windows across different resolutions.
8. Save all edits, create a new database backup, and verify restore using the separately tested restore-to-new-database API before any manual activation.

Physical display behavior, accessibility, Windows launch, long-running sessions, interruption during disk writes and runtime visual appearance remain unverified here. The next major capabilities are structured song metadata/arrangements, licensed scripture import, reliable multimedia and real display management. None is represented as complete in this version.

## Published development package
[Windows ZIP](https://github.com/kobbycode/Kworship/releases/download/v0.2.0/Kworship-0.2.0-win-x64.zip) · [release notes](https://github.com/kobbycode/Kworship/releases/tag/v0.2.0). The asset was built by Windows CI from source commit 5a0913b; later publisher/documentation commits do not change that application binary.
