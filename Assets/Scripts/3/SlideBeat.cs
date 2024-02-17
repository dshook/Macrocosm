using System.Collections.Generic;
using strange.extensions.mediation.impl;
using UnityEngine;
using MoreMountains.NiceVibrations;
using Shapes;

public class SlideBeat : View, IBeat {

  public float lifeTimePerBlip;
  public float animateInTime = 1f;
  public float animateOutTime = 1f;
  public float aboutToShrivelWarningTime = 0.5f;
  float lifeTime;

  public Color color;
  public Color blipColor;
  public Color blipCompleteColor;
  public bool isReverse = false;
  public bool checkEndRelease = false;
  public bool unlocked = true;
  public bool bonus {get; set;}

  public Vector3[] points;

  [Inject] StageRulesService stageRules { get; set; }
	[Inject] InputService input {get; set;}
  [Inject] SpawnService spawner {get; set;}
  [Inject] ResourceLoaderService loader {get; set;}
  [Inject] BeatHitSignal beatHitSignal { get; set; }
  [Inject] FloatingText floatingText {get; set;}
  [Inject] ObjectPool objectPool {get; set;}
  [Inject] SpawnBgCellSignal spawnBgCellSignal {get; set;}

  public BeatTemplateItem beatTemplateItem {get; set;}
  public BeatManager beatManager {get; set;}

  public GameObject roller;
  public GameObject reverseArrow;

  public SpriteRenderer[] rollerRenderers;
  public SpriteRenderer reverseArrowRenderer;
  public SpriteRenderer bonusRenderer;

  public BezierSpline spline;
  public SplineToLineRenderer splineToLine;
  public LineRenderer splineRenderer;

  //Ideally would be a shader outline but those are damn frustrating
  public BezierSpline splineOutline;
  public SplineToLineRenderer splineToLineOutline;
  public LineRenderer splineRendererOutline;

  public Collider2D rollerCollider;

  GameObject slidePrefab;

  float timer = 0f;
  bool dragging = false;
  bool finished = false;
  bool passedHalfReverse = false;
  bool startedHaptic = false;

  float rollerSplinePos = 0f;
  float splineStepDist = 0.005f;

  float moveApartAmt = 0.45f;
  float splitScaleSize = 0.8f;

  private class Blip{
    public GameObject gameObject;
    public Disc renderer;
    public bool hit;
    public float splinePos;
  }
  List<Blip> blips = new List<Blip>();

  const float blipsPerLength = 1f;

