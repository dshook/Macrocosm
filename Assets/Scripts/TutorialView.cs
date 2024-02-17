using UnityEngine;
using UnityEngine.UI;
using strange.extensions.mediation.impl;

public class TutorialView : View {

  [Inject] TutorialSystem tutorials { get; set; }
  [Inject] StageTransitionStartSignal transitionStart { get; set; }

  public Button okButton;
  public ShinyButton shinyOkButton;
  public bool useStageTransitionAsOk = false;

  protected override void Awake () {
    base.Awake();

    transitionStart.AddListener(OnTransitionStart);

    if(okButton != null){
      okButton.onClick.AddListener(() => tutorials.CompleteOpenTutorial());
    }
    if(shinyOkButton != null){
      shinyOkButton.onClick.AddListener(() => tutorials.CompleteOpenTutorial());
    }

    if(!useStageTransitionAsOk && okButton == null && shinyOkButton == null){
      Debug.LogWarning("No way for tutorial to complete!");
    }
  }

  void OnTransitionStart(StageTransitionData transitionData){
    if(useStageTransitionAsOk && transitionData.stage != transitionData.previousActiveStage){
      tutorials.CompleteOpenTutorial();
    }
  }

}
