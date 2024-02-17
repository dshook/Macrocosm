using UnityEngine;
using strange.extensions.mediation.impl;
using System.Linq;
using System;
using System.Collections.Generic;

public class GalaxyTransportShipsManager : View {
  [Inject] GalaxyRouteCache routeCache {get; set;}
  [Inject] StageSevenDataModel stageSevenData {get; set;}
  [Inject] GalaxyShipDoneSignal shipDone {get; set;}
  [Inject] CreateShipSignal createShipSignal {get; set;}
  [Inject] GalaxyRouteResourceAssignedSignal routeResouceAssigned {get; set;}
  [Inject] GalaxyBuildingFinishedSignal buildingFinishedSignal {get; set;}
  [Inject] GalaxyStarImportExportChangedSignal importExportChangedSignal {get; set;}

  public Galaxy galaxy;

  bool needsTransportUpdate = false;

  protected override void Awake () {
    base.Awake();

    shipDone.AddListener(OnShipDoneSignal);
    routeResouceAssigned.AddListener(OnRouteResoureAssigned);
    buildingFinishedSignal.AddListener(OnBuildingFinished);
    importExportChangedSignal.AddListener(OnStarImportExportChanged);
  }

  void Update(){
    if(needsTransportUpdate && Time.deltaTime > 0){
      needsTransportUpdate = false;
      UpdateTransportShips();
    }
  }

  public void QueueUpdateTransportShips(){
    needsTransportUpdate = true;
  }

  /*
    Update Transport ships
    Assumes the routeCache.BuildStarCircuits have been updated prior to running
    Rules For Transporting shieet:
      r1) Each star gets another transport when it builds a market ✓
      r2) Transport ships follow the route line all the way around regardless of their owning star ✓
      r3) The ships should be spread amongst all the routes that are connected to the star ✓
      r4) Only routes that have a resource assigned to them are considered ✓
      r5) If a transport ship has its route cut off from its owning star, or r8 no longer is true, it will try to switch routes to the nearest star & route
          that needs another ship. or if there are none, go idle
      r6) When creating ships, 2 ships of the same type can't leave from the same star to the same dest on the same route so they won't be stacked right on top of each other ✓
      r7) When dropping off cargo, a system importing gets the
          (shipMaxCargo / how many stars are importing this resource before the next exporter on the route) fraction of resources ✓
          Similarly for picking up cargo, pick up a fraction based on how many exporters are next
      r8) Only circuits that have at least one importer and one exporter are considered ✓
      r9) Changes happening in edit mode should wait till exiting edit mode to take affect & spawn new ships ✓
      r10) When choosing a path for a route, transports should prefer to go down the one that takes them to an exporter ASAP
    Situations to handle:
      s1) A new route is created between two stars who have no allocated ships & new ships needs to be made ✓
      s2) An existing route is joined to a star with idle ships & ships need to be dispatched
      s3) A route is removed between two stars as in r5, nothing happens immediately but when ships reach their dest:
          they need to switch to another route as described in r5
      s5) A market is built and new transport ships need to be made ✓
      s6) A route has its resource changed while there are ships out. When they reach their dest they should dump their cargo & switch to the new resource ✓
      s7) A circuit route may end up with too many ships from r5, which need to be rebalanced as they get to other stars ✓
      s8) A resource is assigned to a route and we need to UpdateTransportShips ✓
      s9) A star changes its imports or exports, and needs to UpdateTransportShips for something like r8 ✓

    Switching to ships not being owned by a star TODO:
      * Update s5 to explicitly build a new ship at the correct place before running the update logic ✓
      * Update r3 to have the ships be spread amongst all the routes regardless of parent star ✓
      * Update r5 to have the ship go to nearest route in need instead of parent star ✓
      * s3 shouldn't need ship to go ot home star - same as above ✓
      * Check to see s1 handles not just new ships being made, but potentially dispatched ✓
      * Add new rule for created cargo ships that are unassigned to a route and are idle ✓
      * Update s2 to just dispatch, not create new ships ✓
      * s4 not needed anymore, ship is created when the market finishes building, even if it's idle ✓
      * Update s7 to happen whenever a ship gets to a dest, not just the parent star ✓
      * Handle if a route has two or more disjoint loops/paths.  Ship should be balanced between them ✓
  */
  void UpdateTransportShips(){
    // Debug.Log("UpdateTransportShips");
    UpdateCircuitShipDistribution();
    UpdateIdleShips();
  }

