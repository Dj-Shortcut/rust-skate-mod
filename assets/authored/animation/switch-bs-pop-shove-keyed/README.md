# Own-source Switch BS Pop Shove-it

This is the white-shirt actor's landed Switch Pop Shuvit, exact attempt-table
row 1090 in [the official BATB13 reference](https://www.youtube.com/watch?v=tL8Ey9GS0xQ&t=202).
The cached sequence shows a grip-up board-only half-turn, no longitudinal flip
or body half-turn, a bent landing and upright rollaway. It supplies its own body
capture; no Cole, Fakie Kickflip, Halfcab or other trick body is reused.

The source is a previously cached 38-second excerpt requested at 181–219 s.
Its actual local clock is 30000/1001 fps, PTS `1001 * frame`, time base 1/30000.
The chosen actor occupies local frames 649–736; the next actor cut is 737.
The published retargeted body controls use 657–736. Public video seconds are
181 plus bounded PTS, an estimate rather than verified original global frames.

## Anatomy and source limits

The actor's inferred base stance is Regular; this opposite-stance take is
physically Goofy: anatomical Right leads, Left pops the tail. That is a manual
reading of the actual feet/pop and same-actor Nollie/Switch witnesses. The
bounded movie lacks a complete plain-normal stance witness, so this is not
claimed as calibrated or independently measured stance truth.

All 88 own full-frame foreground VIDEO selections are retained privately.
`captured-body.json` contains original retargeted controls and provenance,
not video pixels or raw detector arrays. One X reflection plus anatomical
Left/Right swap maps filmed Goofy into canonical Regular: Left front, Right tail,
initial nose +Z, toe side +X, Y up. Sparse authored camera-pan yaw keys remove
camera/heading ambiguity. The body is never de-yawed by the spinning board or
rotated into a velocity frame. The actual native carrier supplies travel/jump
once; the body has no authored half-turn.

Monocular depth and contacts are reconstructed. Low-visibility raw landmark
indices remain listed. Weak crouch knee depth uses a bounded toe-side pole;
the hidden leading elbow/hand suffix after Land receives a labelled relaxed
balance prior. Head anatomical up stays separate from its board-facing gaze.
No model confidence, dense interpolation or successful export grants approval.

## Pressure, scoop and catch

| Event | Bounded frame | Authored contact candidate |
| --- | ---: | --- |
| Initial riding / Load | 657 / 678 | Both soles |
| Pop pressure begins | 683 | Both soles |
| Front sole lifts | 684.5 | Rear Right foot |
| Tail strike | 685.5 | Rear toe-ball pressure |
| Rear sole releases | 687 | Neither |
| Distinct free Air | 689 | Neither |
| Front catch / both catch | 697 / 699 | Left, then both |
| Stable landing / recovery | 701 / 711 | Both |

The front foot lifts without a heel or instep flick. The rear ball presses the
toe-side tail corner beyond the bolts, raises the heel and scoops behind the
rider. The board follows original shaped-deck/whole-board geometry with yaw
+180°, zero roll and positive Up throughout. Native intent remains Ollie,
switch=true, tailLeads=false, Flip 0, Shove −180°, Spin/Body 0. The actual input
and scorer remain authoritative.

A half-turn reverses deck-local ends. Final catch points **and toe headings**
are transformed through the final board frame; anatomical feet keep their
world-side positions and the body does not turn with the deck. Existing
shaped-shoe clearance and fixed-chain reach remain strict. Root height, foot
depth, yaw/pitch, sole timing and partial occlusions are authored estimates.

## Rebuild

With NumPy installed, from the repository root:

```sh
python scripts/author_skate_switch_bs_pop_shove.py \
  --output context/artifacts/switch-bs-pop-shove/output
```

The executable binds the own body and original geometry checksums, validates
strict binary encoding, and writes the raw JSON/pack last through temporary
files. Body, geometry and source inputs remain intact. Generated packs, source
footage, detector estimates, private test approvals and diagnostics stay in
ignored `context/`. Raw `reviewed=false` is retained. Source/mesh comparison,
actual four-case gameplay route, CI and native Shadow are separate gates.

## Offline review candidate

[Original rig media and gate limits](../../../../docs/review-media/a4-switch-bs-pop-shove/README.md)
and [local viewer](http://127.0.0.1:63761/a4-switch-bs-pop-shove/) now include
both physical stances and normal/low flights through actual landing and
ordinary 4-second recovery. Actual Pad/full Step/Land/scorer cases select
Switch Pop Shove-it, retain zero flip/body spin and complete the backside
180-degree board scoop; the support, sole/floor and fixed-reach guards pass.
The original 13-part board and frozen skin were exported and read back.

The author inspected source/rig Load, Pop, compact Air, catch, bent landing and
upright recovery, including side Pop and feet through scoop/catch/rollaway.
Four 1×/¼× viewer runs, 28 actual decoded-source phase seeks and 20 focused
contact/side captures finished without browser/runtime errors. These are
controlled offline checks with private test approval, not native Rust/Shadow
acceptance or a library verdict. Raw source packs remain `reviewed=false`.
