using UnityEngine;
using strange.extensions.mediation.impl;
using System;
using TMPro;

public class DemoSystem : View {

  [Inject] DemoDataModel demoData {get; set;}
  [Inject] StageTransitionModel stageData {get; set;}
  [Inject] StageTransitionService stageTransition { get; set; }
  [Inject] StageUnlockedSignal stageUnlockedSignal { get; set; }
  [Inject] GameLoadedSignal gameLoaded { get; set; }
  [Inject] ResourceLoaderService loader { get; set; }
  [Inject] AudioService audioService {get; set;}

  public Transform modalParent;
  public AudioClip unlockSound;
  public AudioClip noUnlockSound;

  bool showedDemoModal = false;

  protected override void Awake () {
    base.Awake();

    //Only applies to demo builds
#if !DEMO
    Destroy(this);
    return;
#endif

//Disable unreachable code warning with conditional compile
#pragma warning disable CS0162
    stageUnlockedSignal.AddListener(OnStageUnlocked);
    gameLoaded.AddListener(OnGameLoaded);
#pragma warning restore

  }

  void Update () {

  }

  void OnStageUnlocked(StageUnlockedData stageUnlocked)
  {
    if(stageUnlocked.stage >= Constants.MAX_DEMO_STAGE){
      demoData.needsDemoUnlock = true;
    }
    CheckShowDemoModal();
  }

  void OnGameLoaded(){
    if(stageData.stagesUnlocked[Constants.MAX_DEMO_STAGE + 1]){
      demoData.needsDemoUnlock = true;
    }
    CheckShowDemoModal();
  }

  TMP_InputField codeInput;
  GameObject demoUnlockModal;

  void CheckShowDemoModal(){
    var showModal = demoData.needsDemoUnlock && !demoData.demoUnlocked && !showedDemoModal;

    if(showModal){
      Debug.LogWarning("Need to unlock demo!");
      var modalPrefab = loader.Load<GameObject>("Prefabs/DemoUnlockModal", false);
      demoUnlockModal = GameObject.Instantiate(modalPrefab, modalParent, false);

      var shinyButton = demoUnlockModal.GetComponentInChildren<ShinyButton>();
      codeInput = demoUnlockModal.GetComponentInChildren<TMP_InputField>();

      shinyButton.onClick.AddListener(ClickModalButton);
    }
  }

  void ClickModalButton(){
    if(codeInput != null && !string.IsNullOrEmpty(codeInput.text) && codeInput.text.ToLowerInvariant() == "ultimate skill macrocosm"){
      demoData.demoUnlocked = true;
      audioService.PlaySfx(unlockSound);
      stageTransition.TransitionTo(Constants.MAX_DEMO_STAGE + 1);
    }else{
      audioService.PlaySfx(noUnlockSound);
    }

    Destroy(demoUnlockModal);
  }

}