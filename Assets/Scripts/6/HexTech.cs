using System;
using System.Collections.Generic;
using UnityEngine;

public class HexTech {

  public string name = "";
  public string descrip = "";
  public int techCost(StageSixRulesProps stageRules) {
    if(_techCost > 0) { return _techCost; }

    float level = techLevel + stageRules.additionalTechLevel;
    return Mathf.RoundToInt(600 + Mathf.Pow(1.59f, level) + 200 * level);
  }
  int _techCost = 0;
  public int techLevel = 0; //What row is it in the tree to calc tech costs
  public HexTechId id = HexTechId.None;
  public string[] iconPaths;

  public HexTechId[] prereqTechs;

  public Action<TechAdvancer> onApply;

  public static string iconFood = GameResource.resourceIconPaths[GameResourceType.Food];
  public static string iconProduction = GameResource.resourceIconPaths[GameResourceType.Production];
  public static string iconScience = GameResource.resourceIconPaths[GameResourceType.Science];
  public static string iconExploration = "Art/stage6/icons/exploration_icon";
  public static string iconHappiness = "Art/stage6/icons/happiness_icon";
  public static string iconHealth = "Art/stage6/icons/health_icon";
  public static string iconSpace = "Art/stage6/icons/space_icon";

  public static string textIconFood = "<sprite=\"hex_text_sprite_atlas\" name=\"food\">";
  public static string textIconProduction = "<sprite=\"hex_text_sprite_atlas\" name=\"production\">";
  public static string textIconScience = "<sprite=\"hex_text_sprite_atlas\" name=\"science\">";
  public static string textIconExploration = "<sprite=\"hex_text_sprite_atlas\" name=\"exploration\">";
  public static string textIconHealth = "<sprite=\"hex_text_sprite_atlas\" name=\"health\">";
  public static string textIconHappiness = "<sprite=\"hex_text_sprite_atlas\" name=\"happiness\">";
  public static string textIconSpace = "<sprite=\"hex_text_sprite_atlas\" name=\"space\">";

