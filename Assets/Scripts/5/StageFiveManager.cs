using UnityEngine;
using strange.extensions.mediation.impl;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System;
using MoreMountains.NiceVibrations;

public class StageFiveManager : View, IStageManager {

  public ShinyButton nextWaveButton;
  public GameObject tilePrefab;
  public GameObject linePrefab;
  public GameObject pathLinePrefab;
  public GameObject pathNumCreepsPrefab;
  public GameObject gameOverScreen;
  public TextMeshProUGUI gameOverText;
  public ShinyButton gameOverContinueButton;
  public WaveDisplay waveDisplay;
  public FilledBar hqPopBar;

  public TextMeshProUGUI populationText;
  public TextMeshProUGUI oreText;
  public TextMeshProUGUI woodText;
  public TextMeshProUGUI moneyText;

  public Transform gridHolder;
  public Transform buildingHolder;
  public Transform creepHolder;
  public Transform shotHolder;
  public Transform pathLinesHolder;

  public TowerSelection towerSelection;

  public GameObject towerSpawnParticle;

  public AudioClip creepDieSound;
  public AudioClip creepCompletePathSound;
  public AudioClip startWaveSound;
  public AudioClip towerBuiltSound;
  public AudioClip towerUpgradeSound;
  public AudioClip towerSoldSound;
  public AudioClip waveCompletedSound;
  public AudioClip defeatedSound;

  public AudioClip ambientMusic;

  [Inject] StageTransitionService stageTransition { get; set; }
  [Inject] StageTransitionModel stageData {get; set;}
  [Inject] StageFiveDataModel stageFiveData {get; set;}
  [Inject] StageRulesService stageRules { get; set; }
  [Inject] MetaGameDataModel metaGameData {get; set;}
  [Inject] TimeService time { get; set; }
  [Inject] FloatingText floatingNumbers {get; set;}
  [Inject] InputService input {get; set;}
  [Inject] ResourceLoaderService loader {get; set;}
  [Inject] TowerBuiltSignal towerBuilt {get; set;}
  [Inject] TowerSellSignal towerSold {get; set;}
  [Inject] TowerUpgradeSignal towerUpgrade {get; set;}
  [Inject] WaveCompletedSignal waveCompleted {get; set;}
  [Inject] GameLoadedSignal gameLoadedSignal { get; set; }
  [Inject] PopulationIncreasedSignal populationIncreased {get; set;}
  [Inject] StringChanger stringChanger {get; set;}
  [Inject] ObjectPool objectPool {get; set;}
  [Inject] AudioService audioService {get; set;}
  [Inject] TutorialSystem tutorialSystem {get; set;}
  [Inject] OpenTutorialFinishedSignal openTutorialFinishedSignal { get; set; }

  Dictionary<TdCreepType, GameObject> creepPrefabs;
  Dictionary<TdTowerType, GameObject> towerPrefabs;

  TdGrid grid = new TdGrid();
  const float tileWidth = 0.5f;
  const int gridWidth = 11;
  const int gridHeight = 15;
  Int2 hqPos = new Int2(5, 7);

  List<TdCreep> creeps = new List<TdCreep>();
  static Int2[] waterStartPositions = new Int2[]{
    new Int2(4, 6),
    new Int2(4, 7),
    new Int2(4, 8),
    new Int2(6, 6),
    new Int2(6, 7),
    new Int2(6, 8),
  };

  static Int2[] hqDestPositions = new Int2[]{
    new Int2(5, 6),
    new Int2(5, 8),
  };

  static Int2[] creepSpawnPoints = new Int2[]{
    new Int2(0, 0),
    new Int2(0, gridHeight - 1),
    new Int2(gridWidth - 1, 0),
    new Int2(gridWidth - 1, gridHeight - 1),
  };

  static Int2[] orePoints = new Int2[]{
    new Int2(2, 2),
    new Int2(2, gridHeight - 3),
    new Int2(gridWidth - 3, 2),
    new Int2(gridWidth - 3, gridHeight - 3),
  };

  static Int2[] forestPoints = new Int2[]{
    new Int2(0, 1),
    new Int2(0, 2),
    new Int2(0, 6),
    new Int2(0, 7),
    new Int2(0, 8),
    new Int2(0, 12),
    new Int2(0, 13),

    new Int2(gridWidth - 1, 1),
    new Int2(gridWidth - 1, 2),
    new Int2(gridWidth - 1, 6),
    new Int2(gridWidth - 1, 7),
    new Int2(gridWidth - 1, 8),
    new Int2(gridWidth - 1, 12),
    new Int2(gridWidth - 1, 13),
  };

  static Color[] spawnPointColors = new Color[]{
    Colors.blue.SetA(0.85f),
    Colors.purple.SetA(0.85f),
    Colors.orange.SetA(0.85f),
    Colors.red.SetA(0.85f)
  };

  //Wave management stuff
  class WaveData {
    public int waveNumber;
    public bool inProgress;
    public bool spawning;
    public int totalSpawned;
    public int totalToSpawn;
    public float spawnAccum = 0f;
    public float waveSpawnTime;
    public float totalTimeAccum = 0f;
  }

