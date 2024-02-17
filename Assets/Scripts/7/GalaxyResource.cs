using System.Collections.Generic;
using PygmyMonkey.ColorPalette;
using UnityEngine;

[System.Serializable]
public class GalaxyResource : GameResource {
  public bool importing {get; set;}
  public bool exporting {get; set;}

  //For depletable resources, how much is there total to mine
  public int? totalAmount;

  public bool isExportingResource(Dictionary<GameResourceType, GameResource> resourceDeltas ){
    var resourceDelta = resourceDeltas.TryGet(type);

    return (
      exporting
      && amount > 0
      && resourceDelta != null
      && resourceDelta.amount > 0
    );
  }

  static public bool canExportResource(GameResourceType type){
    if(type == GameResourceType.Energy || type == GameResourceType.Population){
      return false;
    }
    if(EfficiencyResourceTypes.Contains(type)){
      return false;
    }
    if(UnusedResourceTypes.Contains(type)){
      return false;
    }

    return true;

  }


  public static GameResourceType[] GalaxyResourceTypes = new GameResourceType[]{
    // GameResourceType.Population,
    // GameResourceType.Food,

    GameResourceType.Iron,
    GameResourceType.Silicon,
    GameResourceType.Phosphorus,
    GameResourceType.Sodium,
    GameResourceType.Titanium,
    GameResourceType.Xenon,
    GameResourceType.Promethium,

    GameResourceType.IronPhosporus,
    GameResourceType.IronSodium,
    GameResourceType.IronTitanium,
    GameResourceType.IronXenon,
    GameResourceType.IronPromethium,
    GameResourceType.IronSilicon,
    GameResourceType.SiliconPhosphorus,
    GameResourceType.SiliconSodium,
    GameResourceType.SiliconTitanium,
    GameResourceType.SiliconXenon,
    GameResourceType.SiliconPromethium,
    GameResourceType.PhosphorusSodium,
    GameResourceType.PhosphorusTitanium,
    GameResourceType.PhosphorusXenon,
    GameResourceType.PhosphorusPromethium,
    GameResourceType.SodiumTitanium,
    GameResourceType.SodiumXenon,
    GameResourceType.SodiumPromethium,
    GameResourceType.TitaniumXenon,
    GameResourceType.TitaniumPromethium,
    GameResourceType.XenonPromethium
  };
  public static Dictionary<GameResourceType, string> GalaxyResourceAbbr = new Dictionary<GameResourceType, string>(GameResource.gameResourceTypeComparer){
    { GameResourceType.Iron,       "Fe"},
    { GameResourceType.Silicon,    "Si"},
    { GameResourceType.Phosphorus, "P"},
    { GameResourceType.Sodium,     "Na"},
    { GameResourceType.Titanium,   "Ti"},
    { GameResourceType.Xenon,      "Xe"},
    { GameResourceType.Promethium, "Pm"},
  };

  public struct NameDescrip{
    public string name;
    public string descrip;
  }

  public static Dictionary<GameResourceType, string> GalaxyResourceNames = new Dictionary<GameResourceType, string>(){
    { GameResourceType.Iron,                "Iron"},
    { GameResourceType.Silicon,             "Silicon"},
    { GameResourceType.Phosphorus,          "Phosporus"},
    { GameResourceType.Sodium,              "Sodium"},
    { GameResourceType.Titanium,            "Titanium"},
    { GameResourceType.Xenon,               "Xenon"},
    { GameResourceType.Promethium,          "Promethium"},

    { GameResourceType.IronPhosporus,       "Greenhouse Modules"},
    { GameResourceType.IronSodium,          "Mining Reagents"},
    { GameResourceType.IronTitanium,        "Structural Components"},
    { GameResourceType.IronXenon,           "Thrusters"},
    { GameResourceType.IronPromethium,      "Reactors"},
    { GameResourceType.IronSilicon,         "Iron Research"},
    { GameResourceType.SiliconPhosphorus,   "Phosphorus Research"},
    { GameResourceType.SiliconSodium,       "Sodium Research"},
    { GameResourceType.SiliconTitanium,     "Titanium Research"},
    { GameResourceType.SiliconXenon,        "Xenon Research"},
    { GameResourceType.SiliconPromethium,   "Promethium Research"},
    { GameResourceType.PhosphorusSodium,    "Fertilizers"},
    { GameResourceType.PhosphorusTitanium,  "Food Labs"},
    { GameResourceType.PhosphorusXenon,     "Advanced Medicines"},
    { GameResourceType.PhosphorusPromethium,"Life Extension Fluid"},
    { GameResourceType.SodiumTitanium,      "Driller Bots"},
    { GameResourceType.SodiumXenon,         "Driller Fuel"},
    { GameResourceType.SodiumPromethium,    "Atomic Miners"},
    { GameResourceType.TitaniumXenon,       "Lightweight Thrusters"},
    { GameResourceType.TitaniumPromethium,  "Probability Reactor"},
    { GameResourceType.XenonPromethium,     "Xenogen"},
  };