  public static Dictionary<HexTechId, HexTech> allTechs = new Dictionary<HexTechId, HexTech>(){
    {
      HexTechId.Pottery,
      new HexTech(){
        name = "Pottery",
        descrip = "Unlocks Granary and reveals Salt",
        id = HexTechId.Pottery,
        techLevel = 1,
        iconPaths = new string[]{ iconFood },
        onApply = (TechAdvancer manager) => { manager.RevealResource(HexBonusResource.Salt); }
      }
    },
    {
      HexTechId.Agriculture,
      new HexTech(){
        name = "Agriculture",
        descrip = "Unlocks Settler and Farms for your grasslands.  Revelas Grains",
        id = HexTechId.Agriculture,
        techLevel = 2,
        iconPaths = new string[]{ iconFood },
        prereqTechs = new HexTechId[]{ HexTechId.Pottery },
        onApply = (TechAdvancer manager) => { manager.RevealResource(HexBonusResource.Grains); }
      }
    },
    {
      HexTechId.AnimalDomestication,
      new HexTech(){
        name = "Animal Domestication",
        descrip = "Unlocks Ranches for Livestock",
        id = HexTechId.AnimalDomestication,
        techLevel = 3,
        iconPaths = new string[]{ iconFood },
        prereqTechs = new HexTechId[]{ HexTechId.Agriculture },
      }
    },
    {
      HexTechId.FishingBoats,
      new HexTech(){
        name = "Fishing Boats",
        descrip = "Unlocks Fishing boats for Fish",
        id = HexTechId.FishingBoats,
        techLevel = 3,
        iconPaths = new string[]{ iconFood },
        prereqTechs = new HexTechId[]{ HexTechId.Agriculture },
      }
    },
    {
      HexTechId.Wheel,
      new HexTech(){
        name = "Wheel",
        descrip = "Your scouts can move 50% further and gain +1 range",
        id = HexTechId.Wheel,
        techLevel = 4,
        iconPaths = new string[]{ iconExploration },
        prereqTechs = new HexTechId[]{ HexTechId.AnimalDomestication },
        onApply = (TechAdvancer manager) => { manager.stageSixData.scoutRadiusBonus += 1; }
      }
    },
    {
      HexTechId.Mining,
      new HexTech(){
        name = "Mining",
        descrip = "Reveals Stone and allows construction of Mines",
        id = HexTechId.Mining,
        techLevel = 4,
        iconPaths = new string[]{ iconProduction },
        prereqTechs = new HexTechId[]{ HexTechId.AnimalDomestication },
        onApply = (TechAdvancer manager) => { manager.RevealResource(HexBonusResource.Stone); }
      }
    },
    {
      HexTechId.Sailing,
      new HexTech(){
        name = "Sailing",
        descrip = "Allows your scouts and settlers to cross water",
        id = HexTechId.Sailing,
        techLevel = 4,
        iconPaths = new string[]{ iconExploration },
        prereqTechs = new HexTechId[]{ HexTechId.FishingBoats },
      }
    },
    {
      HexTechId.CropRotation,
      new HexTech(){
        name = "Crop Rotation",
        descrip = $"Reveals Cotton and Increases {textIconFood}bonus by 20%",
        id = HexTechId.CropRotation,
        techLevel = 5,
        iconPaths = new string[]{ iconFood },
        prereqTechs = new HexTechId[]{ HexTechId.Wheel },
        onApply = (TechAdvancer manager) => {
          manager.stageSixData.foodRateBonus += 0.2f;
          manager.RevealResource(HexBonusResource.Cotton);
        }
      }
    },
    {
      HexTechId.Woodworking,
      new HexTech(){
        name = "Woodworking",
        descrip = $"Unlocks Workshop. Increases {textIconProduction}bonus by 15%",
        id = HexTechId.Woodworking,
        techLevel = 5,
        iconPaths = new string[]{ iconProduction },
        prereqTechs = new HexTechId[]{ HexTechId.Mining },
        onApply = (TechAdvancer manager) => { manager.stageSixData.productionRateBonus += 0.15f; }
      }
    },
    {
      HexTechId.Writing,
      new HexTech(){
        name = "Writing",
        descrip = $"Increases {textIconScience}bonus by 20%",
        id = HexTechId.Writing,
        techLevel = 5,
        iconPaths = new string[]{ iconScience },
        prereqTechs = new HexTechId[]{ HexTechId.Sailing },
        onApply = (TechAdvancer manager) => { manager.stageSixData.scienceRateBonus += 0.2f; }
      }
    },
    {
      HexTechId.Irrigation,
      new HexTech(){
        name = "Irrigation",
        descrip = $"Farms produce +1 {textIconFood}. Reveal Sugar",
        id = HexTechId.Irrigation,
        techLevel = 6,
        iconPaths = new string[]{ iconFood },
        prereqTechs = new HexTechId[]{ HexTechId.Woodworking, HexTechId.CropRotation },
        onApply = (TechAdvancer manager) => {
          manager.RevealResource(HexBonusResource.Sugar);
        }
      }
    },
    {
      HexTechId.BronzeSmelting,
      new HexTech(){
        name = "Bronze Smelting",
        descrip = "Unlocks Forge",
        id = HexTechId.BronzeSmelting,
        techLevel = 6,
        iconPaths = new string[]{ iconProduction },
        prereqTechs = new HexTechId[]{ HexTechId.Woodworking },
      }
    },
    {
      HexTechId.Mathematics,
      new HexTech(){
        name = "Mathematics",
        descrip = "Unlocks Study",
        id = HexTechId.Mathematics,
        techLevel = 6,
        iconPaths = new string[]{ iconScience },
        prereqTechs = new HexTechId[]{ HexTechId.Writing },
      }
    },
    {
      HexTechId.Calendar,
      new HexTech(){
        name = "Calendar",
        descrip = $"Increases {textIconFood}bonus by 10%",
        id = HexTechId.Calendar,
        techLevel = 7,
        iconPaths = new string[]{ iconFood },
        prereqTechs = new HexTechId[]{ HexTechId.Irrigation },
        onApply = (TechAdvancer manager) => { manager.stageSixData.foodRateBonus += 0.1f; }
      }
    },
    {
      HexTechId.HorsebackRiding,
      new HexTech(){
        name = "Horseback Riding",
        descrip = "Unlocks Corral. Your scouts can move 50% further and gain +2 range",
        id = HexTechId.HorsebackRiding,
        techLevel = 7,
        iconPaths = new string[]{ iconExploration, iconProduction },
        prereqTechs = new HexTechId[]{ HexTechId.BronzeSmelting },
        onApply = (TechAdvancer manager) => { manager.stageSixData.scoutRadiusBonus += 2; }
      }
    },
    {
      HexTechId.Astronomy,
      new HexTech(){
        name = "Astronomy",
        descrip = $"Increases {textIconScience}bonus by 10%. Your scouts can see further",
        id = HexTechId.Astronomy,
        techLevel = 7,
        iconPaths = new string[]{ iconScience, iconExploration },
        prereqTechs = new HexTechId[]{ HexTechId.Mathematics },
        onApply = (TechAdvancer manager) => { manager.stageSixData.scienceRateBonus += 0.1f; }
      }
    },
    {
      HexTechId.Fermentation,
      new HexTech(){
        name = "Fermentation",
        descrip = "Reveals Grapes and unlocks Winery",
        id = HexTechId.Fermentation,
        techLevel = 8,
        iconPaths = new string[]{ iconFood, iconHappiness },
        prereqTechs = new HexTechId[]{ HexTechId.Calendar },
        onApply = (TechAdvancer manager) => { manager.RevealResource(HexBonusResource.Grapes); }
      }
    },
    {
      HexTechId.Construction,
      new HexTech(){
        name = "Construction",
        descrip = $"Increases {textIconProduction}bonus by 15%. Unlocks Theater",
        id = HexTechId.Construction,
        techLevel = 8,
        iconPaths = new string[]{ iconProduction, iconHappiness },
        prereqTechs = new HexTechId[]{ HexTechId.HorsebackRiding },
        onApply = (TechAdvancer manager) => { manager.stageSixData.productionRateBonus += 0.15f; }
      }
    },
    {
      HexTechId.LegalCodes,
      new HexTech(){
        name = "Legal Codes",
        descrip = "Your settlers gain +1 range. Unlocks Tribunal",
        id = HexTechId.LegalCodes,
        techLevel = 8,
        iconPaths = new string[]{ iconExploration, iconScience },
        prereqTechs = new HexTechId[]{ HexTechId.Astronomy },
        onApply = (TechAdvancer manager) => { manager.stageSixData.settlerRadiusBonus += 1; }
      }
    },
    {
      HexTechId.Plowing,
      new HexTech(){
        name = "Plowing",
        descrip = $"Increases {textIconFood}bonus by 10%",
        id = HexTechId.Plowing,
        techLevel = 9,
        iconPaths = new string[]{ iconFood },
        prereqTechs = new HexTechId[]{ HexTechId.Fermentation },
        onApply = (TechAdvancer manager) => { manager.stageSixData.foodRateBonus += 0.10f; }
      }
    },


    {
      HexTechId.DirtRoads,
      new HexTech(){
        name = "Dirt Roads",
        descrip = "Unlocks Dirt Roads",
        id = HexTechId.DirtRoads,
        techLevel = 9,
        iconPaths = new string[]{ iconProduction },
        prereqTechs = new HexTechId[]{ HexTechId.Construction, HexTechId.LegalCodes },
      }
    },
    {
      HexTechId.IronSmelting,
      new HexTech(){
        name = "Iron Smelting",
        descrip = "Reveals Iron and unlocks Iron Mine and Blacksmith",
        id = HexTechId.IronSmelting,
        techLevel = 10,
        iconPaths = new string[]{ iconProduction },
        prereqTechs = new HexTechId[]{ HexTechId.Plowing, HexTechId.DirtRoads },
        onApply = (TechAdvancer manager) => { manager.RevealResource(HexBonusResource.Iron); }
      }
    },
    {
      HexTechId.WeavingLoom,
      new HexTech(){
        name = "Weaving Loom",
        descrip = $"Unlocks Textile Mill. +3 {textIconHealth}in cities",
        id = HexTechId.WeavingLoom,
        techLevel = 11,
        iconPaths = new string[]{ iconProduction, iconHealth },
        prereqTechs = new HexTechId[]{ HexTechId.IronSmelting },
        onApply = (TechAdvancer manager) => {
          manager.stageSixData.healthBonus += 3;
        }
      }
    },
    {
      HexTechId.MetalCasting,
      new HexTech(){
        name = "Metal Casting",
        descrip = "Unlocks Lumbermill",
        id = HexTechId.MetalCasting,
        techLevel = 11,
        iconPaths = new string[]{ iconProduction },
        prereqTechs = new HexTechId[]{ HexTechId.IronSmelting },
      }
    },
    {
      HexTechId.Masonry,
      new HexTech(){
        name = "Masonry",
        descrip = "Unlocks Aqueduct and Lighthouse",
        id = HexTechId.Masonry,
        techLevel = 12,
        iconPaths = new string[]{ iconFood, iconHealth },
        prereqTechs = new HexTechId[]{ HexTechId.MetalCasting },
      }
    },
    {
      HexTechId.Philosophy,
      new HexTech(){
        name = "Philosophy",
        descrip = $"Unlocks Forum. +3 {textIconHappiness}in cities",
        id = HexTechId.Philosophy,
        techLevel = 12,
        iconPaths = new string[]{ iconScience, iconHappiness },
        prereqTechs = new HexTechId[]{ HexTechId.MetalCasting },
        onApply = (TechAdvancer manager) => { manager.stageSixData.happinessBonus += 3; }
      }
    },
    {
      HexTechId.Concrete,
      new HexTech(){
        name = "Concrete",
        descrip = $"Unlocks Basic Housing. Increases {textIconProduction}bonus by 10%",
        id = HexTechId.Concrete,
        techLevel = 13,
        iconPaths = new string[]{ iconProduction, iconFood },
        prereqTechs = new HexTechId[]{ HexTechId.Masonry },
        onApply = (TechAdvancer manager) => { manager.stageSixData.productionRateBonus += 0.10f; }
      }
    },
    {
      HexTechId.SimpleMachines,
      new HexTech(){
        name = "Simple Machines",
        descrip = $"Increases {textIconScience}and {textIconProduction}bonus by 10%",
        id = HexTechId.SimpleMachines,
        techLevel = 13,
        iconPaths = new string[]{ iconScience, iconProduction },
        prereqTechs = new HexTechId[]{ HexTechId.Philosophy },
        onApply = (TechAdvancer manager) => {
          manager.stageSixData.scienceRateBonus += 0.10f;
          manager.stageSixData.productionRateBonus += 0.10f;
        }
      }
    },
    {
      HexTechId.PublicBaths,
      new HexTech(){
        name = "Public Baths",
        descrip = "Unlocks Public Bath",
        id = HexTechId.PublicBaths,
        techLevel = 14,
        iconPaths = new string[]{ iconHealth },
        prereqTechs = new HexTechId[]{ HexTechId.Concrete },
      }
    },
    {
      HexTechId.Watermill,
      new HexTech(){
        name = "Water Mill",
        descrip = "Unlocks Water Mill for rivers",
        id = HexTechId.Watermill,
        techLevel = 14,
        iconPaths = new string[]{ iconProduction },
        prereqTechs = new HexTechId[]{ HexTechId.Concrete },
      }
    },
    {
      HexTechId.Paper,
      new HexTech(){
        name = "Paper",
        descrip = "Unlocks Library",
        id = HexTechId.Paper,
        techLevel = 14,
        iconPaths = new string[]{ iconScience },
        prereqTechs = new HexTechId[]{ HexTechId.SimpleMachines },
      }
    },
    {
      HexTechId.Architecture,
      new HexTech(){
        name = "Architecture",
        descrip = $"Increases {textIconProduction}bonus by 5%. Unlocks Palace",
        id = HexTechId.Architecture,
        techLevel = 15,
        iconPaths = new string[]{ iconProduction, iconHappiness },
        prereqTechs = new HexTechId[]{ HexTechId.Paper },
        onApply = (TechAdvancer manager) => { manager.stageSixData.productionRateBonus += 0.05f; }
      }
    },
    {
      HexTechId.Algebra,
      new HexTech(){
        name = "Algebra",
        descrip = $"Unlocks School. Increases {textIconScience}bonus by 10%",
        id = HexTechId.Algebra,
        techLevel = 15,
        iconPaths = new string[]{ iconScience },
        prereqTechs = new HexTechId[]{ HexTechId.Paper },
        onApply = (TechAdvancer manager) => {
          manager.stageSixData.scienceRateBonus += 0.10f;
        }
      }
    },
    {
      HexTechId.NumeralSystem,
      new HexTech(){
        name = "Numeral System",
        descrip = $"Increases {textIconFood}{textIconProduction}{textIconScience}bonus by 10%",
        id = HexTechId.NumeralSystem,
        techLevel = 15,
        iconPaths = new string[]{ iconFood, iconProduction, iconScience },
        prereqTechs = new HexTechId[]{ HexTechId.Paper },
        onApply = (TechAdvancer manager) => {
          manager.stageSixData.foodRateBonus += 0.10f;
          manager.stageSixData.scienceRateBonus += 0.10f;
          manager.stageSixData.productionRateBonus += 0.10f;
        }
      }
    },
    {
      HexTechId.StoneRoads,
      new HexTech(){
        name = "Stone Roads",
        descrip = "Unlocks Stone Roads",
        id = HexTechId.StoneRoads,
        techLevel = 16,
        iconPaths = new string[]{ iconProduction },
        prereqTechs = new HexTechId[]{ HexTechId.Architecture, HexTechId.Algebra},
        onApply = (TechAdvancer manager) => { }
      }
    },
    {
      HexTechId.Compass,
      new HexTech(){
        name = "Compass",
        descrip = "Unlocks Port. Your scouts can move 25% further. Your settlers and scouts gain +2 range",
        id = HexTechId.Compass,
        techLevel = 16,
        iconPaths = new string[]{ iconExploration, iconFood },
        prereqTechs = new HexTechId[]{ HexTechId.NumeralSystem, HexTechId.Algebra },
        onApply = (TechAdvancer manager) => {
          manager.stageSixData.settlerRadiusBonus += 2;
          manager.stageSixData.scoutRadiusBonus += 2;
        }
      }
    },
    {
      HexTechId.PrintingPress,
      new HexTech(){
        name = "Printing Press",
        descrip = "Unlocks Printing Press",
        id = HexTechId.PrintingPress,
        techLevel = 17,
        iconPaths = new string[]{ iconFood, iconProduction, iconScience },
        prereqTechs = new HexTechId[]{ HexTechId.Compass },
      }
    },

    {
      HexTechId.SelectiveBreeding,
      new HexTech(){
        name = "Selective Breeding",
        descrip = $"Increases {textIconFood}bonus by 10%",
        id = HexTechId.SelectiveBreeding,
        techLevel = 18,
        iconPaths = new string[]{ iconFood },
        prereqTechs = new HexTechId[]{ HexTechId.PrintingPress },
        onApply = (TechAdvancer manager) => {
          manager.stageSixData.foodRateBonus += 0.10f;
        }
      }
    },
    {
      HexTechId.Physics,
      new HexTech(){
        name = "Physics",
        descrip = $"Unlocks University. Increases {textIconScience}bonus by 10%",
        id = HexTechId.Physics,
        techLevel = 18,
        iconPaths = new string[]{ iconScience },
        prereqTechs = new HexTechId[]{ HexTechId.PrintingPress },
        onApply = (TechAdvancer manager) => {
          manager.stageSixData.scienceRateBonus += 0.10f;
        }
      }
    },
    {
      HexTechId.Chemistry,
      new HexTech(){
        name = "Chemistry",
        descrip = $"Reveals Gold. Unlocks Gold Mine. Increases {textIconScience}and {textIconFood}bonus by 10%",
        id = HexTechId.Chemistry,
        techLevel = 19,
        iconPaths = new string[]{ iconScience, iconFood },
        prereqTechs = new HexTechId[]{ HexTechId.SelectiveBreeding, HexTechId.Physics },
        onApply = (TechAdvancer manager) => {
          manager.RevealResource(HexBonusResource.Gold);
          manager.stageSixData.foodRateBonus += 0.10f;
          manager.stageSixData.scienceRateBonus += 0.10f;
        }
      }
    },
    {
      HexTechId.Optics,
      new HexTech(){
        name = "Optics",
        descrip = $"Unlocks Observatory. Increases {textIconScience}by 5%. Your scouts can move 25% further and gain +1 range",
        id = HexTechId.Optics,
        techLevel = 19,
        iconPaths = new string[]{ iconScience, iconExploration },
        prereqTechs = new HexTechId[]{ HexTechId.Physics },
        onApply = (TechAdvancer manager) => {
          manager.stageSixData.scienceRateBonus += 0.05f;
          manager.stageSixData.scoutRadiusBonus += 1;
        }
      }
    },
    {
      HexTechId.SteamPower,
      new HexTech(){
        name = "Steam Power",
        descrip = "Reveals Coal. Unlocks Coal Mine and Steam Powered Factory",
        id = HexTechId.SteamPower,
        techLevel = 20,
        iconPaths = new string[]{ iconProduction, iconFood, iconScience },
        prereqTechs = new HexTechId[]{ HexTechId.Chemistry, HexTechId.Optics },
        onApply = (TechAdvancer manager) => {
          manager.RevealResource(HexBonusResource.Coal);
        }
      }
    },

    {
      HexTechId.TextileMachinery,
      new HexTech(){
        name = "Textile Machinery",
        descrip = $"+5 {textIconHealth}in cities",
        id = HexTechId.TextileMachinery,
        techLevel = 21,
        iconPaths = new string[]{ iconHealth },
        prereqTechs = new HexTechId[]{ HexTechId.SteamPower },
        onApply = (TechAdvancer manager) => {
          manager.stageSixData.healthBonus += 5;
        }
      }
    },
    {
      HexTechId.MassProducedIron,
      new HexTech(){
        name = "Mass Produced Iron",
        descrip = $"Increases {textIconProduction}on Iron Mines by +2. Increases {textIconProduction}bonus by 5%",
        id = HexTechId.MassProducedIron,
        techLevel = 21,
        iconPaths = new string[]{ iconProduction },
        prereqTechs = new HexTechId[]{ HexTechId.SteamPower },
        onApply = (TechAdvancer manager) => {
          manager.stageSixData.productionRateBonus += 0.05f;
        }
      }
    },
    {
      HexTechId.MechanicalThresher,
      new HexTech(){
        name = "Mechanical Thresher",
        descrip = $"Increases {textIconFood}on Farms by +1",
        id = HexTechId.MechanicalThresher,
        techLevel = 22,
        iconPaths = new string[]{ iconFood },
        prereqTechs = new HexTechId[]{ HexTechId.TextileMachinery },
        onApply = (TechAdvancer manager) => {

        }
      }
    },
    {
      HexTechId.SteelSmelting,
      new HexTech(){
        name = "Steel Smelting",
        descrip = "Unlocks Steel Smelter and Traditional Housing",
        id = HexTechId.SteelSmelting,
        techLevel = 22,
        iconPaths = new string[]{ iconProduction },
        prereqTechs = new HexTechId[]{ HexTechId.MassProducedIron },
        onApply = (TechAdvancer manager) => { }
      }
    },
    {
      HexTechId.Biology,
      new HexTech(){
        name = "Biology",
        descrip = "Unlocks Labratory",
        id = HexTechId.Biology,
        techLevel = 22,
        iconPaths = new string[]{ iconScience },
        prereqTechs = new HexTechId[]{ HexTechId.MassProducedIron },
      }
    },
    {
      HexTechId.Fertilizer,
      new HexTech(){
        name = "Fertilizer",
        descrip = $"Increases {textIconFood}bonus by 10%",
        id = HexTechId.Fertilizer,
        techLevel = 23,
        iconPaths = new string[]{ iconFood },
        prereqTechs = new HexTechId[]{ HexTechId.SteelSmelting, HexTechId.MechanicalThresher },
        onApply = (TechAdvancer manager) => {
          manager.stageSixData.foodRateBonus += 0.10f;
        }
      }
    },
    {
      HexTechId.PoliticalScience,
      new HexTech(){
        name = "Political Science",
        descrip = $"Unlocks City Hall",
        id = HexTechId.PoliticalScience,
        techLevel = 23,
        iconPaths = new string[]{ iconScience, iconHappiness },
        prereqTechs = new HexTechId[]{ HexTechId.SteelSmelting },
      }
    },
    {
      HexTechId.Railways,
      new HexTech(){
        name = "Railways",
        descrip = "Unlocks Railroads",
        id = HexTechId.Railways,
        techLevel = 23,
        iconPaths = new string[]{ iconFood, iconProduction, iconScience },
        prereqTechs = new HexTechId[]{ HexTechId.SteelSmelting, HexTechId.Biology },
        onApply = (TechAdvancer manager) => { }
      }
    },
    {
      HexTechId.OilRefining,
      new HexTech(){
        name = "Oil Refining",
        descrip = "Reveals Oil. Unlocks Oil Well and Offshore Oil Platform",
        id = HexTechId.OilRefining,
        techLevel = 24,
        iconPaths = new string[]{ iconProduction },
        prereqTechs = new HexTechId[]{ HexTechId.Fertilizer, HexTechId.Railways },
        onApply = (TechAdvancer manager) => {
          manager.RevealResource(HexBonusResource.Oil);
        }
      }
    },
    {
      HexTechId.Electricity,
      new HexTech(){
        name = "Electricity",
        descrip = $"Unlocks Power Plant. +5 {textIconHappiness}in cities",
        id = HexTechId.Electricity,
        techLevel = 24,
        iconPaths = new string[]{ iconHappiness, iconScience, iconProduction },
        prereqTechs = new HexTechId[]{ HexTechId.Railways },
        onApply = (TechAdvancer manager) => {
          manager.stageSixData.happinessBonus += 5;
        }
      }
    },
    {
      HexTechId.SewerSystem,
      new HexTech(){
        name = "Sewer System",
        descrip = "Unlocks Sewer System",
        id = HexTechId.SewerSystem,
        techLevel = 25,
        iconPaths = new string[]{ iconHealth },
        prereqTechs = new HexTechId[]{ HexTechId.OilRefining },
        onApply = (TechAdvancer manager) => { }
      }
    },
    {
      HexTechId.Telegraph,
      new HexTech(){
        name = "Telegraph",
        descrip = "Increases trade bonus by 10%",
        id = HexTechId.Telegraph,
        techLevel = 25,
        iconPaths = new string[]{ iconProduction, iconScience },
        prereqTechs = new HexTechId[]{ HexTechId.Electricity},
        onApply = (TechAdvancer manager) => {
          manager.stageSixData.tradeBonus += 0.1f;
        }
      }
    },
    {
      HexTechId.Electrification,
      new HexTech(){
        name = "Electrification",
        descrip = $"Reveals Aluminum. Unlocks Aluminum Mine. Increases {textIconFood}{textIconProduction}{textIconScience}bonus by 15%",
        id = HexTechId.Electrification,
        techLevel = 26,
        iconPaths = new string[]{ iconProduction },
        prereqTechs = new HexTechId[]{ HexTechId.SewerSystem, HexTechId.Telegraph },
        onApply = (TechAdvancer manager) => {
          manager.RevealResource(HexBonusResource.Aluminum);
          manager.stageSixData.foodRateBonus += 0.15f;
          manager.stageSixData.productionRateBonus += 0.15f;
          manager.stageSixData.scienceRateBonus += 0.15f;
        }
      }
    },
    {
      HexTechId.Microbiology,
      new HexTech(){
        name = "Microbiology",
        descrip = $"Increases Medicine {textIconHealth}by +4 in cities. Unlocks Clinic",
        id = HexTechId.Microbiology,
        techLevel = 26,
        iconPaths = new string[]{ iconHealth },
        prereqTechs = new HexTechId[]{ HexTechId.SewerSystem, HexTechId.Telegraph },
      }
    },
    {
      HexTechId.MechanizedAgriculture,
      new HexTech(){
        name = "Mechanized Agriculture",
        descrip = "Unlocks Combine Harvesters",
        id = HexTechId.MechanizedAgriculture,
        techLevel = 27,
        iconPaths = new string[]{ iconFood },
        prereqTechs = new HexTechId[]{ HexTechId.Electrification },
        onApply = (TechAdvancer manager) => { }
      }
    },
    {
      HexTechId.Automobile,
      new HexTech(){
        name = "Automobile",
        descrip = "Unlocks Highway. Increases trade bonus by 5%",
        id = HexTechId.Automobile,
        techLevel = 27,
        iconPaths = new string[]{ iconFood, iconProduction, iconScience },
        prereqTechs = new HexTechId[]{ HexTechId.Electrification, HexTechId.Microbiology },
        onApply = (TechAdvancer manager) => {
          manager.stageSixData.tradeBonus += 0.05f;
        }
      }
    },
    {
      HexTechId.Radio,
      new HexTech(){
        name = "Radio",
        descrip = $"Unlocks Broadcast Tower. Increases {textIconScience}bonus by 10%",
        id = HexTechId.Radio,
        techLevel = 27,
        iconPaths = new string[]{ iconScience, iconProduction },
        prereqTechs = new HexTechId[]{ HexTechId.Electrification },
        onApply = (TechAdvancer manager) => {
          manager.stageSixData.scienceRateBonus += 0.10f;
        }
      }
    },
    {
      HexTechId.Refrigeration,
      new HexTech(){
        name = "Refrigeration",
        descrip = $"Increases {textIconFood}bonus by 10% and {textIconProduction}bonus by 5%",
        id = HexTechId.Refrigeration,
        techLevel = 28,
        iconPaths = new string[]{ iconFood },
        prereqTechs = new HexTechId[]{ HexTechId.Automobile, HexTechId.MechanizedAgriculture },
        onApply = (TechAdvancer manager) => {
          manager.stageSixData.foodRateBonus += 0.10f;
          manager.stageSixData.productionRateBonus += 0.05f;
        }
      }
    },
    {
      HexTechId.Flight,
      new HexTech(){
        name = "Flight",
        descrip = "Unlocks Airport",
        id = HexTechId.Flight,
        techLevel = 28,
        iconPaths = new string[]{ iconFood, iconProduction, iconScience },
        prereqTechs = new HexTechId[]{ HexTechId.Automobile, HexTechId.Radio },
        onApply = (TechAdvancer manager) => { }
      }
    },
    {
      HexTechId.Plastics,
      new HexTech(){
        name = "Plastics",
        descrip = "Unlocks Plastics Plant",
        id = HexTechId.Plastics,
        techLevel = 28,
        iconPaths = new string[]{ iconScience, iconFood, iconProduction },
        prereqTechs = new HexTechId[]{ HexTechId.Radio },
        onApply = (TechAdvancer manager) => { }
      }
    },
    {
      HexTechId.Antibiotics,
      new HexTech(){
        name = "Antibiotics",
        descrip = $"Increases Medicine {textIconHealth}by +6 in cities. Increases {textIconFood}bonus by 5%",
        id = HexTechId.Antibiotics,
        techLevel = 29,
        iconPaths = new string[]{ iconFood, iconHealth },
        prereqTechs = new HexTechId[]{ HexTechId.Refrigeration },
        onApply = (TechAdvancer manager) => {
          manager.stageSixData.foodRateBonus += 0.05f;
        }
      }
    },
    {
      HexTechId.AtomicAge,
      new HexTech(){
        name = "Atomic Age",
        descrip = "Reveals Uranium. Unlocks Uranium Mine and Nuclear Power Plant",
        id = HexTechId.AtomicAge,
        techLevel = 29,
        iconPaths = new string[]{ iconProduction, iconScience },
        prereqTechs = new HexTechId[]{ HexTechId.Flight },
        onApply = (TechAdvancer manager) => {
          manager.RevealResource(HexBonusResource.Uranium);
        }
      }
    },
    {
      HexTechId.Relativity,
      new HexTech(){
        name = "Relativity",
        descrip = $"Increases {textIconScience}bonus by 10%",
        id = HexTechId.Relativity,
        techLevel = 29,
        iconPaths = new string[]{ iconScience },
        prereqTechs = new HexTechId[]{ HexTechId.Plastics, HexTechId.Flight },
        onApply = (TechAdvancer manager) => {
          manager.stageSixData.scienceRateBonus += 0.10f;
        }
      }
    },
    {
      HexTechId.Genetics,
      new HexTech(){
        name = "Genetics",
        descrip = $"Increases {textIconFood}bonus by 10%",
        id = HexTechId.Genetics,
        techLevel = 30,
        iconPaths = new string[]{ iconFood },
        prereqTechs = new HexTechId[]{ HexTechId.Antibiotics },
        onApply = (TechAdvancer manager) => {
          manager.stageSixData.foodRateBonus += 0.10f;
        }
      }
    },
    {
      HexTechId.Rocketry,
      new HexTech(){
        name = "Rocketry",
        descrip = "Unlocks Rocket Lab",
        id = HexTechId.Rocketry,
        techLevel = 30,
        iconPaths = new string[]{ iconProduction, iconScience },
        prereqTechs = new HexTechId[]{ HexTechId.AtomicAge, HexTechId.Relativity },
        onApply = (TechAdvancer manager) => { }
      }
    },
    {
      HexTechId.AdvancedFertilizers,
      new HexTech(){
        name = "Advanced Fertilizers",
        descrip = $"Increases {textIconFood}bonus by 15%",
        id = HexTechId.AdvancedFertilizers,
        techLevel = 31,
        iconPaths = new string[]{ iconFood },
        prereqTechs = new HexTechId[]{ HexTechId.Genetics },
        onApply = (TechAdvancer manager) => {
          manager.stageSixData.foodRateBonus += 0.15f;
        }
      }
    },
    {
      HexTechId.SpecializedMedicine,
      new HexTech(){
        name = "Specialized Medicine",
        descrip = $"Unlocks Hospital. Increases Medicine {textIconHealth}by +7 in cities",
        id = HexTechId.SpecializedMedicine,
        techLevel = 31,
        iconPaths = new string[]{ iconHealth },
        prereqTechs = new HexTechId[]{ HexTechId.Genetics },
        onApply = (TechAdvancer manager) => { }
      }
    },
    {
      HexTechId.Computers,
      new HexTech(){
        name = "Computers",
        descrip = $"Unlocks Processor Fabricator. Increases {textIconFood}{textIconProduction}{textIconScience}bonus by 10%",
        id = HexTechId.Computers,
        techLevel = 31,
        iconPaths = new string[]{ iconFood, iconProduction, iconScience },
        prereqTechs = new HexTechId[]{ HexTechId.Rocketry },
        onApply = (TechAdvancer manager) => {
          manager.stageSixData.foodRateBonus += 0.10f;
          manager.stageSixData.productionRateBonus += 0.10f;
          manager.stageSixData.scienceRateBonus += 0.10f;
        }
      }
    },

    {
      HexTechId.SpaceFlight,
      new HexTech(){
        name = "Space Flight",
        descrip = "Unlocks the final frontier",
        id = HexTechId.SpaceFlight,
        techLevel = 32,
        iconPaths = new string[]{ iconSpace },
        prereqTechs = new HexTechId[]{ HexTechId.SpecializedMedicine, HexTechId.AdvancedFertilizers, HexTechId.Computers },
        onApply = (TechAdvancer manager) => { manager.stageTransition.UnlockNextStage(7); }
      }
    },
    {
      HexTechId.SkipToTheFuture,
      new HexTech(){
        name = "Glimpse the Future",
        descrip = "Fast forward a few years to look beyond and glimpse the future.",
        id = HexTechId.SkipToTheFuture,
        _techCost = 60000,
        techLevel = 33,
        iconPaths = new string[]{ iconSpace },
        prereqTechs = new HexTechId[]{ HexTechId.SpaceFlight },
      }
    },

    {
      HexTechId.Colony2,
      new HexTech(){
        name = "Colony Level 2",
        descrip = "Unlocks Level 2 Colonies",
        id = HexTechId.Colony2,
        techLevel = 34,
        iconPaths = new string[]{ iconSpace },
        prereqTechs = new HexTechId[]{ HexTechId.SkipToTheFuture },
      }
    },
    {
      HexTechId.GalacticIndustry2,
      new HexTech(){
        name = "Galactic Industry 2",
        descrip = "Unlocks Level 2 Galactic Miners and Factory",
        id = HexTechId.GalacticIndustry2,
        techLevel = 34,
        iconPaths = new string[]{ iconSpace },
        prereqTechs = new HexTechId[]{ HexTechId.SkipToTheFuture },
      }
    },
    {
      HexTechId.GalacticMarkets2,
      new HexTech(){
        name = "Galactic Markets 2",
        descrip = "Unlocks Level 2 Galactic Marketplace",
        id = HexTechId.GalacticMarkets2,
        techLevel = 34,
        iconPaths = new string[]{ iconSpace },
        prereqTechs = new HexTechId[]{ HexTechId.SkipToTheFuture },
      }
    },

    {
      HexTechId.Colony3,
      new HexTech(){
        name = "Colony Level 3",
        descrip = "Unlocks Level 3 Colonies",
        id = HexTechId.Colony3,
        techLevel = 35,
        iconPaths = new string[]{ iconSpace },
        prereqTechs = new HexTechId[]{ HexTechId.Colony2 },
      }
    },
    {
      HexTechId.GalacticIndustry3,
      new HexTech(){
        name = "Galactic Industry 3",
        descrip = "Unlocks Level 3 Galactic Miners and Factory",
        id = HexTechId.GalacticIndustry3,
        techLevel = 35,
        iconPaths = new string[]{ iconSpace },
        prereqTechs = new HexTechId[]{ HexTechId.GalacticIndustry2 },
      }
    },
    {
      HexTechId.GalacticMarkets3,
      new HexTech(){
        name = "Galactic Markets 3",
        descrip = "Unlocks Level 3 Galactic Marketplace",
        id = HexTechId.GalacticMarkets3,
        techLevel = 35,
        iconPaths = new string[]{ iconSpace },
        prereqTechs = new HexTechId[]{ HexTechId.GalacticMarkets2 },
      }
    },

    {
      HexTechId.Colony4,
      new HexTech(){
        name = "Colony Level 4",
        descrip = "Unlocks Level 4 Colonies",
        id = HexTechId.Colony4,
        techLevel = 36,
        iconPaths = new string[]{ iconSpace },
        prereqTechs = new HexTechId[]{ HexTechId.Colony3 },
      }
    },
    {
      HexTechId.GalacticIndustry4,
      new HexTech(){
        name = "Galactic Industry 4",
        descrip = "Unlocks Level 4 Galactic Miners and Factory",
        id = HexTechId.GalacticIndustry4,
        techLevel = 36,
        iconPaths = new string[]{ iconSpace },
        prereqTechs = new HexTechId[]{ HexTechId.GalacticIndustry3 },
      }
    },
    {
      HexTechId.GalacticMarkets4,
      new HexTech(){
        name = "Galactic Markets 4",
        descrip = "Unlocks Level 4 Galactic Marketplace",
        id = HexTechId.GalacticMarkets4,
        techLevel = 36,
        iconPaths = new string[]{ iconSpace },
        prereqTechs = new HexTechId[]{ HexTechId.GalacticMarkets3 },
      }
    },

    {
      HexTechId.Colony5,
      new HexTech(){
        name = "Colony Level 5",
        descrip = "Unlocks Level 5 Colonies",
        id = HexTechId.Colony5,
        techLevel = 37,
        iconPaths = new string[]{ iconSpace },
        prereqTechs = new HexTechId[]{ HexTechId.Colony4 },
      }
    },
    {
      HexTechId.GalacticIndustry5,
      new HexTech(){
        name = "Galactic Industry 5",
        descrip = "Unlocks Level 5 Galactic Miners and Factory",
        id = HexTechId.GalacticIndustry5,
        techLevel = 37,
        iconPaths = new string[]{ iconSpace },
        prereqTechs = new HexTechId[]{ HexTechId.GalacticIndustry4 },
      }
    },
    {
      HexTechId.GalacticMarkets5,
      new HexTech(){
        name = "Galactic Markets 5",
        descrip = "Unlocks Level 5 Galactic Marketplace",
        id = HexTechId.GalacticMarkets5,
        techLevel = 37,
        iconPaths = new string[]{ iconSpace },
        prereqTechs = new HexTechId[]{ HexTechId.GalacticMarkets4 },
      }
    },

  };

