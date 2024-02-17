using UnityEngine;
using UnityEngine.UI;
using strange.extensions.mediation.impl;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System;
using static HexCity;
using static CityPopupDisplay;

public class CityManager : View {

  [Inject] ResourceLoaderService loader {get; set;}
  [Inject] CitySelectSignal citySelect { get; set; }
  [Inject] BuildCitySignal buildCitySignal { get; set; }
  [Inject] CityBuiltSignal cityBuiltSignal { get; set; }
  [Inject] HexBuildingFinishedSignal buildingFinished { get; set; }
  [Inject] HexCityBordersChangedSignal cityBordersChangedSignal {get; set; }
  [Inject] StageSixDataModel stageSixData {get; set;}
  [Inject] StageRulesService stageRules { get; set; }
  [Inject] StringChanger stringChanger {get; set;}
  [Inject] FloatingText floatingText { get; set; }
  [Inject] TutorialSystem tutorialSystem {get; set;}
  [Inject] CameraService cameraService {get; set;}

  public HexGrid grid;
  public CameraPanner camPanner;
  public ShinyButton doneButton;
  public ShinyButton scoutButton;
  public ShinyButton scoutCancelButton;

  public ShinyButton buildButton;
  public ShinyButton buildCancelButton;

  public ShinyButton nextCityButton;
  public ShinyButton previousCityButton;

  public BuildingPanel buildingPanel;

  public TMP_Text currentBuilding;
  public UIFilledBar buildingProgress;

  public HexPanelManager panelManager;
  public TriangleControl focusTriangle;
  public TriangleControl citizensTriangle;

  public TMP_Text foodValue;
  public TMP_Text productionValue;
  public TMP_Text scienceValue;

  public TMP_Text happinessText;
  public TMP_Text healthText;
  public RectTransform wellnessPopupLocation;
  public RectTransform productionPopupLocation;

  public LineRenderer cityBorderLine;

  Bounds mapBounds;

  HexCity selectedCity = null;
  GameObject cityPopup = null;

  public List<HexScout> scoutList = new List<HexScout>();
  public List<HexSettler> settlerList = new List<HexSettler>();
  //synced from stage six manager
  public List<HexCity> cityList;

  protected override void Awake() {
    base.Awake();

    doneButton.onClick.AddListener(() => SelectCity(null));
    scoutButton.onClick.AddListener(StartSelectingScout);
    scoutCancelButton.onClick.AddListener(EndSelectingCell);

    buildButton.onClick.AddListener(StartBuilding);
    buildCancelButton.onClick.AddListener(EndBuilding);

    buildingFinished.AddListener(OnBuildingFinished);
    cityBordersChangedSignal.AddListener(OnCityBordersChanged);
    cityBuiltSignal.AddListener(OnCityBuilt);

    nextCityButton.onClick.AddListener(OnClickNextCityButton);
    previousCityButton.onClick.AddListener(OnClickPreviousCityButton);

    buildingPanel.loader = loader;
  }

  public void Reset(){
    selectedCity = null;
    scoutList.Clear();
    settlerList.Clear();
    panelManager.SwitchTo(HexPanel.Map);
  }

  public void OnLoad(){
    UpdateCityInfluencedTiles();
  }

