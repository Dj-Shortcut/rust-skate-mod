# Fakie Inward Heelflip: authored candidate

Batch A7 [#736](https://github.com/Dj-Shortcut/rust-skate-mod/issues/736).
**Authored shared-mechanics derivative, not an independently filmed Inward.**
The reviewed Fakie Heelflip's shared Fakie Kickflip body supplies entry and
recovery; heel contact, board-only BS180 scoop and reversed-deck catch are
authored using the reviewed normal Inward mechanics. The additional 8r83 take
remains unconfirmed. The source/body controls and baker are in
[`assets/authored/animation/fakie-inward-heelflip-keyed`](../../../assets/authored/animation/fakie-inward-heelflip-keyed/).

| Physical stance / flight | Whole sequence | Heel, Air, Catch, Land feet | Landing and recovery |
|---|---|---|---|
| Regular / normal | [Phases](physical-regular-normal-phases.jpg) | [Contacts](physical-regular-normal-contacts.jpg) | [Recovery](physical-regular-normal-recovery.jpg) |
| Regular / low | [Phases](physical-regular-low-phases.jpg) | [Contacts](physical-regular-low-contacts.jpg) | [Recovery](physical-regular-low-recovery.jpg) |
| Goofy / normal | [Phases](physical-goofy-normal-phases.jpg) | [Contacts](physical-goofy-normal-contacts.jpg) | [Recovery](physical-goofy-normal-recovery.jpg) |
| Goofy / low | [Phases](physical-goofy-low-phases.jpg) | [Contacts](physical-goofy-low-contacts.jpg) | [Recovery](physical-goofy-low-recovery.jpg) |

These are rig-only images read back from four animated GLBs: the original skin
and full 13-part shaped skateboard, with no source pixels embedded. Physical
stance is measured from the exported anatomical foot bones against the initial
nose; it is recorded separately from the controlled requested binding. Normal
and low use actual product Pad/32 Hz Step/Land/scoring traces and the 60 Hz normal
capture route, with the two family adapters now integrated in this PR. Native geometry and
foot bindings are controlled; this does not prove native Rust/Shadow behavior.

Authoring source snapshot: `fbd8939c1e9d859cf7eec41a5b584135ee8f1f4f`.
Product route snapshot: `14f1e25ad611802776d703a2a8fceaf80e17ec33`, plus the
same family adapters. Current-main integration at
`c0ecdb714caafa50dd7e143e518718ab7a7a570c` compiled all 81 client sources
against 356 hash-checked current Shadow SDK references (C# 7.3). Only the
separate native foot-calibration implementation changed from the controlled
route baseline; this compilation does not replace a current native replay. Raw pack
`0e503923a10c10c7887f8e331c2f1e45a05a3b243a9cc233501aebd723a3c07d`
remains `reviewed=false`. The installed approved pack is untouched.

The private viewer offers all four cases, 1×/¼× replays and the cached original
[shared Fakie Kickflip body film](https://www.youtube.com/watch?v=PqJZcwTY1QE&t=90.323567)
(frames 2707–2772, 30000/1001fps; filmed Goofy). That film is body reference,
not proof of an Inward heel flick or scoop. Its original pixels stay unmirrored.
The original cache starts at 2664, so the displayed body's first frame is local
1.4347667 seconds, not local 0. Third-party reference video is not in this PR.

Phase inspection covers Load/Pop/Heel/Air/Catch/Land/rollaway/end in both
physical stances and both heights, including close contacts. Complete continuous
1×/¼× review remains pending. The runtime integration is present; Claude owns the
independent offline verdict and library document; native Shadow acceptance
remains a separate gate. A successful export is not an approved animation.
