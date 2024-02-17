using System;
using System.Collections.Generic;
using System.Linq;

public class GalaxyBuilding {

  public string name = "";
  public string shortName = "";
  public string descrip = "";
  public ushort tier = 0;
  public bool repeatable = false;
  public uint? maxQuantity = null;
  public float buildTime = 0f;
  public float maxMoveSpeed = 0f; //for ships in c
  public float maxAcceleration = 0f; //for ships in G
  public int maxTransportResources = 0;

  //For factories
  public int factoryInputAmount = 0; //How many base resources to consume per year
  public int factoryRows = 0; //How many simultaneous combining rows

  //For miners
  public int totalMiningAmountPerYear = 0; //Across all resources mined, how much per year

  //For settlement
  public float settlementEfficiency = 0f;

  //For markets
  public GalaxyBuildingId startingTransportId = GalaxyBuildingId.None;

  //Victory building
  public float factoryEfficiencyBonus = 0f;

  public bool isSystemBuilding = false;
  public float? influenceRadiusLy; //for system buildings
  public bool isVictoryBuilding = false;

  private GameResource[] baseResourceCosts {get; set;}
  private GameResource[] adjustedResourceCosts;

  //Return the real resource costs adjusted for the stage rules
  //Try to only create new arrays once and return a cached result
  public GameResource[] resourceCosts(StageSevenRulesProps stageSevenRules){
    if(baseResourceCosts == null){ return null; }

    if(
      adjustedResourceCosts != null &&
      adjustedResourceCosts[0].amount == baseResourceCosts[0].amount * stageSevenRules.additionalResourceCostMultiplier
    ){
      return adjustedResourceCosts;
    }

    adjustedResourceCosts = baseResourceCosts.Select(a =>(GameResource)a.Clone()).ToArray();
    foreach(var cost in adjustedResourceCosts){
      cost.amount = cost.amount * stageSevenRules.additionalResourceCostMultiplier;
    }
    return adjustedResourceCosts;
  }

  public GalaxyBuildingId id = GalaxyBuildingId.None;

  public string[] iconPaths;

  public HexTechId requiredTech;
  public GalaxyBuildingId[] prereqBuildings; //must have ALL of the mods to get this mod, _this is different than city buildings!!_
  public GalaxyBuildingId[] excludeBuildings; //can't have any of the mods to get this mod

  public Action<StageSevenDataModel> onApply;