  void Update(){
    //only update when city is selected
    if(selectedCity == null){ return; }

    scoutButton.interactable = !selectedCity.data.isScouting;
    StoreTriangleData();

    stringChanger.UpdateString(foodValue, nameof(foodValue), selectedCity.FoodPerSecond, null, "0");
    stringChanger.UpdateString(productionValue, nameof(productionValue), selectedCity.ProductionPerSecond, null, "0");
    stringChanger.UpdateString(scienceValue, nameof(scienceValue), selectedCity.SciencePerSecond, null, "0");

    stringChanger.UpdateString(healthText, nameof(healthText), selectedCity.data.citizenHealthiness, selectedCity.healthThreshold.title);

    stringChanger.UpdateString(happinessText, nameof(happinessText), selectedCity.data.citizenHappiness, selectedCity.happinessThreshold.title);

    if(selectedCity.data.buildQueue.Count > 0){
      buildingProgress.gameObject.SetActive(true);
      var building = selectedCity.data.buildQueue[0];
      buildingProgress.fillAmt = building.progress;
      currentBuilding.text = CityBuilding.allBuildings[building.buildingId].name;
      if(building.finished){
        buildingProgress.color = Colors.green;
      }else{
        buildingProgress.color = Colors.orange;
      }
    }else if(selectedCity.data.buildings.Count > 0){
      //Show previously completed building
      var lastCompleted = selectedCity.data.buildings.Last();
      buildingProgress.gameObject.SetActive(true);
      buildingProgress.fillAmt = 1f;
      buildingProgress.color = Colors.green;
      buildingProgress.label = "100%";
      currentBuilding.text = CityBuilding.allBuildings[lastCompleted.buildingId].name;

    } else{
      buildingProgress.gameObject.SetActive(false);
      currentBuilding.text = "Nothing Building";
    }

    var showPrevNextButtons = cityList.Count > 1;
    nextCityButton.gameObject.SetActive(showPrevNextButtons);
    previousCityButton.gameObject.SetActive(showPrevNextButtons);
  }


  public void SelectCity(HexCity city){
    //no-op for same city selection
    if(selectedCity == city){ return; }

    //end selection of currently selected
    if(selectedCity != null){
      if(EndCitySelection()){
        //Cancel selecting a different city if end city requests it
        return;
      }
    }

    selectedCity = city;

    //actually select the city
    if(selectedCity != null){
      StartCitySelection();
    }else{
      //no new city being selected
      panelManager.SwitchTo(HexPanel.Map);
    }

    citySelect.Dispatch(selectedCity);
  }

  void StartCitySelection(){
    panelManager.SwitchTo(HexPanel.City);
    grid.GetCell(selectedCity.data.coordinates).Select(true);
    SetTriangleData();
    ShowCityResources(true);
    SetupBorderLine();

    //Update the building progress label, do it outside update since it allocs new strings
    UpdateBuildingProgressLabel();

    //center city in view when selecting it
    MoveCameraToCity(selectedCity);

    //Show first tutorial on city management
    tutorialSystem.ShowTutorial(602, 0.5f);

    //Show focus and citizen control tutorial after scouting then reselecting a city
    if(selectedCity.hasScoutedThisSession){
      tutorialSystem.ShowTutorial(604, 1f);
    }
  }

  public void MoveCameraToCity(HexCity selectedCity, float yOffsetForUI = -1.5f, bool skipAnimation = false){

    camPanner.ResetDragVelocity();
    var newCamPos = new Vector3(selectedCity.transform.position.x, selectedCity.transform.position.y + yOffsetForUI, cameraService.Cam.transform.position.z);
    if(skipAnimation){
      cameraService.Cam.transform.localPosition = newCamPos;
    }else{
      LeanTween.moveLocal(cameraService.Cam.gameObject, newCamPos, 0.4f)
        .setEase(LeanTweenType.easeInOutCubic)
        .setIgnoreTimeScale(true);
    }
  }

  //end selection when a city was selected
  //Returns true if the selection should be cancelled and the current city should stay selected
  bool EndCitySelection(){
    if(selectedCity.selectingScout){
      EndSelectingScout(true);
      return true;
    }
    if(selectedCity.selectingSettler){
      EndSelectingSettler(true, true);
      return true;
    }
    RemoveCityPopup();
    grid.GetCell(selectedCity.data.coordinates).Select(false);
    ShowCityResources(false);
    cityBorderLine.gameObject.SetActive(false);

    return false;
  }

  void ShowCityResources(bool show = true){
    foreach(var cell in selectedCity.influencedCells){
      if(cell.display == null){ continue; }

      if(show){
        cell.display.ShowResourcesDisplay(stageSixData);
      }else{
        cell.display.HideResourcesDisplay();
      }
    }
  }

