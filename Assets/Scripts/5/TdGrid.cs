using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TdGrid {

  public TdTile[,] tiles;

  public int Width{ get{ return width; }}
  public int Height{ get{ return height; }}
  public float TileWidth{ get{ return tileWidth; }}
  public int NumPaths{ get{ return numPaths; }}

  int width;
  int height;
  float tileWidth;
  int numPaths;

  public static Int2[] neighborOffsets = new Int2[]{
    // new Int2(-1, -1),
    new Int2(-1, 0),
    // new Int2(-1, 1),
    new Int2(0, 1),
    // new Int2(1, 1),
    new Int2(1, 0),
    // new Int2(1, -1),
    new Int2(0, -1),
  };

  public void Create(int width, int height, float tileSpacing, GameObject tilePrefab, GameObject linePrefab, Transform parent, int numPaths){
    tiles = new TdTile[width, height];
    this.width = width;
    this.height = height;
    this.numPaths = numPaths;
    tileWidth = tileSpacing;
    var gridHalfWidth = (tileSpacing * (float)width * 0.5f);
    var gridHalfHeight = (tileSpacing * (float)height * 0.5f);

    for(int w = 0; w < width; w++){
      for(int h = 0; h < height; h++){

        var newTile = GameObject.Instantiate(
          tilePrefab,
          Vector3.one,
          Quaternion.identity
        );
        newTile.transform.SetParent(parent, false);
        newTile.transform.localPosition = new Vector3(
          w * tileSpacing - gridHalfWidth,
          h * tileSpacing - gridHalfHeight,
          0
        );

        tiles[w,h] = new TdTile(){
          pos = new Int2(w, h),
          bounds = new Bounds(newTile.transform.localPosition, new Vector3(tileWidth, tileWidth)),
          passable = true,
          buildable = true,
          go = newTile,
          svgRenderer = newTile.GetComponentInChildren<SpriteRenderer>(),
          nextPathPos = new Int2[numPaths],
          pathDist    = new int[numPaths],
          validPath   = new bool[numPaths],
        };
      }
    }

    //create grid lines
    var lineOffset = tileSpacing / 2f;
    for(int w = 1; w < width; w++){
      var newLine = GameObject.Instantiate(
        linePrefab,
        Vector3.zero,
        Quaternion.identity
      );
      newLine.transform.SetParent(parent, false);
      var lineRenderer = newLine.GetComponent<LineRenderer>();

      lineRenderer.SetPositions(new Vector3[]{
        new Vector3(
          w * tileSpacing - gridHalfWidth - lineOffset,
          0 * tileSpacing - gridHalfHeight - lineOffset,
          0
        ),
        new Vector3(
          w * tileSpacing - gridHalfWidth - lineOffset,
          height * tileSpacing - gridHalfHeight - lineOffset,
          0
        ),
      });
    }
    for(int h = 1; h < height; h++){
      var newLine = GameObject.Instantiate(
        linePrefab,
        Vector3.zero,
        Quaternion.identity
      );
      newLine.transform.SetParent(parent, false);
      var lineRenderer = newLine.GetComponent<LineRenderer>();

      lineRenderer.SetPositions(new Vector3[]{
        new Vector3(
          0 * tileSpacing - gridHalfWidth - lineOffset,
          h * tileSpacing - gridHalfHeight - lineOffset,
          0
        ),
        new Vector3(
          width * tileSpacing - gridHalfWidth - lineOffset,
          h * tileSpacing - gridHalfHeight - lineOffset,
          0
        ),
      });
    }
  }

  //PERF: Maybe a binary search would be faster, but then again with small n it might not
  public TdTile TryGetTile(Vector2 worldPos){
    var hWidth = tileWidth / 2f;

    for(int w = 0; w < tiles.GetLength(0); w++){
      for(int h = 0; h < tiles.GetLength(1); h++){
        var tile = tiles[w, h];
        var pos = tile.go.transform.position;

        if(worldPos.x <= pos.x + hWidth && worldPos.x >= pos.x - hWidth ){
          if(worldPos.y <= pos.y + hWidth && worldPos.y >= pos.y - hWidth){
            return tile;
          }
        }
        continue;
      }
    }

    return null;
  }

  public TdTile GetTile(Int2 pos){
    if(pos.x < 0 || pos.x > width - 1 || pos.y < 0 || pos.y > height - 1){
      return null;
    }
    return tiles[pos.x, pos.y];
  }

  public IEnumerable<TdTile> GetTiles(){
    for(int w = 0; w < tiles.GetLength(0); w++){
      for(int h = 0; h < tiles.GetLength(1); h++){
        yield return tiles[w, h];
      }
    }
  }

  public void SetTileTerrain(Int2 tilePos, TileTerrain terrain){
    var tile = tiles[tilePos.x, tilePos.y];
    tile.terrain = terrain;

    switch(terrain){
      case TileTerrain.Grass:
        tile.svgRenderer.color = Colors.greenGrass;
        tile.passable = tile.tower == null;
        break;
      case TileTerrain.Water:
        // tile.svgRenderer.color = Colors.water;
        tile.passable = false;
        tile.buildable = false;
        break;
      case TileTerrain.Forest:
        tile.passable = false;
        tile.buildable = false;
        break;
      case TileTerrain.Ore:
        tile.passable = false;
        tile.buildable = false;
        break;
      case TileTerrain.Spawn:
        tile.buildable = false;
        break;
    }
  }

  List<Int2> frontier = new List<Int2>();
  Dictionary<Int2, bool> visited = new Dictionary<Int2, bool>();
  //returns false if the path is blocked to the dest
  public bool UpdatePathPositions(Int2[] dest, Int2[] creepSources, HashSet<Int2>[] creepPositions){
    if(dest.Length != numPaths){
      Debug.LogError("Number of destinations not equal to number of paths the grid was initialized with");
      return false;
    }
    //first clear out all the existing paths
    for(int w = 0; w < tiles.GetLength(0); w++){
      for(int h = 0; h < tiles.GetLength(1); h++){
        var tile = tiles[w, h];
        for(int tp = 0; tp < numPaths; tp++){
          tile.validPath[tp] = false;
        }
      }
    }

    for(int pathIdx = 0; pathIdx < dest.Length; pathIdx++){
      var pathDest = dest[pathIdx];

      frontier.Clear();
      frontier.Add(pathDest);
      visited.Clear();
      visited[pathDest] = true;
      var destTile = tiles[pathDest.x, pathDest.y];
      destTile.pathDist[pathIdx] = 0;
      //Make sure we can get to the destination before anything else
      if(!destTile.passable){
        Debug.Log("Destination not passable");
        return false;
      }

      while(frontier.Count > 0){
        var current = frontier[0];
        frontier.RemoveAt(0);

        var neighbors = NeighborTiles(current);
        foreach(var nextTile in neighbors){
          //make sure they're also passable
          if(!nextTile.passable){
            continue;
          }

          if(!visited.ContainsKey(nextTile.pos)){
            var currentTile = tiles[current.x, current.y];
            nextTile.nextPathPos[pathIdx] = current;
            nextTile.pathDist[pathIdx] = currentTile.pathDist[pathIdx] + 1;
            nextTile.validPath[pathIdx] = true;

            frontier.Add(nextTile.pos);
            visited[nextTile.pos] = true;
          }
        }
      }

      //check to make sure all the creep sources can get to their destinations
      //Little nasty that the creep path idx stuff is also calculated in here
      for(int spi = 0; spi < creepSources.Length; spi++){
        var creepPathIndex = spi / dest.Length;
        if(creepPathIndex != pathIdx) continue;

        var sourceTile = tiles[creepSources[spi].x, creepSources[spi].y];
        if(!sourceTile.validPath[pathIdx]){
          // Debug.Log("No valid path for creeps pathIdx: " + pathIdx );
          return false;
        }
      }
      //Also make sure all currently occupied creep positions have valid paths so you can't trap them in
      if(creepPositions != null){
        foreach(var pos in creepPositions[pathIdx]){
          var sourceTile = tiles[pos.x, pos.y];
          if(!sourceTile.validPath[pathIdx] && !dest.Contains(sourceTile.pos)){
            Debug.Log("No valid path for current creep: " + pathIdx );
            return false;
          }
        }
      }

      // This has the problem of not allowing inner closed off sections
      // if(visited.Count != PassableTileCount()){
      //   return false;
      // }
    }

    return true;
  }

  public int PassableTileCount(){
    var count = 0;
    for(int w = 0; w < tiles.GetLength(0); w++){
      for(int h = 0; h < tiles.GetLength(1); h++){
        if(tiles[w,h].passable){ count++; }
      }
    }

    return count;
  }

  IEnumerable<TdTile> NeighborTiles(Int2 current){

    foreach(var neighborOffet in TdGrid.neighborOffsets){
      var nextPos = neighborOffet + current;
      //make sure it's a valid pos on the map
      if(nextPos.x < 0 || nextPos.x > width - 1 || nextPos.y < 0 || nextPos.y > height - 1){
        continue;
      }

      yield return tiles[nextPos.x, nextPos.y];
    }
  }

  //Find single paths for friendly creeps that ignore some of the rules
  public List<TdTile> FindPath(TdTile start, TdTile end)
  {
    var ret = new List<TdTile>();
    if(start == end) return ret;

    // The set of nodes already evaluated.
    var closedset = new List<TdTile>();

    // The set of tentative nodes to be evaluated, initially containing the start node
    var openset = new List<TdTile>(){ start };

    // The map of navigated nodes.
    var came_from = new Dictionary<TdTile, TdTile>();

    var g_score = new Dictionary<TdTile, int>();
    g_score[start] = 0;    // Cost from start along best known path.

    // Estimated total cost from start to goal through y.
    var f_score = new Dictionary<TdTile, int>();
    f_score[start] = g_score[start] + distanceCost(start, end);

    while (openset.Count > 0) {
      // the node in openset having the lowest f_score[] value
      var current = openset.OrderBy(x => getValueOrMax(f_score,x)).First();
      if (current.pos == end.pos) {
        return ReconstructPath(came_from, end);
      }

      openset.Remove(current);
      closedset.Add(current);

      var neighbors = NeighborTiles(current.pos);
      foreach(var nextTile in neighbors){

        if(closedset.Contains(nextTile)){
          continue;
        }

        var tentative_g_score = getValueOrMax(g_score,current) + distanceCost(current, nextTile);

        if (!openset.Contains(nextTile) || tentative_g_score < getValueOrMax(g_score,nextTile)) {

          came_from[nextTile] = current;
          g_score[nextTile] = tentative_g_score;
          f_score[nextTile] = getValueOrMax(g_score,nextTile) + distanceCost(nextTile, end);
          if (!openset.Contains(nextTile)) {
            openset.Add(nextTile);
          }
        }
      }

    }

    return null;
  }

  private int distanceCost(TdTile start, TdTile end)
  {
    // Manhattan distance plus extra for walking on a tower
    var passableCost = end.passable ? 0 : 3;
    return Mathf.Abs(start.pos.x - end.pos.x) + Mathf.Abs(start.pos.y - end.pos.y) + passableCost;
  }

  private int getValueOrMax(Dictionary<TdTile, int> dict, TdTile key)
  {
    if(dict.ContainsKey(key)) return dict[key];
    return int.MaxValue;
  }


  private List<TdTile> ReconstructPath(Dictionary<TdTile, TdTile> came_from, TdTile current) {
    var total_path = new List<TdTile>() { current };
    while( came_from.ContainsKey(current)){
      current = came_from[current];
      total_path.Add(current);
    }
    total_path.Reverse();
    //remove starting tile
    return total_path.Skip(1).ToList();
  }


}

public class TdTile{
  public Int2 pos {get; set;}
  public Bounds bounds {get; set;}
  public bool passable {get; set;}
  public bool buildable {get; set;}
  public GameObject go {get; set;}
  public SpriteRenderer svgRenderer {get;set;}
  public TileTerrain terrain {get;set;}
  public TowerView tower {get;set;}

  //indexed by path index
  public bool[] validPath {get; set;}
  public Int2[] nextPathPos {get; set;}
  public int[] pathDist {get; set;}
}

public enum TileTerrain {
  Spawn,
  Grass,
  Water,
  Forest,
  Ore
}