  //Keyed by the waveNumber
  Dictionary<int, WaveData> waveData = new Dictionary<int, WaveData>();
  const int maxWavesInProgress = 4;
  int lastStartedWave;

  protected override void Awake() {
    base.Awake();

    gameOverContinueButton.onClick.AddListener(GameOverContinue);
    towerBuilt.AddListener(OnTowerBuilt);
    towerSold.AddListener(OnTowerSold);
    towerUpgrade.AddListener(OnTowerUpgrade);
    waveCompleted.AddListener(OnWaveCompleted);
    populationIncreased.AddListener(OnPopulationIncrease);
    openTutorialFinishedSignal.AddListener(OnOpenTutorialFinished);

    nextWaveButton.onClick.AddListener(StartSpawning);

    waveDisplay.OnClickWaveIndicator = DrawPathLines;

    //regen a new grid so we're all kosher
    gridHolder.DestroyChildren(true);
    grid.Create(gridWidth, gridHeight, tileWidth, tilePrefab, linePrefab, gridHolder, hqDestPositions.Length);

    creepPrefabs = new Dictionary<TdCreepType, GameObject>() {
      {TdCreepType.Normal, loader.Load<GameObject>("Prefabs/5/creep")},
      {TdCreepType.Fast, loader.Load<GameObject>("Prefabs/5/creep_fast")},
      {TdCreepType.Group, loader.Load<GameObject>("Prefabs/5/creep_group")},
      {TdCreepType.Immune, loader.Load<GameObject>("Prefabs/5/creep_immune")},
      {TdCreepType.Spawn, loader.Load<GameObject>("Prefabs/5/creep_spawn")},
      {TdCreepType.Flying, loader.Load<GameObject>("Prefabs/5/creep_flying")},
      {TdCreepType.Boss, loader.Load<GameObject>("Prefabs/5/creep")},
      {TdCreepType.Friendly, loader.Load<GameObject>("Prefabs/5/creep_friendly")},
    };

    towerPrefabs = new Dictionary<TdTowerType, GameObject>() {
      {TdTowerType.Cheap, loader.Load<GameObject>("Prefabs/5/tower_cheap")},
      {TdTowerType.Fast, loader.Load<GameObject>("Prefabs/5/tower_fast")},
      {TdTowerType.Slowing, loader.Load<GameObject>("Prefabs/5/tower_slowing")},
      {TdTowerType.Piercing, loader.Load<GameObject>("Prefabs/5/tower_piercing")},
      {TdTowerType.AOE, loader.Load<GameObject>("Prefabs/5/tower_aoe")},
      {TdTowerType.Lumber, loader.Load<GameObject>("Prefabs/5/tower_lumber")},
      {TdTowerType.Quarry, loader.Load<GameObject>("Prefabs/5/tower_quarry")},
      {TdTowerType.Farm, loader.Load<GameObject>("Prefabs/5/tower_farm")},
    };

    Cleanup();
    towerSelection.SetGrid(grid);

    foreach(var creepPrefab in creepPrefabs.Values){
      objectPool.CreatePool(creepPrefab, 0);
    }
    objectPool.CreatePool(towerSpawnParticle, 0);

    gameLoadedSignal.AddListener(OnLoad);
  }

  void OnLoad(){
    if(stageFiveData.savedTowers != null){
      foreach(var tower in stageFiveData.savedTowers){
        BuildTower(tower, true);
      }
    }
    lastStartedWave = stageFiveData.lastCompletedWave;

    PrevLayout();
  }

  public void Init(bool isInitialCall){

    if(isInitialCall && stageFiveData.population == 0){
      ResetStageData();
    }
  }

  public void OnTransitionTo(StageTransitionData data){
    audioService.PlayMusic(ambientMusic);
  }

  public void OnTransitionAway(bool toMenu){
    audioService.StopMusic();
  }

  //also used when player lost and we need to start over
  public void Cleanup(){

    if(creepPrefabs != null){
      foreach(var prefabType in creepPrefabs){
        objectPool.RecycleAll(prefabType.Value);
      }
    }
    objectPool.RecycleAll(towerSpawnParticle);

    // creepHolder.DestroyChildren();
    buildingHolder.DestroyChildren();
    shotHolder.DestroyChildren();
    waveData.Clear();
    creeps.Clear();

    //when this is called before the stage loads
    if(stageFiveData == null) return;

    ResetStageData();
    lastStartedWave = 0;
    time.Resume();
    gameOverScreen.SetActive(false);
    gameOverText.text = null;
    GridInit();
    waveDisplay.Init();

    PrevLayout();
  }

  const int startingPopulation = 10;
  void ResetStageData(){
    stageFiveData.population = startingPopulation;
    stageFiveData.assignedPopulation = 0;
    stageFiveData.money = 40;
    stageFiveData.ore = 0;
    stageFiveData.wood = 0;
    stageFiveData.lastWaveToIncreasePop = 0;
    stageFiveData.populationRecruited = 0;
    stageFiveData.lastCompletedWave = 0;
    stageFiveData.savedTowers = new List<SavedTower>();
    stageData.stageProgression[5] = 0;
    stageData.activeSubStage[5] = 0;
  }

