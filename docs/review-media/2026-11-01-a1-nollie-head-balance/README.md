# A1 Nollie Kickflip: head and balance-arm repair

Addresses the visible Nollie gate in #682 / batch #684. Source authoring is
`96f64c9f978a7dcc453174aa15d99873d1a66c3f`; managed route is product main
`8354c7f6dd82ef2453d4ac836649d474e0626ce0`; raw pack SHA256 is
`59ea4fe81b52ae6ede06d74346a0a6656b1fe7cdbee8f5b34745ab918beafd95`. Raw/generated source remains `reviewed=false`.

The private original Rodriguez frames5720–5742 were checked before editing.
He really holds a high diagonal arm around Catch/Land. The fixed-rig source
head axis, however, points downward through Pop/Land and puts the visible head
inside the chest. Its reconstructed lead arm also reads as a straight overhead
pole. Under the owner's visible-quality rule, this repair **authors** a proper
head-up axis, board/landing gaze and a bent settling balance arm. It does not
claim measured head/arm depth or literal reproduction of the high source arm.
The anatomical reflection carries the correction into both physical stances.

Only Head/HeadForward and canonical LeftElbow/LeftHand change in the source
bake. Pelvis, torso, shoulders, opposite arm, all legs/feet, both sole frames,
board transforms and every phase/contact event match the pre-A1 source bake.
All limb lengths are retained. There is no shared gameplay, scoring, camera,
placement, F5 or F9 change.

| Height | Physical stance | Load / Pop / Air / Catch / Land / settled End | Pop / Air / Catch / Land before-and-after |
| --- | --- | --- | --- |
| Normal | Regular | [Phases](normal-regular-phases.jpg) | [Upper-body comparison](normal-regular-upper-before-after.jpg) |
| Normal | Goofy | [Phases](normal-goofy-phases.jpg) | [Upper-body comparison](normal-goofy-upper-before-after.jpg) |
| Low | Regular | [Phases](low-regular-phases.jpg) | [Upper-body comparison](low-regular-upper-before-after.jpg) |
| Low | Goofy | [Phases](low-goofy-phases.jpg) | [Upper-body comparison](low-goofy-upper-before-after.jpg) |

Before is the rejected head/arm from PR #679's frozen rig output; after is the
new raw source through the current main route. Both use the same original
frozen skin and13-part skateboard. The proxy's anatomical labels reverse the
requested stance: physical names above come from the actual animated GLB
lead-foot positions, not the fixture argument. They make no global Rust-avatar
handedness claim. Source footage and detector arrays stay private; all committed
images are original rig-only renders or crops of those renders.

The current Pad/full32Hz Step/Land source replay builds and passes all four
cases, with the intended Nollie capture active throughout Air and ordinary
Ground recovery by4s. Private test-only approval never changes the raw pack.
The final exported GLBs were re-read and all phase sheets inspected. Actual Metal-browser
1×/¼× playback reaches4s in all four cases with no runtime errors and moving
reference frames; the compact replay record is in `review-manifest.json`.
This is managed/offline evidence with controlled ground, not Rust/Shadow
acceptance. Claude retains the offline-review decision and library table.

Local comparison: [Nollie A1 viewer](http://127.0.0.1:63761/a1-nollie-head-balance/).
