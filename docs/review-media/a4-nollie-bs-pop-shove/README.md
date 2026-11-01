# Nollie BS Pop Shove-it: offline review evidence

[Viewer](http://127.0.0.1:63761/a4-nollie-bs-pop-shove/) ·
[Own source and authoring contract](../../../assets/authored/animation/nollie-bs-pop-shove-keyed/README.md) ·
[Batch A4](https://github.com/Dj-Shortcut/rust-skate-mod/issues/706)

These are original rig-only images from the actual managed riding route,
using the frozen skin and complete 13-part board. No reference pixels, raw
landmarks, generated pack or game assets are included. The viewer keeps the
original reference beside the rig for normal and quarter-speed playback.

| Physical stance | Normal | Lower jump |
| --- | --- | --- |
| Regular |[Phases](normal-regular-phases.jpg) · [Contacts](normal-regular-contacts.jpg)|[Phases](low-regular-phases.jpg) · [Contacts](low-regular-contacts.jpg)|
| Goofy |[Phases](normal-goofy-phases.jpg) · [Contacts](normal-goofy-contacts.jpg)|[Phases](low-goofy-phases.jpg) · [Contacts](low-goofy-contacts.jpg)|

The source is the same landed white-shirt actor from official BATB13
[tL8Ey9GS0xQ](https://www.youtube.com/watch?v=tL8Ey9GS0xQ&t=195), attempt1088.
The retained extraction is bounded434–537, next actorcut538 excluded; the
animation uses460–525. Actual PTS is1001 × bounded frame at timebase1/30000.
The Regular-base/Left nose-foot assignment is a manual inference checked
against the same actor's other shoves; this excerpt has no complete plain-normal
baseline. The other physical stance is an authored anatomical mirror, not a
second filmed take. Original reference pixels are never mirrored.

The anatomical front foot presses and scoops the nose, then releases before a
distinct free Air event. The board makes one local half-turn while the rider
keeps a small balance windup, with no body half-turn, longitudinal flip or
instep flick. Catch feet retain their anatomical roles while deck-local
front/back reverses. Native input, rotation debt, landing and scoring remain
unchanged.

Monocular camera/depth, exact contact timing, fixed-chain foot paths and
unreliable balance-arm depth are labelled authored. Bent elbows replace the
unreliable T-like reach only during source476–500; settled recovery remains
unchanged. The comparison camera is a fixed approximate source-side view,
with canonical offset[-0.906,0.06,-0.423], not a gameplay camera or pose change.
Pop uses the existing ordinary-charge-to-capture handoff; it is not claimed as
an exact copy of the filmed prelaunch arm backswing. Source strike479.5 is
nose-down32 degrees; the normal Regular Pop panel samples479.57292 at
0.8666667s and retains that same pitch.

The native Pad gesture and full32Hz Step/Land/scorer were replayed through the
actual capture dispatcher and final60Hz body: normal36/36 Air frames, low34/34,
in both native stances, then ordinary Ground with capture off at4s. Raw source
stays `reviewed=false`; private test approval is explicitly not for installation.
`manifest.json` records the exact source/runtime/media hashes and replay results.

Claude owns the independent offline quality verdict and library table.
Native/Shadow gameplay acceptance remains separate.