  void PrevLayout(){
    if(stageFiveData.previousRunSavedTowers != null){
      foreach(var tower in stageFiveData.previousRunSavedTowers){
        BuildGhostTower(tower.towerPosition, tower.towerType);
      }
    }
  }

  void BuildGhostTower(Int2 position, TdTowerType towerType ){
    var tile = grid.GetTile(position);

    if(tile.tower != null) return;

    BuildTower(new SavedTower(){
      towerPosition = position,
      towerType = towerType,
      towerLevel = 0
    }, true, true);
  }

  int prevPop = -1;
  int prevAssPop = -1;

  void Update () {
    UnityEngine.Profiling.Profiler.BeginSample("Update spawn");
    UpdateSpawn();
    UnityEngine.Profiling.Profiler.EndSample();

    UnityEngine.Profiling.Profiler.BeginSample("Input");
    if(input.GetButtonUp()){
      var worldPos = input.pointerWorldPosition;

      var tile = grid.TryGetTile(worldPos);
      if(tile != null){
        MMVibrationManager.Haptic(HapticTypes.LightImpact);
        TryPlaceTower(tile);
      }
    }
    UnityEngine.Profiling.Profiler.EndSample();


    UnityEngine.Profiling.Profiler.BeginSample("Strings");
    if(prevPop != stageFiveData.population || prevAssPop != stageFiveData.assignedPopulation){
      prevPop = stageFiveData.population;
      prevAssPop = stageFiveData.assignedPopulation;
      populationText.text = string.Format("{0}/{1}", stageFiveData.assignedPopulation, stageFiveData.population);
    }

    stringChanger.UpdateString(moneyText, "tdMoney", stageFiveData.money);
    stringChanger.UpdateString(oreText, "tdOre", stageFiveData.ore);
    stringChanger.UpdateString(woodText, "tdWood", stageFiveData.wood);
    UnityEngine.Profiling.Profiler.EndSample();

    //hq bar
    UnityEngine.Profiling.Profiler.BeginSample("Hq Bar");
    var adjustedWavesPerPop = stageFiveData.GetAdjustedWavesPerPop(stageRules);
    var nextWaveToGetPop = adjustedWavesPerPop + stageFiveData.lastWaveToIncreasePop;
    hqPopBar.fillAmt = (float)(stageFiveData.lastCompletedWave - stageFiveData.lastWaveToIncreasePop) / (float)adjustedWavesPerPop;

    //Disable wave button if too many are going on
    nextWaveButton.interactable = waveData.Values.Count < maxWavesInProgress;
    UnityEngine.Profiling.Profiler.EndSample();
  }

  void OnWaveCompleted(int wave){
    waveData.Remove(wave);


    tutorialSystem.ShowTutorial(503);

    if(wave > 10){
      //population and upgrades
      tutorialSystem.ShowTutorial(505, 0.8f);
    }
    if(wave > 15){
      //population and upgrades
      tutorialSystem.ShowPopoutTutorial("5-creep-paths", "Tap the enemy entrances to see where they're going to go!");
    }

    //Unlock stage 6 if we completed the necessary wave
    if(wave >= stageRules.StageFiveRules.waveToUnlockStageSix && !stageData.stagesUnlocked[6]){
      stageTransition.UnlockNextStage(6);
    }

    //Check to see if it's time to get some new peeps.
    var adjustedWavesPerPop = stageFiveData.GetAdjustedWavesPerPop(stageRules);

    if(wave >= adjustedWavesPerPop + stageFiveData.lastWaveToIncreasePop){
      stageFiveData.lastWaveToIncreasePop = wave;
      stageFiveData.population++;
      populationIncreased.Dispatch();
    }
    stageFiveData.lastCompletedWave = wave;

    audioService.PlaySfx(waveCompletedSound);
    audioService.PlayMusic(ambientMusic);
  }

  void OnPopulationIncrease(){
    floatingNumbers.Create(grid.GetTile(hqPos).go.transform.position, Colors.greenText, text: "+1", fontSize: 5, moveUpPct: 0.4f, punchSize: 0.5f, ttl: 1.5f);
  }

