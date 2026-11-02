# Capture viewer scene setup

Call `prepareCaptureScene` from `scripts/capture_viewer_scene.js` in the GLTF
load handler before playing the clip:

```js
import {prepareCaptureScene} from './capture_viewer_scene.js';
const gltf = await loader.loadAsync(item.url);
prepareCaptureScene(gltf.scene);
```

The helper disables frustum culling on every SkinnedMesh. Animated limbs can
leave the bind-pose bounds while the camera follows the moving board; using
those bounds can hide the entire rider. Ordinary meshes retain their culling.

The local first-five viewer uses this guard. The rider must remain visible
through flight, landing and recovery in both overview and side views. This
viewer correction does not approve the animation or establish native gameplay.
Capture media, exported models and private review pages stay outside the repo.
