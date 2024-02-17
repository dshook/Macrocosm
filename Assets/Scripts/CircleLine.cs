using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(LineRenderer))]
public class CircleLine : MonoBehaviour
{
  [Range(3, 100)]
  public int segments;
  public float radiusX;
  public float radiusY;
  public float radius {
    get{
      return radiusX; //Lol if this causes a bug it's your own damn fault
    }
    set{
      radiusX = value;
      radiusY = value;
    }
  }
  public float width = 0.1f;

  LineRenderer line;
  public LineRenderer Line {get {return line;}}

  int _segments;
  float _radiusX;
  float _radiusY;

  void Start()
  {
    line = gameObject.GetComponent<LineRenderer>();

    line.useWorldSpace = false;
    UpdatePoints();
  }

  void Update(){
    if(
      segments != _segments ||
      radiusX != _radiusX ||
      radiusY != _radiusY ||
      line.startWidth != width ||
      line.endWidth != width
    ){
      UpdatePoints();
    }

  }

  public void UpdatePoints()
  {
    line.positionCount = segments + 1;
    line.loop = true;

    _segments = segments;
    _radiusX = radiusX;
    _radiusY = radiusY;

    float x;
    float y;
    float z = 0f;

    float angle = 20f;

    for (int i = 0; i < (segments + 1); i++)
    {
      x = Mathf.Sin(Mathf.Deg2Rad * angle) * radiusX;
      y = Mathf.Cos(Mathf.Deg2Rad * angle) * radiusY;

      line.SetPosition(i, new Vector3(x, y, z));

      angle += (360f / segments);
    }

    line.startWidth = width;
    line.endWidth = width;
  }
}
