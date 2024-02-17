using UnityEngine;
using strange.extensions.mediation.impl;
using TMPro;
using UnityPackages.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VectorGraphics;

public class StageFourManager : View, IStageManager {

  public UIFilledBar hpBar;
  public UIFilledBar enemyHpBar;
  public TextMeshProUGUI livesText;
  public TextMeshProUGUI powerText;
  public TextMeshProUGUI speedText;
  public TextMeshProUGUI moveCountText;
  public CreatureSceneManager sceneManager;
  public ShinyButton moveButton;
  public ShinyButton eatButton;
  public ShinyButton fightButton;
  public ShinyButton mateButton;
  public ShinyButton spinButton;
  public ShinyButton forageButton;
  public SpinWheel spinWheel;

  public GameObject upgradeSelectionGO;
  public Transform upgradeSelectionContentParent;
  public TMP_Text upgradeSelectionExplainerText;


  public GameObject spinWheelGO;

  public GameObject deadScreen;
  public TextMeshProUGUI deadGeneticsText;
  public TextMeshProUGUI deadHelpText;
  public ShinyButton deadContinueButton;

  public ParticleSystem mateEffect;
  public ParticleSystem fightEffect;
  public GameObject effectsHolder;

  ParticleSystem.EmissionModule mateEffectEmission;
  ParticleSystem.EmissionModule fightEffectEmission;

  public Creature creature;


  //Audio
  public AudioClip upgradeTimeClip;
  public AudioClip upgradeSelectedClip;
  public AudioClip mateSuccessfulClip;
  public AudioClip eatClip;
  public AudioClip walkClip;
  public AudioClip dieClip;
  public AudioClip childReleasedClip;
  public AudioClip creatureSpawnClip;
  public AudioIntroRepeatOutro fightAudio;

  public AudioClip music;

  [Inject] StageTransitionService stageTransition { get; set; }
  [Inject] StageRulesService stageRules { get; set; }
  [Inject] StageTransitionModel stageData { get; set; }
  [Inject] StageFourDataModel stageFourData { get; set; }
  [Inject] StageFiveDataModel stageFiveData { get; set; }
  [Inject] ResourceLoaderService loader {get; set;}
  [Inject] FloatingText floatingNumbers {get; set;}
  [Inject] CreatureDoneMovingSignal creatureDoneMoving {get; set;}
  [Inject] CreatureDoneUpgradingSignal creatureDoneUpgrading {get; set;}
  [Inject] SpinWheelFinishedSignal spinFinished {get; set;}
  [Inject] StringChanger stringChanger {get; set;}
  [Inject] GameSavingSignal gameSavingSignal { get; set; }
  [Inject] TutorialSystem tutorialSystem { get; set; }
  [Inject] AudioService audioService { get; set; }

  bool fighting = false;
  bool mating = false;
  bool foraging = false;
  bool procreated = false;

  protected override void Awake () {
    base.Awake();

    moveButton.onClick.AddListener(ClickMoveForward);
    eatButton.onClick.AddListener(ClickEat);
    fightButton.onClick.AddListener(ClickFight);
    mateButton.onClick.AddListener(ClickMateButton);
    spinButton.onClick.AddListener(ClickSpin);
    forageButton.onClick.AddListener(ClickForage);
    deadContinueButton.onClick.AddListener(ClickDeadContinue);
    gameSavingSignal.AddListener(OnSaving);

    creatureDoneMoving.AddListener(() => StartCoroutine(FinishMoving()));
    spinFinished.AddListener(onSpinnerStop);


    mateEffectEmission = mateEffect.emission;
    fightEffectEmission = fightEffect.emission;
  }

