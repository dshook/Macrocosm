using strange.extensions.mediation.impl;
using UnityEngine;

//Only for stage 7 now since it needs to depend on galaxy time, easily changable to just be delta time though
public class Rotater : View {
  [Inject] StageSevenDataModel stageSevenData {get; set;}

  [Tooltip("Degrees per second")]
  public Vector3 speed;


  void Update () {
    transform.localRotation = Quaternion.Euler( transform.localRotation.eulerAngles + (speed * Time.smoothDeltaTime * stageSevenData.timeRate));
  }

}
