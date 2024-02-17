using UnityEngine;

public class HexPathFollower : MonoBehaviour {
  public HexGrid grid {get; set;}
  public HexCoordinates source;
  public HexCoordinates dest;
  public HexCoordinates position;
  public bool canCrossWater;


  public float speed = 0.75f;
  public Transform lookTransform;

  public System.Action OnMoveStepCompleted;
  public System.Action OnPathCompleted;

  bool moving = false;
  HexCell curTile;
  HexCell nextTile;

  void Start(){
  }

  void Update(){
    if(moving){
      //Wait to move till displays are initialized
      if(nextTile.display == null || curTile.display == null){
        return;
      }

      var tileCoordDiff = (nextTile.display.transform.position - curTile.display.transform.position).normalized;

      transform.position = Vector3.MoveTowards(transform.position, nextTile.display.transform.position, speed * Time.smoothDeltaTime);

      var lookRot = Quaternion.LookRotation(Vector3.forward, tileCoordDiff);
      lookTransform.rotation = Quaternion.Slerp(lookTransform.rotation, lookRot, 0.15f);

      if(Vector2.Distance(transform.position, nextTile.display.transform.position) < float.Epsilon){
        moving = false;
        position = nextTile.coordinates;

        if(OnMoveStepCompleted != null){ OnMoveStepCompleted(); }

        if(nextTile.coordinates == dest){
          if(OnPathCompleted != null){ OnPathCompleted(); }
        }else{
          Move();
        }
      }
    }
  }

  public HexGrid.PathfindOptions pathfindOptions{
    get {
      return new HexGrid.PathfindOptions{
        canMoveOnLand = true,
        canMoveOnWater = true,
        avoidWater = !canCrossWater
      };
    }
  }

  public void Move(){
    var pathOptions = new HexGrid.PathfindOptions{
      src = position,
      dest = dest,
      maxTileDistance = 20, //prevent rare excessive searching
      canMoveOnLand = pathfindOptions.canMoveOnLand,
      canMoveOnWater = pathfindOptions.canMoveOnWater,
      avoidWater = pathfindOptions.avoidWater
    };
    //Always pass in that we can pass water to getting a path, we'll complete the path below if we can't actually move on water
    var curPath = grid.FindPath(pathOptions);
    if(curPath == null || curPath.Count == 0){
      OnPathCompleted();
      return;
    }

    curTile = grid.GetCell(position);
    nextTile = grid.GetCell(curPath[0]);

    //Any move rules to check go here
    if(nextTile.IsUnderwater && !canCrossWater){
      OnPathCompleted();
    }
    if(nextTile.HexFeature == HexFeature.Peak){
      OnPathCompleted();
    }

    moving = true;
  }

}