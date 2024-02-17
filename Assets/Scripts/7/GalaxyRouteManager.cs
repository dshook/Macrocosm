using UnityEngine;
using strange.extensions.mediation.impl;
using UnityEngine.UI;
using PygmyMonkey.ColorPalette;
using System.Collections.Generic;
using System.Linq;
using MoreMountains.NiceVibrations;
using Shapes;

public class GalaxyRouteManager : View {
  public GalaxyTransportShipsManager transportShipsManager;
  public ShinyButton toggleEditButton;
  public CameraPanner camPanner;
  public ColorFader shipFader;
  public GalaxyClock galaxyClock;

  public Transform lineHolder;
  public GameObject linePrefab;
  public GameObject dottedCirclePrefab;

  public RectTransform routeButtonHolder;
  public GameObject routeButtonPrefab;
  public GameObject routeButtonIndicatorPrefab;

  public AudioClip connectStarsClip;
  public AudioClip disconnectStarsClip;

  [Inject] TimeService time { get; set; }
  [Inject] InputService input {get; set;}
  [Inject] StageSevenDataModel stageSevenData {get; set;}
  [Inject] GalaxyRouteCache routeCache {get; set;}
  [Inject] GalaxyRouteLinesUpdatedSignal routeLinesUpdated {get; set;}
  [Inject] GalaxyRouteResourceAssignedSignal routeResouceAssigned {get; set;}
  [Inject] SelectGalaxyRouteSignal selectGalaxyRoute {get; set;}
  [Inject] GalaxyStarImportExportChangedSignal importExportChangedSignal {get; set;}
  [Inject] TutorialSystem tutorialSystem {get; set;}
  [Inject] CameraService cameraService {get; set;}
  [Inject] AudioService audioService {get; set;}

  public Galaxy galaxy;

  bool editing = false;
  public bool Editing{ get{ return editing; } }

  uint selectedRoute = 1;

  Color defaultColor;
  Color editingColor;

  GalaxyRouteLine curLine;
  Disc circleIndicator;

  public bool madeChangesEditing = false;

  protected override void Awake () {
    base.Awake();

    routeResouceAssigned.AddListener(OnRouteResoureAssigned);
    selectGalaxyRoute.AddListener(ClickRouteButton);
    toggleEditButton.onClick.AddListener(OnToggleButton);
    importExportChangedSignal.AddListener(OnStarImportExportChanged);
    defaultColor = ColorPaletteData.Singleton.fromName("Stage 7").getColorFromName("Background").color;
    editingColor = ColorPaletteData.Singleton.fromName("Stage 7").getColorFromName("Edit Background").color;

    InitRouteButtons();
  }

  public void Init(bool isInitialCall, Galaxy galaxy){
    routeCache.BuildStarCircuits();
    foreach(var route in stageSevenData.routeConnections){
      var routeId = route.Key;
      foreach(var starOrigin in stageSevenData.routeConnections[routeId]){
        foreach(var destId in starOrigin.Value){
          var originId = starOrigin.Key;
          //recreate any lines that haven't been made yet
          if(!routeCache.createdRouteLines.ContainsKey(routeId) || !routeCache.createdRouteLines[routeId].Any(cl => routeCache.findRouteLine(cl, originId, destId))){
            var originStar = galaxy.stars[originId];
            var destStar = galaxy.stars[destId];
            var newLine = CreateLine(routeId, originStar);
            newLine.dest = destStar;
          }
        }
      }
      UpdateRouteLineColors(routeId);
    }
    UpdateLineDisplay();
    transportShipsManager.QueueUpdateTransportShips();
  }

  public void Cleanup(){
    lineHolder.DestroyChildren();
    if(routeCache != null){
      routeCache.ClearAll();
    }
  }

