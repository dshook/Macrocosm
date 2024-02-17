using UnityEngine;

public class FlyingFollower : MonoBehaviour {
  public TdGrid grid {get; set;}
  public TdTile destination {get; set;}

  public float speed = 0.75f;
  public float bodyRadius = 0.1f;
  public Transform lookTransform;

  public System.Action<GameObject> OnPathCompleted;

  public void Reset(){
    if(OnPathCompleted != null){
      foreach(System.Delegate d in OnPathCompleted.GetInvocationList())
      {
        OnPathCompleted -= (System.Action<GameObject>)d;
      }
    }
  }

  void Start(){
    var directionDiff = (destination.go.transform.position - transform.position).normalized;
    var lookRot = Quaternion.LookRotation(Vector3.forward, directionDiff);
    lookTransform.rotation = lookRot;
  }

  void Update(){
    if(destination == null){
      return;
    }

    var directionDiff = (destination.go.transform.position - transform.position).normalized;

    transform.position = transform.position + (directionDiff * speed * Time.smoothDeltaTime);

    if(Vector2.Distance(destination.go.transform.position, transform.position) < bodyRadius){
      if(OnPathCompleted != null){
        OnPathCompleted(this.gameObject);
      }else{
        Debug.LogWarning("Flying OnPathCompleted null");
      }
      return;
    }

    var lookRot = Quaternion.LookRotation(Vector3.forward, directionDiff);
    lookTransform.rotation = Quaternion.Slerp(lookTransform.rotation, lookRot, 0.2f);
  }


  public void SetTilePos(Int2 tilePos){
    var tile = grid.tiles[tilePos.x, tilePos.y];
    if(!tile.passable){
      Debug.LogError("Trying to set GPF tile position to an unpassable tile");
      return;
    }

    var hWidth = (grid.TileWidth / 2f) - bodyRadius;
    transform.position = tile.go.transform.position + new Vector3(Random.Range(-hWidth, hWidth), Random.Range(-hWidth, hWidth));
  }
}