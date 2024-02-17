using UnityEngine;
using strange.extensions.mediation.impl;

[ExecuteInEditMode]
public class ScaleToScreenSize : View {
  [Inject] CameraService cameraService {get; set;}

  //To account for things like screenshake
  public Vector2 additionalMargin = Vector2.zero;

  public Vector2 percentage = Vector2.one;

  public bool controlX = true;
  public bool controlY = true;
  public bool controlZ = false;

  void LateUpdate(){

#if UNITY_EDITOR
    var camWorldRect = CameraService.calculateCameraWorldRect(Camera.main);
#else
    var camWorldRect = cameraService.cameraWorldRect;
#endif

    var newX = controlX ? camWorldRect.width  * percentage.x + additionalMargin.x : transform.localScale.x;
    var newY = controlY ? camWorldRect.height * percentage.y + additionalMargin.y : transform.localScale.y;
    var newZ = controlZ ? camWorldRect.height * percentage.y + additionalMargin.y : transform.localScale.z;

    transform.localScale = new Vector3(newX, newY, newZ);
  }
}