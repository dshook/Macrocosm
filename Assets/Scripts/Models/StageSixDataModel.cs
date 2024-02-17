using System;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
[Singleton]
public class StageSixDataModel
{
  public int mapSeed {get; set;}
  public List<HexCityData> cities = new List<HexCityData>();

  public int mapGeneratorVersion = 1;
  public HexCell[] gridCells;

  [Obsolete("All data is stored in gridCells now")]
  public List<HexCellData> cellData = new List<HexCellData>();
  public List<HexScoutData> scoutData = new List<HexScoutData>();
  public List<HexSettlerData> settlerData = new List<HexSettlerData>();

  public Dictionary<HexTechId, bool> techs = new Dictionary<HexTechId, bool>(HexTech.techIdComparer);
  public List<TechBuildingData> techQueue = new List<TechBuildingData>();

  public Dictionary<HexBonusResource, bool> bonusResourceRevealed =
     new Dictionary<HexBonusResource, bool>(HexCell.bonusResourceComparer){
        {HexBonusResource.Livestock, true},
        {HexBonusResource.Fish, true},
        {HexBonusResource.Horses, true},
        {HexBonusResource.Reef, true},
     };

  public bool ResearchedTech(HexTechId id){
    return techs.ContainsKey(id) && techs[id];
  }
  public bool RevealedResource(HexBonusResource resource){
    return bonusResourceRevealed.ContainsKey(resource) && bonusResourceRevealed[resource];
  }
  public bool InQueue(HexTechId id){
    return techQueue.Any(tq => tq.techId == id);
  }

  //global science bonuses
  public float foodRateBonus = 0f; //applies to per sec rate
  public float productionRateBonus = 0f;
  public float scienceRateBonus = 0f;

  public float tradeBonus = 0f;

  public int healthBonus = 0;
  public int happinessBonus = 0;

  public int settlerRadiusBonus = 0;
  public int scoutRadiusBonus = 0;

  public int pollutionLevel = 0;
}

[System.Serializable]
public class TechBuildingData{
  public HexTechId techId;
  public float progress = 0f;
  public bool finished = false;
}
