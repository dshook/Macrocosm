using TMPro;
using UnityEngine;
using System.Numerics;
using strange.extensions.mediation.impl;

public class ScoreSystem : View {

  [Inject] ScoreService score {get; set;}
  public GameObject scoreGO;
  public float interpTime = 1f;

  TextMeshProUGUI text;

  protected override void Awake () {
    base.Awake();

    text = scoreGO.GetComponent<TextMeshProUGUI>();
  }

  BigInteger fromScore        = new BigInteger(0);
  BigInteger fromDecimalScore = new BigInteger(0);
  BigInteger toScore          = new BigInteger(0);
  BigInteger toDecimalScore   = new BigInteger(0);

  float timeAccum = 100f;


  BigInteger prevFromScore        = new BigInteger(0);
  BigInteger prevFromDecimalScore = new BigInteger(0);
  BigInteger prevToScore          = new BigInteger(0);
  BigInteger prevToDecimalScore   = new BigInteger(0);
  float      prevInterp = 0f;

  void Update () {
    timeAccum += Time.deltaTime;
    var interp = Mathf.Clamp01(timeAccum / interpTime);

    if(interp == 1f){
      //when interp finishes, advance the from numbers up to where they were going
      fromScore = toScore;
      fromDecimalScore = toDecimalScore;

      //if the to scores aren't up to date when an interp finishes, then start fresh with them
      if(score.score != toScore || score.decimalScore != toDecimalScore){
        timeAccum = 0f;
        interp = 0f;

        toScore = score.score;
        toDecimalScore = score.decimalScore;
        // Debug.Log("Interping from " +  fromDecimalScore + " to " + toDecimalScore);
      }
    }
    //Skip interpolation on small score changes
    if(toScore != fromScore && (toScore - fromScore) < 10){
      fromScore = toScore;
    }
    if(toDecimalScore != fromDecimalScore && (toDecimalScore - fromDecimalScore) < 10){
      fromDecimalScore = toDecimalScore;
    }

    //only create new strings when things have changed to save GC
    if(
      fromScore != prevFromScore ||
      fromDecimalScore != prevFromDecimalScore ||
      toScore != prevToScore ||
      toDecimalScore != prevToDecimalScore ||
      interp != prevInterp
    ){
      text.text = score.Interpolate(fromScore, fromDecimalScore, toScore, toDecimalScore, interp);
    }

    prevFromScore = fromScore;
    prevFromDecimalScore = fromDecimalScore;
    prevToScore = toScore;
    prevToDecimalScore = toDecimalScore;
    prevInterp = interp;
  }

}
