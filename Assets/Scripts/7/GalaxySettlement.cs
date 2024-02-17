using System.Collections.Generic;
using UnityEngine;

//Celestial body settlement data.  Ambiguously named but don't want to break save
[System.Serializable]
public class GalaxySettlementData {
  public uint parentCelestialId;
  public uint parentStarId;

  public Dictionary<GameResourceType, GalaxyResource> resources;
  public List<GalaxyBuildingData> buildQueue = new List<GalaxyBuildingData>();
  public Dictionary<GalaxyBuildingId, int> buildings = new Dictionary<GalaxyBuildingId, int>(GalaxyBuilding.buildingComparer);

  [System.NonSerialized]
  public Dictionary<GameResourceType, GameResource> resourceDeltas = new Dictionary<GameResourceType, GameResource>(GameResource.gameResourceTypeComparer);

  public bool HasBuilding(GalaxyBuildingId buildingId){
    if(buildings != null && buildings.TryGetValue(buildingId, out int buildingCount)){
      return buildingCount > 0;
    }
    return false;
  }

  public GalaxyBuilding GetMinerBuilding(){
    GalaxyBuildingId maxLevel = GalaxyBuildingId.Miner0;
    for(int m = 0; m < GalaxyBuilding.allMines.Length; m++){
      var level = GalaxyBuilding.allMines[m];
      if(HasBuilding(level)){
        maxLevel = level;
      }
    }

    return GalaxyBuilding.allBuildings[maxLevel];
  }

  public GalaxyBuilding GetSettlementBuilding(){
    GalaxyBuildingId maxLevel = GalaxyBuildingId.Settlement1;
    for(int m = 0; m < GalaxyBuilding.allSettlements.Length; m++){
      var level = GalaxyBuilding.allSettlements[m];
      if(HasBuilding(level)){
        maxLevel = level;
      }
    }

    return GalaxyBuilding.allBuildings[maxLevel];
  }

  //Assumes the resource deltas have been updated prior
  public void UpdateNewYearState(StageRulesService stageRules, CelestialBodyData cb, StarSettlementData parentStarSettlement){
    foreach(var kvPair in resourceDeltas){
      var resourceType = kvPair.Key;
      var resourceChange = kvPair.Value;


      resources[resourceType].amount += resourceChange.amount;
      if(resources[resourceType].totalAmount.HasValue){
        //Reduce the total amount by how much we mined
        resources[resourceType].totalAmount -= resourceChange.amount;

      }else{
        Debug.LogWarning("Cb resource doesn't have total value: " + resourceType);
      }
    }


    foreach(var resourceKV in resources){
      var resourceDelta = resourceDeltas.TryGet(resourceKV.Key);
      //Export the resource to the parent star if conditions are met
      if(resourceDelta != null && resourceDelta.amount > 0 && GalaxyResource.canExportResource(resourceKV.Key) ){
        var resourceDest = parentStarSettlement.resources;

        if(!resourceDest.ContainsKey(resourceKV.Key)){
          resourceDest[resourceKV.Key] = new GalaxyResource(){
            type = resourceKV.Key,
            amount = 0
          };
        }

        var amountMined = Mathf.Min(resourceDelta.amount, resourceKV.Value.amount);

        resourceDest[resourceKV.Key].amount += amountMined;
        resourceKV.Value.amount -= amountMined;
      }
    }
  }

  //Calculate how much each resource will change after each year
  public void UpdateResourceDeltas(StageRulesService stageRules, CelestialBodyData cb){
    if(resourceDeltas == null){
      resourceDeltas = new Dictionary<GameResourceType, GameResource>();
    }
    foreach(var kvPair in resourceDeltas){
      resourceDeltas[kvPair.Key].amount = 0;
    }

    var totalUnitsMinedPerYear = GetMinerBuilding().totalMiningAmountPerYear;

    //All resource deposits
    if(resources != null && resources.Count > 0){

      // int totalMiningPriority = resources.Sum(r =>
      //   r.Value.totalAmount.HasValue ?
      //   GalaxyResource.miningPriority[GalaxyResource.GetAbundance(r.Value.totalAmount.Value)]
      //   : 0
      // );

      foreach(var resourceDeposit in resources){
        var resourceType = resourceDeposit.Key;
        var settlementResource = resourceDeposit.Value;

        if(!settlementResource.totalAmount.HasValue){
          Debug.LogWarning("No total amount for resource " + resourceType);
          continue;
        }

        var abundance = GalaxyResource.GetAbundance(settlementResource.totalAmount.Value);
        // var miningPriority = (float)GalaxyResource.miningPriority[abundance] / totalMiningPriority;
        int resourceAmt = Mathf.RoundToInt(
          (float)totalUnitsMinedPerYear * TotalEfficiency(cb)
        );

        //Make sure it's at least 1 for starting out
        resourceAmt = Mathf.Max(resourceAmt, 1);

        //cap at total amount left
        resourceAmt = Mathf.Min(resourceAmt, settlementResource.totalAmount.Value);

        GameResource existing;
        if(!resourceDeltas.TryGetValue(resourceType, out existing)){
          resourceDeltas[resourceType] = new GameResource{ type = resourceType, amount = resourceAmt};
        }else{
          existing.amount = resourceAmt;
        }

      }
    }
  }

  public float TotalEfficiency(CelestialBodyData cb){
    return cb.GravityEfficiency * GetSettlementBuilding().settlementEfficiency;
  }

}

public class GalaxySettlementCreationData {
  public uint sourceStarId;
  public uint sourceCelestialId;

  public uint destCelestialId;
  public uint destStarId;
  public GalaxyBuildingId colonyBuilding;
}