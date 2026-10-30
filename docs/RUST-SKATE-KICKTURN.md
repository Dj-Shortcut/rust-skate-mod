# Low-speed axle kickturn

At a standstill or below 0.95 m/s, hold the existing manual or nose-manual input,
then steer with the normal left-stick input. The controller manual band is a
partial right-stick deflection, held for 0.2 s; it is not a pop or a new button chord.
Preparation first moves the loaded foot onto the real curved tail, beyond the
bolts, and transfers the hips and complete trunk over the loaded axle. The nose
variant mirrors this at the other end. The formed-deck triangles supply the sole
normal, and the existing measured shoe fitter and fixed-length legs must accept
the pose. A complete current pose confirmation, bound to player, input owner and
foot-geometry version, releases the lift; elapsed preparation time alone cannot.

The foot and weight stay over that support throughout lift and pivot. Releasing
the steer/manual input lowers the raised end before the foot and body return to
ordinary riding. A partially lifted incoming manual settles before preparation.
One request is bounded to 180 degrees. Faster manuals and normal carving keep
their existing routes.

The client uses a separate, authored kinematic pivot state. The player centre
moves around the loaded physical axle through the ordinary Rigidbody velocity,
wall sweep and speed-budget path. Both loaded tyre stations need fresh flat
support; the actual truck profile must be straight and settled. Body/deck pitch
uses that same state and the measured authored truck geometry. A manual score
flag or preview-only yaw does not supply the action, and kickturns award no
new trick score. This is not a multibody tyre/grip simulation.

The controlled managed replay exercises the actual controller, GroundStep and
full Step/Sense/query/WallAhead/budget order, with a managed empty flat world and
body integration. Source completion publication is required (#643). Source-build,
mesh inspection, fresh playback and native Rust/Shadow acceptance are separate.
The earlier five-trick and push clips remain frozen; this does not finish batch 2.
