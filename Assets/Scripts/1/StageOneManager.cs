using TMPro;
using UnityEngine;
using strange.extensions.mediation.impl;
using System.Collections.Generic;
using System.Linq;
using PygmyMonkey.ColorPalette;
using MoreMountains.NiceVibrations;
using System.Collections;

public class StageOneManager : View, IStageManager {

  public GameObject atomPrefab;
  public TextMeshProUGUI goalText;
  public TextMeshProUGUI maxSizeText;
  public AtomRenderer goalAtomRenderer;
  public AtomRenderer maxSizeAtomRenderer;
  public GameObject particleHolder;
  public GameObject colliderHolder;
  public KillOutsideBounds killOutsideBounds;
  public StageOneBigText warningText;
  public StageOneBigText successText;
  public float spawnDelay = 0.05f;
  public float transitionTime = 2f;
  public RectTransform stageRectTransform;
  public GradientColor backgroundGradient;

  public AudioClip substageCompleteSound;
  public AudioClip sunquakeSound;

  public AudioClip music;

  public float substageTransitionAnimateTime = 2.5f;

  float spawnTimer = 0f;

  float shakeupTimer = 0f;
  float shakeupChosenTime = 0f;

  float transitionTimer = 0f;

  //for recreating the saved atoms
  int savedAtomSizeIndex = 0;
  bool finishedSubStage = false;

  HashSet<Atom> atomSet = new HashSet<Atom>();

  [Inject] SpawnService spawner {get; set;}
  [Inject] ResourceLoaderService loader {get; set;}
  [Inject] StageTransitionService stageTransition { get; set; }
  [Inject] StageTransitionModel stageTransitionData { get; set; }
  [Inject] StageRulesService stageRules { get; set; }
  [Inject] StageOneDataModel stageOneData { get; set; }
  [Inject] AtomCombinedSignal atomCombinedSignal { get; set; }
  [Inject] AtomDestroyedSignal atomDestroyedSignal { get; set; }
  [Inject] AtomCreatedSignal atomCreatedSignal { get; set; }
  [Inject] GameSavingSignal gameSavingSignal { get; set; }
  [Inject] FloatingText floatingText { get; set; }
  [Inject] ObjectPool objectPool {get; set;}
  [Inject] AudioService audioService {get; set;}
  [Inject] TutorialSystem tutorialSystem {get; set;}

  [Inject] SubStageUnlockedSignal substageUnlockedSignal { get; set; }
  [Inject] StageTwoDataModel stageTwoData { get; set; }
  [Inject] MetaGameDataModel metaGameData {get; set;}
  [Inject] StringChanger stringChanger {get; set;}
  [Inject] TraumaModel trauma { get; set; }

  ColorPalette backgroundPalette;
  const int maxParticles = 90;

  protected override void Awake () {
    base.Awake();

    gameSavingSignal.AddListener(OnSaving);
    substageUnlockedSignal.AddListener(OnSubStageUnlocked);
    atomCombinedSignal.AddListener(OnAtomRemoved);
    atomDestroyedSignal.AddListener(OnAtomRemoved);
    atomCreatedSignal.AddListener(OnAtomCreated);


    backgroundPalette = ColorPaletteData.Singleton.fromName("Stage 1 Background");

    objectPool.CreatePool(atomPrefab, 0);
  }


  public void Init(bool isInitialCall){
    spawnTimer = 0f;
    shakeupTimer = 0f;
    shakeupChosenTime = 0f;
    transitionTimer = 0f;
    finishedSubStage = false;

    //See if there are atoms from the just completed stage that are of the goal size of the new substage
    //To give the player as a bonus
    var bonusCarryoverAtoms = 0;
    if(!isInitialCall){
      bonusCarryoverAtoms = Mathf.Min(
        atomSet.Where(atom => atom.size == stageRules.StageOneRules.goalSize).Count(),
        stageRules.StageOneRules.goalNumber / 2
      );
    }

    atomSet.Clear();
    if(!isInitialCall){
      stageOneData.atomSizes.Clear();
      if(bonusCarryoverAtoms > 0){
        for(var i = 0; i < bonusCarryoverAtoms; i++){
          stageOneData.atomSizes.Add(stageRules.StageOneRules.goalSize);
        }
      }
    }else{
      transitionTimer = transitionTime;
    }

    Cleanup();

    if(stageRules.StageOneRules.maxChargeUp > 1){
      //show charge up tutorial
      tutorialSystem.ShowPopoutTutorial("1-chargeup", "You can now charge up!<br>Tap and hold to fire bigger atoms!");
    }
  }

