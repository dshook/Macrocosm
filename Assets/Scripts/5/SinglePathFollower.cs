using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SinglePathFollower : MonoBehaviour {
  public TdGrid grid {get; set;}
  public List<TdTile> tilePath {get; set;}

  public Int2 currentGridPos {get;set;}

  public float speed = 0.75f;
  public float bodyRadius = 0.1f;
  public Transform lookTransform;

  public System.Action<GameObject> OnPathCompleted;
  Vector2 tileOffset;
  public int tilePathIdx = 0;
  public bool completedPath = false;

  public void Reset(){
    tilePathIdx = 0;
    completedPath = false;

    if(OnPathCompleted != null){
      foreach(System.Delegate d in OnPathCompleted.GetInvocationList())
      {
        OnPathCompleted -= (System.Action<GameObject>)d;
      }
    }
  }

  void Start(){
    //Snap rotation to where we're going to start
    var curTile = tilePath[tilePathIdx];
    var nextTile = tilePath[tilePathIdx + 1];

    var tileCoordDiff = (nextTile.go.transform.position - curTile.go.transform.position).Straighten().normalized;
    var lookRot = Quaternion.LookRotation(Vector3.forward, tileCoordDiff);
    lookTransform.rotation = lookRot;
  }

  void Update(){
    if(grid == null || tilePath == null) return;

    TdTile curTile = grid.tiles[currentGridPos.x, currentGridPos.y];
    if(CheckComplete()){
      return;
    }

    var nextTile = tilePath[tilePathIdx + 1];

    //if we're in the next tile move on
    if(nextTile.bounds.Contains(transform.localPosition)){

      // curTile.svgRenderer.color = Colors.greenGrass;
      currentGridPos = nextTile.pos;
      curTile = nextTile;
      tilePathIdx++;
      if(CheckComplete()){
        return;
      }
      nextTile = tilePath[tilePathIdx + 1];

      // Debug.Log("Navigating to " + curTile.nextPathPos);
      // curTile.svgRenderer.color = Colors.mint;
    }

    var tileCoordDiff = (nextTile.go.transform.position - curTile.go.transform.position).Straighten().normalized;

    // transform.position = transform.position + (tileCoordDiff * speed * Time.smoothDeltaTime);
    transform.position = Vector3.MoveTowards(transform.position, nextTile.go.transform.position + (Vector3)tileOffset, speed * Time.smoothDeltaTime);

    var lookRot = Quaternion.LookRotation(Vector3.forward, tileCoordDiff);
    lookTransform.rotation = Quaternion.Slerp(lookTransform.rotation, lookRot, 0.15f);
  }

  bool CheckComplete(){
    if(completedPath){
      return true;
    }
    if(tilePathIdx + 1 > tilePath.Count - 1){
      completedPath = true;
      if(OnPathCompleted != null){
        OnPathCompleted(this.gameObject);
      }else{
        Debug.LogWarning("SPF OnPathCompleted null");
      }
      return true;
    }
    return false;
  }

  public void SetTilePos(Int2 tilePos){
    var tile = grid.tiles[tilePos.x, tilePos.y];

    var hWidth = (grid.TileWidth / 2f) - bodyRadius;
    tileOffset = new Vector2(Random.Range(-hWidth, hWidth), Random.Range(-hWidth, hWidth));
    currentGridPos = tile.pos;
    transform.position = tile.go.transform.position + (Vector3)tileOffset;
  }
}