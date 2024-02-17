using UnityEngine;
using strange.extensions.mediation.impl;

//Monitors the transform to see if it's X number of scenes out of view to the left,
//and if so, bumps it Y scenes to the right so it be part of a moving loop
[ExecuteInEditMode]
public class RepositionToNextCreatureScene : View {
  [Inject] CameraService cameraService {get; set;}

  public int scenesToLeftThreshold = 2;
  public int scenesToMoveRight = 4;


  void Update(){

#if UNITY_EDITOR
    var camWorldRect = CameraService.calculateCameraWorldRect(Camera.main);
#else
    var camWorldRect = cameraService.cameraWorldRect;
#endif

    var leftThreshold = - camWorldRect.width * scenesToLeftThreshold;
    if(transform.position.x < leftThreshold){
      transform.position = transform.position.AddX(camWorldRect.width * scenesToMoveRight);
    }

  }
}