using TMPro;
using UnityEngine;
using strange.extensions.mediation.impl;
using System.Collections.Generic;
using System.Linq;
using System;

public class CelestialManager : View {
  [Inject] StageSevenDataModel stageSevenData {get; set;}
  [Inject] StageSixDataModel stageSixData {get; set;}
  [Inject] ResourceLoaderService loader {get; set;}
  [Inject] TimeService time { get; set; }
  [Inject] ColonizeCelestialSignal colonizeCelestial { get; set; }
  [Inject] CelestialColonizedSignal celestialColonized { get; set; }
  [Inject] GalaxyTransitionSignal galaxyTransitionSignal { get; set; }
  [Inject] GalaxyTransitionCompleteSignal galaxyTransitionCompleteSignal { get; set; }
  [Inject] FloatingText floatingNumbers {get; set;}
  [Inject] StringChanger stringChanger {get; set;}
  [Inject] StageRulesService stageRules { get; set; }
  [Inject] TutorialSystem tutorialSystem {get; set;}

  public GalaxyPanelManager panelManager;

  //Filled from stage 7 manager
  public Galaxy galaxy = null;

  public GameObject planetOverlay;

  //Description
  public TMP_Text primaryDescriptionText;
  public TMP_Text descriptionValuesText;
  public TMP_Text habitabilityValueText;

  //Resources
  public GalaxyResourceStack celestialResourceStack;
  public TitleLabelValueText miningLevelText;

  //Efficiency
  public TitleLabelValueText gravityText;
  public TitleLabelValueText settlementLevelText;
  public TitleLabelValueText totalEfficiencyText;

  //Colonization
  public GameObject colonizeArea;
  public ShinyButton colonizeButton;
  public RectTransform colonizeTextPoint;
  public TMP_Text nextShipText;

  CelestialBody celestial = null;
  GalaxySettlementData selectedSettlement = null;
  //Track sep for memory alloc passing
  List<GalaxyResource> selectedSettlementResources = new List<GalaxyResource>();

  public ShinyButton buildCancelButton;
  public BuildingPanel buildingPanel;
  public GameObject buildArea;
  public ShinyButton buildButton;
  public UIFilledBar buildingProgress;

  protected override void Awake () {
    base.Awake();
    selectedSettlement = null;

    colonizeButton.onClick.AddListener(ClickColonizeButton);
    celestialColonized.AddListener(OnColonySettled);

    galaxyTransitionSignal.AddListener(OnTransition);
    galaxyTransitionCompleteSignal.AddListener(OnTransitionComplete);

    buildButton.onClick.AddListener(StartBuilding);
    buildCancelButton.onClick.AddListener(EndBuilding);
    buildingPanel.loader = loader;
  }

  void Update () {
    if(selectedSettlement != null){
      UpdateSettlement();
    }else{
      UpdateNonSettlement();

      //Tutorial for colonizing first new world
      if(
        colonizeButton.gameObject.activeInHierarchy &&
        tutorialSystem.CompletedTutorial(709) &&
        //Hacky way for detecting if this is the pre setup world in galaxy gen for us
        celestial.data.diameterM == Constants.MARS_EQUIV_DIAMETER_M
      ){
        tutorialSystem.ShowTutorial(710, 1f);
      }
    }
  }

  void UpdateSettlement(){
    //Update panels
    colonizeArea.SetActive(false);
    buildArea.SetActive(true);
    UpdateBuildArea();
    UpdateEfficiency();

    selectedSettlementResources.Clear();
    selectedSettlementResources.AddRange(selectedSettlement.resources.Values);
    celestialResourceStack.UpdateResourceStack(ref selectedSettlementResources, selectedSettlement.resourceDeltas);
    celestialResourceStack.titleTMP.text = "Resources/Year";

    var miningBuilding = selectedSettlement.GetMinerBuilding();
    miningLevelText.gameObject.SetActive(true);
    miningLevelText.label.text = miningBuilding.shortName;
  }

