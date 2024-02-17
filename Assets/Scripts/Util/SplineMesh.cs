using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(BezierSpline))]
public class SplineMesh : MonoBehaviour {

  [Range(1, 20)]
  public int sampleFrequency = 5;

  [Range(0, 5f)]
  public float lineWidth = 0.3f;

  public AnimationCurve widthCurve;

  BezierSpline spline;
  Mesh mesh;

  private void Awake () {
    spline = GetComponent<BezierSpline>();
    mesh = GetComponent<Mesh>();

    GenerateMesh();
  }

  void Update()
  {
    /*
    for(int i = 0; i <= sampleFrequency; i++){
      float interval = i / (float)sampleFrequency;

      var point = spline.GetPoint(interval);
      var direction = spline.GetDirection(interval);

      var perpendicularLeftVec = PerpendicularLeft(direction) * lineWidth;
      var perpendicularRightVec = PerpendicularRight(direction) * lineWidth;

      Debug.DrawLine(point, point + (Vector3)perpendicularLeftVec, Color.magenta, 0.5f, false);
      Debug.DrawLine(point, point + (Vector3)perpendicularRightVec, Color.cyan, 0.5f, false);
    }
    */
  }

  private Vector3[] vertices;

  public void GenerateMesh(){
    vertices = new Vector3[(sampleFrequency + 1) * 2];

    //iterate over our samples adding two vertices for each one
    for(int s = 0, i = 0; s <= sampleFrequency; s++, i += 2){
      float interval = s / (float)sampleFrequency;

      //get point along spline, and translate to local coords from world
      var point = transform.InverseTransformPoint(spline.GetPoint(interval));
      var direction = spline.GetDirection(interval);

      var positionWidth = widthCurve.Evaluate(interval) * lineWidth;

      var perpendicularLeftVec = direction.PerpendicularLeft() * positionWidth;
      var perpendicularRightVec = direction.PerpendicularRight() * positionWidth;
      // var perpendicularVec = turnLeft ? PerpendicularLeft(diffVector) : PerpendicularRight(diffVector);

      vertices[i] = point + (Vector3)perpendicularLeftVec;
      vertices[i + 1] = point + (Vector3)perpendicularRightVec;
    }

    GetComponent<MeshFilter>().mesh = mesh = new Mesh();
    mesh.name = "Spline Mesh";

    mesh.vertices = vertices;

    //now figure out our triangles
    int [] triangles = new int[sampleFrequency * 6];
    for(int s = 0, ti = 0, vi = 0; s < sampleFrequency; s++, ti += 6, vi += 2){
      //first tri
      triangles[ti] = vi;
      triangles[ti + 1] = vi + 3;
      triangles[ti + 2] = vi + 1;
      //second matching tri
      triangles[ti + 3] = vi;
      triangles[ti + 4] = vi + 2;
      triangles[ti + 5] = vi + 3;
    }

    mesh.triangles = triangles;
    mesh.RecalculateNormals();

    // Debug.Log("Generated Spline Mesh");
  }


}