# Authored normal flip foot component

`SkateCaptureFlick` is an unintegrated presentation component for normal Kickflip
and Heelflip. Its contact sequence still needs visual review before a runtime
hook or approval. It does not change the live board trajectory or scorer. Switch,
nollie, fakie, combined and late flips need explicit separate gating.

After input, a diagonal Kickflip setup or straighter Heelflip setup slides toward
the real nose. Kick's authored ankle motion combines moderate inversion, toes-up
motion and a more crosswise toe heading. At release that orientation is relative
to the upright rider carrier, not added to an already nose-up board. The early
sole slide follows the current real deck; this transition is authored.

## Mesh patches, not a forced instep point

The contact feature comes from the actual maximum-overlap witness of the **same
full-shoe/deck fit** used for clearance. The new fitter overload returns its shoe
triangle, deck triangle and world points. The original overload keeps its offset
and refusal behavior; there is no second fit, tolerance change or contact search.

At bind, actual shoe triangles are classified into upper/outside forefoot and
vamp, rear heel, outsole and other regions. The forefoot region includes the
farther-forward upper toe-box surface seen in the frozen reference mesh. Its
underside and ankle/cuff are excluded geometrically. A material-defined outsole
side boundary cannot be proved from vertices and indices alone; the native
upper/outsole binding remains an explicit review gate. A region centroid guides
the authored sweep, but is never claimed as a pinned contact. The actual support
witness determines the contact point.
A different supporting region or the wrong nose side refuses the endpoint.

These regions are inferred from measured geometry and anatomy, not from a
hardcoded black material name. Their black upper-shoe versus white-outsole
interpretation is checked on the frozen companion. Native avatar footwear and
profile availability remain a Shadow gate. Missing regions refuse binding; the
caller must retain ordinary presentation.

Source footage is blurred and occluded. Therefore the choreography and contact
are **authored**, not measured source contact. `Provenance` and `Contact` retain
that distinction. Source sole-contact masks are never rewritten. `Contact`
contains the actual geometry witness while supported and has no support claim
after release. The selected nose-rim vertex is a trajectory guide; it is not
misreported as the actual mesh contact point.

## Integration contract

- Construct one instance per owner with the immutable deck surface and anatomical
  left/right profiles. Geometry classification and alternate projection profiles
  are cached once; no hierarchy searches or frame allocations are added.
- Call `TryPose` with the measured rig, current frame, ordinary baseline and its
  sole-up vectors, upright carrier, explicit unflipped release deck, elapsed
  flight time, caller-supplied release duration, stance and family.
- The release duration comes from the single onset contract in
  [#606](https://github.com/Dj-Shortcut/rust-skate-mod/pull/606). The helper defines
  no second release-time constant and delays neither pop nor input.
- `releaseDeck` is the real unflipped release endpoint transported into this
  frame's world carrier. Do not reconstruct it from an already-flipping board.
  The helper authors no pitch pulse: the caller owns the board input.
- A steep upper-shoe contact uses a cached rigid re-expression of exactly the
  same vertices and the inverse frame turn for projection. World geometry is
  unchanged. Original sole profiles, source checks and existing fitter guards
  remain intact.
- At release, cache the fitted foot frame in upright-carrier coordinates. Return
  blends from it toward the current free pose over three release durations,
  independently of board roll. Starting after release computes the same analytic
  endpoint without claiming that a skipped contact phase was displayed.
- Reset on a new action, interruption, bail, dismount, unbind or owner change.
  Clock regression or family/stance/duration changes also clear the endpoint.
  Instances are not concurrently callable.
- Missing geometry or classification refuses construction with an argument
  error. The later bind must catch that focused failure and keep ordinary riding.
  Runtime geometry and measured leg failures return `false`, the unchanged input
  frame and a reason; do not block riding or loosen contact/reader guards.

Captured torso, measured ankle-to-toe length, board physics, scoring, source packs,
observations and approvals are preserved. The existing free/catch path owns the
rest of flight. Cached-reference compilation and private mesh probes are not
native execution or full-trick acceptance. Read the current inbox after exports;
Claude reviews live versions without a mandatory wait for a separate approval.
