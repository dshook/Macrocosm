using System.Collections.Generic;
using UnityEngine;

public class HexMapGenerator : MonoBehaviour {

  public HexGrid grid;

  public int mapGeneratorVersion = 2;

  public bool useFixedSeed;

  public int seed;

  public bool ensureCenterLand;

  [Range(0f, 0.5f)]
  public float jitterProbability = 0.25f;

  [Range(20, 200)]
  public int chunkSizeMin = 30;

  [Range(20, 200)]
  public int chunkSizeMax = 100;

  [Range(0f, 1f)]
  public float highRiseProbability = 0.25f;

  [Range(0f, 0.4f)]
  public float sinkProbability = 0.2f;

  [Range(0, 100)]
  public int oceanSinkChunks = 50;

  [Range(20, 200)]
  public int oceanChunkSizeMin = 30;

  [Range(20, 200)]
  public int oceanChunkSizeMax = 100;

  [Range(0f, 0.5f)]
  public float oceanJitterProbability = 0.25f;

  [Range(5, 95)]
  public int landPercentage = 50;

  // [Range(1, 5)]
  // public int waterLevel = 1;
  int waterLevel = 0;

  [Range(-5, 0)]
  public int elevationMinimum = -5;

  [Range(6, 10)]
  public int elevationMaximum = 8;

  [Range(0, 10)]
  public int mapBorderX = 5;

  [Range(0, 10)]
  public int mapBorderZ = 5;

  [Range(0, 10)]
  public int regionBorder = 5;

  [Range(1, 4)]
  public int regionCount = 1;

  [Range(0, 100)]
  public int erosionPercentage = 50;

  [Range(0f, 1f)]
  public float startingMoisture = 0.1f;

  [Range(0f, 1f)]
  public float evaporationFactor = 0.5f;

  [Range(0f, 1f)]
  public float precipitationFactor = 0.25f;

  [Range(0f, 1f)]
  public float runoffFactor = 0.25f;

  [Range(0f, 1f)]
  public float seepageFactor = 0.125f;

  public HexDirection windDirection = HexDirection.NW;

  [Range(1f, 10f)]
  public float windStrength = 4f;

  [Range(0, 20)]
  public int riverPercentage = 10;

  [Range(0f, 1f)]
  public float extraLakeProbability = 0.25f;

  [Range(0f, 1f)]
  public float lowTemperature = 0f;

  [Range(0f, 1f)]
  public float highTemperature = 1f;

  public enum HemisphereMode {
    Both, North, South
  }

  public HemisphereMode hemisphere;

  [Range(0f, 1f)]
  public float temperatureJitter = 0.1f;

  [Range(0, 40)]
  public int climateEvolutionCycles = 40;

  public CellDrawMode cellDrawMode = CellDrawMode.None;

  HexCellPriorityQueue searchFrontier;

  int searchFrontierPhase;

  int cellCount, landCells;

  struct MapRegion {
    public int xMin, xMax, zMin, zMax;
  }

  List<MapRegion> regions;

  List<HexClimateData> climate = new List<HexClimateData>();
  List<HexClimateData> nextClimate = new List<HexClimateData>();

  List<HexDirection> flowDirections = new List<HexDirection>();

  struct Biome {
    public HexTerrainType terrainType;
    public int plant;

    public int temperature;
    public int moisture;

    public Biome (HexTerrainType terrainType, int plant, int temp, int moisture) {
      this.terrainType = terrainType;
      this.plant = plant;
      this.temperature = temp;
      this.moisture = moisture;
    }
  }

  static float[] temperatureBands = { 0.1f, 0.3f, 0.6f };

  static float[] moistureBands = { 0.12f, 0.28f, 0.85f };

  //2d matrix for temperature and moisture levels. Accessed by biomes[t * 4 + m];
  static Biome[] biomes = {
    new Biome(HexTerrainType.Sand, 0, 0, 0), new Biome(HexTerrainType.Snow,  0, 0, 1), new Biome(HexTerrainType.Snow,  0, 0, 2), new Biome(HexTerrainType.Snow,  0, 0, 3),
    new Biome(HexTerrainType.Sand, 0, 1, 0), new Biome(HexTerrainType.Mud,   0, 1, 1), new Biome(HexTerrainType.Mud,   1, 1, 2), new Biome(HexTerrainType.Mud,   2, 1, 3),
    new Biome(HexTerrainType.Sand, 0, 2, 0), new Biome(HexTerrainType.Grass, 0, 2, 1), new Biome(HexTerrainType.Grass, 1, 2, 2), new Biome(HexTerrainType.Grass, 2, 2, 3),
    new Biome(HexTerrainType.Sand, 0, 3, 0), new Biome(HexTerrainType.Grass, 1, 3, 1), new Biome(HexTerrainType.Grass, 2, 3, 2), new Biome(HexTerrainType.Grass, 3, 3, 3)
  };

  static Biome[] waterBiomes = {
    new Biome(HexTerrainType.Water, 0, 0, 0),
    new Biome(HexTerrainType.Water, 0, 1, 0),
    new Biome(HexTerrainType.Water, 0, 2, 0),
    new Biome(HexTerrainType.Water, 0, 3, 0),
  };

