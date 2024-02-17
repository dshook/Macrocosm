using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(LineRenderer), typeof(BezierSpline))]
public class SplineToLineRenderer : MonoBehaviour {

  [Range(1, 50)]
  public int sampleFrequency = 5;

  BezierSpline spline;
  LineRenderer lineRenderer;

  private void Awake () {
    spline = GetComponent<BezierSpline>();
    lineRenderer = GetComponent<LineRenderer>();

    GenerateMesh();
  }

  void Update()
  {
  }

  private Vector3[] vertices;

  public void GenerateMesh(){
    lineRenderer.positionCount = sampleFrequency + 1;


    //iterate over our samples adding two vertices for each one
    for(int s = 0; s <= sampleFrequency; s++){
      float interval = s / (float)sampleFrequency;

      //get point along spline, and translate to local coords from world
      var point = transform.InverseTransformPoint(spline.GetPoint(interval));

      lineRenderer.SetPosition(s, point);
    }
  }


}