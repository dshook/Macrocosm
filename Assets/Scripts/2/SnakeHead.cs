using System.Collections;
using System.Collections.Generic;
using strange.extensions.mediation.impl;
using UnityEngine;

public class SnakeHead : View {

  public Snake snake;
  public Color color;
  public AtomRenderer atomRenderer;

  SpriteRenderer headRenderer;

  protected override void Awake () {
    base.Awake();

    headRenderer = GetComponentInChildren<SpriteRenderer>();
    atomRenderer = GetComponentInChildren<AtomRenderer>();
  }

  //Be late to override the default atom renderer color
  void LateUpdate () {
    headRenderer.color = color;
  }

  void OnCollisionEnter2D(Collision2D col)
  {
    var atom = col.transform.GetComponentInChildren<AtomRenderer>();
    if(atom != null){
      snake.Eat(atom, col);
    }
  }

}
