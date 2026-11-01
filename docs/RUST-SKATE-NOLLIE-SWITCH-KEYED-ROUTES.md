# Own-source Nollie and Switch Kickflip routes

The normal captured-trick selector can opt into two independent Rodriguez
sources. The source authoring and original rig comparison images are delivered
in #679 (Nollie) and #680 (Switch). This change supplies their exact gameplay
recipes; it does not approve a pack or alter input, flight physics or scoring.

| Clip ID | Movement | Pop | Switch / tail leads | Flip / shove / body spin |
| --- | --- | --- | --- | --- |
| `nollie-kickflip-source-keyed-v1` | `nollie-kickflip` | Nollie | false / false | +360 / 0 / 0 |
| `switch-kickflip-source-keyed-v1` | `switch-kickflip` | Ollie | true / false | +360 / 0 / 0 |

Both entries are appended after the existing 21 registry entries, preserving
their indices. There are now 23 exact signatures. Fakie, heel flips, held extra
turns, shoves and body spins do not alias either new source. Existing arrays use
the registry count; no separate fixed-size client table is introduced.

The approved clip ID, movement kind and entire signature must match. The
existing loader still requires a matching pack digest, reviewed clip and owner
approval. No menu, registry entry or source baker approves the generated pack.
The original/generated source packs remain `reviewed=false`.

## Entry and contact ownership

Nollie uses its canonical Left/nose foot for the pressure-release window. Its
own source has Pop at frame5702, nose strike5708, nose release5709 and rear-foot
release5710. That last contact fact is `Pop/None` while the pressure/flick stage
finishes; the first distinct `Air/None` at5712 is the entry endpoint. This avoids
silently targeting an earlier pose when the nose foot releases first.

Switch keeps its own canonical Regular body and exact switch signature; it is
not the normal Kickflip body relabelled. Its rear/tail release5487.8 is followed
by a distinct free-Air endpoint5489. The front sole flag clears before the
side-instep bank; that geometric upper contact persists through5486.2.

For each source the existing parser obtains the pressure endpoint from the
actual events. That endpoint also sets the root-entry join, rather than reusing
another trick's fraction. The observed physical carrier derivative is removed
once for the entry; the authored source carrier is replaced once during the
pressure window. Body and complete board rotation keep one source clock.

Both exact clips require an observed flat takeoff plane at entry and sampling.
The original detailed 13-part board hull supplies their floor envelope with
the existing bounded correction. Reach/contact checks, correction limits,
approval checks and existing-family entry choices are unchanged. Neither new
clip gets the normal Kickflip's fallback root fit.

## Verification and limits

All 81 configured client sources compile as C#7.3 against 356 verified cached
diagnostic references. This checks source compatibility, not current callable
Rust/Unity bindings.

The private actual Pad/full32Hz Step/Land/scorer replays cover normal/low charge
and both requested stances for each source. Nollie is actually composed as a
nose pop with one positive flip. Switch starts from an explicitly recorded
established-switch riding state and actually scores `Switch Kickflip`, with the
nose leading rather than a fakie label applied to a normal trace. Both sources
remain active for every Air frame, land without a bail/extra held turn and
return to ordinary Ground with capture off by4s. Bindings and geometry are
controlled; these are managed-source replays, not a running Rust client.

Four actual rigged GLBs per source retain the weighted rider and original
articulated skateboard. Their own phase images and source-frame references are
on the focused source PRs. Viewer selectors use audited physical lead feet,
because the frozen proxy's request/bone-label map is reversed. This does not
establish the real Rust-avatar mapping.

The library record and offline quality decisions remain Claude's. Native
boarding, the current compass/view binding, real-avatar foot mapping and a
validated combined Shadow package are separate open gates. No footage, raw
detector output, game references or test-only approvals are published here.
