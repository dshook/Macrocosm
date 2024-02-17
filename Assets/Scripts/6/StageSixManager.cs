using UnityEngine;
using UnityEngine.UI;
using strange.extensions.mediation.impl;
using System.Collections.Generic;
using System.Linq;
using MoreMountains.NiceVibrations;

public class StageSixManager : View, IStageManager {

  [Inject] StageTransitionService stageTransition { get; set; }
  [Inject] StageTransitionModel stageData {get; set;}
  [Inject] StageFiveDataModel stageFiveData {get; set;}
  [Inject] StageSixDataModel stageSixData {get; set;}
  [Inject] StageRulesService stageRules { get; set; }
  [Inject] InputService input {get; set;}
  [Inject] GameSaverService gameSaver {get; set;}
  [Inject] ResourceLoaderService loader {get; set;}
  [Inject] GameSavingSignal gameSavingSignal { get; set; }
  [Inject] CitySelectSignal citySelectSignal { get; set; }
  [Inject] BuildCitySignal buildCitySignal { get; set; }
  [Inject] CityBuiltSignal cityBuiltSignal { get; set; }
  [Inject] HexBuildQueueChangeSignal buildQueueChangeSignal { get; set; }
  [Inject] HexBonusResourceRevealedSignal bonusResourceRevealedSignal {get;set;}
  [Inject] HexCellExploredSignal hexCellExploredSignal {get; set;}
  [Inject] AudioService audioService {get; set;}
  [Inject] TutorialSystem tutorialSystem {get; set;}
  [Inject] OpenTutorialFinishedSignal openTutorialFinishedSignal { get; set; }
  [Inject] StatsModel statsModel { get; set; }
  [Inject] ObjectPool objectPool {get; set;}
  [Inject] FloatingText floatingText { get; set; }
  [Inject] SendErrorUserReportSignal sendErrorUserReport { get; set; }

  [Inject] ClickSignal clickSignal {get; set;}

  public HexMapGenerator mapGenerator;
  public HexGrid grid;
  public CameraPanner camPanner;
  public CityManager cityManager;
  public TechManager techManager;
  public HexPanelManager panelManager;

  public ShinyButton buildQueueEmptyButton;

  public AudioClip cityFoundedClip;
  public AudioClip cityFinishedBuildingClip;

  public AudioClip music;

  public GameObject resourceIconPrefab;

  HexCity selectedCity;

  List<HexCity> cityList = new List<HexCity>();

  bool needsBoundsUpdate = false;

  protected override void Awake() {
    base.Awake();

    gameSavingSignal.AddListener(OnSaving);
    citySelectSignal.AddListener((city) => selectedCity = city);
    buildCitySignal.AddListener((cell) => CreateCity(cell));
    buildQueueChangeSignal.AddListener(OnBuildQueueChange);
    bonusResourceRevealedSignal.AddListener(OnBonusResourceRevealed);
    hexCellExploredSignal.AddListener(OnHexCellExplored);
    buildQueueEmptyButton.onClick.AddListener(OnClickBuildQueueEmptyButton);
    openTutorialFinishedSignal.AddListener(OnOpenTutorialFinished);

    techManager.cityList = cityList;
    cityManager.cityList = cityList;

    objectPool.CreatePool(resourceIconPrefab, 0);
  }

  void OnSaving(){
    if(!grid.IsInitialized){ return; }

    //update the cell data from the map
    stageSixData.gridCells = grid.Cells;
    // stageSixData.cellData = grid.GetCells().Select(c => c.data).ToList();
#pragma warning disable 0618
    stageSixData.cellData = null;
#pragma warning restore
    stageSixData.scoutData = cityManager.scoutList.Select(s => s.data).ToList();
    stageSixData.settlerData = cityManager.settlerList.Select(s => s.data).ToList();
  }

