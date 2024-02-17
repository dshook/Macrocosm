using UnityEngine;
using strange.extensions.mediation.impl;

[ExecuteInEditMode]
public class DisableChildrenOutsideScreenSize : View {
  [Inject] CameraService cameraService {get; set;}


  void Update(){

#if UNITY_EDITOR
    var camWorldRect = CameraService.calculateCameraWorldRect(Camera.main);
#else
    var camWorldRect = cameraService.cameraWorldRect;
#endif

    //Only need it to check width for now
    var hWidth = camWorldRect.width / 2f;
    for(var i = 0; i < transform.childCount; i++){
      var child = transform.GetChild(i);
      if(child.localPosition.x > hWidth || child.localPosition.x < -hWidth){
        child.gameObject.SetActive(false);
      }else{
        child.gameObject.SetActive(true);
      }
    }

  }
}