  void ShowExploredResources(bool show = true){
    foreach(var cell in grid.GetCells()){
      if(cell.ExploreStatus == HexExploreStatus.Unexplored){ continue; }
      if(cell.display == null){ continue; }

      if(show){
        cell.display.ShowResourcesDisplay(stageSixData);
      }else{
        cell.display.HideResourcesDisplay();
      }
    }
  }


  public void SelectCell(HexCell selected){
    if(selectedCity == null){
      Debug.LogWarning("Trying to select cell with no city!!");
      return;
    }
    if(selectedCity.selectingScout){
      StartScout(selected);
      return;
    }
    if(selectedCity.selectingSettler){
      SelectSettlerLocation(selected);
      return;
    }
  }

  public void EndSelectingCell(){
    if(selectedCity == null){
      Debug.LogWarning("Trying to deselect cell with no city!!");
      return;
    }
    if(selectedCity.selectingScout){
      EndSelectingScout(false);
    }
    if(selectedCity.selectingSettler){
      EndSelectingSettler(true, false);
    }
  }

  void StartSelectingScout(){
    panelManager.SwitchTo(HexPanel.Scout);

    selectedCity.selectingScout = true;

    var scoutRing = GetScoutTiles(selectedCity);
    foreach(var cell in scoutRing){
      cell.SetSelectable(true);
    }
  }

  public void EndSelectingScout(bool invalidPosition){
    panelManager.SwitchTo(HexPanel.City);
    selectedCity.selectingScout = false;

    var scoutRing = GetScoutTiles(selectedCity);
    foreach(var cell in scoutRing){
      cell.SetSelectable(false);
    }

    if(invalidPosition){
      floatingText.CreateUI("Invalid tile selected", Color.white, false);
    }
  }

  IEnumerable<HexCell> GetScoutTiles(HexCity startingCity){
    var moveBonus = stageSixData.scoutRadiusBonus;

    return grid.GetNeighborsInRange(startingCity.Cell, stageRules.StageSixRules.baseScoutRadius + moveBonus, false)
      .Where(t => t.ExploreStatus != HexExploreStatus.Explored);
  }

  void StartScout(HexCell dest){
    var scout = CreateScout(dest);

    selectedCity.data.isScouting = true;
    selectedCity.hasScoutedThisSession = true;

    EndSelectingScout(false);
    SelectCity(null);
  }

  public HexScout CreateScout(HexCell dest, HexScoutData existingScoutData = null){
    var scoutPrefab = loader.Load<GameObject>("Prefabs/6/Scout");
    var newScoutGO = GameObject.Instantiate(scoutPrefab, grid.unitsHolder);

    HexScoutData scoutData = null;
    if(existingScoutData == null){
      var movePoints = stageRules.StageSixRules.baseScoutMovePoints;
      if(stageSixData.ResearchedTech(HexTechId.Wheel)){ movePoints = Mathf.CeilToInt(movePoints * 1.5f); }
      if(stageSixData.ResearchedTech(HexTechId.HorsebackRiding)){ movePoints = Mathf.CeilToInt(movePoints * 1.5f); }
      if(stageSixData.ResearchedTech(HexTechId.Compass)){ movePoints = Mathf.CeilToInt(movePoints * 1.25f); }
      if(stageSixData.ResearchedTech(HexTechId.Optics)){ movePoints = Mathf.CeilToInt(movePoints * 1.25f); }

      scoutData = new HexScoutData(){
        source = selectedCity.data.coordinates,
        position = selectedCity.data.coordinates,
        dest = dest.coordinates,
        movePoints = movePoints,
        currentMovePoints = movePoints,
        canCrossWater = stageSixData.ResearchedTech(HexTechId.Sailing),
      };
    }else{
      scoutData = existingScoutData;
    }

    newScoutGO.transform.localPosition = grid.GetCell(scoutData.position).localPosition;

    var scout = newScoutGO.GetComponent<HexScout>();
    scout.OnScoutCompleted = ScoutComplete;
    scout.grid = grid;
    scout.stageSixData = stageSixData;
    scout.data = scoutData;
    scout.Init();

    if(stageSixData.scoutData == null){
      stageSixData.scoutData = new List<HexScoutData>();
    }

    //Save the scout if it's not already coming from a save
    if(existingScoutData == null){
      stageSixData.scoutData.Add(scoutData);
    }

    scoutList.Add(scout);
    return scout;
  }