  public void Init(bool isInitialCall){
    if(stageSixData.mapSeed == 0){
      stageSixData.mapSeed = stageRules.StageSixRules.startingMapSeed;
    }

    //Handle the times before the whole grid was saved
    if(stageSixData.cities.Count >= 1 && stageSixData.gridCells == null){
      HandleInvalidState("Have cities but no saved cells");
      return;
    }

    if(stageSixData.gridCells == null){
      //initial map setup case
      var gridStart = Time.realtimeSinceStartup;
      grid.CreateGrid();
      var gridEnd = Time.realtimeSinceStartup;
      mapGenerator.seed = stageSixData.mapSeed;
      mapGenerator.useFixedSeed = true;
      mapGenerator.GenerateMap();
      stageSixData.mapGeneratorVersion = mapGenerator.mapGeneratorVersion;
      Debug.Log(string.Format("Total Map Setup: {0}, Grid: {1}", Time.realtimeSinceStartup - gridStart, gridEnd - gridStart));
    }else{
      //loading grid case
      grid.LoadGrid(stageSixData.gridCells);
    }

    panelManager.SwitchTo(HexPanel.Map);

    var connectionsToBuild = new List<HexGrid.ConnectionParams>();
    if(stageSixData.cities != null){
      foreach(var cityData in stageSixData.cities){
        var cell = grid.GetCell(cityData.coordinates);
        if(cell == null){
          HandleInvalidState("Cell null for city " + cityData.coordinates);
          return;
        }
        var city = CreateCity(cell, cityData);

        foreach(var roadDest in cityData.outgoingConnections){

          connectionsToBuild.Add(new HexGrid.ConnectionParams(){
            origin = cityData.coordinates,
            dest = roadDest.dest,
            connectionBuildingId = roadDest.connectionBuildingId,
            originCity = city
          });
        }
      }
    }
    // Rebuild the roads from highest to lowest tiers to avoid having to rebuild connections
    // This sort duped in HexGrid.CreateRiverOrConnection
    connectionsToBuild = connectionsToBuild
      .OrderByDescending(cpm =>
        HexRiverOrRoad.connectionTier.ContainsKey(cpm.connectionBuildingId) ?
        HexRiverOrRoad.connectionTier[cpm.connectionBuildingId] :
        0
      ).ToList();

    foreach(var cpm in connectionsToBuild){
      grid.CreateRiverOrConnection(cpm);
    }

    if(stageSixData.scoutData != null){
      foreach(var scoutData in stageSixData.scoutData){
        var cell = grid.GetCell(scoutData.dest);
        if(cell == null){
          HandleInvalidState("Cell null for scout " + scoutData.dest);
          return;
        }
        cityManager.CreateScout(cell, scoutData);
      }
    }

    if(stageSixData.settlerData != null){
      foreach(var settlerData in stageSixData.settlerData){
        cityManager.CreateSettler(settlerData.source, settlerData.dest, settlerData);
      }
    }

    techManager.OnLoad();
    cityManager.OnLoad();

    //Have to come back and rebuild improvement buildings after load stuff happens so that all the cities
    //area of influence can be updated for the buildings to be built on
    foreach(var hexCity in cityList){
      if(hexCity.data.buildings.Count > 0){
        foreach(var buildInfo in hexCity.data.buildings){
          var currentBuilding = CityBuilding.allBuildings[buildInfo.buildingId];
          if(currentBuilding.onMap && buildInfo.location.HasValue){
            var cell = grid.GetCell(buildInfo.location.Value);
            if(cell == null){
              HandleInvalidState("Cell null for building " + buildInfo.location.Value);
              return;
            }
            hexCity.AddBuildingToCell(cell, currentBuilding);
          }
        }
      }
    }

    if(stageSixData.cities.Count == 0){
      var startingCity = CreateStartingCity();
    }

    var bounds = UpdateCameraBounds();

    grid.StartCreatingCells(bounds);

    var firstCity = cityList.FirstOrDefault();
    if(firstCity != null){
      cityManager.MoveCameraToCity(firstCity, 0f, true);
    }
  }

