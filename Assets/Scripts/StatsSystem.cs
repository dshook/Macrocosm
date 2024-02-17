using UnityEngine;
using strange.extensions.mediation.impl;
using System;
using System.Collections.Generic;

public class StatsSystem : View {

  [Inject] public StatsModel statsModel {get; set;}
  [Inject] public StageTransitionModel stageData {get; set;}
  [Inject] StageUnlockedSignal stageUnlockedSignal { get; set; }
  [Inject] LearningMoreSignal learningMore {get; set;}
  [Inject] GameLoadedSignal gameLoaded { get; set; }
  [Inject] StageTransitionStartSignal transitionStart { get; set; }

  bool appHasFocus = true;

  protected override void Awake () {
    base.Awake();

    stageUnlockedSignal.AddListener(OnStageUnlocked);
    learningMore.AddListener(OnLearningMore);
    gameLoaded.AddListener(OnGameLoaded);
    transitionStart.AddListener(OnTransitionStart);

  }

  void Update () {
    //Only count app focused time
    if(!appHasFocus){ return; }

    statsModel.totalPlayTime += Time.unscaledDeltaTime;
    statsModel.sessionTime += Time.unscaledDeltaTime;
    statsModel.stageTime[stageData.activeStage] += Time.unscaledDeltaTime;

    if(Input.GetButtonDown(InputService.defaultButton)){
      statsModel.tapCount[stageData.activeStage] += 1;
    }
  }

  void OnStageUnlocked(StageUnlockedData stageUnlocked)
  {
    var stageProgressedTo = stageUnlocked.stage;

    statsModel.stageUnlockedTime[stageProgressedTo] = statsModel.totalPlayTime;
  }

  void OnLearningMore(){

    statsModel.learnMoreCount[stageData.activeStage] += 1;
  }

  void OnApplicationFocus(bool hasFocus)
  {
    appHasFocus = hasFocus;

    statsModel.sessionTime = 0;
  }

  void OnTransitionStart(StageTransitionData transitionData){
    statsModel.sessionStageSwitches.Add(transitionData.stage);
  }


  bool addedStartupTime = false;

  void OnGameLoaded(){
    if(!addedStartupTime){
      //Add in the datetime when the game starts up.  Loading should only happen once but while debugging it happens more
      statsModel.gameStartDates.Add(DateTime.UtcNow.ToString("o"));
      addedStartupTime = true;
    }
    statsModel.sessionTime = 0;

    statsModel.sessionStageSwitches = new List<int>();
  }

}