using UnityEngine;

using TMPro;
using System.Collections.Generic;
using strange.extensions.mediation.impl;
using System.Linq;

[System.Serializable]
public class HexCityData{
  const float third = 1f/3f;

  public int population;
  public float popProgress; //0-1 pct to next level growth

  public HexCoordinates coordinates;

  public List<FinishedCityBuildingData> buildings = new List<FinishedCityBuildingData>();
  public List<CityBuildingData> buildQueue = new List<CityBuildingData>();

  public List<CityConnectionData> outgoingConnections = new List<CityConnectionData>();     //connections built by this city
  public List<CityConnectionData> incomingConnections = new List<CityConnectionData>(); //connections built by other cities

  public bool isScouting = false;

  //Triangle Controls
  public float growth = third;
  public float science = third;
  public float production = third;

  //Triangle Controls
  public float health = third;
  public float work = third;
  public float play = third;

  // The current updated sum of happiness/healthiness
  public int citizenHappiness = 0;
  public int citizenHealthiness = 0;

  public int areaOfInfluence = 1;

  //building bonuses
  public float foodRateBonus = 0f; //applies to per sec rate

  public int productionBonus = 0; //Deprecated
  public float productionRateBonus = 0f;

  public int scienceAmount = 6;
  public float scienceRateBonus = 0f;

  public int healthBonus = 0;
  public int happinessBonus = 0;

  public float tradeBonus = 0f;

  public bool HasBuilding(CityBuildingId buildingId){
    if(buildings != null){
      foreach(var building in buildings){
        if(building.buildingId == buildingId){ return true; }
      }
    }
    return false;
  }

  // Save the last calculated science per second value so the tech advancer/manager can pick it up and use while
  // Stage 6 isn't running
  public float calculatedSciencePerSecond = 0f;
}

[System.Serializable]
public class CityConnectionData{
  public HexCoordinates dest;
  public CityBuildingId connectionBuildingId;
}

[System.Serializable]
public class CityBuildingData{
  public CityBuildingId buildingId;
  public float progress = 0f;
  public bool finished = false;
  public int productionCost = 0;
  public HexCoordinates? location; //settlers/road/building dests
}

[System.Serializable]
public class FinishedCityBuildingData{
  public CityBuildingId buildingId;
  public HexCoordinates? location;
}

public class HexCity : View {
  [Inject] StageRulesService stageRules { get; set; }
  [Inject] StageFiveDataModel stageFiveData {get; set;}
  [Inject] HexBuildingFinishedSignal buildingFinished { get; set; }
  [Inject] HexTechFinishedSignal techFinishedSignal { get; set; }
  [Inject] HexBuildQueueChangeSignal buildQueueChangeSignal { get; set; }
  [Inject] HexCityBordersChangedSignal cityBordersChangedSignal {get; set; }
  [Inject] public StageSixDataModel stageSixData {get; set;} //public so buildings apply can access
  [Inject] public ResourceLoaderService loader {get; set;}
  [Inject] AudioService audioService {get; set;}

  public HexCityData data;
  public HexGrid grid {get; set;}

  public List<HexCell> influencedCells;

  public GameObject scoutFlag;
  public TextMeshPro popText;
  public FilledBar popProgress;
  public FilledBar buildProgress;
  public SpriteRenderer cityRenderer;

  public AudioClip cityAOIGrowsClip;

  public GameObject buildFlag;

  public bool selectingScout {get; set;}
  public bool selectingSettler {get; set;}

  public CityThresholdLevel healthThreshold;
  public CityThresholdLevel happinessThreshold;

  public bool hasScoutedThisSession = false;

  int prevPop = -1;
  int foodInCells = -1;
  int productionInCells = -1;
  int scienceInCells = -1;

  public int FoodInCells{ get{ return foodInCells;} }
  public int ProductionInCells{ get{ return productionInCells;} }
  public int ScienceInCells{ get{ return scienceInCells;} }

  public HexCoordinates coordinates {
    get{ return data.coordinates; }
  }

  protected override void Awake() {
    base.Awake();

    techFinishedSignal.AddListener(OnTechFinished);
  }

  public void Init(){
    CalculateHealthiness();
    CalculateHappiness();
    UpdateCityDisplay();
  }

  void Update(){
    if(foodInCells == -1){
      UpdateResourcesInCells();
    }

    scoutFlag.SetActive(!data.isScouting);
    buildFlag.SetActive(data.buildQueue.Count == 0);

    //not using string changer here because each city would need to be tracked with its own ID
    if(prevPop != data.population){
      prevPop = data.population;
      popText.text = data.population.ToString();
    }

    CalculateHealthiness();

    //"gather" science, to save the amount to the data for TechAdvancer to use
    data.calculatedSciencePerSecond = SciencePerSecond;

    //gather food
    var foodCollected = FoodPerSecond * Time.deltaTime;

    CalculateHappiness();

    UpdateBuildingProgress(foodCollected);

    data.popProgress += foodCollected / FoodNeededForGrowth;
    if(data.popProgress >= 1f){
      Grow();
    }else if(data.popProgress < 0){
      Shrink();
    }

    popProgress.fillAmt = data.popProgress;
    popProgress.color = FoodPerSecond > 0 ? Colors.green : Colors.red;

  }

  void UpdateBuildingProgress(float foodCollected){

    //build a building
    if(data.buildQueue.Count > 0){
      var building = data.buildQueue[0];
      var currentBuilding = CityBuilding.allBuildings[building.buildingId];

      if(building.progress < 1f){
        if(currentBuilding.secondsToBuild == null){
          var production = ProductionPerSecond * Time.deltaTime;
          building.progress += production / building.productionCost;
        }else{
          building.progress += Time.deltaTime / currentBuilding.secondsToBuild.Value;
        }

        buildProgress.gameObject.SetActive(true);
        buildProgress.fillAmt = building.progress;
      }
      if(building.progress >= 1f && !building.finished){
        BuildingFinished();
      }

      //update buildings in queue with population multipliers
      for(var i = 1; i < data.buildQueue.Count; i++){
        var queueBuilding = data.buildQueue[i];
        var buildingTemplate = CityBuilding.allBuildings[queueBuilding.buildingId];

        if(buildingTemplate.multiplyCostOnPopulation){
          queueBuilding.productionCost = buildingTemplate.ProductionCost(data.population, stageSixData);
        }
      }
    }else{
      buildProgress.gameObject.SetActive(false);
    }
  }

