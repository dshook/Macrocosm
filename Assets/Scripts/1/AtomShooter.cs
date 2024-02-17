using System.Collections;
using System.Collections.Generic;
using strange.extensions.mediation.impl;
using UnityEngine;
using MoreMountains.NiceVibrations;

public class AtomShooter : View {

  [Inject] InputService input {get; set;}
  [Inject] StageOneDataModel stageData {get; set;}
  [Inject] StageRulesService stageRules { get; set; }
  [Inject] ObjectPool objectPool {get; set;}
  [Inject] AudioService audioService {get; set;}
  [Inject] CameraService cameraService {get; set;}

  public float shootSpeed = 200f;
  public float chargeUpTime = 0.5f;
  public float moveBackAmount = 0.25f;
  public float moveBackAnimTime = 0.25f;
  public GameObject shootingAmmo;
  public GameObject shotParent;
  public Transform shootPoint;
  public AudioIntroLooper chargeUpLooper;
  public ParticleSystem shootParticles;

  public AudioClip shootSound;

  public GameObject[] chargeLines;

  int charges = 0;
  float chargeAccum = 0f;
  float cooldownTimer = 0f;

  Vector3 startPostition;

  protected override void Awake(){
    base.Awake();

    startPostition = transform.localPosition;
  }

  void Update () {
    //rotate shooter
    transform.rotation = MouseToShooterAngle();

    //try charging up
    if(stageRules.StageOneRules.maxChargeUp > 0 && input.ButtonIsDown()){
      chargeAccum += Time.deltaTime;
      if(!chargeUpLooper.Playing){
        chargeUpLooper.Play();
      }

      if(chargeAccum >= chargeUpTime){
        var newCharges = Mathf.Min(charges + 1, stageRules.StageOneRules.maxChargeUp);
        if(charges != newCharges){
          MMVibrationManager.Haptic(HapticTypes.SoftImpact);
        }
        charges = newCharges;
        chargeAccum = 0f;
      }
    }else{
      chargeUpLooper.Stop();
    }

    //display the charge indicators
    for(var i = 0; i < charges; i++){
      chargeLines[i].gameObject.SetActive(true);
    }
    for(var i = charges; i < chargeLines.Length; i++){
      chargeLines[i].gameObject.SetActive(false);
    }

    cooldownTimer += Time.deltaTime;
    if(input.GetButtonUp() && cooldownTimer >= stageRules.StageOneRules.shootCooldown){
      cooldownTimer = 0;
      Shoot();
    }
  }

  Quaternion MouseToShooterAngle(){
    var pos = cameraService.Cam.WorldToScreenPoint(transform.position);
    var dir = input.pointerPosition - pos;
    var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
    return Quaternion.AngleAxis(angle, Vector3.forward);
  }

  void Shoot()
  {
    var newParticle = objectPool.Spawn(shootingAmmo, shotParent.transform, shootPoint.position, Quaternion.identity);

    var newRb = newParticle.GetComponent<Rigidbody2D>();
    var atom = newParticle.GetComponent<Atom>();

    atom.size = 1 + charges;
    charges = 0;
    chargeAccum = 0f;

    var shootDirection = (Vector2)input.pointerWorldPosition - (Vector2)transform.position;
    shootDirection.Normalize();

    newRb.velocity = shootDirection * shootSpeed;

    var moveBackTime = moveBackAnimTime * 0.25f;
    LeanTween.moveLocal(gameObject, startPostition + ((Vector3)shootDirection.Rotate(180f) * moveBackAmount), moveBackTime)
      .setEase(LeanTweenType.easeOutCirc);

    LeanTween.moveLocal(gameObject, startPostition, moveBackAnimTime - moveBackTime)
      .setDelay(moveBackTime)
      .setEase(LeanTweenType.easeInOutCubic);

    shootParticles.Emit(8);

    MMVibrationManager.Haptic(HapticTypes.LightImpact);
    audioService.PlaySfx(shootSound);
    chargeUpLooper.Stop();
  }

}
