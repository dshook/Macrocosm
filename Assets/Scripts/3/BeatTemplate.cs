using System.Collections.Generic;
using UnityEngine;

public class BeatTemplate : MonoBehaviour
{
  public float beatLength;

  public BeatTemplateItem[] items;
}

[System.Serializable]
public class BeatTemplateItem {
  public RectTransform position;
  public BezierSpline spline;

  public BeatType type;

  public float beat;

  //Stuff Only for tracking in game
  [System.NonSerialized]
  public bool fired = false;

  [System.NonSerialized]
  public bool completed = false;
}

public enum BeatType{
  Single,
  Slide,
  Multi,
  SlideReverse,
  Double,
}

[System.Serializable]
public class BeatTypeComparer : IEqualityComparer<BeatType>
{
  public bool Equals(BeatType a, BeatType b){ return a == b; }
  public int GetHashCode(BeatType a){ return (int)a; }
}