using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;

public class HexGrid : MonoBehaviour {

  public int width = 60;
  public int height = 60;

  public Transform cellHolder;
  public Transform riverHolder;
  public Transform cityHolder;
  public Transform roadHolder;
  public Transform unitsHolder;

  public HexCellDisplay cellPrefab;
  public HexRiverOrRoad riverPrefab;
  public HexRiverOrRoad roadPrefab;
  public HexRiverOrRoad seaConnectionPrefab;

  HexCell[] cells;

  public HexCell[] Cells {get { return cells; }}

  int cellsInstanciatedCount = 0;
  int spawnedRingRadius = 1;
  bool spawningCells = false;
  Bounds? spawningFromBounds;

  HexPathCache pathCache = new HexPathCache();
  List<HexRiverOrRoad> riverAndRoads = new List<HexRiverOrRoad>();
  YieldInstruction eof;

  void Awake () {
    eof = new WaitForEndOfFrame();

    //Find existing cells if they exist
    if(cellHolder.childCount > 0){
      FindGrid();
    }
  }

  void Start () {
  }

  protected void OnEnable(){
    //Have to check if we need to resume creating cells.
    //This can happen if you open the tech manager or other screen that disables the world view before we're finished spawning
    if(spawningCells){
      StartCoroutine(SpawnCellDisplays(spawningFromBounds));
    }
  }

  protected void OnDisable(){
    pathCache.Clear();
  }

  public bool IsInitialized {
    get{ return cells != null && cells.Length > 0; }
  }

  public bool FinishedSpawningCells {
    get { return cells != null && cellsInstanciatedCount == cells.Length; }
  }

  public void FindGrid(){
    if(cellHolder.childCount > 0 && cellCount == 0){
      cells = new HexCell[cellHolder.childCount];
      for(int c = 0; c < cellHolder.childCount; c++){
        var cellDisplay = cellHolder.GetChild(c).GetComponent<HexCellDisplay>();
        cells[c] = cellDisplay.hexCell;
        cells[c].Index = c;
      }
    }
  }


  public void CreateGrid(){
    instTime = 0f;
    cells = new HexCell[height * width];

    for (int z = 0, i = 0; z < height; z++) {
      for (int x = 0; x < width; x++) {
        CreateCell(x, z, i++);
      }
    }

    Debug.Log(string.Format("Grid Create Time, inst: {0}", instTime));
  }

  public void LoadGrid(HexCell[] loadedCells){
    cells = loadedCells;
    for(int i = 0; i < loadedCells.Length; i++){
      SetupCell(cells[i], i);
    }
  }

  public void ClearGrid(){
    cellHolder.DestroyChildren(true);
    riverHolder.DestroyChildren(true);
    cityHolder.DestroyChildren(true);
    unitsHolder.DestroyChildren(true);
    roadHolder.DestroyChildren(true);

    cells = null;
    cellsInstanciatedCount = 0;
  }

  // Call after the grid has been created and restored, and we're ready to make game objects
  // Do the cell spawning out of the main create grid function since it takes so long to spawn all of them
  float spawnCellsStartTime;
  public void StartCreatingCells(Bounds? bounds = null){
    Debug.Log("Hex grid starting to spawn cells");
    spawningCells = true;
    spawningFromBounds = bounds;
    spawnCellsStartTime = Time.realtimeSinceStartup;
    StartCoroutine(SpawnCellDisplays(bounds));
  }

  IEnumerator SpawnCellDisplays(Bounds? bounds){
    HexCell centerCell;
    if(bounds.HasValue){
      centerCell = GetCell(bounds.Value.center);
    }else{
      centerCell = GetCenterCell();
    }

    SpawnCellDisplay(centerCell);
    var time = Time.realtimeSinceStartup;

    var skipTimeCheckForEditor = Application.isEditor && !Application.isPlaying;

    //null check should only be necessary for the rare case where we try to clean up as cells are spawning
    while(cells != null && cellsInstanciatedCount < cells.Length) {

      var ring = GetRing(centerCell, spawnedRingRadius);
      foreach(var cell in ring){
        SpawnCellDisplay(cell);

        //Spawn as much as we can, but skip while calling from editor outside of play mode for testing since it gets stuck
        var timeDiff = Time.realtimeSinceStartup - time;
        if(Time.realtimeSinceStartup - time > Constants.spawnTimeFrameBudget && !skipTimeCheckForEditor){
          yield return eof;
          time = Time.realtimeSinceStartup;
        }
      }

      spawnedRingRadius++;
    }

    spawningCells = false;
    Debug.Log(string.Format("Total Spawn Cell Time: {0}", Time.realtimeSinceStartup - spawnCellsStartTime));
  }