  void HandleInvalidState(string reason){
    sendErrorUserReport.Dispatch($"Invalid hex ({stageSixData.mapSeed}) state: " + reason);
    Debug.LogWarning("Invalid stage six state, resetting.");
    Cleanup();
    gameSaver.ResetStageData(6);
    Init(false);
    floatingText.CreateUI("Resetting for major update, sorry!", Color.white, false, 5f);
  }

  HexCity CreateStartingCity(){

    //find best starting tile
    HexCell bestStartingCell = null;
    var bestStartingScore = 0;
    for(var c = 0; c < grid.cellCount; c++){
      var cell = grid.GetCell(c);

      //Exclude what we don't want to settle on
      if(!cell.IsSettleable || cell.HexBonusResource != HexBonusResource.None){
        continue;
      }

      var cellScore = 0;

      if(cell.HexFeature == HexFeature.None){ cellScore += 5; }
      if(cell.HexFeature == HexFeature.TreesSparse){ cellScore += 3; }
      if(cell.HexFeature == HexFeature.TreesMedium){ cellScore += 2; }
      if(cell.HexFeature == HexFeature.TreesDense){ cellScore += 1; }

      cellScore += cell.Food(stageSixData);

      var hasFreshwater = cell.Freshwater;
      for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
        HexCell neighbor = cell.GetNeighbor(d);
        if(neighbor == null){ continue; }

        if(neighbor.Freshwater){ hasFreshwater = true; }

        cellScore += neighbor.Food(stageSixData);
        //prioritize early food sources more
        if(
          neighbor.HexBonusResource == HexBonusResource.Livestock ||
          neighbor.HexBonusResource == HexBonusResource.Grains ||
          neighbor.HexBonusResource == HexBonusResource.Fish
        ){
          cellScore += 3;
        }
      }

      var outerRing = grid.GetRing(cell, 2);
      foreach(var neighbor in outerRing){
        if(neighbor == null){ continue; }

        cellScore += neighbor.Food(stageSixData);
        //prioritize early food sources more
        if(
          neighbor.HexBonusResource == HexBonusResource.Livestock ||
          neighbor.HexBonusResource == HexBonusResource.Grains ||
          neighbor.HexBonusResource == HexBonusResource.Fish
        ){
          cellScore += 2;
        }
      }

      if(hasFreshwater){
        cellScore += 1000; //To make sure freshwater gets prioritized over others hah
      }

      if(cellScore > bestStartingScore){
        bestStartingCell = cell;
        bestStartingScore = cellScore;
      }
    }

    //Prolly shouldn't be possible lol
    if(bestStartingCell == null){
      bestStartingCell = grid.GetCenterCell();
    }