  public static HashSet<GameResourceType> Tier1ResourceTypes = new HashSet<GameResourceType>{
    GameResourceType.Iron,
    GameResourceType.Silicon,
    GameResourceType.Phosphorus,
    GameResourceType.Sodium,
    GameResourceType.Titanium,
    GameResourceType.Xenon,
    GameResourceType.Promethium,
  };

  public static HashSet<GameResourceType> Tier2ResourceTypes = new HashSet<GameResourceType>{
    GameResourceType.IronSilicon,
    GameResourceType.IronPhosporus,
    GameResourceType.IronSodium,
    GameResourceType.IronTitanium,
    GameResourceType.IronXenon,
    GameResourceType.IronPromethium,
    GameResourceType.SiliconPhosphorus,
    GameResourceType.SiliconSodium,
    GameResourceType.SiliconTitanium,
    GameResourceType.SiliconXenon,
    GameResourceType.SiliconPromethium,
    GameResourceType.PhosphorusSodium,
    GameResourceType.PhosphorusTitanium,
    GameResourceType.PhosphorusXenon,
    GameResourceType.PhosphorusPromethium,
    GameResourceType.SodiumTitanium,
    GameResourceType.SodiumXenon,
    GameResourceType.SodiumPromethium,
    GameResourceType.TitaniumXenon,
    GameResourceType.TitaniumPromethium,
    GameResourceType.XenonPromethium
  };

  public static HashSet<GameResourceType> EfficiencyResourceTypes = new HashSet<GameResourceType>{
    GameResourceType.IronSilicon,
    GameResourceType.SiliconPhosphorus,
    GameResourceType.SiliconSodium,
    GameResourceType.SiliconTitanium,
    GameResourceType.SiliconXenon,
    GameResourceType.SiliconPromethium,
  };

  //Resources that you can't do anything with so prevent the player from making or trading them
  public static HashSet<GameResourceType> UnusedResourceTypes = new HashSet<GameResourceType>{
    GameResourceType.PhosphorusXenon,
    GameResourceType.PhosphorusPromethium,
    GameResourceType.SodiumXenon,
  };


  public class GalaxyDependency{
    public GameResourceType[] dependents;
    public float outputRatio;
  }

