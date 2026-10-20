# Operator workflows and acceptance scenarios

These are proposed reproducible Kworship scenarios and a checklist for reference observation. They are not recordings of EasyWorship behavior. Each run records version, fixture, action, actual result and evidence ID. Unless stated otherwise, start with an offline database, two songs, a licensed scripture fixture and a running congregation output showing item A.

| ID | Action sequence | Required outcome |
|---|---|---|
| W01 | New song; enter title and two sections; save; restart | Exact text/metadata remains searchable; invalid empty title rejected |
| W02 | Edit saved lyrics; save while song is live | Library revision changes; live revision does not change until explicit take |
| W03 | Add Verse, Chorus, Bridge; order V1,C,V2,C,B,C | Preview follows ordering; repeated sections reference correct content |
| W04 | Assign image background; preview then take | Preview changes first; live changes only on take; missing image gives recoverable warning |
| W05 | Enter book chapter:verse-range | Correct verses or a precise parsing/range error; no silent substitution |
| W06 | Change translation with passage selected | Translation label and text update; missing content reported |
| W07 | Add selected passage to schedule; reopen | Range, translation, theme and ordering persist |
| W08 | Import valid video then corrupt video | Valid asset indexed; invalid asset rejected without partial schedule state |
| W09 | Import PPTX with unsupported transitions | Supported path produces slide order; lost features reported before take |
| W10 | Assemble ten-item Sunday service; drag reorder; save/reopen | Stable order and IDs survive; missing dependencies listed |
| W11 | Select B and navigate preview while A is live | Congregation and stage current content remain A |
| W12 | Explicitly take B | Live commits a complete B snapshot without mixed text/background |
| W13 | Next/previous at first/last slides | Defined boundary behavior: clamp within item; never implicitly take another item |
| W14 | Take song, scripture, video in sequence | Correct content, audio ownership and resource cleanup; errors keep prior live output |
| W15 | Toggle black; navigate; unblack | Black output suppresses text/background; latest committed state appears on release |
| W16 | Toggle clear text | Background remains; text hidden; restore does not mutate content |
| W17 | Assign outputs across unequal-resolution monitors | No operator chrome on congregation output; aspect-safe independent layouts |
| W18 | Enable stage; advance live | Stage current and next follow committed item, independent stage formatting |
| W19 | Force termination after save; restart | Last committed schedule and draft recovery offered; outputs start black until operator confirms |
| W20 | Freeze; take next slide; release freeze | Frozen composition stays; release shows latest committed snapshot; audio policy explicitly tested |
| W21 | Disconnect congregation screen during playback; reconnect | Operator responsive; safe disconnected status; explicit output reassignment confirmation |
| W22 | Backup; alter data; restore backup | Verified original content recovered atomically; media omissions reported |

Reference observation must record whether EasyWorship differs from these intended policies. Shortcut keys, modal layouts and drag targets must be learned from evidence rather than mapped from this proposal.

## Source-backed reference workflow map
S01: New > New Song, or right-click song > Edit Song; Words/Slides editing; Ctrl+Enter creates/splits a slide. Theme button selects a theme; dragging a theme onto a slide applies per-slide styling, while master edits apply broadly. Inspector provides formatting and media background choices.

S01: drag a presentation file from Explorer into the schedule, use schedule gear > Add Item > Browse for Presentation/PowerPoint, or import to Presentations resources first. Resource criteria collections and search locate content. Select an item to preview; preview forward/back is isolated from live; double-click preview to take. Live arrows advance output. Exact next/previous hotkeys are not established.

S05: enable Windows extended displays, then Edit > Options > Output Monitor; choose Foldback separately. S07: set Main/Alternate/Foldback Output Monitor to NDI Stream and select format. S09: close the application, export profiles with the transfer tool, then import .ewprofile for the target version. These are reproducible documented steps, not executed results. W05–W08 and W19–W21 still need more detailed reference observation where not covered.
