using UnityEngine;

using System.Collections.Generic;
using strange.extensions.mediation.impl;
using System.Linq;

[SelectionBase]
public class HexRiverOrRoad : View {
  [Inject] ResourceLoaderService loader {get; set;}

  public BezierSpline spline;
  public SplineToLineRenderer splineToLineRenderer;
  public LineRenderer lineRenderer;

  [Range(0, 90)]
  public int snakeynessMaxAngle = 0;

  [Range(0, 1)]
  public float randomPositionOffsetPct = 0;

  [Range(0, 1)]
  public float positionOffsetDistance = 0.2f;

  const float controlPointDistance = 0.3f;

  public CityBuildingId connectionBuildingId = CityBuildingId.None;

  List<HexCell> cells;
  public List<HexCell> Cells {
    get{ return cells; }
  }

  public HexCoordinates? Beginning {
    get { return (cells != null && cells.Count > 0) ? cells[0].coordinates : (HexCoordinates?)null; }
  }
  public HexCoordinates? End {
    get { return (cells != null && cells.Count > 0) ? cells[cells.Count - 1].coordinates : (HexCoordinates?)null; }
  }

  /* TODO
    Explore just regenerating all the affected roads when one upgrades
    Would need to sort by the highest road upgrade type first
  */

  Dictionary<CityBuildingId, string> materials = new Dictionary<CityBuildingId, string>(){
    {CityBuildingId.River, "Prefabs/6/ConnectionMaterials/River"},
    {CityBuildingId.DirtRoad, "Prefabs/6/ConnectionMaterials/DirtRoad"},
    {CityBuildingId.StoneRoad, "Prefabs/6/ConnectionMaterials/StoneRoad"},
    {CityBuildingId.Highway, "Prefabs/6/ConnectionMaterials/Highway"},
    {CityBuildingId.Railroad, "Prefabs/6/ConnectionMaterials/Rail"},
    {CityBuildingId.WaterTradeRoute, "Prefabs/6/ConnectionMaterials/SeaConnection"},
  };
  Dictionary<CityBuildingId, int> connectionSortOrder = new Dictionary<CityBuildingId, int>(){
    {CityBuildingId.River, 1},
    {CityBuildingId.DirtRoad, 2},
    {CityBuildingId.StoneRoad, 3},
    {CityBuildingId.Highway, 4},
    {CityBuildingId.Railroad, 5},
    {CityBuildingId.WaterTradeRoute, 1},
  };

  //Determines what connections can upgrade and merge into each other.
  public static Dictionary<CityBuildingId, int> connectionTier = new Dictionary<CityBuildingId, int>(){
    {CityBuildingId.DirtRoad, 2},
    {CityBuildingId.StoneRoad, 3},
    {CityBuildingId.Highway, 4},
  };

  public static Dictionary<CityBuildingId, int> moveCostBonus = new Dictionary<CityBuildingId, int>(){
    {CityBuildingId.DirtRoad, 1},
    {CityBuildingId.StoneRoad, 2},
    {CityBuildingId.Highway, 3},
    {CityBuildingId.Railroad, 4},
    {CityBuildingId.WaterTradeRoute, 1},
  };

  //Increase the widths of the higher level roads so they cover the lower layers better
  Dictionary<CityBuildingId, float> connectionWidthMultiplier = new Dictionary<CityBuildingId, float>(){
    {CityBuildingId.DirtRoad, 0.1f},
    {CityBuildingId.StoneRoad, 0.12f},
    {CityBuildingId.Highway, 0.14f},
  };

  public void Init(){
    UpdateMaterial();
  }

