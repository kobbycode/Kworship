# Kworship

A Windows worship presentation application in active development. Version 0.2.0 provides a local song library, service planning and text presentation with separate preview/live controls.

## Try the Windows development build

Extract the complete `Kworship-0.2.0-win-x64.zip` folder and run **Kworship.exe** on Windows 11 x64. The .NET runtime is included. See the included `QUICKSTART.txt` for a walkthrough.

Create a song, separate slides with blank lines, save, select it in the library and preview. Open congregation output, choose **Take**, then toggle **Black** off. Stage output shows current/upcoming live lyrics. Move the output windows to your extended-desktop screens and use **F11** for fullscreen; **Esc** exits fullscreen.

## Available features

- Search titles/lyrics; create/edit songs with optional `[Verse 1]` section labels.
- Save/open/duplicate services; add repeated songs and reorder by drag/drop or buttons.
- Independent preview/live slides, explicit take, black/clear/freeze, and two text-output windows.
- Resizable dark workspace, keyboard search/save, unsaved-edit protection and offered draft recovery.
- SQLite transactions, revision conflicts, checked migrations and verified database backups.

Local data is stored in `%LOCALAPPDATA%\Kworship`: `library.db` contains saved songs/services, `workspace.json` contains draft recovery/layout, and `diagnostics.jsonl` contains diagnostics. Database backups exclude unsaved drafts. Recovery is debounced by 700 ms and never automatically activates live output.

This development build is not validated for live services. It has no bundled song/Bible corpus, scripture module, media playback, PowerPoint, automatic display assignment, streaming or installer. Windows visual/hardware validation is pending.

## Build and test

Install SDK 10.0.402, pinned in [global.json](global.json). From the checkout on Windows:

```powershell
dotnet restore Kworship.sln --locked-mode
dotnet build Kworship.sln --configuration Release --no-restore
dotnet test tests/Kworship.Tests/Kworship.Tests.csproj --configuration Release --no-build --no-restore
dotnet run --project src/Kworship.Desktop/Kworship.Desktop.csproj --configuration Release --no-build
```

On the Linux cloud host:

```bash
python3 scripts/install-cloud-sdk.py
source scripts/cloud-env.sh
dotnet restore Kworship.sln --locked-mode
dotnet build Kworship.sln --configuration Release --no-restore
dotnet test tests/Kworship.Tests/Kworship.Tests.csproj --configuration Release --no-build --no-restore
```

The SDK installer verifies the official archive SHA-512. The environment helper sets writable CLI/package/IPC paths without changing HOME. Linux runs core/storage/workspace tests and cross-compiles WPF; it cannot launch the desktop UI. No servers or secrets are required.

For a self-contained Windows package (Python 3.12+ is used only for packaging):

```bash
dotnet publish src/Kworship.Desktop/Kworship.Desktop.csproj --configuration Release --runtime win-x64 --self-contained true --no-restore --output artifacts/Kworship-0.2.0-win-x64
python3 scripts/package-windows.py artifacts/Kworship-0.2.0-win-x64
```

## Validation and design

46 tests pass with zero skipped/failed, and the Release solution cross-build succeeds with zero warnings/errors. A Windows x64 ZIP was produced and checked for required runtime components and ZIP integrity. Windows execution and remote CI are unrun.

See [workspace milestone and Windows checklist](docs/14-operator-workspace.md), [Phase 2 foundation history](docs/13-phase-2-foundation.md), [reference evidence](docs/01-reference-research.md), [architecture](docs/04-architecture-decision.md), and [roadmap](docs/10-implementation-roadmap.md).