  public static HexTechIdComparer techIdComparer = new HexTechIdComparer();
}

public enum HexTechId {
  None,
  Pottery,
  Agriculture,
  AnimalDomestication,
  FishingBoats,
  Sailing,
  Wheel,
  Irrigation,
  CropRotation,
  Woodworking,
  Mining,
  Writing,
  BronzeSmelting,
  Calendar,
  HorsebackRiding,
  Mathematics,
  Astronomy,
  Fermentation,
  LegalCodes,
  Construction,
  DirtRoads,

  IronSmelting,
  MetalCasting,
  Plowing,
  Masonry,
  Philosophy,
  WeavingLoom,
  Concrete,
  PublicBaths,

  SimpleMachines,
  Watermill,
  Paper,
  Architecture,
  NumeralSystem,
  Algebra,
  Compass,
  Coffee,
  PrintingPress,
  SelectiveBreeding,
  Physics,
  Optics,
  Chemistry,

  SteamPower,
  MachineTools,
  TextileMachinery,
  MassProducedIron,
  MechanicalThresher,
  MassProducedPaper,
  // GasLighting,
  Biology,
  StoneRoads,

  SteelSmelting,
  Photography,
  Railways,
  Electricity,
  OilRefining,
  // RubberVulcanization,
  Fertilizer,
  Telegraph,
  SewerSystem,
  AdvancedChemistry,

