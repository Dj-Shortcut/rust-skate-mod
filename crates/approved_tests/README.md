# approved_tests

APPROVED TEST POLICY

Only explicitly owner-approved scenarios may exist in this folder.
Agents may create temporary tests and probes during development, but those
must be deleted before the final squash/push unless specifically approved. A
useful test is not automatically an approved test.

A new permanent scenario is added here only after the owner has approved that
scenario by name. The same holds for a unit test here: being
useful does not make it approved. `make publish-check` refuses `#[test]`,
`#[cfg(test)]`, `mod tests` and `tests/` directories anywhere else in the
workspace.

## Approved scenarios

| name | what it does |
| --- | --- |
| `skate_air_pullback_ollie` | owner-recorded airborne pull-back and forward flick produce one Ollie |
| `skate_ground_diagonal_kickflip` | owner-recorded grounded diagonal flick produces one Ollie with Kickflip |
| `skate_ground_roll_ollie_no_manual` | owner-recorded grounded rim roll and recoil produce one Ollie and no manual |
| `skate_late_flips_and_body_varials` | airborne late composition without a second pop, left-stick full-circle recognition, separate rider rotation and takeoff-end-aware cached landing names |
| `skate_transition_pumping` | travel-relative descent/bottom timing, bounded pump energy per traversal, reset and spam refusal |
| `skate_drop_in_and_launch` | slope gravity, reversal, full tangent drop-in/launch velocity and explicit vertical pop |
| `skate_surface_landing` | board-yaw roll retention, full surface-normal impact bail and downhill fall-to-roll conversion |
| `skate_server_movement_policy` | complete finite batch paths, horizontal cap, fresh authorization guards and independence from diagnostic airtime/support/rearm history |
| `skate_park_piece_layouts` | original eight layouts, mini/large quarterpipe profiles and cell reservations, half-wall attachment joins, and MEGA module poses and profile |
| `skate_park_anchor_yaw_and_snap` | translate and rotate each layout from an existing native snapped building anchor; reported twig bounds and true nominal-edge contact, with gapped and corner-only neighbors refused |
| `skate_park_gesture_selection` | admin-only configurable park-piece cycling, cancel, expiry and retained pending selection |
| `skate_park_build_items` | twelve exact item identities with stable original IDs/markers, single verified debit, complete paid-group recovery eligibility and half-wall metadata validation; invalid and historical free groups refuse |
| `skate_audio_surfaces_and_events` | authored wheel, truck/deck contact and event PCM: duration, peak/DC, determinism, loop seams and invalid requests |
| `skate_rider_secondary_motion` | planted ankles and toes, exact limb lengths, bounded secondary pose motion, deterministic steps and atomic invalid-input rejection |
| `skate_server_plugin_install` | actual source installation, replacement/backup, guarded refusal and cleanup after real Linux file-operation failures |
| `skate_trick_grind_names` | appended grind vocabulary, cached directional names, scoring transitions, retained stance, Darkslide half-flip catch/carry and atomic invalid-state refusal |

The owner approved `skate_transition_pumping`, `skate_drop_in_and_launch` and `skate_surface_landing` by name on 11 October 2026 for [Task 5](https://github.com/Dj-Shortcut/rust-skate-mod/issues/337#issuecomment-6106747983). They execute the actual shared C# library. Native adapter integration, contact sensing and ride feel remain separate validation.

The owner approved `skate_server_movement_policy` by name on 12 October 2026 in the continuation chat for [the owner regression report](https://github.com/Dj-Shortcut/rust-skate-mod/issues/337#issuecomment-6117276716). It executes actual compiled plugin helpers without Rust/Oxide, client or Shadow; native batch/permission flags, patch execution and movement acceptance remain separate validation.

The three recorded-gesture C# skate scenarios are specified in [issue #337](https://github.com/Dj-Shortcut/rust-skate-mod/issues/337#issuecomment-6098063930). Their separate runner and exact trace data live in [skate/](skate/README.md); they start no game.

The owner approved `skate_late_flips_and_body_varials` by name on 12 October 2026 in the continuation chat for [round 2 Task 3](https://github.com/Dj-Shortcut/rust-skate-mod/issues/337#issuecomment-6109434907). It executes the actual shared C# DLL; native input, catching, visual poses and controller feel remain separate validation.

The two park-layout scenarios were approved by name by the owner for [Task 4](https://github.com/Dj-Shortcut/rust-skate-mod/issues/337#issuecomment-6099499139). They execute the actual server plugin's engine-free layout helpers; native placement, save/load and demolition remain separate verification. The owner subsequently authorized adding the complete halfpipe to the same two scenarios.

The owner approved `skate_audio_surfaces_and_events` by name on 11 October 2026 in the handover chat for Task 8. It executes the actual shared C# DLL; waveform properties do not establish native playback or perceived sound quality.

The owner approved `skate_park_gesture_selection` by name on 11 October 2026 for [Task 6](https://github.com/Dj-Shortcut/rust-skate-mod/issues/337#issuecomment-6106613104). It executes the plugin's pure selection policy; native gesture dispatch and admin chat remain separate verification.

The owner approved `skate_park_build_items` by name on 12 October 2026 in the continuation chat. It executes the actual shared/plugin DLL's item identity, verified-debit and recovery predicates. Native item consumption/refund, save/reload, hammer removal and both building frontends remain separate acceptance. No Rust/client/server or Shadow starts.

The owner approved `skate_rider_secondary_motion` by name on 11 October 2026 in the continuation chat for [the rider-motion handover](https://github.com/Dj-Shortcut/rust-skate-mod/issues/337#issuecomment-6108821492). It executes the actual engine-free shared C# DLL. Native rig integration, hand contact, rendering and visual quality remain separate verification.

The owner approved `skate_server_plugin_install` by name on 11 October 2026 in the continuation chat for [PR #361](https://github.com/Dj-Shortcut/rust-skate-mod/pull/361). It runs the actual Bash installer and genuine pinned public source in a disposable Linux container. It never starts Rust/Oxide or accesses a live server; source installation is separate from plugin loading and gameplay.

The owner approved `skate_trick_grind_names` by name on 12 October 2026 in the continuation chat for [round 2 Task 2](https://github.com/Dj-Shortcut/rust-skate-mod/issues/337#issuecomment-6109434907). It executes the actual shared C# DLL; contact classification, deck poses and native gameplay are separate validation.

## Running

Every scenario is a C# or Python runner under [skate/](skate/README.md); that README has the build and run commands. They need the .NET 8 SDK and genuine Rust managed references, so CI runs only `skate_server_plugin_install`.
