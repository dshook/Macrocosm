using strange.extensions.mediation.impl;
using UnityEngine;

public class BgSpawner : View {

  [Inject] CameraService cameraService {get; set;}

  public GameObject[] bgObjects;

  [Tooltip("Shouldn't hold anything else if you know what's good for you")]
  public Transform parent;

  [Range(0, 200)]
  public int amount;

  public float minRandScale = 0.8f;
  public float maxRandScale = 1.4f;

  [Inject] SpawnService spawner {get; set;}

  protected override void Awake () {
    base.Awake();

  }

  public void Spawn(){
    if(!isActiveAndEnabled){
      //don't bother spawning if disabled from low quality
      return;
    }

    foreach(var prefab in bgObjects){
      var number = Mathf.RoundToInt((float)amount / bgObjects.Length);

      spawner.SpawnObjects(prefab, number, 0, 40f, cameraService.Cam.transform.position, parent, (GameObject g) => {
        g.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0, 360f));
        g.transform.localScale = Vector3.one * Random.Range(minRandScale, maxRandScale);
      });
    }
  }

  public void Cleanup(){
    parent.DestroyChildren();
  }
}
