# Reference research and evidence register

Status: Phase 1 desk study complete; hands-on validation remains pending. Date: 2026-10-20.

## Scope and method
Study EasyWorship 7 as the documented legacy workflow baseline and EasyWorship 8 as the current download family. Official All Downloads lists v8 for Windows and Mac and v7 for Windows only. The v7 release notes include 7.4.1.9 (2023-03-03); this is a documented build, not a verified latest installed build. The v8 download article does not expose a precise build in extracted text. Exact installed build and license remain unknown. No installation, screenshots, recordings, manual PDF or executable was inspected. No reverse engineering of binaries was performed.

Use official versioned manuals and release notes first, then authorized application observations, then dated public tutorials as secondary evidence. Record URL, title, version applicability, retrieval date, quoted claim or screenshot timestamp and repeatable observation. A product feature claim establishes advertised support, not reliability or measured performance. Conflicting findings must retain both sources until reproduced.

## Evidence table
| ID | Statement | Classification | Evidence / limit |
|---|---|---|---|
| E01 | Checkout has no tracked files, commits or remote refs | Verified environment fact | git status, git ls-files, git ls-remote --symref origin; rechecked 2026-10-20 |
| E02 | Cloud host is Linux x86_64; Node 24.19.0 and Python 3.12.14 available; dotnet not found on PATH | Verified environment fact | uname and tool probes; not a Windows execution environment |
| E03 | Initial official-site requests failed; subsequent GET requests succeeded | Verified access result | initial proxy CONNECT 403; later support article GET HTTP 200 after draft save |
| E04 | WPF overview and SQLite transactional documentation retrieved | Verified document access | HTTP 200 on subsequent GET; reviewed capability summaries below |
| E05 | Twenty core capabilities are requested for Kworship | Verified user requirement | supplied master prompt; does not establish EasyWorship behavior |
| E06 | EasyWorship supports every inventory item exactly as specified | Unknown | documented support below covers a subset; no hands-on validation |
| E07 | Independent preview/live snapshots are suitable for Kworship | Original design proposal | engineering decision, requires tests |

Documented support is distinct from observed execution. Do not cite user requirements as reference observations. Sources below were retrieved and read on 2026-10-20; article modified dates are source metadata, not test dates.

## Initial source queue (retrieval results below)
- https://support.easyworship.com/support/home — locate versioned manual, release notes, system requirements, keyboard reference, backups and display setup.
- https://www.easyworship.com/software/features — advertised capabilities; confirm canonical URL after access is enabled.
- https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview/ — WPF platform documentation.
- https://doc.qt.io/ — Qt rendering, multimedia, Windows deployment and licensing.
- https://docs.avaloniaui.net/ — alternative cross-platform UI evaluation.
- https://www.sqlite.org/transactional.html — transaction and durability constraints.

## Observation protocol
On a licensed Windows machine, record build, OS, CPU/GPU, drivers, display topology and media codecs. Use original test lyrics and licensed media. Capture operator and output surfaces together. Run each scenario in document 03, record before/action/after, latency, errors and recovery. Test documented hotkeys rather than guessing. Do not inspect private formats through protection bypass or redistribute proprietary assets.

## Material needed
Official support access now works in this instance. Review the saved network configuration for future tasks. Provide an authorized About screenshot, main workspace screenshot, display settings screenshot, and recordings of preview-to-live, clear/black/freeze and display reconnection when available. Documentation-only findings can proceed without installation; actual latency, media reliability, window behavior and recovery require Windows observation. Do not provide credentials in chat.

