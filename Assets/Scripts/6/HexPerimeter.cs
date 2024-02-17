using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HexPerimeter
{

  public static List<HexCell> FindPerimeterLoop(List<HexCell> cells){
    if(cells.Count == 0){
      Debug.LogWarning("Trying to find perim around 0 cells");
      return null;
    }
    //start by finding the top right most cell to start a loop from
    var startTile = cells.OrderByDescending(t => t.coordinates.Z).ThenByDescending(t => t.coordinates.X).FirstOrDefault();
    var count = 0;

    //trace right and down as much as we can until the bottom is found, then start going left and up
    //It's possible to go back the way we came if there is a one cell peninsula
    var perim = new List<HexCell>();
    var travelDirection = HexDirection.SE;
    var currentTile = startTile;
    do
    {
      var directionPriorities = directionPriority(travelDirection);
      foreach (var direction in directionPriorities)
      {
        var nextTile = currentTile.GetNeighbor(direction);
        if (cells.Any(c => nextTile == c))
        {
          perim.Add(currentTile);
          travelDirection = direction;
          currentTile = nextTile;
          break;
        }
      }
      count++;
    }
    while (currentTile != startTile && count < 500);

    // Debug.Log(string.Join(",", perim.Select(x => x.coordinates.ToString())));

    return perim;
  }

  //Which way should we go given which way we came from?
  //The way the directions are set up this works out to going around clockwise given the start at top
  static IEnumerable<HexDirection> directionPriority(HexDirection dir){
    yield return dir.Previous();
    yield return dir;
    yield return dir.Next();
    yield return dir.Next2();
    yield return dir.Opposite(); //Last resort go back the way we came
  }

  //Convert the cell perim to a list of world coords on the hex border around the cells
  public static List<Vector3> GetLinePositions(List<HexCell> cellPerimeter){
    var ret = new List<Vector3>();

    HexDirection prevDir;
    HexDirection nextDir;
    HexCell firstHexCell = cellPerimeter[0];
    HexCell lastHexCell = cellPerimeter[cellPerimeter.Count - 1];
    HexCell nextHexCell = null;
    for(int p = 0; p < cellPerimeter.Count; p++){
      var cell = cellPerimeter[p];
      if(p > 0){
        lastHexCell = cellPerimeter[p - 1];
      }

      prevDir = HexDirectionExtensions.CoordDirection(cell.coordinates, lastHexCell.coordinates);

      if (p + 1 < cellPerimeter.Count) {
        nextHexCell = cellPerimeter[p + 1];
      }
      else {
        nextHexCell = firstHexCell;
      }
      nextDir = HexDirectionExtensions.CoordDirection(cell.coordinates, nextHexCell.coordinates);

      var bendType = GetBendType(prevDir, nextDir);
      // Debug.Log(string.Format("Bend {0} {1}    {2}", prevDir, nextDir, bendType));

      var currentCorner = startingCornerMap[prevDir];

      for(int i = 0; i < (int)bendType; i++){
        AddDir(ret, cell, currentCorner);
        currentCorner = currentCorner.Next();
      }

    }

    return ret;
  }

  // the value signify how many vertices are used on the line going around the corner
  enum HexBendType{
    Inner = 1,
    Straight = 2,
    Outer = 3,
    Acute = 4,
    Uturn = 5
  }

  //Maintaining the clockwise motion starting from the top right most cell, translate the previous/next tile pair into a bend direction
  static HexBendType GetBendType(HexDirection prevDir, HexDirection nextDir){
    if(prevDir == nextDir.Opposite()){
      return HexBendType.Straight;
    }
    if(prevDir == nextDir){
      return HexBendType.Uturn;
    }

    //Not sure what the axiom is here that makes this work but with the direction layout they alternate if you need to go previous or next
    if(nextDir == prevDir.Next2()){
      return HexBendType.Inner;
    }
    if(nextDir == prevDir.Previous2()){
      return HexBendType.Outer;
    }
    if(nextDir == prevDir.Previous()){
      return HexBendType.Acute;
    }

    //Shouldn't hit here
    Debug.LogWarning("Unknown bend type " + prevDir + " " + nextDir);
    return HexBendType.Straight;
  }

  //For the perimeter line what hex corner do we need to start adding on
  static Dictionary<HexDirection, HexCornerDirection> startingCornerMap = new Dictionary<HexDirection, HexCornerDirection>(){
    {HexDirection.NE, HexCornerDirection.SE},
    {HexDirection.E, HexCornerDirection.S},
    {HexDirection.SE, HexCornerDirection.SW},
    {HexDirection.SW, HexCornerDirection.NW},
    {HexDirection.W, HexCornerDirection.N},
    {HexDirection.NW, HexCornerDirection.NE},
  };

  static void AddDir(List<Vector3> ret, HexCell cell, HexCornerDirection dir){
    ret.Add( HexCoordinates.corners[dir] + cell.localPosition );
  }
}