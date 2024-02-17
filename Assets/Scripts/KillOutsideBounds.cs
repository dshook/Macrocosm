using UnityEngine;
using strange.extensions.mediation.impl;

public class KillOutsideBounds : View {

  public RectTransform bounds;
  public Transform childrenToWatch;
  public bool useRecycle = true;

  [Inject] ObjectPool objectPool {get; set;}

  Vector3[] boundsWorldCoords = new Vector3[4];

  void Update () {
    bounds.GetWorldCorners(boundsWorldCoords);

    var killed = 0;

    int childCount = childrenToWatch.childCount;
    for (int i = childCount - 1; i >= 0; i--)
    {
      var child = childrenToWatch.GetChild(i);
      var pos = child.position;

      if( !(
        pos.x > boundsWorldCoords[0].x &&
        pos.x < boundsWorldCoords[3].x &&
        pos.y > boundsWorldCoords[0].y &&
        pos.y < boundsWorldCoords[1].y
      )){
        if(useRecycle){
          objectPool.Recycle(child.gameObject);
        }else{
          GameObject.Destroy(child.gameObject);
        }
        killed++;
      }
    }

    if(killed > 0){
      Logger.Log("Killed Outside bounds: " + killed);
    }
  }

}