## Retrieved official sources
| ID | Title and source | Source modified date | Findings and limits |
|---|---|---|---|
| S01 | [Quick Start Guide](https://support.easyworship.com/support/solutions/articles/24000020385) | 2023-01-05 | v7 explicitly named in view descriptions; song editor, preview/live, schedule, themes, search and presentations documented |
| S02 | [All Downloads](https://support.easyworship.com/support/solutions/articles/24000041409) | 2026-04-27 | v8 Windows/Mac and v7 Windows-only listed; not proof of precise current builds |
| S03 | [EasyWorship 8 Software Download](https://support.easyworship.com/support/solutions/articles/24000104343) | 2026-06-08 | confirms v8 download page; no build in extracted body |
| S04 | [EasyWorship 7 Software Download / release notes](https://support.easyworship.com/support/solutions/articles/24000018839) | 2026-06-08 | includes 7.4.1.9 and historical fixes; not a benchmark or proprietary internals inspection |
| S05 | [Screen Setup](https://support.easyworship.com/support/solutions/articles/24000020382) | 2026-02-12 | extended desktop, output monitor selection and separate Foldback; now includes Windows and Mac instructions |
| S06 | [Alternate Output](https://support.easyworship.com/support/solutions/articles/24000071694) | 2024-11-15 | explicitly subscription-only, third output or livestream; does not imply all editions have it |
| S07 | [NDI Setup](https://support.easyworship.com/support/solutions/articles/24000020413) | 2024-02-14 | Main/Alternate/Foldback NDI output and NDI feed input documented |
| S08 | [System Requirements](https://support.easyworship.com/support/solutions/articles/6000047711) | 2026-04-28 | current page combines Windows/Mac; Windows recommended Win11 x64, 8 GB, SSD, listed RTX cards; minimum Win10/4 GB/GTX1050–1650 for basic use; not a v7 historical compatibility guarantee |
| S09 | [Transfer EasyWorship Profile](https://support.easyworship.com/support/solutions/articles/24000043114) | 2026-05-13 | closed-app export/import, .ewprofile, target version selection and v8 profile selection; does not prove crash consistency |
| S10 | [EasyWorship 7 Manual](https://support.easyworship.com/support/solutions/articles/24000020510) | 2023-12-06 | landing page retrieved; linked easyworship.com/help/EasyWorship7Manual.pdf returned HTTP 404 |

The downloads page labels a link as keyboard shortcuts but it resolves to the EasyWorship Logo article (6000107708). That target was inspected and contains wallpaper, not a shortcut guide. Do not infer application shortcuts from Freshdesk portal JavaScript. Find a correct versioned guide or use authorized observation.

## Documented feature evidence
| Finding | Source | Classification / caveat |
|---|---|---|
| Three views: Preview and Live; Preview and Live Combined; Live with Resource Preview | S01 | documented, v7 context |
| Resource libraries: Songs, Scriptures, Media, Presentations, Themes; collapsible area and thumbnail/list choices | S01 | documented UI, images not visually inspected |
| Preview next/back does not advance live; preview double-click sends immediately live | S01 | documented critical state separation |
| Live panel supports navigation but no editing; double-click exits Logo/Black/Clear | S01 | documented; audio/freeze semantics unknown |
| New/Edit Song, Words/Slides view, Ctrl+Enter creates/splits slides, labels, theme/master/Inspector | S01 | documented; no persistence or concurrency test |
| Drag themes/media to song or slide; theme editor provides font/outline/shadow/transparency/background | S01 | documented |
| Schedules contain songs, scriptures, PowerPoint, camera feeds and other resources; file sharing/reorder described | S01 | documented; exact export schema unknown |
| Resource search supports title, scripture reference and keywords; criteria-based collections | S01 | documented; speed/ranking unknown |
| Screen assignment and separate foldback; NDI input/output; subscription alternate output | S05–S07 | documented; hot-plug behavior unverified |
| Profile backup/import uses .ewprofile and closed application | S09 | documented; backup completeness and corruption recovery unverified |
| Media-related release fixes include album-art audio freezes, looping, sync, mixed-DPI and PowerPoint issues | S04 | historical release claims; motivates regression tests, not claims of current bugs |

## Architecture source notes
[WPF overview](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview/) and [SQLite transactional](https://www.sqlite.org/transactional.html) were successfully retrieved. WPF documentation describes Windows desktop presentation and hardware-accelerated rendering; it does not establish our media/multi-output performance. SQLite documents transactional atomicity/durability; our asset store, backup and crash integration still require tests. Qt, WinUI and Avalonia comparison remains a preliminary engineering assessment pending equally detailed current documentation and spikes.

## Remaining evidence gaps
Exact v8 build and UI differences, full media/codec support matrix, scripture selection gestures, offline licensing behavior, freeze semantics, layout persistence, hot-plug recovery, exact file formats, startup/shutdown timing and real performance remain unknown. These do not prevent a Phase 1 recommendation but must remain explicit follow-up gates. Authorized reference recordings would strengthen the next revision; they are not prerequisites for writing the proposed architecture.
