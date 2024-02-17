
using TMPro;
using UnityEngine;

public class TriangleControl : MonoBehaviour {
  public RectTransform rectTransform;
  public RectTransform control;

  public TextMeshProUGUI topValue;
  public TextMeshProUGUI topLabel;

  public TextMeshProUGUI leftValue;
  public TextMeshProUGUI leftLabel;

  public TextMeshProUGUI rightValue;
  public TextMeshProUGUI rightLabel;

  //assumes the points are in the order top, right, left
  public RectTransform[] triangleBounds = new RectTransform[3];

  Camera worldCamera;

  const float third = 1f/3f;

  float topAmt = third;
  float leftAmt = third;
  float rightAmt = third;

  public float TopAmt { get{ return topAmt; }}
  public float LeftAmt { get{ return leftAmt; }}
  public float RightAmt { get{ return rightAmt; }}

  Vector2 center;
  float sideLength, triangleHeight, apothem, cornerToCenterDistance, rightSideSlope, leftSideSlope, rightIntercept, leftIntercept;
  float sqrtThree = Mathf.Sqrt(3f);

  bool dragging = false;

  void Awake()
  {
    worldCamera = GetComponentInParent<Canvas>().worldCamera;
    //the length in anchored position coordinates of course
    sideLength = triangleBounds[1].anchoredPosition.x - triangleBounds[2].anchoredPosition.x;
    triangleHeight = sideLength / 2f * sqrtThree;

    //center the control
    apothem = sideLength / (2f * Mathf.Tan(Mathf.PI / 3f));
    center = new Vector2(0, triangleBounds[1].anchoredPosition.y + apothem);
    control.anchoredPosition = center;

    cornerToCenterDistance = Vector2.Distance(center, triangleBounds[0].anchoredPosition);
    // cornerToCenterDistance = Vector2.Distance(center, triangleBounds[1].anchoredPosition);
    // cornerToCenterDistance = Vector2.Distance(center, triangleBounds[2].anchoredPosition);

    // Debug.Log("T - R " + Vector2.Distance(triangleBounds[0].anchoredPosition, triangleBounds[1].anchoredPosition));
    // Debug.Log("R - L " + Vector2.Distance(triangleBounds[1].anchoredPosition, triangleBounds[2].anchoredPosition));
    // Debug.Log("L - T " + Vector2.Distance(triangleBounds[2].anchoredPosition, triangleBounds[0].anchoredPosition));

    rightSideSlope = (triangleBounds[1].anchoredPosition.y - triangleBounds[0].anchoredPosition.y) / (triangleBounds[1].anchoredPosition.x - triangleBounds[0].anchoredPosition.x);
    leftSideSlope  = (triangleBounds[2].anchoredPosition.y - triangleBounds[0].anchoredPosition.y) / (triangleBounds[2].anchoredPosition.x - triangleBounds[0].anchoredPosition.x);
    rightIntercept = triangleBounds[0].anchoredPosition.y - rightSideSlope * triangleBounds[0].anchoredPosition.x;
    leftIntercept  = triangleBounds[0].anchoredPosition.y - leftSideSlope * triangleBounds[0].anchoredPosition.x;

    CalculateAmts();
    UpdateLabels();

  }

  void Update(){
    if(dragging){
      UpdateControl();
    }
  }

  void UpdateControl(){
    Vector2 localpoint;
    RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, Input.mousePosition, worldCamera, out localpoint);
    control.localPosition = localpoint;

