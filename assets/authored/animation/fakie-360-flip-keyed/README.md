# Fakie 360 Flip — own landed Rodriguez source

Source/control candidate for [batch A7 #736](https://github.com/Dj-Shortcut/rust-skate-mod/issues/736), not an offline-reviewed or native-approved animation. Root owns exact presentation routing; Claude owns the library and independent offline verdict. The emitted raw pack remains `reviewed=false`.

The original, unmirrored [Berrics take](https://www.youtube.com/watch?v=PqJZcwTY1QE&t=326) is Rodriguez in the striped shirt, full-file frames **9780–9875**,326.326–329.496s at30000/1001fps. Source PTS is `globalFrame *1001`, timebase1/30000. Exact surrounding edits are9764→9765 and9881→9882; this selected continuous shot includes setup, backward travel, landed catch and upright rollaway. The exact attempt-table row21882 corroborates the name/landing; original board action, rather than that label alone, was inspected.

Filmed **Goofy** has the **Right nose/front flick and Left tail scoop**. One X reflection plus anatomical side swap creates canonical Regular **Left front outer-upper instep flick /Right tail sole pop**. Original video pixels stay unmirrored. Tail-leading travel comes from the actual game; the body/nose frame must not be rotated to face travel. The exact intended signature is `Ollie, switch=true, tailLeads=true, flip=+360, shove=-360, body/spin=0`. Full local board roll+360 and board-only yaw+360 are explicit, with no body halfturn and a same-ended catch.

Own first body rise9816 precedes tail scoop9821; the board stays flat through9819.5. Front sole release9819.75 leaves the tail sole pressed, tail sole release9822 is separate from the upper-shoe exit9823.25 and the later all-free Air9824.5. The upper patch is never represented as a sole/heel contact bit. Catch9834.5/9835 and Land9837 lead into the same take's recovery9850–9875. These subframe contacts and 3D board/contact curves are authored visual estimates, not measured shoe pressure.

`captured-body.json` contains only this take's normalized reconstruction controls. Strict VIDEO identity/foot-hint gates selected87/96frames; rejects9824–25 and9829–35 remain rejected in the private raw output. Only bracketed9823..9826 and9828..9836 control gaps are interpolated and labelled, followed by two symmetric low-pass passes. Low-confidence wrists/ankles are listed. Raised hands and part of the crown leave the original Air frame near9827–31: the own pelvis/tuck, fixed-chain knees, stable head/gaze and bent asymmetric shoulder-height arms are explicitly authored reconstruction, including recovery. No other trick's body, contact clocks, root curve or motion keys are read.

The baker shares only interpolation, fixed-chain, shoe/deck surface and rotation math. Frozen original shoe/support geometry and shaped riding surface are hash-pinned; the deck's ground datum0.092m and source carrier are retained. Reference rise is removed once from the board; the own body controls are already carrier-local. Native flight, landing and scoring remain authoritative. Existing contact/reach/floor guards are retained.

```sh
python3 scripts/author_skate_fakie_360_flip.py --output /tmp/skate-fakie-tre
```

NumPy is required. Output JSON and `skate_captures.skmc` stay outside this immutable source folder. The CLI validates identity, body/geometry pins, distinct release/free-Air events and full monotonic roll/shove winding. Source/control bake passes; actual four input/body cases, original-skin/13-part-board views,1×/¼× normal/low Regular/Goofy playback, independent Claude review and Shadow gameplay remain pending. Source footage, model, detector arrays and private fixtures are not bundled or relicensed.