  void OnToggleButton(){
    editing = !editing;
    if(editing){
      time.Pause();
      shipFader.FadeOut(GalaxyTransitioner.fadeTime, false, true);
      galaxyClock.Hide();
      UpdateImportExportIndicators(selectedRoute, true);
      madeChangesEditing = false;

      if(tutorialSystem.CompletedTutorial(713) || stageSevenData.starSettlements.Count > 0 ){
        tutorialSystem.ShowTutorial(714);
      }
    }else{
      time.Resume();
      shipFader.FadeIn(GalaxyTransitioner.fadeTime, false, true);
      galaxyClock.Show();
      if(madeChangesEditing){
        transportShipsManager.QueueUpdateTransportShips();
      }
      UpdateImportExportIndicators(selectedRoute, false);

      //Show finished route tutorial when exiting with a valid route created
      if(
        tutorialSystem.CompletedTutorial(716) &&
        !tutorialSystem.CompletedTutorial(717) &&
        routeCache.allRouteCircuits.Any(rc => rc.Value.Any(c => c.IsValid))
      ){
        tutorialSystem.ShowTutorial(717);
      }
    }
    UpdateRouteButtonDisplay();
    UpdateStarColors();
    UpdateBgColor();
  }

  void Update(){
    if(!editing){ return; }

    //Set in update to handle the stage transition case coming back while still in edit mode
    UpdateBgColor();

    if(curLine != null){
      curLine.endPosition = input.pointerWorldPosition;

      //check to see if we're outside the market radius bounds
      if(Vector2.Distance(curLine.startPosition, curLine.endPosition) > circleIndicator.Radius ){
        BreakDrawingLine(true);
      }

      HandleStarUnderPointer();
    }

    if(input.GetButtonDown()){
      Star star = GetStarUnderPointer();

      if(star != null){
        MMVibrationManager.Haptic(HapticTypes.LightImpact);
        camPanner.Disable();
        StartNewRoute(star);
      }
    }
    if(input.GetButtonUp()){
      camPanner.Enable();
      if(curLine != null){
        Star star = GetStarUnderPointer();
        bool destroyCurLine = false;

        //Check to see if this is a valid new connection
        if(StarIsRoutable(star)){
          destroyCurLine = HandleRoutableStar(star);
        }else{
          destroyCurLine = true;
        }

        BreakDrawingLine(destroyCurLine);
      }
    }
    if(curLine != null){
      curLine.endPosition = input.pointerWorldPosition;
    }
  }

  bool StarIsRoutable(Star star){
    return
      star != null
      && curLine != null
      && star.generatedData.id != curLine.origin.generatedData.id
      && stageSevenData.starSettlements.ContainsKey(star.generatedData.id);
      // && stageSevenData.starSettlements[star.data.id].HasBuilding(GalaxyBuildingId.Market1);
  }

  void StartNewRoute(Star star){
    if(star != null && stageSevenData.starSettlements.ContainsKey(star.generatedData.id)){
      curLine = CreateLine(selectedRoute, star);
      circleIndicator = CreateCircleIndicator(star);
    }
  }

  bool HandleStarUnderPointer(){
    Star star = GetStarUnderPointer();
    bool destroyCurLine = false;

    if(StarIsRoutable(star)){
      destroyCurLine = HandleRoutableStar(star);

      BreakDrawingLine(destroyCurLine);
      StartNewRoute(star);
    }

    return destroyCurLine;
  }

  bool HandleRoutableStar(Star star){
    bool destroyCurLine = false;

    //see if it already exists, and if so disconnect
    if(
      stageSevenData.routeConnections.ContainsKey(selectedRoute) &&
      stageSevenData.routeConnections[selectedRoute].ContainsKey(star.generatedData.id) &&
      stageSevenData.routeConnections[selectedRoute][star.generatedData.id].Contains(curLine.origin.generatedData.id)
    ){
      DisconnectStars(selectedRoute, curLine.origin, star);
      var existingLine = routeCache.createdRouteLines[selectedRoute].FirstOrDefault(r => routeCache.findRouteLine(r, star.generatedData.id, curLine.origin.generatedData.id) );
      Destroy(existingLine.gameObject);
      routeCache.createdRouteLines[selectedRoute].Remove(existingLine);
      destroyCurLine = true;
      UpdateLineDisplay();

      UpdateCircuitRouteColor(curLine.origin.generatedData.id, selectedRoute);
      UpdateCircuitRouteColor(star.generatedData.id, selectedRoute);
    }else{
      ConnectStars(selectedRoute, curLine.origin, star);
      curLine.dest = star;
      UpdateLineDisplay();

      UpdateCircuitRouteColor(curLine.origin.generatedData.id, selectedRoute);
      UpdateCircuitRouteColor(star.generatedData.id, selectedRoute);
    }

    return destroyCurLine;
  }

