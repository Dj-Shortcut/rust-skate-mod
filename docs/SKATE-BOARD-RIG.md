# Original rigid skateboard rig

All geometry, graphics, mesh construction, and materials are original procedural work, dedicated under CC0-1.0. No external models, images, textures, fonts, armatures, or baked animations are used by the asset.

Generate with installed Blender 5.x:

```sh
blender -b --factory-startup --python scripts/generate_skate_board.py -- --output <dir>
```

Generated and previewed with Blender 5.2.0 LTS and Eevee.

Deliverables are `skate_board.glb`, editable `skate_board.blend`, `skate_board_rig.json`, the source generator, and the three PNG previews. The source blend contains the rig in its neutral rest pose only. Studio floor, lights, camera, and the red kingpin-axis guide exist only in the generated previews and are excluded from the asset. The lean image stages the whole board upward to ground its lowest wheel; that translation is never exported.

The asset has 6,626 triangles, 40 nodes, and 39 meshes. Each mesh has exactly one triangle primitive and material with positions and normals. The 190,136-byte GLB embeds one binary buffer and uses only TRS node transforms. Every node has an exact matching `extras.role`. Its root is ground-centred and identity; glTF axes are +Y up, +Z nose, +X right. Deck dimensions are 0.80 by 0.204 m, its flat top is 0.100 m, wheel radii are 0.027 m, wheel centres are +/-0.082 m across and +/-0.245 m along, and hanger pivots are y=0.059 m. The authored deck has concave, 17-degree kicks, and seven separate ply edge strips.

Only bushings preserve fixed rest quaternions that align their local +Y with the slanted kingpin, as expressly clarified by the owner. Runtime must scale these nodes without overwriting their rotation. Every other rest rotation is identity and every scale is one. Hanger axes are local glTF `(0, 0.70710678, -0.70710678)` front and `(0, 0.70710678, 0.70710678)` rear. Set front angle `0.8*L`, rear `-0.8*L`, whole-board roll `-L` about +Z. Wheels spin about local +X; asymmetric colored side marks make that rotation visible. Bushing scale is `(1 + 0.4*c, 1 - c, 1 + 0.4*c)` for `c` from 0 to 0.35.

`clearance_check.json` records actual world triangle BVH overlaps at every half-degree from -20 through +20 degrees. Both hangers and all four complete wheel groups were checked against all fixed deck/baseplate/kingpin/bolt meshes: 486 comparisons passed with no overlap. The minimum sampled vertex-to-fixed-surface distance was about 4 mm. This is a sampled geometric check; it is not a continuous analytical collision proof. Compliant bushings intentionally bridge the hanger and fixed seats and are excluded from the hard-component overlap test. `gltf_readback.json` records the independent binary/JSON readback inside the generator. The final bytes also pass the current production SkateModel parser.

This is the requested standalone asset. The procedural runtime board is unchanged; its existing 0.23 m axle constant differs from this asset's requested 0.245 m. Runtime import, part binding, physics retuning, and native game acceptance remain a separate integration boundary.

Generate into a separate output directory, then review and update only this authored-manifest entry. The existing park generator and eight park models are independent.