  public void Grow(){
    data.popProgress = 0;
    data.population++;

    var newAOI = CalcAreaOfInfluence();
    //Only allowing growing of AOI now to not deal with the problem of buildings built outside of AOI if it shrinks
    if(newAOI > data.areaOfInfluence){
      audioService.PlaySfx(cityAOIGrowsClip);
      data.areaOfInfluence = newAOI;
      UpdateCityDisplay();
      cityBordersChangedSignal.Dispatch(this);
      //make sure new city cells are explored
      foreach(var cell in influencedCells){
        cell.Explore(HexExploreStatus.Explored);
      }
    }

    UpdateResourcesInCells();
  }

  public void Shrink(){
    var populationBefore = data.population;
    var minSize = 1;
    data.population = Mathf.Max(data.population - 1, minSize);

    var actuallyShrunk = populationBefore != data.population;

    data.popProgress = actuallyShrunk ? 0.999f : 0f;

    if(!actuallyShrunk){
      return;
    }

    //Not allowing shrinking of AOI now, see Grow

    UpdateResourcesInCells();
  }

  void UpdateResourcesInCells(){
    foodInCells = 0;
    productionInCells = 0;
    scienceInCells = 0;
    if(influencedCells != null){
      foreach(var cell in influencedCells){
        foodInCells += cell.Food(stageSixData);
        productionInCells += cell.Production(stageSixData);
        scienceInCells += cell.Science(stageSixData);
      }
    }
  }

  void OnTechFinished(HexTechId id){
    UpdateResourcesInCells();
  }

  public void AddBuildingToQueue(CityBuildingId buildingId, int productionCost, HexCoordinates? location){
    data.buildQueue.Add(new CityBuildingData(){
      buildingId = buildingId,
      progress = 0f,
      finished = false,
      productionCost = productionCost,
      location = location,
    });
    buildQueueChangeSignal.Dispatch(this, false);
  }

  public void RemoveBuildingFromQueue(int index, bool buildingFinished = false){
    if(data.buildQueue.IsValidIndex(index)){
      data.buildQueue.RemoveAt(index);
      buildQueueChangeSignal.Dispatch(this, buildingFinished);
    }
  }

  void BuildingFinished(){
    var building = data.buildQueue[0];
    var currentBuilding = CityBuilding.allBuildings[building.buildingId];

    building.finished = true;

    if(currentBuilding.makesObsolete != null){
      foreach(var buildingMadeObsolete in currentBuilding.makesObsolete){
        var removingBuildingTemplate = CityBuilding.allBuildings[buildingMadeObsolete];
        var removingBuildingInstance = data.buildings.FirstOrDefault(b =>
          b.buildingId == buildingMadeObsolete &&
          (building.location.HasValue ? b.location == building.location : true)
        );
        if(removingBuildingTemplate.onRemove != null && removingBuildingInstance != null){
          removingBuildingTemplate.onRemove(this, removingBuildingInstance);
        }

        data.buildings.Remove(removingBuildingInstance);
      }
    }

    if(currentBuilding.onApply != null){
      currentBuilding.onApply(this, building);
    }
    data.buildings.Add(new FinishedCityBuildingData(){
      buildingId = building.buildingId,
      location = building.location
    });

    if(currentBuilding.onMap){
      AddBuildingToCell(grid.GetCell(building.location.Value), currentBuilding);
    }
    buildingFinished.Dispatch(this, building);

    //now pop from the queue and onto the next thing
    RemoveBuildingFromQueue(0, true);

    UpdateResourcesInCells();
  }

  public void AddBuildingToCell(HexCell cell, CityBuilding building){
    if(cell == null){
      Debug.LogWarning("Trying to add building to null cell which shouldn't happen");
      return;
    }
    if(cell.building.HasValue){
      //Remove prior building from cell before adding new one
      RemoveBuildingFromCell(cell);
    }

    cell.building = building.id;

    if(string.IsNullOrEmpty(building.mapImagePath)){
      Debug.LogWarning("Building missing map image: " + building.name);
      return;
    }

    //create on map
    if(cell.display != null){
      cell.display.AddBuildingDisplay(building.mapImagePath);
    }
  }

  public void RemoveBuildingFromCell(HexCell cell){
    if(cell == null){
      Debug.LogWarning("Trying to remove building from null cell which shouldn't happen");
      return;
    }
    if(cell.building == null){
      Debug.LogWarning("Trying to remove building from cell with no buildings which shouldn't happen");
      return;
    }

    cell.building = null;

    if(cell.display != null){
      cell.display.RemoveBuildingDisplay();
    }
  }

  public struct CityBuildingInfo{
    public CityBuilding building;
    public int? productionMultiplier;
    public HexCoordinates? destCoord;

    //Says if building on those coords would replace another building already there
    //For display only
    public CityBuildingId[] replacingBuildings;

    public CityBuildingId? madeObsoleteBy;

    public int quantity;
  }