  //Assumes there's more than one point
  public void SetTilePoints(List<HexCell> cells){

    //Make sure we copy the cells for internal storage so changes to the list that's reused by the caller
    //Won't persist here
    if(this.cells == null){
      this.cells = new List<HexCell>();
    }else{
      this.cells.Clear();
    }

    //for rail alway use random points so they don't overlap the road
    bool useSavedAnchors = connectionBuildingId != CityBuildingId.Railroad;

    for(int r = 0; r < cells.Count; r++){
      var cell = cells[r];
      var point = (Vector2)cell.localPosition;
      //spline has the initial 4 points set up which are used for the 0th and 1st points
      if(r > 1){
        spline.AddCurve();
      }

      var anchorPoint = point;
      bool isRoad = connectionBuildingId != CityBuildingId.River;
      var prevAnchor = isRoad ? cell.RoadAnchorPoint : cell.RiverAnchorPoint;

      //for the initial point just set the origin
      if(r == 0){
        //but also check to see if there's a previously saved offset to use
        if(useSavedAnchors && prevAnchor != Vector2.zero){
          anchorPoint = prevAnchor;
        }
        spline.SetControlPoint(r, anchorPoint);
      }else{
        // the next points set the control points behind themselves
        var pointIdx = r * 3;
        var prevPoint = spline.GetControlPoint(pointIdx - 3);
        var pointDelta = anchorPoint - (Vector2)prevPoint;

        if(r == cells.Count - 1){
          //For the ending point see if we can use a previously saved point to merge into
          if(useSavedAnchors && prevAnchor != Vector2.zero){
            anchorPoint = prevAnchor;
          }
        }
        //possibly modify the anchor point position for some randomness
        else if(randomPositionOffsetPct > 0f){
          if(useSavedAnchors){
            StartRepeatableRandom(cell.coordinates);
          }
          var pctWeight = Random.Range(0, randomPositionOffsetPct);
          anchorPoint = anchorPoint + (pctWeight *
            new Vector2(
              Random.Range(-positionOffsetDistance, positionOffsetDistance),
              Random.Range(-positionOffsetDistance, positionOffsetDistance)
            ));
          if(useSavedAnchors){
            EndRepeatableRandom();
          }
        }

        spline.SetControlPoint(pointIdx, anchorPoint);
        spline.SetControlPoint(pointIdx - 1, (Vector2)prevPoint + (pointDelta * (1 - controlPointDistance)));
        spline.SetControlPoint(pointIdx - 2, (Vector2)prevPoint + (pointDelta * controlPointDistance));
      }

      //Save the anchor points in the cells so they can be merged with other roads/rivers
      if(useSavedAnchors){
        if(isRoad){
          cell.RoadAnchorPoint = anchorPoint;
        }else{
          cell.RiverAnchorPoint = anchorPoint;
        }
      }

      this.cells.Add(cell);
    }

    //Go back over the spline smoothing out the control points so they're in lines with the anchor points
    for(int anchorIdx = 3; anchorIdx < spline.ControlPointCount - 2; anchorIdx += 3){
      var anchorPoint = spline.GetControlPoint(anchorIdx);
      var beforeControlPoint = spline.GetControlPoint(anchorIdx - 1);
      var afterControlPoint = spline.GetControlPoint(anchorIdx + 1);
      var cell = cells[anchorIdx / 3];

      //In the middle between the two control points, when the line is flat this will be 0, otherwise it will be the
      //offset from the anchor point that would be for the "flipped" version of the triangle of the three points
      var controlPointSum = (beforeControlPoint - anchorPoint) + (afterControlPoint - anchorPoint);

      //Only alter the points if they're not already in a line
      if(controlPointSum != Vector3.zero){

        var beforeRotation = Vector2.SignedAngle(beforeControlPoint - anchorPoint, controlPointSum);
        Vector2 rotated;
        if(beforeRotation > 0){
          rotated = ((Vector2)controlPointSum).Rotate(-90f);
        }else{
          rotated = ((Vector2)controlPointSum).Rotate(90f);
        }
        beforeControlPoint = rotated.normalized * (beforeControlPoint - anchorPoint).magnitude + (Vector2)anchorPoint;
        spline.SetControlPoint(anchorIdx - 1, beforeControlPoint);

        var afterRotation = Vector2.SignedAngle(afterControlPoint - anchorPoint, controlPointSum);
        if(afterRotation > 0){
          rotated = ((Vector2)controlPointSum).Rotate(-90f);
        }else{
          rotated = ((Vector2)controlPointSum).Rotate(90f);
        }
        afterControlPoint = rotated.normalized * (afterControlPoint - anchorPoint).magnitude + (Vector2)anchorPoint;
        spline.SetControlPoint(anchorIdx + 1, afterControlPoint);
      }

      //Twist the line up if desired by a random amt at each point
      if(snakeynessMaxAngle > 0){
        var reverse = anchorIdx % 2 == 0 ? 1 : -1;

        if(useSavedAnchors){
          StartRepeatableRandom(cell.coordinates);
        }
        var angle = Random.Range(0, reverse * snakeynessMaxAngle);
        if(useSavedAnchors){
          EndRepeatableRandom();
        }

        var rotatedBefore = ((Vector2)(beforeControlPoint - anchorPoint)).Rotate(angle).normalized * controlPointDistance * 0.5f;
        var rotatedAfter = ((Vector2)(afterControlPoint - anchorPoint)).Rotate(angle).normalized * controlPointDistance * 0.5f;

        spline.SetControlPoint(anchorIdx - 1, (Vector2)beforeControlPoint + rotatedBefore);
        spline.SetControlPoint(anchorIdx + 1, (Vector2)afterControlPoint + rotatedAfter);
      }

    }

    splineToLineRenderer.sampleFrequency = 6 * cells.Count;
    splineToLineRenderer.GenerateMesh();

#if UNITY_EDITOR
    gameObject.name = string.Format("{0} from {1} to {2}", connectionBuildingId, Beginning, End);
#endif
  }

  public void SetColor(Color color){
    if (!Application.isEditor || Application.isPlaying) {
      lineRenderer.material.color = color;
    }else{
      //Do the complicated thing for testing in editor
      var tempMaterial = new Material(lineRenderer.sharedMaterial);
      tempMaterial.color = color;
      lineRenderer.material = tempMaterial;
    }
  }

  public void UpdateMaterial(){
    if(loader != null){ // should only be null when creating through editor
      var mat = loader.Load<Material>(materials[connectionBuildingId]);
      lineRenderer.material = mat;
    }
    lineRenderer.sortingOrder = connectionSortOrder[connectionBuildingId];
    if(connectionWidthMultiplier.ContainsKey(connectionBuildingId)){
      lineRenderer.widthMultiplier = connectionWidthMultiplier[connectionBuildingId];
    }
  }

  Random.State originalRandomState;
  void StartRepeatableRandom(HexCoordinates coords){
    //Make this process repeatably random so that different types of connections can overlap properly
    //IE, all types of roads overlap on each cell, but rivers and rail take different paths
    originalRandomState = Random.state;
    var randOffset = 0;
    if(connectionBuildingId == CityBuildingId.River){ randOffset = 1; }
    if(connectionBuildingId == CityBuildingId.WaterTradeRoute ){ randOffset = 2; }
    Random.InitState(coords.GetHashCode() + randOffset);
  }

  void EndRepeatableRandom(){
    Random.state = originalRandomState;
  }
}