  void ScoutComplete(HexCoordinates cityCoords){
    var city = stageSixData.cities.FirstOrDefault(c => c.coordinates == cityCoords);
    city.isScouting = false;
    stageSixData.scoutData.RemoveAll(s => s.source == cityCoords);
    scoutList.RemoveAll(s => s.data.source == cityCoords);
  }

  void StartSelectingSettler(){
    panelManager.SwitchTo(HexPanel.Scout);

    selectedCity.selectingSettler = true;

    ShowExploredResources(true);

    var settlerTiles = GetSettlerTiles(selectedCity);
    foreach(var cell in settlerTiles){
      cell.SetSelectable(true);
    }
  }

  public void EndSelectingSettler(bool cancelled, bool invalidPosition){
    panelManager.SwitchTo(HexPanel.Building);
    selectedCity.selectingSettler = false;

    ShowExploredResources(false);

    foreach(var cell in grid.GetCells()){
      cell.SetSelectable(false);
    }

    if(cancelled){
      //with invalid selection remove the settler from the build queue which should be the last thing
      RemoveBuildingFromQueue(buildingPanel.queueTransform.GetChild(buildingPanel.queueTransform.childCount - 1));

      if(invalidPosition){
        floatingText.CreateUI("Invalid tile selected", Color.white, false);
      }
    }
  }

  IEnumerable<HexCell> GetSettlerTiles(HexCity startingCity){
    //Exclude points around cities that are already built, have a settler going to them already
    //Or have a settler going there in the build queue
    var cityPoints = stageSixData.cities.Select(c => c.coordinates);
    cityPoints = cityPoints.Concat(stageSixData.settlerData.Select(sd => sd.dest));
    cityPoints = cityPoints.Concat(stageSixData.cities.SelectMany(city =>
      city.buildQueue.Where(b => b.buildingId == CityBuildingId.Settler && b.location.HasValue).Select(b => b.location.Value)
    ));

    var forbiddenPoints = cityPoints.SelectMany(cp => grid.GetNeighborsInRange(grid.GetCell(cp), 2, true)).ToList();

    return grid.GetNeighborsInRange(startingCity.Cell, stageRules.StageSixRules.baseSettlerRadius + stageSixData.settlerRadiusBonus, false)
      .Where(t =>
        t.IsSettleable
        && (t.ExploreStatus == HexExploreStatus.Explored || t.ExploreStatus == HexExploreStatus.Partial)
        && !forbiddenPoints.Contains(t)
      );
  }

  void SelectSettlerLocation(HexCell dest){
    var lastSettler = selectedCity.data.buildQueue[selectedCity.data.buildQueue.Count - 1];
    if(lastSettler.buildingId != CityBuildingId.Settler){
      Debug.LogWarning("Trying to set settler location but something weird happened to the build queue");
    }else{
      lastSettler.location = dest.coordinates;
    }

    EndSelectingSettler(false, false);
  }

  void SettleComplete(HexCoordinates newCityCoords){
    buildCitySignal.Dispatch(grid.GetCell(newCityCoords));

    stageSixData.settlerData.RemoveAll(s => s.dest == newCityCoords);
    settlerList.RemoveAll(s => s.data.dest == newCityCoords);
  }

