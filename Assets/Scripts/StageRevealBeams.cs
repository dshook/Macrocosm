using UnityEngine;
using strange.extensions.mediation.impl;

public class StageRevealBeams : View {

  [Inject] StageUnlockedSignal stageUnlockedSignal { get; set; }

  public GameObject display;
  public GameObject rotationTransform;
  public Transform stageButtonHolder;
  public float animationTime = 2f;
  public float rotationSpeed = 30f;

  bool animating = false;
  float timeAnimating = 0f;

  protected override void Awake () {
    base.Awake();
    stageUnlockedSignal.AddListener(OnStageUnlocked);

    display.SetActive(false);
  }

  void Update(){
    if(!animating){ return; }

    rotationTransform.transform.rotation = Quaternion.Euler(
      rotationTransform.transform.rotation.eulerAngles.AddZ(rotationSpeed * Time.unscaledDeltaTime)
    );

    timeAnimating += Time.unscaledDeltaTime;
    if(timeAnimating > animationTime){
      animating = false;
      display.SetActive(false);
    }
  }

  public void OnStageUnlocked(StageUnlockedData stageUnlocked)
  {
    animating = true;
    timeAnimating = 0f;

    var displayRT = display.GetComponent<RectTransform>();

    var stageButton = stageButtonHolder.GetChild(stageUnlocked.stage - 1);
    var stageButtonRT = stageButton.GetComponent<RectTransform>();

    // display.transform.position = stageButton.position;
    displayRT.anchoredPosition = stageButtonRT.anchoredPosition;

    display.SetActive(true);

    rotationTransform.transform.localScale = Vector3.zero;

    LeanTween.scale(rotationTransform, Vector3.one, animationTime * 0.75f)
      .setEase(LeanTweenType.easeOutExpo)
      .setIgnoreTimeScale(true);

    LeanTween.scale(rotationTransform, Vector3.zero, animationTime * 0.25f)
      .setDelay(animationTime * 0.75f)
      .setEase(LeanTweenType.easeOutExpo)
      .setIgnoreTimeScale(true);

  }


}