  void UpdateIdleShips(){

    var idleShips = galaxy.ships.Where(s =>
      s.data.type == GalaxyShipType.Transport &&
      s.data.phase == ShipTravelPhase.Idle
    );

    foreach(var idleShip in idleShips){
      FindCircuitForShip(idleShip);
    }
  }

  void FindCircuitForShip(GalaxyShip idleShip){

    var currentStarId = idleShip.data.destStarId;

    // Try to find circuit that includes current star that has a deficiency of the right kind of transport
    var routeInfoAtCurrentStar = FindCircuitForShipAtStar(idleShip, currentStarId);
    if(routeInfoAtCurrentStar != null){

      // If found assign & done
      idleShip.data.routeId = routeInfoAtCurrentStar.Value.routeId;
      idleShip.data.resourceType = stageSevenData.routeResources[routeInfoAtCurrentStar.Value.routeId];

      UpdateShipCargo(idleShip.data);
      GotoNextStarInRoute(idleShip, routeInfoAtCurrentStar.Value.destStarId);
      return;
    }
    // Otherwise, find nearest star & circuit with deficiency & go there
    var nearestSettlements = GetNearestStarSettlements(currentStarId);
    foreach(var otherSettlement in nearestSettlements){

      var routeInfoAtDistantStar = FindCircuitForShipAtStar(idleShip, otherSettlement);
      if(routeInfoAtDistantStar != null){
        idleShip.data.routeId = routeInfoAtDistantStar.Value.routeId;
        idleShip.data.resourceType = stageSevenData.routeResources[routeInfoAtDistantStar.Value.routeId];

        //head directly to star in need
        idleShip.data.sourceStarId = idleShip.data.destStarId;
        idleShip.data.sourceCelestialBodyId = idleShip.data.destCelestialBodyId;

        idleShip.data.destStarId = otherSettlement;
        idleShip.data.destCelestialBodyId = stageSevenData.settlements.First(x => x.Value.parentStarId == idleShip.data.destStarId).Key;

        idleShip.Init();
        idleShip.StartMoving();
        return;
      }
    }
  }

  //Returns if it was able to assign the ship to a circuit at the star
  CircuitForShipInfo? FindCircuitForShipAtStar(GalaxyShip idleShip, uint currentStarId){

    var starCircuits = routeCache.GetValidStarCircuits(currentStarId);
    foreach(var circuit in starCircuits){
      var routeId = circuit.routeId;
      var shipsOfSameTypeAlreadyOnCircuit = stageSevenData.ships.Count(s =>
        s.type == GalaxyShipType.Transport &&
        s.shipTypeId == idleShip.data.shipTypeId &&
        s.routeId == routeId &&
        circuit.starIds.Contains(s.destStarId) &&
        s.phase != ShipTravelPhase.Idle
      );

      var needed = circuit.ShipsNeeded(idleShip.data.shipTypeId);

      if(needed > shipsOfSameTypeAlreadyOnCircuit){

        //Possible routes is all the different paths from this start to other stars on a particular route.
        //Will generally be 1-2, but it could be more for a hub system
        var possibleRoutes = stageSevenData.routeConnections[routeId][currentStarId].ToList();

        var starSettlementData = stageSevenData.starSettlements[currentStarId];

        var routeResource = stageSevenData.routeResources[routeId];
        var isExporting = starSettlementData.resources.ContainsKey(routeResource) && starSettlementData.resources[routeResource].exporting;

        //s10 If this star isn't exporting, prioritize the routes by trying to go to the closest exporter first
        if(!isExporting && possibleRoutes.Count > 1){
          possibleRoutes = possibleRoutes.OrderBy(destStarId => GetDistToNextExporter(currentStarId, destStarId, routeId)).ToList();
        }

        for(var i = 0; i < possibleRoutes.Count; i++){
          var destStarId = possibleRoutes[i];

          var existingShipOnSamePath = stageSevenData.ships.FirstOrDefault(s =>
            s.type == GalaxyShipType.Transport
            && s.shipTypeId == idleShip.data.shipTypeId
            && s.routeId == routeId
            && s.sourceStarId == currentStarId
            && s.destStarId == destStarId
          );
          //r6
          if(existingShipOnSamePath != null){ continue; }

          return new CircuitForShipInfo(){
            routeId = routeId,
            destStarId = destStarId
          };

        }
      }
    }
    return null;
  }

