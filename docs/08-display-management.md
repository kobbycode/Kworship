# Proposed output management

Three logical roles: operator shell, congregation composition and independent stage composition. Physical screens are assignments, not hard-coded indices. Streaming is a separate later logical output. Do not assume a machine has three monitors.

Enumerate Windows display identity, bounds, orientation, refresh rate, scale and adapter. Persist a profile with identity plus descriptive fallback; coordinates and screen number alone are insufficient. Offer identify overlays and explicit assignment confirmation. The operator window remains reachable after topology changes. Prevent accidental congregation assignment to the operator monitor unless intentionally overridden.

Each output uses a borderless window on the chosen screen, with per-monitor DPI awareness and aspect-preserving composition. Default is letterbox/pillarbox rather than cropping. Define safe areas in logical coordinates; independently measure text for stage. Mirroring reuses committed content but composes for destination metrics.

On loss: mark disconnected, stop presenting to an invalid target, keep state and operator controls responsive, and never silently relocate clean output over the operator UI. On reconnect: match identity, prepare resources and require confirmation before revealing content. Exact Windows device identity persistence needs a hardware spike. Output startup begins black.

Stage layout includes current text, next text and service clock; later timer and messages have separate controls. Stage must not reveal operator dialogs or unreleased preview edits. Define whether next text follows live item order (default) rather than current preview selection.

Validation matrix: one monitor fallback; two monitors with congregation; three with stage; mixed 100/150/200 percent DPI; 1080p and 4K; rotated stage screen; adapter change; unplug while paused/playing/transitioning; reconnect with changed enumeration; sleep/resume. These checks are unrun on the Linux cloud host.