  void TryPlaceTower(TdTile tile){
    //see if this is a spawn point and draw path lines for those
    if(creepSpawnPoints.Contains(tile.pos)){
      var waveStage = waveData.Values.Count > 0 ? waveData.Values.First().waveNumber : stageFiveData.lastCompletedWave + 1;
      DrawPathLines(waveStage);
      return;
    }

    //special thing for clicking on the hq
    if(tile.pos == hqPos){
      towerSelection.SelectHq(tile);
      return;
    }

    //check to see if any creeps occupy this tile
    var creepOccupied = false;

    foreach(var creep in creeps){
      if(
        creep == null
        || creep.gameObject == null
        || creep.statsType == TdCreepType.Flying
        || creep.type == TdCreepType.Friendly
      ) { continue; }

      var creepTile = grid.tiles[creep.currentGridPos.x, creep.currentGridPos.y];
      // bool tileIsNextTile = creepTile.validPath[creep.pathIdx] ? creepTile.nextPathPos[creep.pathIdx] == tile.pos : false;
      if(creep.currentGridPos == tile.pos){
         creepOccupied = true;
         break;
       }
    }

    if(creepOccupied){
      floatingNumbers.Create(
        Vector3.zero,
        Color.white,
        text: "Can't build on enemies",
        fontSize: 4,
        moveUpPct: 0f
      );
      return;
    }

    //goes to sell screen
    if(tile.tower != null && !tile.tower.isGhost){
      PlaceTower(tile);
      return;
    }
    if(!tile.buildable){
      return;
    }

    //check to see if adding a tower here would block the path
    tile.passable = false;
    var creepPositions = GetCreepPositions();
    var pathClear = grid.UpdatePathPositions(hqDestPositions, creepSpawnPoints, creepPositions);
    tile.passable = true; //then switch it back till one is actually built
    var pathStillClear = grid.UpdatePathPositions(hqDestPositions, creepSpawnPoints, null); //update back to where we were

    if(!pathClear){
      floatingNumbers.Create(
        Vector3.zero,
        Color.white,
        text: "That blocks the path!",
        fontSize: 4,
        moveUpPct: 0f
      );
    }else{
      PlaceTower(tile);
    }
  }

  void PlaceTower(TdTile tile){
    towerSelection.StartSelect(tile);
  }

  void OnTowerBuilt(TowerBuiltData data){
    BuildTower(
      new SavedTower(){
        towerPosition = data.tile.pos,
        towerType = data.towerType,
        towerLevel = 0
      }, false);

    //try to show tutorial for starting waves after towers are built
    if(stageFiveData.money == 0){
      tutorialSystem.ShowTutorial(502);
    }
  }

  static Color ghostTowerTileColor = Color.Lerp(Colors.greenGrass, Colors.transparentBlack50.SetA(0.20f), 0.05f);
  static Color towerTileColor = Color.Lerp(Colors.greenGrass, Colors.transparentBlack50, 0.25f);

  void BuildTower(SavedTower towerInfo, bool isLoad, bool isGhost = false){
    var tile = grid.GetTile(towerInfo.towerPosition);
    var type = towerInfo.towerType;
    var towerLevel = towerInfo.towerLevel;

    var towerStats = Tower.GetStats(loader, type);
    if(!isLoad){
      //Don't allow building the tower without the dough. Should only happen from previous layout setup
      if(!Tower.CanAffordTower(loader, type, towerLevel, stageFiveData)){
        return;
      }
      stageFiveData.money -= towerStats.hasMoneyCost ? towerStats.moneyCost[towerLevel] : 0;
      stageFiveData.wood -= towerStats.hasWoodCost ? towerStats.woodCost[towerLevel] : 0;
      stageFiveData.ore -= towerStats.hasOreCost ? towerStats.oreCost[towerLevel] : 0;
    }

    if(!isGhost){
      if(tile.tower != null){
        if(tile.tower.isGhost){
          Destroy(tile.tower.gameObject);
        }else{
          Debug.LogError("Trying to build on top of another tower");
          return;
        }
      }

      tile.passable = false;
      var creepPositions = GetCreepPositions();
      var successfulGridUpdate = grid.UpdatePathPositions(hqDestPositions, creepSpawnPoints, creepPositions);
      if(!successfulGridUpdate){
        //Still not sure what causes this edge case failure here where getting the error "No valid path for current creep"
        //But do a rollback and return to try to salvage it instead of all creeps not having a path all of a sudden
        tile.passable = true;
        grid.UpdatePathPositions(hqDestPositions, creepSpawnPoints, creepPositions);

        stageFiveData.money += towerStats.hasMoneyCost ? towerStats.moneyCost[towerLevel] : 0;
        stageFiveData.wood += towerStats.hasWoodCost ? towerStats.woodCost[towerLevel] : 0;
        stageFiveData.ore += towerStats.hasOreCost ? towerStats.oreCost[towerLevel] : 0;
        return;
      }
    }

    if(!isGhost && !isLoad){
      audioService.PlaySfx(towerBuiltSound);

      objectPool.Spawn(towerSpawnParticle, tile.go.transform.position, Quaternion.identity);
    }

    tile.svgRenderer.color = isGhost ? ghostTowerTileColor : towerTileColor;

    var towerPrefab = towerPrefabs[type];
    var newTower = GameObject.Instantiate(towerPrefab, tile.go.transform.position, Quaternion.identity, buildingHolder);
    var towerView = newTower.GetComponent<TowerView>();

    towerView.shotParent = shotHolder;
    towerView.creepHolder = creepHolder;
    towerView.stats = towerStats;
    towerView.grid = grid;
    towerView.isGhost = isGhost;
    towerView.data = towerInfo;
    towerView.creeps = creeps;
    towerView.Init();

    tile.tower = towerView;

    //ghetto way to upgrade towers that were saved at higher levels but oh well
    for(int u = 0; u < towerLevel; u++){
      towerView.Upgrade();
    }

    //Save the tower if it's not a load
    if(!isLoad){
      if(stageFiveData.savedTowers == null){
        stageFiveData.savedTowers = new List<SavedTower>();
      }

      stageFiveData.savedTowers.Add(towerInfo);
    }
  }

