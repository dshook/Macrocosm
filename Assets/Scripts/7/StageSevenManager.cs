using TMPro;
using UnityEngine;
using strange.extensions.mediation.impl;
using System.Collections.Generic;
using System.Linq;
using PygmyMonkey.ColorPalette;
using MoreMountains.NiceVibrations;

public class StageSevenManager : View, IStageManager {

  [Inject] ResourceLoaderService loader {get; set;}
  [Inject] StageTransitionService stageTransition { get; set; }
  [Inject] StageTransitionModel stageTransitionData { get; set; }
  [Inject] GameSaverService gameSaver {get; set;}
  [Inject] StageRulesService stageRules { get; set; }
  [Inject] StageSevenDataModel stageSevenData {get; set;}
  [Inject] InputService input {get; set;}
  [Inject] ClickSignal clickSignal {get; set;}
  [Inject] ColonizeCelestialSignal colonizeCelestial { get; set; }
  [Inject] CelestialColonizedSignal celestialColonized { get; set; }
  [Inject] GalaxyShipDoneSignal shipDone {get; set;}
  [Inject] CreateShipSignal createShipSignal {get; set;}
  [Inject] DestroyShipSignal destroyShipSignal {get; set;}
  [Inject] GalaxyBuildingFinishedSignal buildingFinishedSignal {get; set;}
  [Inject] VictorySignal victorySignal {get; set;}
  [Inject] GalaxyRouteCache routeCache {get; set;}
  [Inject] ObjectPool objectPool {get; set;}
  [Inject] TutorialSystem tutorialSystem {get; set;}
  [Inject] OpenTutorialFinishedSignal openTutorialFinishedSignal { get; set; }
  [Inject] GalaxyBgStarsFinishedCreatingSignal galaxyBgStarsFinishedCreatingSignal { get; set; }
  [Inject] FloatingText floatingText { get; set; }
  [Inject] CameraService cameraService { get; set; }
  [Inject] AudioService audioService { get; set; }
  [Inject] SendErrorUserReportSignal sendErrorUserReport { get; set; }
  [Inject] SettingsDataModel settings { get; set; }

  public GalaxyGenerator galaxyGenerator;
  public GalaxySpawner galaxySpawner;
  public CameraPanner camPanner;
  public GalaxyTransitioner galaxyTransitioner;
  public GalaxyRouteManager galaxyRouteManager;
  public GalaxyTransportShipsManager transportShipsManager;
  public CelestialManager celestialManager;

  public Transform shipHolder;

  public AudioClip buildingFinishedClip;
  public AudioClip systemColonizedClip;

  public AudioClip music;

  Galaxy galaxy;
  Bounds mapBounds;

  Vector3? prevCameraPos;
  float? prevCameraZoom;

  //Only for tracking transitions and state when transitioning away
  uint? selectedStarId = null;
  uint? selectedCbId = null;
  Vector3 transitionAwayCameraPosition = Vector3.zero;

  protected override void Awake () {
    base.Awake();

    colonizeCelestial.AddListener(OnSettleSignal);
    shipDone.AddListener(OnShipDoneSingal);
    destroyShipSignal.AddListener(OnDestroyShipSignal);
    createShipSignal.AddListener((data, isNew) => CreateShip(data, isNew));
    buildingFinishedSignal.AddListener(OnBuildingFinishedSignal);
    openTutorialFinishedSignal.AddListener(OnOpenTutorialFinished);
    victorySignal.AddListener(OnVictory);
  }

  // void OnDrawGizmos()
  // {
  //   Gizmos.color = Color.red;
  //   if(galaxy != null){
  //     Gizmos.DrawWireSphere(Vector3.zero, galaxy.radius);
  //   }
  // }