  public void OnTransitionTo(StageTransitionData data){
    killOutsideBounds.enabled = true;

    //Add a kickoff push so that they aren't all static till the timer hits
    var particleComps = particleHolder.GetComponentsInChildren<RandomMovement>();
    for(int i = 0; i < particleComps.Length; i++){
      particleComps[i].AddKickoffForce();
    }

    warningText.Hide();
    successText.Hide();

    audioService.PlayMusic(music);
  }

  public void OnTransitionAway(bool toMenu){
    //turn off the kill outside bounds till we transition back so all the particles don't die when camera is out of place for a frame
    killOutsideBounds.enabled = false;

    audioService.StopMusic();
  }

  public void Cleanup(){
    objectPool.RecycleAll(atomPrefab);
    savedAtomSizeIndex = 0;
  }

  void OnSaving(){
    //Only save once we have spawned the initial atoms so that the save doesn't get overwritten on the transition before they've been spawned in
    if(atomSet.Count > 0){
      stageOneData.atomSizes = atomSet.Select(c => c.size).ToList();
    }
  }

  void Update () {
    int goalSizedAtomsCount = 0;
    var goalSize = stageRules.StageOneRules.goalSize;
    foreach(var atom in atomSet){
      if(atom.size == goalSize){ goalSizedAtomsCount++; }
    }
    stringChanger.UpdateString(goalText, "stageOneGoal", goalSizedAtomsCount, "{0}/{1}", goalSizedAtomsCount, stageRules.StageOneRules.goalNumber);
    goalAtomRenderer.size = goalSize;

    maxSizeAtomRenderer.size = stageRules.StageOneRules.minSplitSize;
    maxSizeText.text = AtomRenderer.elementNames[stageRules.StageOneRules.minSplitSize];

    //Skip all the rest of the logic if we're transitioning to next substage
    if(finishedSubStage){
      return;
    }

    SpawnAtomsIfNeeded();
    UpdateBgColors();

    //limit the number of particles on screen by disabling the colliders when there are too many atoms flying around
    if(atomSet.Count > maxParticles && !successText.visible){
      colliderHolder.SetActive(false);
      warningText.Show("MAX PRESSURE!", "VENTING");
    }else{
      colliderHolder.SetActive(true);
    }

    //Shake everything up periodically
    shakeupTimer += Time.deltaTime;
    if(stageRules.StageOneRules.maxShakeupTime > 0f && !successText.visible){
      if(shakeupChosenTime == 0f){
        shakeupChosenTime = Random.Range(stageRules.StageOneRules.minShakeupTime, stageRules.StageOneRules.maxShakeupTime);
      }

      //Cancel sunquake if we're showing success
      if(successText.visible){
        warningText.Hide();
        shakeupChosenTime = 0f;
        shakeupTimer = 0f;
      }

      //little bit of warning before it happens
      if(shakeupTimer > shakeupChosenTime - 3f){
        trauma.trauma += 0.015f;

        if(!warningText.visible){ //Little hack to only do this once per quake
          warningText.Show("SUNQUAKE", "IMMINENT");
          audioService.PlaySfx(sunquakeSound);
          MMVibrationManager.ContinuousHaptic(0.45f, 0.00f, 3f);
        }
      }

      //LETS GET SHOOK
      if(shakeupTimer >= shakeupChosenTime){
        trauma.trauma += 0.3f;
        //Add bg flash too
        var randMovement = particleHolder.GetComponentsInChildren<RandomMovement>();
        for(int i = 0; i < randMovement.Length; i++){
          //scale up kickoff force by the substage so bigger atoms will get pushed more
          randMovement[i].AddKickoffForce(4.5f + stageTransitionData.stageProgression[1] * 0.125f);
        }

        MMVibrationManager.Haptic(HapticTypes.Failure); //Not actually failure, but it gives a good multiple rumble :)

        warningText.Hide();

        shakeupChosenTime = 0f;
        shakeupTimer = 0f;
      }
    }

    //Increase the stage two atom saturation based on progress
    var pctProgress = Mathf.RoundToInt(((float)goalSizedAtomsCount / (float)stageRules.StageOneRules.goalNumber) * 100f);
    stageTwoData.atomSaturation[goalSize] = (ushort)Mathf.Clamp(
      Mathf.Max(stageTwoData.atomSaturation[goalSize], pctProgress)
    , 0, 100);

    //Check for completion
    if(goalSizedAtomsCount >= stageRules.StageOneRules.goalNumber && !finishedSubStage){
      finishedSubStage = true;

      StartTransitionToNextSubstage();
    }
  }