  public void Init(bool isInitialCall){
    if(stageFourData.creatureData == null){
      stageFourData.creatureData = new CreatureData();
      stageFourData.creatureData.Reset();
    }

    //create a new creature body because the animations get fucked up and stuck in the dead animation after dying
    CreateNewPlayerCreature();

    creature.data = stageFourData.creatureData;
    creature.isPlayerCreature = true;
    creature.Reset();

    Debug.Log("Player creature finished init");

    deadScreen.gameObject.SetActive(false);
    sceneManager.Reset();
    sceneManager.CreateSceneObjects(stageFourData.sceneMoveCount);
    sceneManager.CreateSceneObjects(stageFourData.sceneMoveCount + 1);

    UpdateMoveCountText();

    if(stageFourData.creatureData.children != null){
      foreach(var childData in stageFourData.creatureData.children){
        creature.HaveAKid(childData);
      }
    }
    creature.UpdateDisplay(true);

    sceneManager.playerCreatureData = creature.data;
    sceneManager.GoToScene(stageFourData.sceneMoveCount, true);

    UpdateButtons();
    procreated = false;

    if(creature.data.dead){
      creature.Die(true);
      //TODO: maybe should save the death reason
      CreatureDead(Creature.DeathReason.Starvation);
    }else if(sceneManager.currentScene.data.canUpgrade && !sceneManager.currentScene.data.usedUpgrade){
      UpgradeTime();
    }

    //double check for unlocking stage 5.  This should be unnecessary most of the time but older saves
    //might have the mod but not have stage 5 unlocked and therefore could never unlock it
    if(!stageData.stagesUnlocked[5] && stageFourData.creatureData.modifiers.Contains(CreatureModifierId.TribalNature)){
      stageTransition.UnlockNextSubStage(stageTransition.stageData.activeStage);
    }

    if(!creature.data.dead){
      CheckSavedModsTarget();
    }
  }


  //Check to see if we should give the player some persistent mods to help get them unstuck
  void CheckSavedModsTarget(){

    //Some NRE bug on init sometimes?
    if(creatureDoneUpgrading == null || stageRules == null || stageFourData == null || stageFourData.savedCreatureMods == null){ return; }

    creatureDoneUpgrading.RemoveListener(CheckSavedModsTarget);

    int savedModsCountTarget = Mathf.FloorToInt(stageRules.StageFourRules.persistentModsPerRunsCompleted * stageFourData.runsCompleted);
    int savedModsCount = stageFourData.savedCreatureMods.Count;

    if(savedModsCountTarget > savedModsCount){
      Debug.Log("Player gets a pity saved mod");
      var ableToUpgrade = UpgradeTime(true);
      if(ableToUpgrade){
        creatureDoneUpgrading.AddListener(CheckSavedModsTarget);
      }
    }
  }


  void OnSaving(){
    //Only save once we have initted so that the save doesn't get overwritten on the transition before
    if(sceneManager.currentScene != null){
      stageFourData.currentSceneData = sceneManager.currentScene.data;
    }
  }

  public void OnTransitionTo(StageTransitionData data){
    audioService.PlayMusic(music);
  }

  public void OnTransitionAway(bool toMenu){
    audioService.StopMusic();
  }
  public void Cleanup(){

  }

  const string creatureFoodString = "creatureFood";
  const string creaturePowerString = "creaturePower";
  const string creatureSpeedString = "creatureSpeed";

  void Update () {
    hpBar.fillAmt = Mathf.Clamp01((float)creature.data.food / (float)creature.data.maxFood);
    stringChanger.UpdateString(hpBar.labelText, creatureFoodString, creature.data.food, "{0}/{1}", creature.data.food, creature.data.maxFood);

    stringChanger.UpdateString(powerText, creaturePowerString, creature.data.power);
    stringChanger.UpdateString(speedText, creatureSpeedString, creature.data.speed);
    stringChanger.UpdateString(livesText, "creaturelives", stageFourData.creatureLives);

    if(fighting && sceneManager.currentScene.data.enemyCreatureData != null){
      enemyHpBar.gameObject.SetActive(true);
      var enemy = sceneManager.currentScene.data.enemyCreatureData;
      enemyHpBar.fillAmt = (float)enemy.food / (float)enemy.maxFood;
      stringChanger.UpdateString(enemyHpBar.labelText, "enemyCreatureFood", enemy.food, "{0}/{1}", enemy.food, enemy.maxFood);
    }else{
      enemyHpBar.gameObject.SetActive(false);
    }

    //update creature state
    creature.walking = sceneManager.moving;
    creature.fighting = fighting;
    creature.foraging = foraging;
    creature.mating = mating;

#if DEBUG
    if(Input.GetKeyDown(KeyCode.B)){
      UpdateButtons();
    }
#endif
  }

  void ClickMoveForward(){
    MoveForward();
    UpdateButtons();
  }

