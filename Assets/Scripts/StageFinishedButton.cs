using System;
using strange.extensions.mediation.impl;

public class StageFinishedButton : View {
  public ShinyButton button;
  public Action postClickAction;

  [Inject] StageTransitionModel stageData {get; set;}
  [Inject] StageTransitionService stageTransition { get; set; }

  bool showing = false;

  protected override void Awake () {
    base.Awake();

    button.onClick.AddListener(ClickStage);
  }

  void Update(){
    button.gameObject.SetActive(showing);
  }

  public void Activate(bool skipUnlock = false){
    if(!showing){
      showing = true;
      if(!skipUnlock){
        stageTransition.UnlockNextSubStage(stageData.activeStage);
      }
    }
  }

  //Show the button without unlocking the next substage
  public void Show(){
    showing = true;
  }

  public void Hide(){
    showing = false;
  }

  void ClickStage(){
    stageTransition.TransitionTo(stageData.activeStage);
    showing = false;
    if(postClickAction != null){
      postClickAction();
    }
  }

}
