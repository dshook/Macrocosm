using UnityEngine;

using System.Collections.Generic;
using System;

[Serializable]
public class HexCell {
  public int Index;

  public HexCoordinates coordinates;
  public HexCellData data;
  public HexClimateData climateData;

  [NonSerialized]
  public HexCity city; //for cells with cities on them duh

  public CityBuildingId? building; //any building from a city that's on this tile

  [NonSerialized]
  public HexCity influincingCity; //for cells within cities borders

  [NonSerialized]
  HexCell[] neighbors = new HexCell[6];

  [NonSerialized]
  public HexCellDisplay display;

  [NonSerialized]
  public Vector3 localPosition;

  //Cell attributes
  [SerializeField]
  int elevation;

  public int Elevation {
    get { return elevation; }
    set { elevation = value; }
  }

  [SerializeField]
  HexTerrainType terrainType;
  public HexTerrainType TerrainType{
    get {return terrainType; }
    set { terrainType = value; }
  }

  [SerializeField]
  HexFeature hexFeature;
  public HexFeature HexFeature{
    get {return hexFeature; }
    set { hexFeature = value; }
  }

  [SerializeField]
  HexBonusResource hexBonusResource;
  public HexBonusResource HexBonusResource{
    get {return hexBonusResource; }
    set { hexBonusResource = value; }
  }

  [SerializeField]
  bool freshwater; //For any of the water terrain types
  public bool Freshwater{
    get {return freshwater; }
    set { freshwater = value; }
  }

  public HexExploreStatus ExploreStatus{
    get { return data.exploreStatus; }
    set { data.exploreStatus = value; }
  }

  [NonSerialized]
  bool selected = false;
  [NonSerialized]
  bool selectable = false;

  public bool Selected { get{ return selected; }}
  public bool Selectable { get{ return selectable; }}

  //river stuff
  bool hasIncomingRiver, hasOutgoingRiver, isRiverOrigin;
  HexDirection incomingRiver, outgoingRiver;
  public bool HasIncomingRiver { get { return hasIncomingRiver; } }
  public bool HasOutgoingRiver { get { return hasOutgoingRiver; } }
  public bool IsRiverOrigin { get { return isRiverOrigin; } set{ isRiverOrigin = value; } }
  public bool HasRiver { get { return hasIncomingRiver || hasOutgoingRiver; } }
  public bool HasRiverBeginOrEnd { get { return hasIncomingRiver != hasOutgoingRiver; } }
  public HexDirection RiverBeginOrEndDirection { get { return hasIncomingRiver ? incomingRiver : outgoingRiver; } }
  public HexCell NextRiverCell { get { return hasOutgoingRiver ? GetNeighbor(outgoingRiver) : null; } }
  [NonSerialized]
  public Vector2 RiverAnchorPoint = Vector2.zero;

  [NonSerialized]
  public List<CityBuildingId> connectionBuildings = null;
  public void AddConnection(CityBuildingId buildingId){
    if(connectionBuildings == null){
      connectionBuildings = new List<CityBuildingId>();
    }
    connectionBuildings.Add(buildingId);
  }
  public bool HasConnection(CityBuildingId buildingId){
    return connectionBuildings != null && connectionBuildings.Count > 0 && connectionBuildings.Contains(buildingId);
  }
  public int? LowestConnectionTier(){
    if(connectionBuildings == null){ return null; }
    int? connectionTier = null;
    foreach(var cb in connectionBuildings){
      if( HexRiverOrRoad.connectionTier.ContainsKey(cb) ){
        if(connectionTier == null){
          connectionTier = HexRiverOrRoad.connectionTier[cb];
        }else{
          connectionTier = Mathf.Min(connectionTier.Value, HexRiverOrRoad.connectionTier[cb]);
        }
      }
    }

    return connectionTier;
  }

  [NonSerialized]
  public Vector2 RoadAnchorPoint = Vector2.zero;


  //helper getters
  public bool IsUnderwater{
    get{ return elevation <= 0; }
  }

  public int ViewElevation {
    get { return elevation > 0 ? elevation : 0; }
  }

  public bool IsSettleable {
    get{
      return !IsUnderwater &&
        HexFeature != HexFeature.Lake &&
        HexFeature != HexFeature.Peak &&
        city == null;
    }
  }

  public const int BaseMoveCost = 10;
  public const int AvoidMoveCost = 9999;

