using System;
using UnityEngine;
using strange.extensions.mediation.impl;
using System.Linq;
using System.Collections.Generic;

[Serializable]
public class GalaxyShipData {
  public GalaxyShipType type;
  public GalaxyBuildingId shipTypeId;

  public uint sourceStarId;
  public uint sourceCelestialBodyId;

  public uint destStarId;
  public uint destCelestialBodyId;

  public float timeTraveling;
  // public float velocity;
  public float maxVelocity; //in 0-1 %c
  public float acceleration; //in G

  public ShipTravelPhase phase = ShipTravelPhase.Idle;

  //these shouldn't be modified after init,
  //just here for convienience so we don't have to calc the ship travel stuff every frame
  public float totalTravelTime;
  public float totalDistance;

  //Transport ship data
  public uint routeId;
  public uint owningStarId;
  public GameResourceType resourceType;
  public int resources;
  public int maxResources;

  //Checks if we have a valid source and destination
  public bool ValidatePath(Galaxy galaxy){
    if(galaxy == null){
      Debug.LogWarning("Galaxy null for ship path");
      return false;
    }

    var hasSourceCelestial = galaxy.celestials.ContainsKey(sourceCelestialBodyId);
    var hasDestCelestial = galaxy.celestials.ContainsKey(destCelestialBodyId);
    var hasSourceStar = galaxy.stars.ContainsKey(sourceStarId);
    var hasDestStar = galaxy.stars.ContainsKey(destStarId);

    var validIds = hasSourceCelestial && hasDestCelestial && hasSourceStar && hasDestStar;

    if(!validIds){
      Debug.Log($"Invalid Ids for ship path\nsourceCelestial: {hasSourceCelestial}, destCelestial: {hasDestCelestial}, sourceStar: {hasSourceStar}, destStar: {hasDestStar}");
    }

    var goingSomewhere = sourceCelestialBodyId != destCelestialBodyId;

    // if(!goingSomewhere){
    //   Logger.Log("Ship not going anywhere");
    // }

    return validIds && goingSomewhere;
  }


  public static Dictionary<GalaxyBuildingId, string> shipPrefabPaths = new Dictionary<GalaxyBuildingId, string>(GalaxyBuilding.buildingComparer){
    {GalaxyBuildingId.Colony1, "Prefabs/7/Ship Colony 1"},
    {GalaxyBuildingId.Colony2, "Prefabs/7/Ship Colony 2"},
    {GalaxyBuildingId.Colony3, "Prefabs/7/Ship Colony 3"},
    {GalaxyBuildingId.Colony4, "Prefabs/7/Ship Colony 4"},
    {GalaxyBuildingId.Colony5, "Prefabs/7/Ship Colony 5"},

    {GalaxyBuildingId.Transport1, "Prefabs/7/Ship Transport 1"},
    {GalaxyBuildingId.Transport2, "Prefabs/7/Ship Transport 2"},
    {GalaxyBuildingId.Transport3, "Prefabs/7/Ship Transport 3"},
    {GalaxyBuildingId.Transport4, "Prefabs/7/Ship Transport 4"},
    {GalaxyBuildingId.Transport5, "Prefabs/7/Ship Transport 5"},
  };

  //Set data based on building template data
  public void SetParamsBasedOnShipType(){
    var buildingTemplate = GalaxyBuilding.allBuildings[shipTypeId];
    maxVelocity = buildingTemplate.maxMoveSpeed;
    acceleration = buildingTemplate.maxAcceleration;
    maxResources = buildingTemplate.maxTransportResources;
  }
}

public enum GalaxyShipType {
  Colony,
  Transport
}

public enum ShipTravelPhase {
  Accelerating,
  Cruising,
  Decelerating,
  Idle,
}

public class GalaxyShip : View {
  [Inject] StageSevenDataModel stageSevenData {get; set;}
  [Inject] GalaxyShipDoneSignal shipDone {get; set;}
  [Inject] GalaxyRouteCache routeCache {get; set;}
  [Inject] GalaxyRouteLinesUpdatedSignal routeLinesUpdated {get; set;}

  public Galaxy galaxy {get; set;}
  public GalaxyShipData data;

  public GameObject display;

  public TransportShipDisplay transportShipDisplay;

  ShipTravelInfo travelInfo;

  //Cached route line that the ship is traveling down if any
  GalaxyRouteLine routeLine = null;

  //Hack for now but transports should have their cargo bar set to full (max X) in prefab, then can change here
  float maxTransportFillScale;

  bool validInit = false;

  protected override void Awake() {
    base.Awake();
    routeLinesUpdated.AddListener(FindRouteLine);
  }

