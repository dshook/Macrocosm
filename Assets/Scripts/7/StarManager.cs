using TMPro;
using UnityEngine;
using strange.extensions.mediation.impl;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine.UI;
using Shapes;
using ntw.CurvedTextMeshPro;

public class StarManager : View {
  [Inject] StageSevenDataModel stageSevenData {get; set;}
  [Inject] ResourceLoaderService loader {get; set;}
  [Inject] TimeService time { get; set; }
  [Inject] CelestialColonizedSignal celestialColonized { get; set; }
  [Inject] GalaxyTransitionSignal galaxyTransitionSignal { get; set; }
  [Inject] GalaxyTransitionCompleteSignal galaxyTransitionCompleteSignal { get; set; }
  [Inject] StageSixDataModel stageSixData {get; set;}
  [Inject] StageRulesService stageRules { get; set; }
  [Inject] TutorialSystem tutorialSystem {get; set;}

  public GalaxyPanelManager panelManager;
  public BuildingPanel buildingPanel;
  public GalaxyTransitioner galaxyTransitioner;

  public GameObject starOverlay;

  public TMP_Text starNameText;

  //Description
  public TMP_Text primaryDescriptionText;
  public TMP_Text starDescriptionText;

  //Buildings
  public ShinyButton buildButton;
  public ShinyButton buildCancelButton;
  public GameObject buildArea;
  public UIFilledBar buildingProgress;

  //Market
  public ShinyButton marketButton;
  public StarMarketDisplay starMarketDisplay;

  //Factory
  public ShinyButton factoryButton;
  public GalaxyFactoryManager factoryManager;

  //Explore
  public GameObject exploreArea;
  public ShinyButton exploreGoodButton;
  public ShinyButton exploreBadButton;

  public ShinyButton previousStarButton;
  public ShinyButton nextStarButton;

  public StarExploreManager starExploreManager;

  public Transform distanceIndicatorHolder;
  public GameObject systemRadiusCirclePrefab;
  public ColorFader distanceIndicatorFader;

  public Star star {get; set;}

  //List of starIds that have been settled in order of their founding, for the prev & next buttons
  List<uint> starsSettled;

  protected override void Awake () {
    base.Awake();

    galaxyTransitionSignal.AddListener(OnTransition);
    galaxyTransitionCompleteSignal.AddListener(OnTransitionComplete);

    buildButton.onClick.AddListener(StartBuilding);
    buildCancelButton.onClick.AddListener(EndBuilding);
    marketButton.onClick.AddListener(ClickMarketButton);
    factoryButton.onClick.AddListener(ClickFactoryButton);
    exploreGoodButton.onClick.AddListener(ClickExploreLooksGood);
    exploreBadButton.onClick.AddListener(ClickExploreLooksBad);
    previousStarButton.onClick.AddListener(OnPrevStarClick);
    nextStarButton.onClick.AddListener(OnNextStarClick);

    buildingPanel.loader = loader;
  }

  void Update () {
    if(star == null){ return; }

    if(star.generatedData.inhabited){
      exploreArea.SetActive(false);

      marketButton.gameObject.SetActive(true);
      factoryButton.gameObject.SetActive(star.settlementData.HasBuilding(GalaxyBuildingId.Factory1));

      buildArea.SetActive(true);
      UpdateBuildArea();

      //tutorial check after first system factory is built
      if(star.settlementData.HasBuilding(GalaxyBuildingId.Factory1)){
        tutorialSystem.ShowTutorial(706, 1f);
      }
    }else{
      buildArea.SetActive(false);
      marketButton.gameObject.SetActive(false);
      factoryButton.gameObject.SetActive(false);

      exploreArea.SetActive(true);

    }

    var prevNextButtonsEnabled = star.generatedData.inhabited && stageSevenData.starSettlements.Count > 1;
    previousStarButton.gameObject.SetActive(prevNextButtonsEnabled);
    nextStarButton.gameObject.SetActive(prevNextButtonsEnabled);

    if(star.generatedData.inhabited &&
       tutorialSystem.CompletedTutorial(707) &&
       star.settlementData.resources.ContainsKey(GameResourceType.IronPhosporus)
    ){
      tutorialSystem.ShowTutorial(708);
    }

    if(star.generatedData.inhabited &&
       tutorialSystem.CompletedTutorial(708) &&
       star.settlementData.HasBuilding(GalaxyBuildingId.Colony1)
    ){
      tutorialSystem.ShowTutorial(709);
    }
  }

