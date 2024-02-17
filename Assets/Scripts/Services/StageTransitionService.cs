using System.Collections.Generic;
using UnityEngine;
using System;
using strange.extensions.mediation.impl;
using System.Collections;


public class StageTransitionService : View
{
  [Inject] public StageTransitionModel stageData {get; set;}
  [Inject] SpawnService spawner {get; set;}
  [Inject] ResourceLoaderService loader {get; set;}
  [Inject] StageRulesService stageRules { get; set; }
  [Inject] SubStageUnlockedSignal substageUnlockedSignal { get; set; }
  [Inject] StageUnlockedSignal stageUnlockedSignal { get; set; }
  [Inject] StageTransitionStartSignal transitionStart { get; set; }
  [Inject] StageTransitionEndSignal transitionEnd { get; set; }
  [Inject] TimeService time { get; set; }
  [Inject] AudioService audioService {get; set;}
  [Inject] DemoDataModel demoData {get; set;}
  [Inject] StringChanger stringChanger {get; set;}

  public GameObject[] searchObjects;
  public GameObject stageManagersGo;

  public AudioClip stageUnlockedSound;
  public StageOneBigText stageUnlockedText;

  public class MiniStage
  {
    public int stage;
    public int subStage;
  }

  Dictionary<int, bool> stagesSetup = new Dictionary<int, bool>();
  Dictionary<int, IStageManager> stageManagers;

  protected override void Awake () {
    base.Awake();

    //init dicts
    for(int i = 1; i <= StageTransitionModel.lastStage; i++){
      stagesSetup[i] = false;
    }

    //find stage managers
    stageManagers = new Dictionary<int, IStageManager>(){
      {1, stageManagersGo.transform.Find("1").GetComponent<StageOneManager>()},
      {2, stageManagersGo.transform.Find("2").GetComponent<StageTwoManager>()},
      {3, stageManagersGo.transform.Find("3").GetComponent<StageThreeManager>()},
      {4, stageManagersGo.transform.Find("4").GetComponent<StageFourManager>()},
      {5, stageManagersGo.transform.Find("5").GetComponent<StageFiveManager>()},
      {6, stageManagersGo.transform.Find("6").GetComponent<StageSixManager>()},
      {7, stageManagersGo.transform.Find("7").GetComponent<StageSevenManager>()},
    };

    //Make sure all stage managers are awake so they can register signals
    for(var i = 0; i < stageManagersGo.transform.childCount; i++){
      stageManagersGo.transform.GetChild(i).gameObject.SetActive(true);
    }
  }

  public void Init(){
    FindActiveStage();
    TransitionTo(stageData.activeStage);
  }

  //Transition to the highest unlocked subStage for this stage,
  //or if it's a load, the last active substage
  public void TransitionTo(int stage, bool isLoad = false, int? previousActiveStageOverride = null)
  {
#if DEMO
    if(stage > Constants.MAX_DEMO_STAGE && !demoData.demoUnlocked){
      stage = Constants.MAX_DEMO_STAGE;
    }
#endif
    if(stage < 1 || stage > StageTransitionModel.lastStage){
      Debug.LogError("Trying to transition to invalid stage: " + stage);
      return;
    }

    Debug.Log("Start Transition To: " + stage);

    var previousActiveStage = previousActiveStageOverride != null ? previousActiveStageOverride.Value : stageData.activeStage;
    var previousActiveSubStage = stageData.activeSubStage[stage];
    var subStage = stageData.stageProgression[stage];
    if(isLoad){
      subStage = stageData.activeSubStage[stage];
    }

    if(stageManagers.ContainsKey(previousActiveStage) && !isLoad && previousActiveStage != stage){
      stageManagers[previousActiveStage].OnTransitionAway(false);
    }

    SetGameObjectsActive(stage);

    //Set the new substage as the active one so init can use it
    stageData.activeSubStage[stage] = subStage;

    //Only init if the stage isn't setup or we're going to a different substage of the stame stage
    if(!stagesSetup[stage] || previousActiveSubStage != subStage){
      var isInitialInit = !stagesSetup[stage];
      Debug.Log(String.Format("Init: {0} initial: {1}", stage, isInitialInit));
      stageManagers[stage].Init(isInitialInit);
      stagesSetup[stage] = true;
    }

    var transitionData = new StageTransitionData(){
      stage = stage,
      subStage = subStage,
      previousActiveStage = previousActiveStage,
      previousActiveSubStage = previousActiveSubStage,
      isLoad = isLoad
    };
    transitionStart.Dispatch(transitionData);
    // StartCoroutine(Transition(0.05f, transitionData));
    FinishTransitionTo(transitionData);
  }

