# Runtime compliant truck contact model

The procedural board uses thirteen parts: the primary deck with unchanged riding wood and eight bolts, two rigid
hangers, four wheels, two fixed kingpin/cup assemblies and four closed bushings.
Legacy enum values `Deck=0`, `Truck=1`, `Wheel=2` and the first seven parent indices
remain; `Fixed=3`, `Bushing=4` are added. The wood, eight bolts and original texture remain byte-identical. Only the
baseplate mounting boxes inside the deck part have a thinner underside; their
nominal top plane remains Y=83mm, below the unchanged riding surface. This is not an authored-GLB swap.

## Mechanical constraints

A hanger turns about the line between its fixed pivot cup and kingpin eye, rather
than about the kingpin itself. The fixed cup is at Y=76.5mm and 17.5mm beyond its axle
station; the eye is at Y=59mm, defining an explicit authored 45-degree caster.
Both axles remain at Z=±245mm. This angle is not measured from a reference video.
A closed hemispherical socket opens toward the moving stem and joins the fixed
mounting plate. The inner triangle planes clear the full pivot ball, rather than
relying on a vertex-radius or static-overlap check. Hanger load is a small rotation about the
cup's local X axis, solved to give the requested eye displacement along the fixed
kingpin. The ball centre remains fixed. The metal axle and wheel children follow this complete rigid hanger pose; there
is no independent wheel-height or wheel-yaw trick. Front and rear yaw oppose each
other. The yaw magnitude is atan(sin(displayedLean)) for this caster, while wheel
camber is cancelled against the same deck lean. This is coupled lean-to-steer,
not just a deck roll around stationary wheel axes.

Displayed deck lean is a critically damped response to observed lean, bounded to
±20 degrees. The cup geometry derives opposite front/rear steering and cancels
wheel camber relative to that same displayed deck lean. The existing public
`TruckTurn=0.8` constant is retained for compatibility; it is not used as a second
multiplier in the articulated model. `Truck.Axis` now records the cup steering
axis. Full hanger bases are needed for the additional load articulation.

The bushings have installed axial lengths 18.5mm and 16.5mm. Explicit authored
parameters are 28% preload, 800N balanced preload force and 30kg equivalent mass
per truck. Linearized stiffness is derived from the unloaded lengths and balanced
preload. The eye displacement is limited to ±1.5mm, with critically damped analytic
integration and inelastic travel stops. Reference support load and a bounded
landing-deceleration proxy use actual ground/landing observations. These parameters
are authoring choices, not measured urethane properties or measured wheel loads.

The old four uniform scale-only annuli cannot meet a tilted hanger and both fixed
washers without opening gaps. Their node rest quaternions therefore remain fixed
at ±45 degrees, but the closed ring meshes now deform. The washer cap stays fixed;
the other cap intersects the actual hanger face along kingpin-parallel rays. This
keeps the inner bore centred on the fixed rod. A middle-ring radial correction
preserves actual closed mesh volume. Both caps, compressive strain, bore clearance,
wall thickness and hanger contact-ring coverage are checked before accepting a
state. This explicitly extends the standalone rig's former scale-only simplification.

## Shared supported frame

The existing `PrepareSupport()` render hook advances mechanical state once before
rider posing. A fresh `TryObservedGround` plane permits a bounded fit only during
ordinary, nearly flat riding. Pushing, manuals, slides/reverts, ramp/wall motion,
actions, grabs, scenarios and the showcase do not claim this four-wheel support.
Forward and reverse travel share the same physical board-space articulation.

The finite-width circular tyre profile is read from the actual wheel mesh rings.
A single translation along the observed plane normal puts **all four** tyre
profiles on that plane; a spread over 0.5mm or translation over 40mm is refused.
The displayed deck lean and hanger state are the same inputs to that fit.
`Contact()`, `PoseFor()` and `Follow()` reuse its cached offset, so the rider's
foot plane and DeckFrame are not fitted to an earlier board pose. Confirmed
ordinary ground clears residual air-only Pop/AirPitch display offsets. No player
Transform, velocity, movement force, input or score is written.

Outside supported ground the previous offset relaxes continuously toward zero;
mechanical springs return to preload. Mount, bail, dismount and rebuild reset
state and the actual deformed meshes. Airborne/held/action-owned wheels retain
their last signed rolling velocity with weak exponential bearing damping, so a
shove cannot reverse wheel spin. The existing loose-board/bail route remains separate.

## Evidence and remaining boundary

This is an authored compliant-contact model, **not a full multibody tyre-grip or
calibrated material simulator**. It uses a local observed plane, not four new
terrain raycasts. The rendering wheel's 24 circumferential segments approximate
its circular profile within 0.231mm. Uneven ground, ramp compliance, manual and
grind support, native rendering, footwear, performance and actual riding feel
remain separate acceptance gates. The existing game yaw/lean relationship is
not a no-slip tyre model; these geometric constraints do not prove zero lateral
slip or supply new tyre forces.

A preview must consume the actual hierarchy, full hanger bases and ring vertices.
It must not flatten the rig, replay the old kingpin-only rotation, uniformly shrink
both bushings or invent oscillations. The primary build reports two hangers, four
wheels and four bushings. Existing one-piece/primitive fallback routes do not have
this mechanism and report their degraded rendering path.

Bushing buffers are reused and uploaded only when vertex changes exceed 0.1µm.
Managed allocation checks and cached-reference compilation do not measure Unity's
native mesh marshalling or GPU cost. The combined native playtest remains required.