  void UpdateBuildArea(){
    if(selectedSettlement.buildQueue.Count > 0){
      buildingProgress.gameObject.SetActive(true);
      var building = selectedSettlement.buildQueue[0];
      buildingProgress.fillAmt = building.progress;
      buildingProgress.label = GalaxyBuilding.allBuildings[building.buildingId].name;
      if(building.finished){
        buildingProgress.color = Colors.green;
      }else{
        buildingProgress.color = Colors.orange;
      }
    }else{
      buildingProgress.gameObject.SetActive(false);
    }
  }

  void UpdateNonSettlement(){
    if(celestial == null){ return; }

    UpdateEfficiency();

    colonizeArea.SetActive(true);
    buildArea.SetActive(false);

    var shipOnItsWay = stageSevenData.ships.FirstOrDefault(s => s.destCelestialBodyId == celestial.data.id);
    var colonizable = GetNearestSettlementWithColonyShip() != null;

    nextShipText.gameObject.SetActive(shipOnItsWay != null);
    colonizeButton.gameObject.SetActive(shipOnItsWay == null);

    if(shipOnItsWay != null){
      var yearsLeft = Mathf.RoundToInt(shipOnItsWay.totalTravelTime - shipOnItsWay.timeTraveling);
      stringChanger.UpdateString(nextShipText, "celestialmanager-ship", yearsLeft, "Colony arriving in {0} years" , yearsLeft);
    } else if(colonizable){
      colonizeButton.color = UIColor.Blue;
    }else{
      colonizeButton.color = UIColor.Gray;
    }
  }

  public void GotoCelestial(CelestialBody p){
    if(p == celestial && planetOverlay.activeSelf){
      //Work should be done already
      return;
    }
    celestial = p;

    selectedSettlement = stageSevenData.settlements.TryGet(p.data.id);

    UpdateDescription();

    if(selectedSettlement != null){
      UpdateSettlement();
    }else{

      //Keeping this outside of UpdateNonSettlement so it only happens once
      if(celestial.data.resourceDeposits != null){
        var resources = celestial.data.resourceDeposits.Select(rd => new GalaxyResource(){
          type = rd.type,
          totalAmount = GalaxyResource.startingAmount[rd.abundance]
        }).ToList();
        celestialResourceStack.UpdateResourceStack(ref resources, null);
      }else{
        celestialResourceStack.TearDownResourceStack();
      }
      celestialResourceStack.titleTMP.text = "Resources";
      miningLevelText.gameObject.SetActive(false);

      UpdateNonSettlement();
    }
  }

  void UpdateDescription(){
    var data = celestial.data;

    var typeDescrip = celestial.isMoon ? "Moon" : CelestialBodyData.cbTypeDescrip[celestial.data.type];
    primaryDescriptionText.text = string.Format("{0} {1} {2}",
      celestial.data.sizeClass, CelestialBodyData.cbSubTypeDescrip[data.subType], typeDescrip
    );

    var atmosphereText = string.IsNullOrEmpty(CelestialBodyData.cbAtmosphereDescrip[data.atmosphereType]) ?
      CelestialBodyData.cbAtmospherePressureDescrip[data.atmospherePressure] :
      string.Format("{0} & {1}",
        CelestialBodyData.cbAtmospherePressureDescrip[data.atmospherePressure],
        CelestialBodyData.cbAtmosphereDescrip[data.atmosphereType]
      );

    descriptionValuesText.text = string.Format("{0}\n{1}\n{2}\n{3}\n{4:P0}\n{5}\n",
      CelestialBodyData.cbClimateDescrip[data.climate],
      atmosphereText,
      data.volcanicActivity,
      data.tectonicActivity,
      data.hydrographicCoverage,
      data.radiationActivity
    );

    habitabilityValueText.text = data.habitability.ToString();
  }

  void UpdateEfficiency(){
    stringChanger.UpdateString(gravityText.label, "surface-g", celestial.data.surfaceGravityGs, "{0:0.00} g");
    stringChanger.UpdateString(gravityText.value, "surface-g-eff", celestial.data.GravityEfficiency, "{0:P0}");

    if(selectedSettlement != null){
      settlementLevelText.gameObject.SetActive(true);
      totalEfficiencyText.gameObject.SetActive(true);

      var settlementBuilding = selectedSettlement.GetSettlementBuilding();
      stringChanger.UpdateString(settlementLevelText.value, "settlement-eff", settlementBuilding.settlementEfficiency, "{0:P0}");
      settlementLevelText.label.text = settlementBuilding.shortName;

      totalEfficiencyText.label.text = "Total";
      //Little redundancy in the total efficiency calculation here between the split apart amounts but oh well
      stringChanger.UpdateString(totalEfficiencyText.value, "total-eff", selectedSettlement.TotalEfficiency(celestial.data), "{0:P0}");
    }else{
      settlementLevelText.gameObject.SetActive(false);
      totalEfficiencyText.gameObject.SetActive(false);
    }
  }