  void ClickEat(){
    var foodEaten = creature.Eat(sceneManager.currentScene.data.groundFood, sceneManager.currentScene.enemyCreature);
    ShowAteFoodNumber(foodEaten.foodEaten);
    audioService.PlaySfx(eatClip);

    sceneManager.RemoveGroundFood(foodEaten.foodTypeEaten, foodEaten.creatureEaten);
    UpdateButtons();
  }

  void MoveForward(){
    stageFourData.totalSceneMoveCount++;
    sceneManager.MoveForward();
    UpdateMoveCountText();

    audioService.PlaySfx(walkClip);

    UpdateMoveButton();
    UpdateEatButton();
  }

  void UpdateMoveCountText(){
    moveCountText.text = stageFourData.sceneMoveCount.ToString();
  }

  void ShowAteFoodNumber(int amt){
    floatingNumbers.Create(
      hpBar.transform.position + new Vector3(1f, -1f),
      Colors.greenText,
      text: amt.ToString("+0;-#"),
      fontSize: 4
    );
  }

  IEnumerator FinishMoving(){
    var prevFood = creature.data.food;
    creature.MoveForward();
    var newFood = creature.data.food;
    ShowAteFoodNumber(newFood - prevFood);

    //Clear particles from old scenes
    fightEffect.Clear();
    mateEffect.Clear();

    if(creature.data.dead){
      CreatureDead(Creature.DeathReason.Combat);
      yield break;
    }

    if(stageFourData.sceneMoveCount >= stageRules.StageFourRules.maxSteps){
      creature.Die();
      CreatureDead(Creature.DeathReason.OldAge);
      yield break;
    }

    yield return ReleaseChildrenWhenReady();

    //Time to upgrade
    if(sceneManager.currentScene.data.canUpgrade){
      UpgradeTime();
    }

    UpdateButtons();
  }

  void UpdateButtons(){
    UpdateMoveButton();
    UpdateEatButton();
    UpdateFightButton();
    UpdateMateButton();
    UpdateForageButton();
  }

  void UpdateMoveButton(){
    var active = true;
    if(creature.data.dead){
      active = false;
    }
    else if(fighting || mating || foraging){
      active = false;
    }
    else if(sceneManager.moving){
      active = false;
    }
    //Check to see if you can run from an enemy
    else if(sceneManager.currentScene.data.enemyCreatureData != null &&
       !CreatureData.CreatureCanRun(sceneManager.currentScene.data.enemyCreatureData, creature.data)
    ){
      active = false;
    }
    else if(creature.data.food <= creature.data.FoodConsumedPerStep(stageRules.StageFourRules)){
      //forage button will be active
      active = false;
    }
    else if(sceneManager.currentScene.data.mateData != null && !mating){
      active = true;
    }

    moveButton.gameObject.SetActive(active);

    if(!active){return;}

    if(sceneManager.currentScene.data.enemyCreatureData != null
       && !sceneManager.currentScene.data.enemyCreatureData.dead
       && CreatureData.CreatureCanRun(sceneManager.currentScene.data.enemyCreatureData, creature.data)){
      moveButton.label = "Run";
    }else{
      moveButton.label = "Onwards";
    }
  }

  void UpdateEatButton(){
    var active = false;
    if(creature.data.dead){
      active = false;
    }
    else if(!fighting && !sceneManager.moving && !foraging && creature.CanEat(sceneManager.currentScene.data) && sceneManager.currentScene.data.mateData == null ){
      active = true;
    }
    eatButton.gameObject.SetActive(active);
  }

  void UpdateFightButton(){
    var active = false;
    if(creature.data.dead){
      active = false;
    }
    else if(sceneManager.moving || fighting || foraging || sceneManager.currentScene.data.mateData != null){
      active = false;
    }
    else if(sceneManager.currentScene.data.enemyCreatureData != null && !sceneManager.currentScene.data.enemyCreatureData.dead){
      active = true;
      sceneManager.currentScene.enemyCreature.ShowCombatDisplay();
    }else{
      active = false;
      if(sceneManager.currentScene.data.enemyCreatureData != null){
        sceneManager.currentScene.enemyCreature.HideCombatDisplay();
      }
    }
    fightButton.gameObject.SetActive(active);
  }

  void UpdateMateButton(){
    var active = false;
    if(creature.data.dead){
      active = false;
    }
    else if(!sceneManager.moving && sceneManager.currentScene.data.mateData != null && !mating){
      active = true;
    }
    mateButton.gameObject.SetActive(active);
  }

