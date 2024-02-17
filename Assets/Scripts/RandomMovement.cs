using UnityEngine;

public class RandomMovement : MonoBehaviour {

  public float minMoveDelay = 1.0f;
  public float maxMoveDelay = 2.0f;
  public float moveForce = 1.0f;
  public float moveTorque = 0f;

  Rigidbody2D rb;
  float timeAccum = 0;
  float moveTime = 0f;

  void Awake () {
    rb = GetComponent<Rigidbody2D>();
    moveTime = Random.Range(minMoveDelay, maxMoveDelay);
    timeAccum = 1 + moveTime;
  }

  void Update () {
    timeAccum += Time.deltaTime;
    if(timeAccum > moveTime){
      timeAccum = 0;

      if(rb != null){
        rb.AddForce(new Vector2(Random.Range(-moveForce, moveForce),Random.Range(-moveForce, moveForce)));
        if(moveTorque != 0){
          rb.AddTorque(Random.Range(-moveTorque, moveTorque));
        }
      }

      moveTime = Random.Range(minMoveDelay, maxMoveDelay);
    }
  }

  public void AddKickoffForce(float moveForceMultiplier = 1.5f){
    if(rb != null){
      var kickoffForce = moveForce * moveForceMultiplier;
      rb.AddForce(new Vector2(Random.Range(-kickoffForce, kickoffForce),Random.Range(-kickoffForce, kickoffForce)));
    }
  }
}
