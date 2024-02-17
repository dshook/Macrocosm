using Shapes;
using UnityEngine;

public class GalaxyRouteLine : MonoBehaviour {
  Star _origin;
  public Star origin {
    get {return _origin; }
    set{
      _origin = value;
      UpdatePositions();
    }
  }
  Star _dest;
  public Star dest {
    get {return _dest; }
    set{
      _dest = value;
      UpdatePositions();
    }
  }

  public uint routeId;
  public static float baseWidth = 0.04f;

  //How many total lines of other routes have the same src & dest
  public uint sharedRoutes = 0;
  //based on route id, indicates how far the line should be bumped
  public uint sharePriority = 0;

  public Line lineRenderer;

  Vector2 _lineOffset;
  public Vector2 lineOffset{
    get{ return _lineOffset; }
  }

  public Vector3 startPosition{
    get{
      return lineRenderer.Start;
    }
    set {
      lineRenderer.Start = value;
    }
  }

  public Vector3 endPosition{
    get{
      return lineRenderer.End;
    }
    set {
      lineRenderer.End = value;
    }
  }

  public void UpdatePositions(){
    if(origin != null){
      this.startPosition = origin.transform.position;
    }
    if(dest != null){
      this.endPosition = dest.transform.position;
    }

    //anchored route line logic for route sharing
    var finalWidth = baseWidth - (sharedRoutes * 0.05f * baseWidth);
    lineRenderer.Thickness = finalWidth;

    if(sharedRoutes == 0 || origin == null || dest == null){
      //done for single route between stars
      return;
    }

    //offset the line based on how many shares they are to spread out all the lines
    var evenRoutes = sharedRoutes % 2 == 0;
    var baseBumpAmount = finalWidth;
    var extraBump = evenRoutes ? -0.5f * baseBumpAmount : 0;

    var highPoint = startPosition.y > endPosition.y ? startPosition : endPosition;
    var lowPoint = highPoint == startPosition ? endPosition : startPosition;
    var vecDiff = highPoint - lowPoint;

    var evenOffset = evenRoutes ? 0 : 1;

    var finalBumpAmount = Mathf.CeilToInt((float)(sharePriority - evenOffset) / 2f) * baseBumpAmount + extraBump;
    var direction = (sharePriority % 2 == 0) ? vecDiff.PerpendicularRight() : vecDiff.PerpendicularLeft();

    _lineOffset = finalBumpAmount * direction;

    var spreadPosition0 = (Vector2)startPosition + _lineOffset;
    var spreadPosition1 = (Vector2)endPosition + _lineOffset;
    startPosition = spreadPosition0;
    endPosition = spreadPosition1;

    //now offset the ends so they land on the settlement indicator circle on both sides
    var radius0 = origin.settlementIndicatorRenderer.Radius;
    var radius1 = dest.settlementIndicatorRenderer.Radius;

    var theta0 = Mathf.Acos(finalBumpAmount / radius0);
    var theta1 = Mathf.Acos(finalBumpAmount / radius1);

    //Opposite on the trig triangle
    var circleOffset0 = Mathf.Sin(theta0) * radius0;
    var circleOffset1 = Mathf.Sin(theta1) * radius1;

    var offsetPosition0 = spreadPosition0 - ( (spreadPosition0 - spreadPosition1).normalized * circleOffset0);
    var offsetPosition1 = spreadPosition1 - ( (spreadPosition1 - spreadPosition0).normalized * circleOffset1);

    startPosition = offsetPosition0;
    endPosition = offsetPosition1;
  }

  public void SetColor(Color c){
    lineRenderer.Color = c;
  }

  public void SetRouteLineValidity(bool isValid){
    lineRenderer.Dashed = !isValid;
  }
}