  List<CityBuildingInfo> availableToBuild = new List<CityBuildingInfo>();
  public List<CityBuildingInfo> GetAvailableBuildings(){
    availableToBuild.Clear();

    foreach(var building in CityBuilding.allBuildings.Values){
      int? productionMultiplier = null;
      HexCoordinates? destCoord = null;
      CityBuildingId[] replacingBuildings = null;
      var quantity = 1;

      //Skip already built or building for non repeatable
      if(!building.repeatable){
        if(data.HasBuilding(building.id)){
          continue;
        }
        if(data.buildQueue.Any(bq => bq.buildingId == building.id)){
          continue;
        }
      }else if(building.maxQuantity.HasValue){

        var buildingCount = data.buildQueue.Where(bq => bq.buildingId == building.id).Count() +
          data.buildings.Where(ab => ab.buildingId == building.id).Count();

        var ableToBuild = building.maxQuantity.Value - buildingCount;

        if(ableToBuild <= 0){
          continue;
        }
        quantity = ableToBuild;
      }

      if(building.requiredCitySize > 0 && data.population < building.requiredCitySize){
        continue;
      }

      if(building.requiredTech != HexTechId.None && !stageSixData.ResearchedTech(building.requiredTech) ){
        continue;
      }

      if(building.madeObsoleteByTech != HexTechId.None && stageSixData.ResearchedTech(building.madeObsoleteByTech) ){
        continue;
      }

      if(building.prereqBuildings != null && !building.prereqBuildings.Any(pmod => data.buildings.Any(cm => cm.buildingId == pmod) ) ){
        continue;
      }

      if(building.excludeBuildings != null && building.excludeBuildings.Any(pmod => data.buildings.Any(cm => cm.buildingId == pmod) ) ){
        continue;
      }

      if(building.requiresOcean.HasValue && !building.onMap){
        var cityNextToOcean = false;

        foreach(var cell in influencedCells){
          if(cell.IsUnderwater && !cell.Freshwater){
            cityNextToOcean = true;
            break;
          }
        }

        if(building.requiresOcean.Value != cityNextToOcean){
          continue;
        }
      }

      //Find the buildings already built or building that this building would make obsolete
      if(building.makesObsolete != null){
        replacingBuildings = building.makesObsolete.Where(ob =>
          data.HasBuilding(ob) ||
          data.buildQueue.Any(bq => bq.buildingId == ob)
        ).ToArray();
      }

      //check to see if we've already made this building obsolete,
      //skip connections check here since that's handled separately
      if(
        !building.requiresCityConnection && (
          data.buildings.Any(ab =>
            CityBuilding.allBuildings[ab.buildingId].makesObsolete != null &&
            CityBuilding.allBuildings[ab.buildingId].makesObsolete.Contains(building.id)
          ) ||
          data.buildQueue.Any(bq =>
            CityBuilding.allBuildings[bq.buildingId].makesObsolete != null &&
            CityBuilding.allBuildings[bq.buildingId].makesObsolete.Contains(building.id)
          )
        )
      ){
        continue;
      }

      if(building.onMap){
        var buildingCells = GetCellsForBuilding(influencedCells, building);

        quantity = building.repeatable ?
          buildingCells.Count() :
          Mathf.Min(1, buildingCells.Count());
        if(quantity == 0){
          continue;
        }

        destCoord = buildingCells
          .Select(c => c.coordinates)
          .OrderBy(coord =>
            //Order all the coordinates by the cells that are empty first and not building on
            //So we don't have to replace a building unless really needed
            grid.GetCell(coord).building.HasValue ||
            data.buildQueue.Any(b => b.location.HasValue && b.location.Value == coord) ? 1 : 0
          ).FirstOrDefault();

        if(destCoord.HasValue){
          var destCell = grid.GetCell(destCoord.Value);
          replacingBuildings = destCell.building.HasValue ?
            new CityBuildingId[]{ destCell.building.Value } : null;
          if(replacingBuildings == null){
            var replacingInQueue = data.buildQueue.FirstOrDefault(b =>
              building.id != b.buildingId && b.location.HasValue && b.location.Value == destCoord.Value
            );
            replacingBuildings = replacingInQueue != null ? new CityBuildingId[]{ replacingInQueue.buildingId } : null;
          }
        }
      }

      if(building.requiresCityConnection){
        var roadInfo = GetRoadBuildingInfo(building.id);
        if(roadInfo.HasValue){
          productionMultiplier = roadInfo.Value.productionMultiplier;
          destCoord = roadInfo.Value.destCoord;
          quantity = roadInfo.Value.quantity;
        }else{
          continue;
        }
      }

      if(building.multiplyCostOnPopulation){
        productionMultiplier = data.population;
      }

      availableToBuild.Add(new CityBuildingInfo(){
        building = building,
        productionMultiplier = productionMultiplier,
        destCoord = destCoord,
        replacingBuildings = replacingBuildings,
        quantity = quantity,
      });
    }

    //go through the list to see if there are any buildings that would make another obsolete
    for(int i = 0; i < availableToBuild.Count; i++){
      var possiblyNewerBuilding = availableToBuild[i];
      if(possiblyNewerBuilding.building.makesObsolete != null){
        for(int j = 0; j < availableToBuild.Count; j++){
          var possiblyOlderBuilding = availableToBuild[j];
          if(possiblyNewerBuilding.building.makesObsolete.Contains(possiblyOlderBuilding.building.id)){

            //Oi structs.  Just setting the madeObsoleteBy
            availableToBuild[j] = new CityBuildingInfo(){
              building = possiblyOlderBuilding.building,
              productionMultiplier = possiblyOlderBuilding.productionMultiplier,
              destCoord = possiblyOlderBuilding.destCoord,
              replacingBuildings = possiblyOlderBuilding.replacingBuildings,
              quantity = possiblyOlderBuilding.quantity,
              madeObsoleteBy = possiblyNewerBuilding.building.id,
            };

            continue;
          }
        }
      }
    }

    return availableToBuild;
  }

  IEnumerable<HexCell> GetCellsForBuilding(IEnumerable<HexCell> influencedCells, CityBuilding building){
    //no replacing yourself on the cell
    influencedCells = influencedCells.Where(c => !c.building.HasValue || c.building.Value != building.id);

    //not on top of the city itself
    influencedCells = influencedCells.Where(c => data.coordinates != c.coordinates);

    //not replacing yourself in the build queue either
    influencedCells = influencedCells.Where(c => !data.buildQueue.Any(b =>
      building.id == b.buildingId && b.location.HasValue && b.location.Value == c.coordinates
    ));

    //not replacing a better version of a building
    influencedCells = influencedCells.Where(c => c.building == null
      || CityBuilding.allBuildings[c.building.Value].betterVersionOf == null
      || !CityBuilding.allBuildings[c.building.Value].betterVersionOf.Contains(building.id)
    );


    if(building.requiredTerrain != null){
      influencedCells = influencedCells.Where(c => building.requiredTerrain.Any(t => t == c.TerrainType));
    }
    if(building.requiredFeature != null){
      influencedCells = influencedCells.Where(c => building.requiredFeature.Any(t => t == c.HexFeature));
    }
    if(building.requiredRiver.HasValue){
      influencedCells = influencedCells.Where(c => c.HasRiver == building.requiredRiver.Value && !c.IsUnderwater);
    }
    if(building.requiresOcean.HasValue){
      influencedCells = influencedCells.Where(c => !c.Freshwater && c.IsUnderwater == building.requiresOcean.Value);
    }
    if(building.requiredBonusResource != HexBonusResource.None){
      influencedCells = influencedCells.Where(c => c.HexBonusResource == building.requiredBonusResource);
    }

    return influencedCells;
  }

