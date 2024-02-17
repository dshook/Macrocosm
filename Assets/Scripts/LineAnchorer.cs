using UnityEngine;

//give it the start and end and this will move the line points to match
public class LineAnchorer : MonoBehaviour{
  public RectTransform start;
  public RectTransform end;
  public Vector2 offset = Vector2.zero;

  public UILineRenderer lineRenderer;

  public RectTransform relativeTo;

  void LateUpdate(){
    if(relativeTo == null){
      relativeTo = GetComponent<RectTransform>();
    }

    if(start != null){

      Vector2 localStart;
      Vector2 screenStart = RectTransformUtility.WorldToScreenPoint( null, start.position );
      RectTransformUtility.ScreenPointToLocalPointInRectangle( relativeTo, screenStart, null, out localStart );
      lineRenderer.Points[0] = localStart + offset;
    }

    if(end != null){
      Vector2 localEnd;
      Vector2 screenEnd = RectTransformUtility.WorldToScreenPoint( null, end.position );
      RectTransformUtility.ScreenPointToLocalPointInRectangle( relativeTo, screenEnd, null, out localEnd );

      lineRenderer.Points[1] = localEnd + offset;
    }

    lineRenderer.SetVerticesDirty();
  }
}