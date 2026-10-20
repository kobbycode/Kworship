# Feature inventory

Every row below is a Kworship requirement, not a verified EasyWorship finding. Reference support for a subset is documented in sources S01–S09 in document 01; all execution behavior remains untested. P0 = first usable vertical slice; P1 = service readiness; P2 = later scope. Priorities sequence work, not removal of requested scope.

| Area | Required behavior | Priority | Principal dependency / acceptance |
|---|---|---|---|
| Shell | Dark workspace, search, resize, saved layout, keyboard focus | P0 | UI spec; restart preserves usable layout |
| Songs | CRUD, sections, custom order, titles, author, rights, tags, categories, language, favorite, history, formatting/background | P0/P1 | SQLite; exact round-trip, offline lyric search |
| Scripture | Installed licensed translations, passage parser, ranges, book/chapter/verse and search | P1 | content rights; explicit translation and missing verse errors |
| Sermon | Text and slide-based teaching items | P1 | renderer; preview and persist |
| Media | Images, video, audio, thumbnails, validation, loops, volume, scaling, missing assets | P1 | codec matrix and playback tests |
| PowerPoint | Presentation import or controlled integration | P1 | Windows/Office/license spike; fallback rendered slides with explicit loss warnings |
| Schedule | Add, reorder, duplicate, reopen, edit during live | P0/P1 | stable IDs; live snapshot remains unchanged |
| Presentation | Preview/live, navigation, transitions, transparent composition, clear, black, freeze | P0/P1 | deterministic engine; state tests |
| Displays | Operator, congregation, independent stage, assignment, resolution, aspect, reconnect, mirror | P1 | Windows hardware matrix |
| Stage | Current/upcoming text, timer, service data | P1/P2 | separate projection of committed state |
| Themes | Templates, fonts, safe area, background, scripture formatting | P1 | bounded layout, missing-font fallback |
| Controls | Search, hotkeys, contextual menus, drag/drop, accessible focus | P0/P1 | scoped shortcuts; edit safety |
| Storage | Migrations, integrity, backup, restore, crash recovery | P0/P1 | fault-injection and restore verification |
| Exchange | Versioned import/export, validation, deduplication | P1 | atomic import; no silent content loss |
| Streaming | Independent overlay output and documented transport | P2 | NDI/other transport evaluation; licenses |
| Operations | Offline runtime, diagnostics, packaging, signed upgrade/rollback | P1/P2 | network-disabled service rehearsal |

## Observable technical questions
S08 documents OS/hardware requirements, S05–S07 describe output assignment and NDI, and S09 describes profile transfer. Actual format/codec coverage, output limits, GPU behavior, PowerPoint version compatibility, storage layout, backup completeness and startup/shutdown performance remain unverified. Establish an explicit supported matrix for Kworship rather than inheriting assumptions.

## Inventory evidence coverage
S01 documents shell views, song editing, libraries, themes, preview/live, search, schedule and presentation import. S05 documents display/foldback assignment, S06 alternate output, S07 NDI, and S09 profile transfer. Song favorites/history/custom order, exact scripture parser behavior, freeze, automatic recovery and format compatibility are not established by those sources. Keep these as Kworship requirements rather than advertised reference parity.