  protected override void Start () {
    base.Start();

    //assigning it through the editor seems to be self referential and gets a copy of this which has a bug with itween copying
    slidePrefab = loader.Load<GameObject>("Prefabs/3/SlideBeat");

    for(var i = 0; i < rollerRenderers.Length; i++){
      rollerRenderers[i].color = color;
      rollerRenderers[i].material.SetFloat("_Saturation", 1f);
    }

    if(points.Length > 0){
      var numNewCurves = ((points.Length - 1) / 3) - 1;
      for(int c = 0; c < numNewCurves; c++){
        spline.AddCurve();
        splineOutline.AddCurve();
      }
      //set up all points and positioning
      for(int i = 0; i < points.Length; i++){
        spline.SetControlPoint(i, points[i]);
        splineOutline.SetControlPoint(i, points[i]);
      }
      splineToLine.GenerateMesh();
      splineToLineOutline.GenerateMesh();
      roller.transform.localPosition = points[0];
    }

    //now measure the spline and create blips to fill in
    var splineLen = spline.Length(splineToLine.sampleFrequency);
    int numBlips = (int)Mathf.Floor(splineLen * blipsPerLength);
    if(numBlips == 0){
      Debug.LogWarning("Slider not long enough");
    }
    var blipPrefab = loader.Load<GameObject>("Prefabs/3/slider_blip");
    float blipSpacing = 1 / (float)(numBlips);
    if(numBlips > 0){
      for(int i = 0; i <= numBlips; i++){
        var splinePos = (i) * blipSpacing;
        var newBlipGO = GameObject.Instantiate(
          blipPrefab,
          Vector3.zero,
          Quaternion.identity
        ) as GameObject;
        newBlipGO.transform.SetParent(this.transform, true);
        newBlipGO.transform.position = spline.GetPoint(splinePos);

        var newBlip = new Blip(){
          gameObject = newBlipGO,
          renderer = newBlipGO.GetComponent<Disc>(),
          hit = false,
          splinePos = splinePos
        };

        //hide last blip for reverse since the arrow will indicate
        if(isReverse && i == numBlips){
          newBlip.renderer.enabled = false;
        }

        newBlip.renderer.Color = blipColor;
        blips.Add(newBlip);
      }
    }

    if(isReverse){
      reverseArrow.SetActive(true);
      reverseArrow.transform.localPosition = points[points.Length - 1].SetZ(-0.1f);
      reverseArrowRenderer.color = blipColor;

      var endDir = spline.GetDirection(1f);
      var angle = Mathf.Atan2(endDir.y, endDir.x) * Mathf.Rad2Deg;
      //Rotate 90 degrees to get right rotation instead of 180 since original "straight" rotation is 90 degrees off
      reverseArrow.transform.localRotation = Quaternion.AngleAxis(angle, Vector3.forward) * Quaternion.Euler(Vector3.forward * 90);
    }

    var lifeTimeMult = isReverse ? 2 : 1;
    lifeTime = lifeTimePerBlip * (numBlips + 1) * lifeTimeMult + 0.65f;
    // Debug.Log("Slide beat start with points: " + string.Join(",", points));

    bonusRenderer.gameObject.SetActive(bonus);
  }

  void Update () {
    if(beatManager == null){
      Debug.LogWarning("Beat manager null");
      return;
    }
    if(beatManager.State != BeatManager.BeatManagerState.Playing){ return; }

    timer += Time.deltaTime;

    UpdateFromInput();

    var prevRollerSplinePos = rollerSplinePos;
    var forwardReverse = isReverse && passedHalfReverse ? -1 : 1;

    if(dragging && !finished){
      UpdateRollerSplinePosition();
    }

    //For finishing itself in demo mode
    if(beatManager.demoMode){
      if(isReverse){
        if(!passedHalfReverse){
          //Same as normal but double speed
          rollerSplinePos = Mathf.Clamp01(((timer - 0.3f) * 2f) / ((lifeTime * 0.5f) + animateInTime) );
        }else{
          rollerSplinePos = Mathf.Clamp01( 2f - ((timer - 0.3f) * 2f) / ((lifeTime * 0.5f) + animateInTime) );
        }

      }else{

        rollerSplinePos = Mathf.Clamp01(((timer - 0.3f) * 2f) / ((lifeTime * 1.0f) + animateInTime) );
      }
    }

    var vectorToTarget = spline.GetPoint( Mathf.Clamp01(rollerSplinePos + (forwardReverse * splineStepDist))) - roller.transform.position;
    var lookRot = Quaternion.LookRotation(Vector3.forward, vectorToTarget);
    roller.transform.rotation = lookRot;

    if(!finished){
      roller.transform.position = spline.GetPoint(rollerSplinePos);
    }

    //check some blips
    if(!finished && timer > animateInTime && timer < (lifeTime + animateInTime)){
      foreach(var blip in blips){
        if(blip.hit){ continue; }

        var fudgeFactor = 0.05f;
        var ballPassedBlip = rollerSplinePos > (blip.splinePos - fudgeFactor);
        if(isReverse && passedHalfReverse){
          ballPassedBlip = rollerSplinePos < (blip.splinePos + fudgeFactor);
        }

        // Debug.Log($"Blip at {blip.splinePos} hit: {ballPassedBlip}");

        if(ballPassedBlip){
          blip.hit = true;
          blip.renderer.Color = blipCompleteColor;

          var isLastBlip = blip == blips[blips.Count - 1];

          //check to see if we're done in a few different scenarios:
          //1) it's a normal slider and this is the last blip to hit
          if(isLastBlip && !isReverse){
            Split();
            beatHitSignal.Dispatch(new BeatHitData(){ hit = true, beat = this, baseScore = Mathf.Max(1, blips.Count - 1)});
          }
          //2) it's a reverse slide and we're finishing the first half and it's time to turn around
          if(isReverse && isLastBlip && !passedHalfReverse){
            passedHalfReverse = true;
            //only reset up to the next to last one since we'll be on top of the last one still
            for(var b = 0; b < blips.Count - 1; b++){
              blips[b].hit = false;
              blips[b].renderer.Color = blipColor;
            }
            reverseArrowRenderer.color = blipCompleteColor;
          }

          //3 it's a reverse and we're back to the beginning and done
          if(isReverse && passedHalfReverse && blip == blips[0]){
            Split();
            beatHitSignal.Dispatch(new BeatHitData(){ hit = true, beat = this, baseScore = 2 * Mathf.Max(1, blips.Count - 1)});
          }
        }
      }
    }

    //about to die
    if(!finished && timer > animateInTime + lifeTime - aboutToShrivelWarningTime){
      var saturationAmt = Utils.Remap(timer, animateInTime + lifeTime - aboutToShrivelWarningTime, animateInTime + lifeTime, 1, 0);

      //Desaturate colors
      foreach(var rend in rollerRenderers){
        rend.material.SetFloat("_Saturation", saturationAmt);
      }
    }


    //time to die
    if(timer > (animateInTime + lifeTime) && !finished){
      beatHitSignal.Dispatch(new BeatHitData(){ hit = false, beat = this});
      Shrivel();
    }
  }

