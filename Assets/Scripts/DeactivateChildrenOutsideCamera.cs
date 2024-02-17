using UnityEngine;
using strange.extensions.mediation.impl;

public class DeactivateChildrenOutsideCamera : View {
  public Transform childrenToWatch;
  public Vector2 margin;

  [Inject] CameraService cameraService {get; set;}

  Rect prevCalculatedRect = Rect.zero;
  void Update () {
    var bounds = cameraService.cameraWorldRect;

    if(bounds == prevCalculatedRect){
      //Optimization to only change stuff when needed
      return;
    }

    prevCalculatedRect = bounds;
    int childCount = childrenToWatch.childCount;
    for (int i = childCount - 1; i >= 0; i--)
    {
      var child = childrenToWatch.GetChild(i);
      var pos = child.position;

      var inBounds = (
        pos.x > bounds.xMin - margin.x &&
        pos.x < bounds.xMax + margin.x &&
        pos.y > bounds.yMin - margin.y &&
        pos.y < bounds.yMax + margin.y
      );

      child.gameObject.SetActive(inBounds);
    }

  }

  //Re-enable everything when disabling, mostly useful for testing
  protected override void OnDisable(){
    int childCount = childrenToWatch.childCount;
    for (int i = childCount - 1; i >= 0; i--)
    {
      var child = childrenToWatch.GetChild(i);
      child.gameObject.SetActive(true);
    }

    base.OnDisable();
  }

  //Re-run logic to make sure tiles are deactivated again after switching panels
  protected override void OnEnable(){
    prevCalculatedRect = Rect.zero;

    Update();
  }

}