  void StartTransitionToNextSubstage(){
    //skip showing the big text when stage 2 is unlocked
    if(stageRules.stageUnlockData[2] != stageTransitionData.activeSubStage[stageTransitionData.activeStage]){
      successText.Show(AtomRenderer.elementNames[stageRules.StageOneRules.goalSize], "CREATED");
    }
    warningText.Hide();

    audioService.PlaySfx(substageCompleteSound);

    //Start transition to next substage items
    stageTransition.UnlockNextSubStage(stageTransition.stageData.activeStage);

    //Disable colliders so atoms can fly down naturally, will get reenabled by update after transition is finished
    colliderHolder.SetActive(false);
    LeanTween.moveLocal(particleHolder, particleHolder.transform.localPosition.AddY(-20f), substageTransitionAnimateTime).setEase(LeanTweenType.easeInOutCubic);

    StartCoroutine(FinishTransitionToNextSubstage());
  }

  IEnumerator FinishTransitionToNextSubstage(){
    yield return new WaitForSeconds(substageTransitionAnimateTime);

    particleHolder.transform.localPosition = Vector3.zero;

    stageTransition.TransitionTo(stageTransition.stageData.activeStage);
  }

  void SpawnAtomsIfNeeded(){
    //spawn in atoms if we're below the atom count or need to recreated saved ones
    spawnTimer += Time.deltaTime;
    var neededAtoms = Mathf.Max(stageRules.StageOneRules.startingAtomCount, stageOneData.atomSizes.Count);
    if(spawnTimer > spawnDelay && savedAtomSizeIndex < neededAtoms){
      spawnTimer = 0f;
      var savedAtomSize = (stageOneData.atomSizes.Count > 0 && savedAtomSizeIndex < stageOneData.atomSizes.Count)
        ? stageOneData.atomSizes[savedAtomSizeIndex]
        : (int?)null;

      savedAtomSizeIndex++;

      spawner.SpawnObjects(atomPrefab, 1, stageRectTransform, particleHolder.transform, null, (GameObject g) => {
        var atomInstance = g.GetComponentInChildren<Atom>();
        if(savedAtomSize.HasValue){
          atomInstance.size = savedAtomSize.Value;
        }else{
          //Add one becuase int Random Range is exclusive on the max
          atomInstance.size = UnityEngine.Random.Range(stageRules.StageOneRules.minSpawnSize, stageRules.StageOneRules.maxSpawnSize + 1);
        }

        //tween display in
        var atomRendererGo = atomInstance.gameObject.GetComponentInChildren<AtomRenderer>().gameObject;
        var origSize = atomRendererGo.transform.localScale;
        atomRendererGo.transform.localScale = Vector3.zero;
        LeanTween.scale(atomRendererGo, origSize, 0.5f).setEase(LeanTweenType.easeInOutBounce);
        g.GetComponent<RandomMovement>().AddKickoffForce();

        atomSet.Add(atomInstance);
      });
    }
  }

  void UpdateBgColors(){
    transitionTimer += Time.deltaTime;

    //Lerp through the background colors based on how many substages there are and how many bg colors
    var stageOneProgression = (float)stageTransitionData.activeSubStage[1] / 26f; //hardcoded iron max
    var numberOfColors = backgroundPalette.colorInfoList.Count;

    var startColorIndex = Mathf.RoundToInt(stageOneProgression * (float)(numberOfColors - 1));
    var endColorIndex = startColorIndex + 1;
    if(endColorIndex >= numberOfColors){
      endColorIndex = numberOfColors - 1;
      startColorIndex = endColorIndex - 1;
    }
    var previousStartColorIndex = Mathf.Max(startColorIndex - 1, 0);
    var previousEndColorIndex = Mathf.Max(endColorIndex - 1, 0);
    var lerp = Mathf.Clamp01(transitionTimer / transitionTime);

    backgroundGradient.startColor = Color.Lerp(backgroundPalette.getColorAtIndex(previousStartColorIndex), backgroundPalette.getColorAtIndex(startColorIndex), lerp);
    backgroundGradient.endColor = Color.Lerp(backgroundPalette.getColorAtIndex(previousEndColorIndex), backgroundPalette.getColorAtIndex(endColorIndex), lerp);
  }

  public void OnSubStageUnlocked(StageUnlockedData data){
    //gotta get the rules for one stage back since that's what we just completed
    var stageCompletedRules = stageRules.GetStageOneRules(data.subStage - 1, metaGameData.victoryCount);
    var aSat = stageCompletedRules.goalSize;
    stageTwoData.atomSaturation[aSat] = 100;
  }

  void OnAtomCreated(Atom newAtom){
    atomSet.Add(newAtom);
  }

  void OnAtomRemoved(Atom dyingAtom){
    atomSet.Remove(dyingAtom);
  }

}
