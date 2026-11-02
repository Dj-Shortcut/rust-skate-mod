export function prepareCaptureScene(scene) {
  scene.traverse(object => {
    if (object.isSkinnedMesh) object.frustumCulled = false;
  });
  return scene;
}
