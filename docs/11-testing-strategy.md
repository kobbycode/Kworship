# Testing and performance strategy

All tests in this document are planned, not executed. Phase 1 validation covers document consistency and evidence honesty only.

## Automated layers
- Domain: preview selection/edit never changes live; all control flag combinations; slide boundaries; late callback versions; take failure rollback; stage derives from committed version.
- Storage: real temporary SQLite databases; foreign-key enforcement; arrangement ordering; migration from each supported version; rollback under injected failures; content round-trip; integrity and restore checks.
- Search/scripture: Unicode, ambiguous book names, invalid ranges, missing verses, translation mismatch and rights metadata preservation.
- Imports: corrupt media, unsupported codecs, traversal archives, oversized payloads, duplicate assets and cancellation without partial commit.
- UI: keyboard focus scopes, editor shortcuts, save failures and accessibility names on Windows.
- Integration: actual video/audio playback and independent outputs, not merely open ports, window existence or mocked decoders.

Use original lyrics, generated color bars, known audio signals and explicitly licensed verse fixtures. Playback checks should observe frame movement, loop boundaries, audio continuity, stop/release and missing-file recovery. PPT compatibility fixtures include fonts, animations, embedded media and aspect ratios; document unsupported behavior.

## Proposed measurable targets (not achieved results)
Baseline proposal: Windows 11 x64, 16 GB RAM, SSD, supported GPU, operator plus 1080p60 congregation and stage outputs. Also test 4K60 separately and record exact hardware/driver/build.
- Cached song search p95 under 100 ms at 50,000 songs, excluding UI typing debounce.
- Warm text take visible acknowledgement p95 under 100 ms over 1,000 takes, measured from command to captured output frame.
- Shell input p95 under 50 ms during video and thumbnail work.
- 60 Hz frame budget 16.7 ms; report missed frames and pacing over a 30-minute playback, with a proposed under 0.1 percent missed-frame goal.
- Eight-hour mixed-content soak: no crash, no unintended output and no unbounded growth; post-warm-up memory growth under 10 percent with identical repeated workload.
- Startup to usable local library under 5 s on baseline SSD; crash recovery under 10 s with output safely black.

Thresholds require review and measurements; no generic promise of 4K capability. Use ETW/native profiling, output capture with timestamp correlation, memory/GPU counters and structured event logs. Report median/p95/worst, sample count and test conditions.

## Manual and failure gates
Execute document 03 plus mixed-DPI/hot-plug matrix, sleep/resume, audio-device loss, GPU device loss where safely reproducible, disk full, denied permissions, corrupted backup, missing font and power-loss simulation on disposable data. Network-disabled rehearsal must cover startup and every local service item. Keep passed, failed, skipped and unrun separate. A Windows build is not proof of correct live behavior.
