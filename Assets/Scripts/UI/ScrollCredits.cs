using UnityEngine;
using strange.extensions.mediation.impl;

public class ScrollCredits : View {
  [Inject] InputService input {get; set;}

  public float delay = 4f;
  public float scrollSpeed;
  public float clickScrollSpeedBonus;
  public GameObject textToScroll;

  float textHeight = 0;
  float scrollPos = 0;
  RectTransform textRt;

  float timer = 0f;

  protected override void Start()
  {
    base.Start();

    textRt = textToScroll.GetComponent<RectTransform>();
    textHeight = textRt.rect.height;
  }

  public bool IsFinishedScrolling {
    get{
      return scrollPos >= textRt.rect.height;
    }
  }

  public void Update()
  {
    timer += Time.unscaledDeltaTime;

    float scrollRamp = 1f;
    if(timer < delay){
      scrollRamp = Mathf.Clamp01( LeanTween.easeInSine(0, 1, timer / delay) );
    }

    var speedBonus = 0f;
    if(input.ButtonIsDown(InputService.defaultButton, false)){
      speedBonus = clickScrollSpeedBonus;
    }

    if(!IsFinishedScrolling){
      scrollPos = scrollPos + (scrollRamp * (scrollSpeed + speedBonus) * Time.unscaledDeltaTime);

      textRt.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, -scrollPos, textRt.rect.height);
    }
  }
}