  public HexCell GetFirstCellForBuilding(IEnumerable<HexCell> influencedCells, CityBuilding building){
    return GetCellsForBuilding(influencedCells, building).FirstOrDefault();
  }

  CityBuildingInfo? GetRoadBuildingInfo(CityBuildingId buildingId){
    //see if we can build roads and how long they are
    var accessibleNeighbors = GetAccessibleNeighbors(buildingId);
    if(accessibleNeighbors.Count == 0){
      return null;
    }

    var building = CityBuilding.allBuildings[buildingId];

    NeighborInfo? neighbor = null;

    var connectionsAlreadyBuilt = 0;
    //Count the total quantity of road connections.  The normal check won't handle having
    //multiple different kinds of connections that all fall under the same cap
    if(HexRiverOrRoad.connectionTier.ContainsKey(buildingId)){
      connectionsAlreadyBuilt =
        data.buildings.Count(b => HexRiverOrRoad.connectionTier.ContainsKey(b.buildingId)) +
        data.buildQueue.Count(b => HexRiverOrRoad.connectionTier.ContainsKey(b.buildingId));
    }else{
      connectionsAlreadyBuilt =
        data.buildings.Count(b => b.buildingId == buildingId) +
        data.buildQueue.Count(b => b.buildingId == buildingId);
    }


    var upgradableConnectionCount = 0;
    //make sure that when upgrading a road it selects the same path as the lower tier connection if there is one
    CityConnectionData firstConnectionToUpgrade = null;
    if(HexRiverOrRoad.connectionTier.ContainsKey(buildingId)){
      var upgradableConnections = data.outgoingConnections.Where(oc =>
        //Find connection that is upgradable and lower tier
        HexRiverOrRoad.connectionTier.ContainsKey(oc.connectionBuildingId) &&
        HexRiverOrRoad.connectionTier[oc.connectionBuildingId] < HexRiverOrRoad.connectionTier[buildingId] &&

        //And not something we're already building a road to
        !data.buildQueue.Any(i =>
          i.buildingId == buildingId
          && i.location.HasValue
          && i.location.Value == oc.dest
        )
      ).OrderBy(oc => oc.dest.DistanceTo(data.coordinates));

      upgradableConnectionCount = upgradableConnections.Count();
      firstConnectionToUpgrade = upgradableConnections.FirstOrDefault();
    }

    //filter out cities that already have the same or higher level connection to them and get the closest one
    var filteredNeighbors = accessibleNeighbors
      .Where(aNeighbor =>
        //make sure we don't have an existing connection of the same type or one that's upgradable
        !(data.outgoingConnections.Any(oc =>
          oc.dest == aNeighbor.coordinates &&
          (buildingId == oc.connectionBuildingId || HexRiverOrRoad.connectionTier.ContainsKey(buildingId))
        ))

        && !(data.incomingConnections.Any(ic =>
          ic.dest == aNeighbor.coordinates &&
          (buildingId == ic.connectionBuildingId || HexRiverOrRoad.connectionTier.ContainsKey(buildingId))
        ))

        //make sure the other city isn't building the same connection to us
        && !aNeighbor.cityData.buildQueue.Any(i =>
          (i.buildingId == buildingId || (HexRiverOrRoad.connectionTier.ContainsKey(buildingId) && HexRiverOrRoad.connectionTier.ContainsKey(i.buildingId)))
          && i.location.HasValue
          && i.location.Value == data.coordinates
        )
        //make sure we're not building the same connection
        && !data.buildQueue.Any(i =>
          (i.buildingId == buildingId || (HexRiverOrRoad.connectionTier.ContainsKey(buildingId) && HexRiverOrRoad.connectionTier.ContainsKey(i.buildingId)))
          && i.location.HasValue
          && i.location.Value == aNeighbor.coordinates
        )
      )
      .OrderBy(x => x.distance);

    var maxNewConnections = building.maxQuantity.Value - connectionsAlreadyBuilt;
    var newConnectionPossibleCount = Mathf.Max(0, Mathf.Min(filteredNeighbors.Count(), maxNewConnections));
    var quantity = Mathf.Min(
      building.maxQuantity.Value,
      upgradableConnectionCount + newConnectionPossibleCount
    );

    if(firstConnectionToUpgrade != null){
      neighbor = GetNeighborInfo(stageSixData.cities.FirstOrDefault(c => c.coordinates == firstConnectionToUpgrade.dest), buildingId);
    }else{
      //Make sure we haven't exceeded the total quantity of road connections.  The normal building max quantity check
      //Won't handle having multiple different kinds of connections
      if(building.maxQuantity.HasValue && HexRiverOrRoad.connectionTier.ContainsKey(buildingId)){

        if(connectionsAlreadyBuilt >= building.maxQuantity.Value){
          return null;
        }
      }
      neighbor = filteredNeighbors.FirstOrDefault();
    }

    if(neighbor == null || neighbor.Value.cityData == null){
      return null;
    }

    return new CityBuildingInfo(){
      productionMultiplier = neighbor.Value.distance,
      destCoord = neighbor.Value.coordinates,
      quantity = quantity
    };
  }

  //Get all neighboring cities that are accessible by land or water depending on the building type
  List<NeighborInfo> GetAccessibleNeighbors(CityBuildingId buildingId){
    var ret = new List<NeighborInfo>();
    foreach(var cityData in stageSixData.cities){
      if(cityData.coordinates == data.coordinates){
        continue; //skip ourselves
      }

      var neighborInfo = GetNeighborInfo(cityData, buildingId);

      if(neighborInfo != null){
        ret.Add(neighborInfo.Value);
      }
    }

    return ret.ToList();
  }