  void SpawnCellDisplay(HexCell cell){
    if(cell.display != null){
      return;
    }

    HexCellDisplay cellDisplay = Instantiate<HexCellDisplay>(cellPrefab, cellHolder);
    cell.display = cellDisplay;
    cellDisplay.hexCell = cell;
    cellDisplay.transform.localPosition = cell.localPosition;

    cellDisplay.UpdateTerrainDisplay();

    if(cell.IsRiverOrigin){
      CreateRiver(cell);
    }

    cellsInstanciatedCount++;
  }

  public void ColorCell (Vector3 position, Color color) {
    var cell = GetCell(position);
    ColorCell(cell, color);
  }

  public void ColorCell(HexCell cell, Color color){
    if(cell != null){
      cell.display.cellRenderer.color = color;
    }
  }

  int CoordsToIndex(HexCoordinates coordinates){
    return coordinates.X + coordinates.Z * width + coordinates.Z / 2;
  }

  public HexCell GetCell(Vector3 worldPos){
    var position = cellHolder.InverseTransformPoint(worldPos);
    HexCoordinates coordinates = HexCoordinates.FromPosition(position);
    return GetCell(coordinates);
  }

  public HexCell GetCell(HexCoordinates coordinates){
    var idx = CoordsToIndex(coordinates);
    if(idx < cells.Length && idx >= 0){
      return cells[idx];
    }
    return null;
  }

  //Offset != Hex coords
  public HexCell GetCell (int xOffset, int zOffset) {
    return cells[xOffset + zOffset * width];
  }

  public HexCell GetCell (int cellIndex) {
    return cells[cellIndex];
  }

  public IEnumerable<HexCell> GetCells(){
    for (int i = 0; i < cellCount; i++) {
      yield return GetCell(i);
    }
  }

  public int cellCount {
    get{ return cells != null ? cells.Length : 0; }
  }

  public HexCell GetCenterCell(){
    var mapBounds = GetBounds();
    return GetCell(mapBounds.center).GetNeighbor(HexDirection.SE);
  }

  float instTime = 0f;

  void CreateCell (int x, int z, int i) {
    var instStart = Time.realtimeSinceStartup;
    HexCell cell = cells[i] = new HexCell();

    cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
    cell.Index = i;
    cell.data = new HexCellData(){
      coordinates = cell.coordinates,
      exploreStatus = HexExploreStatus.Unexplored
    };
    var instEnd = Time.realtimeSinceStartup;
    instTime += instEnd - instStart;

    SetupCell(cell, i);
  }

  //Set up all the runtime only (not saved) references
  void SetupCell(HexCell cell, int i){
    Vector3 position;

    //Convert back to the offset coordinates
    var z = cell.coordinates.Z;
    var x = cell.coordinates.X + z / 2;

    position.x = (x + z * 0.5f - z / 2) * (HexCoordinates.innerRadius * 2f);
    position.y = cell.coordinates.Z * (HexCoordinates.outerRadius * 1.5f);
    position.z = 0f;

    cell.localPosition = position;

    //set up neighbors
    if (x > 0) {
      cell.SetNeighbor(HexDirection.W, cells[i - 1]);
    }
    if (z > 0) {
      if ((z & 1) == 0) {
        cell.SetNeighbor(HexDirection.SE, cells[i - width]);
        if (x > 0) {
          cell.SetNeighbor(HexDirection.SW, cells[i - width - 1]);
        }
      }
      else {
        cell.SetNeighbor(HexDirection.SW, cells[i - width]);
        if (x < width - 1) {
          cell.SetNeighbor(HexDirection.SE, cells[i - width + 1]);
        }
      }
    }
  }

  //World coord bounds
  public Bounds GetBounds(){

    var cellWidth = HexCoordinates.innerRadius * 2f;
    var cellHeight = HexCoordinates.outerRadius * 2f;

    var blPos = Vector2.zero;
    var trPos = new Vector2(
      width * cellWidth - (cellWidth / 2f),
      cellHeight * 0.75f * (height - 1)
    );

    var center = new Vector2((trPos.x - blPos.x) / 2f + blPos.x, (trPos.y - blPos.y) / 2f + blPos.y );
    var size = trPos - blPos;

    return new Bounds(center, size);
  }