  Star GetStarUnderPointer(){
    Star star = null;
    RaycastHit2D hit = Physics2D.Raycast(input.pointerWorldPosition, Vector3.forward);
    if(hit.collider != null){
      star = hit.transform.parent.GetComponent<Star>();
    }

    return star;
  }

  GalaxyRouteLine CreateLine(uint routeId, Star origin){
    var newLine = GameObject.Instantiate(linePrefab);
    newLine.transform.SetParent(lineHolder, false);

    var routeLine = newLine.GetComponent<GalaxyRouteLine>();
    routeLine.origin = origin;
    routeLine.routeId = routeId;
    routeLine.SetColor(GetRouteColor(routeId));

    if(!routeCache.createdRouteLines.ContainsKey(routeId)){
      routeCache.createdRouteLines[routeId] = new List<GalaxyRouteLine>();
    }
    routeCache.createdRouteLines[routeId].Add(routeLine);
    return routeLine;
  }

  Disc CreateCircleIndicator(Star origin){
    var newLine = GameObject.Instantiate(dottedCirclePrefab);
    newLine.transform.SetParent(lineHolder, false);
    newLine.transform.position = origin.transform.position;

    var circle = newLine.GetComponent<Disc>();
    var starMarket = origin.settlementData.GetMarket();
    var radius = starMarket != null ? starMarket.influenceRadiusLy ?? 0f : 0f;
    circle.Radius = radius / Galaxy.distanceScale[GalaxyViewMode.Galaxy];
    return circle;
  }

  void ConnectStars(uint routeId, Star s1, Star s2){
    ConnectStarOneWay(routeId, s1, s2);
    ConnectStarOneWay(routeId, s2, s1);

    madeChangesEditing = true;
    routeCache.BuildCircuitForRoute(routeId);

    if(tutorialSystem.CompletedTutorial(715)){
      tutorialSystem.ShowTutorial(716);
    }
    MMVibrationManager.Haptic(HapticTypes.MediumImpact);
    audioService.PlaySfx(connectStarsClip);
  }

  void ConnectStarOneWay(uint routeId, Star s1, Star s2){
    //Hook up the route connections
    if(!stageSevenData.routeConnections.ContainsKey(routeId)){
      stageSevenData.routeConnections[routeId] = new Dictionary<uint, HashSet<uint>>();
    }
    if(!stageSevenData.routeConnections[routeId].ContainsKey(s1.generatedData.id)){
      stageSevenData.routeConnections[routeId][s1.generatedData.id] = new HashSet<uint>();
    }

    stageSevenData.routeConnections[routeId][s1.generatedData.id].Add(s2.generatedData.id);

    //Hook up the starConnections
    if(!stageSevenData.starConnections.ContainsKey(s1.generatedData.id)){
      stageSevenData.starConnections[s1.generatedData.id] = new HashSet<uint>();
    }
    if(!stageSevenData.starConnections.ContainsKey(s2.generatedData.id)){
      stageSevenData.starConnections[s2.generatedData.id] = new HashSet<uint>();
    }

    stageSevenData.starConnections[s1.generatedData.id].Add(routeId);
    stageSevenData.starConnections[s2.generatedData.id].Add(routeId);
  }

  void DisconnectStars(uint routeId, Star s1, Star s2){
    stageSevenData.routeConnections[routeId][s1.generatedData.id].Remove(s2.generatedData.id);
    stageSevenData.routeConnections[routeId][s2.generatedData.id].Remove(s1.generatedData.id);

    if(stageSevenData.routeConnections[routeId][s1.generatedData.id].Count == 0){
      stageSevenData.starConnections[s1.generatedData.id].Remove(routeId);
    }
    if(stageSevenData.routeConnections[routeId][s2.generatedData.id].Count == 0){
      stageSevenData.starConnections[s2.generatedData.id].Remove(routeId);
    }

    madeChangesEditing = true;
    routeCache.BuildCircuitForRoute(routeId);
    MMVibrationManager.Haptic(HapticTypes.MediumImpact);
    audioService.PlaySfx(disconnectStarsClip);
  }