  public static Dictionary<GalaxyBuildingId, GalaxyBuilding> allBuildings = new Dictionary<GalaxyBuildingId, GalaxyBuilding>(buildingComparer){
    {
      GalaxyBuildingId.Factory1,
      new GalaxyBuilding(){
        id = GalaxyBuildingId.Factory1,
        name = "System Factory Level 1",
        descrip = "Combines Resources",
        isSystemBuilding = true,
        buildTime = 4f,
        factoryInputAmount = 5,
        factoryRows = 1,
        tier = 1,
        baseResourceCosts = new GameResource[]{
          new GameResource(){type = GameResourceType.Iron, amount = 100},
        },
        iconPaths = new string[]{ "Art/stage7/factory_1" },
      }
    },
    {
      GalaxyBuildingId.Factory2,
      new GalaxyBuilding(){
        id = GalaxyBuildingId.Factory2,
        name = "System Factory Level 2",
        descrip = "Combines 2 resources simultaneously. Processes 50 units/yr",
        isSystemBuilding = true,
        buildTime = 4f,
        factoryInputAmount = 50,
        factoryRows = 2,
        tier = 2,
        prereqBuildings = new GalaxyBuildingId[]{ GalaxyBuildingId.Factory1 },
        requiredTech = HexTechId.GalacticIndustry2,
        baseResourceCosts = new GameResource[]{
          new GameResource(){type = GameResourceType.Iron, amount = 1000},
          new GameResource(){type = GameResourceType.IronTitanium, amount = 200},
        },
        iconPaths = new string[]{ "Art/stage7/factory_2" },
      }
    },
    {
      GalaxyBuildingId.Factory3,
      new GalaxyBuilding(){
        id = GalaxyBuildingId.Factory3,
        name = "System Factory Level 3",
        descrip = "Combines 3 resources simultaneously. Processes 500 units/yr",
        isSystemBuilding = true,
        buildTime = 8f,
        factoryInputAmount = 500,
        factoryRows = 3,
        tier = 3,
        prereqBuildings = new GalaxyBuildingId[]{ GalaxyBuildingId.Factory2 },
        requiredTech = HexTechId.GalacticIndustry3,
        baseResourceCosts = new GameResource[]{
          new GameResource(){type = GameResourceType.IronTitanium, amount = 2000},
          new GameResource(){type = GameResourceType.IronPromethium, amount = 1000},
        },
        iconPaths = new string[]{ "Art/stage7/factory_3" },
      }
    },
    {
      GalaxyBuildingId.Factory4,
      new GalaxyBuilding(){
        id = GalaxyBuildingId.Factory4,
        name = "System Factory Level 4",
        descrip = "Combines 4 resources simultaneously. Processes 5,000 units/yr",
        isSystemBuilding = true,
        buildTime = 16f,
        factoryInputAmount = 5000,
        factoryRows = 4,
        tier = 4,
        prereqBuildings = new GalaxyBuildingId[]{ GalaxyBuildingId.Factory3 },
        requiredTech = HexTechId.GalacticIndustry4,
        baseResourceCosts = new GameResource[]{
          new GameResource(){type = GameResourceType.IronTitanium, amount = 20000},
          new GameResource(){type = GameResourceType.IronPromethium, amount = 10000},
        },
        iconPaths = new string[]{ "Art/stage7/factory_4" },
      }
    },
    {
      GalaxyBuildingId.Factory5,
      new GalaxyBuilding(){
        id = GalaxyBuildingId.Factory5,
        name = "System Factory Level 5",
        descrip = "Combines 5 resources simultaneously. Processes 25,000 units/yr",
        isSystemBuilding = true,
        buildTime = 32f,
        factoryInputAmount = 25000,
        factoryRows = 5,
        tier = 5,
        prereqBuildings = new GalaxyBuildingId[]{ GalaxyBuildingId.Factory4 },
        requiredTech = HexTechId.GalacticIndustry5,
        baseResourceCosts = new GameResource[]{
          new GameResource(){type = GameResourceType.IronTitanium, amount = 200000},
          new GameResource(){type = GameResourceType.TitaniumPromethium, amount = 10000},
        },
        iconPaths = new string[]{ "Art/stage7/factory_5" },
      }
    },

    {
      GalaxyBuildingId.Colony1,
      new GalaxyBuilding(){
        id = GalaxyBuildingId.Colony1,
        name = "Colony Level 1",
        descrip = "Can colonize Terrestrial worlds with Moderate or better habitability.<br>Max speed 0.2c Max Acceleration 0.1G",
        isSystemBuilding = true,
        repeatable = true,
        buildTime = 5f,
        maxMoveSpeed = 0.2f,
        maxAcceleration = 0.1f,
        tier = 1,
        baseResourceCosts = new GameResource[]{
          new GameResource(){type = GameResourceType.Iron, amount = 100},
          new GameResource(){type = GameResourceType.IronPhosporus, amount = 50},
        },
        iconPaths = new string[]{ "Art/stage7/ships/galaxy_colony_1" },
      }
    },
    {
      GalaxyBuildingId.Colony2,
      new GalaxyBuilding(){
        id = GalaxyBuildingId.Colony2,
        name = "Colony Level 2",
        descrip = "Can colinize Gas Giant or Terrestrial worlds with Poor or better habitability.<br>Max speed 0.4c Max Acceleration 0.3G",
        isSystemBuilding = true,
        repeatable = true,
        requiredTech = HexTechId.Colony2,
        buildTime = 10f,
        maxMoveSpeed = 0.3f,
        maxAcceleration = 0.2f,
        tier = 2,
        baseResourceCosts = new GameResource[]{
          new GameResource(){type = GameResourceType.IronPhosporus, amount = 5000},
          new GameResource(){type = GameResourceType.IronTitanium, amount = 500},
        },
        iconPaths = new string[]{ "Art/stage7/ships/galaxy_colony_2" },
      }
    },
    {
      GalaxyBuildingId.Colony3,
      new GalaxyBuilding(){
        id = GalaxyBuildingId.Colony3,
        name = "Colony Level 3",
        descrip = "Can colinize any worlds with Poor or better habitability.<br>Max speed 0.6c Max Acceleration 0.5G",
        isSystemBuilding = true,
        repeatable = true,
        requiredTech = HexTechId.Colony3,
        buildTime = 15f,
        maxMoveSpeed = 0.6f,
        maxAcceleration = 0.5f,
        tier = 3,
        baseResourceCosts = new GameResource[]{
          new GameResource(){type = GameResourceType.PhosphorusTitanium, amount = 5000},
          new GameResource(){type = GameResourceType.IronTitanium, amount = 5000},
          new GameResource(){type = GameResourceType.PhosphorusSodium, amount = 500},
        },
        iconPaths = new string[]{ "Art/stage7/ships/galaxy_colony_3" },
      }
    },
    {
      GalaxyBuildingId.Colony4,
      new GalaxyBuilding(){
        id = GalaxyBuildingId.Colony4,
        name = "Colony Level 4",
        descrip = "Can colinize any worlds with Terrible or better habitability.<br>Max speed 0.7c Max Acceleration 0.7G",
        isSystemBuilding = true,
        repeatable = true,
        requiredTech = HexTechId.Colony4,
        buildTime = 20f,
        maxMoveSpeed = 0.7f,
        maxAcceleration = 0.7f,
        tier = 4,
        baseResourceCosts = new GameResource[]{
          new GameResource(){type = GameResourceType.PhosphorusTitanium, amount = 20000},
          new GameResource(){type = GameResourceType.PhosphorusSodium, amount = 5000},
          new GameResource(){type = GameResourceType.TitaniumPromethium, amount = 1000},
        },
        iconPaths = new string[]{ "Art/stage7/ships/galaxy_colony_4" },
      }
    },
    {
      GalaxyBuildingId.Colony5,
      new GalaxyBuilding(){
        id = GalaxyBuildingId.Colony5,
        name = "Colony Level 5",
        descrip = "Can colinize any world and starts settlement at level 3.<br>Max speed 0.86c Max Acceleration 1.0G",
        isSystemBuilding = true,
        repeatable = true,
        requiredTech = HexTechId.Colony5,
        buildTime = 30f,
        maxMoveSpeed = 0.86f,
        maxAcceleration = 1.0f,
        tier = 5,
        baseResourceCosts = new GameResource[]{
          new GameResource(){type = GameResourceType.PhosphorusTitanium, amount = 100000},
          new GameResource(){type = GameResourceType.PhosphorusSodium, amount = 50000},
          new GameResource(){type = GameResourceType.TitaniumPromethium, amount = 10000},
        },
        iconPaths = new string[]{ "Art/stage7/ships/galaxy_colony_5" },
      }
    },

    {
      GalaxyBuildingId.Market0,
      new GalaxyBuilding(){
        id = GalaxyBuildingId.Market0,
        name = "Basic System Market",
        descrip = "Allows resource trading",
        isSystemBuilding = true,
        buildTime = 0f,
        influenceRadiusLy = 20,
        iconPaths = new string[]{ "Art/stage7/market_0" },
      }
    },
    {
      GalaxyBuildingId.Market1,
      new GalaxyBuilding(){
        id = GalaxyBuildingId.Market1,
        name = "System Market Level 1",
        descrip = "Allows resource trading and building 1 additional Level 1 Transports",
        isSystemBuilding = true,
        buildTime = 2f,
        // requiredTech = HexTechId.GalacticMarkets,
        influenceRadiusLy = 40,
        startingTransportId = GalaxyBuildingId.Transport1,
        tier = 1,
        baseResourceCosts = new GameResource[]{
          new GameResource(){type = GameResourceType.Iron, amount = 100},
          new GameResource(){type = GameResourceType.Titanium, amount = 100},
        },
        onApply = (stageSevenData) => {
          stageSevenData.buildingLimits.AddOrUpdate(GalaxyBuildingId.Transport1, 1u, (uint old) => old + 1u);
        },
        iconPaths = new string[]{ "Art/stage7/market_1" },
      }
    },
    {
      GalaxyBuildingId.Market2,
      new GalaxyBuilding(){
        id = GalaxyBuildingId.Market2,
        name = "System Market Level 2",
        descrip = "Increases route radius to 50ly and allows building 2 additional Level 2 Transports",
        isSystemBuilding = true,
        buildTime = 2f,
        requiredTech = HexTechId.GalacticMarkets2,
        prereqBuildings = new GalaxyBuildingId[]{ GalaxyBuildingId.Market1 },
        influenceRadiusLy = 50,
        startingTransportId = GalaxyBuildingId.Transport2,
        tier = 2,
        baseResourceCosts = new GameResource[]{
          new GameResource(){type = GameResourceType.IronTitanium, amount = 100},
          new GameResource(){type = GameResourceType.IronXenon, amount = 100},
        },
        onApply = (stageSevenData) => {
          stageSevenData.buildingLimits.AddOrUpdate(GalaxyBuildingId.Transport2, 2u, (uint old) => old + 2u);
        },
        iconPaths = new string[]{ "Art/stage7/market_2" },
      }
    },
    {
      GalaxyBuildingId.Market3,
      new GalaxyBuilding(){
        id = GalaxyBuildingId.Market3,
        name = "System Market Level 3",
        descrip = "Increases route radius to 60ly and allows building 3 additional Level 3 Transports",
        isSystemBuilding = true,
        buildTime = 2f,
        requiredTech = HexTechId.GalacticMarkets3,
        prereqBuildings = new GalaxyBuildingId[]{ GalaxyBuildingId.Market2 },
        influenceRadiusLy = 60,
        startingTransportId = GalaxyBuildingId.Transport3,
        tier = 3,
        baseResourceCosts = new GameResource[]{
          new GameResource(){type = GameResourceType.IronTitanium, amount = 1000},
          new GameResource(){type = GameResourceType.IronXenon, amount = 1000},
        },
        onApply = (stageSevenData) => {
          stageSevenData.buildingLimits.AddOrUpdate(GalaxyBuildingId.Transport3, 3u, (uint old) => old + 3u);
        },
        iconPaths = new string[]{ "Art/stage7/market_3" },
      }
    },
    {
      GalaxyBuildingId.Market4,
      new GalaxyBuilding(){
        id = GalaxyBuildingId.Market4,
        name = "System Market Level 4",
        descrip = "Increases route radius to 65ly and allows building 3 additional Level 4 Transports",
        isSystemBuilding = true,
        buildTime = 2f,
        requiredTech = HexTechId.GalacticMarkets4,
        prereqBuildings = new GalaxyBuildingId[]{ GalaxyBuildingId.Market3 },
        influenceRadiusLy = 65,
        startingTransportId = GalaxyBuildingId.Transport4,
        tier = 4,
        baseResourceCosts = new GameResource[]{
          new GameResource(){type = GameResourceType.IronTitanium, amount = 10000},
          new GameResource(){type = GameResourceType.TitaniumXenon, amount = 1000},
          new GameResource(){type = GameResourceType.IronPromethium, amount = 1000},
        },
        onApply = (stageSevenData) => {
          stageSevenData.buildingLimits.AddOrUpdate(GalaxyBuildingId.Transport4, 3u, (uint old) => old + 3u);
        },
        iconPaths = new string[]{ "Art/stage7/market_4" },
      }
    },
    {
      GalaxyBuildingId.Market5,
      new GalaxyBuilding(){
        id = GalaxyBuildingId.Market5,
        name = "System Market Level 5",
        descrip = "Increases route radius to 70ly and allows building 3 additional Level 5 Transports",
        isSystemBuilding = true,
        buildTime = 2f,
        requiredTech = HexTechId.GalacticMarkets5,
        prereqBuildings = new GalaxyBuildingId[]{ GalaxyBuildingId.Market4 },
        influenceRadiusLy = 70,
        startingTransportId = GalaxyBuildingId.Transport5,
        tier = 5,
        baseResourceCosts = new GameResource[]{
          new GameResource(){type = GameResourceType.TitaniumXenon, amount = 10000},
          new GameResource(){type = GameResourceType.TitaniumPromethium, amount = 10000},
          new GameResource(){type = GameResourceType.XenonPromethium, amount = 1000},
        },
        onApply = (stageSevenData) => {
          stageSevenData.buildingLimits.AddOrUpdate(GalaxyBuildingId.Transport5, 3u, (uint old) => old + 3u);
        },
        iconPaths = new string[]{ "Art/stage7/market_5" },
      }
    },

    {
      GalaxyBuildingId.Transport1,
      new GalaxyBuilding(){
        id = GalaxyBuildingId.Transport1,
        name = "Transport Level 1",
        descrip = "Transports 100 resources.<br>Max speed 0.6c Max Acceleration 0.7G ",
        isSystemBuilding = true,
        repeatable = true,
        buildTime = 2f,
        maxMoveSpeed = 0.6f,
        maxAcceleration = 0.7f,
        maxTransportResources = 100,
        tier = 1,
        prereqBuildings = new GalaxyBuildingId[]{ GalaxyBuildingId.Market1 },
        baseResourceCosts = new GameResource[]{
          new GameResource(){type = GameResourceType.IronPhosporus, amount = 100},
          new GameResource(){type = GameResourceType.IronXenon, amount = 100},
        },
        iconPaths = new string[]{ "Art/stage7/ships/galaxy_transport_1_ui" },
      }
    },
    {
      GalaxyBuildingId.Transport2,
      new GalaxyBuilding(){
        id = GalaxyBuildingId.Transport2,
        name = "Transport Level 2",
        descrip = "Transports 1,000 resources.<br>Max speed 0.7c Max Acceleration 1.2G ",
        isSystemBuilding = true,
        repeatable = true,
        buildTime = 2f,
        maxMoveSpeed = 0.7f,
        maxAcceleration = 1.2f,
        maxTransportResources = 1000,
        tier = 2,
        prereqBuildings = new GalaxyBuildingId[]{ GalaxyBuildingId.Market2 },
        baseResourceCosts = new GameResource[]{
          new GameResource(){type = GameResourceType.IronPhosporus, amount = 1000},
          new GameResource(){type = GameResourceType.IronXenon, amount = 1000},
        },
        iconPaths = new string[]{ "Art/stage7/ships/galaxy_transport_2_ui" },
      }
    },
    {
      GalaxyBuildingId.Transport3,
      new GalaxyBuilding(){
        id = GalaxyBuildingId.Transport3,
        name = "Transport Level 3",
        descrip = "Transports 10,000 resources.<br>Max speed 0.8c Max Acceleration 1.8G ",
        isSystemBuilding = true,
        repeatable = true,
        buildTime = 2f,
        maxMoveSpeed = 0.8f,
        maxAcceleration = 1.8f,
        maxTransportResources = 10000,
        tier = 3,
        prereqBuildings = new GalaxyBuildingId[]{ GalaxyBuildingId.Market3 },
        baseResourceCosts = new GameResource[]{
          new GameResource(){type = GameResourceType.PhosphorusTitanium, amount = 500},
          new GameResource(){type = GameResourceType.TitaniumXenon, amount = 500},
        },
        iconPaths = new string[]{ "Art/stage7/ships/galaxy_transport_3_ui" },
      }
    },
    {
      GalaxyBuildingId.Transport4,
      new GalaxyBuilding(){
        id = GalaxyBuildingId.Transport4,
        name = "Transport Level 4",
        descrip = "Transports 50,000 resources.<br>Max speed 0.86c Max Acceleration 2.2G ",
        isSystemBuilding = true,
        repeatable = true,
        buildTime = 2f,
        maxMoveSpeed = 0.86f,
        maxAcceleration = 2.2f,
        maxTransportResources = 50000,
        tier = 4,
        prereqBuildings = new GalaxyBuildingId[]{ GalaxyBuildingId.Market4 },
        baseResourceCosts = new GameResource[]{
          new GameResource(){type = GameResourceType.PhosphorusTitanium, amount = 5000},
          new GameResource(){type = GameResourceType.TitaniumXenon, amount = 5000},
          new GameResource(){type = GameResourceType.TitaniumPromethium, amount = 500},
        },
        iconPaths = new string[]{ "Art/stage7/ships/galaxy_transport_4_ui" },
      }
    },
    {
      GalaxyBuildingId.Transport5,
      new GalaxyBuilding(){
        id = GalaxyBuildingId.Transport5,
        name = "Transport Level 5",
        descrip = "Transports 100,000 resources.<br>Max speed 0.86c Max Acceleration 3.0G ",
        isSystemBuilding = true,
        repeatable = true,
        buildTime = 2f,
        maxMoveSpeed = 0.86f,
        maxAcceleration = 3f,
        maxTransportResources = 100000,
        tier = 5,
        prereqBuildings = new GalaxyBuildingId[]{ GalaxyBuildingId.Market5 },
        baseResourceCosts = new GameResource[]{
          new GameResource(){type = GameResourceType.PhosphorusTitanium, amount = 50000},
          new GameResource(){type = GameResourceType.TitaniumPromethium, amount = 5000},
          new GameResource(){type = GameResourceType.XenonPromethium, amount = 500},
        },
        iconPaths = new string[]{ "Art/stage7/ships/galaxy_transport_5_ui" },
      }
    },


    // {
    //   GalaxyBuildingId.Market6,
    //   new GalaxyBuilding(){
    //     id = GalaxyBuildingId.Market6,
    //     name = "System Market Level 6",
    //     descrip = "Increases transport ship capacity, route radius, and number of routes.",
    //     isSystemBuilding = true,
    //     buildTime = 2f,
    //     requiredTech = HexTechId.GalacticMarkets6,
    //     prereqBuildings = new GalaxyBuildingId[]{ GalaxyBuildingId.Market5 },
    //     influenceRadiusLy = 70,
    //     maxMoveSpeed = 0.86f,
    //     maxAcceleration = 3f,
    //     maxTransportResources = 500000,
    //     resourceCosts = new GameResource[]{
    //       new GameResource(){type = GameResourceType.IronTitanium, amount = 1000000},
    //       new GameResource(){type = GameResourceType.TitaniumXenon, amount = 100000},
    //       new GameResource(){type = GameResourceType.TitaniumPromethium, amount = 10000},
    //     },
    //   }
    // },
    // {
    //   GalaxyBuildingId.Market7,
    //   new GalaxyBuilding(){
    //     id = GalaxyBuildingId.Market7,
    //     name = "System Market Level 7",
    //     descrip = "Increases transport ship capacity, route radius, and number of routes.",
    //     isSystemBuilding = true,
    //     buildTime = 2f,
    //     requiredTech = HexTechId.GalacticMarkets7,
    //     prereqBuildings = new GalaxyBuildingId[]{ GalaxyBuildingId.Market6 },
    //     influenceRadiusLy = 70,
    //     maxMoveSpeed = 0.86f,
    //     maxAcceleration = 3f,
    //     maxTransportResources = 1000000,
    //     resourceCosts = new GameResource[]{
    //       new GameResource(){type = GameResourceType.TitaniumXenon, amount = 1000000},
    //       new GameResource(){type = GameResourceType.TitaniumPromethium, amount = 100000},
    //       new GameResource(){type = GameResourceType.XenonPromethium, amount = 10000},
    //     },
    //   }
    // },


    //Buildings for celestial body settlements
    {
      GalaxyBuildingId.Settlement1,
      new GalaxyBuilding(){
        id = GalaxyBuildingId.Settlement1,
        name = "Settlement Level 1",
        shortName = "Level 1",
        descrip = "Starting level for new settlements. Efficiency 30%",
        settlementEfficiency = 0.3f,
        tier = 1,
        iconPaths = new string[]{ CelestialBody.cbSettlementIcons[1]},
      }
    },
    {
      GalaxyBuildingId.Settlement2,
      new GalaxyBuilding(){
        id = GalaxyBuildingId.Settlement2,
        name = "Settlement Level 2",
        shortName = "Level 2",
        descrip = "Settlement efficiency raised to 60%",
        settlementEfficiency = 0.6f,
        buildTime = 5f,
        tier = 2,
        prereqBuildings = new GalaxyBuildingId[]{ GalaxyBuildingId.Settlement1 },
        baseResourceCosts = new GameResource[]{
          new GameResource(){type = GameResourceType.IronPhosporus, amount = 500},
          new GameResource(){type = GameResourceType.IronTitanium, amount = 500},
        },
        iconPaths = new string[]{ CelestialBody.cbSettlementIcons[2]},
      }
    },
    {
      GalaxyBuildingId.Settlement3,
      new GalaxyBuilding(){
        id = GalaxyBuildingId.Settlement3,
        name = "Settlement Level 3",
        shortName = "Level 3",
        descrip = "Settlement efficiency raised to 100%",
        settlementEfficiency = 1.0f,
        buildTime = 10f,
        tier = 3,
        prereqBuildings = new GalaxyBuildingId[]{ GalaxyBuildingId.Settlement2 },
        baseResourceCosts = new GameResource[]{
          new GameResource(){type = GameResourceType.IronPhosporus, amount = 5000},
          new GameResource(){type = GameResourceType.IronTitanium, amount = 5000},
          new GameResource(){type = GameResourceType.IronPromethium, amount = 500},
        },
        iconPaths = new string[]{ CelestialBody.cbSettlementIcons[3]},
      }
    },
    {
      GalaxyBuildingId.Settlement4,
      new GalaxyBuilding(){
        id = GalaxyBuildingId.Settlement4,
        name = "Settlement Level 4",
        shortName = "Level 4",
        descrip = "Settlement efficiency raised to 150%",
        requiredTech = HexTechId.Colony4,
        settlementEfficiency = 1.5f,
        buildTime = 20f,
        tier = 4,
        prereqBuildings = new GalaxyBuildingId[]{ GalaxyBuildingId.Settlement3 },
        baseResourceCosts = new GameResource[]{
          new GameResource(){type = GameResourceType.PhosphorusTitanium, amount = 10000},
          new GameResource(){type = GameResourceType.PhosphorusSodium, amount = 30000},
          new GameResource(){type = GameResourceType.IronPromethium, amount = 5000},
        },
        iconPaths = new string[]{ CelestialBody.cbSettlementIcons[4]},
      }
    },
    {
      GalaxyBuildingId.Settlement5,
      new GalaxyBuilding(){
        id = GalaxyBuildingId.Settlement5,
        name = "Settlement Level 5",
        shortName = "Level 5",
        descrip = "Settlement efficiency raised to 210%",
        requiredTech = HexTechId.Colony5,
        settlementEfficiency = 2.1f,
        buildTime = 30f,
        tier = 5,
        prereqBuildings = new GalaxyBuildingId[]{ GalaxyBuildingId.Settlement4 },
        baseResourceCosts = new GameResource[]{
          new GameResource(){type = GameResourceType.PhosphorusTitanium, amount = 100000},
          new GameResource(){type = GameResourceType.PhosphorusSodium, amount = 300000},
          new GameResource(){type = GameResourceType.XenonPromethium, amount = 10000},
        },
        iconPaths = new string[]{ CelestialBody.cbSettlementIcons[5]},
      }
    },

    {
      GalaxyBuildingId.Miner0,
      new GalaxyBuilding(){
        id = GalaxyBuildingId.Miner0,
        name = "Basic Miner",
        shortName = "Basic Miner",
        descrip = "Mines resouces and ships them to orbit.",
        buildTime = 1f,
        totalMiningAmountPerYear = 1,
        iconPaths = new string[]{ "Art/stage7/miner_0" },
      }
    },
    {
      GalaxyBuildingId.Miner1,
      new GalaxyBuilding(){
        id = GalaxyBuildingId.Miner1,
        name = "Miner Level 1",
        shortName = "Level 1",
        descrip = "Mines 10 resouces per year when at 100% efficiency",
        buildTime = 1f,
        totalMiningAmountPerYear = 10,
        tier = 1,
        baseResourceCosts = new GameResource[]{
          new GameResource(){type = GameResourceType.Iron, amount = 10},
        },
        iconPaths = new string[]{ "Art/stage7/miner_1" },
      }
    },
    {
      GalaxyBuildingId.Miner2,
      new GalaxyBuilding(){
        id = GalaxyBuildingId.Miner2,
        name = "Miner Level 2",
        shortName = "Level 2",
        descrip = "Mines 100 resouces per year when at 100% efficiency",
        buildTime = 5f,
        totalMiningAmountPerYear = 100,
        tier = 2,
        prereqBuildings = new GalaxyBuildingId[]{ GalaxyBuildingId.Miner1 },
        requiredTech = HexTechId.GalacticIndustry2,
        baseResourceCosts = new GameResource[]{
          new GameResource(){type = GameResourceType.Iron, amount = 500},
          new GameResource(){type = GameResourceType.IronSodium, amount = 500},
        },
        iconPaths = new string[]{ "Art/stage7/miner_2" },
      }
    },
    {
      GalaxyBuildingId.Miner3,
      new GalaxyBuilding(){
        id = GalaxyBuildingId.Miner3,
        name = "Miner Level 3",
        shortName = "Level 3",
        descrip = "Mines 1,000 resouces per year when at 100% efficiency",
        buildTime = 10f,
        totalMiningAmountPerYear = 1000,
        tier = 3,
        prereqBuildings = new GalaxyBuildingId[]{ GalaxyBuildingId.Miner2 },
        requiredTech = HexTechId.GalacticIndustry3,
        baseResourceCosts = new GameResource[]{
          new GameResource(){type = GameResourceType.IronSodium, amount = 5000},
          new GameResource(){type = GameResourceType.SodiumTitanium, amount = 5000},
        },
        iconPaths = new string[]{ "Art/stage7/miner_3" },
      }
    },
    {
      GalaxyBuildingId.Miner4,
      new GalaxyBuilding(){
        id = GalaxyBuildingId.Miner4,
        name = "Miner Level 4",
        shortName = "Level 4",
        descrip = "Mines 10,000 resouces per year when at 100% efficiency",
        buildTime = 15f,
        totalMiningAmountPerYear = 10000,
        tier = 4,
        prereqBuildings = new GalaxyBuildingId[]{ GalaxyBuildingId.Miner3 },
        requiredTech = HexTechId.GalacticIndustry4,
        baseResourceCosts = new GameResource[]{
          new GameResource(){type = GameResourceType.SodiumTitanium, amount = 50000},
          new GameResource(){type = GameResourceType.SodiumPromethium, amount = 5000},
        },
        iconPaths = new string[]{ "Art/stage7/miner_4" },
      }
    },
    {
      GalaxyBuildingId.Miner5,
      new GalaxyBuilding(){
        id = GalaxyBuildingId.Miner5,
        name = "Miner Level 5",
        shortName = "Level 5",
        descrip = "Mines 100,000 resouces per year when at 100% efficiency",
        buildTime = 20f,
        totalMiningAmountPerYear = 100000,
        tier = 5,
        prereqBuildings = new GalaxyBuildingId[]{ GalaxyBuildingId.Miner4 },
        requiredTech = HexTechId.GalacticIndustry5,
        baseResourceCosts = new GameResource[]{
          new GameResource(){type = GameResourceType.SodiumTitanium, amount = 100000},
          new GameResource(){type = GameResourceType.SodiumPromethium, amount = 10000},
        },
        iconPaths = new string[]{ "Art/stage7/miner_5" },
      }
    },

    {
      GalaxyBuildingId.Victory1,
      new GalaxyBuilding(){
        id = GalaxyBuildingId.Victory1,
        name = "Matryoshka Brain Layer 1/7",
        descrip = "Begin Computing Ascension. Increases factory efficiency by 10%",
        isSystemBuilding = true,
        buildTime = 10f,
        factoryEfficiencyBonus = 0.1f,
        tier = 1,
        baseResourceCosts = new GameResource[]{
          new GameResource(){type = GameResourceType.Iron, amount = 100000},
          new GameResource(){type = GameResourceType.Promethium, amount = 10000},
        },
        iconPaths = new string[]{ "Art/stage7/victory_1" },
      }
    },
    {
      GalaxyBuildingId.Victory2,
      new GalaxyBuilding(){
        id = GalaxyBuildingId.Victory2,
        name = "Matryoshka Brain Layer 2/7",
        descrip = "Continue Computing Ascension. Increases factory efficiency by 20%",
        isSystemBuilding = true,
        buildTime = 20f,
        factoryEfficiencyBonus = 0.2f,
        tier = 2,
        prereqBuildings = new GalaxyBuildingId[]{ GalaxyBuildingId.Victory1 },
        baseResourceCosts = new GameResource[]{
          new GameResource(){type = GameResourceType.Titanium, amount = 50000},
          new GameResource(){type = GameResourceType.IronPromethium, amount = 5000},
        },
        iconPaths = new string[]{ "Art/stage7/victory_1" },
      }
    },
    {
      GalaxyBuildingId.Victory3,
      new GalaxyBuilding(){
        id = GalaxyBuildingId.Victory3,
        name = "Matryoshka Brain Layer 3/7",
        descrip = "Continue Computing Ascension. Increases factory efficiency by 30%",
        isSystemBuilding = true,
        buildTime = 20f,
        factoryEfficiencyBonus = 0.3f,
        tier = 3,
        prereqBuildings = new GalaxyBuildingId[]{ GalaxyBuildingId.Victory2 },
        baseResourceCosts = new GameResource[]{
          new GameResource(){type = GameResourceType.Titanium, amount = 100000},
          new GameResource(){type = GameResourceType.IronPromethium, amount = 10000},
        },
        iconPaths = new string[]{ "Art/stage7/victory_3" },
      }
    },
    {
      GalaxyBuildingId.Victory4,
      new GalaxyBuilding(){
        id = GalaxyBuildingId.Victory4,
        name = "Matryoshka Brain Layer 4/7",
        descrip = "Continue Computing Ascension. Increases factory efficiency by 40%",
        isSystemBuilding = true,
        buildTime = 20f,
        factoryEfficiencyBonus = 0.4f,
        tier = 4,
        prereqBuildings = new GalaxyBuildingId[]{ GalaxyBuildingId.Victory3 },
        baseResourceCosts = new GameResource[]{
          new GameResource(){type = GameResourceType.Xenon, amount = 10000},
          new GameResource(){type = GameResourceType.TitaniumPromethium, amount = 10000},
        },
        iconPaths = new string[]{ "Art/stage7/victory_4" },
      }
    },
    {
      GalaxyBuildingId.Victory5,
      new GalaxyBuilding(){
        id = GalaxyBuildingId.Victory5,
        name = "Matryoshka Brain Layer 5/7",
        descrip = "Continue Computing Ascension. Increases factory efficiency by 50%",
        isSystemBuilding = true,
        buildTime = 20f,
        factoryEfficiencyBonus = 0.5f,
        tier = 5,
        prereqBuildings = new GalaxyBuildingId[]{ GalaxyBuildingId.Victory4 },
        baseResourceCosts = new GameResource[]{
          new GameResource(){type = GameResourceType.Xenon, amount = 100000},
          new GameResource(){type = GameResourceType.TitaniumPromethium, amount = 100000},
        },
        iconPaths = new string[]{ "Art/stage7/victory_5" },
      }
    },
    {
      GalaxyBuildingId.Victory6,
      new GalaxyBuilding(){
        id = GalaxyBuildingId.Victory6,
        name = "Matryoshka Brain Layer 6/7",
        descrip = "Continue Computing Ascension. Increases factory efficiency by 60%",
        isSystemBuilding = true,
        buildTime = 25f,
        factoryEfficiencyBonus = 0.6f,
        tier = 6,
        prereqBuildings = new GalaxyBuildingId[]{ GalaxyBuildingId.Victory5 },
        baseResourceCosts = new GameResource[]{
          new GameResource(){type = GameResourceType.IronTitanium, amount = 100000},
          new GameResource(){type = GameResourceType.XenonPromethium, amount = 10000},
        },
        iconPaths = new string[]{ "Art/stage7/victory_6" },
      }
    },
    {
      GalaxyBuildingId.Victory7,
      new GalaxyBuilding(){
        id = GalaxyBuildingId.Victory7,
        name = "Matryoshka Brain Layer 7/7",
        descrip = "Finish Computing Ascension.",
        isSystemBuilding = true,
        buildTime = 30f,
        tier = 7,
        prereqBuildings = new GalaxyBuildingId[]{ GalaxyBuildingId.Victory6 },
        baseResourceCosts = new GameResource[]{
          new GameResource(){type = GameResourceType.IronTitanium, amount = 1000000},
          new GameResource(){type = GameResourceType.PhosphorusSodium, amount = 1000000},
          new GameResource(){type = GameResourceType.XenonPromethium, amount = 100000},
        },
        isVictoryBuilding = true,
        iconPaths = new string[]{ "Art/stage7/victory_7" },
      }
    },
  };


