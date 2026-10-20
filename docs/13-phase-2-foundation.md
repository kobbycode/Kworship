# Phase 2 foundation: implementation and evidence

Date: 2026-10-20. The user's “do it” following the Phase 1 package authorized proceeding with the proposed foundation. Existing documents are retained as research history. This report supersedes their statements that no application exists.

## ADR-002: toolchain and implementation boundaries

Use .NET 10 LTS, SDK 10.0.402, pinned by global.json. Microsoft's [support documentation](https://learn.microsoft.com/en-us/dotnet/core/releases-and-support) identifies .NET 10 as LTS through November 2028. The [official release metadata](https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json) supplied the Linux x64 archive URL and SHA-512 used for installation. Package references and six NuGet lockfiles pin the current dependency graph. Updates require an intentional version/lockfile change and regression run.

The WPF recommendation is accepted for the foundation shell; production renderer and playback choices remain subject to the original hardware gates. Windows 11 x64 is the initial test target, not a claim of a fully established support matrix. Renderer, Office integration, codec licensing and stream transport decisions remain open.

## Projects

| Project | Implemented responsibility |
|---|---|
| Kworship.Domain | validated immutable slide/item snapshots; deterministic typed presentation reducer |
| Kworship.Application | library/output/clock contracts, song-to-slide conversion and output-before-state-commit session |
| Kworship.Infrastructure.Sqlite | SQLite schema v1, song and schedule transactions, revision conflict detection, backup/restore validation |
| Kworship.Platform.Windows | WPF text-output test adapter; no monitor enumeration or native GPU pipeline |
| Kworship.Desktop | minimal dark song editor/library and preview/live workspace, explicit controls, local diagnostics |
| Kworship.Tests | 24 discovered xUnit cases using real SQLite and deterministic state tests |

## Actual storage scope and policies

Schema v1 deliberately implements only songs, schedules, schedule items and migration receipts. It does not pretend to implement the complete database proposal. SQLite rollback journal and FULL synchronous durability are used, with foreign keys enabled on every connection, a five-second timeout and short operation-local connections. WAL is deferred until workload measurements justify it. Writers are serialized by SQLite transactions; stale revisions fail rather than overwriting another edit.

Song lyrics use blank lines to separate fixture slides; stable section editing and arrangements come later. Slide IDs are generated for each newly prepared song snapshot; snapshot identity is stable only for that preparation. Schedule item order preserves repeated song references. Schedule output uses IDs and does not yet persist a versioned presentation snapshot.

Backup uses SQLite BackupDatabase rather than copying an open database. It verifies integrity, foreign keys and supported migration metadata. Restore validates the source before copying and refuses existing destinations. Activation/replacement of a user's live database is deliberately not provided yet. There are no managed media assets in this schema, so these are database-only backups. Backup destinations should be unique files in an application-controlled local directory; concurrent filesystem replacement and hostile file races are outside this initial single-user API contract.

## Validation executed

Commands run from `/workspace/Kworship` after sourcing `scripts/cloud-env.sh`:

```bash
python3 scripts/install-cloud-sdk.py
dotnet restore Kworship.sln --locked-mode
dotnet build Kworship.sln --configuration Release --no-restore
dotnet test tests/Kworship.Tests/Kworship.Tests.csproj --configuration Release --no-build --no-restore --logger 'trx;LogFileName=foundation.trx'
```

Results: installer repeat recognized SDK; locked restore succeeded; six-project Release build succeeded with zero warnings/errors; 24 tests passed, zero failed/skipped. The current run's TRX is generated under `tests/Kworship.Tests/TestResults/foundation.trx` (ignored build output). No CI job has been remotely executed.

Tests exercise preview/live isolation, retained live revisions, defensive copying, safe startup, explicit takes, navigation overflow/boundaries, black/clear/freeze precedence, failed output delivery, Unicode/reopen persistence, revision conflicts, schedule order/repeats, foreign-key rollback without partial writes, backup round-trip, corrupt/unrelated database rejection, unsupported schema rejection and migration checksum validation.

Initial builds exposed one nullable-reference error in a test; corrected and rerun successfully. First-run SDK initialization failed on the read-only user home. A trace isolated named-mutex initialization; `XDG_RUNTIME_DIR` and `XDG_DATA_HOME` under `/workspace/toolchains` resolved it. Permission grants alone did not change the container's read-only mount. No HOME override, checksum bypass or TLS disablement was used.

## Required Windows validation — not executed

1. Run the README Windows commands with pinned SDK; verify native SQLite loads and all 24 cases pass.
2. Start the shell. Save an original two-slide song, close/reopen, and confirm exact title/lyrics persistence.
3. Preview/take a song, turn Black off, and open the text-output test window. Confirm output text follows live rather than preview selection/navigation.
4. Save an edited song while its previous snapshot is live; confirm output is unchanged until preview-and-take. Confirm Clear/Freeze/Black controls and output-window close/reopen behavior.
5. Verify keyboard editing, resize bounds, accessibility labels, local data path and failure messages. Dirty editor recovery and layout persistence are not yet implemented.
6. Run separate disposable media/GPU/display spikes on representative hardware before approving production output architecture. Record performance and device topology; a text test window does not validate projector operation.

Phase 2 implementation is reviewable, with Linux tests and cross-build verified. The original Windows execution/hardware exit gates remain pending. Do not label this a production application or claim the full Phase 2 validation gate is closed.
