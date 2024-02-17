using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using strange.extensions.mediation.impl;
using System.Linq;

public class EditorCheats : View
{

  public GameObject holder;
  public Button nextSubStageButton;
  public Button nextStageButton;
  public Button resetButton;
  public Button resetStageButton;

  [Inject] StageTransitionModel stageData { get; set; }
  [Inject] StageTransitionService stageTransition { get; set; }
  [Inject] StageFourDataModel stage4Data {get; set;}
  [Inject] GameSaverService gameSaver { get; set; }
  [Inject] FloatingText floatingNumbers { get; set; }
  [Inject] TimeService time { get; set; }
  [Inject] ToggleMenuVisibilitySignal toggleMenuVisibilitySignal {get; set;}
  [Inject] MenuToggledSignal menuToggleSignal {get; set;}
  [Inject] TutorialSystem tutorialSystem {get; set;}
  [Inject] VictorySignal victorySignal {get; set;}

  Action onMenuClose = null;

  protected override void Awake()
  {
    base.Awake();
    if (!Debug.isDebugBuild)
    {
      Destroy(this.gameObject);
      Destroy(holder);
    }

    menuToggleSignal.AddListener(OnMenuToggle);

    nextSubStageButton.onClick.AddListener(() =>
    {
      Debug.Log("CHEAT! Next sub stage for " + stageData.activeStage);
      stageData.usedCheat = true;

      if(stageData.activeStage == 4){
        stage4Data.creatureLives++;
      }else{
        stageTransition.UnlockNextSubStage(stageData.activeStage);
        stageTransition.TransitionTo(stageData.activeStage);
      }
    });

    nextStageButton.onClick.AddListener(() =>
    {
      stageData.usedCheat = true;

      //Check for victory unlocked first if all stages are already unlocked
      if(stageData.stagesUnlocked[stageData.stagesUnlocked.Length - 1]){
        Debug.Log("CHEAT! A hollow victory");

        victorySignal.Dispatch(true);
        toggleMenuVisibilitySignal.Dispatch();
        return;
      }

      int nextStageToUnlock = 2;
      for (int i = nextStageToUnlock; i < stageData.stagesUnlocked.Length; i++)
      {
        if (!stageData.stagesUnlocked[i])
        {
          nextStageToUnlock = i;
          break;
        }
      }
      Debug.Log("CHEAT! Next stage for " + nextStageToUnlock);
      stageTransition.UnlockNextStage(nextStageToUnlock);
    });

    resetButton.onClick.AddListener(() =>
    {
      Debug.Log("Reset all!");
      var activeStage = stageData.activeStage;
      for (int i = 0; i < stageData.stageProgression.Length; i++)
      {
        stageData.stageProgression[i] = 0;
        if (i >= 1)
        {
          stageData.stagesUnlocked[i] = false;
          stageTransition.ResetStage(i);
        }
      }

      gameSaver.ResetData();
      //have to pass in the previous active stage here so that the onTransitionAway will get called on the right stage
      //since game saver reset wipes it
      onMenuClose = () => stageTransition.TransitionTo(1, false, activeStage);
    });

    resetStageButton.onClick.AddListener(() =>
    {
      Debug.Log("CHEAT! Reset stage");
      stageData.usedCheat = true;
      stageData.stageProgression[stageData.activeStage] = 0;
      stageData.activeSubStage[stageData.activeStage] = 0;
      gameSaver.ResetStageData(stageData.activeStage);
      tutorialSystem.ResetStageTutorialComplete(stageData.activeStage);
      stageTransition.ResetStage(stageData.activeStage);

      onMenuClose = () => stageTransition.TransitionTo(stageData.activeStage, true);
    });
  }

  void Update()
  {
    if (
      Input.GetKeyDown(KeyCode.Plus) ||
      Input.GetKeyDown(KeyCode.KeypadPlus) ||
      Input.GetKeyDown(KeyCode.RightBracket)
    ){
      time.ChangeTimescale(0.1f);
      floatingNumbers.CreateUI("TS: " + Math.Round(Time.timeScale, 1, MidpointRounding.AwayFromZero), color: Color.white, false);
    }
    if (
      (Input.GetKeyDown(KeyCode.Minus) ||
       Input.GetKeyDown(KeyCode.KeypadMinus) ||
       Input.GetKeyDown(KeyCode.LeftBracket)
      )
      && Time.timeScale >= 0.1f
    ){
      time.ChangeTimescale(-0.1f);
      floatingNumbers.CreateUI("TS: " + Math.Round(Time.timeScale, 1, MidpointRounding.AwayFromZero), color: Color.white, false);
    }

  }

  void OnMenuToggle(bool newState){
    //Run final reset stuff after the menu closes so we don't transition back to the stage while its open
    if(!newState){
      if(onMenuClose != null){
        onMenuClose();
        onMenuClose = null;
      }
    }
  }
}
