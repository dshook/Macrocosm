using UnityEngine;

[System.Serializable]
public struct Int2 {

    public int x;
    public int y;

    public Int2(int x, int y){
      this.x = x;
      this.y = y;
    }

    //
    // Summary:
    //     Shorthand for writing Int2(1, 0).
    public static Int2 right { get{ return new Int2(1, 0); } }
    //
    // Summary:
    //     Shorthand for writing Int2(-1, 0).
    public static Int2 left { get{ return new Int2(-1, 0); } }
    //
    // Summary:
    //     Shorthand for writing Int2(0, -1).
    public static Int2 down { get{ return new Int2(0, -1); } }
    //
    // Summary:
    //     Shorthand for writing Int2(0, 1).
    public static Int2 up { get{ return new Int2(0, 1); } }
    //
    // Summary:
    //     Shorthand for writing Int2(1, 1).
    public static Int2 one { get{ return new Int2(1, 1); } }
    //
    // Summary:
    //     Shorthand for writing Int2(0, 0).
    public static Int2 zero { get{ return new Int2(0, 0); } }

    public static Int2 operator +(Int2 a, Int2 b){
      return new Int2(a.x + b.x, a.y + b.y);
    }

    public static Int2 operator -(Int2 a){
      return new Int2(-a.x, -a.y);
    }

    public static Int2 operator -(Int2 a, Int2 b){
      return new Int2(a.x - b.x, a.y - b.y);
    }

    public static Int2 operator *(int d, Int2 a){
      return new Int2(a.x * d, a.y * d);
    }

    public static Int2 operator *(Int2 a, int d){
      return new Int2(a.x * d, a.y * d);
    }

    public static Int2 operator *(Int2 a, Int2 b){
      return new Int2(a.x * b.x, a.y * b.y);
    }

    public static bool operator ==(Int2 lhs, Int2 rhs){
      return lhs.x == rhs.x && lhs.y == rhs.y;
    }

    public static bool operator !=(Int2 lhs, Int2 rhs){
      return lhs.x != rhs.x || lhs.y != rhs.y;
    }

    public static implicit operator Vector2(Int2 v){
      return new Vector2(v.x, v.y);
    }

    public static implicit operator Int2(Vector2 v){
      return new Int2((int)v.x, (int)v.y);
    }

    public bool Equals(Int2 other){
      return this == other;
    }

    public override bool Equals(object other){
      var casted = other as Int2?;
      if (casted != null)
      {
        return casted.Value == this;
      }
      return false;
    }

    public override int GetHashCode(){
      int hash = 13;
      hash = (hash * 7) + x.GetHashCode();
      hash = (hash * 7) + y.GetHashCode();
      return hash;
    }

    public override string ToString(){
      return x + "," + y;
    }
}