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

Ordinary four-case route integration, full original-rig/13-part-board viewer and source-decoded 1×/¼× review are pending. Claude owns library/offline verdicts; native Shadow acceptance is separate. A successful bake is not an approved animation or game test.