  public void Init(bool isInitialCall){
    if(stageSevenData.achievedVictory){
      victorySignal.Dispatch(false);
      return;
    }

    //Reset time rate to normal when starting up
    stageSevenData.timeRate = stageRules.StageSevenRules.baseYearTimeRate;

    if(stageSevenData.mapSeed == 0){
      stageSevenData.mapSeed = stageRules.StageSevenRules.startingMapSeed;
    }

    galaxyGenerator.seed = stageSevenData.mapSeed;
    galaxyGenerator.useFixedSeed = true;
    galaxy = galaxyGenerator.GenerateMap();

    galaxySpawner.objectPool = objectPool;
    galaxySpawner.galaxyBgStarsFinishedCreatingSignal = galaxyBgStarsFinishedCreatingSignal;
    galaxySpawner.StartCreatingStars(galaxy, stageSevenData);

    mapBounds = galaxy.GetBounds();
    camPanner.bounds = mapBounds;

    routeCache.galaxy = galaxy;
    galaxyTransitioner.galaxy = galaxy;
    galaxyRouteManager.galaxy = galaxy;
    transportShipsManager.galaxy = galaxy;
    celestialManager.galaxy = galaxy;

    //create starting settlement if needed
    if(stageSevenData.settlements.Count == 0){
      var startingInfo = galaxyGenerator.FindStartingPlanet(galaxy);
      if(startingInfo == null){
        Debug.LogError("Lol couldn't find a starting planet with seed: " + galaxyGenerator.seed);
        return;
      }else{
        Debug.Log("Found starting planet: " + startingInfo.Value.star.id + " planetId:" + startingInfo.Value.planetData.id);
      }

      selectedStarId = startingInfo.Value.star.id;
      selectedCbId = startingInfo.Value.planetData.id;
      galaxyGenerator.SetStartingSystemSpecialSauce(startingInfo.Value);
      galaxySpawner.SpawnStar(galaxy, startingInfo.Value.star);
      var startingStar = galaxy.stars[startingInfo.Value.star.id];
      SettlePlanet(startingStar, startingInfo.Value.planetData, null);

      //Extra starting state
      stageSevenData.settlements[startingInfo.Value.planetData.id].buildings[GalaxyBuildingId.Settlement2] = 1;
      stageSevenData.settlements[startingInfo.Value.planetData.id].buildings[GalaxyBuildingId.Settlement3] = 1;
    }else{
      foreach(var existingSettlement in stageSevenData.settlements.Values){
        var star = galaxy.stars.TryGet(existingSettlement.parentStarId);
        if(star == null){
          HandleInvalidGalaxyState("Init star null " + existingSettlement.parentStarId);
          return;
        }

        var cb = galaxy.celestials.TryGet(existingSettlement.parentCelestialId);
        if(cb == null){
          HandleInvalidGalaxyState("Init cb null " + existingSettlement.parentCelestialId);
          return;
        }

        //Make sure we don't have any resources that aren't used anymore
        var hasOldResource = existingSettlement.resources != null && (
          existingSettlement.resources.ContainsKey(GameResourceType.Energy) ||
          existingSettlement.resources.ContainsKey(GameResourceType.Food) ||
          existingSettlement.resources.ContainsKey(GameResourceType.Population)
        );

        if(hasOldResource){
          HandleInvalidGalaxyState("Old Resource");
          return;
        }

        SettlePlanet(star, cb, existingSettlement);
      }
      foreach(var starSettlement in stageSevenData.starSettlements.Values){
        var star = galaxy.stars.TryGet(starSettlement.starId);
        //This one should never happen from the check above but just to be safe...
        if(star == null){
          HandleInvalidGalaxyState("Star null for star settlement " + starSettlement.starId);
          return;
        }
        star.settlementData = starSettlement;
      }

      var firstSettlement = stageSevenData.settlements.First().Value;
      galaxyGenerator.SetStartingSystemSpecialSauce(new StarPlanet(){
        star = galaxy.generatedStars[firstSettlement.parentStarId],
        planetData = galaxy.celestials[firstSettlement.parentCelestialId]
      });
      var startingStar = galaxy.stars.TryGet(firstSettlement.parentStarId);
      if(startingStar != null){
        //Need to update display if it's spawned already so name is consistent
        startingStar.UpdateDisplay(StageSevenManager.StarPalette);
      }
      selectedStarId = firstSettlement.parentStarId;
      selectedCbId = firstSettlement.parentCelestialId;
    }

    //recreate ships
    foreach(var ship in stageSevenData.ships){
      CreateShip(ship, false);
    }

    //recreate routes
    galaxyRouteManager.Init(isInitialCall, galaxy);

    galaxyTransitioner.GoToStarAndCb(selectedStarId, selectedCbId, transitionAwayCameraPosition);

    TryMigrateTransportLimitData();
    if(!settings.lowQuality){
      galaxySpawner.StartCreatingBgStars(galaxy);
    }

  }