  //Be very careful to test the differences between mouse and touch input
  void UpdateFromInput(){
    var inputOnRoller = false;

    if(input.touchSupported){
      foreach(var touch in input.touches){
        var touchPosition = input.GetPointerWorldPosition(touch.position);
        if(rollerCollider.OverlapPoint(touchPosition)){
          inputOnRoller = true;
          break;
        }
      }

      if(inputOnRoller){
        var clickTime = Mathf.Abs(timer - animateInTime);
        //close enough?
        if(!dragging && clickTime < lifeTime && inputOnRoller){
          StartDragging();
        }
      }else{
        StopDragging();
      }
    }else{
      var touchPosition = input.pointerWorldPosition;
      if(rollerCollider.OverlapPoint(touchPosition)){
        inputOnRoller = true;
      }

      if(input.GetButtonDown()){
        var clickTime = Mathf.Abs(timer - animateInTime);
        //close enough?
        if(!dragging && clickTime < lifeTime && inputOnRoller){
          StartDragging();
        }
      }

      if(input.GetButtonUp()){
        StopDragging();
      }
    }

  }

  void StartDragging(){
    dragging = true;
    if(!startedHaptic){
      MMVibrationManager.ContinuousHaptic(0.3f, 0.1f, 5.0f);
      startedHaptic = true;
    }
  }

  void StopDragging(){
    if(startedHaptic){
      MMVibrationManager.StopContinuousHaptic();
      startedHaptic = false;
    }
    dragging = false;
  }

  //Find nearest point on the spline to the mouse position
  void UpdateRollerSplinePosition(){
    //step from our current spline position forward and find the min distance to our input point
    var inPoint = input.pointerWorldPosition;

    float minDist = float.PositiveInfinity;
    int minIndex = 0;
    Vector3 minSplinePoint = Vector3.zero;

    //Exit when we've exceeded the bounds of the spline
    for(int i = 0; i < 9999; i++){
      var t = i * splineStepDist;
      if(t > 1f || t < 0f){ break; }

      var splinePoint = spline.GetPoint(t);
      var dist = Vector2.Distance(splinePoint, inPoint);

      if(dist < minDist){
        minDist = dist;
        minIndex = i;
        minSplinePoint = splinePoint;
      }
    }

    rollerSplinePos = (minIndex * splineStepDist);
    var inCollider = rollerCollider.OverlapPoint(inPoint);
    if(!inCollider){
      // Debug.DrawRay(minSplinePoint, (Vector3)inPoint - minSplinePoint, Color.yellow, 5f);
      StopDragging();
      return;
    }
  }

