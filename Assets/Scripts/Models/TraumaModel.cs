using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

[Singleton]
public class TraumaModel
{
  private float _trauma = 0f;

  public float trauma
  {
    get{
      return _trauma;
    }
    set{
      _trauma = Mathf.Clamp(value, 0f, 1f);
    }
  }
}