  public void OnTransitionTo(StageTransitionData data){
    if(!data.fromMenu){
      camPanner.ResetDragVelocity();

      galaxyTransitioner.GoToStarAndCb(selectedStarId, selectedCbId, transitionAwayCameraPosition);
    }

    clickSignal.AddListener(OnClick);
    stageSevenData.timeRate = 0f;

    audioService.PlayMusic(music);
  }

  public void OnTransitionAway(bool toMenu){
    if(clickSignal != null){
      clickSignal.RemoveListener(OnClick);
    }

    if(!toMenu){
      //save galaxy transitioner state so we can restore it on the transition to
      selectedStarId = galaxyTransitioner.SelectedStar != null ? galaxyTransitioner.SelectedStar.generatedData.id : (uint?)null;
      selectedCbId = galaxyTransitioner.SelectedCb != null ? galaxyTransitioner.SelectedCb.data.id : (uint?)null;
      transitionAwayCameraPosition = cameraService.Cam.transform.localPosition;

      cameraService.Cam.transform.localPosition = new Vector3(0, 0, cameraService.Cam.transform.localPosition.z);
      camPanner.ResetZoom();
    }
    audioService.StopMusic();
  }

  public void Cleanup(){
    galaxySpawner.Clear();
    if(galaxy != null){
      galaxy.stars.Clear();
      galaxy.bgStars.Clear();
      galaxyTransitioner.TransitionToGalaxy(null, true);
      galaxyTransitioner.Cleanup();
    }
    shipHolder.DestroyChildren();
    galaxyRouteManager.Cleanup();
  }

  void Update () {
    if(stageSevenData.achievedVictory){
      return;
    }

    if(stageSevenData.timeRate > 0 && stageSevenData.year > 1){
      tutorialSystem.ShowTutorial(702, 1f);
    }

    //Time marches on... at least while you're not paused
    var timeAmt = Time.deltaTime * stageSevenData.timeRate;

    var curYear = (int)stageSevenData.year;
    stageSevenData.year += timeAmt;
    var isNewYear = (int)stageSevenData.year != curYear;

    //update all the settlements
    if(timeAmt != 0){
      foreach(var settlement in stageSevenData.settlements.Values){
        var cbData = galaxy.celestials.TryGet(settlement.parentCelestialId);
        if(cbData == null){
          HandleInvalidGalaxyState("cbData null " + settlement.parentCelestialId);
          return;
        }
        settlement.UpdateResourceDeltas(stageRules, cbData);

        UpdateBuildQueue(
          ref settlement.buildQueue,
          ref settlement.buildings,
          ref stageSevenData.starSettlements[settlement.parentStarId].resources,
          timeAmt,
          settlement.parentStarId,
          settlement.parentCelestialId
        );
        if(isNewYear){
          var parentSettlement = stageSevenData.starSettlements.TryGet(settlement.parentStarId);
          if(parentSettlement == null){
            HandleInvalidGalaxyState("Parent Settlement null " + settlement.parentStarId);
            return;
          }
          settlement.UpdateNewYearState(stageRules, cbData, parentSettlement);
        }
      }

      //update any inhabited systems
      foreach(var starSettlement in stageSevenData.starSettlements.Values){
        UpdateBuildQueue(
          ref starSettlement.buildQueue,
          ref starSettlement.buildings,
          ref starSettlement.resources,
          timeAmt,
          starSettlement.starId,
          null
        );
        if(isNewYear){
          starSettlement.UpdateNewYearState(stageRules);
        }
      }
    }

    CheckTutorials();

#if UNITY_EDITOR
    if(Input.GetKeyDown(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.R)){
      var selectedCelestial = galaxy.celestials
        .Where(s => s.Value.resourceDeposits != null && s.Value.resourceDeposits.Length == 3)
        .Skip(debugSkipCount)
        .FirstOrDefault();

      galaxyTransitioner.TransitionToSystem(galaxy.stars[selectedCelestial.Value.parentStarId]);
      debugSkipCount++;
    }
#endif

  }

