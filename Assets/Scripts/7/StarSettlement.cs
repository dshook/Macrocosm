using UnityEngine;
using System.Linq;
using System.Collections.Generic;

[System.Serializable]
public class StarFactoryData {
  public GameResourceType? output;

  //Assuming you have the full input resource amounts
  public int? GetNewYearsAmountProduced(StarSettlementData settlementData, int inputAmount){
    if(output == null){ return null; }

    var resourceInputDependencies = GalaxyResource.resourceDependencies[output.Value];
    var totalResourceBonus = 1f + settlementData.factoryEfficiencyBonus;

    //Don't give the efficiency bonus for creating the efficiency resources like FeSi
    if(!GalaxyResource.EfficiencyResourceTypes.Contains(output.Value)){

      //Should only be 2 dependents but for completion doing it in a loop
      foreach(var resourceDependent in resourceInputDependencies.dependents){
        totalResourceBonus += GetTier1ResourceBonus(resourceDependent, settlementData);
      }
    }

    return Mathf.RoundToInt(inputAmount * resourceInputDependencies.outputRatio * totalResourceBonus);
  }

  //For a resource type, say Iron, what's our pct bonus for combining which is based off IronSilicon
  public float GetTier1ResourceBonus(GameResourceType resourceType, StarSettlementData settlementData){
    var relatedSiliconResourceType = GalaxyResource.resourceDependencies.FirstOrDefault(kv =>
      kv.Value.dependents.Contains(resourceType) &&
      kv.Value.dependents.Contains(GameResourceType.Silicon)
    );
    var settlementRelatedSiliconResource = settlementData.resources.TryGet(relatedSiliconResourceType.Key);
    if(settlementRelatedSiliconResource == null){
      return 0;
    }
    return GalaxyResource.GetResourceEfficiencyBonus(settlementRelatedSiliconResource.amount);
  }
}

//Data that actually get saved
[System.Serializable]
public class StarSettlementData {
  public uint starId;
  public float foundedAt;

  public float factoryEfficiencyBonus = 0f;

  public Dictionary<GameResourceType, GalaxyResource> resources = new Dictionary<GameResourceType, GalaxyResource>(GameResource.gameResourceTypeComparer);
  public Dictionary<GalaxyBuildingId, int> buildings = new Dictionary<GalaxyBuildingId, int>(GalaxyBuilding.buildingComparer);
  public List<GalaxyBuildingData> buildQueue = new List<GalaxyBuildingData>();
  public List<StarFactoryData> factoryData = new List<StarFactoryData>();

  public bool HasBuilding(GalaxyBuildingId buildingId){
    if(buildings != null && buildings.TryGetValue(buildingId, out int buildingCount)){
      return buildingCount > 0;
    }
    return false;
  }

  public ushort GetMaxMarketLevel(){
    int maxMarket = 0;
    for(int m = 0; m < GalaxyBuilding.allMarkets.Length; m++){
      if(HasBuilding(GalaxyBuilding.allMarkets[m])){
        maxMarket = m;
      }
    }

    return (ushort)maxMarket;
  }

  public ushort GetMaxVictoryBuildingLevel(){
    ushort maxTier = 0;
    for(int m = 0; m < GalaxyBuilding.allVictory.Length; m++){
      if(HasBuilding(GalaxyBuilding.allVictory[m])){
        maxTier = GalaxyBuilding.allBuildings[GalaxyBuilding.allVictory[m]].tier;
      }
    }

    return maxTier;
  }

  public GalaxyBuilding GetMarket(){
    var marketLevel = GetMaxMarketLevel();
    return GalaxyBuilding.allBuildings[GalaxyBuilding.allMarkets[marketLevel]];
  }

  public GalaxyBuilding GetFactory(){
    GalaxyBuildingId maxFactory = GalaxyBuildingId.None;
    foreach(var factoryId in GalaxyBuilding.allFactories){
      if(HasBuilding(factoryId)){
        maxFactory = factoryId;
      }
    }

    if(maxFactory == GalaxyBuildingId.None){
      return null;
    }

    return GalaxyBuilding.allBuildings[maxFactory];
  }


  //Assumes the resource deltas have been updated prior
  public void UpdateNewYearState(StageRulesService stageRules){
    var factory = GetFactory();
    if(factory != null){
      foreach(var factoryRowData in factoryData){
        if(factoryRowData.output == null){ continue; }

        var dependency = GalaxyResource.resourceDependencies[factoryRowData.output.Value];
        var inputAmount = factory.factoryInputAmount;

        //check dependent resources
        if(dependency.dependents.Any(d => !resources.ContainsKey(d) || resources[d].amount < inputAmount )){
          continue;
        }

        //deduct dependents,
        foreach(var dep in dependency.dependents){
          resources[dep].amount -= inputAmount;
        }

        var outputAmount = factoryRowData.GetNewYearsAmountProduced(this, inputAmount);

        if(!outputAmount.HasValue){
          continue;
        }

        //add output
        if(!resources.ContainsKey(factoryRowData.output.Value)){
          resources[factoryRowData.output.Value] = new GalaxyResource{ type = factoryRowData.output.Value, amount = 0};
        }
        resources[factoryRowData.output.Value].amount += outputAmount.Value;

      }
    }
  }
}