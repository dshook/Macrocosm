using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Same as RandomMovement but doing janky physics instead of a full rigidbody
public class RandomMovementNoRB : MonoBehaviour {

  public float drag = 0.05f;

  public float minMoveDelay = 1.0f;
  public float maxMoveDelay = 2.0f;
  public float moveForce = 1.0f;

  float timeAccum = 0;
  float moveTime = 0f;

  Vector2 velocity = Vector2.zero;

  void Awake () {
    moveTime = Random.Range(minMoveDelay, maxMoveDelay);
    timeAccum = 1 + moveTime;
  }

  void Update () {
    timeAccum += Time.deltaTime;
    if(timeAccum > moveTime){
      timeAccum = 0;

      velocity += new Vector2(Random.Range(-moveForce, moveForce),Random.Range(-moveForce, moveForce));

      moveTime = Random.Range(minMoveDelay, maxMoveDelay);
    }

    //normal physics stuff
    transform.position = transform.position + (Vector3)(velocity * Time.fixedDeltaTime);

    velocity = velocity * (1 - drag * Time.fixedDeltaTime);
  }

  public void AddKickoffForce(float moveForceMultiplier = 1.5f){
    var kickoffForce = moveForce * moveForceMultiplier;
    velocity += new Vector2(Random.Range(-kickoffForce, kickoffForce),Random.Range(-kickoffForce, kickoffForce));
  }
}