  int debugSkipCount = 0;

  //hack to reset all the data if we get into an invalid state with generation changing at the moment
  void HandleInvalidGalaxyState(string reason){
    Debug.LogWarning("Invalid galaxy state, resetting. " + reason);
    sendErrorUserReport.Dispatch($"Invalid galaxy ({stageSevenData.mapSeed}) state: " + reason);
    Cleanup();
    gameSaver.ResetStageData(7);
    Init(false);
    floatingText.CreateUI("Resetting for major update, sorry!", Color.white, false, 5f);
  }

  void OnClick(Vector2 pos){
    if(galaxyRouteManager.Editing){
      //Route manager will handle all clicks when editing
      return;
    }

    RaycastHit2D hit = Physics2D.Raycast(input.pointerWorldPosition, Vector3.forward);

    Star star = null;
    CelestialBody cb = null;
    // If it hits something...
    if(hit.collider != null){
      //use parent directly to only go up a single level
      cb = hit.transform.parent.GetComponent<CelestialBody>();
      star = hit.transform.parent.GetComponent<Star>();
    }

    //Prevent clicking to cb before it's explored
    if(cb != null && cb.IsExplored){
      MMVibrationManager.Haptic(HapticTypes.LightImpact);
      if(cb == galaxyTransitioner.SelectedCb){
        //planet/moon -> system
        galaxyTransitioner.TransitionToSystem(cb.star);
      }else{
        //system -> planet/moon
        galaxyTransitioner.TransitionToCelestialBody(cb);
      }
    }else if(star != null){
      MMVibrationManager.Haptic(HapticTypes.LightImpact);
      if(star == galaxyTransitioner.SelectedStar){
        //system -> galaxy
        galaxyTransitioner.TransitionToGalaxy(star);
      }else{
        //galaxy -> system
        galaxyTransitioner.TransitionToSystem(star);
      }
    }
  }

  void OnSettleSignal(GalaxySettlementCreationData creationData){

    var colonyTemplate = GalaxyBuilding.allBuildings[creationData.colonyBuilding];

    var newShipData = new GalaxyShipData{
      type = GalaxyShipType.Colony,
      shipTypeId = creationData.colonyBuilding,
      sourceStarId = creationData.sourceStarId,
      sourceCelestialBodyId = creationData.sourceCelestialId,
      destStarId = creationData.destStarId,
      destCelestialBodyId = creationData.destCelestialId,
    };
    var newColony = CreateShip(newShipData, true);
    newColony.StartMoving();
  }

  void OnShipDoneSingal(GalaxyShip ship){
    if(ship.data.type == GalaxyShipType.Colony){
      var star = galaxy.stars[ship.data.destStarId];
      var cb = galaxy.celestials[ship.data.destCelestialBodyId];
      SettlePlanet(star, cb);
      destroyShipSignal.Dispatch(ship);
    }
  }

