# A1 Inward Heelflip visual candidate

[Batch A1 #684](https://github.com/Dj-Shortcut/rust-skate-mod/issues/684).
[Source and rebuild instructions](../../../assets/authored/animation/inward-heelflip-derived/README.md).

**Authored shared-Heel + BS180 derivation, not a filmed Inward take.** The private
viewer compares against the shared normal Heel body reference, labelled as such.
The source search, including 113 landed normal Inward annotations elsewhere and
the three indexed cached games, is recorded with the source. No source pixels,
footage, pack or GLB is committed here. These sheets show only the original rig.

| Physical mesh stance | Flight | Load → Pop → Air → Catch → Land → Rollaway → End | Heel exit, catch and rollaway close views |
| --- | --- | --- | --- |
| Regular, left foot first | Normal | [Phase sheet](physical-regular-normal.jpg) | [Contacts](physical-regular-normal-contacts.jpg) |
| Regular, left foot first | Low | [Phase sheet](physical-regular-low.jpg) | [Contacts](physical-regular-low-contacts.jpg) |
| Goofy, right foot first | Normal | [Phase sheet](physical-goofy-normal.jpg) | [Contacts](physical-goofy-normal-contacts.jpg) |
| Goofy, right foot first | Low | [Phase sheet](physical-goofy-low.jpg) | [Contacts](physical-goofy-low-contacts.jpg) |

Physical labels come from the exported GLB foot-bone positions against deck
forward. The frozen preview's requested binding maps oppositely; the viewer
selects the measured physical stance. This is not native stance validation.

The candidate has a tail-loaded pop and heel-led exit, complete negative flip
plus backside half-shove, non-spinning body, bent balance elbows and a catch on
the reversed deck. Both foot positions and toe headings are transformed into
the reversed board frame, preserving the anatomical world sides. The body stays
low through the free-Air entry endpoint so the earlier tail contact remains
reachable. No runtime reach/floor guard or physics rule was relaxed.

The actual Pad → complete 32 Hz Step/Land/scorer → current 60 Hz body replay
selects this exact ID, scores Inward Heelflip, captures all Air frames (36 normal,
34 low), lands and returns to uncaptured ordinary Ground through 4 s in all four
cases. The original raw pack remains `reviewed=false`. Test approval belongs to
a separate private `TEST-ONLY-NOT-FOR-INSTALL` copy.

All four cases were checked in the decoded-source viewer at **1× and ¼×**,
including seven phase seeks per case, plus the heel exit/catch/rollaway close
views. The final mesh has the original skin and a complete 13-part skateboard.
The exporter does not adjust poses. Initial straight balance elbows were caught
in our own mesh review and corrected before these images were made.

The author checked the candidate; **Claude's offline judgement and Shadow's
native/gameplay gates remain pending**. Export and managed replay are not game
approval. The library document remains Claude's responsibility.

Final source authoring: `89d742f`; exact-route freeze: `09e0a16`.
Raw source pack SHA-256:
`69683c621864d4ed949e307f0543ded035e8a0046bfeb7e939ab061aa7fcc5dd`.