  public int MoveCost(HexGrid.PathfindOptions options) {
    var moveCost = BaseMoveCost;
    if(hexFeature == HexFeature.Peak){
      return -1;
    }
    if(hexFeature == HexFeature.Mountains){
      moveCost += 3;
    }
    if(hexFeature == HexFeature.Hills){
      moveCost += 2;
    }
    if(hexFeature == HexFeature.TreesMedium){
      moveCost += 2;
    }
    if(hexFeature == HexFeature.TreesDense){
      moveCost += 3;
    }
    if(terrainType == HexTerrainType.Ice){
      moveCost += 5;
    }

    if(HasRiver){
      moveCost -= 2;
    }
    var bestConnectionBonus = 0;
    if(connectionBuildings != null && connectionBuildings.Count > 0){
      foreach(var cb in connectionBuildings){
        if(HexRiverOrRoad.moveCostBonus.ContainsKey(cb)){
          bestConnectionBonus = Mathf.Max(bestConnectionBonus, HexRiverOrRoad.moveCostBonus[cb]);
        }
      }
    }
    moveCost -= bestConnectionBonus;

    if(city != null){
      moveCost -= 2;
    }

    if(IsUnderwater){
      if(options.canMoveOnWater){
        moveCost += 4;
        if(options.avoidWater){
          moveCost = AvoidMoveCost;
        }
      }else{
        return -1;
      }
    }else{
      if(!options.canMoveOnLand){
        return -1;
      }
      if(options.avoidLand){
        moveCost = AvoidMoveCost;
      }
    }

    return moveCost;
  }

  public int Food(StageSixDataModel stageSixData) {
    var food = foodBaseAmts[terrainType];

    //City buildings
    if(city != null){
      food += 1;

      //TODO: should this sort of building live on the cell to speed up the contains check?
      if(city.data.HasBuilding(CityBuildingId.PrimativeHousing)){
        food += 1;
      }
      if(city.data.HasBuilding(CityBuildingId.BasicHousing)){
        food += 3;
      }
      if(city.data.HasBuilding(CityBuildingId.TraditionalHousing)){
        food += 5;
      }

      if(city.data.HasBuilding(CityBuildingId.CombineHarvesters) && building.HasValue && building.Value == CityBuildingId.Farm){
        food += 1;
      }
    }

    //Feature adjustments
    if(hexFeature == HexFeature.Mountains || hexFeature == HexFeature.Peak){
      food = 0;
    }
    if(hexFeature == HexFeature.Hills){
      food -= 1;
    }

    if(HasRiver || Freshwater){
      food += 1;
    }

    //Building Adjustments
    if(building.HasValue){
      if(building.Value == CityBuildingId.Farm){
        if(stageSixData.ResearchedTech(HexTechId.Irrigation)){ food += 1; }
        if(hexBonusResource == HexBonusResource.Grains && stageSixData.RevealedResource(HexBonusResource.Grains)) {
          food += 1;
        }
        if(stageSixData.ResearchedTech(HexTechId.MechanicalThresher)){ food += 1; }
      }

      food += CityBuilding.allBuildings[building.Value].cellBonusFood;
    }

    //Bonus resource Adjustments
    if(ExploreStatus == HexExploreStatus.Explored){
      if(
        (hexBonusResource == HexBonusResource.Livestock && stageSixData.RevealedResource(HexBonusResource.Livestock)) ||
        (hexBonusResource == HexBonusResource.Grains && stageSixData.RevealedResource(HexBonusResource.Grains)) ||
        (hexBonusResource == HexBonusResource.Fish && stageSixData.RevealedResource(HexBonusResource.Fish)) ||
        (hexBonusResource == HexBonusResource.Reef && stageSixData.RevealedResource(HexBonusResource.Reef))
      ){
        food += 1;
      }
    }

    //Territory bonuses
    if(influincingCity != null){
      if(IsUnderwater && influincingCity.data.HasBuilding(CityBuildingId.Lighthouse) ){
        food += 1;
      }
    }

    return Mathf.Max(0, food);
  }