  IEnumerator Transition(float delay, StageTransitionData data) {
    yield return new WaitForSecondsRealtime(delay);

    FinishTransitionTo(data);
  }

  public void FinishTransitionTo(StageTransitionData data){
    stageData.activeStage = data.stage;
    stageData.activeSubStage[data.stage] = data.subStage;

    Debug.Log($"Finish Transition to Stage: {data.stage} sub {data.subStage}");

    DefaultTransitionTo();
    stageManagers[data.stage].OnTransitionTo(data);

    transitionEnd.Dispatch(data);
  }

  public void UnlockNextSubStage(int stage){
    stageData.stageProgression[stage]++;

    substageUnlockedSignal.Dispatch(new StageUnlockedData(){stage = stage, subStage = stageData.stageProgression[stage]});

    //see if this qualifies as a next stage unlock
    if(stageRules.stageUnlockData.Length > stage && stageData.stageProgression[stage] > stageRules.stageUnlockData[stage + 1]){
      UnlockNextStage(stage + 1);
    }
  }

  public void UnlockNextStage(int stage){

    //Make sure the stage progression for the previous stage is up to where it should be to have this stage unlocked
    //This shouldn't ever do anything in normal playthrough, only when using the stage cheats
    stageData.stageProgression[stage - 1] = Mathf.Max(stageData.stageProgression[stage - 1], stageRules.stageUnlockData[stage]);

    //Skip if already unlocked for whatever reason
    if(stageData.stagesUnlocked[stage]){
      return;
    }

    stageData.stagesUnlocked[stage] = true;

    Debug.Log("Unlocking stage: " + stage);
    stageUnlockedSignal.Dispatch(new StageUnlockedData(){stage = stage});

    audioService.PlaySfx(stageUnlockedSound);
    stageUnlockedText.Show("Stage " + stage, "UNLOCKED!", 5f, true);
  }

  public void SetGameObjectsActive(int stage){
    for(int i = 0; i < searchObjects.Length; i++){
      var searchObject = searchObjects[i];
      for(int j = 0; j < searchObject.transform.childCount; j++){
        var child = searchObject.transform.GetChild(j);
        int childNum = -1;
        if(!Int32.TryParse(child.name, out childNum)){
          continue;
        }

        if(childNum == stage){
          child.gameObject.SetActive(true);
        }else{
          child.gameObject.SetActive(false);
        }
      }
    }
  }

  void DefaultTransitionTo(){
    Physics.SyncTransforms();
    time.SetTimescale(TimeService.normalTimeScale);
  }

  public void ResetStage(int stage){
    stageManagers[stage].Cleanup();
    stagesSetup[stage] = false;
  }

  public void ResetAllStages(){
    foreach(var key in stageManagers.Keys){
      ResetStage(key);
    }
    stringChanger.ClearAll();
    loader.FreeAll();
  }

  //for setup at the beginning when switching stages in the editor
  void FindActiveStage(){
    int activeStage = 1;
    for(int i = 0; i < stageManagersGo.transform.childCount; i++){
      if(stageManagersGo.transform.GetChild(i).gameObject.activeSelf){
          Int32.TryParse(stageManagersGo.transform.GetChild(i).name, out activeStage);
      }
    }

    //find the last stage progression with that stage
    stageData.activeStage = activeStage;
    stageData.activeSubStage[activeStage] = stageData.stageProgression[activeStage];
  }

  public void TransitionToMenu(){
    if(stageManagers.ContainsKey(stageData.activeStage)){
      stageManagers[stageData.activeStage].OnTransitionAway(true);
    }

    // At one point I was deactivating the current stage to "handle menu pausing better" but
    // This has the unfortunate side effect of resetting all objects that use the object pool
    // And reset OnEnable.  Did some quick testing and everything seems fine with pausing and unpausing
    // Without deactivating the whole stage so we'll see....
    // SetGameObjectsActive(0);
  }

  public void TransitionFromMenu(){
    // SetGameObjectsActive(stageData.activeStage);
    if(stageManagers.ContainsKey(stageData.activeStage)){
      var transitionData = new StageTransitionData(){
        stage = stageData.activeStage,
        subStage = stageData.activeSubStage[stageData.activeStage],
        previousActiveStage = -1,
        previousActiveSubStage = -1,
        isLoad = false,
        fromMenu = true,
      };
      stageManagers[stageData.activeStage].OnTransitionTo(transitionData);
    }
  }
}

