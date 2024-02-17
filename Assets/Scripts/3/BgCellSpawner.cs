using System.Collections;
using strange.extensions.mediation.impl;
using UnityEngine;

public class BgCellSpawner : View {

  [Inject] SpawnService spawner {get; set;}
  [Inject] SpawnBgCellSignal spawnBgCellSignal {get; set;}
  [Inject] ObjectPool objectPool {get; set;}
  [Inject] AudioService audioService {get; set;}

  public GameObject bgCellPrefab;
  public BeatManager beatManager;

  public Sprite singleBgSprite;
  public Sprite doubleBgSprite;
  public Sprite multiBgSprite;
  public Sprite slideBgSprite;

  public float cleanupAnimateTime = 2f;

  protected override void Awake () {
    base.Awake();

    spawnBgCellSignal.AddListener(OnSpawnCell);
    objectPool.CreatePool(bgCellPrefab, 0);
  }

  public void Init(){
  }

  protected override void OnDestroy(){
    spawnBgCellSignal.RemoveListener(OnSpawnCell);
  }

  public void Cleanup(){

    LeanTween.moveLocal(this.gameObject, this.transform.localPosition.AddX(10f), cleanupAnimateTime).setEase(LeanTweenType.easeInOutCubic);

    StartCoroutine(FinishCleanup());
  }

  IEnumerator FinishCleanup(){
    yield return new WaitForSeconds(cleanupAnimateTime);

    this.gameObject.transform.localPosition = Vector3.zero;

    if(objectPool != null){
      objectPool.RecycleAll(bgCellPrefab);
    }else{
      this.transform.DestroyChildren();
    }
  }

  void OnSpawnCell(BeatType type, Vector2 pos, Quaternion rotation, float delay){
    var beat = objectPool.Spawn(
      bgCellPrefab,
      Vector3.zero,
      rotation
    );
    beat.transform.SetParent(this.transform, true);
    beat.transform.position = pos;
    var animInTime = 0.5f;

    var rend = beat.GetComponentInChildren<SpriteRenderer>();
    //hax but also lazy to get a proper data structure here
    switch(type){
      case BeatType.Single:
        rend.sprite = singleBgSprite;
        break;
      case BeatType.Double:
        rend.sprite = doubleBgSprite;
        break;
      case BeatType.Multi:
        rend.sprite = multiBgSprite;
        animInTime = 0.25f;
        break;
      case BeatType.Slide:
      case BeatType.SlideReverse:
        rend.sprite = slideBgSprite;
        break;
      default:
        Debug.LogWarning("Unknown beat type for bg cell: " + type);
        break;
    }

    beat.transform.localScale = Vector3.zero;
    LeanTween.scale(beat, Vector3.one, animInTime).setDelay(delay);
  }

}