  void SettlePlanet(Star star, CelestialBodyData cbData, GalaxySettlementData existingData = null){

    //Create a brand new settlement not from loaded data
    if(existingData == null){
      var resources = new Dictionary<GameResourceType, GalaxyResource>();

      //Create new resources for the world by copying over the generated galaxy abundances into the starting amounts
      if(cbData.resourceDeposits != null){
        foreach(var cbRes in cbData.resourceDeposits){
          resources[cbRes.type] = new GalaxyResource(){
            type = cbRes.type,
            amount = 0,
            totalAmount = GalaxyResource.startingAmount[cbRes.abundance]
          };
        }
      }

      var newSettlement = new GalaxySettlementData(){
        parentCelestialId = cbData.id,
        parentStarId = star.generatedData.id,
        resources = resources,
        resourceDeltas = new Dictionary<GameResourceType, GameResource>(GameResource.gameResourceTypeComparer),
        buildings = new Dictionary<GalaxyBuildingId, int>(GalaxyBuilding.buildingComparer) {
          {GalaxyBuildingId.Miner0, 1},
          {GalaxyBuildingId.Settlement1, 1},
        }
      };

      stageSevenData.settlements[newSettlement.parentCelestialId] = newSettlement;
      newSettlement.UpdateResourceDeltas(stageRules, cbData); //could be unneccesary here

      if(stageSevenData.settlements.Count == 2){
        tutorialSystem.ShowTutorial(711, 1f);
      }

      audioService.PlaySfx(systemColonizedClip);
    }

    //Create new star settlement data if this is the first time this star has been settled
    if(!stageSevenData.starSettlements.ContainsKey(star.generatedData.id) ){
      stageSevenData.starSettlements[star.generatedData.id] = new StarSettlementData(){
        starId = star.generatedData.id,
        foundedAt = stageSevenData.year,
        resources = new Dictionary<GameResourceType, GalaxyResource>(GameResource.gameResourceTypeComparer),
        buildings = new Dictionary<GalaxyBuildingId, int>(GalaxyBuilding.buildingComparer) {
          {GalaxyBuildingId.Market0, 1},
        }
      };

      if(stageSevenData.starSettlements.Count == 2){
        tutorialSystem.ShowTutorial(713, 1f);
      }
    }

    star.settlementData = stageSevenData.starSettlements[star.generatedData.id];

    if(!stageSevenData.starData.ContainsKey(star.generatedData.id)){
      //For starting world mostly
      stageSevenData.starData[star.generatedData.id] = new StarData(){ exploreStatus = StarExploreStatus.Seen };
    }

    star.generatedData.inhabited = true;

    celestialColonized.Dispatch(stageSevenData.settlements[cbData.id]);
  }

  void UpdateBuildQueue(
    ref List<GalaxyBuildingData> buildQueue,
    ref Dictionary<GalaxyBuildingId, int> buildings,
    ref Dictionary<GameResourceType, GalaxyResource> resources,
    float timeAmt,
    uint starId,
    uint? celestialId
  ){
    if(buildQueue.Count == 0){
      return;
    }
    var curBuildInfo = buildQueue[0];
    var currentBuilding = GalaxyBuilding.allBuildings[curBuildInfo.buildingId];

    if(curBuildInfo.finished){


      if(currentBuilding.onApply != null){
        currentBuilding.onApply(stageSevenData);
      }

      //Nasty to have to do this but the star system buildings are a dictionary to support
      buildings.AddOrUpdate(curBuildInfo.buildingId, 1, i => i + 1 );

      buildingFinishedSignal.Dispatch(curBuildInfo, starId, celestialId);

      //now pop from the queue and onto the next thing
      buildQueue.RemoveAt(0);
      // UpdateBuildQueue(timeAmt);
      return;
    }

    if(curBuildInfo.started){
      curBuildInfo.progress += timeAmt * (1f / currentBuilding.buildTime);
      if(curBuildInfo.progress >= 1f){
        curBuildInfo.finished = true;
        //let it sit in the finished state for a frame before being applied
        return;
      }
    }

    //try to scoop up the resources needed to start building otherwise
    if(!curBuildInfo.started && !curBuildInfo.finished){
      //check to see if resources are fulfilled and mark as started if so
      var hasAllResources = true;
      foreach(var rc in currentBuilding.resourceCosts(stageRules.StageSevenRules)){
        if(!resources.ContainsKey(rc.type) || resources[rc.type].amount < rc.amount){
          hasAllResources = false;
          break;
        }
      }
      if(hasAllResources){
        curBuildInfo.started = true;
        foreach(var resourceCost in currentBuilding.resourceCosts(stageRules.StageSevenRules)){
          resources[resourceCost.type].amount -= resourceCost.amount;
        }
      }
    }
  }