  public int Production(StageSixDataModel stageSixData) {
    var production = 0;

    if(city != null){
      production += 1;

      //TODO: should this sort of building live on the cell to speed up the contains check?
      if(city.data.HasBuilding(CityBuildingId.Workshop)){
        production += 1;
      }
      if(city.data.HasBuilding(CityBuildingId.SteamPoweredFactory)){
        production += 6;
      }
    }

    if(hexFeature == HexFeature.Mountains){
      production += 2;
    }

    if(hexFeature == HexFeature.Hills){
      production += 1;
    }

    if(hexFeature == HexFeature.TreesSparse || hexFeature == HexFeature.TreesMedium){
      production += 1;
    }
    if(hexFeature == HexFeature.TreesDense){
      production += 2;
    }

    if(building.HasValue){
      production += CityBuilding.allBuildings[building.Value].cellBonusProduction;
    }

    //Bonus resource Adjustments
    if(ExploreStatus == HexExploreStatus.Explored){
      if(
        (hexBonusResource == HexBonusResource.Stone && stageSixData.RevealedResource(HexBonusResource.Stone)) ||
        (hexBonusResource == HexBonusResource.Horses && stageSixData.RevealedResource(HexBonusResource.Horses))
      ){
        production += 1;
      }
    }

    //Territory bonuses
    if(influincingCity != null){
      if(IsUnderwater && influincingCity.data.HasBuilding(CityBuildingId.Port)){
        production += 1;
      }

    }

    //mine bonuses
    if(
      building.HasValue &&
      building.Value == CityBuildingId.Mine
    ){
      if(
        hexBonusResource == HexBonusResource.Gold &&
        stageSixData.RevealedResource(HexBonusResource.Gold)
      ){
        production += 2;
      }

      if(
        hexBonusResource == HexBonusResource.Aluminum &&
        stageSixData.RevealedResource(HexBonusResource.Aluminum)
      ){
        production += 4;
      }
    }

    if(
      building.HasValue &&
      building.Value == CityBuildingId.IronMine
    ){
      if(
        influincingCity != null &&
        influincingCity.data.HasBuilding(CityBuildingId.Blacksmith)
      ){
        production += 2;
      }

      if(stageSixData.ResearchedTech(HexTechId.MassProducedIron)){
        production += 2;
      }
    }

    return production;
  }

  public int Science(StageSixDataModel stageSixData) {
    var science = 0;

    if(city != null){
      science = city.data.scienceAmount;
    }

    if(building.HasValue){
      science += CityBuilding.allBuildings[building.Value].cellBonusScience;
    }

    return science;
  }

  public int Health(StageSixDataModel stageSixData) {
    var health = 0;

    if( hexBonusResource == HexBonusResource.Cotton && stageSixData.RevealedResource(HexBonusResource.Cotton)){
      health += 2;
    }

    if( hexBonusResource == HexBonusResource.Sugar && stageSixData.RevealedResource(HexBonusResource.Sugar)){
      health -= 1;
    }

    if( hexBonusResource == HexBonusResource.Salt && stageSixData.RevealedResource(HexBonusResource.Salt)){
      health += 1;
    }

    if(building.HasValue){
      health += CityBuilding.allBuildings[building.Value].cellBonusHealth;
    }

    return health;
  }

  public int Happiness(StageSixDataModel stageSixData) {
    var happiness = 0;

    if( hexBonusResource == HexBonusResource.Sugar && stageSixData.RevealedResource(HexBonusResource.Sugar)){
      happiness += 3;
    }
    if( hexBonusResource == HexBonusResource.Salt && stageSixData.RevealedResource(HexBonusResource.Salt)){
      happiness += 1;
    }

    if( hexBonusResource == HexBonusResource.Reef && stageSixData.RevealedResource(HexBonusResource.Reef)){
      happiness += 2;
    }

    if( hexBonusResource == HexBonusResource.Uranium && stageSixData.RevealedResource(HexBonusResource.Uranium)){
      happiness -= 5;
    }

    if(building.HasValue){
      happiness += CityBuilding.allBuildings[building.Value].cellBonusHappiness;
    }

    return happiness;
  }

  public HexCell GetNeighbor (HexDirection direction) {
    return neighbors[(int)direction];
  }

  public void SetNeighbor (HexDirection direction, HexCell cell) {
    if(neighbors == null){
      neighbors = new HexCell[6];
    }
    if(cell.neighbors == null){
      cell.neighbors = new HexCell[6];
    }

    neighbors[(int)direction] = cell;
    cell.neighbors[(int)direction.Opposite()] = this;
  }


  //Show as a possible scouting location
  public void SetSelectable(bool isSelectable){
    if(selectable != isSelectable){
      selectable = isSelectable;
      if(display != null){
        display.UpdateTerrainDisplay();
      }
    }
  }

  //Sets the explore status and makes sure we only explore more and not less
  public void Explore(HexExploreStatus newStatus){
    if(data.exploreStatus == HexExploreStatus.Explored){
      return;
    }
    if(data.exploreStatus == HexExploreStatus.Partial && newStatus == HexExploreStatus.Unexplored){
      return;
    }
    if(data.exploreStatus == newStatus){
      return;
    }

    ExploreStatus = newStatus;
    if(display != null){
      display.ExploreUpdate();
    }
  }

  //Highlight for selecting cities
  public void Select(bool isSelected){
    selected = isSelected;
    if(display != null){
      display.UpdateTerrainDisplay();
    }
  }

