using System;
using System.Collections.Generic;
using UnityEngine;

public class CityBuilding {

  public string name = "";
  public string descrip = "";

  int _productionCost = 0;
  public int buildingLevel = 0; //Corresponds to the tech level it was unlocked at
  private Func<StageSixDataModel, int> calcProductionCost = null;

  public int ProductionCost(int productionMultiplier, StageSixDataModel stageSixData){
    int productionCost = 0;

    if(_productionCost > 0) {
      //Fixed cost
      productionCost = _productionCost;
    }else if(calcProductionCost != null){
      //Dynamic cost
      productionCost = calcProductionCost(stageSixData);
    }else{
      //Automatic building level calculation
      productionCost = Mathf.RoundToInt(20 + Mathf.Pow(buildingLevel, 3.14f) + 45 * buildingLevel);
    }

    return useProductionMultiplier ?
        productionCost * productionMultiplier
      : productionCost;
  }

  public int? secondsToBuild = null;

  public float foodCostPct = 0;
  public CityBuildingId id = CityBuildingId.None;

  public string[] iconPaths;

  public bool repeatable = false;
  public int? maxQuantity = null;
  public bool onMap = false;
  public bool requiresCityConnection = false; //for roads & transport
  public int maxConnectionDistance = 0; //Max number of tiles for path
  public bool multiplyCostOnPopulation = false;
  public bool useProductionMultiplier = false; //is the production cost multiplied by some other value
  public string mapImagePath;

  public int cellBonusFood = 0;
  public int cellBonusProduction = 0;
  public int cellBonusScience = 0;
  public int cellBonusHealth = 0;
  public int cellBonusHappiness = 0;
  public float cityConnectionBonus = 0;

  public HexTechId requiredTech;
  public CityBuildingId[] prereqBuildings; //must have any of the buildings to get this building
  public CityBuildingId[] excludeBuildings; //can't have any of the buildings to get this building

  public HexTechId madeObsoleteByTech = HexTechId.None; //Once researched, make this unavailable to build
  public CityBuildingId[] makesObsolete; //What buildings this will replace and remove from the city
  //If the building is on the map, this indicates what buildings it's strictly a better version of
  //and shouldn't be replaced by any of these buildings
  public CityBuildingId[] betterVersionOf;

  public HexTerrainType[] requiredTerrain = null;
  public HexFeature[] requiredFeature = null;
  public HexBonusResource requiredBonusResource = HexBonusResource.None;
  public bool? requiredRiver = null;
  public bool? requiresOcean = null; //If on map, must be built on ocean, otherwise city must be next to ocean
  public int  requiredCitySize = 0; //If bigger than 0, must have this much pop to build

  public Action<HexCity, CityBuildingData> onApply;
  public Action<HexCity, FinishedCityBuildingData> onRemove;

  public static string iconFood = HexTech.iconFood;
  public static string iconProduction = HexTech.iconProduction;
  public static string iconScience = HexTech.iconScience;
  public static string iconExploration = HexTech.iconExploration;
  public static string iconHappiness = HexTech.iconHappiness;
  public static string iconHealth = HexTech.iconHealth;
  public static string iconSpace = HexTech.iconSpace;

  public static string textIconFood = HexTech.textIconFood;
  public static string textIconProduction = HexTech.textIconProduction;
  public static string textIconScience = HexTech.textIconScience;
  public static string textIconExploration = HexTech.textIconExploration;
  public static string textIconHealth = HexTech.textIconHealth;
  public static string textIconHappiness = HexTech.textIconHappiness;

  static string tradingSuffix = $"of their surplus {textIconFood}{textIconProduction}{textIconScience}output";