  void Split(){
    finished = true;
    StopDragging();
    var quarterAnimTime = 0.25f * animateOutTime;

    //Reset saturation in case it was reduced when about to die
    foreach(var rend in rollerRenderers){
      rend.material.SetFloat("_Saturation", 1f);
    }

    //set up the split
    var rollerClone = Instantiate(roller, transform.position, transform.rotation, roller.transform.parent);
    rollerClone.transform.localPosition = roller.transform.localPosition;

    //fade out roller & clone cell
    LeanTween.color(roller, Colors.transparentWhite, animateOutTime);
    LeanTween.color(rollerClone, Colors.transparentWhite, animateOutTime);

    if(reverseArrow.activeInHierarchy){
      LeanTween.color(reverseArrow, reverseArrowRenderer.color.SetA(0), quarterAnimTime);
    }

    var majorAxis = (points[points.Length - 1] - points[0]).normalized;

    //shrink a bit
    LeanTween.scale(roller, Vector3.one * splitScaleSize, quarterAnimTime * 3).setEase(LeanTweenType.easeOutQuad);
    LeanTween.scale(rollerClone, Vector3.one * splitScaleSize, quarterAnimTime * 3).setEase(LeanTweenType.easeOutQuad);


    //move away from each other
    var moveTo = roller.transform.position + (Vector3)majorAxis.PerpendicularRight() * moveApartAmt;
    LeanTween.move(roller, moveTo, quarterAnimTime * 2)
      .setDelay(quarterAnimTime)
      .setEase(LeanTweenType.easeOutCubic);


    var moveToClone = roller.transform.position + (Vector3)majorAxis.PerpendicularLeft() * moveApartAmt;
    LeanTween.move(rollerClone, moveToClone, quarterAnimTime * 2)
      .setDelay(quarterAnimTime)
      .setEase(LeanTweenType.easeOutCubic);

    FadeDisplay(quarterAnimTime, 0f);

    spawnBgCellSignal.Dispatch(beatTemplateItem.type, moveTo, roller.transform.rotation, 0);
    spawnBgCellSignal.Dispatch(beatTemplateItem.type, moveToClone, roller.transform.rotation, 0);

    Die(animateOutTime);
    Destroy(rollerClone, animateOutTime);
  }

  void FadeDisplay(float time, float delay){
    //fade spline and blips out
    var fade = splineRenderer.gameObject.AddComponent<ColorFader>();
    var fadeOutline = splineRendererOutline.gameObject.AddComponent<ColorFader>();
    fade.timeToLive = fadeOutline.timeToLive = time;
    fade.delay = fadeOutline.timeToLive = delay;

    foreach(var blip in blips){
      LeanTween.color(blip.gameObject, blip.renderer.Color.SetA(0), time).setDelay(delay);
      LeanTween.alpha(blip.gameObject, 0f, time).setDelay(delay);
    }

    LeanTween.color(bonusRenderer.gameObject, bonusRenderer.color.SetA(0), time).setDelay(delay);
  }

  //Not successful
  void Shrivel(){
    finished = true;
    StopDragging();
    var quarterAnimTime = 0.25f * animateOutTime;


    //Desaturate colors
    foreach(var rend in rollerRenderers){
      rend.material.SetFloat("_Saturation", 0f);
      LeanTween.color(rend.gameObject, rend.color.SetA(0), animateOutTime);
    }
    LeanTween.color(bonusRenderer.gameObject, bonusRenderer.color.SetA(0), animateOutTime);

    if(reverseArrow.activeInHierarchy){
      LeanTween.color(reverseArrow, reverseArrowRenderer.color.SetA(0), quarterAnimTime);
    }

    //Track and blips go away fast
    FadeDisplay(quarterAnimTime, 0f);

    LeanTween.scale(roller, Vector3.zero, animateOutTime).setEase(LeanTweenType.easeInQuad);

    Die(animateOutTime);
  }

  void Die(float delay = 0f){
    Destroy(gameObject, delay);
  }


}