  void UpdateForageButton(){
    var active = false;
    if(creature.data.dead
      || fighting
      || foraging
      || mating
      || sceneManager.moving
      || creature.CanEat(sceneManager.currentScene.data)
      || sceneManager.currentScene.data.mateData != null
    ){
      active = false;
    }

    //Should only activate when your only choice is go onwards and starve
    else if(creature.data.food <= creature.data.FoodConsumedPerStep(stageRules.StageFourRules)){
      active = true;
    }

    forageButton.gameObject.SetActive(active);

    if(active){
      tutorialSystem.ShowTutorial(403);
    }
  }

  void ClickFight(){
    Debug.Log("Fightin' Time");
    spinWheel.Setup(GetFightOptions());

    var creatureSpinBonus = creature.data.wheelSpeedPctChange;
    var enemySpinBonus = sceneManager.currentScene.enemyCreature.data.wheelSpeedPctChange;

    var totalBonus = creatureSpinBonus - enemySpinBonus;

    //ramp up the wheel speed with an easing, and with your total experience
    var wheelSpeed = stageRules.StageFourRules.baseWheelSpeed
      + Mathf.RoundToInt((float)stageFourData.totalSceneMoveCount / 4f)
      + Easings.easeInSine(0, 400, Mathf.Clamp01((float)stageFourData.sceneMoveCount / 44f) );
    wheelSpeed = wheelSpeed * (1 - totalBonus);

    spinWheel.speed = wheelSpeed;

    spinWheelGO.SetActive(true);
    spinButton.gameObject.SetActive(true);
    spinButton.label = "Spin";

    fighting = true;

    UpdateButtons();
    tutorialSystem.ShowTutorial(401);
  }

  void ClickForage(){
    Debug.Log("Foraging");
    spinWheel.Setup(GetForageOptions(), false);

    //ramp up the wheel speed with an easing, and with your total experience
    //Should be similar, but a little harder, than fight speed
    var wheelSpeed = (stageRules.StageFourRules.baseWheelSpeed * 1.5f)
      + Mathf.RoundToInt((float)stageFourData.totalSceneMoveCount / 4f)
      + Easings.easeInSine(0, 400, Mathf.Clamp01((float)stageFourData.sceneMoveCount / 44f) );

    spinWheel.speed = wheelSpeed;

    spinWheelGO.SetActive(true);
    spinButton.gameObject.SetActive(true);
    spinButton.label = "Spin";

    foraging = true;

    UpdateButtons();
  }

  //Returns if the player can upgrade
  bool UpgradeTime(bool isSavedModUpgrade = false){
    upgradeSelectionExplainerText.gameObject.SetActive(isSavedModUpgrade);

    var selectedMods = creature.data.getAvailableMods(
      stageFourData,
      stageFourData.sceneMoveCount,
      stageFourData.mapSeed + stageFourData.sceneMoveCount,
      3
    );
    if(selectedMods.Length == 0){
      return false; //too bad
    }
    Debug.Log("Upgrade time");

    audioService.PlaySfx(upgradeTimeClip);
    var upgradePrefab = loader.Load<GameObject>("Prefabs/4/UpgradeSelection");
    upgradeSelectionGO.SetActive(true);

    foreach(var mod in selectedMods){

      var newMod = GameObject.Instantiate(
        upgradePrefab,
        Vector3.zero,
        Quaternion.identity
      );
      newMod.transform.SetParent(upgradeSelectionContentParent, false);

      var titleTransform = newMod.transform.Find("Background/Title");

      var title = newMod.transform.Find("Background/Title").GetComponent<TMP_Text>();
      var descrip = newMod.transform.Find("Background/Descrip").GetComponent<TMP_Text>();
      var button = newMod.GetComponent<ShinyButton>();

      var icon = newMod.transform.Find("Background/Icon").GetComponentInChildren<SVGImage>();
      var loadedIcon = loader.Load<Sprite>(CreatureModifier.modifierIcons[mod.type]);
      icon.sprite = loadedIcon;

      var incompatibleMods = creature.data.GetIncompatibleMods(mod.id);
      string descripText = mod.descrip;

      if(incompatibleMods.Count > 0){
        descripText = string.Format(
          "<#{0}>Removes {1}</color><br>{2}"
          , Colors.purpleText.ToHex()
          , string.Join(", ", incompatibleMods.Select(m => CreatureModifier.allModifiers[m].name))
          , mod.descrip
        );
      }

      title.text = mod.name;
      descrip.text = descripText;
      button.color = (mod.persistent || isSavedModUpgrade) ? UIColor.Green : UIColor.Blue;

      button.onClick.AddListener(() => ClickUpgradeButton(mod, isSavedModUpgrade));
    }

    //Manually update the flexbox layout which can get messed up with adding and removing children
    var flexboxLayout = upgradeSelectionContentParent.GetComponent<UIFlexbox>();
    flexboxLayout.Draw();

    return true;
  }

