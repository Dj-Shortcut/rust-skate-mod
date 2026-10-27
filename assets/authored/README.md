# From-scratch content

These models are authored by this project's Blender generator. No original
game files, imported meshes, texture packs, sound packs or model libraries
are inputs. Source code is Apache-2.0; the generated models are
[CC0-1.0](CC0-1.0.txt).

The separately documented [video-derived ollie motion](animation/ollie-video/README.md)
is **CC BY-SA 3.0**, not CC0. It contains retargeted animation data with source
attribution; no source footage, detector model or imported game mesh is bundled.

Regenerate the ramps with Blender 5.2.0. Replace the example executable path
with the matching installation:

```bash
/path/to/blender-5.2.0/blender -b --factory-startup --python scripts/generate_skate_ramp_assets.py -- --output assets/authored
```

The output includes editable `.blend` sources, self-contained `.glb` models
and a manifest of bounds, vertex/triangle counts and SHA-256 hashes.
Coordinates in Blender are Z-up, front toward -Y; glTF export converts to
its standard Y-up coordinates. Units are meters.

- `skate_bank`, `skate_kicker`, `skate_quarterpipe`, `skate_ledge_low`,
  `skate_ledge_high`, `skate_long_ledge`, `skate_funbox`, `skate_halfpipe`:
  authored park structures with wood, concrete and steel materials, coping,
  solid sides/platforms, and separate static collision proxies.

The ramp generator preserves other manifest entries and replaces only its eight.
The ramp GLBs default to the `Visual` scene. `CollisionProxies` is a separate,
opt-in scene; the `.blend` keeps its matching collection hidden from the
viewport/render. There are no transparent collision materials. An engine
adapter must explicitly turn proxy meshes into colliders. The open halfpipe
must not be replaced by a single convex hull. Individual meshes are closed;
adjacent pieces intentionally remain distinct. Units/pivots, full visual and
collision bounds and the difference from the native park assembly are in
[the ramp guide](../../docs/RUST-SKATE-RAMP-ASSETS.md).
For actual Blender previews, add `--preview-dir /absolute/private/output`.
Editable sources include cameras/lights; exports exclude those. Materials use
authored colours and PBR values without external textures. The manifest records
authoring versions; another version can change export layout/triangulation.

The Rust PC client adapter source includes GLB reading, park matching and
checking commands. Codex has not verified its execution, native overlay/contact,
network placement or riding; the pack's geometry checks do not establish those.

The four additional round transition models are listed in [the ramp table](../../docs/RUST-SKATE-RAMP-ASSETS.md#round-transition-assets). Use the generator `--only` selector for a focused update. All existing models and unrelated manifest entries are preserved. Native binding is a separate integration step.
