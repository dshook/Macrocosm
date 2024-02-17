using System;
using System.Collections.Generic;

[System.Serializable]
public class GameResource : ICloneable {
  public GameResourceType type {get; set;}
  public int amount {get; set;} //TODO: might need to expand this to a bigger size

  public static Dictionary<GameResourceType, string> resourceIconPaths = new Dictionary<GameResourceType, string>(gameResourceTypeComparer){
    {GameResourceType.Gold, "Art/stage5/coin"},
    {GameResourceType.Wood, "Art/wood"},
    {GameResourceType.Ore, "Art/stage5/tribe_ore"},

    {GameResourceType.Food, "Art/stage6/icons/food_icon"},
    {GameResourceType.Production, "Art/stage6/icons/production_icon"},
    {GameResourceType.Science, "Art/stage6/icons/science_icon"},
    {GameResourceType.Time, "Art/stage6/icons/time_icon"},

    {GameResourceType.Energy, "Art/stage7/energy_icon"},
    {GameResourceType.Population, "Art/stage7/population_icon"},
    {GameResourceType.Iron, "Art/stage7/resources/iron_icon"},
    {GameResourceType.Silicon, "Art/stage7/resources/silicon_icon"},
    {GameResourceType.Phosphorus, "Art/stage7/resources/phosphorus_icon"},
    {GameResourceType.Sodium, "Art/stage7/resources/sodium_icon"},
    {GameResourceType.Titanium, "Art/stage7/resources/titanium_icon"},
    {GameResourceType.Xenon, "Art/stage7/resources/xenon_icon"},
    {GameResourceType.Promethium, "Art/stage7/resources/promethium_icon"},

    {GameResourceType.IronSilicon, "Art/stage7/resources/ironsilicon_icon"},
    {GameResourceType.IronPhosporus, "Art/stage7/resources/ironphosphorus_icon"},
    {GameResourceType.IronSodium, "Art/stage7/resources/ironsodium_icon"},
    {GameResourceType.IronTitanium, "Art/stage7/resources/irontitanium_icon"},
    {GameResourceType.IronXenon, "Art/stage7/resources/ironxenon_icon"},
    {GameResourceType.IronPromethium, "Art/stage7/resources/ironpromethium_icon"},
    {GameResourceType.SiliconPhosphorus, "Art/stage7/resources/siliconphosphorus_icon"},
    {GameResourceType.SiliconSodium, "Art/stage7/resources/siliconsodium_icon"},
    {GameResourceType.SiliconTitanium, "Art/stage7/resources/silicontitanium_icon"},
    {GameResourceType.SiliconXenon, "Art/stage7/resources/siliconxenon_icon"},
    {GameResourceType.SiliconPromethium, "Art/stage7/resources/siliconpromethium_icon"},
    {GameResourceType.PhosphorusSodium, "Art/stage7/resources/phosphorussodium_icon"},
    {GameResourceType.PhosphorusTitanium, "Art/stage7/resources/phosphorustitanium_icon"},
    {GameResourceType.PhosphorusXenon, "Art/stage7/resources/phosphorusxenon_icon"},
    {GameResourceType.PhosphorusPromethium, "Art/stage7/resources/phosphoruspromethium_icon"},
    {GameResourceType.SodiumTitanium, "Art/stage7/resources/sodiumtitanium_icon"},
    {GameResourceType.SodiumXenon, "Art/stage7/resources/sodiumxenon_icon"},
    {GameResourceType.SodiumPromethium, "Art/stage7/resources/sodiumpromethium_icon"},
    {GameResourceType.TitaniumXenon, "Art/stage7/resources/titaniumxenon_icon"},
    {GameResourceType.TitaniumPromethium, "Art/stage7/resources/titaniumpromethium_icon"},
    {GameResourceType.XenonPromethium, "Art/stage7/resources/xenonpromethium_icon"},
  };

  public static GameResourceTypeComparer gameResourceTypeComparer = new GameResourceTypeComparer();

  public object Clone()
  {
    return this.MemberwiseClone();
  }
}

[System.Serializable]
public enum GameResourceType {
  //Stage 5
  Gold,
  Wood,
  Ore,

  //Stage 6
  Food, //Also stage 7
  Production,
  Science,
  Time,

  //Stage 7, make sure to update in galaxy resource
  Energy,
  Population,
  //tier 1
  Iron,
  Silicon,
  Phosphorus,
  Sodium,
  Titanium,
  Xenon,
  Promethium,

  //tier 2
  IronSilicon,
  IronPhosporus,
  IronSodium,
  IronTitanium,
  IronXenon,
  IronPromethium,
  SiliconPhosphorus,
  SiliconSodium,
  SiliconTitanium,
  SiliconXenon,
  SiliconPromethium,
  PhosphorusSodium,
  PhosphorusTitanium,
  PhosphorusXenon,
  PhosphorusPromethium,
  SodiumTitanium,
  SodiumXenon,
  SodiumPromethium,
  TitaniumXenon,
  TitaniumPromethium,
  XenonPromethium,

  None
}

[System.Serializable]
public class GameResourceTypeComparer : IEqualityComparer<GameResourceType>
{
  public bool Equals(GameResourceType a, GameResourceType b){ return a == b; }
  public int GetHashCode(GameResourceType a){ return (int)a; }
}