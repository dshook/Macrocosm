using UnityEngine;
using System;
using System.Linq;

//A Bezier spline for use in canvas UI using rect transforms
public class BezierSplineUI : MonoBehaviour
{

  [SerializeField]
  private bool loop;

  public bool Loop {
    get {
      return loop;
    }
    set {
      loop = value;
    }
  }

  public RectTransform[] childTransforms {
    get
    {
      return GetComponentsInChildren<RectTransform>();
    }
  }

  public Vector3[] Points
  {
    get
    {
      return childTransforms.Select(rt => rt.position).ToArray();;
    }
  }

  public int ControlPointCount
  {
    get
    {
      return Points.Length;
    }
  }

  public Vector3 GetControlPoint(int index)
  {
    return Points[index];
  }


  public int CurveCount
  {
    get
    {
      return (Points.Length - 1) / 3;
    }
  }

  public bool IsValid {
    get{ return CurveCount > 0; }
  }

  public Vector3 GetLocalPoint(float t)
  {
    int i;
    if (t >= 1f)
    {
      t = 1f;
      i = Points.Length - 4;
    }
    else
    {
      t = Mathf.Clamp01(t) * CurveCount;
      i = (int)t;
      t -= i;
      i *= 3;
    }
    return Bezier.GetPoint(Points[i], Points[i + 1], Points[i + 2], Points[i + 3], t);
  }

  public Vector3 GetPoint(float t)
  {
    return transform.TransformPoint(GetLocalPoint(t));
  }

  public Vector3 GetPointFromBeginning(float t)
  {
    int i = 0;
    i = (int)t;
    t -= i;
    i *= 3;

    if (i > Points.Length - 4)
    {
      i = Points.Length - 4;
    }

    return transform.TransformPoint(Bezier.GetPoint(Points[i], Points[i + 1], Points[i + 2], Points[i + 3], t));
  }

  public Vector3 GetVelocity(float t)
  {
    int i;
    if (t >= 1f)
    {
      t = 1f;
      i = Points.Length - 4;
    }
    else
    {
      t = Mathf.Clamp01(t) * CurveCount;
      i = (int)t;
      t -= i;
      i *= 3;
    }
    return transform.TransformDirection(Bezier.GetFirstDerivative(Points[i], Points[i + 1], Points[i + 2], Points[i + 3], t));
  }

  public Vector3 GetDirection(float t)
  {
    return GetVelocity(t).normalized;
  }


  public float Length(int samplePoints)
  {
    float interval = 1f / samplePoints;
    float sum = 0f;
    Vector3? lastPos = null;
    for (int i = 0; i < samplePoints; i++)
    {
      var position = GetLocalPoint(interval * i);
      if (lastPos.HasValue)
      {
        sum += Vector3.Distance(position, lastPos.Value);
      }
      lastPos = position;
    }

    return sum;
  }
}