  public static GalaxyBuildingId[] allMarkets = new GalaxyBuildingId[]{
    GalaxyBuildingId.Market0,
    GalaxyBuildingId.Market1,
    GalaxyBuildingId.Market2,
    GalaxyBuildingId.Market3,
    GalaxyBuildingId.Market4,
    GalaxyBuildingId.Market5,
    GalaxyBuildingId.Market6,
    GalaxyBuildingId.Market7,
  };

  public static GalaxyBuildingId[] allTransports = new GalaxyBuildingId[]{
    GalaxyBuildingId.Transport1,
    GalaxyBuildingId.Transport2,
    GalaxyBuildingId.Transport3,
    GalaxyBuildingId.Transport4,
    GalaxyBuildingId.Transport5,
  };

  public static GalaxyBuildingId[] allFactories = new GalaxyBuildingId[]{
    GalaxyBuildingId.Factory1,
    GalaxyBuildingId.Factory2,
    GalaxyBuildingId.Factory3,
    GalaxyBuildingId.Factory4,
    GalaxyBuildingId.Factory5,
  };

  public static GalaxyBuildingId[] allMines = new GalaxyBuildingId[]{
    GalaxyBuildingId.Miner0,
    GalaxyBuildingId.Miner1,
    GalaxyBuildingId.Miner2,
    GalaxyBuildingId.Miner3,
    GalaxyBuildingId.Miner4,
    GalaxyBuildingId.Miner5,
  };