  struct CircuitForShipInfo {
    public uint routeId;
    public uint destStarId;
  }

  //Updates all the circuits assigning how many of each transport type should be on the circuit
  //Uses round robin assigning the highest capacity transports to the longest routes first
  void UpdateCircuitShipDistribution(){
    var sortedTransports = stageSevenData.ships
      .Where(s => s.type == GalaxyShipType.Transport)
      .OrderByDescending(s => s.maxResources)
      .ToList();

    var sortedCircuits = routeCache.allRouteCircuits.Values
      .SelectMany(x => x)
      .Where(c => c.IsValid)
      .OrderByDescending(c => c.circuitLength)
      .ToList();

    if(sortedCircuits.Count == 0){
      return; //EZ-PZ
    }

    //clear any previous allocations
    foreach(var circuit in sortedCircuits){
      if(circuit.transportAllocation != null){
        circuit.transportAllocation.Clear();
      }
    }

    var transportIndex = 0;
    var circuitIndex = 0;

    while(transportIndex < sortedTransports.Count){
      var circuit = sortedCircuits[circuitIndex];
      var transport = sortedTransports[transportIndex];

      if(circuit.transportAllocation == null){
        circuit.transportAllocation = new Dictionary<GalaxyBuildingId, ushort>();
      }

      if(!circuit.transportAllocation.ContainsKey(transport.shipTypeId)){
        circuit.transportAllocation[transport.shipTypeId] = 0;
      }

      circuit.transportAllocation[transport.shipTypeId]++;

      transportIndex++;
      circuitIndex++;

      //loop back over the circuits
      if(circuitIndex == sortedCircuits.Count){ circuitIndex = 0; }
    }
  }

  //Handle continuing the transports routes after they have finished a route
  void OnShipDoneSignal(GalaxyShip ship) {
    if (ship.data.type != GalaxyShipType.Transport) {
      return;
    }

    var validRoute = true;
    //s7 check to see if we need to rebalance to a different route
    var currentCircuit = routeCache.GetStarCircuit(ship.data.destStarId, ship.data.routeId);
    if(currentCircuit != null){
      var numberOnCurrentRoute = GetTransportShipOnCircuitCount(ship.data.destStarId, ship.data.routeId, ship.data.shipTypeId, true);
      var numberAllocated = currentCircuit.ShipsNeeded(ship.data.shipTypeId);

      if( numberOnCurrentRoute > numberAllocated){
        validRoute = false;
      }

      if(!currentCircuit.IsValid){
        validRoute = false;
      }
    }

    // var validCircuit = CircuitIsValid(ship.data.routeId, ship.data.destStarId);
    var starStillHasRoute = stageSevenData.starConnections.ContainsKey(ship.data.destStarId)
      && stageSevenData.starConnections[ship.data.destStarId].Contains(ship.data.routeId);

    //with route gone or invalid as in r5 & s3 we need to dump cargo and try to find another route
    if (!validRoute || !starStillHasRoute ){

      ship.data.phase = ShipTravelPhase.Idle;
      ship.data.routeId = 0;
      DumpCargo(ship.data, stageSevenData.starSettlements[ship.data.destStarId]);

      FindCircuitForShip(ship);
      return;
    }


    //happy path where route still exists & is valid
    UpdateShipCargo(ship.data);
    GotoNextStarInRoute(ship);
  }