  public void GenerateMap () {
    var startTime = Time.realtimeSinceStartup;
    Random.State originalRandomState = Random.state;
    if (!useFixedSeed) {
      seed = NumberExtensions.GenerateNewSeed();
    }
    Random.InitState(seed);

    grid.FindGrid();
    cellCount = grid.cellCount;

    if (searchFrontier == null) {
      searchFrontier = new HexCellPriorityQueue();
    }

    //Reset some things first
    for (int i = 0; i < cellCount; i++) {
      var cell = grid.GetCell(i);
      cell.Elevation = waterLevel;
      cell.IsRiverOrigin = false;
      cell.RemoveRiver();
      cell.HexFeature = HexFeature.None;
    }

    CreateRegions();
    var regionTime = Time.realtimeSinceStartup;
    SinkOcean();
    CreateLand();
    var landTime = Time.realtimeSinceStartup;
    ErodeLand();

    //Makes sure that the center cell is on land
    if(ensureCenterLand){
      //TODO: only makes sense for a 1 region map right now
      var centerCell = grid.GetCenterCell();
      var maxTries = 20;
      var tries = 0;
      while(centerCell.Elevation <= waterLevel && tries < maxTries){
        AdjustTerrain(chunkSizeMax, chunkSizeMax, regions[0], jitterProbability, false, centerCell);
        tries++;
      }
    }

    var erodeTime = Time.realtimeSinceStartup;
    CreateClimate();
    var climateTime = Time.realtimeSinceStartup;
    CreateRivers();
    UpdateCellsData();
    var cellDataTime = Time.realtimeSinceStartup;
    FindFreshwater();
    var freshwaterTime = Time.realtimeSinceStartup;

    Random.state = originalRandomState;
    var endTime = Time.realtimeSinceStartup;
    Debug.Log(string.Format("Total Gen Time: {0}, Region: {1}, Land: {2}, Erode: {3}, Climate: {4}, CD: {5}, FreshW: {6}",
      endTime - startTime,
      regionTime - startTime,
      landTime - regionTime,
      erodeTime - landTime,
      climateTime - erodeTime,
      cellDataTime - climateTime,
      freshwaterTime - cellDataTime
    ));


#if UNITY_EDITOR
    //Some stats for design
    Dictionary<HexBonusResource, int> bonusResourceTotals = new Dictionary<HexBonusResource, int>();
    for (int cellI = 0; cellI < cellCount; cellI++) {
      var cell = grid.GetCell(cellI);

      bonusResourceTotals.AddOrUpdate(cell.HexBonusResource, 1, i => i + 1);
    }

    bonusResourceTotals.LogKeyValues("Bonus Resources");
#endif
  }

  void CreateRegions () {
    if (regions == null) {
      regions = new List<MapRegion>();
    }
    else {
      regions.Clear();
    }

    MapRegion region;
    switch (regionCount) {
    default:
      region.xMin = mapBorderX;
      region.xMax = grid.width - mapBorderX;
      region.zMin = mapBorderZ;
      region.zMax = grid.height - mapBorderZ;
      regions.Add(region);
      break;
    case 2:
      if (Random.value < 0.5f) {
        region.xMin = mapBorderX;
        region.xMax = grid.width / 2 - regionBorder;
        region.zMin = mapBorderZ;
        region.zMax = grid.height - mapBorderZ;
        regions.Add(region);
        region.xMin = grid.width / 2 + regionBorder;
        region.xMax = grid.width - mapBorderX;
        regions.Add(region);
      }
      else {
        region.xMin = mapBorderX;
        region.xMax = grid.width - mapBorderX;
        region.zMin = mapBorderZ;
        region.zMax = grid.height / 2 - regionBorder;
        regions.Add(region);
        region.zMin = grid.height / 2 + regionBorder;
        region.zMax = grid.height - mapBorderZ;
        regions.Add(region);
      }
      break;
    case 3:
      region.xMin = mapBorderX;
      region.xMax = grid.width / 3 - regionBorder;
      region.zMin = mapBorderZ;
      region.zMax = grid.height - mapBorderZ;
      regions.Add(region);
      region.xMin = grid.width / 3 + regionBorder;
      region.xMax = grid.width * 2 / 3 - regionBorder;
      regions.Add(region);
      region.xMin = grid.width * 2 / 3 + regionBorder;
      region.xMax = grid.width - mapBorderX;
      regions.Add(region);
      break;
    case 4:
      region.xMin = mapBorderX;
      region.xMax = grid.width / 2 - regionBorder;
      region.zMin = mapBorderZ;
      region.zMax = grid.height / 2 - regionBorder;
      regions.Add(region);
      region.xMin = grid.width / 2 + regionBorder;
      region.xMax = grid.width - mapBorderX;
      regions.Add(region);
      region.zMin = grid.height / 2 + regionBorder;
      region.zMax = grid.height - mapBorderZ;
      regions.Add(region);
      region.xMin = mapBorderX;
      region.xMax = grid.width / 2 - regionBorder;
      regions.Add(region);
      break;
    }
  }

