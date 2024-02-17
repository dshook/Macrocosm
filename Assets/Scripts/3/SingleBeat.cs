using System.Collections.Generic;
using MoreMountains.NiceVibrations;
using strange.extensions.mediation.impl;
using UnityEngine;

public class SingleBeat : View, IBeat {

  public float lifeTime;
  public float animateInTime = 1f;
  public float animateOutTime = 1f;
  public float aboutToShrivelWarningTime = 0.5f;
  public float scaleRandomDeviance = 0.15f;
  public bool bonus {get; set;}

  public int numberOfHits;

  public bool isClone = false;

  [Inject] StageRulesService stageRules { get; set; }
	[Inject] InputService input {get; set;}
  [Inject] SpawnService spawner {get; set;}
  [Inject] BeatHitSignal beatHitSignal { get; set; }
  [Inject] ObjectPool objectPool {get; set;}
  [Inject] FloatingText floatingText {get; set;}
  [Inject] SpawnBgCellSignal spawnBgCellSignal {get; set;}

  public BeatTemplateItem beatTemplateItem {get; set;}
  public BeatManager beatManager {get; set;}

  public SpriteRenderer[] renderers;
  public SpriteRenderer bonusRenderer;

  public Collider2D circleCollider;

  bool processedDeath;
  bool processedSplit;
  bool processedCloneThings;
  float timer;

  float moveApartAmt;
  float splitScaleSize;
  float stretchAmt;
  float quarterAnimTime;

  int hits;

  //Reset stuff when being recycled
  protected override void OnEnable(){
    base.OnEnable();

    // isClone = false;
    processedDeath = false;
    processedSplit = false;
    processedCloneThings = false;
    timer = 0f;

    hits = 0;
    numberOfHits = 1;

    moveApartAmt = 0.5f;
    splitScaleSize = 0.8f;
    stretchAmt = 0.2f;

    //Reset sort order messed with by clone & fadeout
    for(var i = 0; i < renderers.Length; i++){
      renderers[i].sortingOrder = i + 1;
      renderers[i].color = Color.white;
      renderers[i].material.SetFloat("_Saturation", 1f);
    }

    bonusRenderer.sortingOrder = 0;
    bonusRenderer.color = bonusRenderer.color.SetA(1);
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
    LeanTween.cancel(gameObject);
  }

  public void Init(){
    quarterAnimTime = animateOutTime * 0.25f;

    transform.localScale = Vector3.zero;
    LeanTween.scale(gameObject, Vector3.one * Random.Range(1 - scaleRandomDeviance, 1 + scaleRandomDeviance), animateInTime)
      .setEase(LeanTweenType.easeOutBack);

    bonusRenderer.gameObject.SetActive(bonus);
  }

  void Update () {
    if(beatManager == null){
      Debug.LogWarning("Beat manager null");
      return;
    }
    if(beatManager.State != BeatManager.BeatManagerState.Playing){ return; }

    if(isClone){
      if(!processedCloneThings){
        DoCloneThings();
      }
      return;
    }

    timer += Time.deltaTime;
    var progress = 1 - Mathf.Clamp01(animateInTime - timer);

    // When demoing, only allow simulated clicking every so often based on the hits
    var demoClick = beatManager.demoMode && timer > (hits * 0.3f) + Random.Range(0.3f, 0.8f);

    var clickTime = Mathf.Abs(timer - animateInTime);
    var allowingNewClicks = clickTime < lifeTime;
    if(stillAlive && allowingNewClicks && (input.GetTouchBeganOnCollider(circleCollider) || demoClick )){
      hits++;
      MMVibrationManager.Haptic(HapticTypes.SoftImpact);

      //Jiggle a bit if not complete
      if(hits < numberOfHits){
        //stretch vertically
        LeanTween.scale(gameObject, Vector3.one + Vector3.up * stretchAmt, quarterAnimTime * 0.5f)
          .setEase(LeanTweenType.easeOutBack);

        //stretch horizontally
        LeanTween.scale(gameObject, Vector3.one + Vector3.right * stretchAmt, quarterAnimTime * 0.5f)
          .setDelay(quarterAnimTime * 0.5f)
          .setEase(LeanTweenType.easeOutBack);

        //back to original
        LeanTween.scale(gameObject, Vector3.one, quarterAnimTime)
          .setDelay(quarterAnimTime)
          .setEase(LeanTweenType.easeOutBounce);
      }

      if(hits >= numberOfHits){
        Split();
      }
    }

    //about to die
    if(!processedSplit && !processedDeath && timer > animateInTime + lifeTime - aboutToShrivelWarningTime){
      var saturationAmt = Utils.Remap(timer, animateInTime + lifeTime - aboutToShrivelWarningTime, animateInTime + lifeTime, 1, 0);

      //Desaturate colors
      foreach(var rend in renderers){
        rend.material.SetFloat("_Saturation", saturationAmt);
      }
    }

    //too late
    if(!processedSplit && !processedDeath && timer > animateInTime + lifeTime){
      Shrivel();
      beatHitSignal.Dispatch(new BeatHitData(){ hit = false, beat = this });
    }

    //time to die
    if(timer > animateInTime + lifeTime + animateOutTime){
      Die();
    }
  }