  void ClickUpgradeButton(CreatureModifier mod, bool isSavedModUpgrade){
    upgradeSelectionContentParent.DestroyChildren();
    upgradeSelectionGO.SetActive(false);
    sceneManager.currentScene.data.usedUpgrade = true;

    if(mod == null){
      return;
    }

    audioService.PlaySfx(upgradeSelectedClip);
    if(isSavedModUpgrade){
      stageFourData.savedCreatureMods.Add(mod.id);
    }
    creature.data.AddMod(mod);
    creature.UpdateDisplay(false);

    UpdateButtons();

    //Clear cached strings, probably only needed for the max food/hp one
    stringChanger.ClearValue(creatureFoodString);
    stringChanger.ClearValue(creaturePowerString);
    stringChanger.ClearValue(creatureSpeedString);

    //check for unlocks
    if(mod.tribePopulationBonus > 0){
      stageFiveData.creaturePopulationBonus += mod.tribePopulationBonus;
    }

    creatureDoneUpgrading.Dispatch();
  }

  void ClickMateButton(){
    Debug.Log("Mating time");
    mating = true;
    sceneManager.currentScene.mate.HideMateDisplay();

    spinWheel.Setup(GetMateOptions(), false);

    var wheelSpeed = (stageRules.StageFourRules.baseWheelSpeed * 1.5f) + (creature.children.Count * 70)
      + Easings.easeInSine(0, 250, Mathf.Clamp01((float)stageFourData.sceneMoveCount / 44f) );
    ;

    spinWheel.speed = wheelSpeed;

    spinWheelGO.SetActive(true);
    spinButton.gameObject.SetActive(true);
    spinButton.label = "Spin";

    UpdateButtons();

  }

  IEnumerator FinishMating(SpinWheelOption spinOption){
    var success = false;
    switch(spinOption.effect){
      case SpinEffect.Win:
        success = true;
        break;
      case SpinEffect.Lose:
        success = false;
        break;
    }

    if(success){
      effectsHolder.transform.position = effectsHolder.transform.position.SetX(0);
      mateEffect.Clear();
      mateEffect.Play();
      mateEffectEmission.enabled = true;

      audioService.PlaySfx(mateSuccessfulClip);
      stageFourData.totalMatesSuccess++;
    }

    var yourMate = sceneManager.currentScene.mate;
    var theyLeaveDelay = success ? 2f : 0f;
    sceneManager.currentScene.data.mateData = null;

    if(theyLeaveDelay > 0){
      yield return new WaitForSeconds(theyLeaveDelay);
    }

    Destroy(yourMate.gameObject, 4f);
    LeanTween.move(yourMate.gameObject, yourMate.transform.position.AddX(-14), 3f).setEase(LeanTweenType.easeInOutSine);
    yourMate.walking = true;

    yield return new WaitForSeconds(1);

    mateEffectEmission.enabled = false;

    if(success){
      CreatureModifierId? bonusModifierFromMate = null;
      //Add in bonus modId from other parent if they have more mods signalling they got a bonus one
      if(yourMate.data.modifiers.Count > creature.data.modifiers.Count){
        bonusModifierFromMate = yourMate.data.modifiers.Last();
      }
      var childData = creature.HaveAKid(null, bonusModifierFromMate);

      creature.data.children.Add(childData);

      tutorialSystem.ShowTutorial(402);
    }

    spinWheelGO.SetActive(false);
    mating = false;
    UpdateButtons();
  }