  void BreakDrawingLine(bool destroyCurLine){
    if(destroyCurLine){
      routeCache.createdRouteLines[curLine.routeId].Remove(curLine);
      GameObject.Destroy(curLine.gameObject);
    }
    curLine = null;

    GameObject.Destroy(circleIndicator.gameObject);
    circleIndicator = null;
  }


  void UpdateLineDisplay(){
    //first reset all the lines share stuff
    foreach(var routeId in routeCache.createdRouteLines.Keys){
      foreach(var line in routeCache.createdRouteLines[routeId]){
        line.sharedRoutes = 0;
        line.sharePriority = 0;
        line.UpdatePositions();
      }
    }

    var allLines = routeCache.createdRouteLines.SelectMany(x => x.Value);

    //first make sure all the settled stars get updated for at least 0 route size (for init before there are any routes)
    foreach(var ss in stageSevenData.starSettlements){
      galaxy.stars[ss.Value.starId].UpdateSettlementIndicatorSize(0);
    }

    //then group by starId and take the count of all routes
    foreach(var starConnection in stageSevenData.starConnections){
      galaxy.stars[starConnection.Key].UpdateSettlementIndicatorSize((uint)starConnection.Value.Count());
    }

    //Update all the line displays, grouping them by shared origin & dest pairs
    foreach(var routeId in routeCache.createdRouteLines.Keys){
      foreach(var line in routeCache.createdRouteLines[routeId]){
        if(line.sharedRoutes > 0){ continue; }

        var otherLines = allLines.Where(l =>
          l != line && (
            (l.origin == line.origin && l.dest == line.dest) || (l.origin == line.dest && l.dest == line.origin)
          )
        ).OrderBy(x => x.routeId);

        //cast should always be valid if we keep the number of possible routes below uint max
        var otherCount = (uint)(otherLines.Count() + 1);
        uint priority = 1;
        line.sharedRoutes = otherCount;
        line.sharePriority = priority;
        line.UpdatePositions();

        foreach(var otherLine in otherLines){
          otherLine.sharedRoutes = otherCount;
          otherLine.sharePriority = ++priority;
          otherLine.UpdatePositions();
        }
      }
    }

    routeLinesUpdated.Dispatch();
  }

  //Route Button stuff

  GameObject routeButtonIndicator;
  Dictionary<uint, GalaxyRouteButton> routeButtons = new Dictionary<uint, GalaxyRouteButton>();
  const uint maxRoutes = 7;

  void InitRouteButtons(){

    //Start at 1, so routeId 0 can mean no route
    for(uint i = 1; i <= maxRoutes; i++){
      var newButton = GameObject.Instantiate(routeButtonPrefab, Vector3.zero, Quaternion.identity, routeButtonHolder);

      var routeButton = newButton.GetComponent<GalaxyRouteButton>();

      var routeId = i;
      routeButton.routeId = routeId;
      routeButton.routeSelectDisplay.color = GetRouteColor(routeId);

      routeButtons[routeId] = routeButton;
    }

    routeButtonIndicator = GameObject.Instantiate(routeButtonIndicatorPrefab);

    UpdateRouteButtonDisplay();
  }

  int GetNumberOfRoutes(){
    //Find number of allowed routes based on how many markets are built, capping at 7 though
    int enabledRoutes = 0;
    foreach(var settlement in stageSevenData.starSettlements){
      if(settlement.Value.HasBuilding(GalaxyBuildingId.Market1)){
        enabledRoutes++;
      }
    }

    return Mathf.Min(7, enabledRoutes);
  }

  void UpdateRouteButtonDisplay(){

    routeButtonHolder.gameObject.SetActive(editing);

    if(editing){
      int enabledRoutes = GetNumberOfRoutes();

      for(uint i = 1; i <= maxRoutes; i++){
        routeButtons[i].gameObject.SetActive(i <= enabledRoutes);
      }
    }

    //route indicator
    if(editing){
      if(selectedRoute == 0){
        routeButtonIndicator.SetActive(false);
      }else{
        routeButtonIndicator.SetActive(true);
        routeButtonIndicator.transform.SetParent(routeButtons[selectedRoute].transform, false);
      }
    }else{
      routeButtonIndicator.SetActive(false);
    }
  }