  public IEnumerable<HexCell> GetRing(HexCell center, int radius){
    if(radius <= 0){
      yield break;
    }
    var ringCoord = center.coordinates + (HexDirection.W.Offset() * radius);

    for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
      for(int j = 0; j < radius; j++){
        var cell = GetCell(ringCoord);
        if(cell != null){
          yield return cell;
        }
        ringCoord = HexCoordinates.Neighbor(ringCoord, d);
      }
    }
  }

  public IEnumerable<HexCell> GetNeighborsInRange(HexCell center, int radius, bool includeCenter){
    if(includeCenter){
      yield return center;
    }
    if(radius <= 0){
      yield break;
    }
    for(int r = 1; r <= radius; r++){
      var ring = GetRing(center, r);
      foreach(var cell in ring) yield return cell;
    }
  }

  public void UpdateDisplay(CellDrawMode drawMode = CellDrawMode.None, bool showFeatures = true){
    riverHolder.DestroyChildren(true);

    for (int i = 0; i < cellCount; i++) {
      HexCell cell = GetCell(i);
      if(cell.display != null){
        cell.display.UpdateTerrainDisplay(drawMode);
      }
    }

    //Create rivers now. Have to do this separately so the colors are set right
    for (int i = 0; i < cellCount; i++) {
      HexCell cell = GetCell(i);
      if(cell.IsRiverOrigin){
        CreateRiver(cell);
      }
    }
  }

  HexRiverOrRoad SpawnRiverOrRoad(CityBuildingId connectionBuildingId){
    var prefab = connectionBuildingId == CityBuildingId.River ? riverPrefab :
      (connectionBuildingId == CityBuildingId.WaterTradeRoute ? seaConnectionPrefab : roadPrefab);
    var parent = connectionBuildingId == CityBuildingId.River ? riverHolder : roadHolder;
    var riverRoad = Instantiate<HexRiverOrRoad>(prefab, parent);
    riverRoad.transform.SetParent(parent, false);
    riverRoad.transform.localPosition = Vector3.zero;
    riverRoad.connectionBuildingId = connectionBuildingId;
    riverRoad.Init();

    riverAndRoads.Add(riverRoad);
    return riverRoad;
  }

  void CreateRiver(HexCell origin){
    var river = SpawnRiverOrRoad(CityBuildingId.River);

    river.SetTilePoints(GetRiverTilePoints(origin).ToList());


    //Kinda sucks to iterate this again to just get the destination cell but oh well
    var nextCell = origin;
    var dest = origin;
    while(nextCell != null){
      dest = nextCell;

      nextCell = nextCell.NextRiverCell;
    }
    if(dest.HexFeature == HexFeature.Lake){
      river.SetColor(Colors.lake);
    }else{
      river.SetColor(HexCellDisplay.GetCellDisplayColor(dest));
    }
  }

  //Entrypoint for creating a connection between two places. If the connection type is upgradable, then we have to
  //make a bunch of delete and upgrade checks
  public void CreateRiverOrConnection(ConnectionParams connectionParams){
    var connectionBuildingId = connectionParams.connectionBuildingId;
    var connectionIsUpgradable = HexRiverOrRoad.connectionTier.ContainsKey(connectionBuildingId);

    if(!connectionIsUpgradable){
      CreateIndividualRiverOrConnection(connectionParams);
      return;
    }

    var newConnectionTier = HexRiverOrRoad.connectionTier[connectionBuildingId];
    //now the fun begins to figure out if we need to blow up connections and start from scratch

    //1. first traverse the path seeing if we hit any roads of lower upgrade tier
    var path = FindPath(GetPathfindOptionsForConnection(connectionParams));

    if(path == null){
      //Only should get here in the "fixing data created with buggy code" case lol
      Debug.LogWarning($"Could not find path from {connectionParams.origin} to {connectionParams.dest} for {connectionParams.connectionBuildingId}");
      return;
    }

    var cellsWithLowerTierRoads = new List<HexCell>();
    foreach(var pointInPath in path){
      var pathCell = GetCell(pointInPath);

      var pathConnectionTier = pathCell.LowestConnectionTier();
      if(pathConnectionTier.HasValue && pathConnectionTier < newConnectionTier){
        cellsWithLowerTierRoads.Add(pathCell);
      }
    }

    //2. if no lower tier roads along the path we can just do the easy case and be done
    if(cellsWithLowerTierRoads.Count == 0){
      CreateIndividualRiverOrConnection(connectionParams);
      return;
    }

    //3. if there were lower tier roads, we now have to BFS and find all the connected cities
    // including ones that are connected by higher tier roads because lower tiers can merge into higher
    var connectedCells = new HashSet<HexCell>();
    var connectedCities = new HashSet<HexCity>();
    while(cellsWithLowerTierRoads.Count > 0){
      var current = cellsWithLowerTierRoads[0];
      cellsWithLowerTierRoads.RemoveAt(0);

      connectedCells.Add(current);

      //Don't continue searching past cities
      if(current.city != null){
        connectedCities.Add(current.city);
        continue;
      }

      for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
        HexCell neighbor = current.GetNeighbor(d);
        if(neighbor == null) { continue; }
        if(connectedCells.Contains(neighbor)) { continue; }

        var neighborHasUpgradableConnection = neighbor.connectionBuildings != null &&
          neighbor.connectionBuildings.Any(cb => HexRiverOrRoad.connectionTier.ContainsKey(cb));
        if(neighborHasUpgradableConnection){
          cellsWithLowerTierRoads.Add(neighbor);
        }
      }
    }

    //4. then we have to remove all road segments that were part of the road network of a lower tier
    for(var i = riverAndRoads.Count - 1; i >= 0; i--){
      var riverRoad = riverAndRoads[i];
      if(
        HexRiverOrRoad.connectionTier.ContainsKey(riverRoad.connectionBuildingId) &&
        HexRiverOrRoad.connectionTier[riverRoad.connectionBuildingId] < newConnectionTier &&
        riverRoad.Cells.All(riverRoadCell => connectedCells.Contains(riverRoadCell))
      ){
        RemoveRiverOrRoad(riverRoad);
      }
    }

    //5. then we can build the road that was originally asked for in the function
    CreateIndividualRiverOrConnection(connectionParams);

    //6. then for all cities that were identified in #3 rebuild their lower tier connections to other cities in the set
    //   which should end up merging with the new road.
    //   Specifically exclude ones that match the original origin and dest because #5
    //   Build them in order of the highest to lowest tiers so they stack and connect properly
    var connectionsToBuild = new List<ConnectionParams>();
    foreach(var city in connectedCities){
      foreach(var outboundConnection in city.data.outgoingConnections){
        if((city.data.coordinates == connectionParams.origin && outboundConnection.dest == connectionParams.dest) ||
           (city.data.coordinates == connectionParams.dest   && outboundConnection.dest == connectionParams.origin)
        ){
          continue;
        }

        if(!connectedCities.Any(c => c.coordinates == outboundConnection.dest)){
          continue;
        }

        if(
          HexRiverOrRoad.connectionTier.ContainsKey(outboundConnection.connectionBuildingId) &&
          HexRiverOrRoad.connectionTier[outboundConnection.connectionBuildingId] < newConnectionTier
        ){
          connectionsToBuild.Add(new ConnectionParams(){
            origin = city.data.coordinates,
            dest = outboundConnection.dest,
            connectionBuildingId = outboundConnection.connectionBuildingId,
            originCity = city
          });
        }
      }
    }
    connectionsToBuild = connectionsToBuild
      .OrderByDescending(cpm =>
        HexRiverOrRoad.connectionTier.ContainsKey(cpm.connectionBuildingId) ?
        HexRiverOrRoad.connectionTier[cpm.connectionBuildingId] :
        0
      ).ToList();

    foreach(var cpm in connectionsToBuild){
      CreateIndividualRiverOrConnection(cpm);
    }
  }

  public struct ConnectionParams {
    public HexCoordinates origin;
    public HexCoordinates dest;
    public CityBuildingId connectionBuildingId;

    public HexCity originCity;
  }

  //Get a connection from origin to dest, utilizing existing connections if they're along the path. Can create multiple
  //segments if that's the case.
  void CreateIndividualRiverOrConnection(ConnectionParams connectionParams){
    HexRiverOrRoad road = null;

    var path = FindPath(GetPathfindOptionsForConnection(connectionParams));
    if(path == null){
      //Only should get here in the "fixing data created with buggy code" case lol
      Debug.LogWarning($"Could not find path from {connectionParams.origin} to {connectionParams.dest} for {connectionParams.connectionBuildingId}");
      return;
    }

    var connectionBuildingId = connectionParams.connectionBuildingId;
    var fromCell = GetCell(connectionParams.origin);
    var roadPoints = new List<HexCell>(){fromCell};

    //Walk the path and see if any of the cells already have a road,
    //in which case we need to stop the current road, follow the road along the path as far as possible
    //and make a new road if it diverges
    foreach(var pointInPath in path){
      var pathCell = GetCell(pointInPath);

      var existingConnectionToMergeInto = pathCell.HasConnection(connectionBuildingId);
      if(
        !existingConnectionToMergeInto
        && pathCell.connectionBuildings != null
        && pathCell.connectionBuildings.Count > 0
        && HexRiverOrRoad.connectionTier.ContainsKey(connectionBuildingId)
      ){
        //Check to see if there are any higher tier connections to merge into
        existingConnectionToMergeInto = pathCell.connectionBuildings.Any(cb =>
          HexRiverOrRoad.connectionTier.ContainsKey(cb) &&
          HexRiverOrRoad.connectionTier[cb] >= HexRiverOrRoad.connectionTier[connectionBuildingId]
        );
      }

      if(existingConnectionToMergeInto){
        //If we've started a road and we hit an existing road of this type tie the current one off
        if(roadPoints.Count > 1){
          if(road == null){
            road = SpawnRiverOrRoad(connectionBuildingId);
          }
          roadPoints.Add(pathCell);
          SetRoadTilePoints(road, roadPoints);
          road = null;
          roadPoints.Clear();
        }

        //While we're traversing the road, keep the last visited tile as the first roadPoint
        if(roadPoints.Count == 0){
          roadPoints.Add(pathCell);
        }else{
          roadPoints[0] = pathCell;
        }
      }else{
        if(road == null){
          road = SpawnRiverOrRoad(connectionBuildingId);
        }
        roadPoints.Add(pathCell);
      }
    }

    //End the current road after traversing the path
    if(road != null){
      SetRoadTilePoints(road, roadPoints);
    }
  }

  void SetRoadTilePoints(HexRiverOrRoad road, List<HexCell> cells){
    //Make sure to record connection building for upgradable roads
    foreach(var cell in cells){
      cell.AddConnection(road.connectionBuildingId);
    }

    road.SetTilePoints(cells);
  }

  void RemoveRiverOrRoad(HexRiverOrRoad riverOrRoad){
    foreach(var cell in riverOrRoad.Cells){
      cell.connectionBuildings.Remove(riverOrRoad.connectionBuildingId);
    }

    riverAndRoads.Remove(riverOrRoad);
    Destroy(riverOrRoad.gameObject);
  }

  public PathfindOptions GetPathfindOptionsForConnection(ConnectionParams connectionParams){

    var building = CityBuilding.allBuildings[connectionParams.connectionBuildingId];
    var requiresOcean = connectionParams.connectionBuildingId == CityBuildingId.River ? false :
     building.requiresOcean ?? false;

    return new HexGrid.PathfindOptions{
      src = connectionParams.origin,
      dest = connectionParams.dest,
      canMoveOnLand  =  true,
      canMoveOnWater =  requiresOcean,
      avoidLand      =  requiresOcean,
      maxTileDistance    = building.maxConnectionDistance,
      //Setting a max move cost prevents the route from going entirely over land despite high cost
      //This should limit the pathfiding from taking land paths outside the area of influence
      maxMoveCost    = HexCell.AvoidMoveCost * (connectionParams.originCity != null ? connectionParams.originCity.AreaOfInfluence : 1)
    };
  }

  IEnumerable<HexCell> GetRiverTilePoints(HexCell origin){
    var nextCell = origin;
    while(nextCell != null){
      yield return nextCell;

      nextCell = nextCell.NextRiverCell;
    }
  }

  public struct PathfindOptions {
    public HexCoordinates src;
    public HexCoordinates dest;
    public bool canMoveOnWater;
    public bool canMoveOnLand;
    public bool avoidWater;
    public bool avoidLand;
    public int? maxTileDistance; //Max tile distance away before giving up
    public int? maxMoveCost; //Max cumulative move cost before giving up
  }

  public List<HexCoordinates> FindPath(PathfindOptions options){
    if(pathCache.HasPath(options)){
      return pathCache.GetPath(options);
    }

    var fromCell = GetCell(options.src);
    var toCell = GetCell(options.dest);

    var startTime = Time.realtimeSinceStartup;
    var success = Search(fromCell, toCell, options);
    var endTime = Time.realtimeSinceStartup;

    // Debug.Log(string.Format("Pathfinding took {0}. Cells Searched {1}", endTime - startTime, cellsSearched));

    if(success){
      var retList = new List<HexCoordinates>();
      //walk the dest back to the src and then reverse
      for(var cur = toCell; cur != fromCell; cur = cur.PathFrom){
        retList.Add(cur.coordinates);
      }
      retList.Reverse();
      pathCache.AddPath(options, retList);
      return retList;
    }else{
      // Debug.LogWarning("Could not find path.");
      pathCache.AddPath(options, null);
      return null;
    }
  }

  PriorityQueue<HexCell> frontier;
  Dictionary<HexCell, int> costSoFar = new Dictionary<HexCell, int>();
  int cellsSearched = 0;

  bool Search(
    HexCell fromCell,
    HexCell toCell,
    PathfindOptions options
  ) {

    //First check max distance
    var cityDistance = fromCell.coordinates.DistanceTo(toCell.coordinates);
    if(cityDistance > options.maxTileDistance){
      return false;
    }

    cellsSearched = 0;
    costSoFar.Clear();

    if (frontier == null) {
      frontier = new PriorityQueue<HexCell>();
    }
    else {
      frontier.Clear();
    }

    //reset all cells search phase
    foreach (var cell in GetCells()) {
      cell.SearchTileDistance = 0;
      cell.Distance = 0;
    }

    frontier.Enqueue(fromCell, 0);
    while (!frontier.IsEmpty()) {
      HexCell current = frontier.Dequeue();
      cellsSearched++;

      if (current == toCell) {
        return true;
      }
      if(options.maxTileDistance.HasValue && current.SearchTileDistance > options.maxTileDistance.Value){
        continue;
      }
      if(options.maxMoveCost.HasValue && current.Distance > options.maxMoveCost.Value){
        continue;
      }


      for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
        HexCell neighbor = current.GetNeighbor(d);
        if ( neighbor == null ) {
          continue;
        }

        //tile move rules
        int moveCost = neighbor.ExploreStatus == HexExploreStatus.Unexplored ?
          HexCell.BaseMoveCost :
          neighbor.MoveCost(options);
        //Don't consider tiles that we aren't able to move to, but ignore for dest so water routes can work
        if (moveCost < 0 && neighbor != toCell) {
          continue;
        }

        int distance = current.Distance + moveCost;

        if(!costSoFar.ContainsKey(neighbor) || distance < costSoFar[neighbor]){
          neighbor.Distance = distance;
          costSoFar[neighbor] = distance;
          neighbor.SearchTileDistance = current.SearchTileDistance + 1;

          var priority = distance + neighbor.coordinates.DistanceTo(toCell.coordinates);
          frontier.Enqueue(neighbor, priority);
          neighbor.PathFrom = current;
        }
      }
    }
    return false;
  }

  //Find each cities area of influence that flood fills out from each city to a max distance based on the city pop
  public Dictionary<HexCell, HexCity> FindCityInfluencedTiles(List<HexCity> cities) {
    var frontier = new List<HexCell>();
    costSoFar.Clear();
    var seed = new Dictionary<HexCell, HexCity>();

    //Set the distance to 0 at all start poitns and each start point its own seed
    foreach(var city in cities){
      frontier.Add(city.Cell);
      costSoFar[city.Cell] = 0;
      seed[city.Cell] = city;
    }

    while (frontier.Count > 0) {
      var current = frontier[0];
      frontier.RemoveAt(0);
      var maxDistance = seed[current].AreaOfInfluence;

      for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
        HexCell next = current.GetNeighbor(d);
        if(next == null){ continue; }
        if(!costSoFar.ContainsKey(next) && costSoFar[current] < maxDistance){
          costSoFar[next] = costSoFar[current] + 1;
          seed[next] = seed[current];
          frontier.Add(next);
        }
      }
    }

    return seed;
  }
}