  public HexSettler CreateSettler(HexCoordinates src, HexCoordinates dest, HexSettlerData existingSettlerData = null){
    var settlerPrefab = loader.Load<GameObject>("Prefabs/6/Settler");

    var newSettlerGO = GameObject.Instantiate(settlerPrefab, Vector3.zero, Quaternion.identity);


    HexSettlerData settlerData = null;
    if(existingSettlerData == null){
      settlerData = new HexSettlerData(){
        source = src,
        position = src,
        dest = dest,
        canCrossWater = stageSixData.ResearchedTech(HexTechId.Sailing),
      };
    }else{
      settlerData = existingSettlerData;
    }

    newSettlerGO.transform.SetParent(grid.unitsHolder, false);
    newSettlerGO.transform.localPosition = grid.GetCell(settlerData.position).localPosition;

    var settler = newSettlerGO.GetComponent<HexSettler>();
    settler.OnSettleCompleted = SettleComplete;
    settler.grid = grid;
    settler.data = settlerData;
    settler.Init();

    if(stageSixData.settlerData == null){
      stageSixData.settlerData = new List<HexSettlerData>();
    }

    //Save the scout if it's not already coming from a save
    if(existingSettlerData == null){
      stageSixData.settlerData.Add(settlerData);
    }

    settlerList.Add(settler);
    return settler;
  }

  void StoreTriangleData(){
    selectedCity.data.growth = focusTriangle.TopAmt;
    selectedCity.data.science = focusTriangle.LeftAmt;
    selectedCity.data.production = focusTriangle.RightAmt;

    selectedCity.data.health = citizensTriangle.TopAmt;
    selectedCity.data.work = citizensTriangle.LeftAmt;
    selectedCity.data.play = citizensTriangle.RightAmt;
  }

  void SetTriangleData(){
    focusTriangle.SetAmts(selectedCity.data.growth, selectedCity.data.science, selectedCity.data.production);

    citizensTriangle.SetAmts(selectedCity.data.health, selectedCity.data.work, selectedCity.data.play);
  }

  void StartBuilding(){
    buildingPanel.Clear();

    //get all available buildings
    var selectedBuildings = selectedCity.GetAvailableBuildings();

    UpdateBuildPanel(selectedBuildings);

    buildingPanel.tabs.FocusTab(0);

    //and show
    panelManager.SwitchTo(HexPanel.Building);

    //recreate the build queue if we need to
    for(var i = 0; i < selectedCity.data.buildQueue.Count; i++){
      var building = selectedCity.data.buildQueue[i];
      CreateBuildQueueSelection(CityBuilding.allBuildings[building.buildingId], building.productionCost);
    }
  }

  void UpdateBuildPanel(List<CityBuildingInfo> selectedBuildings){

    for(var i = 0; i < selectedBuildings.Count; i++){
      var buildingInfo = selectedBuildings[i];

      GameResource[] resourceCosts;
      if(buildingInfo.building.secondsToBuild == null){
        resourceCosts = new GameResource[]{ new GameResource{
          type = GameResourceType.Production,
          amount = buildingInfo.building.ProductionCost(
            buildingInfo.productionMultiplier != null ?
            buildingInfo.productionMultiplier.Value :
            0,
            stageSixData
          )
        }};
      }else{
        resourceCosts = new GameResource[]{ new GameResource{
          type = GameResourceType.Time,
          amount = buildingInfo.building.secondsToBuild.Value
        }};
      }

      buildingPanel.CreateOrUpdateBuildListItem(i, (BuildingSelectionDisplay display) => {
        display.buildId = (int)buildingInfo.building.id;
        display.title = buildingInfo.quantity != 1 ? $"{buildingInfo.quantity} {buildingInfo.building.name}" : buildingInfo.building.name;
        display.descrip = BuildingSelectionDescription(buildingInfo, 0);
        display.resourceCosts = resourceCosts;
        display.iconPaths = buildingInfo.building.iconPaths;
        display.OnClick = () => ClickBuildingUpgrade(buildingInfo, display);
      });
    }

    //remove extra building displays from previous updates if they exist
    if(buildingPanel.selectionDisplayCount > selectedBuildings.Count){
      for(var i = buildingPanel.selectionDisplayCount - 1; i >= selectedBuildings.Count; i--){
        buildingPanel.RemoveBuildListItem(i);
      }
    }

    if(selectedBuildings.Count == 0){
      buildingPanel.ShowEmptyMessage("Grow the city to send a settler!<br><br>Research technology to unlock new buildings!");
    }

    //completed buildings
    foreach(var buildingInfo in selectedCity.data.buildings){

      buildingPanel.CreateCompletedListItem((BuildingCreatedDisplay display) => {
        var buildingTemplate = CityBuilding.allBuildings[buildingInfo.buildingId];

        display.buildId = (int)buildingTemplate.id;
        display.title = buildingTemplate.name;
        display.descrip = buildingTemplate.descrip;
        display.iconPaths = buildingTemplate.iconPaths;
      });
    }
  }