  NeighborInfo? GetNeighborInfo(HexCityData cityData, CityBuildingId buildingId){
    if(cityData == null){ return null; }

    var requiresOcean = CityBuilding.allBuildings[buildingId].requiresOcean ?? false;

    var pathfindOptions = grid.GetPathfindOptionsForConnection( new HexGrid.ConnectionParams(){
      origin = data.coordinates,
      dest = cityData.coordinates,
      connectionBuildingId = buildingId,
      originCity = this
    });

    var path = grid.FindPath(pathfindOptions);

    //little hack to make sure sea paths have at least some ocean tile in the path
    if(requiresOcean && path != null){
      var hasOcean = false;
      foreach(var coord in path){
        var cell = grid.GetCell(coord);
        if(cell.IsUnderwater && !cell.Freshwater){
          hasOcean = true;
          break;
        }
      }

      if(!hasOcean){ path = null; }
    }

    if(path != null){
      return new NeighborInfo(){
        cityData = cityData,
        coordinates = cityData.coordinates,
        distance = path.Count
      };
    }

    return null;
  }

  public struct NeighborInfo{
    public HexCityData cityData;
    public HexCoordinates coordinates;
    public int distance;
  }

  public class CityChangeInfo{
    public string descrip;
    public int change;
    public float? changePct;
    public float? changeFloat;
  }

  public struct CityThresholdLevel{
    public int threshold;
    public int lvl;
    public string title;
    public string descrip;
    public float growthRateMultiplier;
    public float productivityMultiplier;
  }

  public enum HealthFactor{
    Overcrowding,
    FreshWater,
    Sanitation,
    Medicine,
    Starvation,
    Pollution,
    HealthFocus,
    BuildingBonuses,
    ScienceBonuses,
    TileBonuses,
    Overworked,
  }

  public Dictionary<HealthFactor, CityChangeInfo> healthFactors = new Dictionary<HealthFactor, CityChangeInfo>(){
    {HealthFactor.Overcrowding   , new CityChangeInfo(){ change = 0, descrip = "Overcrowding" }},
    {HealthFactor.FreshWater     , new CityChangeInfo(){ change = 0, descrip = "Fresh Water" }},
    {HealthFactor.Sanitation     , new CityChangeInfo(){ change = 0, descrip = "Sanitation" }},
    {HealthFactor.Medicine       , new CityChangeInfo(){ change = 0, descrip = "Medicine" }},
    {HealthFactor.Overworked     , new CityChangeInfo(){ change = 0, descrip = "Overworked" }},
    {HealthFactor.Pollution      , new CityChangeInfo(){ change = 0, descrip = "Pollution" }},
    {HealthFactor.HealthFocus    , new CityChangeInfo(){ change = 0, descrip = "Health Focus" }},
    {HealthFactor.BuildingBonuses, new CityChangeInfo(){ change = 0, descrip = "Building Bonuses" }},
    {HealthFactor.ScienceBonuses , new CityChangeInfo(){ change = 0, descrip = "Science Bonuses" }},
    {HealthFactor.TileBonuses    , new CityChangeInfo(){ change = 0, descrip = "Tile Bonuses" }},
  };

  void UpdateHealthinessFactors(){
    healthFactors[HealthFactor.Overcrowding].change = -Mathf.RoundToInt(0.1f * data.population); //TODO

    //Clean Water & tile bonuses
    int freshWaterCells = 0;
    int tileHealth = 0;
    if(influencedCells != null){
      foreach(var cell in influencedCells){
        if(cell.Freshwater){ freshWaterCells++; }
        tileHealth += cell.Health(stageSixData);
      }
    }
    if(data.HasBuilding(CityBuildingId.Aqueduct)){
      freshWaterCells++;
    }
    if(freshWaterCells > 0){
      // healthFactors[HealthFactor.FreshWater].change = Mathf.Min(5 * freshWaterCells, 10) - Mathf.RoundToInt( (float)data.population / 10f );
      healthFactors[HealthFactor.FreshWater].change = 5;
    }else{
      healthFactors[HealthFactor.FreshWater].change = -5;
    }

    healthFactors[HealthFactor.TileBonuses].change = tileHealth;

    //Overworked
    if(data.work > 0.7f){
      //Ramp up the more work percentage, and make it more serious for larger cities
      float overworkFactor = (data.work - 0.7f) / 0.05f;
      healthFactors[HealthFactor.Overworked].change = -1 -
        Mathf.RoundToInt(overworkFactor * (1f + data.population / 50f));
    }else{
      healthFactors[HealthFactor.Overworked].change = 0;
    }

    //Sanitation
    float sanitationPenalty = (float)data.population / 10;
    if(data.HasBuilding(CityBuildingId.SewerSystem)){ sanitationPenalty -= (float)data.population / 30; }
    else if(data.HasBuilding(CityBuildingId.PublicBaths)){ sanitationPenalty -= (float)data.population / 50; }

    healthFactors[HealthFactor.Sanitation].change = -6 - Mathf.RoundToInt(sanitationPenalty);

    //Medicine
    healthFactors[HealthFactor.Medicine].change = -9;
    if(stageSixData.ResearchedTech(HexTechId.Microbiology)){ healthFactors[HealthFactor.Medicine].change += 4; }
    if(stageSixData.ResearchedTech(HexTechId.Antibiotics)){ healthFactors[HealthFactor.Medicine].change += 6; }
    if(stageSixData.ResearchedTech(HexTechId.SpecializedMedicine)){ healthFactors[HealthFactor.Medicine].change += 7; }

    //Pollution
    healthFactors[HealthFactor.Pollution].change = -stageSixData.pollutionLevel;

    healthFactors[HealthFactor.HealthFocus].change = Mathf.RoundToInt( (data.health * data.population) / 3f );
    healthFactors[HealthFactor.BuildingBonuses].change = data.healthBonus;
    healthFactors[HealthFactor.ScienceBonuses].change = stageSixData.healthBonus;
  }

  void CalculateHealthiness(){
    UpdateHealthinessFactors();

    //sum up the changes
    data.citizenHealthiness = 0;
    foreach(var factor in healthFactors.Values){
      data.citizenHealthiness += factor.change;
    }

    healthThreshold = GetThreshold(healthThresholds, data.citizenHealthiness);
  }

