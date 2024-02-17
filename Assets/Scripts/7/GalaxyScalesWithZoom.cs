using System.Collections.Generic;
using UnityEngine;
using strange.extensions.mediation.impl;

public class GalaxyScalesWithZoom : View {
  [Inject] StageSevenDataModel stageSevenData {get; set;}

  public Transform overrideTransform;


  void LateUpdate () {
    var t = (overrideTransform != null ? overrideTransform : transform);
    //scale display for different view sizes
    if(scaleTable[stageSevenData.viewMode] != t.localScale.x){
      t.localScale = Vector3.one * scaleTable[stageSevenData.viewMode];
    }
  }

  static Dictionary<GalaxyViewMode, float> scaleTable = new Dictionary<GalaxyViewMode, float>(){
    {GalaxyViewMode.Galaxy, 1.0f},
    {GalaxyViewMode.System, 0.2f},
    {GalaxyViewMode.Planet, 0.05f},
  };
}
