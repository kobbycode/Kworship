# Preliminary architecture decision ADR-001

Status: preliminary recommendation accepted for the Phase 2 foundation following user approval. See document 13 for the pinned toolchain, implementation and remaining validation gates.

## Decision criteria and qualitative comparison
Ratings are engineering expectations, not benchmark results. All candidates require native playback/display integration and representative Windows trials.

| Criterion | C# / WPF | C# / WinUI 3 | C++ / Qt | C# / Avalonia |
|---|---|---|---|---|
| Windows shell maturity | Strong desktop ecosystem | Modern Windows shell, deployment complexity to test | Mature desktop model | Cross-platform desktop model |
| GPU / high-resolution output | Separate renderer advisable | Composition integration attractive | Native scene graph attractive | Renderer and native interop spike needed |
| Low-latency media | Media Foundation/native bridge required | Native bridge required | Qt Multimedia backend validation required | Native bridge required |
| Multi-monitor / DPI | Win32 interop and DPI tests | Win32 interop and DPI tests | Cross-platform screen APIs; hot-plug tests | Platform-specific behavior tests |
| Memory / responsiveness | Managed shell; isolate decoding | Managed/native boundary | Fine control; higher ownership burden | Managed shell; platform backend costs |
| Windows compatibility | Candidate broad desktop baseline | Confirm Windows App SDK minimums | Confirm compiler/Qt release matrix | Confirm platform support matrix |
| Persistence / tests | .NET SQLite and headless core | Same core opportunity | SQLite C API; C++ tooling | Same .NET core opportunity |
| Maintenance / packaging | Familiar Windows deployment options | App SDK lifecycle adds dependency | C++ build and licensing burden | Native packages per OS |
| Future macOS | Replace shell/output adapters | Replace shell/output adapters | Strongest shared native UI option | Shared .NET UI candidate |

## Preliminary recommendation
For a Windows-first product, propose C# with a supported LTS .NET release and WPF operator shell, a UI-independent presentation core, SQLite local storage and a separately testable Windows rendering/playback adapter. Pin exact SDK and packages only in Phase 2 after confirming their current support lifecycle. WPF is an operator-shell decision, not evidence that its default media control satisfies reliable 4K presentation.

Use a single dedicated render thread per graphics device ownership strategy, Media Foundation playback initially evaluated behind an interface, and borderless output windows. Choose Direct3D/DirectWrite versus a higher-level compositor only after a spike measures text quality, frame pacing, overlays and device loss. Do not add a codec bundle without distribution/license review.

Why this tradeoff: Windows is primary; a managed core supports deterministic state and persistence testing, while native adapters contain hardware complexity. Qt remains a serious alternative if near-term macOS delivery becomes mandatory or native rendering measurements favor it. WinUI 3 offers modern composition but does not remove device-loss, codec or presentation-state work. Avalonia merits a prototype if shared UI becomes a business requirement.

## Review gates
Measure 1080p/4K rendering, two outputs, mixed DPI, H.264 playback, hot-plug and shell input latency on target hardware. Reject the recommendation if the adapter cannot meet document 11 targets without unstable interop. Record ADRs for renderer, codec packaging, PowerPoint path, streaming transport, backup format and installer/signing before those implementations.