    return CreateCity(bestStartingCell);
  }

  public void OnTransitionTo(StageTransitionData data){
    clickSignal.AddListener(OnClick);

    UpdateCameraBounds();

    var firstCity = cityList.FirstOrDefault();
    if(firstCity != null && !data.fromMenu){
      cityManager.MoveCameraToCity(firstCity, 0f, true);
    }

    camPanner.ResetDragVelocity();
    OnBuildQueueChange(null, false); //update the build queue empty flag
    panelManager.SwitchTo(HexPanel.Map);

    audioService.PlayMusic(music);
  }

  public void OnTransitionAway(bool toMenu){
    if(clickSignal != null){
      clickSignal.RemoveListener(OnClick);
    }

    if(!toMenu){
      camPanner.ResetZoom();
    }
    audioService.StopMusic();
  }

  public void Cleanup(){
    if(stageSixData == null){ return; }

    //clear the grid, cities, and others
    stageSixData.cities.Clear();
    stageSixData.gridCells = null;
    stageSixData.scoutData.Clear();
    stageSixData.settlerData.Clear();
    cityList.Clear();

    cityManager.Reset();
    techManager.Reset();

    grid.ClearGrid();
  }

  void Update(){
    //While grid is still spawning cells update the camera bounds
    if(!grid.FinishedSpawningCells){
      needsBoundsUpdate = true;
    }

    //check for needing to show settler tutorial
    if(stageSixData.ResearchedTech(HexTechId.Agriculture)
      && stageSixData.cities.Count == 1
      && !stageSixData.cities[0].buildQueue.Any(x => x.buildingId == CityBuildingId.Settler)
      && stageSixData.cities[0].population >= CityBuilding.allBuildings[CityBuildingId.Settler].requiredCitySize
    ){
      tutorialSystem.ShowTutorial(605);
    }

    //growing your cities tutorial
    if(selectedCity == null && statsModel.stageTime[6] > 60 * 10 ){
      tutorialSystem.ShowPopoutTutorial("6-growing-city", "Grow your cities to expand their area of influence! The first increase is at population 36");
    }

    //Pollution tutorial
    if(stageSixData.pollutionLevel > 0){
      tutorialSystem.ShowTutorial(606);
    }
  }

  void LateUpdate(){
    if(needsBoundsUpdate){
      UpdateCameraBounds();
    }
  }

  void OnClick(Vector2 pos){
    var cell = grid.GetCell(input.pointerWorldPosition);
    if(cell != null){
      MMVibrationManager.Haptic(HapticTypes.LightImpact);
      if(cell.Selectable){
        cityManager.SelectCell(cell);
        return;
      }
      var city = cityList.FirstOrDefault(c => c.data.coordinates == cell.coordinates);
      cityManager.SelectCity(city);

    }
  }

  HexCity CreateCity(HexCell cell, HexCityData existingCityData = null){
    if(cityList.Any(c => c.data.coordinates == cell.coordinates)){
      //Skip adding a city when it's already created here
      //This should only happen when resetting the stage data and the cleanup creates the starting city
      //And the the Init loads that city from data
      return null;
    }

    var hexCityPrefab = loader.Load<GameObject>("Prefabs/6/HexCity");

    var newCity = GameObject.Instantiate(hexCityPrefab, Vector3.zero, Quaternion.identity, grid.cityHolder);

    newCity.transform.SetParent(grid.cityHolder, false);
    newCity.transform.localPosition = cell.localPosition;
    newCity.name = cell.coordinates.ToString();

    HexCityData cityData = null;
    if(existingCityData == null){
      cityData = new HexCityData(){
        population = 1,
        coordinates = cell.coordinates,
        isScouting = false,
      };
    }else{
      cityData = existingCityData;
    }

    if(cityData.buildings == null){
      cityData.buildings = new List<FinishedCityBuildingData>();
    }

    var hexCity = newCity.GetComponent<HexCity>();
    hexCity.data = cityData;
    hexCity.grid = grid;

    cell.city = hexCity;

    //make sure surrounding area is explored
    cell.Explore(HexExploreStatus.Explored);
    foreach(var ringCell in grid.GetRing(cell, 1)){
      ringCell.Explore(HexExploreStatus.Explored);

      //Force a display update in case any bonus resource icons need to flip
      if(ringCell.display != null){
        ringCell.display.UpdateTerrainDisplay();
      }
    }

    //and the ring around that as partially explored
    foreach(var ringCell in grid.GetRing(cell, 2)){
      ringCell.Explore(HexExploreStatus.Partial);
    }

    if(stageSixData.cities == null){
      stageSixData.cities = new List<HexCityData>();
    }

    if(existingCityData == null){
      //Save the city if it's not already coming from a save
      stageSixData.cities.Add(cityData);

      CheckForEmptyBuildQueues(false);
      audioService.PlaySfx(cityFoundedClip);
    }
    cityList.Add(hexCity);

    hexCity.Init();

    cityBuiltSignal.Dispatch(hexCity);

    return hexCity;
  }

  void OnBuildQueueChange(HexCity city, bool buildingFinished){
    CheckForEmptyBuildQueues(buildingFinished);
  }

  void CheckForEmptyBuildQueues(bool buildingFinished){
    var cityNeedsBuilding = false;
    var isEstablishedCity = false;
    foreach(var cityData in stageSixData.cities){
      if(cityData.buildQueue.Count == 0){
        cityNeedsBuilding = true;
        if(cityData.population > 1){
          isEstablishedCity = true;
        }
        break;
      }
    }

    //Only play the build queue empty sound when it's an established city (not just founded)
    //Also only play when we're toggling the button on
    if(
      isEstablishedCity &&
      cityNeedsBuilding &&
      buildingFinished &&
      !buildQueueEmptyButton.gameObject.activeSelf
    ){
      audioService.PlaySfx(cityFinishedBuildingClip);
    }

    buildQueueEmptyButton.gameObject.SetActive(cityNeedsBuilding);
  }

  int emptyQueueIndex = 0;

  //to to next city with empty queue, up the index so we cycle through the cities
  void OnClickBuildQueueEmptyButton(){
    var idleCities = stageSixData.cities.Where(c => c.buildQueue.Count == 0).ToList();

    if(idleCities.Count == 0){ return; }

    if(emptyQueueIndex >= idleCities.Count){
      emptyQueueIndex = 0;
    }

    var toGoTo = idleCities.Skip(emptyQueueIndex).First();
    cityManager.SelectCity(cityList.FirstOrDefault(c => c.data.coordinates == toGoTo.coordinates));

    emptyQueueIndex++;
  }

  void OnBonusResourceRevealed(HexBonusResource resource){
    //Update tiles on grid that have the resource
    for (int i = 0; i < grid.cellCount; i++) {
      HexCell cell = grid.GetCell(i);
      if(cell.HexBonusResource == resource && cell.display != null){
        cell.display.UpdateTerrainDisplay();
      }
    }
  }

  void OnHexCellExplored(){
    needsBoundsUpdate = true;
  }

  Bounds UpdateCameraBounds(){
    needsBoundsUpdate = false;

    float minX = float.MaxValue;
    float minY = float.MaxValue;
    float maxX = float.MinValue;
    float maxY = float.MinValue;

    var validBounds = false;
    for (int i = 0; i < grid.cellCount; i++) {
      var cell = grid.GetCell(i);
      if(cell.display == null){ continue; }

      if(cell.ExploreStatus == HexExploreStatus.Unexplored){ continue; }

      var cellPos = cell.display.Position;

      minX = Mathf.Min(minX, cellPos.x);
      minY = Mathf.Min(minY, cellPos.y);
      maxX = Mathf.Max(maxX, cellPos.x);
      maxY = Mathf.Max(maxY, cellPos.y);

      validBounds = true;
    }

    Bounds mapBounds;

    if(validBounds){
      var trPos = new Vector2(maxX, maxY);
      var blPos = new Vector2(minX, minY);

      var center = new Vector2((trPos.x - blPos.x) / 2f + blPos.x, (trPos.y - blPos.y) / 2f + blPos.y );
      var size = trPos - blPos;

      mapBounds = new Bounds(center, size);
    }else{
      //when the game first starts up and there are no cell displays created yet make some fake bounds
      var firstCity = cityList.FirstOrDefault();
      var center = firstCity != null ? firstCity.gameObject.transform.position : Vector3.zero;
      mapBounds = new Bounds(center, Vector2.one * 5f);
    }

    camPanner.bounds = mapBounds;

    //Allow zooming out further when more is explored
    var zoomRangeNormalized = Mathf.Clamp01(Mathf.Max(mapBounds.size.x, mapBounds.size.y) / 40f);
    camPanner.maxZoomSize = Mathf.Lerp(5f, 15f, zoomRangeNormalized);

    return mapBounds;
  }

  void OnOpenTutorialFinished(int tutorialId){
    if(tutorialId == 600){
      tutorialSystem.ShowTutorial(601, 2f);
    }
  }
}

/*

Sorting layers

1 - Cell Backgrounds
2 - Roads & Rivers
3 - Cell features / City border line
5 - Cell buildings / city display / city flags (higher order in layer)
7 - Resource icons
8 - Population text / progress bar / Cell debug
9 - Units (Scout & Settler) & move bar (higher order in layer)
10 - Cell overlay
11 - Bonus Resources
*/