  void GotoNextStarInRoute(GalaxyShip ship, uint nextStarOverride = 0){
    var shipData = ship.data;
    var currentStarId = ship.data.destStarId;
    var previousStarId = ship.data.sourceStarId;

    var newDest = nextStarOverride;
    if(nextStarOverride == 0){
      newDest = routeCache.GetNextStarInRoute(shipData.routeId, currentStarId, previousStarId);
    }

    if(newDest == 0){
      return; //Don't think this should happen...
    }

    //Update ship to have it go on its merry way
    shipData.sourceStarId = shipData.destStarId;
    shipData.sourceCelestialBodyId = shipData.destCelestialBodyId;

    shipData.destStarId = newDest;
    shipData.destCelestialBodyId = stageSevenData.settlements.First(x => x.Value.parentStarId == shipData.destStarId).Key;

    ship.Init();
    ship.StartMoving();
  }

  //If importBeforeExport is false that means we should check how many exporters before an importer
  uint GetNumberOfStarsBeforeImportExportOnRoute(uint routeId, uint currentStarId, uint previousStarId, bool importBeforeExport){
    uint count = 0;
    var visitCount = 0;
    var resourceType = stageSevenData.routeResources[routeId];
    var circuit = routeCache.GetStarCircuit(currentStarId, routeId);

    //Error handling of weird data states while fixing things, shouldn't normally happen
    if(circuit == null || circuit.starIds.Count <= 1){
      return count;
    }

    do{
      visitCount++;
      var starResources = stageSevenData.starSettlements[currentStarId].resources;
      if(starResources.ContainsKey(resourceType)){
        var importCheck = starResources[resourceType].importing;
        var exportCheck = starResources[resourceType].exporting && starResources[resourceType].amount > 0;
        if(
          (importBeforeExport && importCheck) || (!importBeforeExport && exportCheck)
        ){
          count++;
        }
        else if(
          (importBeforeExport && exportCheck) || (!importBeforeExport && importCheck)
        ){
          break;
        }
      }

      var inspectedStarId = currentStarId;
      currentStarId = routeCache.GetNextStarInRoute(routeId, currentStarId, previousStarId);
      previousStarId = inspectedStarId;
    }while(visitCount < circuit.starIds.Count); //should never hit this, but just as a failsafe break out

    return count;
  }

