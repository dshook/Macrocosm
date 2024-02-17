using strange.extensions.mediation.impl;
using UnityEngine;
using System.Linq;
using MoreMountains.NiceVibrations;

public class MultiBeat : View, IBeat {

  public float lifeTimePerCell;
  float lifeTime;
  public float animateInTime = 1f;
  public float animateOutTime = 1f;
  public float aboutToShrivelWarningTime = 1f;
  public bool bonus {get; set;}

  public Vector3[] points;

  public Color color;
  public int? comboNumber = null;

  public bool isClone = false;

  [Inject] StageRulesService stageRules { get; set; }
	[Inject] InputService input {get; set;}
  [Inject] BeatHitSignal beatHitSignal { get; set; }
  [Inject] ResourceLoaderService loader { get; set; }
  [Inject] FloatingText floatingText {get; set;}
  [Inject] ObjectPool objectPool {get; set;}
  [Inject] SpawnBgCellSignal spawnBgCellSignal {get; set;}

  public BeatTemplateItem beatTemplateItem {get; set;}
  public BeatManager beatManager {get; set;}

  public BezierSpline spline;
  public SplineDecorator splineDecorator;

  GameObject singleBeatPrefab;
  MultiBeatCell[] cells = null;

  bool processedDeath = false;
  bool processedSplit = false;
  float timer = 0f;

  float moveApartAmt = 0.5f;
  float splitScaleSize = 0.8f;


  protected override void Start () {
    base.Start();


    //assigning it through the editor seems to be self referential and gets a copy of this which has a bug with itween copying
    singleBeatPrefab = loader.Load<GameObject>("Prefabs/3/MultiBeat");

    if(points.Length > 0){
      //set up all points and positioning
      for(int i = 0; i < points.Length; i++){
        spline.SetControlPoint(i, points[i]);
      }
    }

    splineDecorator.objectPool = objectPool;
    splineDecorator.Init();

    cells = gameObject.GetComponentsInChildren<MultiBeatCell>();

    for(var c = 0; c < cells.Length; c++){
      var cell = cells[c];
      cell.bonus = bonus;
      cell.bonusRenderer.gameObject.SetActive(bonus);
    }

    if(isClone){
      DoCloneThings();
      return;
    }

    var scaleDev = 1f;
    for(var c = 0; c < cells.Length; c++){
      var cell = cells[c];
      cell.transform.localScale = Vector3.zero;
      LeanTween.scale(cell.gameObject, Vector3.one * scaleDev, animateInTime).setDelay(c * 0.05f).setEase(LeanTweenType.easeOutBack);
    }

    lifeTime = cells.Length * lifeTimePerCell;

  }

  void Update () {
    if(beatManager == null){
      Debug.LogWarning("Beat manager null");
      return;
    }
    if(beatManager.State != BeatManager.BeatManagerState.Playing){ return; }
    if(isClone){ return; }

    timer += Time.deltaTime;
    var progress = 1 - Mathf.Clamp01(animateInTime - timer);

    if(Mathf.Abs(timer - animateInTime) < lifeTime){
    for(var c = 0; c < cells.Length; c++){
        var cell = cells[c];

        var demoClick = beatManager.demoMode && timer > (c * 0.5f) + Random.Range(0.1f, 0.6f);

        if(!cell.hit && (input.GetTouchBeganOnCollider(cell.col) || demoClick)){
          cell.GetHit();
          MMVibrationManager.Haptic(HapticTypes.SoftImpact);

          var done = cells.All(cc => cc.hit);
          if(done && !processedSplit){
            Split();
            beatHitSignal.Dispatch(new BeatHitData(){ hit = true, beat = this, baseScore = cells.Length });
          }

          break;
        }
      }
    }

    //about to die
    if(!processedSplit && !processedDeath && timer > animateInTime + lifeTime - aboutToShrivelWarningTime){
      var saturationAmt = Utils.Remap(timer, animateInTime + lifeTime - aboutToShrivelWarningTime, animateInTime + lifeTime, 1, 0);

      //Desaturate colors
      foreach(var cell in cells){
        if(!cell.hit){
          cell.SetRendSaturation(saturationAmt);
        }
      }
    }

    //too late
    if(!processedSplit && !processedDeath && timer > animateInTime + lifeTime){
      Shrivel();
      beatHitSignal.Dispatch(new BeatHitData(){ hit = false, beat = this });
    }
  }