  void ClickBuildingUpgrade(HexCity.CityBuildingInfo buildingInfo, BuildingSelectionDisplay display){
    var building = buildingInfo.building;

    //Get any dest coords for the current building selection (like for roads)
    var productionCost = display.resourceCosts[0].amount;
    var location = buildingInfo.destCoord;

    selectedCity.AddBuildingToQueue(building.id, productionCost, location);

    UpdateBuildPanel(selectedCity.GetAvailableBuildings());

    CreateBuildQueueSelection(buildingInfo.building, productionCost);

    //Get coords for settler city location if needed
    if(building.id == CityBuildingId.Settler){
      StartSelectingSettler();
    }

    UpdateBuildingProgressLabel();
  }

  string BuildingSelectionDescription(HexCity.CityBuildingInfo buildingInfo, int buildIndex){
    var descrip = buildingInfo.building.descrip;

    if(buildingInfo.replacingBuildings != null && buildingInfo.replacingBuildings.Length > 0){
      //For now just displaying the first replaced building since any others should be obvious
      var replacementBuilding = CityBuilding.allBuildings[buildingInfo.replacingBuildings.First()];
      descrip = string.Format("<#{0}>Replaces {1}</color><br>{2}", Colors.redText.ToHex(), replacementBuilding.name, descrip);
    }else if(buildingInfo.madeObsoleteBy.HasValue && buildingInfo.madeObsoleteBy.Value != CityBuildingId.None){
      var obsoleteByBuilding = CityBuilding.allBuildings[buildingInfo.madeObsoleteBy.Value];
      descrip = string.Format("<#{0}>Made Obsolete by {1}</color><br>{2}", Colors.purpleText.ToHex(), obsoleteByBuilding.name, descrip);
    }

    return descrip;
  }

  void CreateBuildQueueSelection(CityBuilding building, int productionCost){

    buildingPanel.CreateQueueSelection((BuildingSelectionDisplay bsd) => {
      bsd.buildId = (int)building.id;
      bsd.title = building.name;
      bsd.resourceCosts = new GameResource[]{ new GameResource{
        type = building.secondsToBuild == null ? GameResourceType.Production : GameResourceType.Time,
        amount = productionCost
      }};
      bsd.iconPaths = building.iconPaths;
      bsd.OnClick = () => RemoveBuildingFromQueue(bsd.transform);
    });

  }

  void RemoveBuildingFromQueue(Transform queueTransform){
    var index = queueTransform.GetSiblingIndex();
    selectedCity.RemoveBuildingFromQueue(index);

    UpdateBuildPanel(selectedCity.GetAvailableBuildings());

    buildingPanel.RemoveQueueDisplay(index);
  }

  void EndBuilding(){
    buildingPanel.Clear();
    panelManager.SwitchTo(HexPanel.City);

    //finished building tutorial, show next one
    if(selectedCity.data.buildQueue.Count > 0){
      tutorialSystem.ShowTutorial(603, 1f);
    }
  }

  void OnBuildingFinished(HexCity city, CityBuildingData buildingData){
    if(buildingData.buildingId == CityBuildingId.Settler){
      CreateSettler(city.data.coordinates, buildingData.location.Value);
    }
  }

  void UpdateBuildingProgressLabel(){
    if(selectedCity.data.buildQueue.Count > 0){
      var building = selectedCity.data.buildQueue[0];
      buildingProgress.label = string.Format("{0}/{1}",
        Mathf.RoundToInt(building.progress * building.productionCost).ToShortFormat(),
        building.productionCost.ToShortFormat()
      );
    }
  }