  static CityThresholdLevel[] healthThresholds = new CityThresholdLevel[]{
    new CityThresholdLevel(){ lvl = -4, threshold = -40, title = "Pestilence", descrip = "Food rate 5%", growthRateMultiplier = 0.05f },
    new CityThresholdLevel(){ lvl = -3, threshold = -30, title = "Plague",     descrip = "Food rate 10%", growthRateMultiplier = 0.1f },
    new CityThresholdLevel(){ lvl = -2, threshold = -20, title = "Sickly",     descrip = "Food rate 40%", growthRateMultiplier = 0.4f },
    new CityThresholdLevel(){ lvl = -1, threshold = -10, title = "Unhealthy",  descrip = "Food rate 70%", growthRateMultiplier = 0.7f },
    new CityThresholdLevel(){ lvl =  0, threshold =   0, title = "Neutral",    descrip = "Food rate 100%", growthRateMultiplier = 1f },
    new CityThresholdLevel(){ lvl =  1, threshold =  10, title = "Healthy",    descrip = "Food rate 115%", growthRateMultiplier = 1.15f },
    new CityThresholdLevel(){ lvl =  2, threshold =  20, title = "Fit"    ,    descrip = "Food rate 130%", growthRateMultiplier = 1.3f },
    new CityThresholdLevel(){ lvl =  3, threshold =  30, title = "Perfect",    descrip = "Food rate 145%", growthRateMultiplier = 1.45f },
  };

  public CityThresholdLevel GetThreshold(CityThresholdLevel[] thresholds, int amt){
    var level = thresholds[0];
    for(int i = 1; i < thresholds.Length; i++){
      if(amt >= thresholds[i].threshold){
        level = thresholds[i];
      }
    }
    return level;
  }

  public enum HappinessFactor{
    Overcrowding,
    AccessToNature,
    Starvation,
    Purpose,
    Overworked,
    Healthiness,
    PlayFocus,
    BuildingBonuses,
    ScienceBonuses,
    TileBonuses
  }

  public Dictionary<HappinessFactor, CityChangeInfo> happinessFactors = new Dictionary<HappinessFactor, CityChangeInfo>(){
    {HappinessFactor.Starvation     , new CityChangeInfo(){ change = 0, descrip = "Starvation" }},
    {HappinessFactor.Overcrowding   , new CityChangeInfo(){ change = 0, descrip = "Overcrowding" }},
    {HappinessFactor.AccessToNature , new CityChangeInfo(){ change = 0, descrip = "Access to Nature" }},
    {HappinessFactor.Overworked     , new CityChangeInfo(){ change = 0, descrip = "Overworked" }},
    {HappinessFactor.Healthiness    , new CityChangeInfo(){ change = 0, descrip = "Healthiness" }},
    {HappinessFactor.PlayFocus      , new CityChangeInfo(){ change = 0, descrip = "Play Focus" }},
    {HappinessFactor.BuildingBonuses, new CityChangeInfo(){ change = 0, descrip = "Building Bonuses" }},
    {HappinessFactor.ScienceBonuses , new CityChangeInfo(){ change = 0, descrip = "Science Bonuses" }},
    {HappinessFactor.TileBonuses    , new CityChangeInfo(){ change = 0, descrip = "Tile Bonuses" }},
  };

  void UpdateHappinessFactors(){
    happinessFactors[HappinessFactor.Overcrowding].change = -Mathf.RoundToInt(0.1f * data.population);

    //Access to nature && tile bonuses
    int undevelopedCells = 0;
    int tileHappiness = 0;
    if(influencedCells != null){
      foreach(var cell in influencedCells){
        if(!cell.building.HasValue){
          undevelopedCells++;
        }
        tileHappiness += cell.Happiness(stageSixData);
      }
    }
    happinessFactors[HappinessFactor.AccessToNature].change = Mathf.Min(12, Mathf.RoundToInt( undevelopedCells * 12f / data.population ));

    happinessFactors[HappinessFactor.TileBonuses].change = tileHappiness;

    //Starvation
    if(FoodPerSecond < 0){
      happinessFactors[HappinessFactor.Starvation].change = Mathf.RoundToInt(FoodPerSecond / 2f);
    }else{
      happinessFactors[HappinessFactor.Starvation].change = 0;
    }

    //Overworked
    if(data.work > 0.7f){
      //Ramp up the more work percentage, and make it more serious for larger cities
      float overworkFactor = (data.work - 0.7f) / 0.05f;
      happinessFactors[HappinessFactor.Overworked].change = -1 -
        Mathf.RoundToInt(overworkFactor * (1f + data.population / 50f));
    }else{
      happinessFactors[HappinessFactor.Overworked].change = 0;
    }

    //Healthiness
    happinessFactors[HappinessFactor.Healthiness].change = healthThreshold.lvl * 2;

    happinessFactors[HappinessFactor.PlayFocus].change = Mathf.RoundToInt( (data.play * data.population) / 3f );
    happinessFactors[HappinessFactor.BuildingBonuses].change = data.happinessBonus;
    happinessFactors[HappinessFactor.ScienceBonuses].change = stageSixData.happinessBonus;
  }

  void CalculateHappiness(){
    UpdateHappinessFactors();

    //sum up the changes
    data.citizenHappiness = 0;
    foreach(var factor in happinessFactors.Values){
      data.citizenHappiness += factor.change;
    }

    happinessThreshold = GetThreshold(happinessThresholds, data.citizenHappiness);
  }

  static CityThresholdLevel[] happinessThresholds = new CityThresholdLevel[]{
    new CityThresholdLevel(){ lvl = -4, threshold = -40, title = "Enraged",  descrip = "Production 5%", productivityMultiplier = 0.05f },
    new CityThresholdLevel(){ lvl = -3, threshold = -30, title = "Livid",    descrip = "Production 10%", productivityMultiplier = 0.1f },
    new CityThresholdLevel(){ lvl = -2, threshold = -20, title = "Angry",    descrip = "Production 40%", productivityMultiplier = 0.4f },
    new CityThresholdLevel(){ lvl = -1, threshold = -10, title = "Unhappy",  descrip = "Production 70%", productivityMultiplier = 0.7f },
    new CityThresholdLevel(){ lvl =  0, threshold =   0, title = "Neutral",  descrip = "Production 100%", productivityMultiplier = 1f },
    new CityThresholdLevel(){ lvl =  1, threshold =  10, title = "Happy",    descrip = "Production 115%", productivityMultiplier = 1.15f },
    new CityThresholdLevel(){ lvl =  2, threshold =  20, title = "Joyful",   descrip = "Production 130%", productivityMultiplier = 1.3f },
    new CityThresholdLevel(){ lvl =  3, threshold =  30, title = "Ecstatic", descrip = "Production 145%", productivityMultiplier = 1.45f },
  };