  void SinkOcean(){
    //first apply a sort of vignette around the border so the edges are deeper

    //left edge
    for(int z = 0; z < grid.height; z++){
      for(int x = 0; x < mapBorderX; x++){
        var cell = grid.GetCell(x, z);

        int awayFromEdge = (cell.coordinates.Y - cell.coordinates.X) / -2;
        float edgePct = (float)(mapBorderX - awayFromEdge) / mapBorderX;
        edgePct = Mathf.Clamp01(edgePct);

        cell.Elevation = Mathf.RoundToInt((float)elevationMinimum * edgePct);
      }
    }

    //right edge
    for(int z = 0; z < grid.height; z++){
      for(int x = grid.width - mapBorderX - 1; x < grid.width; x++){
        var cell = grid.GetCell(x, z);

        int awayFromEdge = (cell.coordinates.Y - cell.coordinates.X) / -2;
        float edgePct = (float)(grid.width - awayFromEdge - 1) / mapBorderX;
        edgePct = 1f - Mathf.Clamp01(edgePct);

        cell.Elevation = Mathf.RoundToInt((float)elevationMinimum * edgePct);
      }
    }

    //For top and bottom
    for (int i = 0; i < cellCount; i++) {
      HexCell cell = grid.GetCell(i);
      float edgePct = 0;

      if(cell.coordinates.Z < mapBorderZ){
        edgePct = (float)(mapBorderZ - cell.coordinates.Z) / mapBorderZ;
      }
      else if(cell.coordinates.Z > grid.height - mapBorderZ - 1){
        edgePct = (float)(cell.coordinates.Z - (grid.height - mapBorderZ - 1)) / mapBorderZ;
      }
      else{
        continue;
      }

      edgePct = Mathf.Clamp01(edgePct);

      cell.Elevation = Mathf.Min(cell.Elevation, Mathf.RoundToInt((float)elevationMinimum * edgePct));
    }

    //now sink some random chunks
    for(int sc = 0; sc < oceanSinkChunks; sc++){
      for (int i = 0; i < regions.Count; i++) {
        MapRegion region = regions[i];
        int chunkSize = Random.Range(oceanChunkSizeMin, oceanChunkSizeMax - 1);

        AdjustTerrain(chunkSize, 0, region, oceanJitterProbability, true);
      }
    }
  }

  void CreateLand () {
    int landBudget = Mathf.RoundToInt(cellCount * landPercentage * 0.01f);
    landCells = landBudget;
    for (int guard = 0; guard < 10000; guard++) {
      bool sink = Random.value < sinkProbability;
      for (int i = 0; i < regions.Count; i++) {
        MapRegion region = regions[i];
        int chunkSize = Random.Range(chunkSizeMin, chunkSizeMax - 1);

        if (sink) {
          landBudget = AdjustTerrain(chunkSize, landBudget, region, jitterProbability, true);
        }
        else {
          landBudget = AdjustTerrain(chunkSize, landBudget, region, jitterProbability, false);
          if (landBudget == 0) {
            return;
          }
        }
      }
    }
    if (landBudget > 0) {
      landCells -= landBudget;
      Logger.LogWarning("Failed to use up " + landBudget + " land budget.");
    }
  }

