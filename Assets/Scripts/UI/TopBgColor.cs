using UnityEngine;
using strange.extensions.mediation.impl;

public class TopBgColor : View
{
  [Inject] StageTransitionStartSignal transitionStart { get; set; }
  [Inject] PaletteService palettes {get; set;}

  public Unity.VectorGraphics.SVGImage bgImage;

  protected override void Awake () {
    base.Awake();
    transitionStart.AddListener(OnTransition);
  }

  void Update(){
  }

  void OnTransition(StageTransitionData data){

    bgImage.color = palettes.topBg.getColorAtIndex(data.stage - 1);
  }

}