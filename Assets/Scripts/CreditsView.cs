using UnityEngine;
using UnityEngine.Events;
using strange.extensions.mediation.impl;

public class CreditsView : View {

  public GameObject display;
  public ShinyButton closeButton;

  public ScrollCredits scrollCredits;

  public UnityEvent onClose = new UnityEvent();

  protected override void Awake () {
    base.Awake();

    closeButton.onClick.AddListener(() => onClose.Invoke());
  }

  public void Update()
  {
    if(scrollCredits.IsFinishedScrolling){
      onClose.Invoke();
    }
  }

}