  void Split(){
    processedSplit = true;
    var quarterAnimTime = 0.25f * animateOutTime;

    foreach(var cell in cells){
      cell.SetRendSaturation(1f);
    }

    //set up the split
    var newBeatGO = Instantiate(singleBeatPrefab, transform.position, transform.rotation, transform.parent);
    var newBeat = newBeatGO.GetComponent<MultiBeat>();
    newBeat.isClone = true;
    newBeat.color = color;
    newBeat.points = points;
    newBeat.animateInTime = newBeat.animateOutTime = animateInTime;
    newBeat.beatTemplateItem = beatTemplateItem;
    newBeat.beatManager = beatManager;

    var majorAxis = (points[points.Length - 1] - points[0]).normalized;

    //shrink a bit
    LeanTween.scale(gameObject, Vector3.one * splitScaleSize, quarterAnimTime * 3).setEase(LeanTweenType.easeOutQuad);

    //move away from the other cell
    var moveTo = transform.position + (Vector3)majorAxis.PerpendicularRight() * moveApartAmt;
    LeanTween.move(gameObject, moveTo, quarterAnimTime * 2)
      .setDelay(quarterAnimTime)
      .setEase(LeanTweenType.easeOutCubic);

    //spawn bg cells
    for(var c = 0; c < cells.Length; c++){
      spawnBgCellSignal.Dispatch(beatTemplateItem.type, cells[c].transform.localPosition + moveTo, cells[c].transform.rotation, quarterAnimTime * 2);
    }

    //and finally go away
    FadeDisplay(animateOutTime, 0f);

    Die(animateOutTime);
  }

  //animations for split/cloned single cells
  void DoCloneThings(){
    var quarterAnimTime = 0.25f * animateOutTime;

    //make sure we're behind the original
    for(var c = 0; c < cells.Length; c++){
      cells[c].DoCloneThings();
    }

    //shrink a bit same as original
    LeanTween.scale(gameObject, Vector3.one * splitScaleSize, quarterAnimTime * 3).setEase(LeanTweenType.easeOutQuad);

    //move away from the other cell
    var majorAxis = (points[points.Length - 1] - points[0]).normalized;
    var moveTo = transform.position + (Vector3)majorAxis.PerpendicularLeft() * moveApartAmt;
    LeanTween.move(gameObject, moveTo, quarterAnimTime * 2)
      .setDelay(quarterAnimTime)
      .setEase(LeanTweenType.easeOutCubic);

    //spawn clone bg cells
    for(var c = 0; c < cells.Length; c++){
      spawnBgCellSignal.Dispatch(beatTemplateItem.type, cells[c].transform.localPosition + moveTo, cells[c].transform.rotation, quarterAnimTime * 2);
    }

    //then go away
    FadeDisplay(animateOutTime, 0f);

    Die(animateOutTime);
  }

  //Not successful
  void Shrivel(){
    processedDeath = true;

    foreach(var cell in cells){
      cell.SetRendSaturation(0f);
    }

    FadeDisplay(animateOutTime, 0f);

    LeanTween.scale(gameObject, Vector3.zero, animateOutTime).setEase(LeanTweenType.easeInQuad);

    Die(animateOutTime);
  }

  void FadeDisplay(float time, float delay){
    for(var c = 0; c < cells.Length; c++){
      var cellDelay = delay + (c * 0.05f);
      cells[c].FadeDisplay(time, cellDelay);
    }
  }

  void Die(float delay = 0f){
    for(var c = 0; c < cells.Length; c++){
      var cell = cells[c];
      LeanTween.cancel(cell.gameObject);
      objectPool.Recycle(cell.gameObject, delay);
    }
    Destroy(this.gameObject, delay + Constants.oneFrameTimeWorstCase);
  }

}
