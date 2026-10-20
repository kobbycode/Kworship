# Kworship

A Windows worship presentation application in active development. Version 0.4.1 adds an offline ASV (1901) scripture browser and authorised Zefania XML imports. Save passages alongside hymns in the existing service-planning workspace.

## Try the Windows development build

[Download Kworship 0.4.1 for Windows](https://github.com/kobbycode/Kworship/releases/download/v0.4.1/Kworship-0.4.1-win-x64.zip) from the [development release](https://github.com/kobbycode/Kworship/releases/tag/v0.4.1).

Extract the complete `Kworship-0.4.1-win-x64.zip` folder and run **Kworship.exe** on Windows 11 x64. The .NET runtime is included. See the included `QUICKSTART.txt` for a walkthrough.

Create a song, separate slides with blank lines, save, select it in the library and preview. Open congregation output, choose **Take**, then toggle **Black** off. Stage output shows current/upcoming live lyrics. Move the output windows to your extended-desktop screens and use **F11** for fullscreen; **Esc** exits fullscreen.

Open **Scripture library…** to choose an ASV passage, save it to the library, then preview or add it to your service. See [scripture instructions and source provenance](docs/17-scripture-library.md).

## Available features

- Browse offline ASV scripture, import authorised Zefania XML translations, and save referenced verse slides with attribution.
- Search titles, lyrics and authors; edit verses/choruses/bridges through **Sections & details**, with repeated custom play order and saved credits.
- Save/open/duplicate services; add repeated songs and reorder by drag/drop or buttons.
- Independent preview/live slides, explicit take, black/clear/freeze, and two text-output windows.
- **Display setup** identifies screens and saves separate congregation/stage assignments. Assigned outputs require confirmation; display changes suspend outputs until explicitly reopened.
- Resizable dark workspace, keyboard search/save, unsaved-edit protection and offered draft recovery.
- SQLite transactions, revision conflicts, checked migrations and verified database backups.

Local data is stored in `%LOCALAPPDATA%\Kworship`: `library.db` contains saved songs/services, `workspace.json` contains draft recovery/layout, and `diagnostics.jsonl` contains diagnostics. Imported Bible files are in the `Bibles` subfolder; keep these separately from database backups, which include saved passages but exclude source translations and unsaved drafts. Recovery is debounced by 700 ms and never automatically activates live output.

This development build is not validated for live services. It includes public-domain ASV text. Bulk hymn import, media playback, PowerPoint, streaming and an installer are still pending. Physical hot-plug/mixed-DPI rehearsal is pending.

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
dotnet publish src/Kworship.Desktop/Kworship.Desktop.csproj --configuration Release --runtime win-x64 --self-contained true --no-restore --output artifacts/Kworship-0.4.1-win-x64
python3 scripts/package-windows.py artifacts/Kworship-0.4.1-win-x64
```

## Validation and design

75 automated tests cover core, storage, scripture and workspace behavior. The Windows smoke workflow additionally exercises real WPF controls and output windows, including scripture import/range selection and service saving. Publication requires both platform test reports, a passing Windows UI smoke report and package integrity checks. Physical display checks remain manual.

See [workspace milestone and Windows checklist](docs/14-operator-workspace.md), [Phase 2 foundation history](docs/13-phase-2-foundation.md), [reference evidence](docs/01-reference-research.md), [architecture](docs/04-architecture-decision.md), and [roadmap](docs/10-implementation-roadmap.md).

[UI correction and regression coverage](docs/15-ui-correction.md) · [Windows-rendered workspace](https://github.com/kobbycode/Kworship/releases/download/v0.4.1/workspace.png).

See [structured songs, display setup and upgrade guidance](docs/16-songs-and-displays.md). On the first v1-to-v2 library upgrade, a verified `library.db.before-v2-<id>.db` backup is retained alongside your library. Keep it: older app versions cannot open schema v2.