  void UpdateShipCargo(GalaxyShipData shipData){
    var starSettlementData = stageSevenData.starSettlements[shipData.destStarId];

    if(!starSettlementData.resources.ContainsKey(shipData.resourceType)){
      return;
    }

    //s6 check to see if the route assigned resource is still the one the ship is assigned to, if not: dump & switch
    if(!stageSevenData.routeResources.ContainsKey(shipData.routeId) || stageSevenData.routeResources[shipData.routeId] != shipData.resourceType){
      DumpCargo(shipData, starSettlementData);
      if(stageSevenData.routeResources.ContainsKey(shipData.routeId)){
        shipData.resourceType = stageSevenData.routeResources[shipData.routeId];
      }else{
        shipData.resourceType = GameResourceType.None;
      }
    }

    if(!starSettlementData.resources.ContainsKey(shipData.resourceType)){
      return;
    }

    var shipAvailableCapacity = shipData.maxResources - shipData.resources;

    //picov
    if(starSettlementData.resources[shipData.resourceType].exporting && shipAvailableCapacity > 0){
      var starsExportingBeforeImportingOnCircuit = GetNumberOfStarsBeforeImportExportOnRoute(shipData.routeId, starSettlementData.starId, shipData.sourceStarId, false);

      //Avoid divide by 0 error when ignoring exporters that don't have any resources
      starsExportingBeforeImportingOnCircuit = starsExportingBeforeImportingOnCircuit <= 0 ? 1 : starsExportingBeforeImportingOnCircuit;

      //rule r7
      int fractionalAmt = Mathf.RoundToInt((float)shipAvailableCapacity / (float)starsExportingBeforeImportingOnCircuit );
      int transferAmount = Math.Min(fractionalAmt, starSettlementData.resources[shipData.resourceType].amount);

      starSettlementData.resources[shipData.resourceType].amount -= transferAmount;
      shipData.resources += transferAmount;
    }

    //dropov
    if(starSettlementData.resources[shipData.resourceType].importing){
      var starsImportingBeforeExportingOnCircuit = GetNumberOfStarsBeforeImportExportOnRoute(shipData.routeId, starSettlementData.starId, shipData.sourceStarId, true);

      //Avoid divide by 0 error when ignoring exporters that don't have any resources
      starsImportingBeforeExportingOnCircuit = starsImportingBeforeExportingOnCircuit <= 0 ? 1 : starsImportingBeforeExportingOnCircuit;

      //rule r7
      int fractionalAmt = Mathf.RoundToInt((float)shipData.resources / (float)starsImportingBeforeExportingOnCircuit );
      int transferAmount = Math.Min(fractionalAmt, shipData.resources);

      starSettlementData.resources[shipData.resourceType].amount += transferAmount;
      shipData.resources -= transferAmount;
    }
  }

  void DumpCargo(GalaxyShipData shipData, StarSettlementData starSettlementData){
    if(!starSettlementData.resources.ContainsKey(shipData.resourceType)){
      starSettlementData.resources[shipData.resourceType] = new GalaxyResource(){ type = shipData.resourceType, amount = 0 };
    }
    starSettlementData.resources[shipData.resourceType].amount += shipData.resources;
    shipData.resources = 0;
  }

  //s5, can only happen while game is running so shouldn't need to queue
  void OnBuildingFinished(GalaxyBuildingData buildingInfo, uint starId, uint? celestialId){
    if(GalaxyBuilding.allMarkets.Contains(buildingInfo.buildingId) || GalaxyBuilding.allTransports.Contains(buildingInfo.buildingId)){
      //Janky, but either use the current building to get the ship params (for transports) or get the corresponding transport building (for markets)
      var buildingTemplate = GalaxyBuilding.allBuildings[buildingInfo.buildingId].startingTransportId != GalaxyBuildingId.None ?
        GalaxyBuilding.allBuildings[GalaxyBuilding.allBuildings[buildingInfo.buildingId].startingTransportId] :
        GalaxyBuilding.allBuildings[buildingInfo.buildingId];

      var firstSourceCbSettlement = stageSevenData.settlements
        .First(x => x.Value.parentStarId == starId);

      // Set source == dest for now so it will start idle until it's assigned a route
      var newShip = new GalaxyShipData(){
        type = GalaxyShipType.Transport,
        shipTypeId = buildingTemplate.id,
        sourceStarId = starId,
        sourceCelestialBodyId = celestialId ?? firstSourceCbSettlement.Key,
        destStarId = starId,
        destCelestialBodyId = celestialId ?? firstSourceCbSettlement.Key,
        owningStarId = starId,
      };
      newShip.SetParamsBasedOnShipType();

      createShipSignal.Dispatch(newShip, true);

      UpdateTransportShips();
    }
  }

  int GetTransportShipCount(){
    return stageSevenData.ships.Count(s => s.type == GalaxyShipType.Transport);
  }

  int GetTransportShipCount(uint starId, uint routeId){
    return stageSevenData.ships.Count(s => s.type == GalaxyShipType.Transport && s.owningStarId == starId && s.routeId == routeId);
  }
  int GetTransportShipOnRouteCount(uint routeId, bool includeIdle = false){
    return stageSevenData.ships.Count(s =>
      s.type == GalaxyShipType.Transport &&
      s.routeId == routeId &&
      (includeIdle || s.phase != ShipTravelPhase.Idle)
    );
  }