  public static GalaxyBuildingId[] allSettlements = new GalaxyBuildingId[]{
    GalaxyBuildingId.Settlement1,
    GalaxyBuildingId.Settlement2,
    GalaxyBuildingId.Settlement3,
    GalaxyBuildingId.Settlement4,
    GalaxyBuildingId.Settlement5,
  };

  public static GalaxyBuildingId[] allVictory = new GalaxyBuildingId[]{
    GalaxyBuildingId.Victory1,
    GalaxyBuildingId.Victory2,
    GalaxyBuildingId.Victory3,
    GalaxyBuildingId.Victory4,
    GalaxyBuildingId.Victory5,
    GalaxyBuildingId.Victory6,
    GalaxyBuildingId.Victory7,
  };

  public static GalaxyBuildingId[] sortOrder = new GalaxyBuildingId[] {
    //System buildings
    GalaxyBuildingId.Factory1,
    GalaxyBuildingId.Colony1,
    GalaxyBuildingId.Market1,
    GalaxyBuildingId.Transport1,
    GalaxyBuildingId.Factory2,
    GalaxyBuildingId.Market2,
    GalaxyBuildingId.Transport2,
    GalaxyBuildingId.Colony2,
    GalaxyBuildingId.Factory3,
    GalaxyBuildingId.Market3,
    GalaxyBuildingId.Transport3,
    GalaxyBuildingId.Colony3,
    GalaxyBuildingId.Factory4,
    GalaxyBuildingId.Market4,
    GalaxyBuildingId.Transport4,
    GalaxyBuildingId.Colony4,
    GalaxyBuildingId.Factory5,
    GalaxyBuildingId.Transport5,
    GalaxyBuildingId.Colony5,
    GalaxyBuildingId.Market5,
    GalaxyBuildingId.Market6,
    GalaxyBuildingId.Market7,

    //Cb Buildings
    GalaxyBuildingId.Miner1,
    GalaxyBuildingId.Settlement1,
    GalaxyBuildingId.Miner2,
    GalaxyBuildingId.Settlement2,
    GalaxyBuildingId.Miner3,
    GalaxyBuildingId.Settlement3,
    GalaxyBuildingId.Miner4,
    GalaxyBuildingId.Settlement4,
    GalaxyBuildingId.Miner5,
    GalaxyBuildingId.Settlement5,
  };

