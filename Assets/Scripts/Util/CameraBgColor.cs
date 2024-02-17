using UnityEngine;
using strange.extensions.mediation.impl;

public class CameraBgColor : View
{
  [Inject] StageTransitionStartSignal transitionStart { get; set; }
  [Inject] PaletteService palettes {get; set;}
  [Inject] CameraService cameraService {get; set;}

  protected override void Awake () {
    base.Awake();
    transitionStart.AddListener(OnTransition);
  }

  void Update(){
  }

  void OnTransition(StageTransitionData data){

    cameraService.Cam.backgroundColor = palettes.cameraBackground.getColorAtIndex(data.stage - 1);
  }

}