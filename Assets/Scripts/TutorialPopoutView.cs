using UnityEngine;
using UnityEngine.UI;
using strange.extensions.mediation.impl;
using UnityEngine.EventSystems;
using TMPro;

public class TutorialPopoutView : View, IPointerClickHandler {

  [Inject] TutorialSystem tutorials { get; set; }

  public float popInDistanceX;
  public float animateTime = 0.75f;
  public float maxDisplayTime = 7f;

  public GameObject popupGO;
  public TMP_Text bodyText;

  float showTime = 0f;
  bool showing = false;
  RectTransform rectTransform;
  float startXPos;

  protected override void Awake () {
    base.Awake();

    rectTransform = popupGO.GetComponent<RectTransform>();
    startXPos = rectTransform.anchoredPosition.x;
  }

  void Update(){
    if(!showing) return;

    showTime += Time.unscaledDeltaTime;

    if(showTime > maxDisplayTime){
      Hide();
    }
  }

  public bool Show(string txt){
    if(showing) return false;

    popupGO.SetActive(true);
    bodyText.text = txt;
    LeanTween.value(popupGO, TweenPosition, startXPos, startXPos + popInDistanceX, animateTime)
      .setEase(LeanTweenType.easeOutBack)
      .setIgnoreTimeScale(true);
    showing = true;
    showTime = 0f;

    return true;
  }

  public void Hide(){
    if(!showing) return;

    LeanTween.value(popupGO, TweenPosition, rectTransform.anchoredPosition.x, startXPos, animateTime)
      .setEase(LeanTweenType.easeOutBack).setOnComplete(FinishHiding)
      .setIgnoreTimeScale(true);
    showing = false;
  }

  void FinishHiding(){
    if(!showing){
      popupGO.SetActive(false);
    }
  }

  void TweenPosition(float newValue){
    rectTransform.anchoredPosition = new Vector2(newValue, rectTransform.anchoredPosition.y);
  }

  public void OnPointerClick(PointerEventData eventData)
  {
    Hide();
  }

}