  public static int GetSortOrder(GalaxyBuildingId id){
    return sortOrder.Contains(id) ?
      Array.IndexOf(GalaxyBuilding.sortOrder, id) :
      999;
  }

  public static GBComparer buildingComparer = new GBComparer();
}

[System.Serializable]
public class GalaxyBuildingData{
  public GalaxyBuildingId buildingId;
  public float progress = 0f;
  public bool started = false;
  public bool finished = false;
}

public enum GalaxyBuildingId {
  None,
  Colony1,
  Colony2,
  Colony3,
  Colony4,
  Colony5,
  Market0,
  Market1,
  Market2,
  Market3,
  Market4,
  Market5,
  Market6,
  Market7,
  Factory1,
  Factory2,
  Factory3,
  Factory4,
  Factory5,
  Transport1,
  Transport2,
  Transport3,
  Transport4,
  Transport5,
  Awesome1,
  Awesome2,
  Awesome3,
  Miner0,
  Miner1,
  Miner2,
  Miner3,
  Miner4,
  Miner5,
  Settlement1,
  Settlement2,
  Settlement3,
  Settlement4,
  Settlement5,
  Victory1,
  Victory2,
  Victory3,
  Victory4,
  Victory5,
  Victory6,
  Victory7,
}

[Serializable]
public class GBComparer : IEqualityComparer<GalaxyBuildingId>
{
  public bool Equals(GalaxyBuildingId a, GalaxyBuildingId b){ return a == b; }
  public int GetHashCode(GalaxyBuildingId a){ return (int)a; }
}

public struct GalaxyBuildingInfo{
  public GalaxyBuilding building;
  public int? quantity;
  public bool disabled;
}