  public HexCell Cell{
    get{
      return grid.GetCell(data.coordinates);
    }
  }

  public int AreaOfInfluence{
    get{ return data.areaOfInfluence; }
  }

  public int CalcAreaOfInfluence(){
    if(data.population < 36) return 1;
    if(data.population < 72) return 2;
    if(data.population < 288) return 3;
    if(data.population < 576) return 4;

    return 5;
  }

  void UpdateCityDisplay(){
    var aoi = CalcAreaOfInfluence();
    var sprite = loader.Load<Sprite>("Art/stage6/hex_town_" + aoi);
    cityRenderer.sprite = sprite;
  }

  public int FoodNeededForGrowth{
    get{
      return 20 + Mathf.RoundToInt(data.population * 1.10f) + Mathf.RoundToInt(Mathf.Pow(data.population, 1.32f));
    }
  }

  public enum FoodFactor{
    HealthGrowthRate,
    RateBonus,
    AmountInCells,
    TradeBonus,
    BuildingSettler,
  }

  public Dictionary<FoodFactor, CityChangeInfo> foodFactors = new Dictionary<FoodFactor, CityChangeInfo>(){
    {FoodFactor.HealthGrowthRate, new CityChangeInfo(){ changePct = 0, descrip = "Health Growth Rate" }},
    {FoodFactor.RateBonus       , new CityChangeInfo(){ changePct = 0, descrip = "Food Bonus" }},
    {FoodFactor.AmountInCells   , new CityChangeInfo(){ change    = 0, descrip = "Food in Cells" }},
    {FoodFactor.TradeBonus      , new CityChangeInfo(){ change    = 0, descrip = "Trade Bonus" }},
    {FoodFactor.BuildingSettler , new CityChangeInfo(){ change    = 0, descrip = "Building Settler" }},
  };

  //Calc & Store the calculated food per second once per frame so everything is consistent
  int foodPerSecondCalulatedFrame = 0;
  float _foodPerSecond = 0;

  public float FoodPerSecond{
    get{
      if(foodPerSecondCalulatedFrame == Time.frameCount){
        return _foodPerSecond;
      }
      foodPerSecondCalulatedFrame = Time.frameCount;

      _foodPerSecond = BaseFoodPerSecond;

      var foodTradeBonus = SumTradeBonus(GameResourceType.Food);
      _foodPerSecond += foodTradeBonus;

      //Handle the population equilibrium case and set the food to 0 in that case so the city
      //Doesn't grow and shrink rapidly
      var predictedFoodCollected = _foodPerSecond * Time.unscaledDeltaTime;
      var predictedPopChange = predictedFoodCollected / FoodNeededForGrowth;
      if(_foodPerSecond < 0 && (predictedPopChange + data.popProgress < 0)){
        //Simulate shrinking and see if we're back to positive food
        data.population -= 1;
        CalculateHealthiness();

        var shrunkFoodPerSecond = BaseFoodPerSecond;
        shrunkFoodPerSecond += foodTradeBonus;

        data.population += 1;
        CalculateHealthiness();

        if(shrunkFoodPerSecond > 0){
          _foodPerSecond = 0;
        }
      }

      //update the food factors for the details panel
      foodFactors[FoodFactor.HealthGrowthRate].changePct = healthThreshold.growthRateMultiplier;
      foodFactors[FoodFactor.RateBonus].changePct = data.foodRateBonus + stageSixData.foodRateBonus;
      foodFactors[FoodFactor.AmountInCells].change = foodInCells;
      foodFactors[FoodFactor.TradeBonus].changeFloat = foodTradeBonus;
      foodFactors[FoodFactor.BuildingSettler].change = -ExtraFoodConsumed;

      return _foodPerSecond;
    }
  }

  //Excluding trade bonuses
  public float BaseFoodPerSecond{
    get{
      var foodConsumed = data.population;

      foodConsumed += ExtraFoodConsumed;

      var maxPopBasedOnFood = foodInCells * stageRules.StageSixRules.foodInCellFeedsPopCount;

      var foodProduced =
          data.growth
        * data.work
        * healthThreshold.growthRateMultiplier
        * (1 + data.foodRateBonus + stageSixData.foodRateBonus)
        * maxPopBasedOnFood
        ;

      var growthRate = foodProduced - foodConsumed;
      return growthRate;
    }
  }

  public int ExtraFoodConsumed {
    get{
      var extra = 0;
      if(data.buildQueue.Count > 0){
        var building = data.buildQueue[0];
        var currentBuilding = CityBuilding.allBuildings[building.buildingId];

        if(currentBuilding.foodCostPct > 0){
          extra = Mathf.RoundToInt(currentBuilding.foodCostPct * data.population);
        }
      }

      return extra;
    }
  }

  public enum ProductionFactor{
    HappinessProductivityRate,
    RateBonus,
    AmountInCells,
    TradeBonus,
  }

  public Dictionary<ProductionFactor, CityChangeInfo> productionFactors = new Dictionary<ProductionFactor, CityChangeInfo>(){
    {ProductionFactor.HappinessProductivityRate, new CityChangeInfo(){ changePct = 0, descrip = "Happiness Production Rate" }},
    {ProductionFactor.RateBonus                , new CityChangeInfo(){ changePct = 0, descrip = "Production Bonus" }},
    {ProductionFactor.AmountInCells            , new CityChangeInfo(){ change    = 0, descrip = "Production in Cells" }},
    {ProductionFactor.TradeBonus               , new CityChangeInfo(){ change    = 0, descrip = "Trade Bonus" }},
  };

