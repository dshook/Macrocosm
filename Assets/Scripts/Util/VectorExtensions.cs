using System.Linq;
using UnityEngine;

public static class VectorExtensions
{
  public static Vector2 AddX(this Vector2 vec, float amt)
  {
    return new Vector2(vec.x + amt, vec.y);
  }

  public static Vector2 AddY(this Vector2 vec, float amt)
  {
    return new Vector2(vec.x, vec.y + amt);
  }

  public static Vector2 Add(this Vector2 vec, float xAmt, float yAmt)
  {
    return new Vector2(vec.x + xAmt, vec.y + yAmt);
  }

  public static Vector2 Add(this Vector2 vec, float amt)
  {
    return new Vector2(vec.x + amt, vec.y + amt);
  }

  public static Vector2 SetX(this Vector2 vec, float value)
  {
      vec.Set(value, vec.y);
      return vec;
  }

  public static Vector2 SetY(this Vector2 vec, float value)
  {
      vec.Set(vec.x, value);
      return vec;
  }

  public static Vector3 Set(this Vector3 vec, Vector2 value)
  {
      vec.Set(value.x, value.y, vec.z);
      return vec;
  }

  public static Vector3 SetX(this Vector3 vec, float value)
  {
      vec.Set(value, vec.y, vec.z);
      return vec;
  }

  public static Vector3 SetY(this Vector3 vec, float value)
  {
      vec.Set(vec.x, value, vec.z);
      return vec;
  }

  public static Vector3 SetZ(this Vector3 vec, float value)
  {
      vec.Set(vec.x, vec.y, value);
      return vec;
  }

  public static Vector3 AddX(this Vector3 vec, float amt)
  {
      return new Vector3(vec.x + amt, vec.y, vec.z);
  }

  public static Vector3 AddY(this Vector3 vec, float amt)
  {
      return new Vector3(vec.x, vec.y + amt, vec.z);
  }

  public static Vector3 AddZ(this Vector3 vec, float amt)
  {
      return new Vector3(vec.x, vec.y, vec.z + amt);
  }

  public static Vector3 Add(this Vector3 vec, float xAmt, float yAmt, float zAmt)
  {
      return new Vector3(vec.x + xAmt, vec.y + yAmt, vec.z + zAmt);
  }

  public static Vector3 Add(this Vector3 vec, float amt)
  {
      return new Vector3(vec.x + amt, vec.y + amt, vec.z + amt);
  }

  public static Vector2 Rotate(this Vector2 v, float degrees) {
    float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
    float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);

    float tx = v.x;
    float ty = v.y;
    v.x = (cos * tx) - (sin * ty);
    v.y = (sin * tx) + (cos * ty);
    return v;
  }

  public static Vector2 PerpendicularRight(this Vector3 orig){
    var vec = new Vector2(orig.y, -orig.x);
    vec.Normalize();
    return vec;
  }

  public static Vector2 PerpendicularLeft(this Vector3 orig){
    var vec = new Vector2(orig.y, -orig.x);
    vec.Normalize();
    return vec * -1;
  }

  //Mokes sure the vector points directly along one axis
  public static Vector3 Straighten(this Vector3 orig){
    var ax = Mathf.Abs(orig.x);
    var ay = Mathf.Abs(orig.y);
    var az = Mathf.Abs(orig.z);

    if(ax > ay && ax > az){
      return new Vector3(orig.x, 0, 0);
    }
    if(ay > ax && ay > az){
      return new Vector3(0, orig.y, 0);
    }
    if(az > ax && az > ay){
      return new Vector3(0, 0, orig.z);
    }

    return orig;
  }

  public static Vector3 Swirl(this Vector3 position, Vector3 axis, float amount)
  {
    var d = position.magnitude;

    var a = (float)Mathf.Pow(d, 0.1f) * amount;
    return Quaternion.AngleAxis(a, axis) * position;
  }

  //linePnt - point the line passes through
  //lineDir - unit vector in direction of line, either direction works
  //pnt - the point to find nearest on line for
  public static Vector2 NearestPointOnLine(Vector2 linePnt, Vector2 lineDir, Vector2 pnt)
  {
    lineDir.Normalize();//this needs to be a unit vector
    var v = pnt - linePnt;
    var d = Vector2.Dot(v, lineDir);
    return linePnt + lineDir * d;
  }

  public static Bounds GetBounds(this Vector3[] points){
    var minX = points.Min(v => v.x);
    var maxX = points.Max(v => v.x);
    var minY = points.Min(v => v.y);
    var maxY = points.Max(v => v.y);

    var middle = new Vector2(minX + (maxX - minX) / 2f, minY + (maxY - minY) / 2f);
    var size = new Vector2(maxX - minX, maxY - minY);

    return new Bounds(middle, size);
  }

  //
  // Summary:
  //     Is other bounds completely contained in the bounding box?
  //
  public static bool ContainsBounds(this Bounds orig, Bounds other){
    if(
      orig.max.x >= other.max.x &&
      orig.max.y >= other.max.y &&
      orig.max.z >= other.max.z &&

      orig.min.x <= other.min.x &&
      orig.min.y <= other.min.y &&
      orig.min.z <= other.min.z
    ){
      return true;
    }

    return false;
  }

  //use Vector2.SignedAngle ?
  public static float GetAngleBetween(Vector2 source, Vector2 target){
    return Mathf.DeltaAngle(Mathf.Atan2(source.y, source.x) * Mathf.Rad2Deg,
                            Mathf.Atan2(target.y, target.x) * Mathf.Rad2Deg);
  }

}