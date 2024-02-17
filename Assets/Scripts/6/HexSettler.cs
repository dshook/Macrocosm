using System;
using UnityEngine;

[Serializable]
public class HexSettlerData {
  public HexCoordinates source;
  public HexCoordinates dest;
  public HexCoordinates position;
  public bool canCrossWater;
}

public class HexSettler : MonoBehaviour {
  public System.Action<HexCoordinates> OnSettleCompleted;

  public HexGrid grid {get; set;}
  public HexPathFollower follower;

  public HexSettlerData data;

  public void Init(){
    follower.grid = grid;

    follower.source = data.source;
    follower.dest = data.dest;

    follower.position = data.position;
    follower.OnPathCompleted = PathComplete;
    follower.OnMoveStepCompleted = MoveStepCompleted;
    follower.canCrossWater = data.canCrossWater;
    follower.Move();
  }

  void PathComplete(){
    OnSettleCompleted(data.dest);
    Destroy(this.gameObject);
  }

  void MoveStepCompleted(){
    data.position = follower.position;
    var cell = grid.GetCell(data.position);
    cell.Explore(HexExploreStatus.Explored);
  }

}