  void ClickSpin(){
    if(!spinWheel.Spinning){
      spinButton.label = "Stop";
      spinButton.color = UIColor.Gray;
      spinButton.interactable = false;
      StartCoroutine(CanStopSpinner());

      //Effects
      effectsHolder.transform.position = effectsHolder.transform.position.SetX(0);

      if(fighting){
        fightEffect.Play();
        fightEffectEmission.enabled = true;
        fightAudio.Play();
      }

      spinWheel.Spin();
    }else{
      spinButton.gameObject.SetActive(false);
      fightAudio.Stop();
      spinWheel.Stop();
    }
  }

  IEnumerator CanStopSpinner(){
    yield return new WaitForSeconds(spinWheel.spinUpTime * 2);

    spinButton.interactable = true;
    spinButton.color = UIColor.Blue;
  }

  void onSpinnerStop(SpinWheelOption spinOption){
    if(fighting){
      StartCoroutine(FinishFighting(spinOption));
    }else if(mating){
      StartCoroutine(FinishMating(spinOption));
    }else if (foraging){
      StartCoroutine(FinishForaging(spinOption));
    }
  }

  IEnumerator FinishFighting(SpinWheelOption spinOption){
    var opponent = sceneManager.currentScene.enemyCreature;

    Logger.Log("Finished Fighting " + spinOption.effect);

    switch(spinOption.effect){
      case SpinEffect.Win:
        opponent.Die();
        break;
      case SpinEffect.Lose:
        creature.Die();
        break;
      case SpinEffect.YouLoseFood:
        creature.data.ChangeFood(-spinOption.amt);
        ShowAteFoodNumber(-spinOption.amt);
        break;
      case SpinEffect.TheyLoseFood:
        opponent.data.ChangeFood(-spinOption.amt);
        break;
      case SpinEffect.BothLoseFood:
        creature.data.ChangeFood(-spinOption.amt);
        opponent.data.ChangeFood(-spinOption.amt);
        ShowAteFoodNumber(-spinOption.amt);
        break;
      case SpinEffect.TheyRun:
        Destroy(opponent.gameObject, 4f);
        opponent.walking = true;
        LeanTween.move(opponent.gameObject, opponent.transform.position.AddX(-14), 3f).setEase(LeanTweenType.easeInOutSine);
        sceneManager.currentScene.data.enemyCreatureData = null;
        sceneManager.currentScene.enemyCreature = null;
        break;
      case SpinEffect.YouRun:
        MoveForward();
        break;
    }

    if(creature.data.dead || creature.data.food <= 0){
      Logger.Log("Creature dead");
      CreatureDead(Creature.DeathReason.Combat);
    }

    fightEffectEmission.enabled = false;

    yield return new WaitForSeconds(1.0f);

    Logger.Log("Finished waiting");
    spinWheelGO.SetActive(false);
    fighting = false;

    if(creature.data.dead){ yield break; }

    Logger.Log("Releasing children when ready");
    yield return ReleaseChildrenWhenReady();

    Logger.Log("Updating buttons");
    UpdateButtons();
  }


  IEnumerator FinishForaging(SpinWheelOption spinOption){

    switch(spinOption.effect){
      case SpinEffect.Win:
        var foodEaten = creature.Forage();
        ShowAteFoodNumber(foodEaten.foodEaten);
        audioService.PlaySfx(eatClip);

        break;
      case SpinEffect.Lose:
        creature.Die();
        break;
    }

    if(creature.data.dead || creature.data.food <= 0){
      CreatureDead(Creature.DeathReason.Starvation);
    }

    yield return new WaitForSeconds(1.25f);

    spinWheelGO.SetActive(false);
    foraging = false;

    if(creature.data.dead){ yield break; }

    UpdateButtons();
  }

  void CreatureDead(Creature.DeathReason deathReason){

    deadScreen.gameObject.SetActive(true);
    deadHelpText.gameObject.SetActive(false);

    audioService.PlaySfx(dieClip);

    var modsToPass = creature.data.GetModsToPassOn(stageFourData, procreated);
    stageFourData.savedCreatureMods = modsToPass;
    var modNames = modsToPass.GroupBy(m => CreatureModifier.allModifiers[m].name).Select(gp => (gp.Count() > 1 ? gp.Count() + " " : string.Empty) + gp.Key);

    if(procreated){
      deadGeneticsText.text = string.Format("But your child passed on some of your genetics: {0}", string.Join(", ", modNames));

      deadGeneticsText.gameObject.SetActive(true);
    }else{
      if(stageFourData.savedCreatureMods != null && stageFourData.savedCreatureMods.Count > 0){
        deadGeneticsText.text = string.Format("But you keep your previous genetics: {0}", string.Join(", ", modNames));

        deadGeneticsText.gameObject.SetActive(true);
      }else{
        deadGeneticsText.gameObject.SetActive(false);
        deadHelpText.gameObject.SetActive(true);
      }
    }

    stageFourData.childrenBonusMods.Clear();
    UpdateButtons();
  }

