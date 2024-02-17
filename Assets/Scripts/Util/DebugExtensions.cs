using System;
using UnityEngine;

public static class DebugExtensions
{
  public static void DrawDebug(this Bounds bounds, Color col){
    Debug.DrawLine(new Vector2(bounds.min.x, bounds.max.y), new Vector2(bounds.max.x, bounds.max.y), col, 15f);
    Debug.DrawLine(new Vector2(bounds.min.x, bounds.min.y), new Vector2(bounds.max.x, bounds.min.y), col, 15f);

    Debug.DrawLine(new Vector2(bounds.min.x, bounds.min.y), new Vector2(bounds.min.x, bounds.max.y), col, 15f);
    Debug.DrawLine(new Vector2(bounds.max.x, bounds.min.y), new Vector2(bounds.max.x, bounds.max.y), col, 15f);
  }

  public static void DrawPoint(Vector3 pos, Color color, float ttl = 4.5f){
    var dbgPoint = Resources.Load("Prefabs/DebugPoint") as GameObject;
    var newPoint = GameObject.Instantiate(dbgPoint, pos, Quaternion.identity);

    var rend = newPoint.GetComponent<SpriteRenderer>();
    rend.color = color;

    var killAfter = newPoint.GetComponent<DestroyAfter>();
    killAfter.timeToLive = ttl;

  }

  public static void DrawScreenPoint(Vector3 pos, Color color, float ttl = 4.5f){
    var newPos = Camera.main.ScreenToWorldPoint( pos.SetZ(Camera.main.nearClipPlane));

    DrawPoint(newPos, color, ttl);
  }

  public static void DebugWithTime(string message, float startTime){
    Debug.Log(string.Format("{0} - f{1} @{2:0.000}s Δ{3:0.000}s",
      message,
      Time.frameCount,
      startTime,
      Time.realtimeSinceStartup - startTime
    ));
  }
}