  public static Dictionary<CityBuildingId, CityBuilding> allBuildings = new Dictionary<CityBuildingId, CityBuilding>(){
    {
      CityBuildingId.PrimativeHousing,
      new CityBuilding(){
        name = "Primative Housing",
        descrip = $"Increases {textIconFood}on the city tile by +1",
        id = CityBuildingId.PrimativeHousing,
        buildingLevel = 0,
        iconPaths = new string[]{ iconFood },
      }
    },
    {
      CityBuildingId.Granary,
      new CityBuilding(){
        name = "Granary",
        descrip = $"Increases {textIconFood}bonus by 15%",
        id = CityBuildingId.Granary,
        buildingLevel = 1,
        requiredTech = HexTechId.Pottery,
        iconPaths = new string[]{ iconFood },
        onApply = (HexCity city, CityBuildingData data) => { city.data.foodRateBonus += 0.15f; }
      }
    },
    {
      CityBuildingId.Settler,
      new CityBuilding(){
        name = "Settler",
        descrip = $"Settle a new city. Always takes 60 seconds to build and 50% more {textIconFood} is consumed while building the Settler",
        id = CityBuildingId.Settler,
        _productionCost = 60,
        secondsToBuild = 60,
        foodCostPct = 0.5f,
        repeatable = true,
        requiredCitySize = 10,
        requiredTech = HexTechId.Agriculture,
        iconPaths = new string[]{ iconFood, iconExploration },
      }
    },
    {
      CityBuildingId.Farm,
      new CityBuilding(){
        name = "Farm",
        descrip = $"Increases {textIconFood}on a grassland by +1 and {textIconFood}bonus by 5%",
        id = CityBuildingId.Farm,
        calcProductionCost = (StageSixDataModel stageSixData) => {
          var cost = 100;
          if(stageSixData.ResearchedTech(HexTechId.IronSmelting)){ cost += 150; }
          if(stageSixData.ResearchedTech(HexTechId.SteamPower)){   cost += 200; }
          if(stageSixData.ResearchedTech(HexTechId.AtomicAge)){    cost += 500; }
          return cost;
        },
        repeatable = true,
        onMap = true,
        cellBonusFood = 1,
        requiredTech = HexTechId.Agriculture,
        requiredTerrain = new HexTerrainType[]{ HexTerrainType.Grass },
        requiredFeature = new HexFeature[]{ HexFeature.None },
        requiredRiver = false,
        mapImagePath = "Art/stage6/buildings/hex_farm",
        iconPaths = new string[]{ iconFood },
        onApply = (HexCity city, CityBuildingData data) => { city.data.foodRateBonus += 0.05f; }
      }
    },
    {
      CityBuildingId.FishingBoats,
      new CityBuilding(){
        name = "Fishing Boats",
        descrip = $"Increases {textIconFood}on Fish by +2",
        id = CityBuildingId.FishingBoats,
        calcProductionCost = (StageSixDataModel stageSixData) => {
          var cost = 100;
          if(stageSixData.ResearchedTech(HexTechId.IronSmelting)){ cost += 150; }
          if(stageSixData.ResearchedTech(HexTechId.SteamPower)){   cost += 200; }
          if(stageSixData.ResearchedTech(HexTechId.AtomicAge)){    cost += 500; }
          return cost;
        },
        repeatable = true,
        onMap = true,
        cellBonusFood = 2,
        requiredTech = HexTechId.FishingBoats,
        requiredBonusResource = HexBonusResource.Fish,
        iconPaths = new string[]{ iconFood },
        mapImagePath = "Art/stage6/buildings/hex_fishing",
      }
    },
    {
      CityBuildingId.Ranch,
      new CityBuilding(){
        name = "Ranch",
        descrip = $"Increases {textIconFood}on Livestock by +2 and increases {textIconProduction}bonus by 5%",
        id = CityBuildingId.Ranch,
        buildingLevel = 3,
        repeatable = true,
        onMap = true,
        cellBonusFood = 2,
        requiredTech = HexTechId.AnimalDomestication,
        requiredBonusResource = HexBonusResource.Livestock,
        mapImagePath = "Art/stage6/buildings/hex_ranch",
        iconPaths = new string[]{ iconProduction, iconFood },
        onApply = (HexCity city, CityBuildingData data) => { city.data.productionRateBonus += 0.05f; }
      }
    },
    {
      CityBuildingId.Mine,
      new CityBuilding(){
        name = "Mine",
        descrip = $"Increases {textIconProduction}on a hill or mountain by +1",
        id = CityBuildingId.Mine,
        calcProductionCost = (StageSixDataModel stageSixData) => {
          var cost = 200;
          if(stageSixData.ResearchedTech(HexTechId.IronSmelting)){ cost += 100; }
          if(stageSixData.ResearchedTech(HexTechId.SteamPower)){   cost += 200; }
          if(stageSixData.ResearchedTech(HexTechId.AtomicAge)){    cost += 600; }
          return cost;
        },
        repeatable = true,
        onMap = true,
        cellBonusProduction = 1,
        requiredTech = HexTechId.Mining,
        requiredFeature = new HexFeature[]{ HexFeature.Mountains, HexFeature.Hills },
        mapImagePath = "Art/stage6/buildings/hex_mine",
        iconPaths = new string[]{ iconProduction },
      }
    },
    {
      CityBuildingId.Workshop,
      new CityBuilding(){
        name = "Workshop",
        descrip = $"Increases {textIconProduction}on the city tile by +1",
        id = CityBuildingId.Workshop,
        buildingLevel = 5,
        requiredTech = HexTechId.Woodworking,
        iconPaths = new string[]{ iconProduction },
      }
    },
    {
      CityBuildingId.Forge,
      new CityBuilding(){
        name = "Forge",
        descrip = $"Increases {textIconProduction}bonus by 15%",
        id = CityBuildingId.Forge,
        buildingLevel = 6,
        requiredTech = HexTechId.BronzeSmelting,
        iconPaths = new string[]{ iconProduction },
        onApply = (HexCity city, CityBuildingData data) => { city.data.productionRateBonus += 0.15f; }
      }
    },
    {
      CityBuildingId.Study,
      new CityBuilding(){
        name = "Study",
        descrip = $"Increases {textIconScience}by +3",
        id = CityBuildingId.Study,
        buildingLevel = 6,
        requiredTech = HexTechId.Mathematics,
        iconPaths = new string[]{ iconScience },
        onApply = (HexCity city, CityBuildingData data) => { city.data.scienceAmount += 3; }
      }
    },
    {
      CityBuildingId.Corral,
      new CityBuilding(){
        name = "Corral",
        descrip = $"Increases {textIconProduction}on Horses by +2 and {textIconProduction}bonus by 5%",
        id = CityBuildingId.Corral,
        buildingLevel = 7,
        repeatable = true,
        onMap = true,
        cellBonusProduction = 2,
        requiredTech = HexTechId.HorsebackRiding,
        requiredBonusResource = HexBonusResource.Horses,
        mapImagePath = "Art/stage6/buildings/hex_corral",
        iconPaths = new string[]{ iconProduction },
        onApply = (HexCity city, CityBuildingData data) => { city.data.productionRateBonus += 0.05f; }
      }
    },
    {
      CityBuildingId.Winery,
      new CityBuilding(){
        name = "Winery",
        descrip = $"Increases {textIconFood}by +1 and {textIconHappiness}by +3 on Grapes",
        id = CityBuildingId.Winery,
        buildingLevel = 8,
        repeatable = true,
        onMap = true,
        cellBonusFood = 1,
        cellBonusHappiness = 3,
        requiredTech = HexTechId.Fermentation,
        requiredBonusResource = HexBonusResource.Grapes,
        mapImagePath = "Art/stage6/buildings/hex_winery",
        iconPaths = new string[]{ iconHappiness, iconFood },
      }
    },
    {
      CityBuildingId.Theater,
      new CityBuilding(){
        name = "Theater",
        descrip = $"Increases {textIconHappiness}by +5",
        id = CityBuildingId.Theater,
        buildingLevel = 8,
        requiredTech = HexTechId.Construction,
        iconPaths = new string[]{ iconHappiness },
        onApply = (HexCity city, CityBuildingData data) => { city.data.happinessBonus += 5; }
      }
    },
    {
      CityBuildingId.Tribunal,
      new CityBuilding(){
        name = "Tribunal",
        descrip = $"Increases {textIconScience}by +4 and {textIconHappiness}by +2",
        id = CityBuildingId.Tribunal,
        buildingLevel = 8,
        requiredTech = HexTechId.LegalCodes,
        madeObsoleteByTech = HexTechId.PoliticalScience,
        iconPaths = new string[]{ iconScience, iconHappiness },
        onApply = (HexCity city, CityBuildingData data) => {
          city.data.scienceAmount += 4;
          city.data.happinessBonus += 2;
        },
        onRemove = (HexCity city, FinishedCityBuildingData buildingData) => {
          city.data.scienceAmount -= 4;
          city.data.happinessBonus -= 2;
        }
      }
    },
    {
      CityBuildingId.DirtRoad,
      new CityBuilding(){
        name = "Dirt Road",
        descrip = $"Build a road to a nearby city. Trading cities share 5% {tradingSuffix}",
        id = CityBuildingId.DirtRoad,
        _productionCost = 100,
        useProductionMultiplier = true,
        repeatable = true,
        requiresCityConnection = true,
        cityConnectionBonus = 0.05f,
        maxConnectionDistance = 7,
        maxQuantity = 3,
        requiredTech = HexTechId.DirtRoads,
        iconPaths = new string[]{ iconFood, iconProduction, iconScience },
        onApply = (HexCity city, CityBuildingData data) => {
          city.BuildRoad(data, CityBuildingId.DirtRoad);
        },
        onRemove = (HexCity city, FinishedCityBuildingData buildingData) => {
          city.RemoveRoad(buildingData, CityBuildingId.DirtRoad);
        }
      }
    },
    {
      CityBuildingId.IronMine,
      new CityBuilding(){
        name = "Iron Mine",
        descrip = $"Increases {textIconProduction}on an Iron deposit by +2",
        id = CityBuildingId.IronMine,
        buildingLevel = 10,
        repeatable = true,
        onMap = true,
        cellBonusProduction = 2,
        requiredTech = HexTechId.IronSmelting,
        requiredBonusResource = HexBonusResource.Iron,
        betterVersionOf = new CityBuildingId[]{ CityBuildingId.Mine },
        mapImagePath = "Art/stage6/buildings/hex_mine",
        iconPaths = new string[]{ iconProduction },
      }
    },
    {
      CityBuildingId.Blacksmith,
      new CityBuilding(){
        name = "Blacksmith",
        descrip = $"Increases {textIconProduction}on Iron Mine by +2. Increases {textIconProduction}bonus by 10%",
        id = CityBuildingId.Blacksmith,
        buildingLevel = 10,
        requiredTech = HexTechId.IronSmelting,
        prereqBuildings = new CityBuildingId[]{ CityBuildingId.IronMine },
        iconPaths = new string[]{ iconProduction },
        onApply = (HexCity city, CityBuildingData data) => { city.data.productionRateBonus += 0.10f; }
      }
    },
    {
      CityBuildingId.TextileMill,
      new CityBuilding(){
        name = "Textile Mill",
        descrip = $"Increases {textIconProduction}by +1 and {textIconHealth}by +3 on Cotton",
        id = CityBuildingId.TextileMill,
        buildingLevel = 11,
        repeatable = true,
        onMap = true,
        cellBonusProduction = 1,
        cellBonusHealth = 3,
        requiredTech = HexTechId.WeavingLoom,
        requiredBonusResource = HexBonusResource.Cotton,
        mapImagePath = "Art/stage6/buildings/hex_textilemill",
        iconPaths = new string[]{ iconProduction, iconHealth },
      }
    },
    {
      CityBuildingId.Lumbermill,
      new CityBuilding(){
        name = "Lumber Mill",
        descrip = $"Increases {textIconProduction}in a forest by +1 but reduced {textIconFood}by -1",
        id = CityBuildingId.Lumbermill,
        buildingLevel = 11,
        repeatable = true,
        onMap = true,
        cellBonusFood = -1,
        cellBonusProduction = 1,
        requiredTech = HexTechId.MetalCasting,
        requiredFeature = new HexFeature[]{ HexFeature.TreesDense, HexFeature.TreesMedium },
        mapImagePath = "Art/stage6/buildings/hex_lumbermill",
        iconPaths = new string[]{ iconProduction },
      }
    },
    {
      CityBuildingId.Aqueduct,
      new CityBuilding(){
        name = "Aqueduct",
        descrip = $"Acts as a source of fresh water and increases {textIconHealth}by +5",
        id = CityBuildingId.Aqueduct,
        buildingLevel = 12,
        requiredTech = HexTechId.Masonry,
        iconPaths = new string[]{ iconHealth },
        onApply = (HexCity city, CityBuildingData data) => { city.data.healthBonus += 5; }
      }
    },
    {
      CityBuildingId.Lighthouse,
      new CityBuilding(){
        name = "Lighthouse",
        descrip = $"Influenced water tiles get +1 Food",
        id = CityBuildingId.Lighthouse,
        buildingLevel = 12,
        requiresOcean = true,
        requiredTech = HexTechId.Masonry,
        iconPaths = new string[]{ iconFood },
      }
    },
    {
      CityBuildingId.Forum,
      new CityBuilding(){
        name = "Forum",
        descrip = $"Increases {textIconScience}bonus by 5% and {textIconHappiness}by +3",
        id = CityBuildingId.Forum,
        buildingLevel = 12,
        requiredTech = HexTechId.Philosophy,
        madeObsoleteByTech = HexTechId.PrintingPress,
        iconPaths = new string[]{ iconScience, iconHappiness },
        onApply = (HexCity city, CityBuildingData data) => {
          city.data.scienceRateBonus += 0.05f;
          city.data.happinessBonus += 3;
        },
        onRemove = (HexCity city, FinishedCityBuildingData buildingData) => {
          city.data.scienceRateBonus -= 0.05f;
          city.data.happinessBonus -= 3;
        }
      }
    },
    {
      CityBuildingId.BasicHousing,
      new CityBuilding(){
        name = "Basic Housing",
        descrip = $"Increases {textIconFood}on the city tile by +3",
        id = CityBuildingId.BasicHousing,
        requiredTech = HexTechId.Concrete,
        buildingLevel = 13,
        makesObsolete = new CityBuildingId[]{ CityBuildingId.PrimativeHousing },
        iconPaths = new string[]{ iconFood },
      }
    },
    {
      CityBuildingId.Library,
      new CityBuilding(){
        name = "Library",
        descrip = $"Increases {textIconScience}by +5",
        id = CityBuildingId.Library,
        buildingLevel = 14,
        requiredTech = HexTechId.Paper,
        prereqBuildings = new CityBuildingId[]{ CityBuildingId.Study },
        iconPaths = new string[]{ iconScience },
        onApply = (HexCity city, CityBuildingData data) => { city.data.scienceAmount += 5; }
      }
    },
    {
      CityBuildingId.PublicBaths,
      new CityBuilding(){
        name = "Public Bath",
        descrip = $"One less sanititation {textIconHealth} penalty per 50 population",
        id = CityBuildingId.PublicBaths,
        buildingLevel = 14,
        requiredTech = HexTechId.PublicBaths,
        madeObsoleteByTech = HexTechId.SewerSystem,
        iconPaths = new string[]{ iconHealth },
      }
    },
    {
      CityBuildingId.Watermill,
      new CityBuilding(){
        name = "Water Mill",
        descrip = $"Increases tile {textIconProduction}by +2 and city {textIconProduction}bonus by 10%. Requires River",
        id = CityBuildingId.Watermill,
        buildingLevel = 14,
        onMap = true,
        requiredRiver = true,
        requiredTech = HexTechId.Watermill,
        iconPaths = new string[]{ iconProduction },
        cellBonusProduction = 2,
        mapImagePath = "Art/stage6/buildings/hex_watermill",
        onApply = (HexCity city, CityBuildingData data) => {
          city.data.productionRateBonus += 0.1f;
        }
      }
    },
    {
      CityBuildingId.Palace,
      new CityBuilding(){
        name = "Palace",
        descrip = $"Increases city trade bonus by 5%. Increases {textIconScience}by +6 and {textIconHappiness}by +4",
        id = CityBuildingId.Palace,
        buildingLevel = 15,
        requiredTech = HexTechId.Architecture,
        makesObsolete = new CityBuildingId[]{ CityBuildingId.Tribunal },
        iconPaths = new string[]{ iconScience, iconHappiness },
        onApply = (HexCity city, CityBuildingData data) => {
          city.data.scienceAmount += 6;
          city.data.happinessBonus += 4;
          city.data.tradeBonus += 0.05f;
        },
        onRemove = (HexCity city, FinishedCityBuildingData buildingData) => {
          city.data.scienceAmount -= 6;
          city.data.happinessBonus -= 4;
          city.data.tradeBonus -= 0.05f;
        }
      }
    },
    {
      CityBuildingId.School,
      new CityBuilding(){
        name = "School",
        descrip = $"Increases {textIconScience}by +7",
        id = CityBuildingId.School,
        buildingLevel = 15,
        requiredTech = HexTechId.Algebra,
        iconPaths = new string[]{ iconScience },
        prereqBuildings = new CityBuildingId[]{ CityBuildingId.Library },
        onApply = (HexCity city, CityBuildingData data) => { city.data.scienceAmount += 7; }
      }
    },
    {
      CityBuildingId.Port,
      new CityBuilding(){
        name = "Port",
        descrip = $"Unlocks Water Trade Route. Influenced water tiles get +1 {textIconProduction}",
        id = CityBuildingId.Port,
        requiresOcean = true,
        requiredTech = HexTechId.Compass,
        buildingLevel = 16,
        iconPaths = new string[]{ iconProduction },
        onApply = (HexCity city, CityBuildingData data) => {
          city.data.foodRateBonus += 0.05f;
          city.data.productionRateBonus += 0.05f;
        }
      }
    },
    {
      CityBuildingId.StoneRoad,
      new CityBuilding(){
        name = "Stone Road",
        descrip = $"Build a stone road to a nearby city. Trading cities share 6% {tradingSuffix}",
        id = CityBuildingId.StoneRoad,
        _productionCost = 300,
        useProductionMultiplier = true,
        repeatable = true,
        requiresCityConnection = true,
        cityConnectionBonus = 0.06f,
        maxConnectionDistance = 9,
        maxQuantity = 3,
        makesObsolete = new CityBuildingId[]{ CityBuildingId.DirtRoad },
        requiredTech = HexTechId.StoneRoads,
        iconPaths = new string[]{ iconFood, iconProduction, iconScience },
        onApply = (HexCity city, CityBuildingData data) => {
          city.BuildRoad(data, CityBuildingId.StoneRoad);
        },
        onRemove = (HexCity city, FinishedCityBuildingData buildingData) => {
          city.RemoveRoad(buildingData, CityBuildingId.StoneRoad);
        }
      }
    },
    {
      CityBuildingId.WaterTradeRoute,
      new CityBuilding(){
        name = "Water Trade Route",
        descrip = $"Open a water trade route to a nearby city. Trading cities share 7% {tradingSuffix}",
        id = CityBuildingId.WaterTradeRoute,
        _productionCost = 400,
        useProductionMultiplier = true,
        repeatable = true,
        maxQuantity = 3,
        requiresCityConnection = true,
        maxConnectionDistance = 15,
        requiresOcean = true,
        cityConnectionBonus = 0.07f,
        prereqBuildings = new CityBuildingId[]{ CityBuildingId.Port },
        iconPaths = new string[]{ iconProduction },
        onApply = (HexCity city, CityBuildingData data) => { city.BuildRoad(data, CityBuildingId.WaterTradeRoute); }
      }
    },
    {
      CityBuildingId.PrintingPress,
      new CityBuilding(){
        name = "Printing Press",
        descrip = $"Increases trade bonus by 5%, {textIconScience}by 10%, and {textIconHappiness} by +5",
        id = CityBuildingId.PrintingPress,
        buildingLevel = 17,
        requiredTech = HexTechId.PrintingPress,
        makesObsolete = new CityBuildingId[]{ CityBuildingId.Forum },
        iconPaths = new string[]{ iconScience, iconFood, iconProduction },
        onApply = (HexCity city, CityBuildingData data) => {
          city.data.scienceRateBonus += 0.1f;
          city.data.tradeBonus += 0.05f;
          city.data.happinessBonus += 5;
        }
      }
    },
    {
      CityBuildingId.University,
      new CityBuilding(){
        name = "University",
        descrip = $"Increases {textIconScience}by +12",
        id = CityBuildingId.University,
        buildingLevel = 18,
        requiredTech = HexTechId.Physics,
        prereqBuildings = new CityBuildingId[]{ CityBuildingId.School },
        iconPaths = new string[]{ iconScience },
        onApply = (HexCity city, CityBuildingData data) => { city.data.scienceAmount += 12; }
      }
    },
    {
      CityBuildingId.Observatory,
      new CityBuilding(){
        name = "Observatory",
        descrip = $"Increases tile {textIconScience}by +6 and city {textIconScience}bonus by 10%. Requires Mountain",
        id = CityBuildingId.Observatory,
        buildingLevel = 19,
        onMap = true,
        requiredFeature = new HexFeature[]{ HexFeature.Mountains },
        requiredTech = HexTechId.Optics,
        iconPaths = new string[]{ iconScience },
        cellBonusScience = 6,
        mapImagePath = "Art/stage6/buildings/hex_observatory",
        onApply = (HexCity city, CityBuildingData data) => {
          city.data.scienceRateBonus += 0.1f;
        }
      }
    },
    {
      CityBuildingId.GoldMine,
      new CityBuilding(){
        name = "Gold Mine",
        descrip = $"Increases {textIconProduction}on a Gold deposit by +2 and {textIconHappiness}by +5.",
        id = CityBuildingId.GoldMine,
        buildingLevel = 19,
        repeatable = true,
        onMap = true,
        cellBonusProduction = 2,
        cellBonusHappiness = 5,
        betterVersionOf = new CityBuildingId[]{ CityBuildingId.Mine },
        requiredTech = HexTechId.Chemistry,
        requiredBonusResource = HexBonusResource.Gold,
        mapImagePath = "Art/stage6/buildings/hex_mine",
        iconPaths = new string[]{ iconProduction, iconHappiness },
      }
    },
    {
      CityBuildingId.CoalMine,
      new CityBuilding(){
        name = "Coal Mine",
        descrip = $"Increases {textIconProduction}on a Coal deposit by +4. Reduces {textIconHealth}by -3",
        id = CityBuildingId.CoalMine,
        buildingLevel = 20,
        repeatable = true,
        onMap = true,
        cellBonusProduction = 4,
        cellBonusHealth = -3,
        betterVersionOf = new CityBuildingId[]{ CityBuildingId.Mine },
        requiredTech = HexTechId.SteamPower,
        requiredBonusResource = HexBonusResource.Coal,
        mapImagePath = "Art/stage6/buildings/hex_mine",
        iconPaths = new string[]{ iconProduction },
      }
    },
    {
      CityBuildingId.SteamPoweredFactory,
      new CityBuilding(){
        name = "Steam Powered Factory",
        descrip = $"Increases {textIconProduction}on the city tile by +6. Increases Pollution by +1. Requires a Coal Mine",
        id = CityBuildingId.SteamPoweredFactory,
        buildingLevel = 20,
        makesObsolete = new CityBuildingId[]{ CityBuildingId.Workshop },
        requiredTech = HexTechId.SteamPower,
        prereqBuildings = new CityBuildingId[]{ CityBuildingId.CoalMine },
        iconPaths = new string[]{ iconProduction },
        onApply = (HexCity city, CityBuildingData data) => { city.stageSixData.pollutionLevel += 1; },
        onRemove = (HexCity city, FinishedCityBuildingData buildingData) => { city.stageSixData.pollutionLevel -= 1; },
      }
    },
    {
      CityBuildingId.TraditionalHousing,
      new CityBuilding(){
        name = "Traditional Housing",
        descrip = $"Increases {textIconFood}on the city tile by +5",
        id = CityBuildingId.TraditionalHousing,
        requiredTech = HexTechId.SteelSmelting,
        buildingLevel = 22,
        makesObsolete = new CityBuildingId[]{ CityBuildingId.BasicHousing, CityBuildingId.PrimativeHousing },
        iconPaths = new string[]{ iconFood },
      }
    },
    {
      CityBuildingId.SteelSmelter,
      new CityBuilding(){
        name = "Steel Smelter",
        descrip = $"Increase {textIconProduction}bonus by 15%. Requires Iron Mine",
        id = CityBuildingId.SteelSmelter,
        buildingLevel = 22,
        requiredTech = HexTechId.SteelSmelting,
        iconPaths = new string[]{ iconProduction },
        prereqBuildings = new CityBuildingId[]{ CityBuildingId.IronMine },
        onApply = (HexCity city, CityBuildingData data) => {
          city.data.productionRateBonus += 0.15f;
        }
      }
    },
    {
      CityBuildingId.Labratory,
      new CityBuilding(){
        name = "Labratory",
        descrip = $"Increases {textIconScience}bonus by 15%",
        id = CityBuildingId.Labratory,
        buildingLevel = 22,
        requiredTech = HexTechId.Biology,
        iconPaths = new string[]{ iconScience },
        onApply = (HexCity city, CityBuildingData data) => {
          city.data.scienceRateBonus += 0.15f;
        }
      }
    },
    {
      CityBuildingId.CityHall,
      new CityBuilding(){
        name = "City Hall",
        descrip = $"Increases city trade bonus by 8%. Increases {textIconScience}by +10 and {textIconHappiness}by +6",
        id = CityBuildingId.CityHall,
        buildingLevel = 23,
        requiredTech = HexTechId.PoliticalScience,
        makesObsolete = new CityBuildingId[]{ CityBuildingId.Tribunal, CityBuildingId.Palace },
        iconPaths = new string[]{ iconScience, iconHappiness },
        onApply = (HexCity city, CityBuildingData data) => {
          city.data.tradeBonus += 0.08f;
          city.data.scienceAmount += 10;
          city.data.happinessBonus += 6;
        },
        onRemove = (HexCity city, FinishedCityBuildingData buildingData) => {
          city.data.tradeBonus -= 0.08f;
          city.data.scienceAmount -= 10;
          city.data.happinessBonus -= 6;
        }
      }
    },
    {
      CityBuildingId.Railroad,
      new CityBuilding(){
        name = "Railroad",
        descrip = $"Build a railroad to a nearby city. Trading cities share 8% {tradingSuffix}",
        id = CityBuildingId.Railroad,
        _productionCost = 950,
        useProductionMultiplier = true,
        repeatable = true,
        requiresCityConnection = true,
        cityConnectionBonus = 0.08f,
        maxConnectionDistance = 10,
        maxQuantity = 3,
        requiredTech = HexTechId.Railways,
        iconPaths = new string[]{ iconFood, iconProduction, iconScience },
        onApply = (HexCity city, CityBuildingData data) => {
          city.BuildRoad(data, CityBuildingId.Railroad);
        },
        onRemove = (HexCity city, FinishedCityBuildingData buildingData) => {
          city.RemoveRoad(buildingData, CityBuildingId.Railroad);
        }
      }
    },
    {
      CityBuildingId.OilWell,
      new CityBuilding(){
        name = "Oil Well",
        descrip = $"Increases {textIconProduction}by +5 and Reduces {textIconHealth}by -4 on Oil",
        id = CityBuildingId.OilWell,
        buildingLevel = 24,
        repeatable = true,
        onMap = true,
        cellBonusProduction = 5,
        cellBonusHealth = -4,
        requiredTech = HexTechId.OilRefining,
        requiredTerrain = new HexTerrainType[]{
          HexTerrainType.Sand,
          HexTerrainType.Grass,
          HexTerrainType.Mud,
          HexTerrainType.Stone,
          HexTerrainType.Snow,
          HexTerrainType.Ice,
        },
        requiredBonusResource = HexBonusResource.Oil,
        mapImagePath = "Art/stage6/buildings/hex_oilwell",
        iconPaths = new string[]{ iconProduction },
      }
    },
    {
      CityBuildingId.OffshoreOilPlatform,
      new CityBuilding(){
        name = "Offshore Oil Platform",
        descrip = $"Increases {textIconProduction}by +6 and Reduces {textIconHealth}by -5 on Oil",
        id = CityBuildingId.OffshoreOilPlatform,
        buildingLevel = 25,
        repeatable = true,
        onMap = true,
        cellBonusProduction = 6,
        cellBonusHealth = -5,
        requiredTech = HexTechId.OilRefining,
        requiredTerrain = new HexTerrainType[]{
          HexTerrainType.Water,
          HexTerrainType.Shallows,
        },
        requiredBonusResource = HexBonusResource.Oil,
        mapImagePath = "Art/stage6/buildings/hex_offshoreoil",
        iconPaths = new string[]{ iconProduction },
      }
    },
    {
      CityBuildingId.PowerPlant,
      new CityBuilding(){
        name = "Power Plant",
        descrip = $"Increases {textIconFood}{textIconProduction}{textIconScience}bonus by 10%. Increases Pollution by +1. Requires a worked source of Coal or Oil.",
        id = CityBuildingId.PowerPlant,
        buildingLevel = 24,
        requiredTech = HexTechId.Electricity,
        iconPaths = new string[]{ iconFood, iconProduction, iconScience },
        prereqBuildings = new CityBuildingId[]{ CityBuildingId.CoalMine, CityBuildingId.OilWell, CityBuildingId.OffshoreOilPlatform },
        onApply = (HexCity city, CityBuildingData data) => {
          city.data.foodRateBonus += 0.1f;
          city.data.productionRateBonus += 0.1f;
          city.data.scienceRateBonus += 0.1f;
          city.stageSixData.pollutionLevel += 1;
        },
        onRemove = (HexCity city, FinishedCityBuildingData buildingData) => {
          city.data.foodRateBonus -= 0.1f;
          city.data.productionRateBonus -= 0.1f;
          city.data.scienceRateBonus -= 0.1f;
          city.stageSixData.pollutionLevel -= 1;
        },
      }
    },
    {
      CityBuildingId.SewerSystem,
      new CityBuilding(){
        name = "Sewer System",
        descrip = $"One less sanititation {textIconHealth}penalty per 30 population",
        id = CityBuildingId.SewerSystem,
        buildingLevel = 25,
        requiredTech = HexTechId.SewerSystem,
        makesObsolete = new CityBuildingId[]{ CityBuildingId.PublicBaths },
        iconPaths = new string[]{ iconHealth },
        onApply = (HexCity city, CityBuildingData data) => { }
      }
    },
    {
      CityBuildingId.Highway,
      new CityBuilding(){
        name = "Highway",
        descrip = $"Build a Highway to a nearby city. Trading cities share 10% {tradingSuffix}",
        id = CityBuildingId.Highway,
        _productionCost = 1800,
        useProductionMultiplier = true,
        repeatable = true,
        requiresCityConnection = true,
        cityConnectionBonus = 0.1f,
        maxConnectionDistance = 10,
        maxQuantity = 3,
        makesObsolete = new CityBuildingId[]{ CityBuildingId.StoneRoad, CityBuildingId.DirtRoad },
        requiredTech = HexTechId.Automobile,
        iconPaths = new string[]{ iconFood, iconProduction, iconScience },
        onApply = (HexCity city, CityBuildingData data) => {
          city.BuildRoad(data, CityBuildingId.Highway);
        },
        onRemove = (HexCity city, FinishedCityBuildingData buildingData) => {
          city.RemoveRoad(buildingData, CityBuildingId.Highway);
        }
      }
    },
    {
      CityBuildingId.AluminumMine,
      new CityBuilding(){
        name = "Aluminum Mine",
        descrip = $"Increases {textIconProduction}on an Aluminum deposit by +6",
        id = CityBuildingId.AluminumMine,
        buildingLevel = 26,
        repeatable = true,
        onMap = true,
        cellBonusProduction = 6,
        betterVersionOf = new CityBuildingId[]{ CityBuildingId.Mine },
        requiredTech = HexTechId.Electrification,
        requiredBonusResource = HexBonusResource.Aluminum,
        mapImagePath = "Art/stage6/buildings/hex_mine",
        iconPaths = new string[]{ iconProduction },
      }
    },
    {
      CityBuildingId.Clinic,
      new CityBuilding(){
        name = "Clinic",
        descrip = $"Increases {textIconHealth}by +5",
        id = CityBuildingId.Clinic,
        buildingLevel = 26,
        requiredTech = HexTechId.Microbiology,
        iconPaths = new string[]{ iconHealth },
        onApply = (HexCity city, CityBuildingData data) => {
          city.data.healthBonus += 5;
        }
      }
    },
    {
      CityBuildingId.CombineHarvesters,
      new CityBuilding(){
        name = "Combine Harvesters",
        descrip = $"Increase {textIconFood}on Farms by +1",
        id = CityBuildingId.CombineHarvesters,
        buildingLevel = 27,
        requiredTech = HexTechId.MechanizedAgriculture,
        iconPaths = new string[]{ iconFood },
        onApply = (HexCity city, CityBuildingData data) => { }
      }
    },
    {
      CityBuildingId.BroadcastTower,
      new CityBuilding(){
        name = "Broadcast Tower",
        descrip = $"Increases {textIconScience}by +10 and {textIconHappiness} by +5",
        id = CityBuildingId.BroadcastTower,
        buildingLevel = 27,
        requiredTech = HexTechId.Radio,
        iconPaths = new string[]{ iconScience },
        onApply = (HexCity city, CityBuildingData data) => {
          city.data.scienceAmount += 10;
          city.data.happinessBonus += 5;
        }
      }
    },
    {
      CityBuildingId.Airport,
      new CityBuilding(){
        name = "Airport",
        descrip = "Explore a large area around the city. Increases trade bonus by 10%. Increases Pollution by +1",
        id = CityBuildingId.Airport,
        buildingLevel = 28,
        requiredTech = HexTechId.Flight,
        iconPaths = new string[]{ iconFood, iconProduction, iconScience },
        onApply = (HexCity city, CityBuildingData data) => {
          city.data.tradeBonus += 0.10f;
          city.ExploreAround(10);
          city.stageSixData.pollutionLevel += 1;
        },
        onRemove = (HexCity city, FinishedCityBuildingData buildingData) => {
          city.data.tradeBonus -= 0.10f;
          city.stageSixData.pollutionLevel -= 1;
        },
      }
    },
    {
      CityBuildingId.PlasticsPlant,
      new CityBuilding(){
        name = "Plastics Plant",
        descrip = $"Increases {textIconFood}{textIconProduction}{textIconScience}bonus by 10%. Increases Pollution by +1",
        id = CityBuildingId.PlasticsPlant,
        buildingLevel = 28,
        requiredTech = HexTechId.Plastics,
        iconPaths = new string[]{ iconFood, iconProduction, iconScience },
        onApply = (HexCity city, CityBuildingData data) => {
          city.data.foodRateBonus += 0.1f;
          city.data.productionRateBonus += 0.1f;
          city.data.scienceRateBonus += 0.1f;
          city.stageSixData.pollutionLevel += 1;
        },
        onRemove = (HexCity city, FinishedCityBuildingData buildingData) => {
          city.data.foodRateBonus -= 0.1f;
          city.data.productionRateBonus -= 0.1f;
          city.data.scienceRateBonus -= 0.1f;
          city.stageSixData.pollutionLevel -= 1;
        },
      }
    },
    {
      CityBuildingId.UraniumMine,
      new CityBuilding(){
        name = "Uranium Mine",
        descrip = $"Increases {textIconProduction}on a Uranium deposit by +8. Reduces {textIconHealth}by -1",
        id = CityBuildingId.UraniumMine,
        buildingLevel = 29,
        repeatable = true,
        onMap = true,
        cellBonusProduction = 8,
        cellBonusHealth = -1,
        betterVersionOf = new CityBuildingId[]{ CityBuildingId.Mine },
        requiredTech = HexTechId.AtomicAge,
        requiredBonusResource = HexBonusResource.Uranium,
        mapImagePath = "Art/stage6/buildings/hex_mine",
        iconPaths = new string[]{ iconProduction },
      }
    },
    {
      CityBuildingId.NuclearPowerPlant,
      new CityBuilding(){
        name = "Nuclear Power Plant",
        descrip = $"Increases {textIconFood}{textIconProduction}{textIconScience}bonus by 20%. Requires a Uranium Mine",
        id = CityBuildingId.NuclearPowerPlant,
        buildingLevel = 29,
        makesObsolete = new CityBuildingId[]{ CityBuildingId.PowerPlant },
        requiredTech = HexTechId.AtomicAge,
        prereqBuildings = new CityBuildingId[]{ CityBuildingId.UraniumMine },
        iconPaths = new string[]{ iconFood, iconProduction, iconScience },
        onApply = (HexCity city, CityBuildingData data) => {
          city.data.foodRateBonus += 0.2f;
          city.data.productionRateBonus += 0.2f;
          city.data.scienceRateBonus += 0.2f;
          city.stageSixData.pollutionLevel += 1;
        },
        onRemove = (HexCity city, FinishedCityBuildingData buildingData) => {
          city.data.foodRateBonus -= 0.2f;
          city.data.productionRateBonus -= 0.2f;
          city.data.scienceRateBonus -= 0.2f;
          city.stageSixData.pollutionLevel -= 1;
        },
      }
    },
    {
      CityBuildingId.RocketLab,
      new CityBuilding(){
        name = "Rocket Lab",
        descrip = $"Increases {textIconScience}by +14 and {textIconProduction}bonus by 5%",
        id = CityBuildingId.RocketLab,
        buildingLevel = 30,
        requiredTech = HexTechId.Rocketry,
        prereqBuildings = new CityBuildingId[]{ CityBuildingId.School },
        iconPaths = new string[]{ iconScience },
        onApply = (HexCity city, CityBuildingData data) => {
          city.data.scienceAmount += 14;
          city.data.productionRateBonus += 0.05f;
        }
      }
    },
    {
      CityBuildingId.ProcessorFabricator,
      new CityBuilding(){
        name = "Processor Fabricator",
        descrip = $"Increases {textIconScience}by +15 and {textIconProduction}bonus by 6%",
        id = CityBuildingId.ProcessorFabricator,
        buildingLevel = 31,
        requiredTech = HexTechId.Computers,
        prereqBuildings = new CityBuildingId[]{ CityBuildingId.University },
        iconPaths = new string[]{ iconScience, iconProduction },
        onApply = (HexCity city, CityBuildingData data) => {
          city.data.scienceAmount += 15;
          city.data.productionRateBonus += 0.06f;
        }
      }
    },
    {
      CityBuildingId.Hospital,
      new CityBuilding(){
        name = "Hospital",
        descrip = $"Increases {textIconHealth}by +10",
        id = CityBuildingId.Hospital,
        buildingLevel = 31,
        requiredTech = HexTechId.SpecializedMedicine,
        iconPaths = new string[]{ iconHealth },
        onApply = (HexCity city, CityBuildingData data) => { city.data.healthBonus += 10; },
        onRemove = (HexCity city, FinishedCityBuildingData buildingData) => { city.data.healthBonus -= 10; }
      }
    },
    {
      CityBuildingId.AdvancedHousing,
      new CityBuilding(){
        name = "Advanced Housing",
        descrip = $"Increases {textIconFood}on the city tile by +10",
        id = CityBuildingId.AdvancedHousing,
        requiredTech = HexTechId.SkipToTheFuture,
        buildingLevel = 32,
        prereqBuildings = new CityBuildingId[]{ CityBuildingId.TraditionalHousing },
        makesObsolete = new CityBuildingId[]{ CityBuildingId.TraditionalHousing, CityBuildingId.BasicHousing, CityBuildingId.PrimativeHousing },
        iconPaths = new string[]{ iconFood },
      }
    },
    {
      CityBuildingId.PersonalizedMedicine,
      new CityBuilding(){
        name = "Personalized Medicine",
        descrip = $"Increases {textIconHealth}by +20",
        id = CityBuildingId.PersonalizedMedicine,
        buildingLevel = 33,
        requiredTech = HexTechId.SkipToTheFuture,
        prereqBuildings = new CityBuildingId[]{ CityBuildingId.Hospital },
        makesObsolete = new CityBuildingId[]{ CityBuildingId.Hospital },
        iconPaths = new string[]{ iconHealth },
        onApply = (HexCity city, CityBuildingData data) => {
          city.data.healthBonus += 20;
        }
      }
    },
    {
      CityBuildingId.FusionPowerPlant,
      new CityBuilding(){
        name = "Fusion Power Plant",
        descrip = $"Increases {textIconFood}{textIconProduction}{textIconScience}bonus by 30%",
        id = CityBuildingId.FusionPowerPlant,
        buildingLevel = 34,
        makesObsolete = new CityBuildingId[]{ CityBuildingId.PowerPlant },
        requiredTech = HexTechId.SkipToTheFuture,
        iconPaths = new string[]{ iconFood, iconProduction, iconScience },
        onApply = (HexCity city, CityBuildingData data) => {
          city.data.foodRateBonus += 0.3f;
          city.data.productionRateBonus += 0.3f;
          city.data.scienceRateBonus += 0.3f;
        },
        onRemove = (HexCity city, FinishedCityBuildingData buildingData) => {
          city.data.foodRateBonus -= 0.3f;
          city.data.productionRateBonus -= 0.3f;
          city.data.scienceRateBonus -= 0.3f;
        },
      }
    },
    {
      CityBuildingId.AIScienceHub,
      new CityBuilding(){
        name = "AI Science Hub",
        descrip = $"Increases {textIconScience}by +30",
        id = CityBuildingId.AIScienceHub,
        buildingLevel = 35,
        requiredTech = HexTechId.SkipToTheFuture,
        prereqBuildings = new CityBuildingId[]{ CityBuildingId.ProcessorFabricator },
        iconPaths = new string[]{ iconScience },
        onApply = (HexCity city, CityBuildingData data) => {
          city.data.scienceAmount += 30;
        }
      }
    },
    {
      CityBuildingId.Spaceport,
      new CityBuilding(){
        name = "Spaceport",
        descrip = "Increases trade bonus by 20%. Explore a very large area around the city.",
        id = CityBuildingId.Spaceport,
        buildingLevel = 36,
        requiredTech = HexTechId.SkipToTheFuture,
        iconPaths = new string[]{ iconFood, iconProduction, iconScience },
        prereqBuildings = new CityBuildingId[]{ CityBuildingId.Airport },
        makesObsolete = new CityBuildingId[]{ CityBuildingId.Airport },
        onApply = (HexCity city, CityBuildingData data) => {
          city.data.tradeBonus += 0.2f;
          city.ExploreAround(40);
        },
        onRemove = (HexCity city, FinishedCityBuildingData buildingData) => {
          city.data.tradeBonus -= 0.2f;
        },
      }
    },
    {
      CityBuildingId.MatterRecombiner,
      new CityBuilding(){
        name = "Matter Recombiner",
        descrip = $"Increases {textIconProduction}bonus by 25%. Reduces pollution by -1.",
        id = CityBuildingId.MatterRecombiner,
        buildingLevel = 37,
        requiredTech = HexTechId.SkipToTheFuture,
        iconPaths = new string[]{ iconProduction },
        onApply = (HexCity city, CityBuildingData data) => {
          city.data.productionRateBonus += 0.25f;
          city.stageSixData.pollutionLevel -= 1;
        }
      }
    },

  };

}