  bool stillAlive{
    get{
      return !processedDeath && !processedSplit;
    }
  }

  void Split(){
    processedSplit = true;
    beatHitSignal.Dispatch(new BeatHitData(){ hit = true, beat = this, baseScore = numberOfHits });

    //Reset saturation in case it was reduced when about to die
    foreach(var rend in renderers){
      rend.material.SetFloat("_Saturation", 1f);
    }

    //set up the split
    var newBeatGO = objectPool.Spawn(this, transform.parent, transform.position, transform.rotation);
    var newBeat = newBeatGO.GetComponent<SingleBeat>();
    newBeat.isClone = true;
    newBeat.beatTemplateItem = beatTemplateItem;
    newBeat.beatManager = beatManager;
    newBeat.Init();

    //stretch vertically
    LeanTween.scale(gameObject, Vector3.one + Vector3.up * stretchAmt, quarterAnimTime)
      .setEase(LeanTweenType.easeOutBack);

    //stretch horizontally
    LeanTween.scale(gameObject, Vector3.one + Vector3.right * stretchAmt, quarterAnimTime)
      .setDelay(quarterAnimTime)
      .setEase(LeanTweenType.easeOutBack);

    //back to original
    LeanTween.scale(gameObject, Vector3.one * splitScaleSize, quarterAnimTime)
      .setDelay(quarterAnimTime * 2)
      .setEase(LeanTweenType.easeOutBounce);

    //move away from the other cell
    var moveTo = transform.position + (Quaternion.Euler(0, 0, 90) * transform.rotation) * (Vector2.up * moveApartAmt);
    LeanTween.move(gameObject, moveTo, quarterAnimTime)
      .setDelay(quarterAnimTime * 2)
      .setEase(LeanTweenType.easeOutCubic);

    spawnBgCellSignal.Dispatch(beatTemplateItem.type, moveTo, transform.rotation, 0);

    //and finally go away
    FadeDisplay(quarterAnimTime, quarterAnimTime * 3);

    Die(animateOutTime);
  }

  //animations for split/cloned single cells
  public void DoCloneThings(){
    processedCloneThings = true;

    //make sure we're behind the original
    for(var i = 0; i < renderers.Length; i++){
      renderers[i].sortingOrder--;
    }
    bonusRenderer.sortingOrder--;

    //pop in at the right time
    gameObject.transform.localScale = Vector3.zero;
    LeanTween.scale(gameObject, Vector3.one * splitScaleSize, quarterAnimTime / 2f).setDelay(quarterAnimTime * 2f).setEase(LeanTweenType.easeOutExpo);

    //move away from the other cell
    var moveTo = transform.position + (Quaternion.Euler(0, 0, -90) * transform.rotation) * (Vector2.up * moveApartAmt);
    LeanTween.move(gameObject, moveTo, quarterAnimTime)
      .setDelay(quarterAnimTime * 2)
      .setEase(LeanTweenType.easeOutCubic);

    spawnBgCellSignal.Dispatch(beatTemplateItem.type, moveTo, transform.rotation, 0);

    //then go away
    FadeDisplay(quarterAnimTime, quarterAnimTime * 3);

    Die(animateOutTime);
  }

  //Not successful
  void Shrivel(){
    processedDeath = true;

    //Desaturate colors
    foreach(var rend in renderers){
      rend.material.SetFloat("_Saturation", 0f);
    }

    FadeDisplay(animateOutTime, 0);

    LeanTween.scale(gameObject, Vector3.zero, animateOutTime) .setEase(LeanTweenType.easeInQuad);
  }

  void Die(float delay = 0f){
    objectPool.Recycle(this.gameObject, delay);
  }

  void FadeDisplay(float animTime, float delay){
    foreach(var rend in renderers){
      LeanTween.color(rend.gameObject, rend.color.SetA(0), animTime).setDelay(delay);
    }
    LeanTween.color(bonusRenderer.gameObject, bonusRenderer.color.SetA(0), animTime).setDelay(delay);
  }
}