  void ClickDeadContinue(){
    stageFourData.creatureLives--;
    stageFourData.runsCompleted++;

    // Record run length for stats
    stageFourData.runLengths.Add(stageFourData.sceneMoveCount);


    //clear the persistent data and restore saved mods to new creature
    stageFourData.creatureData.Reset();
    if(stageFourData.savedCreatureMods != null){
      foreach(var savedMod in stageFourData.savedCreatureMods){
        stageFourData.creatureData.AddMod(CreatureModifier.allModifiers[savedMod], true);
      }
    }
    stageFourData.creatureData.food = stageFourData.creatureData.maxFood;
    stageFourData.sceneMoveCount = 0;
    stageFourData.mapSeed = Random.Range(1, int.MaxValue - 1);
    stageFourData.currentSceneData = null;

    audioService.PlaySfx(creatureSpawnClip);

    Init(false);
  }

  void CreateNewPlayerCreature(){
    var newCreatureGO = GameObject.Instantiate(
      sceneManager.creaturePrefab,
      creature.transform.position,
      Quaternion.identity,
      creature.transform.parent
    );

    var newCreature = newCreatureGO.GetComponent<Creature>();

    Destroy(creature.gameObject);

    creature = newCreature;
  }

  List<SpinWheelOption> GetFightOptions(){
    var opponentData = sceneManager.currentScene.data.enemyCreatureData;

    var winSlots = 2;
    var loseSlots = 2;
    var bothLoseFoodSlots = 2;
    var theyLoseFoodSlots = 2;
    var youLoseFoodSlots = 1;
    var theyRunSlots = 2;
    var youRunSlots = 1;

    var powerDiff = Mathf.Clamp(creature.data.power - opponentData.power, -6, 6);
    var speedDiff = Mathf.Clamp(creature.data.speed - opponentData.speed, -6, 6);

    //when you have the advantage
    if(powerDiff >= 2){
      loseSlots--;
      theyLoseFoodSlots++;
    }
    if(powerDiff >= 4){
      bothLoseFoodSlots--;
      winSlots++;
    }
    if(powerDiff >= 6){
      loseSlots--;
      winSlots++;
    }
    if(speedDiff >= 2){
      theyRunSlots--;
      theyLoseFoodSlots++;
    }
    if(speedDiff >= 4){
      theyRunSlots--;
      winSlots++;
    }

    //and when you don't have the advantage
    if(powerDiff <= -2){
      loseSlots++;
      theyLoseFoodSlots--;
    }
    if(powerDiff <= -4){
      bothLoseFoodSlots++;
      winSlots--;
    }
    if(powerDiff <= -6){
      loseSlots++;
      winSlots--;
    }
    if(speedDiff < 0){
      theyRunSlots++;
      theyLoseFoodSlots--;
    }
    if(speedDiff <= -2){
      theyRunSlots++;
      youRunSlots--;
    }
    if(speedDiff <= -4){
      theyRunSlots++;
      if(winSlots > 0){
        winSlots--;
      }else{
        bothLoseFoodSlots--;
      }
    }

    //Check for the ambush only case to remove the running options
    if(creature.data.modifiers.Any(m => m == CreatureModifierId.Ambush) && creature.data.power > opponentData.speed){
      winSlots += theyRunSlots;
      theyRunSlots = 0;
    }
    if(opponentData.modifiers.Any(m => m == CreatureModifierId.Ambush) && opponentData.power > creature.data.speed){
      loseSlots += youRunSlots;
      youRunSlots = 0;
    }

    if(creature.data.modifiers.Any(m => m == CreatureModifierId.Stubborn)){
      winSlots += youRunSlots;
      youRunSlots = 0;
    }
    if(opponentData.modifiers.Any(m => m == CreatureModifierId.Stubborn)){
      loseSlots += theyRunSlots;
      theyRunSlots = 0;
    }

    //can't lose outright in the first few stages
    if(stageFourData.sceneMoveCount < 5){
      youLoseFoodSlots += loseSlots;
      loseSlots = 0;
    }

    const int loseFoodBase = 30;
    var theyLoseFoodAmt = loseFoodBase + creature.data.theyLoseFoodBonus;
    var youLoseFoodAmt = loseFoodBase + opponentData.theyLoseFoodBonus;

    var spinOptions = new List<SpinWheelOption>(){
      new SpinWheelOption(){
        color = Colors.green, slots = winSlots, descrip = "Win", effect = SpinEffect.Win
      },
      new SpinWheelOption(){
        color = Colors.red, slots = loseSlots, descrip = "Lose", effect = SpinEffect.Lose
      },
      new SpinWheelOption(){
        color = Colors.yellow, slots = bothLoseFoodSlots, descrip = "Both -20 Food", effect = SpinEffect.BothLoseFood, amt = 20
      },
      new SpinWheelOption(){
        color = Colors.blue, slots = theyLoseFoodSlots, descrip = string.Format("They Lose -{0} Food", theyLoseFoodAmt), effect = SpinEffect.TheyLoseFood, amt = theyLoseFoodAmt
      },
      new SpinWheelOption(){
        color = Colors.orange, slots = youLoseFoodSlots, descrip = string.Format("You Lose -{0} Food", youLoseFoodAmt), effect = SpinEffect.YouLoseFood, amt = youLoseFoodAmt
      },
      new SpinWheelOption(){
        color = Colors.mint, slots = theyRunSlots, descrip ="They Run Away", effect = SpinEffect.TheyRun
      },
      new SpinWheelOption(){
        color = Colors.purple, slots = youRunSlots, descrip ="You Run Away", effect = SpinEffect.YouRun
      },
    };

    return spinOptions;
  }

