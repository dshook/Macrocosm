
using UnityEngine;

public static class NumberExtensions
{
  //Makes sure to return a formatted string that is 5 characters max, e.g. 12.3K
  public static string ToShortFormat(this int num){

    //negative
    if (num <= -1000000000)
      return ((float)num / 1000000000).ToString("0.#B");

    if (num <= -10000000)
      return (num / 1000000).ToString("#,0M");

    if (num <= -1000000)
      return ((float)num / 1000000).ToString("0.#") + "M";

    if (num <= -10000)
      return (num / 1000).ToString("#,0K");

    if (num <= -1000)
      return ((float)num / 1000).ToString("0.#") + "K";

    if(num < 0)
      return num.ToString("+0;-#");

    //positive
    if (num >= 1000000000)
      return ((float)num / 1000000000).ToString("0.##B");

    if (num >= 100000000)
      return (num / 1000000).ToString("#,0M");

    if (num >= 10000000)
      return ((float)num / 1000000).ToString("0.#") + "M";

    if (num >= 1000000)
      return ((float)num / 1000000).ToString("0.##") + "M";

    if (num >= 100000)
      return (num / 1000).ToString("#,0K");

    if (num >= 10000)
      return ((float)num / 1000).ToString("0.#") + "K";

    return num.ToString("#,0");

  }

  //Probably simpler ways of doing this but /shrug
  public static string ToShortFormat(this float num){

    //negative
    if (num <= -1000000000)
      return (num / 1000000000).ToString("0.#B");

    if (num <= -10000000)
      return (num / 1000000).ToString("#,0M");

    if (num <= -1000000)
      return (num / 1000000).ToString("0.#") + "M";

    if (num <= -10000)
      return (num / 1000).ToString("#,0K");

    if (num <= -1000)
      return (num / 1000).ToString("0.#") + "K";

    if(num < 0)
      return num.ToString("F1");

    //positive
    if (num >= 1000000000)
      return (num / 1000000000).ToString("0.##B");

    if (num >= 100000000)
      return (num / 1000000).ToString("#,0M");

    if (num >= 10000000)
      return (num / 1000000).ToString("0.#") + "M";

    if (num >= 1000000)
      return (num / 1000000).ToString("0.##") + "M";

    if (num >= 100000)
      return (num / 1000).ToString("#,0K");

    if (num >= 10000)
      return (num / 1000).ToString("0.#") + "K";

    if (num >= 1000)
      return num.ToString("0,0");

    return num.ToString("F1");

  }

  // 12345 -> 12,345
  public static string ToThousandsFormat(this int num){
    return string.Format("{0:#,0}", num);
  }


  //Makes sure to return a formatted string that is 3 characters max (4 for negative numbers), e.g. "12K" Rounding is unavoidable here
  public static string ToSuperShortFormat(this int num){

    //negative
    if (num <= -1000000000)
      return ((float)num / 1000000000).ToString("0B");

    if (num <= -100000000)
      return ((float)num / 1000000000).ToString(".0B");

    if (num <= -1000000)
      return ((float)num / 1000000).ToString("0") + "M";

    if (num <= -100000)
      return ((float)num / 1000000).ToString(".0M");

    if (num <= -10000)
      return ((float)num / 1000).ToString("0K");

    if (num <= -1000)
      return ((float)num / 1000).ToString("0") + "K";

    if(num < 0)
      return num.ToString("+0;-0");

    //positive
    if (num >= 1000000000)
      return ((float)num / 1000000000).ToString("0B");

    if (num >= 100000000)
      return ((float)num / 1000000000).ToString(".0B");

    if (num >= 1000000)
      return ((float)num / 1000000).ToString("0") + "M";

    if (num >= 100000)
      return ((float)num / 1000000).ToString(".0") + "M";

    if (num >= 10000)
      return ((float)num / 1000).ToString("0K");

    if (num >= 1000)
      return ((float)num / 1000).ToString("0") + "K";

    return num.ToString("0");

  }

  public static string Format(this int number, NumberFormatLength format){
    switch(format){
      case NumberFormatLength.Short:
        return number.ToShortFormat();
      case NumberFormatLength.SuperShort:
        return number.ToSuperShortFormat();
    }

    //Normal
    return number.ToString("#,0;-#,0");
  }

  public static string OrdinalFormat(this uint num)
  {
    if( num <= 0 ) return num.ToString();

    switch(num % 100)
    {
        case 11:
        case 12:
        case 13:
            return num + "th";
    }

    switch(num % 10)
    {
        case 1:
            return num + "st";
        case 2:
            return num + "nd";
        case 3:
            return num + "rd";
        default:
            return num + "th";
    }
  }

  public static int GenerateNewSeed(){
    var newSeed = Random.Range(0, int.MaxValue);
    newSeed ^= (int)System.DateTime.Now.Ticks;
    newSeed ^= (int)Time.unscaledTime;
    newSeed &= int.MaxValue;

    return newSeed;
  }
}

public enum NumberFormatLength {
  Normal,
  Short,
  SuperShort
}