  //set up through event system
  public void OnHappinessPanelClick(){
    CloseOrCreateCityPopup( CityPopupDisplayType.Happiness, selectedCity);
  }

  public void OnHealthPanelClick(){
    CloseOrCreateCityPopup( CityPopupDisplayType.Health, selectedCity);
  }

  public void OnFoodPanelClick(){
    CloseOrCreateCityPopup( CityPopupDisplayType.Food, selectedCity);
  }
  public void OnProductionPanelClick(){
    CloseOrCreateCityPopup( CityPopupDisplayType.Production, selectedCity);
  }
  public void OnSciencePanelClick(){
    CloseOrCreateCityPopup( CityPopupDisplayType.Science, selectedCity);
  }

  void CloseOrCreateCityPopup(CityPopupDisplayType type, HexCity city){
    //If the popup is already open, close that and be done to get the toggle behavior
    if(cityPopup != null){
      RemoveCityPopup();
      return;
    }

    var popupPrefab = loader.Load<GameObject>("Prefabs/6/Popup");
    var newPopup = GameObject.Instantiate(popupPrefab, Vector3.zero, Quaternion.identity);
    RectTransform popupLocation;
    switch(type){
      case CityPopupDisplayType.Food:
      case CityPopupDisplayType.Production:
      case CityPopupDisplayType.Science:
        popupLocation = productionPopupLocation;
        break;
      default:
        popupLocation = wellnessPopupLocation;
        break;
    }
    newPopup.transform.SetParent(popupLocation, false);
    var rt = newPopup.GetComponent<RectTransform>();
    var contentPopup = newPopup.GetComponent<ContentPopup>();

    //contents
    var cityPopupDisplay = contentPopup.popupDisplay;

    cityPopupDisplay.displayType = type;
    cityPopupDisplay.city = city;
    cityPopupDisplay.Init();

    //save the ref for closing
    cityPopup = newPopup;
  }

  void RemoveCityPopup(){
    if(cityPopup != null){
      Destroy(cityPopup);
    }
  }

  void OnCityBordersChanged(HexCity city){
    UpdateCityInfluencedTiles();
  }

  void OnCityBuilt(HexCity city){
    UpdateCityInfluencedTiles();
  }


  //City border line stuff
  void SetupBorderLine(){
    // Debug for all city influences
    // foreach(var city in cityList){
    //   var cityColor = Colors.random;
    //   foreach(var cell in city.influencedCells){
    //     cell.UpdateTerrainDisplay(CellDrawMode.None, false, cityColor);
    //   }
    // }

    var perim = HexPerimeter.FindPerimeterLoop(selectedCity.influencedCells);

    var linePositions = HexPerimeter.GetLinePositions(perim);

    cityBorderLine.positionCount = linePositions.Count;

    for(var c = 0; c < linePositions.Count; c++){

      cityBorderLine.SetPosition(c, linePositions[c]);
    }

    cityBorderLine.gameObject.SetActive(true);
  }

  void UpdateCityInfluencedTiles(){

    var cityInfluencedTiles = grid.FindCityInfluencedTiles(cityList);
    var grouped = cityInfluencedTiles.GroupBy(x => x.Value);
    foreach(var groupKey in grouped){
      groupKey.Key.influencedCells = groupKey.Select(x => x.Key).ToList();
      foreach(var cell in groupKey.Key.influencedCells){
        cell.influincingCity = groupKey.Key;
      }
    }

  }

  void OnClickNextCityButton(){
    var curIndex = cityList.IndexOf(selectedCity);
    var nextIndex = (curIndex + 1) % cityList.Count;
    SelectCity(cityList[nextIndex]);
  }

  void OnClickPreviousCityButton(){
    var curIndex = cityList.IndexOf(selectedCity);
    var nextIndex = (curIndex - 1);
    nextIndex = nextIndex < 0 ? cityList.Count - 1 : nextIndex;
    SelectCity(cityList[nextIndex]);
  }
}
