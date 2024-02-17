using System;
using TMPro;
using UnityEngine;

public class RotateArounder : MonoBehaviour {

  public float timeToLive = 1.5f;
  public float delay = 0f;

  public bool started = true;
  public Transform rotateAround;
  public Vector3 axis = Vector3.forward;
  public float destAngle = 0f;


  float timer = 0f;
  float angleSum = 0f;

  void Awake () {

  }

  void Update () {
    if(!started){ return; }

    timer += Time.deltaTime;
    if(timer < delay){
      return;
    }

    //Should be handled more elegantly if you use RotateTo,
    if(timeToLive <= 0){
      DoRotate(destAngle);
      started = false;
      return;
    }
    var angleStep = (destAngle / timeToLive) * Time.deltaTime;
    angleSum += angleStep;

    DoRotate(angleStep);

    if(timer >= timeToLive + delay){
      //take care of cleanup
      DoRotate(destAngle - angleSum);

      started = false;
      return;
    }
  }

  void DoRotate(float angle){
    transform.RotateAround(rotateAround.position, axis, angle);
  }

  public void RotateTo(float angle, float? ttl = null, bool immediate = false){
    destAngle = angle;
    angleSum = 0f;
    if(ttl.HasValue){ timeToLive = ttl.Value; }
    timer = 0f;
    started = true;
    if(ttl <= 0 || immediate){
      DoRotate(angle);
      started = false;
    }
  }

}
