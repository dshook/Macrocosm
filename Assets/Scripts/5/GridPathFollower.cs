using UnityEngine;

public class GridPathFollower : MonoBehaviour {
  public TdGrid grid {get; set;}
  public int pathIdx {get; set;}

  public Int2 currentGridPos {get;set;}

  public float speed = 0.75f;
  public float bodyRadius = 0.1f;
  public Transform lookTransform;

  public System.Action<GameObject> OnPathCompleted;
  Vector2 tileOffset;

  public void Reset(){
    if(OnPathCompleted != null){
      foreach(System.Delegate d in OnPathCompleted.GetInvocationList())
      {
        OnPathCompleted -= (System.Action<GameObject>)d;
      }
    }
  }

  void Start(){
    //Snap rotation to where we're going to start
    TdTile curTile = grid.tiles[currentGridPos.x, currentGridPos.y];
    var nextTile = grid.tiles[curTile.nextPathPos[pathIdx].x, curTile.nextPathPos[pathIdx].y];

    var tileCoordDiff = (nextTile.go.transform.position - curTile.go.transform.position).Straighten().normalized;
    var lookRot = Quaternion.LookRotation(Vector3.forward, tileCoordDiff);
    lookTransform.rotation = lookRot;
  }

  void Update(){
    if(grid == null) return;

    if(OnPathCompleted == null){
      Debug.LogWarning("OnPathCompleted null");
      return;
    }

    TdTile curTile = grid.tiles[currentGridPos.x, currentGridPos.y];
    if(curTile == null || !curTile.validPath[pathIdx]){
      OnPathCompleted(this.gameObject);
      return;
    }

    TdTile nextTile = grid.tiles[curTile.nextPathPos[pathIdx].x, curTile.nextPathPos[pathIdx].y];

    if(nextTile == null){
      Debug.LogWarning("GridPathFollower next tile is null");
      OnPathCompleted(this.gameObject);
      return;
    }

    //if we're in the next tile move on
    if(nextTile.bounds.Contains(transform.localPosition)){

      // curTile.svgRenderer.color = Colors.greenGrass;

      currentGridPos = curTile.nextPathPos[pathIdx];
      curTile = grid.tiles[currentGridPos.x, currentGridPos.y];
      nextTile = grid.tiles[curTile.nextPathPos[pathIdx].x, curTile.nextPathPos[pathIdx].y];

      // Debug.Log("Navigating to " + curTile.nextPathPos);
      // curTile.svgRenderer.color = Colors.mint;
    }

    if(!curTile.validPath[pathIdx]){
      OnPathCompleted(this.gameObject);
      return;
    }

    var tileCoordDiff = (nextTile.go.transform.position - curTile.go.transform.position).Straighten().normalized;

    // transform.position = transform.position + (tileCoordDiff * speed * Time.smoothDeltaTime);
    var nextPosition = nextTile.go.transform.position + (Vector3)tileOffset;
    transform.position = Vector3.MoveTowards(transform.position, nextTile.go.transform.position + (Vector3)tileOffset, speed * Time.smoothDeltaTime);

    var lookRot = Quaternion.LookRotation(Vector3.forward, tileCoordDiff);
    lookTransform.rotation = Quaternion.Slerp(lookTransform.rotation, lookRot, 0.15f);
  }

  public void FindRandomStartPoint(){
    while(true){
      var randoTile = grid.tiles[Random.Range(0, grid.Width), Random.Range(0, grid.Height)];
      if(randoTile.passable){
        currentGridPos = randoTile.pos;
        var hWidth = (grid.TileWidth / 2f) - bodyRadius;
        tileOffset = new Vector3(Random.Range(-hWidth, hWidth), Random.Range(-hWidth, hWidth));
        transform.position = randoTile.go.transform.position;
        break;
      }
    }
  }

  public void SetTilePos(Int2 tilePos){
    var tile = grid.tiles[tilePos.x, tilePos.y];
    if(!tile.passable){
      Debug.LogError("Trying to set GPF tile position to an unpassable tile");
      return;
    }

    var hWidth = (grid.TileWidth / 2f) - bodyRadius;
    tileOffset = new Vector2(Random.Range(-hWidth, hWidth), Random.Range(-hWidth, hWidth));
    currentGridPos = tile.pos;
    transform.position = tile.go.transform.position + (Vector3)tileOffset;
  }
}