  void OnTowerSold(TowerBuiltData data){
    var populationCost = data.tile.tower.AccumulatedPopCost();

    stageFiveData.money += data.tile.tower.MoneySellValue;;
    stageFiveData.wood += data.tile.tower.WoodSellValue;
    stageFiveData.ore += data.tile.tower.OreSellValue;
    stageFiveData.assignedPopulation -= data.tile.tower.PopulationSellValue;

    //Send all the friendly creeps back to HQ, or intercept ones that are on their way to upgrade already
    var needToSendBack = populationCost;
    var enRoute = creeps.Where(c => c.type == TdCreepType.Friendly && c.goingToTower != null && c.goingToTower == data.tile.tower);
    foreach(var friendlyEnRoute in enRoute){
      needToSendBack--;
      var newPath = grid.FindPath(grid.GetTile(friendlyEnRoute.singlePathFollower.currentGridPos), grid.GetTile(hqPos));
      friendlyEnRoute.singlePathFollower.tilePath = newPath;
      friendlyEnRoute.singlePathFollower.tilePathIdx = 0;
    }
    for(int f = 0; f < needToSendBack; f++){
      var friendlyPath = grid.FindPath(data.tile, grid.GetTile(hqPos));
      var friendlyCreep = SpawnCreep(TdCreepType.Friendly, data.tile.pos, 0, -1, friendlyPath);
    }

    Destroy(data.tile.tower.gameObject);
    data.tile.svgRenderer.color = Colors.greenGrass;
    data.tile.tower = null;
    data.tile.passable = true;
    grid.UpdatePathPositions(hqDestPositions, creepSpawnPoints, null);

    var savedTower = stageFiveData.savedTowers.FirstOrDefault(st => st.towerPosition == data.tile.pos);
    stageFiveData.savedTowers.Remove(savedTower);

    audioService.PlaySfx(towerSoldSound);
  }

  void OnTowerUpgrade(TowerBuiltData data){
    var towerStats = Tower.GetStats(loader, data.towerType);
    var nextLevel = data.tile.tower.data.towerLevel + 1;
    var popCost = towerStats.hasPopCost ? towerStats.populationCost[nextLevel] : 0;
    var moneyCost = towerStats.hasMoneyCost ? towerStats.moneyCost[nextLevel] : 0;
    var woodCost = towerStats.hasWoodCost ? towerStats.woodCost[nextLevel] : 0;
    var oreCost = towerStats.hasOreCost ? towerStats.oreCost[nextLevel] : 0;

    //pay for it
    stageFiveData.money -= moneyCost;
    stageFiveData.wood -= woodCost;
    stageFiveData.ore -= oreCost;
    stageFiveData.assignedPopulation += popCost;

    data.tile.tower.StartUpgrade();

    //Make a friendly creep to go upgrade them
    var friendlyPath = grid.FindPath(grid.GetTile(hqPos), data.tile);
    var friendlyCreep = SpawnCreep(TdCreepType.Friendly, hqPos, 0, 1, friendlyPath);
    friendlyCreep.goingToTower = data.tile.tower;

    audioService.PlaySfx(towerUpgradeSound);

    // //then save
    // var savedTower = stageFiveData.savedTowers.FirstOrDefault(st => st.towerPosition == data.tile.pos);
    // savedTower.towerLevel = data.tile.tower.towerLevel;
  }

  void OnCreepDies(TdCreep creep, bool pathCompleted){
    if(creep.type == TdCreepType.Friendly){
      if(creep.goingToTower != null){
        creep.goingToTower.Upgrade();
      }
    }
    //Did they make it to HQ?
    else if(pathCompleted){
      stageFiveData.population -= creep.popDamage;
      floatingNumbers.Create(creep.gameObject.transform.position, Colors.redText, text: "-"+creep.popDamage, fontSize: 5, moveUpPct: 0.8f, punchSize: 1f, ttl: 1.5f);
      audioService.PlaySfx(creepCompletePathSound);

      if(stageFiveData.population <= 0){
        GameOver();
      }

      //downgrade any towers if the assigned population exceeds our current population now
      while(stageFiveData.assignedPopulation > stageFiveData.population){
        stageFiveData.assignedPopulation--;

        //find the last built tower that has upgraded (above tower level 0) so we can steal assigned population from
        for(int towerIndex = stageFiveData.savedTowers.Count - 1; towerIndex >= 0; towerIndex--){
          var savedTower = stageFiveData.savedTowers[towerIndex];
          if(savedTower.towerLevel > 0){
            savedTower.towerLevel--;
            grid.GetTile(savedTower.towerPosition).tower.Downgrade();
            break;
          }
        }
      }
    }else{
      if(creep.statsType == TdCreepType.Spawn){
        //Make some more friends
        var thing1 = SpawnCreep(TdCreepType.Group, creep.currentGridPos, creep.pathIdx, creep.waveNumber);
        var thing2 = SpawnCreep(TdCreepType.Group, creep.currentGridPos, creep.pathIdx, creep.waveNumber);
        if(creep.type == TdCreepType.Boss){
          var creepStats = loader.Load<TdCreepStats>(GetCreepStatsPath(TdCreepType.Group));
          thing1.SetBaseStats(creepStats, true, creep.waveNumber, stageRules);
          thing2.SetBaseStats(creepStats, true, creep.waveNumber, stageRules);
        }
      }

      stageFiveData.money += creep.money;
      floatingNumbers.Create(creep.gameObject.transform.position, Colors.yellow, text: "+"+creep.money, fontSize: 2, ttl: 0.5f);
      audioService.PlaySfx(creepDieSound);
    }
    creeps.RemoveAll(cr => cr == null || cr == creep);
    //Detect the wave being over if the last creep of a wave dies while we're not spawning
    var creepWave = creep.waveNumber;
    var creepsOfThisWaveCount = creeps.Count(c => c.waveNumber == creepWave);
    if(waveData.ContainsKey(creepWave) && !waveData[creepWave].spawning && creepsOfThisWaveCount == 0){
      waveCompleted.Dispatch(creepWave);
    }
  }

