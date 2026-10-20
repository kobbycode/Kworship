# Phased roadmap and proposed Phase 2

Historical Phase 1 plan; user subsequently approved proceeding. Phase 2 foundation implementation and results are recorded in document 13. The Phase 1 desk study is available for review; outstanding hands-on gaps are explicit and must be accepted or scheduled before implementation.

| Phase | Deliverable | Exit gate |
|---|---|---|
| 1 Research | evidence, inventory, scenarios, architecture drafts | official version baseline and sources reviewed; unknowns accepted or assigned; user approval |
| 2 Foundation | solution, domain core, persistence contracts, CI | Windows build + meaningful core/storage tests; SDK/dependencies pinned; ADRs accepted |
| 3 Shell | resizable dark workspace and navigation | keyboard/focus and layout persistence verified; high-fidelity design approved |
| 4 Engine | isolated preview/live, text output, controls | W11–W16/W20 pass; atomic resource failure behavior tested |
| 5 Displays | congregation/stage windows and configuration | hardware matrix and W17/W18/W21 pass |
| 6 Content | song editor and licensed scripture packages | W01–W07 pass offline; rights reviewed |
| 7 Media/schedule | playable media, import and saved service | W08–W10/W14 pass; PPT path evidence and codec matrix |
| 8 Advanced | themes, transitions, stage timers, stream adapter | independent outputs, performance and licensing gates |
| 9 Reliability | profiling, recovery, soak, accessibility | W19/W22 and documented eight-hour rehearsal pass |
| 10 Distribution | signed installer and safe upgrades | clean Windows install, upgrade, rollback and restore validated |

## Phase 2 proposal for approval
1. Confirm Windows baseline and representative operator hardware; verify current LTS .NET/WPF support. Choose and pin SDK, packages and CI runner.
2. Create Kworship.Domain, Application, Infrastructure.Sqlite, Platform.Windows, Desktop and Tests projects. Keep domain target platform-neutral and Windows rendering in adapters.
3. Define immutable item/slide/snapshot IDs, typed commands and interfaces for storage, clock, output and assets. Implement reducer first with preview/live isolation tests and no multimedia claims.
4. Add initial SQLite migrations, transaction/revision handling and fixture-only song/schedule storage; exercise rollback and backup/restore.
5. Add structured diagnostics and explicit application-data paths. No real song or Bible corpus without distribution permission.
6. Run disposable rendering/media/display spikes on Windows before committing the native pipeline. Record measurements and rejected options.
7. CI builds core/tests on Linux where feasible and desktop/tests on Windows. Preserve a reproducible Windows build; cloud Linux alone is insufficient.

Phase 2 excludes full media, PowerPoint automation, streaming, production installer and claims of service readiness. Deliver one testable foundation milestone before expanding. See the README for the installed SDK and build commands.

## Workspace milestone
Version 0.2.0 implements the operator workspace, song/service workflows, text-only congregation/stage windows and draft recovery. See document 14 for current validation. Full media, physical display management, licensed scripture and reliability/distribution gates remain open.
