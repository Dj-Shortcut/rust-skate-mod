# Original build wheel silhouettes

Twelve original CC0 shapes in clockwise build-wheel order, starting with bank at the top. Each SVG contains one white filled path (including its disconnected contour details); the two PNG sizes are white straight-alpha masks with an embedded sRGB profile. Tint the masks at runtime; source files contain no colored edge or shadow.

`preview.png` is the 512px cream/red wheel with bank selected; `preview_128.png` checks the complete wheel at small size. The low bank and ledges retain a low silhouette so their profiles remain distinct from the mini, normal and large quarterpipes. Every icon shares the same baseline and at least10% opaque margin.

Regenerate with `python scripts/generate_skate_icons.py --output <directory>` (Pillow). All paths are directly authored polygons and analytic curves; no imported or traced game/stock icons or icon fonts. The generator checks dimensions, white alpha masks and margins; no native menu binding or in-game appearance is claimed by these files.