  void GameOver(){
    //prevent doubling up from multiple creeps dying or something
    if(gameOverScreen.activeInHierarchy){
      //If multiple creeps die on the same frame there's a bug where your population is negative for the next round
      //Hopefully this fixes that
      stageFiveData.population = startingPopulation;
      stageFiveData.assignedPopulation = 0;
      return;
    }

    audioService.PlaySfx(defeatedSound);
    audioService.PlayMusic(ambientMusic);
    time.Pause();
    var lastFinishedWave = stageFiveData.lastCompletedWave;
    gameOverText.text = "You made it to wave " + lastFinishedWave;
    if(lastFinishedWave > stageFiveData.stageHighScore){
      stageFiveData.stageHighScore = lastFinishedWave;
      gameOverText.text += "\nNEW HIGH SCORE!";
    }

    gameOverScreen.SetActive(true);
    stageFiveData.previousRunSavedTowers = stageFiveData.savedTowers;
    ResetStageData();
  }

  void GameOverContinue(){
    Cleanup();
    tutorialSystem.ShowTutorial(504);
  }

  void GridInit(){
    for(int x = 0; x < grid.tiles.GetLength(0); x++){
      for(int y = 0; y < grid.tiles.GetLength(1); y++){
        grid.tiles[x, y].tower = null;
        grid.SetTileTerrain(new Int2(x, y), TileTerrain.Grass);
      }
    }
    foreach(var waterStart in StageFiveManager.waterStartPositions){
      grid.SetTileTerrain(waterStart, TileTerrain.Water);
    }
    foreach(var tile in creepSpawnPoints){
      grid.SetTileTerrain(tile, TileTerrain.Spawn);
    }
    foreach(var tile in orePoints){
      grid.SetTileTerrain(tile, TileTerrain.Ore);
    }
    foreach(var tile in forestPoints){
      grid.SetTileTerrain(tile, TileTerrain.Forest);
    }
    grid.tiles[hqPos.x, hqPos.y].passable = false;
    grid.UpdatePathPositions(hqDestPositions, creepSpawnPoints, GetCreepPositions());
  }

  int GetTotalToSpawn(int waveStage){
    var toSpawn = 0;
    var rules = stageRules.GetStageFiveRules(waveStage, metaGameData.victoryCount);
    var creepStats = loader.Load<TdCreepStats>(GetCreepStatsPath(rules.creepType));

    toSpawn = (
      creepStats.creepsPerSpawnPoint +
      rules.additionalCreepsPerWave +
      Mathf.RoundToInt((float)waveStage * creepStats.creepsPerWaveMultiplier)
    ) * creepSpawnPoints.Length;

    if(rules.creepType == TdCreepType.Boss && TdCreep.GetBossSubType(waveStage, metaGameData.victoryCount, stageRules) == TdCreepType.Group){
      toSpawn = (2 + (waveStage / 10)) * creepSpawnPoints.Length;
    }

    return toSpawn;
  }

  TdCreepType GetWaveCreepType(int waveStage){
    var rules = stageRules.GetStageFiveRules(waveStage, metaGameData.victoryCount);
    if(rules.creepType == TdCreepType.Boss){
      return TdCreep.GetBossSubType(waveStage, metaGameData.victoryCount, stageRules);
    }
    return rules.creepType;
  }