    ConstrainControl();
    CalculateAmts();
    UpdateLabels();
  }

  void ConstrainControl(){
    //bottom line
    if(control.anchoredPosition.y < triangleBounds[1].anchoredPosition.y){
      control.anchoredPosition = new Vector2(control.anchoredPosition.x, triangleBounds[1].anchoredPosition.y);
    }

    //top
    if(control.anchoredPosition.y > triangleBounds[0].anchoredPosition.y){
      control.anchoredPosition = new Vector2(control.anchoredPosition.x, triangleBounds[0].anchoredPosition.y);
    }

    //right side
    var Ax = triangleBounds[0].anchoredPosition.x;
    var Ay = triangleBounds[0].anchoredPosition.y;
    var Bx = triangleBounds[1].anchoredPosition.x;
    var By = triangleBounds[1].anchoredPosition.y;
    var X = control.anchoredPosition.x;
    var Y = control.anchoredPosition.y;
    var rightSideSign = Mathf.Sign((Bx - Ax) * (Y - Ay) - (By - Ay) * (X - Ax));

    if(rightSideSign > 0){
      //intersect based on the control's y position onto the right side line to see where we should put it
      var rightSideX = (control.anchoredPosition.y - rightIntercept) / rightSideSlope;
      control.anchoredPosition = new Vector2(rightSideX, Mathf.Min(control.anchoredPosition.y, triangleBounds[0].anchoredPosition.y));
    }

    //left side, same as right side but flipped ofc
    Ax = triangleBounds[0].anchoredPosition.x;
    Ay = triangleBounds[0].anchoredPosition.y;
    Bx = triangleBounds[2].anchoredPosition.x;
    By = triangleBounds[2].anchoredPosition.y;
    X = control.anchoredPosition.x;
    Y = control.anchoredPosition.y;
    var leftSideSign = Mathf.Sign((Bx - Ax) * (Y - Ay) - (By - Ay) * (X - Ax));

    if(leftSideSign < 0){
      var leftSideX = (control.anchoredPosition.y - leftIntercept) / leftSideSlope;
      control.anchoredPosition = new Vector2(leftSideX, Mathf.Min(control.anchoredPosition.y, triangleBounds[0].anchoredPosition.y));
    }

  }

  void CalculateAmts(){
    var controlPos = control.anchoredPosition;
    var topPos   = triangleBounds[0].anchoredPosition;
    var rightPos = triangleBounds[1].anchoredPosition;
    var leftPos  = triangleBounds[2].anchoredPosition;

    //need to factor in the angle then blending the triangle height with side length
    var nearestPointOnBottomLine = VectorExtensions.NearestPointOnLine(leftPos, rightPos - leftPos, controlPos);
    var nearestPointOnLeftLine = VectorExtensions.NearestPointOnLine(leftPos, topPos - leftPos, controlPos);
    var nearestPointOnRightLine = VectorExtensions.NearestPointOnLine(rightPos, topPos - rightPos, controlPos);
    topAmt   = Vector2.Distance(controlPos, nearestPointOnBottomLine) / triangleHeight;
    rightAmt = Vector2.Distance(controlPos, nearestPointOnLeftLine) / triangleHeight;
    leftAmt  = Vector2.Distance(controlPos, nearestPointOnRightLine) / triangleHeight;

  }

  void SetControlPosFromAmts(){
    var topWeight   = topAmt   * triangleBounds[0].anchoredPosition;
    var rightWeight = rightAmt * triangleBounds[1].anchoredPosition;
    var leftWeight  = leftAmt  * triangleBounds[2].anchoredPosition;

    control.anchoredPosition = topWeight + rightWeight + leftWeight;
  }

  void UpdateLabels(){
    UpdateLabel(topValue, topAmt);
    UpdateLabel(leftValue, leftAmt);
    UpdateLabel(rightValue, rightAmt);
  }

  public void SetAmts(float top, float left, float right){
    topAmt = top;
    leftAmt = left;
    rightAmt = right;

    SetControlPosFromAmts();
    ConstrainControl();
    CalculateAmts();
    UpdateLabels();
  }

  void UpdateLabel(TextMeshProUGUI text, float value){
    text.text = value.ToString("0%");
  }

  //All these set up with event triggers
  public void OnControlPointerDown(){
    dragging = true;
  }

  public void OnControlPointerUp(){
    dragging = false;
    CalculateAmts();
    UpdateLabels();
  }

  public void OnBackgroundClick(){
    dragging = true;
    UpdateControl();
  }
}