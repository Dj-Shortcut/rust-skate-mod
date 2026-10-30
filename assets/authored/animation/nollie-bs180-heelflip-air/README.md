# Source-led Nollie Backside 180 Heelflip Air

This family uses the striped-rider take 3447–3559 from the owner's existing
[Berrics reference](https://www.youtube.com/watch?v=PqJZcwTY1QE&t=115).
The private original pack is bound to SHA256
`862f205f76712654bf0a0015406edb1bcd17da20d89b5fef139e00ae1940de9a`.
The older model-issue take 3361–3438 is not this input.

First remove the separately diagnosed native entry/root masking and the
NBS-specific full-deck free-foot projection. The source correction then
reconstructs only free Air 3505–3515.5: pelvis and both ankles collect together,
the upper group folds over the knees, and the far canonical-left elbow bends.
The right-arm gesture, source facing, tangential foot paths and shoe
orientations are retained. Hidden arm depth and metric controls are explicitly
authored, not calibrated motion capture.

```sh
python scripts/author_skate_nollie_bs180_heelflip_air.py \
  --input /absolute/path/to/original/source.json \
  --output context/artifacts/nollie-bs180-heelflip/output
```

Complete board motion, body-yaw directions, events and contact flags remain
exact. Pop through 3505 and Catch 3516/Land 3518/recovery stay source-identical.
The existing strict writer checks fixed chains and interpolated midpoints
before replacing output. Original and output remain `Reviewed=false`;
private test approval, graphical review and Shadow acceptance are separate.
No footage, raw detector output, private evaluation pack or mesh is published.
