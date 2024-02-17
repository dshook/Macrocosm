using UnityEngine;

public class TdShot : MonoBehaviour {
  public Vector2 velocity;
  public float ttl = 2f;
  public int damage;
  public bool isSlowing = false;
  public bool isPiercing = false;
  public float maxDistance;
  public Vector2 shotFrom;

  public ObjectPool objectPool;

  float timeAlive = 0;
  bool hitCreep = false;

  protected void OnEnable(){
    ttl = 2f;
    isSlowing = false;
    timeAlive = 0;
    hitCreep = false;
    maxDistance = 0f;
    shotFrom = Vector2.zero;
  }

  void Update()
  {
    transform.position = transform.position + (Vector3)velocity * Time.deltaTime;
    timeAlive += Time.deltaTime;

    var travelDistance = Vector2.Distance(shotFrom, transform.position);
    if(timeAlive > ttl || travelDistance >= maxDistance){
      Die();
    }
  }

  void OnTriggerEnter2D(Collider2D other)
  {
    if(hitCreep && !isPiercing){
      return;
    }


    //This way avoids GC alloc in editor
    // https://medium.com/chenjd-xyz/unity-tip-use-trygetcomponent-instead-of-getcomponent-to-avoid-memory-allocation-in-the-editor-fe0c3121daf6
    other.gameObject.TryGetComponent<TdCreep>(out var creep);
    if(creep != null && creep.type != TdCreepType.Friendly){
      hitCreep = true;
      if(isSlowing){
        creep.Freeze();
      }
      creep.health -= damage;
      if(!isPiercing){
        Die();
      }
    }
  }

  void Die(){
    //Should never be null if I didn't screw up, but didn't feel like converting these to views
    objectPool.Recycle(this.gameObject);
  }

}