  public void GofromCb(){
    // planetOverlay.SetActive(false);
    celestialResourceStack.TearDownResourceStack();
    stringChanger.ClearValue("settlementLevel");
    stringChanger.ClearValue("settlementPopulation");
    stringChanger.ClearValue("surface-g");
    stringChanger.ClearValue("surface-g-eff");
    stringChanger.ClearValue("settlement-eff");
    stringChanger.ClearValue("total-eff");
    celestial = null;
    selectedSettlement = null;
  }

  void ClickColonizeButton(){
    var settlementWColony = GetNearestSettlementWithColonyShip();
    var colonyShipNeeded = celestial.GetColonyShipLevelNeeded();
    if(settlementWColony == null){
      //TODO: actually make a good error message, optional, figure out why the font size has to be so fucking big
      var colonyNeeded = GalaxyBuilding.allBuildings[colonyShipNeeded];
      var text = string.Format("Need a {0} built", colonyNeeded.name);
      floatingNumbers.Create(colonizeTextPoint, color: Color.white, text: text, moveUpPct: 0, ttl: 5f, fontSize: 320);
      return;
    }

    settlementWColony.buildings[colonyShipNeeded]--;

    var firstCbFromSettlementWithColony = stageSevenData.settlements.First(x => x.Value.parentStarId == settlementWColony.starId);

    colonizeCelestial.Dispatch(
      new GalaxySettlementCreationData() {
        sourceStarId = settlementWColony.starId,
        sourceCelestialId = firstCbFromSettlementWithColony.Key,
        destCelestialId = celestial.data.id,
        destStarId = celestial.star.generatedData.id,
        colonyBuilding = colonyShipNeeded
      }
    );
  }

  StarSettlementData GetNearestSettlementWithColonyShip(){
    if(celestial == null) return null;

    var colonyNeeded = celestial.GetColonyShipLevelNeeded();

    var starPosition = galaxy.stars[celestial.data.parentStarId].generatedData.position;

    return stageSevenData.starSettlements.Values
      .Where(settlement => settlement.buildings.TryGet(colonyNeeded) > 0)
      .OrderBy(settlement => Vector2.Distance(galaxy.stars[settlement.starId].generatedData.position, starPosition) )
      .FirstOrDefault();
  }

  void OnColonySettled(GalaxySettlementData settlementData){
    if(celestial != null && settlementData.parentCelestialId == celestial.data.id){
      selectedSettlement = settlementData;
    }
  }

  List<GalaxyBuildingInfo> buildingList = new List<GalaxyBuildingInfo>();