  int GetTransportShipOnCircuitCount(uint starId, uint routeId, GalaxyBuildingId shipType, bool includeIdle = false){
    var circuit = routeCache.GetStarCircuit(starId, routeId);

    return stageSevenData.ships.Count(s =>
      s.type == GalaxyShipType.Transport &&
      s.shipTypeId == shipType &&
      s.routeId == routeId &&
      circuit.starIds.Contains(s.destStarId) &&
      (includeIdle || s.phase != ShipTravelPhase.Idle)
    );
  }

  int GetTransportShipIdleAtStarCount(uint starId){
    return stageSevenData.ships.Count(s =>
      s.type == GalaxyShipType.Transport &&
      s.phase == ShipTravelPhase.Idle &&
      (s.sourceStarId == starId || s.destStarId == starId)
    );
  }

  GalaxyShip GetClosestIdleShip(uint starId){
    var closestIndex = -1;
    var closestDist = float.MaxValue;
    var starPosition = galaxy.stars[starId].generatedData.position;

    for(var i = 0; i < galaxy.ships.Count; i++){
      var ship = galaxy.ships[i];
      if(ship.data.type != GalaxyShipType.Transport || ship.data.phase != ShipTravelPhase.Idle){
        continue;
      }

      if(ship.data.sourceStarId == starId || ship.data.destStarId == starId){
        closestIndex = i;
        break;
      }

      var distance = Vector2.Distance(galaxy.stars[ship.data.destStarId].generatedData.position, starPosition);
      if(distance < closestDist){
        closestDist = distance;
        closestIndex = i;
      }
    }

    if(closestIndex >= 0){
      return galaxy.ships[closestIndex];
    }
    return null;

  }

  //Going from source to dest star id's on route initially (and then continuing on the route), how long of a distance is it to an exporter for the route resource
  //Assumes the source starId isn't an exporter
  float GetDistToNextExporter(uint sourceStarId, uint destStarId, uint routeId){
    float totalDistance = 0f;

    var previousStarId = sourceStarId;
    var currentStarId = destStarId;

    var traverseLimit = 100;
    var iterations = 0;

    while(++iterations < traverseLimit){
      totalDistance += Vector2.Distance(galaxy.stars[previousStarId].transform.position, galaxy.stars[currentStarId].transform.position);

      var starSettlementData = stageSevenData.starSettlements[currentStarId];
      var routeResource = stageSevenData.routeResources[routeId];
      var isExporting = starSettlementData.resources.ContainsKey(routeResource) && starSettlementData.resources[routeResource].exporting;

      if(isExporting){
        break;
      }

      var nextStarId = routeCache.GetNextStarInRoute(routeId, currentStarId, previousStarId);
      if(nextStarId == 0 || nextStarId == currentStarId){
        //something went wrong traversing the route
        break;
      }
      previousStarId = currentStarId;
      currentStarId = nextStarId;
    }

    if(iterations == traverseLimit){
      Debug.LogWarning($"Unable to find true distance from {sourceStarId} to {destStarId} on route {routeId}");
    }

    return totalDistance;
  }

  //s8
  void OnRouteResoureAssigned(uint routeId){
    QueueUpdateTransportShips();
  }

  //s9
  void OnStarImportExportChanged(uint starId){
    QueueUpdateTransportShips();
  }

  IEnumerable<uint> GetNearestStarSettlements(uint starId){
    var starPosition = galaxy.stars[starId].generatedData.position;

    return stageSevenData.starSettlements
      .Where(ss => ss.Key != starId)
      .OrderBy(ss => Vector2.Distance(galaxy.stars[ss.Key].generatedData.position, starPosition) )
      .Select(ss => ss.Key);
  }
}