using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

[System.Serializable]
public class ScoreService
{
  public BigInteger score { get; set; }
  public BigInteger decimalScore { get; set; }

  public string Interpolate(BigInteger fromScore, BigInteger fromDecimalScore, BigInteger toScore, BigInteger toDecimalScore, float interpolation = 1f){
    interpolation = Mathf.Clamp01(interpolation);

    var interpedScore = toScore;
    var interpedDecimal = toDecimalScore;
    if(fromScore != toScore){
      interpedScore = (BigInteger)((double)(toScore - fromScore) * interpolation) + fromScore;
    }
    if(fromDecimalScore != toDecimalScore){
      interpedDecimal = (BigInteger)((double)(toDecimalScore - fromDecimalScore) * interpolation) + fromDecimalScore;
    }
    return Format(interpedScore, interpedDecimal);
  }

  public string Format(BigInteger formatScore, BigInteger formatDecimalScore){
    if(formatScore == 0){
      return string.Format("0.{0:000000000000000000}", formatDecimalScore);
    }
    var scoreMagnitude = (int)BigInteger.Log10(formatScore) + 1;
    var paddedDecimal = formatDecimalScore.ToString().PadLeft( Mathf.Clamp(18 - scoreMagnitude, 0, 18), '0');
    return string.Format("{0:0,0}.{1:0}", formatScore, paddedDecimal);
  }

}

