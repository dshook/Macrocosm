using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct HexCoordinates {

  public const float outerRadius = 0.475f;
  public const float innerRadius = outerRadius * 0.866025404f;

  [SerializeField]
  private int x, z;

  public int X {
    get {
      return x;
    }
  }

  public int Z {
    get {
      return z;
    }
  }

  public int Y {
    get {
      return -X - Z;
    }
  }

  public HexCoordinates (int x, int z) {
    this.x = x;
    this.z = z;
  }

  public static HexCoordinates FromOffsetCoordinates (int x, int z) {
    return new HexCoordinates(x - z / 2, z);
  }

  public static HexCoordinates FromPosition (Vector3 position) {
    float x = position.x / (innerRadius * 2f);
    float y = -x;

    float offset = position.y / (outerRadius * 3f);
    x -= offset;
    y -= offset;

    int iX = Mathf.RoundToInt(x);
    int iY = Mathf.RoundToInt(y);
    int iZ = Mathf.RoundToInt(-x -y);

    if (iX + iY + iZ != 0) {
      float dX = Mathf.Abs(x - iX);
      float dY = Mathf.Abs(y - iY);
      float dZ = Mathf.Abs(-x -y - iZ);

      if (dX > dY && dX > dZ) {
        iX = -iY - iZ;
      }
      else if (dZ > dY) {
        iZ = -iX - iY;
      }
    }

    return new HexCoordinates(iX, iZ);
  }

  public static Dictionary<HexCornerDirection, Vector3> corners = new Dictionary<HexCornerDirection, Vector3>(){
    {HexCornerDirection.N,  new Vector3(0f, outerRadius, 0f)},
    {HexCornerDirection.NE, new Vector3(innerRadius, 0.5f * outerRadius, 0f)},
    {HexCornerDirection.SE, new Vector3(innerRadius, -0.5f * outerRadius, 0f)},
    {HexCornerDirection.S,  new Vector3(0f, -outerRadius, 0f)},
    {HexCornerDirection.SW, new Vector3(-innerRadius, -0.5f * outerRadius, 0f)},
    {HexCornerDirection.NW, new Vector3(-innerRadius, 0.5f * outerRadius, 0f)},
  };


  public int DistanceTo (HexCoordinates other) {
    return
      ((x < other.x ? other.x - x : x - other.x) +
      (Y < other.Y ? other.Y - Y : Y - other.Y) +
      (z < other.z ? other.z - z : z - other.z)) / 2;
  }

  public static HexCoordinates Neighbor(HexCoordinates coordinates, HexDirection direction) {
    return coordinates + direction.Offset();
  }

  public static HexCoordinates operator +(HexCoordinates a, HexCoordinates b){
    return new HexCoordinates(a.x + b.x, a.z + b.z);
  }

  public static HexCoordinates operator -(HexCoordinates a, HexCoordinates b){
    return new HexCoordinates(a.x - b.x, a.z - b.z);
  }

  public static HexCoordinates operator *(HexCoordinates a, int k){
    return new HexCoordinates(a.x * k, a.z * k);
  }

  public static bool operator ==(HexCoordinates a, HexCoordinates b){
    return a.x == b.x && a.z == b.z;
  }

  public static bool operator !=(HexCoordinates a, HexCoordinates b){
    return a.x != b.x || a.z != b.z;
  }

  public bool Equals(HexCoordinates other){
    return this == other;
  }

  public override bool Equals(object other){
    var casted = other as HexCoordinates?;
    if (casted != null)
    {
      return casted.Value == this;
    }
    return false;
  }

  public override int GetHashCode(){
    int hash = 13;
    hash = (hash * 7) + x.GetHashCode();
    hash = (hash * 7) + z.GetHashCode();
    return hash;
  }

  public override string ToString () {
    return "(" +
      X.ToString() + ", " + Y.ToString() + ", " + Z.ToString() + ")";
  }

  public string ToStringOnSeparateLines () {
    return X.ToString() + "\n" + Y.ToString() + "\n" + Z.ToString();
  }

}

public enum HexDirection {
  NE, E, SE, SW, W, NW
}

public enum HexCornerDirection {
  N, NE, SE, S, SW, NW
}

public static class HexDirectionExtensions {

  public static HexDirection Opposite (this HexDirection direction) {
    return (int)direction < 3 ? (direction + 3) : (direction - 3);
  }

  public static HexDirection Previous (this HexDirection direction) {
    return direction == HexDirection.NE ? HexDirection.NW : (direction - 1);
  }

  public static HexDirection Next (this HexDirection direction) {
    return direction == HexDirection.NW ? HexDirection.NE : (direction + 1);
  }

  public static HexDirection Previous2 (this HexDirection direction) {
    direction -= 2;
    return direction >= HexDirection.NE ? direction : (direction + 6);
  }

  public static HexDirection Next2 (this HexDirection direction) {
    direction += 2;
    return direction <= HexDirection.NW ? direction : (direction - 6);
  }

  public static HexCoordinates Offset(this HexDirection direction){
    switch(direction){
      case HexDirection.NE:
        return new HexCoordinates(0, 1);
      case HexDirection.E:
        return new HexCoordinates(1, 0);
      case HexDirection.SE:
        return new HexCoordinates(1, -1);
      case HexDirection.SW:
        return new HexCoordinates(0, -1);
      case HexDirection.W:
        return new HexCoordinates(-1, 0);
      case HexDirection.NW:
        return new HexCoordinates(-1, 1);
    }
    return new HexCoordinates(0, 0);
  }

  //inverse of above and only handles neighboring coordinates
  public static HexDirection CoordDirection(HexCoordinates first, HexCoordinates second){
    var xDiff = second.X - first.X;
    var zDiff = second.Z - first.Z;

    if(xDiff == 0 && zDiff == 1){
      return HexDirection.NE;
    }
    if(xDiff == 1 && zDiff == 0){
      return HexDirection.E;
    }
    if(xDiff == 1 && zDiff == -1){
      return HexDirection.SE;
    }
    if(xDiff == 0 && zDiff == -1){
      return HexDirection.SW;
    }
    if(xDiff == -1 && zDiff == 0){
      return HexDirection.W;
    }
    if(xDiff == -1 && zDiff == 1){
      return HexDirection.NW;
    }

    Debug.LogWarning("Getting default coord direction because you're comparing not neighbor cells");
    return HexDirection.NE;
  }

  public static HexCornerDirection Previous (this HexCornerDirection direction) {
    return direction == HexCornerDirection.N ? HexCornerDirection.NW : (direction - 1);
  }

  public static HexCornerDirection Next (this HexCornerDirection direction) {
    return direction == HexCornerDirection.NW ? HexCornerDirection.N : (direction + 1);
  }
}