  void StartBuilding(){
    if(selectedSettlement == null){
      return;
    }

    //get all available buildings
    GetAvailableBuildings(buildingList);

    var starSettlement = stageSevenData.starSettlements[selectedSettlement.parentStarId];

    foreach(var buildingInfo in buildingList){
      buildingPanel.CreateBuildListItem((BuildingSelectionDisplay display) => {
        display.buildId = (int)buildingInfo.building.id;
        display.title = buildingInfo.building.name;
        display.descrip = buildingInfo.building.descrip;
        display.resourceCosts = buildingInfo.building.resourceCosts(stageRules.StageSevenRules);
        display.availableResources = starSettlement.resources;
        display.quantity = buildingInfo.quantity;
        display.totalQuantity = buildingInfo.quantity;
        display.iconPaths = buildingInfo.building.iconPaths;
        display.disabled = buildingInfo.disabled;
        display.OnClick = () => buildingPanel.ClickGalaxyBuildingUpgrade(buildingInfo, display, selectedSettlement.buildQueue, starSettlement.resources, stageRules.StageSevenRules);
      });
    }
    if(buildingList.Count == 0){
      if(selectedSettlement.HasBuilding(GalaxyBuildingId.Settlement5) && selectedSettlement.HasBuilding(GalaxyBuildingId.Miner5) ){
        buildingPanel.ShowEmptyMessage("This settlement is fully upgraded!");
      }else{
        buildingPanel.ShowEmptyMessage("Research more tech in Stage 6 to unlock more!");
      }
    }

    //completed buildings
    foreach(var buildingInfo in selectedSettlement.buildings){
      if(buildingInfo.Value == 0){ continue; }

      buildingPanel.CreateCompletedListItem((BuildingCreatedDisplay display) => {
        var buildingTemplate = GalaxyBuilding.allBuildings[buildingInfo.Key];
        var quantity = buildingInfo.Value;
        display.buildId = (int)buildingTemplate.id;
        display.title = buildingTemplate.name;
        display.descrip = buildingTemplate.descrip;
        display.iconPaths = buildingTemplate.iconPaths;
        display.quantity = quantity;
      });
    }

    buildingPanel.tabs.FocusTab(0);
    buildingPanel.comingFromPanel = GalaxyPanel.Celestial;

    //and show
    time.Pause();
    panelManager.SwitchTo(GalaxyPanel.Building);

    //recreate the build queue if we need to
    for(var i = 0; i < selectedSettlement.buildQueue.Count; i++){
      var building = selectedSettlement.buildQueue[i];
      buildingPanel.CreateGalaxyBuildQueueSelection(
        GalaxyBuilding.allBuildings[building.buildingId],
        building,
        selectedSettlement.buildQueue,
        starSettlement.resources,
        stageRules.StageSevenRules
      );
    }
  }

  void EndBuilding(){
    if(buildingPanel.comingFromPanel != GalaxyPanel.Celestial){ return; }
    buildingPanel.Clear();
    panelManager.SwitchTo(GalaxyPanel.Celestial);
    time.Resume();

    //tutorial check
    if(selectedSettlement.buildQueue.Count == 1 && selectedSettlement.buildQueue[0].buildingId == GalaxyBuildingId.Miner1){
      tutorialSystem.ShowTutorial(703, 1f);
    }
  }


  public void GetAvailableBuildings(List<GalaxyBuildingInfo> ret){
    ret.Clear();
    if(selectedSettlement == null){
      return;
    }

    foreach(var building in GalaxyBuilding.allBuildings.Values){
      //Only non system buildings here
      if(building.isSystemBuilding){
        continue;
      }
      int? quantity = building.repeatable ? (int?)null : 1;
      bool disabled = false;

      if(!building.repeatable){
        if(selectedSettlement.HasBuilding(building.id)){
          continue;
        }
        //still return the ones in the queue with quantity 0 so that if the player cancels it in the queue it can show back up
        if(selectedSettlement.buildQueue.Any(bq => bq.buildingId == building.id)){
          quantity = 0;
        }
      }

      if(building.resourceCosts(stageRules.StageSevenRules) == null){
        //Hack for Market level 0 for now
        continue;
      }

      if(building.requiredTech != HexTechId.None && !stageSixData.ResearchedTech(building.requiredTech) ){
        disabled = true;
      }

      if(building.prereqBuildings != null && building.prereqBuildings.Any(pmod => !selectedSettlement.HasBuilding(pmod) ) ){
        continue;
      }

      if(building.excludeBuildings != null && building.excludeBuildings.Any(pmod => selectedSettlement.HasBuilding(pmod) ) ){
        continue;
      }

      ret.Add(new GalaxyBuildingInfo(){
        building = building,
        quantity = quantity,
        disabled = disabled,
      });
    }

    ret.Sort((a, b) => GalaxyBuilding.GetSortOrder(a.building.id) - GalaxyBuilding.GetSortOrder(b.building.id) );
  }

  void OnTransition(GalaxyTransitionInfo info){
    GofromCb();
  }

  void OnTransitionComplete(GalaxyTransitionCompleteInfo info){
    if(info.to == GalaxyViewMode.Planet){
      GotoCelestial(info.transitioner.SelectedCb);
    }
  }
}
