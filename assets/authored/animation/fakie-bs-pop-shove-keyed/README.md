# Own-source Fakie BS Pop Shove-it

The black-shirt/cap/tattooed-arm rider in [BATB13 Jamie Griffin vs Zach Doelling](https://www.youtube.com/watch?v=tL8Ey9GS0xQ&t=212) performs a backwards tail-leading pop, one board-only backside halfturn, no longitudinal flip or body halfturn, a two-foot catch and a landed flat-ground rollaway. The original cached 181–219s excerpt is reused; no new full-video download or reference pixels are published.

The retained single-actor bounded decoder window is 923–1006, before the next actor cut 1007. Actual cached PTS is 1001×frame with time base 1/30000, fps 30000/1001. Requested public times are only 181+boundedPTS estimates; original full-video frame numbers/hash are unknown. The private strict detector selected this foreground actor in 84/84 frames using fifteen visually checked source-pixel anchors; low model visibility/depth uncertainty is retained.

Manual stance inference is **Goofy-base Fakie**: original anatomical Left makes the leading tail pressure, Right remains nose-side. Same-actor references and actual pop corroborate this, but the bounded excerpt lacks an independent complete plain-normal take. One X reflection plus all Left/Right anatomical pairs produces canonical Regular, Right/tail pop and Left/nose-side lift. The input switch flag never swaps anatomy a second time; body/nose frame and backward velocity remain distinct.

## Motion and reconstruction

Own torso/arm segment directions come from this actual take. Estimated panning-camera yaw, coherent hip/shoulder axes, monocular knee/depth ambiguity, independent board/landing gaze, root heights, shoes and contacts are labelled authored. Raw source frame 967 has an unreliable hip-depth flip; it does not become a body turn. Same-take upright recovery is stabilized rather than following detector leg jitter.

The Right shoe sets up on the toe-side tail beyond the bolts, then pops and scoops. The Left/front foot lifts without a kickflip/heelflip/instep flick. Free shoes stay in the body/nose frame while the deck alone yaws 180; catch targets reverse deck-local ends so the feet and body do not rotate with it. Original shaped-deck/full-shoe clearance, fixed-chain lengths, reach reserve and pressure floor support remain strict. There is no old Cole or Halfcab motion input.

Source estimates: Pop 951, front sole release 951.8, tail strike 953.1, Right-tail release 954.2, distinct later free Air 956, first catch 963.5, both catch 965, source Land 967, upright recovery 992. These are authored visual marks, not calibrated3D contacts. Real native input, shove, observed landing and scoring stay authoritative. The intended signature is Ollie/switch=true/tailLeads=true, flip 0/shove −180/spin 0/body 0.

Source carrier translation/jump is absent from the body. Reference rise is removed only from the pressure board offset and the actual physical carrier is added once at playback. Canonical board+noseZ remains independent of backwards travel; body heading has no extra halfturn.

## Regenerate

```sh
python scripts/author_skate_fakie_bs_pop_shove.py --output context/private/fakie-bs-shove
```

NumPy is required. The executable pins its own original body controls, read-only geometry/math and shaped-board surface. It builds the strict pack before atomically replacing each output. Generated source JSON/packs, raw detections, private test-only approval, footage and model weights remain ignored. Raw packs are always `reviewed=false`.

The ordinary controlled managed four-case route passes with one real input pop and Fakie Pop Shove-it scoring, normal36/36 and low34/34 captured Air samples in each physical stance, and ordinary Ground/capture-off at4s. Full original-skin/13-part-board exports and all four source-decoded1×/¼× viewer replays pass; current phase/feet/head/arm views are visually inspected. The rig-only sheets and exact source/runtime/image pins are in [the review folder](../../../../docs/review-media/a4-fakie-bs-pop-shove/README.md). Claude owns library/offline verdicts; native Shadow acceptance is separate. A successful bake/export/replay does not promote either gate.


The first source-facing representative was rejected for a steep/seated Pop and a wrist projected across the face at Catch. The follow-up is explicitly authored: a bounded gentler pressure-pitch envelope plus brief body extension, and only the canonical Right arm opens outward/down at Catch. The original source yaw/events/native input are unchanged. From decoder 958 onward all board/foot/non-arm source geometry is exact to the first candidate; Land 967 and later recovery are unchanged. Four strict actual routes pass; the source-facing five-phase follow-up and full four-case mesh/viewer review are complete. Camera orientation is solely an estimated viewing basis, never a body correction.


A later dense scoop review found a second, unverified high arm at bounded954–956. The genuine tattooed original Right/canonical Left arm is visibly raised and stays exact. The autograph/QR overlay hides the opposite original Left/canonical Right arm, whose detector elbow/wrist visibility is0.067/0.385 at954. Only that canonical Right elbow/hand is authored into bent side balance953.1–957.5; it returns exactly before accepted Air957.83. Pop952.94, genuine Left arm, all other joints, feet/board, source events and later Air/Catch/Land/recovery remain exact to the prior candidate. The mechanical native-registry union from1a74a0c990075c6c774d8c79564fee20c2be79c7 to edbed3dea6e84c34df3261714f85727ff7c920d2 reproduced all four full managed-body JSON files byte-for-byte with the old unchanged data. The new two-joint correction passes all four strict actual routes; refreshed mesh/source comparison and replays are pending.