  public static Dictionary<GameResourceType, GalaxyDependency> resourceDependencies = new Dictionary<GameResourceType, GalaxyDependency>(GameResource.gameResourceTypeComparer){
    {GameResourceType.IronSilicon,          new GalaxyDependency(){ outputRatio = 0.90f, dependents = new GameResourceType[]{GameResourceType.Iron, GameResourceType.Silicon}}},
    {GameResourceType.IronPhosporus,        new GalaxyDependency(){ outputRatio = 0.85f, dependents = new GameResourceType[]{GameResourceType.Iron, GameResourceType.Phosphorus}}},
    {GameResourceType.IronSodium,           new GalaxyDependency(){ outputRatio = 0.80f, dependents = new GameResourceType[]{GameResourceType.Iron, GameResourceType.Sodium}}},
    {GameResourceType.IronTitanium,         new GalaxyDependency(){ outputRatio = 0.75f, dependents = new GameResourceType[]{GameResourceType.Iron, GameResourceType.Titanium}}},
    {GameResourceType.IronXenon,            new GalaxyDependency(){ outputRatio = 0.70f, dependents = new GameResourceType[]{GameResourceType.Iron, GameResourceType.Xenon}}},
    {GameResourceType.IronPromethium,       new GalaxyDependency(){ outputRatio = 0.65f, dependents = new GameResourceType[]{GameResourceType.Iron, GameResourceType.Promethium}}},
    {GameResourceType.SiliconPhosphorus,    new GalaxyDependency(){ outputRatio = 0.60f, dependents = new GameResourceType[]{GameResourceType.Silicon, GameResourceType.Phosphorus}}},
    {GameResourceType.SiliconSodium,        new GalaxyDependency(){ outputRatio = 0.55f, dependents = new GameResourceType[]{GameResourceType.Silicon, GameResourceType.Sodium}}},
    {GameResourceType.SiliconTitanium,      new GalaxyDependency(){ outputRatio = 0.50f, dependents = new GameResourceType[]{GameResourceType.Silicon, GameResourceType.Titanium}}},
    {GameResourceType.SiliconXenon,         new GalaxyDependency(){ outputRatio = 0.45f, dependents = new GameResourceType[]{GameResourceType.Silicon, GameResourceType.Xenon}}},
    {GameResourceType.SiliconPromethium,    new GalaxyDependency(){ outputRatio = 0.40f, dependents = new GameResourceType[]{GameResourceType.Silicon, GameResourceType.Promethium}}},
    {GameResourceType.PhosphorusSodium,     new GalaxyDependency(){ outputRatio = 0.35f, dependents = new GameResourceType[]{GameResourceType.Phosphorus, GameResourceType.Sodium}}},
    {GameResourceType.PhosphorusTitanium,   new GalaxyDependency(){ outputRatio = 0.30f, dependents = new GameResourceType[]{GameResourceType.Phosphorus, GameResourceType.Titanium}}},
    {GameResourceType.PhosphorusXenon,      new GalaxyDependency(){ outputRatio = 0.25f, dependents = new GameResourceType[]{GameResourceType.Phosphorus, GameResourceType.Xenon}}},
    {GameResourceType.PhosphorusPromethium, new GalaxyDependency(){ outputRatio = 0.20f, dependents = new GameResourceType[]{GameResourceType.Phosphorus, GameResourceType.Promethium}}},
    {GameResourceType.SodiumTitanium,       new GalaxyDependency(){ outputRatio = 0.15f, dependents = new GameResourceType[]{GameResourceType.Sodium, GameResourceType.Titanium}}},
    {GameResourceType.SodiumXenon,          new GalaxyDependency(){ outputRatio = 0.10f, dependents = new GameResourceType[]{GameResourceType.Sodium, GameResourceType.Xenon}}},
    {GameResourceType.SodiumPromethium,     new GalaxyDependency(){ outputRatio = 0.08f, dependents = new GameResourceType[]{GameResourceType.Sodium, GameResourceType.Promethium}}},
    {GameResourceType.TitaniumXenon,        new GalaxyDependency(){ outputRatio = 0.07f, dependents = new GameResourceType[]{GameResourceType.Titanium, GameResourceType.Xenon}}},
    {GameResourceType.TitaniumPromethium,   new GalaxyDependency(){ outputRatio = 0.06f, dependents = new GameResourceType[]{GameResourceType.Titanium, GameResourceType.Promethium}}},
    {GameResourceType.XenonPromethium,      new GalaxyDependency(){ outputRatio = 0.05f, dependents = new GameResourceType[]{GameResourceType.Xenon, GameResourceType.Promethium}}},
  };

  public struct GalaxyResourceGeneration {
    public Vector2 perlinOrigin;
    public float[] perlinOctaves;
    public float boost;
  }

  public struct ResourceProbability{
    public GameResourceType type;
    public float probability;
  }

