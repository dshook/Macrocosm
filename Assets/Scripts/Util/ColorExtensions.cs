using System;
using UnityEngine;

public static class ColorExtensions
{
    public static Color ToColor(this string hex)
    {
      return HexToColor(hex);
    }

    public static Color SetR(this Color color, float value)
    {
      color = new Color(value, color.g, color.b, color.a);
      return color;
    }
    public static Color SetG(this Color color, float value)
    {
      color = new Color(color.r, value, color.b, color.a);
      return color;
    }
    public static Color SetB(this Color color, float value)
    {
      color = new Color(color.r, color.g, value, color.a);
      return color;
    }
    public static Color SetA(this Color color, float value)
    {
      color = new Color(color.r, color.g, color.b, value);
      return color;
    }

    public static Color HexToColor(string hex)
    {
      if(String.IsNullOrEmpty(hex)){
        return Color.magenta;
      }
      if(hex.Length != 6 && hex.Length != 8){
        return Color.magenta;
      }

      hex = hex.Replace("0x", string.Empty);//in case the string is formatted 0xFFFFFF
      hex = hex.Replace("#", string.Empty);//in case the string is formatted #FFFFFF
      byte a = 255;//assume fully visible unless specified in hex
      byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
      byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
      byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
      //Only use alpha if the string has enough characters
      if (hex.Length == 8)
      {
        a = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
      }
      return new Color32(r, g, b, a);
    }

    public static string ToHex(this Color32 c)
    {
      return c.r.ToString("X2") + c.g.ToString("X2") + c.b.ToString("X2");
    }

    public static string ToHex(this Color c)
    {
      return ColorUtility.ToHtmlStringRGBA( c ) ;
    }

    public static Color DesaturateColor(this Color c, float amount)
    {
      var L = 0.3f * c.r + 0.6f * c.g + 0.1f * c.b;
      float new_r = c.r + amount * (L - c.r);
      float new_g = c.g + amount * (L - c.g);
      float new_b = c.b + amount * (L - c.b);
      return new Color(new_r, new_g, new_b);
    }

    public static HSVColor ToHSV(this Color c){
      float H,S,V;

      Color.RGBToHSV(c, out H, out S, out V);

      return new HSVColor(){ H = H, S = S, V = V, A = c.a };
    }

    public static Color ToColor(this HSVColor hsvc){
      var c = Color.HSVToRGB(hsvc.H, hsvc.S, hsvc.V);
      c.a = hsvc.A;
      return c;
    }

    public static HSVColor ChangeH(this HSVColor color, float value)
    {
      color.H += value;
      return color;
    }
    public static HSVColor ChangeS(this HSVColor color, float value)
    {
      color.S += value;
      return color;
    }
    public static HSVColor ChangeV(this HSVColor color, float value)
    {
      color.V += value;
      return color;
    }
    public static HSVColor ChangeA(this HSVColor color, float value)
    {
      color.A += value;
      return color;
    }

    public static HSVColor SetH(this HSVColor color, float value)
    {
      color.H = value;
      return color;
    }
    public static HSVColor SetS(this HSVColor color, float value)
    {
      color.S = value;
      return color;
    }
    public static HSVColor SetV(this HSVColor color, float value)
    {
      color.V = value;
      return color;
    }
    public static HSVColor SetA(this HSVColor color, float value)
    {
      color.A = value;
      return color;
    }
}

public class HSVColor
{
  float h;
  public float H
  {
    get{ return h; }
    set{
      h = value;
      //try to rotate around the values if they exceed the boundaries, but clamp just to make sure
      if(h > 1f) h -= 1f;
      if(h < 0f) h += 1f;
      h = Mathf.Clamp(h, 0, 1f);
    }
  }

  float s;
  public float S
  {
    get{ return s; }
    set{
      s = Mathf.Clamp(value, 0, 1f);
    }
  }

  float v;
  public float V
  {
    get{ return v; }
    set{
      v = Mathf.Clamp(value, 0, 1f);
    }
  }

  float a;
  public float A
  {
    get{ return a; }
    set{
      a = Mathf.Clamp(value, 0, 1f);
    }
  }
}