  //more river junk
  public void SetOutgoingRiver (HexDirection direction) {
    if (hasOutgoingRiver && outgoingRiver == direction) {
      return;
    }

    HexCell neighbor = GetNeighbor(direction);
    if (!IsValidRiverDestination(neighbor)) {
      return;
    }

    RemoveOutgoingRiver();
    if (hasIncomingRiver && incomingRiver == direction) {
      RemoveIncomingRiver();
    }
    hasOutgoingRiver = true;
    outgoingRiver = direction;

    neighbor.RemoveIncomingRiver();
    neighbor.hasIncomingRiver = true;
    neighbor.incomingRiver = direction.Opposite();
    neighbor.isRiverOrigin = false;
  }

  bool IsValidRiverDestination (HexCell neighbor) {
    return neighbor != null && (
      elevation >= neighbor.elevation || neighbor.elevation == 0
    );
  }

  public void RemoveIncomingRiver () {
    if (!hasIncomingRiver) {
      return;
    }
    hasIncomingRiver = false;

    HexCell neighbor = GetNeighbor(incomingRiver);
    neighbor.hasOutgoingRiver = false;
  }

  public void RemoveOutgoingRiver () {
    if (!hasOutgoingRiver) {
      return;
    }
    hasOutgoingRiver = false;

    HexCell neighbor = GetNeighbor(outgoingRiver);
    neighbor.hasIncomingRiver = false;
  }

  public void RemoveRiver () {
    RemoveOutgoingRiver();
    RemoveIncomingRiver();
  }

  //pathfinding and climate stuff
  [NonSerialized]
  public int SearchHeuristic;
  [NonSerialized]
  public int SearchPhase;
  [NonSerialized]
  public int SearchTileDistance;

  [NonSerialized]
  public HexCell PathFrom;

  [NonSerialized]
  int distance;
  public int Distance {
    get { return distance; }
    set { distance = value; }
  }

  public int SearchPriority {
    get { return distance + SearchHeuristic; }
  }
  [NonSerialized]
  public HexCell NextWithSamePriority;

  //LUT's
  public static Dictionary<HexTerrainType, int> foodBaseAmts = new Dictionary<HexTerrainType, int>(terrainComparer) {
    {HexTerrainType.Sand, 0},
    {HexTerrainType.Grass, 1},
    {HexTerrainType.Mud, 1},
    {HexTerrainType.Stone, 0},
    {HexTerrainType.Snow, 0},
    {HexTerrainType.Ice, 0},
    {HexTerrainType.Water, 0},
    {HexTerrainType.Shallows, 1},
  };

  public static HexTerrainTypeComparer terrainComparer = new HexTerrainTypeComparer();
  public static HexFeatureComparer featureComparer = new HexFeatureComparer();
  public static HexBonusResourceComparer bonusResourceComparer = new HexBonusResourceComparer();
}

//All the cell data that needs to be persisted
[Serializable]
public class HexCellData {
  public HexCoordinates coordinates {get; set;}
  public HexExploreStatus exploreStatus {get; set;}
}

public enum HexTerrainType {
  Sand,
  Grass,
  Mud,
  Stone,
  Snow,
  Ice,
  Water,
  Shallows,
}
[System.Serializable]
public class HexTerrainTypeComparer : IEqualityComparer<HexTerrainType>
{
  public bool Equals(HexTerrainType a, HexTerrainType b){ return a == b; }
  public int GetHashCode(HexTerrainType a){ return (int)a; }
}


public enum HexFeature{
  None,
  Mountains,
  Hills,
  TreesDense,
  TreesMedium,
  TreesSparse,
  Lake,
  Peak,
}
[System.Serializable]
public class HexFeatureComparer : IEqualityComparer<HexFeature>
{
  public bool Equals(HexFeature a, HexFeature b){ return a == b; }
  public int GetHashCode(HexFeature a){ return (int)a; }
}

public enum HexExploreStatus {
  Unexplored,
  Partial,
  Explored,
}

[System.Serializable]
public struct HexClimateData {
  public float clouds, moisture, temperature;

  public override string ToString(){
    return string.Format("Clouds: {0}, Moisture: {1}, Temp: {2}", clouds, moisture, temperature);
  }
}

public enum HexBonusResource{
  None,
  Livestock, //innate
  Grains, //agriculture
  Fish, //innate
  Stone, //mining
  Cotton, //crop rotation
  Iron, //iron smelting
  Coal, //Steam power
  Horses, //innate
  Sugar, //irrigation
  Salt, //pottery
  Grapes, //fermentation
  Gold, //chemistry
  Reef, //innate
  Oil, //oil drilling
  Aluminum, //Electrification
  Uranium //Atomic Age
}
[System.Serializable]
public class HexBonusResourceComparer : IEqualityComparer<HexBonusResource>
{
  public bool Equals(HexBonusResource a, HexBonusResource b){ return a == b; }
  public int GetHashCode(HexBonusResource a){ return (int)a; }
}