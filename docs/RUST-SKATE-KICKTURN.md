# Low-speed axle kickturn

At a standstill or below 0.95 m/s, hold the existing manual or nose-manual input,
then steer with the normal left-stick input. The controller manual band is a
partial right-stick deflection, held for 0.2 s; it is not a pop or a new button chord.
Release the steer/manual input to lower the raised end and return to ordinary
riding. One request is bounded to 180 degrees. Faster manuals and normal carving
keep their existing routes.

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
