using UnityEngine;
using System;

public class OrthographicZoom : MonoBehaviour
{
  public float lerpSpeed = 0.3f;
  public Vector2 padding; //how much space inside the bounds should we always have
  public Vector2 margin; //offets useful for correcting for the top bars
  public float maxSize = 20f;
  public float minSize = 0.03f;

  private Camera cam;

  public Snake snake;

  void Awake()
  {
    cam = Camera.main;
  }

  private void LateUpdate()
  {
    var target = GetTarget();
    if(target != null){
      OrthoZoom(target.Value.center, Mathf.Max(target.Value.size.y, target.Value.size.x), true);
    }
  }


  private void OrthoZoom(Vector2 center, float regionHeight, bool lerp)
  {
    var dest = new Vector3(center.x, center.y, Constants.defaultCameraPosition.z);
    if(lerp){
      cam.transform.position = Vector3.Slerp(cam.transform.position, dest, lerpSpeed);
      cam.orthographicSize = Mathf.Clamp(Mathf.Lerp(cam.orthographicSize, regionHeight, lerpSpeed), minSize, maxSize);
    }else{
      cam.transform.position = dest;
      cam.orthographicSize = Mathf.Clamp(regionHeight, minSize, maxSize);
    }
  }

  public void ResetZoom(){
    // Debug.Log("Resetting cam to " + defaultCenter + " zoom " + defaultHeight + " cam is " + cam);
    if(cam == null) return;

    var target = GetTarget();
    if(target != null){
      OrthoZoom(target.Value.center, Mathf.Max(target.Value.size.y, target.Value.size.x), false);
    }else{
      OrthoZoom(Constants.defaultCameraPosition, Constants.defaultCameraOrthoSize, false);
    }
  }

  private Bounds? GetTarget()
  {
    if(snake != null){
      var bounds = snake.CalculateBounds();
      bounds.size += (Vector3)padding;
      bounds.center = new Vector3(bounds.center.x, bounds.center.y, Constants.defaultCameraPosition.z) + (Vector3)margin;

      return bounds;
    }else{
      // Debug.Log("Could not find bounds");
      return null;
    }

  }
}