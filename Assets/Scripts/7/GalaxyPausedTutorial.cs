using UnityEngine;
using strange.extensions.mediation.impl;

public class GalaxyPausedTutorial : View {

  [Inject] TutorialSystem tutorialSystem {get; set;}
  [Inject] StageSevenDataModel stageSevenData {get; set;}

  public float pausedTimeBeforeTutorial = 8f;

  float timer = 0f;

  void Update () {
    if(stageSevenData.viewMode == GalaxyViewMode.Galaxy && stageSevenData.timeRate == 0){
      timer += Time.unscaledDeltaTime;

      if(timer > pausedTimeBeforeTutorial){
        tutorialSystem.ShowPopoutTutorial("7-paused-reminder", "Remember to use the clock to unpause!");
      }
    }
  }
}