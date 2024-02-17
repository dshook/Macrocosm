using TMPro;
using UnityEngine;
using strange.extensions.mediation.impl;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class TutorialSystem : View {

  [Inject] TutorialModel model {get; set;}

  [Inject] TimeService time { get; set; }
  [Inject] StageTransitionEndSignal transitionEnd { get; set; }
  [Inject] StageUnlockedSignal stageUnlockedSignal { get; set; }
  [Inject] SubStageUnlockedSignal subStageUnlockedSignal { get; set; }
  [Inject] StageTwoElementNeededSignal elementNeededSignal { get; set; }
  [Inject] BeatTutorialFinishedSignal beatTutorialFinishedSignal { get; set; }
  [Inject] OpenTutorialFinishedSignal openTutorialFinishedSignal { get; set; }
  [Inject] GameLoadedSignal gameLoadedSignal { get; set; }
  [Inject] SettingsDataModel settings { get; set; }
  [Inject] ResourceLoaderService loader { get; set; }
  [Inject] MenuToggledSignal menuToggleSignal {get; set;}
  [Inject] StatsModel statsModel { get; set; }
  [Inject] StageTransitionModel stageData {get; set;}

  public GameObject tutorialGO;
  public TutorialPopoutView popoutView;

  GameObject activeTutorial = null;
  bool menuOpen = false;

  List<int> tutorialQueue = new List<int>();

  float timeSinceLastStageUnlock = 0f;

  protected override void Awake () {
    base.Awake();
    transitionEnd.AddListener(TransitionEnd);

    stageUnlockedSignal.AddListener(OnStageUnlocked);
    subStageUnlockedSignal.AddListener(OnSubStageUnlocked);

    elementNeededSignal.AddListener(ShowAtomSatTutorial);
    beatTutorialFinishedSignal.AddListener(BeatTutorialDone);

    gameLoadedSignal.AddListener(OnLoad);
    menuToggleSignal.AddListener(OnMenuToggled);
  }

  void OnLoad(){
    if(model.tutorialsCompleted == null){
      model.tutorialsCompleted = new Dictionary<int, bool>();
    }

    if(model.activeTutorialIdx >= 0){
      //trick to get around the 2 tutorials at once check
      var toShow = model.activeTutorialIdx;
      model.activeTutorialIdx = -1;
      ShowTutorial(toShow);
    }
  }


  void Update () {
    //Check to show queued tutorials
    if(tutorialQueue.Count > 0 && Time.frameCount % 20 == 0){
      if(ShowTutorial(tutorialQueue[0])){
        tutorialQueue.RemoveAt(0);
      }
    }

    //reminder to go to next stage if unlocked
    timeSinceLastStageUnlock += Time.deltaTime;
    if(timeSinceLastStageUnlock > 30f ){
      for(int stage = 1; stage <= StageTransitionModel.lastStage; stage++){
        if(stageData.stagesUnlocked[stage] && statsModel.stageTime[stage] == 0){
          var message = visitMessages[stage];
          ShowPopoutTutorial(message, message);
        }
      }
    }
  }

  //This is dumb but prevents GC when the visit message needs to be checked
  private static Dictionary<int, string> visitMessages = new Dictionary<int, string>(){
    {1, "Remember to check out Stage 1!"},
    {2, "Remember to check out Stage 2!"},
    {3, "Remember to check out Stage 3!"},
    {4, "Remember to check out Stage 4!"},
    {5, "Remember to check out Stage 5!"},
    {6, "Remember to check out Stage 6!"},
    {7, "Remember to check out Stage 7!"},
  };

  public void CompleteOpenTutorial(){
    if(model.activeTutorialIdx < 0 || !model.tutorialsCompleted.ContainsKey(model.activeTutorialIdx) ){
      Debug.LogWarning("Trying to complete inactive tutorial. idx: " + model.activeTutorialIdx);
      return;
    }

    var tutorialIndex = model.activeTutorialIdx;
    model.tutorialsCompleted[model.activeTutorialIdx] = true;

    Destroy(activeTutorial);

    activeTutorial = null;
    model.activeTutorialIdx = -1;

    time.Resume();
    model.paused = false;
    openTutorialFinishedSignal.Dispatch(tutorialIndex);
  }

  public bool CompletedTutorial(int index){
    return model.tutorialsCompleted.ContainsKey(index) && model.tutorialsCompleted[index];
  }

  //Returns true if the tutorial was shown now
  public bool ShowTutorial(int index, float delay = 0f, bool disableQueue = false){
    //All the reasons we shouldn't show the tutorial at all
    if(settings.tutorialsDisabled || CompletedTutorial(index)){
      return false;
    }

    //Reasons we shouldn't show it right now, but could add it to the queue
    if(
      model.activeTutorialIdx != -1 || //can't show 2 tutorials at once
      menuOpen
    ){
      if(!disableQueue && !tutorialQueue.Contains(index)){
        tutorialQueue.Add(index);
      }
      return false;
    }

    model.activeTutorialIdx = index;

    if(delay == 0){
      ShowTutorialWork(index);
    }else{
      StartCoroutine(ShowTutorialWorkDelay(index, delay));
    }
    return true;
  }

  IEnumerator ShowTutorialWorkDelay(int index, float delay){
    yield return new WaitForSecondsRealtime(delay);

    //Double check menu is closed before showing a tutorial on delay
    //in case they opened it while the delay was going
    while(menuOpen){
      yield return new WaitForSecondsRealtime(delay);
    }

    ShowTutorialWork(index);
  }

  void ShowTutorialWork(int index){
    var tut = GetTutorialObject(index);
    if(tut == null){
      Debug.LogWarning("Invalid Tutorial Index: " + index);
      return;
    }

    time.Pause();
    model.paused = true;

    activeTutorial = tut;
    model.tutorialsCompleted[index] = false;
    activeTutorial.SetActive(true);
  }

  public void ShowPopoutTutorial(string tutorialKey, string txt){
    if(model.popoutTutorialsCompleted.ContainsKey(tutorialKey) && model.popoutTutorialsCompleted[tutorialKey]){
      return;
    }

    if(popoutView.Show(txt)){
      model.popoutTutorialsCompleted[tutorialKey] = true;
    }
  }


  GameObject GetTutorialObject(int index){
    var tutorialPrefab = loader.Load<GameObject>(tutorialPath(index), false);
    if(tutorialPrefab == null){
      return null;
    }

    var newTut = GameObject.Instantiate(tutorialPrefab, tutorialGO.transform, false);

    return newTut;
  }

  string tutorialPath(int index){
    return "Prefabs/Tutorials/" + index;
  }

  void TransitionEnd(StageTransitionData data)
  {
    switch(data.stage){
      case 1:
        switch(data.subStage){
          case 0:
            ShowTutorial(0);
            break;
          case 1:
            ShowTutorial(101);
            break;
        }
        break;
      case 2:
        switch(data.subStage){
          case 0:
            ShowTutorial(200, 0.1f);
            break;
          case 1:
            ShowTutorial(201);
            break;
        }
        break;
      case 3:
        switch(data.subStage){
          case 0:
            ShowTutorial(300, 0.1f);
            break;
          case 3:
            ShowTutorial(302, 0.1f);
            break;
        }
        break;
      case 4:
        switch(data.subStage){
          case 0:
            ShowTutorial(400, 0.1f);
            break;
        }
        break;
      case 5:
        switch(data.subStage){
          case 0:
            ShowTutorial(500, 0.1f);
            break;
        }
        break;
      case 6:
        switch(data.subStage){
          case 0:
            ShowTutorial(600, 0.1f);
            break;
        }
        break;
      case 7:
        switch(data.subStage){
          case 0:
            ShowTutorial(700, 0.1f);
            break;
        }
        break;
    }

    //Check for transitioning away from 6 to show tech continues tutorial
    if(data.previousActiveStage == 6 && !data.isLoad){
        ShowTutorial(607, 0.1f);
    }
  }

  public void OnStageUnlocked(StageUnlockedData stageUnlocked)
  {
    timeSinceLastStageUnlock = 0f;
    // switch(stageUnlocked.stage){
    //   case 2:
    //     ShowTutorial(100);
    //     break;
    //   case 3:
    //     ShowTutorial(209);
    //     break;
    //   case 4:
    //     ShowTutorial(309);
    //     break;
    // }
  }

  public void OnSubStageUnlocked(StageUnlockedData stageUnlocked)
  {
    if(stageUnlocked.stage == 1 && stageUnlocked.subStage == 5){
      ShowPopoutTutorial("1-switching", "You can switch between stages at any time!");
    }
  }

  void ShowAtomSatTutorial(int elementNeeded){
    const int tutorialId = 202;
    if(ShowTutorial(tutorialId, 0, true)){
      if(!activeTutorial){ return; }
      var text = activeTutorial.GetComponentInChildren<TMP_Text>(true);
      //TODO: should be popout tutorial?
      text.text = string.Format("There isn't much {0} in the universe yet.<br><br>Go back to Stage 1 to make more!", AtomRenderer.elementNames[elementNeeded]);
    }
  }

  void BeatTutorialDone(BeatTutorialData data){
    switch(data.subStage){
      case 0:
        ShowTutorial(301);
        break;
    }
  }

  // Assumes the tutorials for a stage are numbered with the 100's place, i.e. 700's for stage 7 tuts
  public void ResetStageTutorialComplete(int stage){
    var keysToChange = model.tutorialsCompleted.Keys.Where(key => (key / 100) == stage ).ToList();
    foreach(var key in keysToChange){
      model.tutorialsCompleted[key] = false;
    }
  }

  void OnMenuToggled(bool menuOpen){
    this.menuOpen = menuOpen;
  }
}