  void UpdateBuildArea(){
    if(star.settlementData.buildQueue.Count > 0){
      buildingProgress.gameObject.SetActive(true);
      var building = star.settlementData.buildQueue[0];
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

  public void GetAvailableBuildings(List<GalaxyBuildingInfo> ret, Star star){
    ret.Clear();
    var starData = star.generatedData;
    if(!starData.inhabited){
      return;
    }
    var settlementData = star.settlementData;

    foreach(var building in GalaxyBuilding.allBuildings.Values){
      //Only system buildings here
      if(!building.isSystemBuilding){
        continue;
      }
      int? quantity = building.repeatable ? (int?)null : 1;
      bool disabled = false;

      if(!building.repeatable){
        if(settlementData.HasBuilding(building.id)){
          continue;
        }
        //still return the ones in the queue with quantity 0 so that if the player cancels it in the queue it can show back up
        if(settlementData.buildQueue.Any(bq => bq.buildingId == building.id)){
          quantity = 0;
        }
      }else if(building.maxQuantity.HasValue){
        var buildingCount = 0;
        settlementData.buildings.TryGetValue(building.id, out buildingCount);
        buildingCount += settlementData.buildQueue.Where(bq => bq.buildingId == building.id).Count();

        quantity = (int)building.maxQuantity.Value - buildingCount;
      }else if(stageSevenData.buildingLimits.ContainsKey(building.id)){
        // Should be similar logic to above but looking at the globals instead of just this settlement
        var buildingCount = 0;
        foreach(var ss in stageSevenData.starSettlements){
          var localCount = 0;
          ss.Value.buildings.TryGetValue(building.id, out localCount);
          localCount += ss.Value.buildQueue.Where(bq => bq.buildingId == building.id).Count();

          buildingCount += localCount;
        }

        quantity = (int)stageSevenData.buildingLimits[building.id] - buildingCount;
      }

      if(building.resourceCosts(stageRules.StageSevenRules) == null){
        //Hack for Market level 0 for now
        continue;
      }

      if(building.requiredTech != HexTechId.None && !stageSixData.ResearchedTech(building.requiredTech) ){
        disabled = true;
      }

      if(building.prereqBuildings != null && !building.prereqBuildings.Any(pmod => settlementData.HasBuilding(pmod) ) ){
        continue;
      }

      if(building.excludeBuildings != null && building.excludeBuildings.Any(pmod => settlementData.HasBuilding(pmod) ) ){
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

  List<GalaxyBuildingInfo> buildingList = new List<GalaxyBuildingInfo>();

  void StartBuilding(){
    //get all available buildings
    GetAvailableBuildings(buildingList, star);

    foreach(var buildingInfo in buildingList){
      buildingPanel.CreateBuildListItem((BuildingSelectionDisplay display) => {
        display.buildId = (int)buildingInfo.building.id;
        display.title = buildingInfo.building.name;
        display.descrip = buildingInfo.building.descrip;
        display.resourceCosts = buildingInfo.building.resourceCosts(stageRules.StageSevenRules);
        display.availableResources = star.settlementData.resources;
        display.quantity = buildingInfo.quantity;
        display.totalQuantity = buildingInfo.quantity;
        display.iconPaths = buildingInfo.building.iconPaths;
        display.disabled = buildingInfo.disabled;
        display.OnClick = () => buildingPanel.ClickGalaxyBuildingUpgrade(buildingInfo, display, star.settlementData.buildQueue, star.settlementData.resources, stageRules.StageSevenRules);
      });
    }

    //completed buildings
    foreach(var buildingInfo in star.settlementData.buildings){
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
    buildingPanel.comingFromPanel = GalaxyPanel.Star;

    //and show
    time.Pause();
    panelManager.SwitchTo(GalaxyPanel.Building);

    //recreate the build queue if we need to
    for(var i = 0; i < star.settlementData.buildQueue.Count; i++){
      var building = star.settlementData.buildQueue[i];
      buildingPanel.CreateGalaxyBuildQueueSelection(
        GalaxyBuilding.allBuildings[building.buildingId],
        building,
        star.settlementData.buildQueue,
        star.settlementData.resources,
        stageRules.StageSevenRules
      );
    }
  }

  void EndBuilding(){
    if(buildingPanel.comingFromPanel != GalaxyPanel.Star){ return; }
    buildingPanel.Clear();
    panelManager.SwitchTo(GalaxyPanel.Star);
    time.Resume();
  }


  public void GotoStar(Star s){
    star = s;

    starNameText.text = star.generatedData.name;
    UpdateDescription();
    RemoveUnusedResources();
    RestoreExploreButtons();
    Update(); //manually call update so we avoid a one frame pop of UI

    starOverlay.SetActive(true);
    if(star.data == null){
      // starOverlay.SetActive(false);
      starExploreManager.StartExploring(star);
    }else{
    }
    ShowSystemIndicatorLines(s);

    //tutorials
    if(star.generatedData.inhabited && tutorialSystem.CompletedTutorial(704)){
      tutorialSystem.ShowTutorial(705, 1f);
    }
  }

  public void GofromStar(){
    starOverlay.SetActive(false);
    starExploreManager.FinishExploring(true);
    star = null;
    HideSystemIndicatorLines();
  }

  void UpdateDescription(){
    var data = star.generatedData;

    primaryDescriptionText.text = string.Format("{0}{1} {2} {3}",
      data.type, data.subType, data.luminosityClass, Star.luminosityClassDescrip[data.luminosityClass]
    );

    starDescriptionText.text = string.Format("{0} Solar Masses\n{1} Kelvin\n{2}B Years Old",
      data.solarMass.ToShortFormat(),
      data.tempK,
      data.age.ToShortFormat()
    );
  }

  //when finished the exploring animation for new systems
  public void OnFinishedExploring(bool wasCancelled){
    if(!wasCancelled){
      starOverlay.SetActive(true);
    }
  }

  //Remove all resources that have 0 amounts and aren't importing or exporting
  void RemoveUnusedResources(){
    if(star.settlementData == null){ return; }

    foreach(var resource in star.settlementData.resources.Keys.ToList()){
      var res = star.settlementData.resources[resource];
      if(res.amount == 0 && !res.importing && !res.exporting){
        star.settlementData.resources.Remove(resource);
      }
    }
  }

  void ClickMarketButton(){
    starMarketDisplay.Open(star.settlementData);
  }

  void ClickFactoryButton(){
    factoryManager.OpenFactory(star.settlementData);
  }

  //Explore button stuff
  //Color blue = neutral, color green = on
  void ClickExploreLooksGood(){
    exploreGoodButton.color = exploreGoodButton.color == UIColor.Blue ? UIColor.Green : UIColor.Blue;
    if(exploreGoodButton.color == UIColor.Green && exploreBadButton.color == UIColor.Green){
      exploreBadButton.color = UIColor.Blue;
    }

    UpdateStarExploreData();
  }
  void ClickExploreLooksBad(){
    exploreBadButton.color = exploreBadButton.color == UIColor.Blue ? UIColor.Green : UIColor.Blue;
    if(exploreGoodButton.color == UIColor.Green && exploreBadButton.color == UIColor.Green){
      exploreGoodButton.color = UIColor.Blue;
    }

    UpdateStarExploreData();
  }

  void RestoreExploreButtons(){
    var starData = stageSevenData.starData.TryGet(star.generatedData.id);
    if(starData == null || starData.exploreStatus == StarExploreStatus.Seen){
      exploreGoodButton.color = UIColor.Blue;
      exploreBadButton.color = UIColor.Blue;
      return;
    }

    if(starData.exploreStatus == StarExploreStatus.LooksGood){
      exploreGoodButton.color = UIColor.Green;
      exploreBadButton.color = UIColor.Blue;
    }
    if(starData.exploreStatus == StarExploreStatus.LooksBad){
      exploreBadButton.color = UIColor.Green;
      exploreGoodButton.color = UIColor.Blue;
    }
  }

  void UpdateStarExploreData(){
    if(star == null){
      //Unity started calling OnExploreChange through the event system on startup? Wtf?
      return;
    }
    StarExploreStatus newStatus = StarExploreStatus.Seen;
    if(exploreGoodButton.color == UIColor.Green){
      newStatus = StarExploreStatus.LooksGood;
    }
    if(exploreBadButton.color == UIColor.Green){
      newStatus = StarExploreStatus.LooksBad;
    }
    if(!stageSevenData.starData.ContainsKey(star.generatedData.id)){
      stageSevenData.starData[star.generatedData.id] = new StarData();
    }
    stageSevenData.starData[star.generatedData.id].exploreStatus = newStatus;
    star.UpdateExploreDisplay();
  }

  //prev & next buttons
  void OnPrevStarClick(){
    GoToNextStar(false);
  }
  void OnNextStarClick(){
    GoToNextStar(true);
  }

  void GoToNextStar(bool forward){
    var offset = forward ? 1 : -1;
    if(starsSettled == null || starsSettled.Count == 0){
      starsSettled = stageSevenData.starSettlements
        .OrderBy(x => x.Value.foundedAt)
        .Select(x => x.Key)
        .ToList();
    }
    if(starsSettled.Count == 0){
      Debug.LogWarning("Could not to to prev/next star with 0 settled stars");
      return;
    }
    var settlementIndex = starsSettled.IndexOf(star.generatedData.id);
    var newIndex = (settlementIndex + offset) % starsSettled.Count;
    newIndex = newIndex < 0 ? newIndex + starsSettled.Count : newIndex;

    galaxyTransitioner.TransitionToSystem(starsSettled[newIndex], true);
  }

  void OnCelestialColonize(GalaxySettlementData settlement){
    if(starsSettled != null && !starsSettled.Contains(settlement.parentStarId)){
      starsSettled.Add(settlement.parentStarId);
    }
  }

  void ShowSystemIndicatorLines(Star s){
    distanceIndicatorHolder.transform.position = s.transform.position;

    if(distanceIndicatorHolder.childCount == 0){
      CreateSystemIndicatorLines();
    }

    distanceIndicatorFader.FadeIn(GalaxyTransitioner.transitionTime / 2f);
  }

  void CreateSystemIndicatorLines(){
    float[] distances      = new float[]{ 0.5f, 1f, 2f, 4f, 8f, 16f, 32f };
    float[] angularOffsets = new float[]{ 51.5f, 12.9f, -8.9f, -23.2f, -33.3f, -41.0f, -46.9f };

    for(var i = 0; i < distances.Length; i++){
      var dist = distances[i];

      var indicator = Instantiate<GameObject>(systemRadiusCirclePrefab, Vector3.zero, Quaternion.identity);
      var distString = dist.ToString();
      indicator.name = distString;
      indicator.transform.SetParent(distanceIndicatorHolder, false);

      var radius = Galaxy.GetSystemViewScale(dist);

      var line = indicator.GetComponentInChildren<Disc>();
      var label = indicator.GetComponentInChildren<TMP_Text>();
      var labelModifier = indicator.GetComponentInChildren<TextProOnACircle>();

      line.Radius = radius;
      label.text = distString + " AU";
      labelModifier.m_radius = radius;
      labelModifier.m_arcDegrees = 26f / radius;
      labelModifier.m_angularOffset = angularOffsets[i];
      labelModifier.m_bottomOfCircle = false;
    }

    distanceIndicatorFader = distanceIndicatorHolder.GetComponent<ColorFader>();
    distanceIndicatorFader.UpdateComponents();

  }

  void HideSystemIndicatorLines(){
    if(distanceIndicatorFader != null){
      distanceIndicatorFader.FadeOut(GalaxyTransitioner.transitionTime / 8f);
    }
  }

  void OnTransition(GalaxyTransitionInfo info){
    GofromStar();
  }

  void OnTransitionComplete(GalaxyTransitionCompleteInfo info){
    if(info.to == GalaxyViewMode.System){
      GotoStar(info.transitioner.SelectedStar);
    }
  }
}
