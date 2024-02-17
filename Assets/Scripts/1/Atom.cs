using System.Collections;
using System.Collections.Generic;
using strange.extensions.mediation.impl;
using TMPro;
using UnityEngine;
using MoreMountains.NiceVibrations;

public class Atom : View {

  // public int size = 1;
  int _size = 1;
  public int size{
    get{ return _size; }
    set{
      _size = value;
      atomRenderer.size = _size;
      UpdateSizeAndScale();
    }
  }

  public float combiningCollisionSpeed = 20f;
  public int splitSpeed = 25;

  public float baseSize = 0.18f;

  //Should only be set by other particle scripts
  public bool collidable = true;

  [Inject] TraumaModel trauma { get; set; }
  [Inject] StageRulesService stageRules { get; set; }
  [Inject] AtomCreatedSignal atomCreated { get; set; }
  [Inject] AtomCombinedSignal atomCombinedSignal { get; set; }
  [Inject] AtomDestroyedSignal atomDestroyedSignal { get; set; }
  [Inject] ObjectPool objectPool {get; set;}
  [Inject] ResourceLoaderService loader {get; set;}
  [Inject] AudioService audioService {get; set;}

  public Rigidbody2D rb;
  public ParticleSystem collideEmitter;
  public GameObject rendererGO;
  public AtomRenderer atomRenderer;
  public GameObject correctSizeGO;

  public AudioClip combineSound;
  public AudioClip combineGoalSound;
  public AudioClip splitSound;

  float postCollisionImmunityTime = 0.2f;

  float collideTimeAccum = 0;
  float splitTimeAccum = 0;



  protected override void Awake () {
    base.Awake();
  }

  //Reset stuff when being recycled
  protected override void OnEnable(){
    base.OnEnable();

    collidable = true;
    collideTimeAccum = 0;
    splitTimeAccum = 0;
    correctSizeGO.SetActive(false);
    UpdateSizeAndScale();
    atomCreated.Dispatch(this);
  }

  protected override void OnDisable(){
    Cleanup();
    base.OnDisable();
  }

  protected override void OnDestroy(){
    Cleanup();
    base.OnDestroy();
  }

  void Cleanup(){
    atomDestroyedSignal.Dispatch(this);
    LeanTween.cancel(gameObject);
  }

  void UpdateSizeAndScale(){
    var scale = baseSize * (1 + size * 0.04f);
    rb.mass = size * 0.004f + 0.05f;

    rendererGO.transform.localScale = new Vector2(scale, scale);

  }


  void Update () {
    if(!collidable){
      collideTimeAccum += Time.deltaTime;
      if(collideTimeAccum > postCollisionImmunityTime){
        collidable = true;
        collideTimeAccum = 0f;
      }
    }

    // Don't remember why I had this here, but it was screwing up the tweening in of the atoms when they're created
    // UpdateSizeAndScale();

    if(size >= stageRules.StageOneRules.minSplitSize){
      splitTimeAccum += Time.deltaTime;

      //shake the position some
      transform.position += new Vector3(Random.Range(-1, 1), Random.Range(-1, 1)) * 0.08f;
    }

    if(size == stageRules.StageOneRules.goalSize){
      correctSizeGO.SetActive(true);
    }else{
      correctSizeGO.SetActive(false);
    }

    if(splitTimeAccum > (stageRules.StageOneRules.maxSize - size)){
      SplitParticle();
    }
  }

  void OnCollisionEnter2D(Collision2D col)
  {
    var particleMove = col.transform.GetComponent<Atom>();
    if(particleMove != null){
      // Debug.Log("Collision mag " + col.relativeVelocity.magnitude);
      if(col.relativeVelocity.magnitude > combiningCollisionSpeed && (collidable && particleMove.collidable)){
        CombineParticles(particleMove, col);
      }
    }
  }

  void CombineParticles(Atom other, Collision2D col){
    collidable = false;

    //manually set the velocity here to avoid recycling the other particle before the collision resolves
    //and not ending up with any momentem shift
    rb.velocity = rb.velocity + (0.8f * other.rb.velocity);

    size += other.size;

    collideEmitter.Emit(5 * size);

    other.collidable = false;
    other.gameObject.SetActive(false);
    atomCombinedSignal.Dispatch(other);
    objectPool.Recycle(other.gameObject);


    if(size == stageRules.StageOneRules.goalSize){
      audioService.PlaySfx(combineGoalSound);
    }else{
      audioService.PlaySfx(combineSound);
    }
  }

  void SplitParticle(){
    var atomPrefab = loader.Load<GameObject>("Prefabs/1/AtomWithTrail"); //Can't use property since it thinks its talking about yourself
    var newParticleGO = objectPool.Spawn(atomPrefab, transform.parent, transform.position, Quaternion.identity);
    newParticleGO.transform.position = transform.position;

    var newRb = newParticleGO.GetComponent<Rigidbody2D>();
    var newAtom = newParticleGO.GetComponent<Atom>();

    //set both particles to be uncollidable for a sec so they don't recombine
    collidable = false;
    newAtom.collidable = false;

    trauma.trauma += 0.25f + ((size - stageRules.StageOneRules.minSplitSize) * 0.05f);
    MMVibrationManager.Haptic(HapticTypes.RigidImpact);

    //split our size, giving the new particle the shaft on odd numbers
    newAtom.size = (int)Mathf.Floor(size / 2);
    size = (int)Mathf.Ceil(size / 2);

    //then shoot off in a random direction
    var shootDirection = Quaternion.Euler(0, Random.Range(0, 360), 0) * Vector2.one;
    // shootDirection.Normalize();

    newRb.velocity = shootDirection * splitSpeed;
    rb.velocity = shootDirection * -1 * splitSpeed;

    collideEmitter.Emit(40);
    audioService.PlaySfx(splitSound);
    atomCreated.Dispatch(newAtom);
  }

}