  void StartSpawning(){
    lastStartedWave++;
    var waveToStart = lastStartedWave;

    var curRules = stageRules.GetStageFiveRules(waveToStart, metaGameData.victoryCount);
    var totalToSpawn = GetTotalToSpawn(waveToStart);
    var creepStats = loader.Load<TdCreepStats>(GetCreepStatsPath(curRules.creepType));

    var newWaveData = new WaveData(){
      waveNumber = waveToStart,
      inProgress = true,
      spawning = true,
      totalSpawned = 0,
      totalToSpawn = totalToSpawn,
      spawnAccum = float.MaxValue,
      //calculate how much time it'll take to spawn this wave based on the delay and spawn points
      waveSpawnTime = (totalToSpawn / creepSpawnPoints.Length) * creepStats.perCreepDelay,
      totalTimeAccum = 0,
    };

    waveDisplay.PlayNextWave(newWaveData.waveSpawnTime);

    waveData[waveToStart] = newWaveData;

    //mark all towers as being used for selling
    var tiles = grid.GetTiles();
    foreach(var tile in tiles){
      if(tile.tower != null){
        tile.tower.data.hasBeenUsed = true;
      }
    }

    //see if we get a gold bonus for starting a wave early
    if(waveData.ContainsKey(waveToStart - 1) && creeps.Count > 0){
      var spawnTimeRemaining = waveData[waveToStart - 1].waveSpawnTime - waveData[waveToStart - 1].totalTimeAccum;

      //base creep bonus on gold amount
      var totalCreepGold = creeps.Sum(c => c.money);
      //The creeps count will include friendlies but should be insignificant and not worth the perf hit to filter
      var bonusGold = Mathf.RoundToInt(spawnTimeRemaining * waveToStart) + totalCreepGold / 7;

      if(bonusGold > 0){
        // Debug.Log("Bonus gold: " + bonusGold);
        stageFiveData.money += bonusGold;
        floatingNumbers.Create(nextWaveButton.transform.position.Add(0.2f, 0.2f, 0), Colors.yellow, text: "+"+bonusGold, fontSize: 4, ttl: 0.5f);
      }
    }

    audioService.PlaySfx(startWaveSound);
    audioService.PlayMusic(creepStats.music);
  }

  void UpdateSpawn(){

    foreach(var wd in waveData){
      var waveNumber = wd.Key;
      var curWave = wd.Value;

      if(!curWave.spawning){ continue; }

      var curRules = stageRules.GetStageFiveRules(waveNumber, metaGameData.victoryCount);
      var creepStats = loader.Load<TdCreepStats>(GetCreepStatsPath(curRules.creepType));

      curWave.totalTimeAccum += Time.deltaTime;
      curWave.spawnAccum += Time.deltaTime;

      if(curWave.spawnAccum < creepStats.perCreepDelay){ continue; }

      curWave.spawnAccum = 0f;

      for(int spi = 0; spi < creepSpawnPoints.Length; spi++){
        var spawnPoint = creepSpawnPoints[spi];

        SpawnCreep(curRules.creepType, spawnPoint, spi / hqDestPositions.Length, waveNumber);

        curWave.totalSpawned++;
      }

      if(curWave.totalSpawned == curWave.totalToSpawn){
        curWave.spawning = false;
      }
    }
  }

  TdCreep SpawnCreep(TdCreepType type, Int2 spawnPoint, int pathIdx, int waveNumber, List<TdTile> tilePath = null){
    var prefabType = type == TdCreepType.Boss ? TdCreep.GetBossSubType(waveNumber, metaGameData.victoryCount, stageRules) : type;

    var newCreep = objectPool.Spawn(creepPrefabs[prefabType], Vector3.one, Quaternion.identity);
    newCreep.transform.SetParent(creepHolder, false);
    newCreep.transform.localScale = Vector3.one;

    var creep = newCreep.GetComponent<TdCreep>();
    creep.Reset();
    creep.type = type;
    var statsType = type;
    var isBoss = type == TdCreepType.Boss;
    if(isBoss){
      statsType = TdCreep.GetBossSubType(waveNumber, metaGameData.victoryCount, stageRules);
    }
    var creepStats = loader.Load<TdCreepStats>(GetCreepStatsPath(statsType));
    creep.SetBaseStats(creepStats, isBoss, waveNumber, stageRules);
    creep.objectPool = objectPool;
    creep.waveNumber = waveNumber;
    creep.statsType = statsType;

    if(creep.statsType == TdCreepType.Flying){
      var flyingFollower = newCreep.GetComponent<FlyingFollower>();
      flyingFollower.grid = grid;
      flyingFollower.SetTilePos(spawnPoint);
      flyingFollower.destination = grid.GetTile(hqPos);

      flyingFollower.OnPathCompleted += creep.OnPathCompleted;
    }else if(creep.type == TdCreepType.Friendly){
      var singlePathFollower = newCreep.GetComponent<SinglePathFollower>();
      singlePathFollower.grid = grid;
      singlePathFollower.SetTilePos(spawnPoint);
      singlePathFollower.tilePath = tilePath;

      singlePathFollower.OnPathCompleted += creep.OnPathCompleted;

      //give em a speed boost if there's no wave going on
      if(waveData.Values.Count == 0){
        creep.speed *= 2.5f;
      }
    } else{
      var gpf = newCreep.GetComponent<GridPathFollower>();
      gpf.grid = grid;
      gpf.pathIdx = pathIdx;
      gpf.SetTilePos(spawnPoint);

      gpf.OnPathCompleted += creep.OnPathCompleted;
    }

    creep.OnDead += OnCreepDies;
    creeps.Add(creep);
    return creep;
  }

  public static string GetCreepStatsPath(TdCreepType creepType){
    return "Prefabs/5/CreepStats/" + creepType;
  }

