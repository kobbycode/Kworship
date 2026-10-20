# Desktop UI correction, version 0.2.2

## Report and diagnosis

The user's Windows screenshot showed a white main window surrounding dark panels, black preview/status text on dark surfaces, and a default white service picker. The user also reported that clicking buttons appeared to do nothing.

The main window derived from Window but did not explicitly reference the application Window style. WPF implicit styles keyed to a base control type do not automatically style arbitrary derived classes. Default window/text colors therefore survived despite dark panel colors. The initial editor fields lacked visible labels, saved songs were not automatically selected, and many controls accepted clicks with no applicable content. Those issues could make successful or rejected actions appear inert. They do not prove every reported button failure had the same cause.

## Changes

- Named AppWindow style explicitly applied to MainWindow, with a primary text resource and default readable TextBlock foreground.
- Dark ComboBox/popup/item templates replace the stock light service picker.
- Song title, lyrics and saved-service labels clarify the inline editing workflow.
- Save disabled until valid title/lyrics are present. Preview/add/edit require selection; take requires a preview; navigation respects slide boundaries; live controls require live content.
- Saving automatically selects the song and shows readable instructions for preview, take and black-screen reveal. Saved services are selected in their picker.
- A Windows-only smoke mode starts the actual WPF application with disposable data, invokes accessible button actions, checks button hit-testing and real bindings, saves/reloads SQLite content, exercises preview/live separation and both output windows, asserts foreground/background colors, opens the service popup, and captures a Windows-rendered workspace screenshot. It does not simulate physical mouse hardware or prove external-monitor behavior.
- Windows packaging is blocked if the UI smoke fails. Release promotion additionally requires a successful smoke report and screenshot, along with both platforms' test reports and the Windows ZIP checksum.

## Execution

From a Windows checkout after a Release build:

```powershell
dotnet src/Kworship.Desktop/bin/Release/net10.0-windows/Kworship.dll --smoke-test artifacts/windows-ui-smoke
```

The process exits nonzero on failure and writes result.json plus workspace.png on success or failure.png where possible. The fixture database/recovery directory is separate from normal user data. Existing user songs/services are not migrated or reset by this correction.

Local Linux cross-build and the existing 46 tests passed. Windows and Linux build run 39383467473 passed, including all 46 backend tests per platform and the new Windows UI smoke. Release promotion verifies that smoke report and its screenshot before publishing. A successful compilation alone is not considered UI validation. Physical pointer use, screen readers, mixed-DPI outputs and external display reliability still require manual testing.

[Corrected Windows build](https://github.com/kobbycode/Kworship/releases/download/v0.2.2/Kworship-0.2.2-win-x64.zip) · [captured workspace](https://github.com/kobbycode/Kworship/releases/download/v0.2.2/workspace.png).

Final 0.2.2 build run 39383916023 also passed on both platforms and passed the Windows UI smoke, including an additional assertion that the saved-service picker renders the service title. The final palette includes dark scrollbars.