  int AdjustTerrain (int chunkSize, int budget, MapRegion region, float jitter, bool isSink = false, HexCell firstCell = null) {
    searchFrontierPhase += 1;
    if(firstCell == null){
      firstCell = GetRandomCell(region);
    }
    firstCell.SearchPhase = searchFrontierPhase;
    firstCell.Distance = 0;
    searchFrontier.Enqueue(firstCell);
    HexCoordinates center = firstCell.coordinates;

    int rise = Random.value < highRiseProbability ? 2 : 1;
    if(isSink) { rise *= -1; }
    int size = 0;
    while (size < chunkSize && searchFrontier.Count > 0) {
      HexCell current = searchFrontier.Dequeue();
      int originalElevation = current.Elevation;
      int newElevation = originalElevation + rise;
      if (!isSink && newElevation > elevationMaximum) {
        continue;
      }
      if (isSink && newElevation < elevationMinimum) {
        continue;
      }
      current.Elevation = newElevation;
      if (
        !isSink &&
        originalElevation <= waterLevel &&
        newElevation > waterLevel
      ) {
        budget--;
        if(budget == 0){
          break;
        }
      }
      if(
        isSink &&
        originalElevation > waterLevel &&
        newElevation <= waterLevel
      ){
        budget++;
      }
      size += 1;

      for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
        HexCell neighbor = current.GetNeighbor(d);
        if (neighbor != null && neighbor.SearchPhase < searchFrontierPhase) {
          neighbor.SearchPhase = searchFrontierPhase;
          neighbor.Distance = neighbor.coordinates.DistanceTo(center);
          neighbor.SearchHeuristic = Random.value < jitter ? 1: 0;
          searchFrontier.Enqueue(neighbor);
        }
      }
    }
    searchFrontier.Clear();
    return budget;
  }

  void ErodeLand () {
    List<HexCell> erodibleCells = ListPool<HexCell>.Get();
    for (int i = 0; i < cellCount; i++) {
      HexCell cell = grid.GetCell(i);
      if (IsErodible(cell)) {
        erodibleCells.Add(cell);
      }
    }

    int targetErodibleCount =
      (int)(erodibleCells.Count * (100 - erosionPercentage) * 0.01f);

    while (erodibleCells.Count > targetErodibleCount) {
      int index = Random.Range(0, erodibleCells.Count);
      HexCell cell = erodibleCells[index];
      HexCell targetCell = GetErosionTarget(cell);

      cell.Elevation -= 1;
      targetCell.Elevation += 1;

      if (!IsErodible(cell)) {
        erodibleCells[index] = erodibleCells[erodibleCells.Count - 1];
        erodibleCells.RemoveAt(erodibleCells.Count - 1);
      }

      for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
        HexCell neighbor = cell.GetNeighbor(d);
        if (
          neighbor != null && neighbor.Elevation == cell.Elevation + 2 &&
          !erodibleCells.Contains(neighbor)
        ) {
          erodibleCells.Add(neighbor);
        }
      }

      if (IsErodible(targetCell) && !erodibleCells.Contains(targetCell)) {
        erodibleCells.Add(targetCell);
      }

      for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
        HexCell neighbor = targetCell.GetNeighbor(d);
        if (
          neighbor != null && neighbor != cell &&
          neighbor.Elevation == targetCell.Elevation + 1 &&
          !IsErodible(neighbor)
        ) {
          erodibleCells.Remove(neighbor);
        }
      }
    }

    ListPool<HexCell>.Add(erodibleCells);
  }

  bool IsErodible (HexCell cell) {
    int erodibleElevation = cell.Elevation - 2;
    for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
      HexCell neighbor = cell.GetNeighbor(d);
      if (neighbor != null && neighbor.Elevation <= erodibleElevation) {
        return true;
      }
    }
    return false;
  }

  HexCell GetErosionTarget (HexCell cell) {
    List<HexCell> candidates = ListPool<HexCell>.Get();
    int erodibleElevation = cell.Elevation - 2;
    for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
      HexCell neighbor = cell.GetNeighbor(d);
      if (neighbor != null && neighbor.Elevation <= erodibleElevation) {
        candidates.Add(neighbor);
      }
    }
    HexCell target = candidates[Random.Range(0, candidates.Count)];
    ListPool<HexCell>.Add(candidates);
    return target;
  }

  void CreateClimate () {
    climate.Clear();
    nextClimate.Clear();
    HexClimateData initialData = new HexClimateData();
    initialData.moisture = startingMoisture;
    HexClimateData clearData = new HexClimateData();
    for (int i = 0; i < cellCount; i++) {
      climate.Add(initialData);
      nextClimate.Add(clearData);
    }

    for (int cycle = 0; cycle < climateEvolutionCycles; cycle++) {
      for (int i = 0; i < cellCount; i++) {
        EvolveClimate(i);
      }
      List<HexClimateData> swap = climate;
      climate = nextClimate;
      nextClimate = swap;
    }

    //determine temp, and then set each cells climate data for debugging
    for (int i = 0; i < cellCount; i++) {
      var cell = grid.GetCell(i);
      var cellClimate = climate[i];
      cellClimate.temperature = DetermineTemperature(cell);
      climate[i] = cellClimate;

      cell.climateData = cellClimate; //this should only be needed for debug
    }
  }

  void EvolveClimate (int cellIndex) {
    HexCell cell = grid.GetCell(cellIndex);
    HexClimateData cellClimate = climate[cellIndex];

    if (cell.IsUnderwater) {
      cellClimate.moisture = 1f;
      cellClimate.clouds = Mathf.Clamp01(cellClimate.clouds + evaporationFactor);
    }
    else {
      float evaporation = cellClimate.moisture * evaporationFactor;
      cellClimate.moisture -= evaporation;
      cellClimate.clouds += evaporation;
    }

    float precipitation = cellClimate.clouds * precipitationFactor;
    cellClimate.clouds -= precipitation;
    cellClimate.moisture += precipitation;

    float cloudMaximum = 1f - cell.ViewElevation / (elevationMaximum + 1f);
    if (cellClimate.clouds > cloudMaximum) {
      cellClimate.moisture += cellClimate.clouds - cloudMaximum;
      cellClimate.clouds = cloudMaximum;
    }

    HexDirection mainDispersalDirection = windDirection.Opposite();
    float cloudDispersal = cellClimate.clouds * (1f / (5f + windStrength));
    float runoff = cellClimate.moisture * runoffFactor * (1f / 6f);
    float seepage = cellClimate.moisture * seepageFactor * (1f / 6f);

    for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
      HexCell neighbor = cell.GetNeighbor(d);
      if (neighbor == null) {
        continue;
      }
      HexClimateData neighborClimate = nextClimate[neighbor.Index];
      if (d == mainDispersalDirection) {
        neighborClimate.clouds += cloudDispersal * windStrength;
      }
      else {
        neighborClimate.clouds += cloudDispersal;
      }

      int elevationDelta = neighbor.ViewElevation - cell.ViewElevation;
      if (elevationDelta < 0) {
        cellClimate.moisture -= runoff;
        neighborClimate.moisture += runoff;
      }
      else if (elevationDelta == 0) {
        cellClimate.moisture -= seepage;
        neighborClimate.moisture += seepage;
      }

      nextClimate[neighbor.Index] = neighborClimate;
    }

    HexClimateData nextCellClimate = nextClimate[cellIndex];
    nextCellClimate.moisture += cellClimate.moisture;
    if (nextCellClimate.moisture > 1f) {
      nextCellClimate.moisture = 1f;
    }
    // nextCellClimate.clouds += cellClimate.clouds;
    nextClimate[cellIndex] = nextCellClimate;
    climate[cellIndex] = new HexClimateData();
  }

  void CreateRivers () {
    List<HexCell> riverOrigins = ListPool<HexCell>.Get();
    for (int i = 0; i < cellCount; i++) {
      HexCell cell = grid.GetCell(i);
      if (cell.IsUnderwater) {
        continue;
      }
      var data = climate[i];
      float weight = data.moisture * (cell.Elevation - waterLevel) / (elevationMaximum - waterLevel);
      if (weight > 0.75f) {
        riverOrigins.Add(cell);
        riverOrigins.Add(cell);
      }
      if (weight > 0.5f) {
        riverOrigins.Add(cell);
      }
      if (weight > 0.25f) {
        riverOrigins.Add(cell);
      }
    }

    int originalRiverBudget = Mathf.RoundToInt(landCells * riverPercentage * 0.01f);
    var riverBudget = originalRiverBudget;
    while (riverBudget > 0 && riverOrigins.Count > 0) {
      //TODO: seems like you could just shuffle the river origin list first
      int index = Random.Range(0, riverOrigins.Count);
      int lastIndex = riverOrigins.Count - 1;
      HexCell origin = riverOrigins[index];
      riverOrigins[index] = riverOrigins[lastIndex];
      riverOrigins.RemoveAt(lastIndex);

      if (!origin.HasRiver) {
        //disqualify rivers that are close together to each other or near water
        bool isValidOrigin = true;
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
          HexCell neighbor = origin.GetNeighbor(d);
          if (neighbor != null && (neighbor.HasRiver || neighbor.IsUnderwater)) {
            isValidOrigin = false;
            break;
          }
        }
        if (isValidOrigin) {
          var newRiverLength = CreateRiver(origin);
          riverBudget -= newRiverLength;
          if(newRiverLength > 0){
            origin.IsRiverOrigin = true;
          }
        }
      }
    }

    if (riverBudget > 0) {
      Logger.LogWarning(string.Format("Failed to use up river cell budget: {0}/{1}", riverBudget, originalRiverBudget));
    }

    ListPool<HexCell>.Add(riverOrigins);
  }

  int CreateRiver (HexCell origin) {
    int length = 1;
    HexCell cell = origin;
    HexDirection direction = HexDirection.NE;
    while (!cell.IsUnderwater) {
      int minNeighborElevation = int.MaxValue;
      flowDirections.Clear();
      for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
        HexCell neighbor = cell.GetNeighbor(d);
        if (neighbor == null) {
          continue;
        }

        if (neighbor.Elevation < minNeighborElevation) {
          minNeighborElevation = neighbor.Elevation;
        }

        if (neighbor == origin || neighbor.HasIncomingRiver) {
          continue;
        }

        int delta = neighbor.Elevation - cell.Elevation;
        //rivers must go downhill
        if (delta > 0) {
          continue;
        }

        if (neighbor.HasOutgoingRiver) {
          cell.SetOutgoingRiver(d);
          return length;
        }

        //find next direction for river to flow

        //downhill has most priority
        if (delta < 0) {
          flowDirections.Add(d);
          flowDirections.Add(d);
          flowDirections.Add(d);
        }
        //if it's the origin of the river or the river hasn't turned that much it gets another ticket
        if ( length == 1 || (d != direction.Next2() && d != direction.Previous2())) {
          flowDirections.Add(d);
        }

        //every direction gets a shot
        flowDirections.Add(d);
      }

      if (flowDirections.Count == 0) {
        if (length == 1) {
          return 0;
        }

        if (minNeighborElevation >= cell.Elevation) {
          cell.HexFeature = HexFeature.Lake;
          if (minNeighborElevation == cell.Elevation) {
            cell.Elevation = minNeighborElevation - 1;
          }
        }
        break;
      }

      direction = flowDirections[Random.Range(0, flowDirections.Count)];
      cell.SetOutgoingRiver(direction);
      length += 1;

      if (minNeighborElevation == cell.Elevation && Random.value < extraLakeProbability) {
        cell.HexFeature = HexFeature.Lake;
      }

      cell = cell.GetNeighbor(direction);
    }
    return length;
  }

  void UpdateCellsData () {
    int rockDesertElevation = elevationMaximum / 2;
    int peakElevationMin = elevationMaximum;
    int mountainElevationMin = Mathf.RoundToInt((float)elevationMaximum * 0.75f);
    int hillElevationMin = mountainElevationMin - 1;

    for (int i = 0; i < cellCount; i++) {
      HexCell cell = grid.GetCell(i);
      float temperature = climate[i].temperature;
      float moisture = climate[i].moisture;
      Biome cellBiome = new Biome();
      int tempBand = 0;
      for (; tempBand < temperatureBands.Length; tempBand++) {
        if (temperature < temperatureBands[tempBand]) {
          break;
        }
      }
      if (!cell.IsUnderwater) {
        int moistureBand = 0;
        for (; moistureBand < moistureBands.Length; moistureBand++) {
          if (moisture < moistureBands[moistureBand]) {
            break;
          }
        }
        cellBiome = biomes[tempBand * 4 + moistureBand];

        if (cellBiome.terrainType == HexTerrainType.Sand) {
          if (cell.Elevation >= rockDesertElevation) {
            cellBiome.terrainType = HexTerrainType.Stone;
          }
        }
        else if (cell.Elevation == elevationMaximum) {
          cellBiome.terrainType = HexTerrainType.Snow;
        }

        if (cellBiome.terrainType == HexTerrainType.Snow) {
          cellBiome.plant = 0;
        }
        // else if (cellBiome.plant < 3 && cell.HasRiver) {
        //   cellBiome.plant += 1;
        // }

        cell.TerrainType = cellBiome.terrainType;
        // cell.PlantLevel = cellBiome.plant;

        //hex features
        if(cell.HexFeature != HexFeature.None){
          //do nothing if we've already set a feature earlier
        } else if(cell.Elevation >= peakElevationMin){
          cell.HexFeature = HexFeature.Peak;
        } else if(cell.Elevation >= mountainElevationMin){
          cell.HexFeature = HexFeature.Mountains;
        }else if(cell.Elevation >= hillElevationMin){
          cell.HexFeature = HexFeature.Hills;
        }else if(cellBiome.plant == 1){
          cell.HexFeature = HexFeature.TreesSparse;
        }else if(cellBiome.plant == 2){
          cell.HexFeature = HexFeature.TreesMedium;
        }else if(cellBiome.plant == 3){
          cell.HexFeature = HexFeature.TreesDense;
        }else{
          cell.HexFeature = HexFeature.None;
        }
      }
      else {
        //underwater biomes
        HexTerrainType terrain;

        cellBiome = waterBiomes[tempBand];

        //on potential costal areas look for changes in terrain
        if (cell.Elevation == 0) {
          int cliffs = 0, slopes = 0;
          for ( HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
            HexCell neighbor = cell.GetNeighbor(d);
            if (neighbor == null) {
              continue;
            }
            int delta = neighbor.Elevation - cell.Elevation;
            if (delta == 1) {
              slopes += 1;
            }
            else if (delta > 1) {
              cliffs += 1;
            }
          }

          // if (cliffs + slopes > 3) {
          //   terrain = 1;
          // }
          // else if (cliffs > 0) {
          //   terrain = 3;
          // } else
          if (slopes > 0) {
            terrain = HexTerrainType.Shallows;
          }
          else {
            terrain = HexTerrainType.Water;
          }
        }
        else {
          terrain = HexTerrainType.Water;
        }

        if (cell.Elevation > -2 && temperature < temperatureBands[0]) {
          terrain = HexTerrainType.Ice;
        }
        cell.TerrainType = terrain;
        cell.HexFeature = HexFeature.None;
      }

      AssignBonusResources(cell, cellBiome);

    }
  }

  //Assign in priority of earlier revealed resources, to give them higher chance
  //just by virtue of them rolling before other resources
  void AssignBonusResources (HexCell cell, Biome biome) {
    //Livestock
    if(cell.HexFeature == HexFeature.None || cell.HexFeature == HexFeature.Hills){
      const float grassProb = 0.09f;
      const float mudProb = 0.5f * grassProb;
      var prob = cell.TerrainType == HexTerrainType.Grass ? grassProb : (cell.TerrainType == HexTerrainType.Mud ? mudProb : 0);
      if (Random.value < prob) {
        cell.HexBonusResource = HexBonusResource.Livestock;
        return;
      }
    }

    //Fish
    if(
      (cell.TerrainType == HexTerrainType.Water || cell.TerrainType == HexTerrainType.Shallows)
      && cell.Elevation >= -2
    ){
      if (Random.value < 0.080f) {
        cell.HexBonusResource = HexBonusResource.Fish;
        return;
      }
    }

    //Reefs
    if(
      (cell.TerrainType == HexTerrainType.Shallows && biome.temperature > 1)
    ){
      if (Random.value < 0.09f) {
        cell.HexBonusResource = HexBonusResource.Reef;
        return;
      }
    }

    //Horses
    if(
      (
        cell.HexFeature == HexFeature.None ||
        cell.HexFeature == HexFeature.Hills ||
        cell.HexFeature == HexFeature.TreesSparse ||
        cell.HexFeature == HexFeature.Lake
      )
      && (
        cell.TerrainType == HexTerrainType.Grass ||
        cell.TerrainType == HexTerrainType.Mud
      )
    ){
      var prob = 0.02f;

      if(cell.HasRiver){ prob += 0.03f; }

      for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
        HexCell neighbor = cell.GetNeighbor(d);
        if (neighbor != null && neighbor.HasRiver) {
          prob += 0.01f;
        }
      }

      if (Random.value < prob) {
        cell.HexBonusResource = HexBonusResource.Horses;
        return;
      }
    }

    //Salt
    if(
      (
        cell.HexFeature == HexFeature.None
      )
      && (
        cell.TerrainType == HexTerrainType.Sand ||
        cell.TerrainType == HexTerrainType.Stone
      )
      && biome.moisture <= 2
      && !cell.HasRiver
    ){
      var prob = 0.12f;

      if (Random.value < prob) {
        cell.HexBonusResource = HexBonusResource.Salt;
        return;
      }
    }

    //Grains
    if(cell.HexFeature == HexFeature.None && cell.TerrainType == HexTerrainType.Grass){
      if (Random.value < 0.15f) {
        cell.HexBonusResource = HexBonusResource.Grains;
        return;
      }
    }

    //Stone
    if(
      (
        cell.HexFeature == HexFeature.None ||
        cell.HexFeature == HexFeature.Hills ||
        cell.HexFeature == HexFeature.Mountains ||
        cell.HexFeature == HexFeature.Peak
      )
      && (
        cell.TerrainType == HexTerrainType.Mud ||
        cell.TerrainType == HexTerrainType.Stone ||
        cell.TerrainType == HexTerrainType.Sand
      )
      && !cell.HasRiver
    ){
      var prob = 0.0f;
      if(cell.HexFeature == HexFeature.Hills){ prob += 0.03f; }
      if(cell.HexFeature == HexFeature.Mountains){ prob += 0.04f; }
      if(cell.HexFeature == HexFeature.Peak){ prob += 0.05f; }

      if(cell.TerrainType == HexTerrainType.Stone){ prob += 0.05f; }
      if(cell.TerrainType == HexTerrainType.Sand){ prob += 0.01f; }

      if (Random.value < prob) {
        cell.HexBonusResource = HexBonusResource.Stone;
        return;
      }
    }

    //Cotton
    if((cell.HexFeature == HexFeature.None || cell.HexFeature == HexFeature.TreesSparse) &&
        cell.TerrainType == HexTerrainType.Grass &&
        !cell.HasRiver
    ){
      var prob = 0.04f;

      for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
        HexCell neighbor = cell.GetNeighbor(d);
        if (neighbor != null && neighbor.HasRiver) {
          prob += 0.05f;
        }
      }

      if (Random.value < prob) {
        cell.HexBonusResource = HexBonusResource.Cotton;
        return;
      }
    }

    //Sugar
    if(
      (
        cell.HexFeature == HexFeature.TreesSparse ||
        cell.HexFeature == HexFeature.TreesMedium ||
        cell.HexFeature == HexFeature.TreesDense
      )
    ){
      var prob = 0f;

      if(cell.HexFeature == HexFeature.TreesSparse){ prob = 0.02f; }
      if(cell.HexFeature == HexFeature.TreesMedium){ prob = 0.03f; }
      if(cell.HexFeature == HexFeature.TreesDense){ prob = 0.05f; }

      if(cell.HasRiver){ prob += 0.03f; }

      if (Random.value < prob) {
        cell.HexBonusResource = HexBonusResource.Sugar;
        return;
      }
    }

    //Grapes
    if(
      (
        cell.HexFeature == HexFeature.None ||
        cell.HexFeature == HexFeature.Hills
      )
      && (
        cell.TerrainType == HexTerrainType.Grass ||
        cell.TerrainType == HexTerrainType.Stone
      )
      && biome.moisture >= 1
      && biome.moisture <= 2
    ){
      var prob = 0.06f;

      if (Random.value < prob) {
        cell.HexBonusResource = HexBonusResource.Grapes;
        return;
      }
    }

    //Iron
    if(
      (
        cell.HexFeature == HexFeature.None ||
        cell.HexFeature == HexFeature.Hills ||
        cell.HexFeature == HexFeature.Mountains ||
        cell.HexFeature == HexFeature.Peak ||
        cell.HexFeature == HexFeature.TreesSparse ||
        cell.HexFeature == HexFeature.TreesMedium ||
        cell.HexFeature == HexFeature.TreesDense
      )
      && (
        cell.TerrainType == HexTerrainType.Mud ||
        cell.TerrainType == HexTerrainType.Stone ||
        cell.TerrainType == HexTerrainType.Snow
      )
    ){
      var prob = 0.03f;

      if (Random.value < prob) {
        cell.HexBonusResource = HexBonusResource.Iron;
        return;
      }
    }

    //Gold
    if(
      (
        cell.HexFeature == HexFeature.None ||
        cell.HexFeature == HexFeature.Hills ||
        cell.HexFeature == HexFeature.Mountains ||
        cell.HexFeature == HexFeature.Peak
      )
      && !cell.IsUnderwater
    ){
      var prob = 0.03f;

      if(cell.HasRiver){ prob += 0.02f; }

      if (Random.value < prob) {
        cell.HexBonusResource = HexBonusResource.Gold;
        return;
      }
    }

    //Coal
    if(
      (
        cell.HexFeature == HexFeature.Hills ||
        cell.HexFeature == HexFeature.TreesSparse ||
        cell.HexFeature == HexFeature.TreesMedium ||
        cell.HexFeature == HexFeature.TreesDense
      )
      && (
        cell.Elevation > 0
      )
    ){
      var prob = 0.027f;

      if (Random.value < prob) {
        cell.HexBonusResource = HexBonusResource.Coal;
        return;
      }
    }

    //Oil
    if(
      (
        cell.HexFeature != HexFeature.Hills &&
        cell.HexFeature != HexFeature.Mountains &&
        cell.HexFeature != HexFeature.Peak
      )
    ){
      if (Random.value < 0.02f) {
        cell.HexBonusResource = HexBonusResource.Oil;
        return;
      }
    }

    //Aluminum
    if(
      (
        cell.HexFeature == HexFeature.Hills ||
        cell.HexFeature == HexFeature.Mountains ||
        cell.HexFeature == HexFeature.Peak
      )
      && !cell.IsUnderwater
      && !cell.HasRiver
    ){
      var prob = 0.06f;

      if (Random.value < prob) {
        cell.HexBonusResource = HexBonusResource.Aluminum;
        return;
      }
    }

    //Uranium
    if(
      !cell.IsUnderwater &&
      GetLatitude(cell) <= 0.35f
    ){
      if (Random.value < 0.027f) {
        cell.HexBonusResource = HexBonusResource.Uranium;
        return;
      }
    }

  }

  //Go through all the water tiles and find the ones that are surrounded by land,
  //and then mark them as freshwater
  void FindFreshwater(){
    List<HexCell> neighborWaterQueue = new List<HexCell>();
    HashSet<HexCell> oceanCells = new HashSet<HexCell>();

    for (int cellIdx = 0; cellIdx < cellCount; cellIdx++) {
      HexCell cell = grid.GetCell(cellIdx);

      // Ignore all cells that are land or already found as ocean or freshwater
      if(!cell.IsUnderwater ||
         oceanCells.Contains(cell) ||
         cell.Freshwater
      ){
        continue;
      }
      neighborWaterQueue.Clear();

      for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
        HexCell neighbor = cell.GetNeighbor(d);
        if(neighbor == null){
          //no neighbor means off the map and this should be seawater
          oceanCells.Add(cell);
        } else if(neighbor.IsUnderwater){
          if(oceanCells.Contains(neighbor)){
            //touching neighbor ocean cell means we're also ocean
            oceanCells.Add(cell);
            break;
          }else{
            neighborWaterQueue.Add(neighbor);
          }
        }
      }

      if(neighborWaterQueue.Count > 0){
        if(oceanCells.Contains(cell)){
          //If we found the cell is actually ocean then mark all the neighbors as ocean and continue
          foreach(var neighbor in neighborWaterQueue){
            oceanCells.Add(neighbor);
          }
          continue;
        }else{
          //iterate the neighbor queue either looking for other connecting ocean
          //or being trapped in by land to indicate freshwater.
          //Start by assuming fresh if the cell isn't already in the ocean
          var isFresh = !oceanCells.Contains(cell);
          for(var n = 0; n < neighborWaterQueue.Count; n++){
            HexCell queueCell = neighborWaterQueue[n];

            //Check for indications of not being fresh
            if(queueCell == null || oceanCells.Contains(queueCell)){
              isFresh = false;
              // break;  //removing break so that all connecting water can be explored
            }

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
              HexCell queueNeighbor = queueCell.GetNeighbor(d);

              if(queueNeighbor == null || oceanCells.Contains(queueNeighbor)){
                isFresh = false;
              }else if(queueNeighbor.IsUnderwater && !neighborWaterQueue.Contains(queueNeighbor)){
                neighborWaterQueue.Add(queueNeighbor);
              }

            }
          }

          //Now mark all the neighbor queue as either fresh or not
          for(var n = 0; n < neighborWaterQueue.Count; n++){
            HexCell queueCell = neighborWaterQueue[n];
            if(queueCell == null){ continue; }

            if(isFresh){
              queueCell.Freshwater = true;
            }else{
              oceanCells.Add(queueCell);
            }
          }
        }
      }else{
        if(!oceanCells.Contains(cell)){
          //if we haven't already found this cell in the ocean and there's no water neighbors to explore
          //this must mean we're freshwater
          cell.Freshwater = true;
        }
      }
    }

    // Second pass to mark all rivers and lake tiles as freshwater
    for (int i = 0; i < cellCount; i++) {
      HexCell cell = grid.GetCell(i);

      if(!cell.IsUnderwater && (cell.HasRiver || cell.HexFeature == HexFeature.Lake)){
        cell.Freshwater = true;
      }
    }
  }

  float DetermineTemperature (HexCell cell) {
    float latitude = GetLatitude(cell);

    float temperature =
      Mathf.LerpUnclamped(lowTemperature, highTemperature, latitude);

    temperature *= 1f - (cell.ViewElevation - waterLevel) /
      (elevationMaximum - waterLevel + 1f);

    float jitter = SampleNoise(cell.localPosition * 0.1f);

    temperature += (jitter * 2f - 1f) * temperatureJitter;

    return temperature;
  }

  float GetLatitude(HexCell cell){
    float latitude = (float)cell.coordinates.Z / grid.height;
    if (hemisphere == HemisphereMode.Both) {
      latitude *= 2f;
      if (latitude > 1f) {
        latitude = 2f - latitude;
      }
    }
    else if (hemisphere == HemisphereMode.North) {
      latitude = 1f - latitude;
    }
    return latitude;
  }

  HexCell GetRandomCell (MapRegion region) {
    return grid.GetCell(
      Random.Range(region.xMin, region.xMax),
      Random.Range(region.zMin, region.zMax)
    );
  }

  public void UpdateCellDisplay(){
    if(grid.cellCount == 0){
      grid.FindGrid();
    }
    grid.UpdateDisplay(cellDrawMode);
  }

  public const float noiseScale = 0.003f;

  public float SampleNoise(Vector3 position){
    return Mathf.PerlinNoise(position.x * noiseScale, position.z * noiseScale);
  }
}