using System;
using System.Collections.Generic;

[System.Serializable]
[Singleton]
public class StageSevenDataModel
{
  public int mapSeed {get; set;}

  public float year {get; set;}

  public float timeRate {get; set;}

  public GalaxyViewMode viewMode = GalaxyViewMode.Galaxy;

  //from parent celestial id to settlement data
  public Dictionary<uint, GalaxySettlementData> settlements = new Dictionary<uint, GalaxySettlementData>();

  //from star id to star settlement data
  public Dictionary<uint, StarSettlementData> starSettlements = new Dictionary<uint, StarSettlementData>();

  //from star id to star data
  public Dictionary<uint, StarData> starData = new Dictionary<uint, StarData>();

  public HashSet<GalaxyShipData> ships = new HashSet<GalaxyShipData>();

  //Route id 0 is invalid route, id's start at 1

  //outer is route id
  //inner dict is bi-directional, from starId -> set of all the other stars it's connected to
  public  Dictionary<uint, Dictionary<uint, HashSet<uint>>> routeConnections
    = new Dictionary<uint, Dictionary<uint, HashSet<uint>>>();

  //The flip side view of the data above, from starId -> set of all routes that are connected
  public Dictionary<uint, HashSet<uint>> starConnections = new Dictionary<uint, HashSet<uint>>();

  //What resource each route is carrying
  public Dictionary<uint, GameResourceType> routeResources = new Dictionary<uint, GameResourceType>();

  //Global maximum building counts.  Set up for transports.  Only applies if the key exists
  public Dictionary<GalaxyBuildingId, uint> buildingLimits = new Dictionary<GalaxyBuildingId, uint>();

  public bool achievedVictory = false;
}