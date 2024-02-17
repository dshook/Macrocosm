using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GalaxyRouteCache {

  [Inject] StageSevenDataModel stageSevenData {get; set;}

  public Galaxy galaxy;

  //From route id to all circuits for the route
  public Dictionary<uint, List<GalaxyCircuit>> allRouteCircuits = new Dictionary<uint, List<GalaxyCircuit>>();

  //Finds the circuit (set of all connected stars) for this star and routes, and populates the cache if it needs to compute
  public void BuildStarCircuits(){
    allRouteCircuits.Clear();


    foreach(var routeId in stageSevenData.routeConnections.Keys){
      BuildCircuitForRoute(routeId);
    }
  }

  public void BuildCircuitForRoute(uint routeId){
    // Debug.Log($"Building circuits for route {routeId}");

    allRouteCircuits.Remove(routeId);

    HashSet<uint> visitedStarIds = new HashSet<uint>();

    visitedStarIds.Clear();
    var starsOnRoute = stageSevenData.routeConnections[routeId];

    foreach(var starConnection in starsOnRoute){
      var starIdOnRoute = starConnection.Key;

      if(starConnection.Value.Count == 0){
        continue;
      }

      //If we've already seen this star on this route then it must be part of an existing circuit already so we can continue
      if(visitedStarIds.Contains(starIdOnRoute)){
        continue;
      }else{
        //Otherwise it's part of a new circuit we need to traverse
        var newCircuit = new GalaxyCircuit(){
          routeId = routeId,
          starIds = new HashSet<uint>(),
        };

        var startingStarId = starIdOnRoute;
        var currentStarId = startingStarId;
        uint previousStarId = 0;

        ushort validityScore =  0;
        if(stageSevenData.routeResources.ContainsKey(routeId)){ validityScore += 1; }
        bool hasImporter = false;
        bool hasExporter = false;
        GameResourceType? routeResourceType = stageSevenData.routeResources.TryGet(routeId);

        var allConnectedStarsWeNeedToHit = new HashSet<uint>(){ currentStarId };
        var loopCount = 0;
        var killCount = 100;

        while(true){
          loopCount++;
          newCircuit.starIds.Add(currentStarId);
          visitedStarIds.Add(currentStarId);

          allConnectedStarsWeNeedToHit.UnionWith(stageSevenData.routeConnections[routeId][currentStarId]);

          //Update circuit validity tracking
          if(routeResourceType != null){
            var starResources = stageSevenData.starSettlements[currentStarId].resources;
            if(starResources.ContainsKey(routeResourceType.Value)){
              if(starResources[routeResourceType.Value].importing){
                hasImporter = true;
              }
              if(starResources[routeResourceType.Value].exporting){
                hasExporter = true;
              }
            }
          }

          var nextStarId = GetNextStarInRoute(routeId, currentStarId, previousStarId);
          previousStarId = currentStarId;
          currentStarId = nextStarId;

          newCircuit.circuitLength += Vector2.Distance(galaxy.stars[previousStarId].transform.position, galaxy.stars[currentStarId].transform.position);

          if(loopCount >= killCount){
            break;
          }

          if(
            currentStarId == startingStarId &&
            allConnectedStarsWeNeedToHit.Count == newCircuit.starIds.Count
          ){
            break;
          }
        }

        if(loopCount == killCount){
          Debug.LogWarning("Hit loop kill count for circuit traversal");
        }

        //Finish updating circuit validity
        if(hasImporter){ validityScore += 1; }
        if(hasExporter){ validityScore += 1; }
        newCircuit.validityScore = validityScore;

        //circuit built now
        if(!allRouteCircuits.ContainsKey(routeId)){
          allRouteCircuits[routeId] = new List<GalaxyCircuit>();
        }
        allRouteCircuits[routeId].Add(newCircuit);
      }
    }

  }


  //Finds the circuit (set of all connected stars) for this star and routes, and populates the cache if it needs to compute
  public GalaxyCircuit GetStarCircuit(uint starId, uint routeId){
    if(!allRouteCircuits.ContainsKey(routeId)){
      return null;
    }

    return allRouteCircuits[routeId].FirstOrDefault(rc => rc.starIds.Contains(starId));
  }

  public void ClearAll(){
    allRouteCircuits.Clear();
    createdRouteLines.Clear();
  }


  public Dictionary<uint, List<GalaxyRouteLine>> createdRouteLines = new Dictionary<uint, List<GalaxyRouteLine>>();

  //Returns true if the route line matches the origin & dest in either direction
  public Func<GalaxyRouteLine, uint, uint, bool> findRouteLine = (cl, originId, destId) => {
    return (cl.origin != null && cl.dest != null) &&
      (
        (cl.origin.generatedData.id == originId && cl.dest.generatedData.id == destId) ||
        (cl.origin.generatedData.id == destId   && cl.dest.generatedData.id == originId)
      );
  };

  public void UpdateRouteCircuitsValidity(uint routeId){
    // Debug.Log($"Updating route {routeId} circuit validity");

    var routeCircuits = allRouteCircuits.TryGet(routeId);

    if(routeCircuits == null){ return; }

    foreach(var circuit in routeCircuits){
      UpdateCircuitValidity(circuit);
    }
  }

  public void UpdateStarCircuitsValidity(uint starId){
    // Debug.Log($"Updating star {starId} circuit validity");

    foreach(var routeCircuits in allRouteCircuits){
      if(routeCircuits.Value == null){ continue; }

      foreach(var circuit in routeCircuits.Value){
        if(circuit.starIds.Contains(starId)){
          UpdateCircuitValidity(circuit);
        }
      }
    }
  }

  //Updates & checks if a circuit is valid and returns a score based on the conditions
  //0 - no resource, 1 - resource but no import/export, 2 - import or export, 3 - fully valid
  void UpdateCircuitValidity(GalaxyCircuit circuit){
    ushort score = 0;
    bool hasImporter = false;
    bool hasExporter = false;

    if(!stageSevenData.routeResources.ContainsKey(circuit.routeId)){
      circuit.validityScore = score;
      return;
    }
    score += 1;

    var resourceType = stageSevenData.routeResources[circuit.routeId];
    foreach(var circuitStarId in circuit.starIds){

      var starResources = stageSevenData.starSettlements[circuitStarId].resources;
      if(starResources.ContainsKey(resourceType)){
        if(starResources[resourceType].importing){
          hasImporter = true;
        }
        if(starResources[resourceType].exporting){
          hasExporter = true;
        }
      }

      if(hasImporter && hasExporter){
        break;
      }
    }

    if(hasImporter){
      score += 1;
    }
    if(hasExporter){
      score += 1;
    }

    circuit.validityScore = score;
  }

  public uint GetNextStarInRoute(uint routeId, uint currentStarId, uint previousStarId){
    //Error handling for weird data states while debugging
    if(!stageSevenData.routeConnections.ContainsKey(routeId)){
      Debug.LogWarning("Missing route connection for routeId: " + routeId);
      return currentStarId;
    }
    if(!stageSevenData.routeConnections[routeId].ContainsKey(currentStarId)){
      Debug.LogWarning("Missing route connection for routeId: " + routeId + " starId: " + currentStarId);
      return stageSevenData.routeConnections[routeId].First().Key;
    }

    //Find the destination route that makes the most sense, following it around to the right
    var destRoutes = stageSevenData.routeConnections[routeId][currentStarId];

    if(destRoutes.Count == 0){
      Debug.LogError($"No destination routes for route {routeId} starId {currentStarId}");
      return currentStarId;
    }

    //If we don't have any previous star to go off of just assume it was the first possible route
    if(previousStarId == 0 || previousStarId == currentStarId){
      previousStarId = destRoutes.First();
    }

    uint newDest = 0;
    if(destRoutes.Count == 1){
      //Go back where we came from if that's the only way
      newDest = destRoutes.First();
    }else if(destRoutes.Count == 2){
      newDest = destRoutes.FirstOrDefault(x => x != previousStarId);
    }else{
      //if there are more than two possible paths (including where we came from) find the one with the
      //least angle from where we came from to try to follow the route along the edges
      //TODO: need to handle going both directions, and _know_ which direction you're going
      var cameFromVec = galaxy.stars[previousStarId].transform.position - galaxy.stars[currentStarId].transform.position;
      var maxAngle = float.MaxValue;
      foreach(var destRoute in destRoutes.Where(x => x != previousStarId)){
        var goingToVec = galaxy.stars[destRoute].transform.position - galaxy.stars[currentStarId].transform.position;
        var angle = Vector2.SignedAngle(cameFromVec, goingToVec);
        if(angle < 0){ angle = 360f + angle; }

        if(angle < maxAngle){
          maxAngle = angle;
          newDest = destRoute;
        }
      }
    }
    if(newDest == 0){
      Debug.LogWarning("Unable to find route for transport ship");
    }
    return newDest;
  }

  public IEnumerable<GalaxyCircuit> GetValidStarCircuits(uint starId){
    return allRouteCircuits.Values
      .SelectMany(x => x)
      .Where(c => c.IsValid && c.starIds.Contains(starId));
  }
}

public class GalaxyCircuit {
  public uint routeId;
  public HashSet<uint> starIds;

  //How long in world space it takes to go around the circuit before you get back to the same star
  public float circuitLength;

  //How valid the circuit is for transporting
  //0 - no resource, 1 - resource but no import/export, 2 - import or export, 3 - fully valid
  public ushort validityScore;

  public Dictionary<GalaxyBuildingId, ushort> transportAllocation;

  public bool IsValid{
    get { return validityScore == 3; }
  }

  public ushort ShipsNeeded(GalaxyBuildingId transportType){
    if(transportAllocation != null){
      return transportAllocation.TryGet(transportType);
    }
    return 0;
  }
}