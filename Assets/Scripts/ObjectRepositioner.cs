using UnityEngine;
using TMPro;


//Move game objects inside the holder that are way outside the camera bounds to be close to,
//but still outside the bounds of the camera
public class ObjectRepositioner : MonoBehaviour
{
  public Transform holder;
  public int howManyPerFrame = 3;
  public float multiplierPlacementRadius = 1.03f;

  [Range(0, 1f)]
  public float idealTotalDensity = 0.10f;

  // public TMP_Text debugText;

  float _innerRadius = 0f;
  public float innerRadius {
    get{
      return _innerRadius;
    }
  }

  float _outerRadius = 0f;
  public float outerRadius {
    get{
      return _outerRadius;
    }
  }

  Camera cam;
  void Awake(){
    cam = Camera.main;
  }

  void Update () {
    if(holder.childCount == 0 ){ return; }

    var camBounds = GetCamBounds();
    var camRadius = Mathf.Min(camBounds.topRight.x - camBounds.topLeft.x, camBounds.topLeft.y - camBounds.bottomLeft.y);
    _innerRadius = multiplierPlacementRadius * camRadius;

    _outerRadius = GetMaxDistanceRadius(holder.childCount);

    for(int i = 0; i < howManyPerFrame; i++){
      var randIndex = UnityEngine.Random.Range(0, holder.childCount);
      var child = holder.GetChild(randIndex);
      var childPos = child.position;

      if(Vector2.Distance(childPos, cam.transform.position) < outerRadius){
        continue;
      }

      //find a new position for it
      //calculate a donut that starts slightly bigger than the camera bounds, and ends at the max distance
      //then grab a random point off in it and there we go
      child.position = RandomExtensions.RandomPointInCircle(innerRadius, outerRadius, cam.transform.position);

    }

    /*

    //debug stuff
    var circleArea = Mathf.PI * Mathf.Pow(outerRadius, 2);
    var totalDensity = holder.childCount / circleArea;
    var countInsideInnerCircle = 0;
    for(int t = 0; t < holder.childCount; t++){
      if(Vector2.Distance(holder.GetChild(t).transform.position, cam.transform.position) < innerRadius){
        countInsideInnerCircle++;
      }
    }
    var innerDensity = countInsideInnerCircle / (Mathf.PI * Mathf.Pow(innerRadius, 2));
    debugText.text = string.Format("T: {0:0.00}<br>I: {1:0.00}", totalDensity, innerDensity);

    */
  }

  public float GetMaxDistanceRadius(int atomCount){
    return Mathf.Sqrt(atomCount / idealTotalDensity / Mathf.PI);
  }

/*
  void OnDrawGizmos()
  {
    var camBounds = GetCamBounds();
    var camRadius = Mathf.Min(camBounds.topRight.x - camBounds.topLeft.x, camBounds.topLeft.y - camBounds.bottomLeft.y);
    var innerRadius = multiplierPlacementRadius * camRadius;

    Gizmos.color = Color.yellow;
    Gizmos.DrawWireSphere(cam.transform.position, camRadius);

    Gizmos.DrawWireSphere(cam.transform.position, innerRadius);

    Gizmos.DrawWireSphere(cam.transform.position, outerRadius);
  }
*/

  struct RectCorners{
    public Vector2 topLeft;
    public Vector2 topRight;
    public Vector2 bottomRight;
    public Vector2 bottomLeft;
  }

  RectCorners GetCamBounds(){
    var vertExtent = cam.orthographicSize;
    var horzExtent = vertExtent * Screen.width / Screen.height;

    var center = cam.transform.position;
    var topY = center.y + vertExtent;
    var botY = center.y - vertExtent;
    var leftX = center.x - horzExtent;
    var rightX = center.x + horzExtent;

    // return new Bounds(cam.transform.position, new Vector2(horzExtent, vertExtent));

    return new RectCorners(){
      topLeft = new Vector2(leftX, topY),
      topRight = new Vector2(rightX, topY),
      bottomLeft = new Vector2(leftX, botY),
      bottomRight = new Vector2(rightX, botY),
    };
  }
}