public enum CityBuildingId {
  None,
  PrimativeHousing,
  Granary,
  Farm,
  FishingBoats,
  Ranch,
  Mine,
  Workshop,
  Library,
  Lumbermill,
  Settler,
  Aqueduct,
  Theater,
  Forge,
  DirtRoad,
  Tribunal,
  Study,
  Blacksmith,
  Corral,
  Winery,
  TextileMill,
  BasicHousing,
  Palace,
  School,
  Port,
  WaterTradeRoute,
  Lighthouse,
  Forum,
  PublicBaths,
  Watermill,
  PrintingPress,
  University,
  Observatory,
  SteamPoweredFactory,
  SteelSmelter,
  Labratory,
  StoneRoad,
  Railroad,
  Highway,
  River, //Unused as building but for compat with connections
  OilWell,
  OffshoreOilPlatform,
  PowerPlant,
  SewerSystem,
  CombineHarvesters,
  BroadcastTower,
  Airport,
  IronMine,
  CoalMine,
  GoldMine,
  AluminumMine,
  UraniumMine,
  PlasticsPlant,
  NuclearPowerPlant,
  RocketLab,
  ProcessorFabricator,
  Hospital,
  TraditionalHousing,
  Clinic,
  FusionPowerPlant,
  AdvancedHousing,
  PersonalizedMedicine,
  AIScienceHub,
  Spaceport,
  MatterRecombiner,
  CityHall,
  //ISP Node
}