using System.Collections.Generic;
using UnityEngine;

public static class RandomExtensions
{
  public static void Shuffle<T>(this System.Random rng, T[] array)
  {
    int n = array.Length;
    while (n > 1)
    {
      int k = rng.Next(n--);
      T temp = array[n];
      array[n] = array[k];
      array[k] = temp;
    }
  }

  public static T[] Shuffle<T>(this T[] array)
  {
    int n = array.Length;
    while (n > 1)
    {
      int k = Random.Range(0, n--);
      T temp = array[n];
      array[n] = array[k];
      array[k] = temp;
    }

    return array;
  }

  public static List<T> Shuffle<T>(this List<T> array)
  {
    int n = array.Count;
    while (n > 1)
    {
      int k = Random.Range(0, n--);
      T temp = array[n];
      array[n] = array[k];
      array[k] = temp;
    }

    return array;
  }

  public static T RandomItem<T>(this T[] array)
  {
    return array[Random.Range(0, array.Length)];
  }

  /// <summary>
  /// Generates a single value from a normal distribution, using Box-Muller
  /// https://en.wikipedia.org/wiki/Box%E2%80%93Muller_transform
  /// </summary>
  /// <param name="random">A (uniform) random number generator</param>
  /// <param name="standardDeviation">The standard deviation of the distribution</param>
  /// <param name="mean">The mean of the distribution</param>
  /// <returns>A normally distributed value</returns>
  public static float NormallyDistributedSingle(float standardDeviation, float mean)
  {
      // *****************************************************
      // Intentionally duplicated to avoid IEnumerable overhead
      // *****************************************************
      var u1 = Random.Range(0, 1f); //these are uniform(0,1) random doubles
      var u2 = Random.Range(0, 1f);

      var x1 = Mathf.Sqrt(-2.0f * Mathf.Log(u1));
      var x2 = 2.0f * Mathf.PI * u2;
      var z1 = x1 * Mathf.Sin(x2); //random normal(0,1)
      return (float)(z1 * standardDeviation + mean);
  }

  /// <summary>
  /// Generates a single normal value clamped within min and max
  /// </summary>
  /// <remarks>
  /// Originally used inverse phi method, but this method, found here:
  /// http://arxiv.org/pdf/0907.4010.pdf
  /// is significantly faster
  /// </remarks>
  /// <param name="random">A (uniform) random number generator</param>
  /// <param name="standardDeviation">The standard deviation of the distribution</param>
  /// <param name="mean">The mean of the distribution</param>
  /// <param name="min">The minimum allowed value (does not bias)</param>
  /// <param name="max">The max allowed value (does not bias)</param>
  /// <returns>A single sample from a normal distribution, clamped to within min and max in an unbiased manner.</returns>
  public static float NormallyDistributedSingle(float standardDeviation, float mean, float min, float max)
  {
      var nMax = (max - mean) / standardDeviation;
      var nMin = (min - mean) / standardDeviation;
      var nRange = nMax - nMin;
      var nMaxSq = nMax * nMax;
      var nMinSq = nMin * nMin;
      var subFrom = nMinSq;
      if (nMin < 0 && 0 < nMax) subFrom = 0;
      else if (nMax < 0) subFrom = nMaxSq;

      var sigma = 0.0;
      double u;
      float z;
      do
      {
        z = nRange * Random.Range(0, 1f) + nMin; // uniform[normMin, normMax]
        sigma = Mathf.Exp((subFrom - z * z) / 2);
        u = Random.Range(0, 1f);
      } while (u > sigma);

      return z * standardDeviation + mean;
  }

  //calculate a random point in a donut from inner radius to outer radius
  public static Vector2 RandomPointInCircle(float innerRadius, float outerRadius, Vector2 center){

    var radius = Random.Range(innerRadius, outerRadius);
    var angleInRadians = Random.Range(0, Mathf.PI * 2f);

    return new Vector2(
      center.x + radius * Mathf.Cos(angleInRadians),
      center.y + radius * Mathf.Sin(angleInRadians)
    );
  }

  public static int RollDice(int rolls, int sides){
    int sum = 0;
    for(var i = 0; i < rolls; i++){
      sum += Random.Range(1, sides + 1);
    }
    return sum;
  }

  public static float SamplePerlinNoise(Vector2 origin, Vector2 normalizedPoint, float scale) {
    Vector2 samplePoint = normalizedPoint * scale + origin;
    return Mathf.PerlinNoise(samplePoint.x, samplePoint.y);
  }

  public static float SamplePerlinOctaves(Vector2 origin, Vector2 point, float[] scales){
    float sample = 0f;
    for(var s = 0; s < scales.Length; s++){
      var scale = scales[s];
      sample += SamplePerlinNoise(origin, point, scale);
    }
    return sample / scales.Length;
  }
}
