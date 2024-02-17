using System;
using System.Linq;
using UnityEngine;

public static class EnumExtensions{
  //Only works for auto valued enums
  public static T GetRandom<T>() where T : struct, IConvertible
  {
    var randomRange = UnityEngine.Random.Range(0, Enum.GetValues(typeof(T)).Length);
    // return (T)Convert.ChangeType(randomRange, typeof(T));
    // return (T)randomRange;
    return (T)Enum.ToObject(typeof(T), randomRange);
  }
}