  List<SpinWheelOption> GetMateOptions(){

    var slotWidth = 1 + creature.data.mateSliceBonus;
    var timesToRepeat = 12 / 2 / slotWidth;

    var spinOptions = new List<SpinWheelOption>();
    for(var i = 0; i < timesToRepeat; i++)
    {
      spinOptions.Add(
        new SpinWheelOption(){
          color = Colors.green, slots = slotWidth, descrip = "Yes", effect = SpinEffect.Win
        }
      );
      spinOptions.Add(
        new SpinWheelOption(){
          color = Colors.red, slots = slotWidth, descrip = "No", effect = SpinEffect.Lose
        }
      );
    };


    return spinOptions;
  }

  List<SpinWheelOption> GetForageOptions(){

    var spinOptions = new List<SpinWheelOption>();

    spinOptions.Add(
      new SpinWheelOption(){
        color = Colors.green, slots = 1, descrip = "Success", effect = SpinEffect.Win
      }
    );
    spinOptions.Add(
      new SpinWheelOption(){
        color = Colors.red, slots = 5, descrip = "Failure", effect = SpinEffect.Lose
      }
    );
    spinOptions.Add(
      new SpinWheelOption(){
        color = Colors.green, slots = 1, descrip = "Success", effect = SpinEffect.Win
      }
    );
    spinOptions.Add(
      new SpinWheelOption(){
        color = Colors.red, slots = 5, descrip = "Failure", effect = SpinEffect.Lose
      }
    );

    return spinOptions;
  }

  IEnumerator ReleaseChildrenWhenReady(){
    if(sceneManager.currentScene.enemyCreature != null && !sceneManager.currentScene.enemyCreature.data.dead){
      yield break;
    }

    //check releasing kids
    var reared = creature.children.Where(c => c.data.age >= stageRules.StageFourRules.childIncubationSteps).ToArray();
    foreach(var child in reared){
      if(!procreated){
        floatingNumbers.Create(
          transform.position,
          Color.white,
          text: "Passed on your genes!",
          fontSize: 3.5f,
          ttl: 3f
        );
      }
      procreated = true;
      stageFourData.creatureLives++;
      stageFourData.totalChildCount++;
      if(child.data.bonusModifier.HasValue){
        stageFourData.childrenBonusMods.Add(child.data.bonusModifier.Value);
      }

      stageTransition.UnlockNextStage(5);

      audioService.PlaySfx(childReleasedClip);
      creature.ReleaseKid(child);
      yield return new WaitForSeconds(2);
    }
  }
}
