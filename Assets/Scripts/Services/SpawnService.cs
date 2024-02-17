using System.Collections.Generic;
using UnityEngine;
using System;

public class SpawnService
{
  [Inject] ResourceLoaderService loader {get; set;}
  [Inject] ObjectPool objectPool {get; set;}

  Vector3[] spawnWorldCoords = new Vector3[4];

  public void SpawnObjects(
    GameObject toSpawn,
    int howMany,
    RectTransform spawnArea,
    Transform parent = null,
    float? padding = null,
    Action<GameObject> postInitFunc = null,
    float? zPos = null
  )
  {
    Physics.SyncTransforms();
    spawnArea.GetWorldCorners(spawnWorldCoords);
    var pad = padding ?? 0f;

    for(int i = 0; i < howMany; i++){
      var newGo = objectPool.Spawn(
        toSpawn,
        parent,
        new Vector3(
          UnityEngine.Random.Range(spawnWorldCoords[0].x + pad, spawnWorldCoords[3].x - pad),
          UnityEngine.Random.Range(spawnWorldCoords[0].y + pad, spawnWorldCoords[1].y - pad),
          zPos.HasValue ? zPos.Value : 0f
        ),
        Quaternion.identity
      );
      // var newGo = GameObject.Instantiate(
      //   toSpawn,
      //   new Vector3(
      //     UnityEngine.Random.Range(spawnWorldCoords[0].x + pad, spawnWorldCoords[3].x - pad),
      //     UnityEngine.Random.Range(spawnWorldCoords[0].y + pad, spawnWorldCoords[1].y - pad),
      //     zPos.HasValue ? zPos.Value : 0f
      //   ),
      //   Quaternion.identity,
      //   parent
      // );

      if(postInitFunc != null){
        postInitFunc.Invoke(newGo);
      }
    }
  }

  //Spawn in circle area
  public void SpawnObjects(
    GameObject toSpawn,
    int howMany,
    float innerRadius,
    float outerRadius,
    Vector2 center,
    Transform parent = null,
    Action<GameObject> postInitFunc = null,
    float? zPos = null
  )
  {

    for(int i = 0; i < howMany; i++){
      var newGo = objectPool.Spawn(
        toSpawn,
        parent,
        RandomExtensions.RandomPointInCircle(innerRadius, outerRadius, center),
        Quaternion.identity
      );

      if(postInitFunc != null){
        postInitFunc.Invoke(newGo);
      }
    }
  }

  public void SpawnPrefab(
    string prefabPath,
    Transform parent,
    Vector2 position,
    Action<GameObject> postInitFunc = null
  ){
    var toSpawn = loader.Load<GameObject>(prefabPath);

    var newGo = GameObject.Instantiate(
      toSpawn,
      position,
      Quaternion.identity
    );
    if(parent != null){
      newGo.transform.SetParent(parent, true);
    }

    if(postInitFunc != null){
      postInitFunc.Invoke(newGo);
    }
  }
}