  protected override void OnDestroy(){
    routeLinesUpdated.RemoveListener(FindRouteLine);
  }

  public bool Init(){

    if(galaxy == null){
      validInit = false;
      Debug.LogWarning("Ship init failed with null galaxy");
      return false;
    }
    if(data == null){
      validInit = false;
      Debug.LogWarning("Ship init failed with null data");
      return false;
    }

    if(!data.ValidatePath(galaxy)){
      UpdateDisplay(); //Still update display here to hide idle ships
      validInit = false;
      // Debug.Log("Ship init failed with invalid path");
      return false;
    }

    var sourceCb = galaxy.celestials[data.sourceCelestialBodyId];
    var destCb = galaxy.celestials[data.destCelestialBodyId];

    data.SetParamsBasedOnShipType(); //should be redundant but just in case

    var totalDistance = Vector2.Distance(sourcePoint, destPoint) * Galaxy.distanceScale[GalaxyViewMode.Galaxy];
    if(data.sourceStarId == data.destStarId){
      //TODO: this may not work for moons, but it might not matter
      totalDistance = Mathf.Abs(sourceCb.parentDistanceAU - destCb.parentDistanceAU) * Galaxy.distanceScale[GalaxyViewMode.System];
    }

    travelInfo = ObservedTravelTimeYears(data.maxVelocity, data.acceleration, totalDistance);

    data.totalTravelTime = travelInfo.totalObservedTimeYears;
    data.totalDistance = travelInfo.distanceLy;

    //For initting the first time set position & rotation to avoid the lerp in
    if(!validInit){
      var tileCoordDiff = (destPoint - sourcePoint).normalized;
      transform.rotation = Quaternion.LookRotation(Vector3.forward, tileCoordDiff);

      Vector2 offset = Vector2.zero;
      if(routeLine != null && stageSevenData.viewMode == GalaxyViewMode.Galaxy){
        offset = routeLine.lineOffset;
      }
      var moveToPoint = UpdateMovePosition(offset);
      transform.position = moveToPoint;
    }

    /*
    Debug.Log(string.Format("Created ship {4}->{5} going {0} ly, {1} max vel, {2} accel, {3} total time",
      travelInfo.distanceLy,
      travelInfo.maxSpeedC,
      travelInfo.accelG,
      travelInfo.totalObservedTimeYears,
      sourceStar.data.id,
      destStar.data.id
    ));
    */
    UpdateDisplay();
    FindRouteLine();

    validInit = true;
    return true;
  }

  //After setting the ship data with new destination and initing, call StartMoving to start the ship accelerating
  public bool StartMoving(){
    if(!validInit){
      return false;
    }

    data.timeTraveling = 0;
    data.phase = ShipTravelPhase.Accelerating;
    UpdateDisplay();

    return true;
  }

  //Lots of noisy debugging code for NRE happening from ship Init - FindCircuitForShip
  Vector2 sourcePoint {
    get {
      if(galaxy == null){
        Debug.LogWarning("Galaxy null in ship sourcePoint");
      }
      if(!galaxy.stars.ContainsKey(data.sourceStarId)){
        Debug.LogWarning($"Invalid source star ID {data.sourceStarId} in ship sourcePoint");
      }
      var sourceStar = galaxy.stars[data.sourceStarId];
      if(sourceStar == null){
        Debug.LogWarning($"Star null for ID {data.sourceStarId} in ship sourcePoint");
      }
      if(stageSevenData.viewMode == GalaxyViewMode.Galaxy){
        if(sourceStar.generatedData == null) {
          Debug.LogWarning($"Generated data null for ID {data.sourceStarId} in ship sourcePoint");
        }
        return sourceStar.generatedData.position;
      }

      Vector2 sourceCbOffset = Vector2.zero;
      if(!galaxy.celestials.ContainsKey(data.sourceCelestialBodyId)) {
        Debug.LogWarning($"Invalid source celestial ID {data.sourceCelestialBodyId} in ship sourcePoint");
      }else{
        var sourceCb = galaxy.celestials[data.sourceCelestialBodyId];
        if(sourceCb == null){
          Debug.LogWarning($"Celestial null for ID {data.sourceCelestialBodyId} in ship sourcePoint");
        }else{
          sourceCbOffset = sourceCb.GetPositionOffsetFromStar(galaxy);
        }
      }
      return (Vector2)sourceStar.transform.position + sourceCbOffset;
    }
  }
  Vector2 destPoint {
    get {
      if(galaxy == null){
        Debug.LogWarning("Galaxy null in ship destPoint");
      }
      if(!galaxy.stars.ContainsKey(data.destStarId)){
        Debug.LogWarning($"Invalid dest star ID {data.destStarId} in ship destPoint");
      }
      var destStar = galaxy.stars[data.destStarId];
      if(destStar == null){
        Debug.LogWarning($"Star null for ID {data.destStarId} in ship destPoint");
      }
      if(stageSevenData.viewMode == GalaxyViewMode.Galaxy){
        if(destStar.generatedData == null) {
          Debug.LogWarning($"Generated data null for ID {data.destStarId} in ship destPoint");
        }
        return destStar.generatedData.position;
      }

      Vector2 destCbOffset = Vector2.zero;
      if(!galaxy.celestials.ContainsKey(data.destCelestialBodyId)) {
        Debug.LogWarning($"Invalid dest celestial ID {data.destCelestialBodyId} in ship destPoint");
      }else{
        var destCb = galaxy.celestials[data.destCelestialBodyId];
        if(destCb == null){
          Debug.LogWarning($"Celestial null for ID {data.destCelestialBodyId} in ship destPoint");
        }else{
          destCbOffset = destCb.GetPositionOffsetFromStar(galaxy);
        }
      }
      return (Vector2)destStar.transform.position + destCbOffset;
    }
  }

