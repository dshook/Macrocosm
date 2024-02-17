using UnityEngine;
using System;


[RequireComponent(typeof(LineRenderer))]
[ExecuteInEditMode]
public class LineRendererScroller : MonoBehaviour
{
  public float scrollSpeed = 0.5f;
  public Material mat;

  LineRenderer lineRenderer;

  protected void Awake () {
    lineRenderer = GetComponent<LineRenderer>();
  }

  void Update () {
    if(mat == null) return;
    mat.mainTextureScale = new Vector2(1f / lineRenderer.widthMultiplier, 1f);
    mat.mainTextureOffset = new Vector2( -scrollSpeed * Time.deltaTime + mat.mainTextureOffset.x, mat.mainTextureOffset.y);
  }

}