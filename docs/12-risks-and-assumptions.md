# Risks, assumptions and open decisions

| Risk / unknown | Impact | Mitigation / required evidence |
|---|---|---|
| Future-task network policy needs review | Research could lose access after restoration | domain draft saved; current instance GET requests succeed |
| No authorized installation/material | Cannot observe exact UI, hotkeys or recovery | About/workspace/output screenshots and scenario recordings |
| Linux-only current host | Cannot validate Windows shell/media/displays | Windows CI and representative physical hardware |
| Preview/live shared mutation | Accidental congregation changes | immutable snapshots and adversarial state tests |
| GPU/codec/device loss | Black frames, stalls or crash | renderer spike, bounded resources, recovery tests |
| PowerPoint dependency and fidelity | Unsupported transitions/fonts/media | Office/version/license decision; explicit supported fallback |
| Bible/song/media rights | Distribution and attribution obligations | provenance and rights review before bundling |
| Codec/Qt/stream transport licensing | Packaging constraints | legal/dependency audit before adoption |
| Database/asset inconsistency | Lost service content | transactional references, staged imports, verified backup |
| Multi-monitor identity instability | Content on wrong display | confirmation and disconnected-safe policies |
| Third-party format uncertainty | Compatibility claims unsupported | documented formats or authorized samples; no protection bypass |
| Signing and update trust | Unsafe releases and rollback failure | managed signing, verified artifacts, upgrade rehearsals |
| Accessibility and low-light operation | Operator errors | keyboard/screen-reader/contrast and rehearsal reviews |

Assumptions: Windows-first delivery; local single-operator data; optional future macOS; no cloud account required for a service; no copyrighted corpus bundled; source repository is intended as Kworship's foundation. Multi-user synchronization and remote control require separate architecture if requested.

## Decisions requiring review
Confirm target reference build, Windows minimum, operator hardware, PowerPoint fidelity needs, stream transport, distribution model and whether macOS is near-term. These do not prevent drafting architecture, but they gate implementation scope. Proposed freeze/black audio behavior and output reconnect confirmation require operator review.

## Phase 1 status
Phase 1 desk research, source-backed interface map, preliminary architecture and original static workspace mockup are prepared for review. Authorized behavior observations, precise v8 build, full format/shortcut matrix and Windows measurements remain unverified. Do not describe the research as exhaustive or begin Phase 2 until the user reviews the known gaps and approves the architecture and roadmap. This pause follows the user's supplied master prompt, section 16: “STOP after completing Phase 1” and “Do not begin implementing the application until ... reviewed and approved.”

## Subsequent approval
The user approved proceeding after review of the Phase 1 package. Phase 2 foundation was implemented; see document 13 for actual validation and remaining Windows/hardware gates. The earlier stop instruction was satisfied before implementation began.