  GalaxyShip CreateShip(GalaxyShipData shipData, bool isNew){
    var validPath = shipData.ValidatePath(galaxy);

    if(isNew){
      stageSevenData.ships.Add(shipData);

      //Space is big when sending first interstellar colony
      if(shipData.type == GalaxyShipType.Colony && !stageSevenData.starSettlements.ContainsKey(shipData.sourceStarId)){
        tutorialSystem.ShowPopoutTutorial("7-space-is-big", "Remember that space is reaally big and it takes awhile to get around!");
      }
    }

    if(!GalaxyShipData.shipPrefabPaths.ContainsKey(shipData.shipTypeId)){
      Debug.LogWarning("Missing ship prefab for " + shipData.shipTypeId);
      return null;
    }

    var shipPrefab = loader.Load<GameObject>(GalaxyShipData.shipPrefabPaths[shipData.shipTypeId]);
    var newShipGo = GameObject.Instantiate(shipPrefab, Vector3.zero, Quaternion.identity, shipHolder);

    var galaxyShip = newShipGo.GetComponent<GalaxyShip>();

    galaxyShip.galaxy = galaxy;
    galaxyShip.data = shipData;
    galaxyShip.Init();

    galaxy.ships.Add(galaxyShip);

    return galaxyShip;
  }

  void OnDestroyShipSignal(GalaxyShip ship){
    Destroy(ship.gameObject);
    stageSevenData.ships.Remove(ship.data);
    galaxy.ships.Remove(ship);
  }

  public static ColorPalette StarPalette{
    get{ return ColorPaletteData.Singleton.fromName("Stage 7 Stars"); }
  }
  public static ColorPalette StarPaletteInverted{
    get{ return ColorPaletteData.Singleton.fromName("Stage 7 Stars Invert"); }
  }
  public static ColorPalette PlanetRingPalette{
    get{ return ColorPaletteData.Singleton.fromName("Stage 7 Planet Rings"); }
  }
  public static ColorPalette VictoryLayersPalette{
    get{ return ColorPaletteData.Singleton.fromName("Stage 7 Victory Layers"); }
  }

  void OnBuildingFinishedSignal(GalaxyBuildingData data, uint starId, uint? celestialId){
    var buildingTemplate = GalaxyBuilding.allBuildings[data.buildingId];
    if(buildingTemplate.isVictoryBuilding){
      //Whoooo!
      stageSevenData.achievedVictory = true;
      victorySignal.Dispatch(true);
    }

    //Applying the star settlement bonus here for lack of better place
    if(buildingTemplate.factoryEfficiencyBonus != 0){
      var starSettlement = stageSevenData.starSettlements[starId];
      starSettlement.factoryEfficiencyBonus += buildingTemplate.factoryEfficiencyBonus;
    }

    //tutorial check after first miner has finished
    if(data.buildingId == GalaxyBuildingId.Miner1){
      tutorialSystem.ShowTutorial(704, 1f);
    }
    //tutorial check after first system market finished
    if(data.buildingId == GalaxyBuildingId.Market1){
      tutorialSystem.ShowTutorial(712, 1f);
    }

    if(buildingTemplate.isSystemBuilding && stageSevenData.viewMode == GalaxyViewMode.System){
      audioService.PlaySfx(buildingFinishedClip);
    }
    if(!buildingTemplate.isSystemBuilding && stageSevenData.viewMode == GalaxyViewMode.Planet){
      audioService.PlaySfx(buildingFinishedClip);
    }
  }

