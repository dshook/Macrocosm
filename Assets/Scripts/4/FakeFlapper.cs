using MoreMountains.NiceVibrations;
using strange.extensions.mediation.impl;
using UnityEngine;

[ExecuteInEditMode]
public class FakeFlapper : View {
  [Inject] AudioService audioService {get; set;}

  public Transform wheel;

  public float maxDeflectionAngle = 12f;
  public float wheelRotationForMaxDeflection = 5f;
  public float pegThicknessAngle = 5f;

  public float flapperWedgeAngleCorrection = 1f;

  public Vector3 neutralAngle = new Vector3(0, 0, 90);

  public AudioClip hitClip;

  const int pegCount = 12;
  float pegAngle;
  bool isNeutral;

  protected override void Awake()
  {
    base.Awake();
    pegAngle = 360f / pegCount;
  }

  void Update(){
    if(wheel == null){ return; }

    var wheelRot = wheel.eulerAngles.z - pegThicknessAngle;

    var angleRemainder = wheelRot % pegAngle;

    if(angleRemainder <= wheelRotationForMaxDeflection){
      var deflection = -Mathf.Lerp(0, maxDeflectionAngle, angleRemainder / wheelRotationForMaxDeflection * flapperWedgeAngleCorrection);
      transform.eulerAngles = neutralAngle + new Vector3(0, 0, deflection);

      if(isNeutral){
        isNeutral = false;
        if(audioService != null){
          audioService.PlaySfx(hitClip);
        }
        MMVibrationManager.Haptic(HapticTypes.LightImpact);
      }
    }else{
      transform.eulerAngles = neutralAngle;
      isNeutral = true;
    }


    //If we need to fake high speeds someday
    // if(curSpeed > thresholdSpeed){
    //   fakingIt = true;
    //   flapper.eulerAngles = new Vector3(0, 0, 60f + Random.Range(-5f, 5f));
    // }
  }

}