  void UpdateStarColors(){
    var palette = editing ? StageSevenManager.StarPaletteInverted : StageSevenManager.StarPalette;
    foreach(var star in galaxy.stars.Values){
      star.UpdateDisplay(palette);
    }
    foreach(var bgStar in galaxy.bgStars){
      bgStar.UpdateDisplay(palette);
    }
  }

  void UpdateBgColor(){
    cameraService.Cam.backgroundColor = editing ? editingColor : defaultColor;
  }

  void ClickRouteButton(uint routeId){

    UpdateImportExportIndicators(selectedRoute, false);
    selectedRoute = routeId;
    UpdateImportExportIndicators(selectedRoute, true);

    UpdateRouteButtonDisplay();
    UpdateLineDisplay();
  }

  void UpdateImportExportIndicators(uint routeId, bool show){
    foreach(var settlement in stageSevenData.starSettlements){
      var star = galaxy.stars[settlement.Key];
      if(show && stageSevenData.routeResources.ContainsKey(routeId)){
        star.ShowImportExportIndicators(stageSevenData.routeResources[routeId]);
      }else{
        star.HideImportExportIndicators();
      }
    }
  }

  //Update all the lines in a circuit based on the routes validity score
  void UpdateCircuitRouteColor(uint starId, uint routeId){
    var circuit = routeCache.GetStarCircuit(starId, routeId);
    if(circuit == null){
      return;
    }

    var routeColor = GetRouteColor(routeId);
    if(!circuit.IsValid){
      routeColor = routeColor.ToHSV()
        .ChangeV(-0.20f)
        .ToColor();
    }


    foreach(var circuitStarId in circuit.starIds){
      if(!routeCache.createdRouteLines.ContainsKey(routeId)){ continue; }

      //Find all lines from this star on this route
      var starLines = routeCache.createdRouteLines[routeId].Where(cl=>
        (cl.origin != null && cl.dest != null) &&
        // cl.routeId == routeId &&
        (cl.origin.generatedData.id == circuitStarId || cl.dest.generatedData.id == circuitStarId)
      );

      foreach(var line in starLines){
        line.SetRouteLineValidity(circuit.IsValid);
        //Maintain the alpha here so when the lines are faded out they don't become visible with the change
        line.SetColor(routeColor.SetA(line.lineRenderer.Color.a));
      }
    }
  }

  void UpdateRouteLineColors(uint routeId){
    if(stageSevenData.routeConnections.ContainsKey(routeId)){
      //Update all circuits for the route's color
      //PERF: going to be duplicating effort here by not checking that we're going over the same star in different connections
      var starsInRoute = stageSevenData.routeConnections[routeId].Keys;
      foreach(var starId in starsInRoute){
        UpdateCircuitRouteColor(starId, routeId);
      }
    }
  }

  public static Color GetRouteColor(uint routeId){
    return ColorPaletteData.Singleton.fromName("Stage 7 Routes").getColorAtIndex((int)routeId);
  }

  void OnRouteResoureAssigned(uint routeId){
    routeCache.UpdateRouteCircuitsValidity(routeId);
    UpdateImportExportIndicators(routeId, editing);
    UpdateRouteLineColors(routeId);
    madeChangesEditing = true;

    if(tutorialSystem.CompletedTutorial(714)){
      tutorialSystem.ShowTutorial(715);
    }
  }

  void OnStarImportExportChanged(uint starId){
    // Debug.Log($"Star Import/Export changed {starId}");

    routeCache.UpdateStarCircuitsValidity(starId);

    if(stageSevenData.starConnections.ContainsKey(starId)){
      foreach(var routeId in stageSevenData.starConnections[starId]){
        UpdateCircuitRouteColor(starId, routeId);
      }
    }

    //Need to update transport ships to update the circuit ship distribution, but don't need to do rebuild circuits
    transportShipsManager.QueueUpdateTransportShips();
  }
}