  //For handling old saves that haven't set the global limits for transports based on the buildings they've built
  //Could remove before release
  void TryMigrateTransportLimitData(){
    if(stageSevenData.buildingLimits != null && stageSevenData.buildingLimits.Keys.Count > 0){
      return;
    }

    if(stageSevenData.buildingLimits == null){
      stageSevenData.buildingLimits = new Dictionary<GalaxyBuildingId, uint>();
    }

    foreach(var starSettlement in stageSevenData.starSettlements){
      foreach(var building in starSettlement.Value.buildings){
        var buildingTemplate = GalaxyBuilding.allBuildings[building.Key];
        if(GalaxyBuilding.allMarkets.Contains(building.Key) && buildingTemplate.onApply != null){
          buildingTemplate.onApply(stageSevenData);
        }
      }
    }
  }

  void OnOpenTutorialFinished(int tutorialId){
    if(tutorialId == 700){
      tutorialSystem.ShowTutorial(701, 1f);
    }
    if(tutorialId == 717){
      tutorialSystem.ShowTutorial(718);
    }
  }

  //For random tips that have certain conditions but don't need to be triggered at specific moments
  void CheckTutorials(){
    if(Time.frameCount % 50 != 0){ return; }

    //Reminder that space is really big when sending your first colony ship
    if(
      stageSevenData.viewMode == GalaxyViewMode.Galaxy
      && stageSevenData.timeRate != 0
      && stageSevenData.ships.Any(ship => ship.shipTypeId == GalaxyBuildingId.Colony1)
    ){
      tutorialSystem.ShowPopoutTutorial("7-big-space", "If it looks like your colony is moving slow, it's actually because space is reaalllly big!");
    }

    //Reminder if you have a lot of resources and no lvl 2 transports you should build more transports
    if(
      stageSevenData.viewMode == GalaxyViewMode.Galaxy
      && stageSevenData.timeRate != 0
      && !stageSevenData.ships.Any(ship => ship.shipTypeId == GalaxyBuildingId.Transport2)
      && stageSevenData.starSettlements.Any(ss => ss.Value.resources.Any(r => r.Value.amount > 10000))
    ){
      tutorialSystem.ShowPopoutTutorial("7-build-transports", "Transports are the backbone of your economy, make sure to build as many as you can!");
    }

    //Little tip about colony levels
    if(
      stageSevenData.viewMode == GalaxyViewMode.Planet
      && stageSevenData.settlements.ContainsKey(galaxyTransitioner.SelectedCb.data.id)
      && stageSevenData.settlements[galaxyTransitioner.SelectedCb.data.id].GetSettlementBuilding().tier == 1
      && stageSevenData.starSettlements[galaxyTransitioner.SelectedCb.data.parentStarId].resources.Any(sr => sr.Value.amount > 10)
    ){
      tutorialSystem.ShowPopoutTutorial("7-colony-levels", "Remember to upgrade your colony. This boosts mining efficiency for more resources per year!");
    }

    if(
      galaxyRouteManager.Editing &&
      stageSevenData.routeConnections.Count > 0 &&
      stageSevenData.year > 1000
    ){
      tutorialSystem.ShowPopoutTutorial("7-unused-routes", "Remember to check for unused routes and remove them to free up the transports");
    }
  }

  void OnVictory(bool isNewVictory){
    //Go to star that built the victory building for the text crawl
    uint systemIdWithVictory = 0;
    if(stageSevenData.starSettlements != null){
      foreach(var ss in stageSevenData.starSettlements){
        if(ss.Value.HasBuilding(GalaxyBuildingId.Victory7)){
          systemIdWithVictory = ss.Key;
          break;
        }
      }
    }

    if(systemIdWithVictory != 0){
      galaxyTransitioner.GoToStarAndCb(systemIdWithVictory, null, transitionAwayCameraPosition);
    }else{
      galaxyTransitioner.GoToStarAndCb(stageSevenData.starSettlements.First().Key, null, transitionAwayCameraPosition);
    }
  }
}