  public static Dictionary<GameResourceType, GalaxyResourceGeneration> GalaxyResourceGen = new Dictionary<GameResourceType, GalaxyResourceGeneration>(GameResource.gameResourceTypeComparer){
    {GameResourceType.Iron, new GalaxyResourceGeneration(){
      perlinOrigin = Vector2.zero,
      perlinOctaves = new float[]{ 3, 5.5f },
      boost = 0.14f,
    }},
    {GameResourceType.Silicon, new GalaxyResourceGeneration(){
      perlinOrigin = new Vector2(1000, 1000),
      perlinOctaves = new float[]{ 5, 7 },
      boost = 0.10f,
    }},
    {GameResourceType.Phosphorus, new GalaxyResourceGeneration(){
      perlinOrigin = new Vector2(2000, 2000),
      perlinOctaves = new float[]{ 6, 9.5f },
      boost = 0.085f,
    }},
    {GameResourceType.Sodium, new GalaxyResourceGeneration(){
      perlinOrigin = new Vector2(3000, 3000),
      perlinOctaves = new float[]{ 8, 11 },
      boost = 0.075f,
    }},
    {GameResourceType.Titanium, new GalaxyResourceGeneration(){
      perlinOrigin = new Vector2(4000, 4000),
      perlinOctaves = new float[]{ 5, 15 },
      boost = 0.055f,
    }},
    {GameResourceType.Xenon, new GalaxyResourceGeneration(){
      perlinOrigin = new Vector2(5000, 5000),
      perlinOctaves = new float[]{ 15, 25 },
      boost = 0.04f,
    }},
    {GameResourceType.Promethium, new GalaxyResourceGeneration(){
      perlinOrigin = new Vector2(6000, 6000),
      perlinOctaves = new float[]{ 7, 14, 28, 35},
      boost = 0.02f,
    }},
  };

  public static Dictionary<ResourceAbundance, int> startingAmount = new Dictionary<ResourceAbundance, int>(){
    {ResourceAbundance.Worthless,  700},
    {ResourceAbundance.Scant,      7700},
    {ResourceAbundance.Poor,       77700},
    {ResourceAbundance.Average,    777700},
    {ResourceAbundance.Abundant,   7777700},
    {ResourceAbundance.Rich,       77777700},
    {ResourceAbundance.Motherlode, 777777700},
  };

  public static ResourceAbundance GetAbundance(int amount){
    if(amount <= 0){
      return ResourceAbundance.Depleted;
    }else{
      foreach(var startingAmount in GalaxyResource.startingAmount){
        if(amount <= startingAmount.Value ){
          return startingAmount.Key;
        }
      }
      return ResourceAbundance.Motherlode;
    }
  }

  public static Color GetAbundanceColor(ColorPalette resourcePalette, ResourceAbundance abundance){
    return resourcePalette.getColorAtIndex(colorPaletteIndex[abundance]);
  }

  public static Dictionary<ResourceAbundance, ushort> colorPaletteIndex = new Dictionary<ResourceAbundance, ushort>(resourceAbundanceTypeComparer){
    {ResourceAbundance.Depleted,   0},
    {ResourceAbundance.Worthless,  1},
    {ResourceAbundance.Scant,      2},
    {ResourceAbundance.Poor,       3},
    {ResourceAbundance.Average,    4},
    {ResourceAbundance.Abundant,   5},
    {ResourceAbundance.Rich,       6},
    {ResourceAbundance.Motherlode, 7},
  };

  //Convert from a silicon resource amount to the 0-1 pct bonus for that resource
  public static float GetResourceEfficiencyBonus(int amount){
    return Mathf.Max(0,
      (Mathf.Log(amount + 403, 1.04f)  / 90f) - 1.7f
    );
  }

  public static ResourceAbundanceTypeComparer resourceAbundanceTypeComparer = new ResourceAbundanceTypeComparer();

  public static Dictionary<ResourceAbundance, string> ResourceAbundanceNames = new Dictionary<ResourceAbundance, string>(){
    {ResourceAbundance.Depleted, "Depleted"},
    {ResourceAbundance.Worthless, "Worthless"},
    {ResourceAbundance.Scant, "Scant"},
    {ResourceAbundance.Poor, "Poor"},
    {ResourceAbundance.Average, "Average"},
    {ResourceAbundance.Abundant, "Abundant"},
    {ResourceAbundance.Rich, "Rich"},
    {ResourceAbundance.Motherlode, "Motherlode"},
  };

}

[System.Serializable]
public enum ResourceAbundance{
  Depleted = -99,
  Worthless = -3,
  Scant = -2,
  Poor = -1,
  Average = 0,
  Abundant = 1,
  Rich = 2,
  Motherlode = 3,
}

[System.Serializable]
public class ResourceAbundanceTypeComparer : IEqualityComparer<ResourceAbundance>
{
  public bool Equals(ResourceAbundance a, ResourceAbundance b){ return a == b; }
  public int GetHashCode(ResourceAbundance a){ return (int)a; }
}