  Microbiology,
  Electrification,
  WaterTreatment,
  Radio,
  Automobile,
  Electronics,
  MechanizedAgriculture,
  // Highways,
  Plastics,
  Refrigeration,
  Flight,
  // ReplacableParts,
  // AbstractMathematics,

  Relativity,
  AtomicAge,
  Computers,
  Antibiotics,
  SpecializedMedicine,
  // Radar,
  Lasers,
  Rocketry,
  Genetics,
  AdvancedFertilizers,

  SpaceFlight,
  Robotics,
  Recycling,
  Internet,
  SolarPower,
  Satellites,
  DNASequencing,
  Miniaturization,
  CellPhones,

  //TODO below
  SkipToTheFuture,

  //Colony ships and settlement levels
  Colony2,
  Colony3,
  Colony4,
  Colony5,

  //Markets and transport levels
  GalacticMarkets,
  GalacticMarkets2,
  GalacticMarkets3,
  GalacticMarkets4,
  GalacticMarkets5,
  GalacticMarkets6,
  GalacticMarkets7,

  //Factory and mining
  GalacticIndustry, //May not actually use this one with innate first levels of factory & miners
  GalacticIndustry2,
  GalacticIndustry3,
  GalacticIndustry4,
  GalacticIndustry5,

  InterstellarTravel,

  PoliticalScience
}
[System.Serializable]
public class HexTechIdComparer : IEqualityComparer<HexTechId>
{
  public bool Equals(HexTechId a, HexTechId b){ return a == b; }
  public int GetHashCode(HexTechId a){ return (int)a; }
}