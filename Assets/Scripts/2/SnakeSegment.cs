using System.Collections;
using System.Collections.Generic;
using strange.extensions.mediation.impl;
using UnityEngine;

public class SnakeSegment : View {

  public Snake snake;

  public GameObject background;

  //After this segment has been eaten it's immune from head collisions for a little bit
  float immuneTimer = 0f;
  const float immuneTime = 0.25f;
  public bool isImmune = false;

  void Update () {
    if(isImmune){
      immuneTimer += Time.deltaTime;

      if(immuneTimer >= immuneTime){
        isImmune = false;
        immuneTimer = 0f;
      }
    }
  }

  public void onEaten(){
    isImmune = true;
    background.SetActive(true);
  }

  public void onBreak(){
    background.SetActive(false);
  }

  void OnCollisionEnter2D(Collision2D col)
  {
    var atom = col.transform.GetComponentInChildren<AtomRenderer>();
    var otherSegment = col.transform.GetComponent<SnakeSegment>();
    if(atom != null && snake != null && otherSegment != null && !otherSegment.enabled){
      snake.Break(this.gameObject, false);
    }
  }

}
