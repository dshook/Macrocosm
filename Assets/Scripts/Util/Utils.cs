using UnityEngine;

public static class Utils
{
  public static bool Approximately(float a, float b, float epsilon = 0.001f)
  {
    return Mathf.Abs(a - b) < epsilon;
  }

  /// <summary>
  /// Remaps value x between AB to CD
  /// </summary>
  /// <param name="x"></param>
  /// <param name="A"></param>
  /// <param name="B"></param>
  /// <param name="C"></param>
  /// <param name="D"></param>
  /// <returns></returns>
  public static float Remap(float x, float A, float B, float C, float D)
  {
    float remappedValue = C + (x - A) / (B - A) * (D - C);
    return remappedValue;
  }
}