  public float ProductionPerSecond{
    get{
      var production = BaseProductionPerSecond;

      var totalBonus = SumTradeBonus(GameResourceType.Production);

      //update the production factors for the details panel
      productionFactors[ProductionFactor.HappinessProductivityRate].changePct = happinessThreshold.productivityMultiplier;
      productionFactors[ProductionFactor.RateBonus].changePct = data.productionRateBonus + stageSixData.productionRateBonus;
      productionFactors[ProductionFactor.AmountInCells].change = productionInCells;
      productionFactors[ProductionFactor.TradeBonus].changeFloat = totalBonus;

      return production + totalBonus;
    }
  }

  //Excluding trade bonuses
  public float BaseProductionPerSecond{
    get{
      return Mathf.Max(1,
        data.population
        * data.production
        * data.work
        * stageRules.StageSixRules.baseCityProductionRate
        * happinessThreshold.productivityMultiplier
        * (1 + data.productionRateBonus + stageSixData.productionRateBonus)
        * productionInCells
      );
    }
  }

  public enum ScienceFactor{
    RateBonus,
    AmountInCells,
    TradeBonus,
  }

  public Dictionary<ScienceFactor, CityChangeInfo> scienceFactors = new Dictionary<ScienceFactor, CityChangeInfo>(){
    {ScienceFactor.RateBonus                , new CityChangeInfo(){ changePct = 0, descrip = "Science Bonus" }},
    {ScienceFactor.AmountInCells            , new CityChangeInfo(){ change    = 0, descrip = "Science in Cells" }},
    {ScienceFactor.TradeBonus               , new CityChangeInfo(){ change    = 0, descrip = "Trade Bonus" }},
  };

  public float SciencePerSecond{
    get{
      var science = BaseSciencePerSecond;

      var totalBonus = SumTradeBonus(GameResourceType.Science);

      scienceFactors[ScienceFactor.RateBonus].changePct = data.scienceRateBonus + stageSixData.scienceRateBonus;
      scienceFactors[ScienceFactor.AmountInCells].change = scienceInCells;
      scienceFactors[ScienceFactor.TradeBonus].changeFloat = totalBonus;

      return science + totalBonus;
    }
  }

  //Excluding trade bonuses
  public float BaseSciencePerSecond{
    get{
      return Mathf.Max(1,
        data.population
        * data.science
        * data.work
        * stageRules.StageSixRules.baseCityScienceRate //how much pop it takes to produce 1 science
        * (1 + data.scienceRateBonus + stageSixData.scienceRateBonus)
        * (data.scienceAmount + scienceInCells)
      );
    }
  }

  public float SumTradeBonus(GameResourceType resourceType){
    var tradeBonus = 0f;
    foreach(var roadData in data.incomingConnections){
      var connectedCity = grid.GetCell(roadData.dest).city;
      if(connectedCity == null){ continue; }

      var baseResourceAmount = 0f;
      switch(resourceType){
        case GameResourceType.Food      : baseResourceAmount = connectedCity.BaseFoodPerSecond; break;
        case GameResourceType.Production: baseResourceAmount = connectedCity.BaseProductionPerSecond; break;
        case GameResourceType.Science   : baseResourceAmount = connectedCity.BaseSciencePerSecond; break;
      }

      if(baseResourceAmount > 0){
        tradeBonus += baseResourceAmount *
          CityBuilding.allBuildings[roadData.connectionBuildingId].cityConnectionBonus *
          (1 + data.tradeBonus);
      }
    }
    foreach(var roadData in data.outgoingConnections){
      var connectedCity = grid.GetCell(roadData.dest).city;
      if(connectedCity == null){ continue; }

      var baseResourceAmount = 0f;
      switch(resourceType){
        case GameResourceType.Food      : baseResourceAmount = connectedCity.BaseFoodPerSecond; break;
        case GameResourceType.Production: baseResourceAmount = connectedCity.BaseProductionPerSecond; break;
        case GameResourceType.Science   : baseResourceAmount = connectedCity.BaseSciencePerSecond; break;
      }

      if(baseResourceAmount > 0){
        tradeBonus += baseResourceAmount *
          CityBuilding.allBuildings[roadData.connectionBuildingId].cityConnectionBonus *
          (1 + data.tradeBonus) *
          (1 + stageSixData.tradeBonus);
      }
    }
    return tradeBonus;
  }

  public void BuildRoad(CityBuildingData buildingData, CityBuildingId buildingId){
    data.outgoingConnections.Add(new CityConnectionData(){
      dest = buildingData.location.Value,
      connectionBuildingId = buildingId
    });

    var destCity = stageSixData.cities.FirstOrDefault(c => c.coordinates == buildingData.location.Value);

    destCity.incomingConnections.Add(new CityConnectionData(){
      dest = data.coordinates,
      connectionBuildingId = buildingId
    });

    grid.CreateRiverOrConnection(new HexGrid.ConnectionParams(){
      origin = data.coordinates,
      dest = destCity.coordinates,
      connectionBuildingId = buildingId,
      originCity = this
    });
  }

  //Note this doesn't actually remove the road gameobject since it will be upgraded to a new road type
  //during the road upgrade process
  public void RemoveRoad(FinishedCityBuildingData buildingData, CityBuildingId buildingId){
    var foundOutgoing = data.outgoingConnections.FirstOrDefault(oc => oc.connectionBuildingId == buildingId && oc.dest == buildingData.location.Value);
    if(foundOutgoing == null){
      Debug.LogWarning("Unable to find outgoing connection to remove");
    }else{
      data.outgoingConnections.Remove(foundOutgoing);
    }

    var destCity = stageSixData.cities.FirstOrDefault(c => c.coordinates == buildingData.location.Value);

    var foundIncoming = destCity.incomingConnections.FirstOrDefault(ic => ic.connectionBuildingId == buildingId && ic.dest == data.coordinates);
    if(foundIncoming == null){
      Debug.LogWarning("Unable to find incoming connection to remove");
    }else{
      destCity.incomingConnections.Remove(foundIncoming);
    }
  }

  public void ExploreAround(int radius){
    if(radius <= 1){ return; }

    var middleCell = grid.GetCell(data.coordinates);

    for(var v = 1; v <= radius; v++){
      foreach(var cell in grid.GetRing(middleCell, v)){
        cell.Explore(HexExploreStatus.Explored);
      }
    }
  }
}