  void Update(){
    if(!validInit){
      return;
    }

    //point where we're going
    var tileCoordDiff = (destPoint - sourcePoint).normalized;
    transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(Vector3.forward, tileCoordDiff), 0.2f);

    Vector2 offset = Vector2.zero;
    if(routeLine != null && stageSevenData.viewMode == GalaxyViewMode.Galaxy){
      offset = routeLine.lineOffset;
    }


    if(data.phase == ShipTravelPhase.Idle){
      if(data.destStarId != 0 && data.destCelestialBodyId != 0){
        transform.position = destPoint + offset;
      }
      return;
    }

    var timeAmt = Time.deltaTime * stageSevenData.timeRate;
    data.timeTraveling += timeAmt;

    var moveToPoint = UpdateMovePosition(offset);
    transform.position = Vector2.Lerp(transform.position, moveToPoint, 0.2f);


    if(data.type == GalaxyShipType.Transport && transportShipDisplay != null){
      transportShipDisplay.fillAmt = (float)data.resources / (float)data.maxResources;
    }

  }

  Vector2 UpdateMovePosition(Vector2 offset){
    Vector2 moveToPoint = transform.position;

    //find accel end point by taking the % along the whole vector route
    Vector2 accelEndPoint = sourcePoint + (destPoint - sourcePoint) * (travelInfo.distanceBoostingLy / travelInfo.distanceLy);
    // Vector2 decelStartPoint = destPoint;
    var decelStartPoint = sourcePoint + (destPoint - sourcePoint) * ((travelInfo.distanceBoostingLy + travelInfo.distanceCruisingLy) / travelInfo.distanceLy);

    if(data.phase == ShipTravelPhase.Idle){
      moveToPoint = sourcePoint + offset;
    }else if(data.phase == ShipTravelPhase.Accelerating){
      var phaseProgress = data.timeTraveling / travelInfo.observedTimeBoostingYears;

      //this should technically be an acceleration curve up I think, but for now the lerp is easier
      moveToPoint = Vector2.Lerp(sourcePoint, accelEndPoint, phaseProgress) + offset;

      if(phaseProgress >= 1f){
        if(travelInfo.observedTimeCruisingYears > 0f){
          data.phase = ShipTravelPhase.Cruising;
        }else{
          data.phase = ShipTravelPhase.Decelerating;
        }
      }
    }else if(data.phase == ShipTravelPhase.Cruising){
      var phaseProgress = (data.timeTraveling - travelInfo.observedTimeBoostingYears) / travelInfo.observedTimeCruisingYears;

      moveToPoint = Vector2.Lerp(accelEndPoint, decelStartPoint, phaseProgress) + offset;

      if(phaseProgress >= 1f){
        data.phase = ShipTravelPhase.Decelerating;
      }
    }else if(data.phase == ShipTravelPhase.Decelerating){
      var phaseProgress = (data.timeTraveling - travelInfo.observedTimeBoostingYears - travelInfo.observedTimeCruisingYears) / travelInfo.observedTimeBoostingYears;

      //same as accel, this should be a decelleration curve
      moveToPoint = Vector2.Lerp(decelStartPoint, destPoint, phaseProgress) + offset;

      if(phaseProgress >= 1f){
        FinishMoving();
      }
    }

    return moveToPoint;
  }

  void FinishMoving(){
    //immediately set the ship phase to idle once the route is done, any route updates from there should go through Init again
    data.phase = ShipTravelPhase.Idle;
    routeLine = null;
    UpdateDisplay();

    shipDone.Dispatch(this);
  }

  public void UpdateDisplay(){
    if(data.type == GalaxyShipType.Transport && transportShipDisplay != null){
      transportShipDisplay.color = GalaxyRouteManager.GetRouteColor(data.routeId);
    }

    var showDisplay = data.phase != ShipTravelPhase.Idle;

    display.SetActive(showDisplay);
  }

  //Find the line we're travling down so we can use its offset later
  void FindRouteLine(){
    routeLine = null;

    if(routeCache == null || routeCache.createdRouteLines == null || !routeCache.createdRouteLines.ContainsKey(data.routeId)){
      return;
    }

    routeLine = routeCache.createdRouteLines[data.routeId].FirstOrDefault(r =>
      routeCache.findRouteLine(r, data.sourceStarId, data.destStarId)
    );
  }

  public static ShipTravelInfo ObservedTravelTimeYears(float maxSpeedC, float accelG, float distanceLy)
  {
    //DEBUG HACK! SHOULD NOT EXCEED LIGHT SPEEEEED!
    if(maxSpeedC > 1){
      Debug.LogWarning("Creating warp speed ship!");
      return new ShipTravelInfo{
        maxSpeedC = maxSpeedC,
        accelG = accelG,
        distanceLy = distanceLy,
        totalObservedTimeYears = maxSpeedC,
        observedTimeBoostingYears = 0.001f,
        observedTimeCruisingYears = maxSpeedC,
        distanceBoostingLy = 0.001f,
        distanceCruisingLy = distanceLy
      };
    }

    double maxVelocityMPS = Constants.SPEED_OF_LIGHT * maxSpeedC;
    double acc = accelG * Constants.G;

    //Calculate boosting
    double shipBoost = Constants.SPEED_OF_LIGHT / acc * Atanh(maxVelocityMPS / (double)Constants.SPEED_OF_LIGHT);

    var rapidity = acc * shipBoost / Constants.SPEED_OF_LIGHT;
    var earthTimeBoost = Math.Sinh(rapidity) * Constants.SPEED_OF_LIGHT / acc;
    var distanceBoost = (Math.Cosh(rapidity) - 1) * Constants.SPEED_OF_LIGHT * Constants.SPEED_OF_LIGHT / acc;
    var velocity = Math.Tanh(rapidity) * Constants.SPEED_OF_LIGHT;
    var distAll = distanceLy * Constants.LIGHT_YEAR;

    //cap boosting time to half the journey (with 0 cruise time)
    if(distanceBoost > distAll / 2f){
      distanceBoost = distAll / 2f;
    }

    //Calculate cruise
    var gamma = 1 / Math.Sqrt(1 - velocity * velocity / (Constants.SPEED_OF_LIGHT * Constants.SPEED_OF_LIGHT));
    var distanceCruise = distAll - 2 * distanceBoost;
    var earthTimeCruise = distanceCruise / velocity;

    var shipTimeCruise = earthTimeCruise / gamma;

    // Now recompute everything based on earthTime
    shipTimeCruise = earthTimeCruise / gamma;
    distanceCruise = earthTimeCruise * velocity;
    var earthTimeAll = earthTimeBoost * 2 + earthTimeCruise;
    var shipTimeAll = shipBoost * 2 + shipTimeCruise;
    var distanceAll = distanceBoost * 2 + distanceCruise;

    return new ShipTravelInfo{
      maxSpeedC = maxSpeedC,
      accelG = accelG,
      distanceLy = distanceLy,
      totalObservedTimeYears = (float)(earthTimeAll / Constants.SECONDS_PER_YEAR),
      observedTimeBoostingYears = (float)(earthTimeBoost / Constants.SECONDS_PER_YEAR),
      observedTimeCruisingYears = (float)(earthTimeCruise / Constants.SECONDS_PER_YEAR),
      distanceBoostingLy = (float)(distanceBoost / Constants.LIGHT_YEAR),
      distanceCruisingLy = (float)(distanceCruise / Constants.LIGHT_YEAR)
    };
  }

  static double Atanh(double x)
  {
    return 0.5d * Math.Log((1d + x) / (1d - x));
  }

  public struct ShipTravelInfo {
    public float maxSpeedC;
    public float accelG;
    public float distanceLy;
    public float totalObservedTimeYears;
    public float observedTimeBoostingYears;
    public float observedTimeCruisingYears;
    public float distanceBoostingLy;
    public float distanceCruisingLy;
  }
}