  void DrawPathLines(int waveStage){
    pathLinesHolder.DestroyChildren();
    var creepType = GetWaveCreepType(waveStage);
    var creepsSpawning = GetTotalToSpawn(waveStage) / creepSpawnPoints.Length;

    for(int spi = 0; spi < creepSpawnPoints.Length; spi++){
      var spawnPoint = creepSpawnPoints[spi];
      var pathIdx = spi / hqDestPositions.Length;

      var spawnTile = grid.GetTile(spawnPoint);
      var finalPosition = grid.GetTile(hqPos).go.transform.position;

      var newLine = GameObject.Instantiate(
        pathLinePrefab,
        Vector3.zero,
        Quaternion.identity
      );
      newLine.transform.SetParent(pathLinesHolder, false);
      var lineRenderer = newLine.GetComponent<LineRenderer>();

      lineRenderer.startColor = spawnPointColors[spi];
      lineRenderer.endColor = spawnPointColors[spi];

      var shaderChanger = newLine.GetComponent<ShaderChanger>();
      shaderChanger.changeAmt += 0.05f * spi;

      if(creepType == TdCreepType.Flying){
        lineRenderer.SetPositions(new Vector3[]{
          finalPosition,
          spawnTile.go.transform.position,
        });
      }else{
        var creepPath = new List<Vector3>();
        var curTile = spawnTile;

        do{
          creepPath.Add(curTile.go.transform.position);
          curTile = grid.tiles[curTile.nextPathPos[pathIdx].x, curTile.nextPathPos[pathIdx].y];
        }while(curTile.validPath[pathIdx]);

        //add in the hq real position at the end of the path
        creepPath.Add(finalPosition);

        //Reverse the array because of the dotted line shader marching thing which only goes positive and one way
        //without messing up
        creepPath.Reverse();

        lineRenderer.positionCount = creepPath.Count;
        lineRenderer.SetPositions(creepPath.ToArray());

      }

      LeanTween.color(newLine, Colors.transparentWhite, 5f).setDelay(2f);
      var destroyAfter = newLine.AddComponent<DestroyAfter>();
      destroyAfter.timeToLive = 7f;

      //create text objs for creep numbers
      var numCreepsObj = GameObject.Instantiate(
        pathNumCreepsPrefab,
        Vector3.zero,
        Quaternion.identity
      );
      numCreepsObj.transform.SetParent(pathLinesHolder, false);
      numCreepsObj.transform.position = spawnTile.go.transform.position;
      var numCreepsText = numCreepsObj.GetComponent<TMP_Text>();
      numCreepsText.text = creepsSpawning.ToString();

      LeanTween.colorText(numCreepsObj, Colors.transparentWhite, 5f).setDelay(2f);
      var destroyAfterNum = numCreepsObj.AddComponent<DestroyAfter>();
      destroyAfterNum.timeToLive = 7f;
    }
  }

  HashSet<Int2>[] creepPositions;
  HashSet<Int2>[] GetCreepPositions(){
    if(creeps.Count == 0){ return null; }

    //Reset and initialize
    if(creepPositions == null){
      creepPositions = new HashSet<Int2>[hqDestPositions.Length];
    }
    for(var pathIdx = 0; pathIdx < creepPositions.Length; pathIdx++){
      if(creepPositions[pathIdx] == null){
        creepPositions[pathIdx] = new HashSet<Int2>();
      }else{
        creepPositions[pathIdx].Clear();
      }
    }

    foreach(var creep in creeps){
      if(creep.statsType == TdCreepType.Flying || creep.statsType == TdCreepType.Friendly){
        continue;
      }
      //Add both current position and next grid position to avoid bugs where you place a tower right where
      //A creep is going next frame
      var pathIdx = creep.gridPathFollower.pathIdx;
      TdTile curTile = grid.tiles[creep.gridPathFollower.currentGridPos.x, creep.gridPathFollower.currentGridPos.y];
      TdTile nextTile = grid.tiles[curTile.nextPathPos[pathIdx].x, curTile.nextPathPos[pathIdx].y];

      creepPositions[pathIdx].Add(curTile.pos);
      creepPositions[pathIdx].Add(nextTile.pos);
    }

    return creepPositions;
  }

  void OnOpenTutorialFinished(int tutorialId){
    //Show the next building tutorial in the series and set up initial ghost towers
    if(tutorialId == 500){
      stageFiveData.previousRunSavedTowers = new List<SavedTower>(){
        new SavedTower(){ towerPosition = new Int2(4, 4), towerType = TdTowerType.Cheap },
        new SavedTower(){ towerPosition = new Int2(4, 5), towerType = TdTowerType.Cheap },
        new SavedTower(){ towerPosition = new Int2(4, 9), towerType = TdTowerType.Cheap },
        new SavedTower(){ towerPosition = new Int2(4, 10), towerType = TdTowerType.Cheap },

        new SavedTower(){ towerPosition = new Int2(6, 4), towerType = TdTowerType.Cheap },
        new SavedTower(){ towerPosition = new Int2(6, 5), towerType = TdTowerType.Cheap },
        new SavedTower(){ towerPosition = new Int2(6, 9), towerType = TdTowerType.Cheap },
        new SavedTower(){ towerPosition = new Int2(6, 10), towerType = TdTowerType.Cheap },
      };

      PrevLayout();

      tutorialSystem.ShowTutorial(501);
    }
  }
}
