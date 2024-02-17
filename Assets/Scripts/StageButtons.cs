using TMPro;
using UnityEngine;
using strange.extensions.mediation.impl;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class StageButtons : View {
  public GameObject stageButtonPrefab;

  [Inject] StageTransitionModel stageData {get; set;}
  [Inject] StageTransitionService stageTransition { get; set; }
  [Inject] StageUnlockedSignal stageUnlockedSignal { get; set; }
  [Inject] StageTransitionStartSignal stageTransitionStart { get; set; }
  [Inject] StageTransitionEndSignal stageTransitionEnd { get; set; }
  [Inject] GameLoadedSignal gameLoaded { get; set; }

  Dictionary<int, ButtonData> stageButtons = new Dictionary<int, ButtonData>();

  class ButtonData {
    public ShinyButton button;
    public bool interactable;
    public bool isSelected;
  }

  protected override void Awake () {
    base.Awake();
    stageUnlockedSignal.AddListener(OnStageUnlocked);
    stageTransitionStart.AddListener(TransitionStarted);
    stageTransitionEnd.AddListener(TransitionEnded);
    gameLoaded.AddListener(OnGameLoaded);

    for(int c = 0; c < transform.childCount; c++){
      var buttonChild = transform.GetChild(c);

      var button = buttonChild.GetComponent<ShinyButton>();
      int buttonStage = int.Parse(buttonChild.name);
      button.onClick.AddListener(() => ClickStage(buttonStage));
      stageButtons[buttonStage] = new ButtonData(){
        button = button,
        interactable = false,
        isSelected = false,
      };

      //hide the button so they can be revealed when the game loads up
      button.gameObject.SetActive(false);
    }

  }

  void OnGameLoaded(){
    for(int stage = 1; stage <= StageTransitionModel.lastStage; stage++){
      stageButtons[stage].interactable = stageData.stagesUnlocked.Length > stage && stageData.stagesUnlocked[stage];
      stageButtons[stage].isSelected = stage == stageTransition.stageData.activeStage;

      UpdateButtonDisplay(stage);

    }

    StartCoroutine(RevealButtons());
  }

  //Have to do this nonsense to get TMP to not render white squares for the text
  //In the buttons when the scale is set to 0 for the tween in
  IEnumerator RevealButtons(){
    for(int stage = 1; stage <= StageTransitionModel.lastStage; stage++){
      var go = stageButtons[stage].button.gameObject;
      go.SetActive(true);
    }
    yield return new WaitForSecondsRealtime(0.2f);
    for(int stage = 1; stage <= StageTransitionModel.lastStage; stage++){
      RevealButton(stage, false);
    }
  }

  public void OnStageUnlocked(StageUnlockedData stageUnlocked)
  {
    var stageProgressedTo = stageUnlocked.stage;

    //when a new stage comes online, PUNCH IT!
    if(!stageButtons[stageProgressedTo].interactable){
      stageButtons[stageProgressedTo].interactable = true;

      UpdateButtonDisplay(stageProgressedTo);
    }

  }

  void RevealButton(int stage, bool skipDelay){
    var go = stageButtons[stage].button.gameObject;
    go.transform.localScale = Vector3.zero;
    var delay = skipDelay ? 0f : stage * 0.10f + 0.9f;
    LeanTween.scale(go, Vector3.one, 0.6f)
      .setEase(LeanTweenType.easeOutBack)
      .setDelay(delay)
      .setIgnoreTimeScale(true);
  }

  void ClickStage(int stageNumber){
    stageButtons[stageTransition.stageData.activeStage].isSelected = false;
    stageButtons[stageNumber].isSelected = true;

    UpdateButtonDisplay(stageTransition.stageData.activeStage);
    UpdateButtonDisplay(stageNumber);
    stageTransition.TransitionTo(stageNumber);
  }

  void TransitionStarted(StageTransitionData data){
    // CleanupTweens();
  }

  void TransitionEnded(StageTransitionData data){
    //Make sure the right stage buttons state is preserved after the transition, eg. if the stage transition changed for demo build
    foreach(var stageButton in stageButtons){
      stageButton.Value.isSelected = stageButton.Key == data.stage;
      UpdateButtonDisplay(stageButton.Key);
    }
  }

  void UpdateButtonDisplay(int stageNumber){
    var buttonData = stageButtons[stageNumber];

    stageButtons[stageNumber].button.interactable = buttonData.interactable;
    stageButtons[stageNumber].button.isSelected = buttonData.isSelected;
    stageButtons[stageNumber].button.color = buttonData.isSelected ? UIColor.Green :
      (buttonData.interactable ? UIColor.DarkPurple : UIColor.Gray);
  }

}
