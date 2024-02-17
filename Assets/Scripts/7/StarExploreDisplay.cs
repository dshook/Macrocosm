using Shapes;
using UnityEngine;
using strange.extensions.mediation.impl;

public class StarExploreDisplay : View {

  public Disc sweep;
  public Disc[] rings;

  public float exploreTime;

  public AnimationCurve alphaColorCurve;

  //Degrees per second
  public float sweepSpeed = 90f;

  float timeAccum = 0f;
  bool exploring = false;
  float ringGrowSpeed = 0f;

  protected override void Awake () {
    base.Awake();
  }

  void Update () {
    if(!exploring){ return; }

    timeAccum += Time.unscaledDeltaTime;
    var t = Mathf.Clamp01(timeAccum / exploreTime);

    var alphaColor = Mathf.Clamp01(alphaColorCurve.Evaluate(t));

    sweep.transform.rotation = Quaternion.Euler(0, 0, -sweepSpeed * timeAccum);
    sweep.ColorEnd = sweep.ColorEnd.SetA(alphaColor);

    for(var r = 0; r < rings.Length; r++){
      var ring = rings[r];
      ring.Radius = Mathf.Max(0f,
        ((timeAccum - (r * 0.25f)) * ringGrowSpeed )
      );
      ring.ColorOuter = ring.ColorOuter.SetA(alphaColor);
    }
  }

  public void StartExploring(Star s){
    timeAccum = 0;
    exploring = true;

    //In world units for the system
    ringGrowSpeed = Galaxy.GetSystemViewScale(64f) / exploreTime;

    transform.position = s.transform.position;
    Update();

    sweep.gameObject.SetActive(true);
    foreach(var ring in rings){
      ring.Radius = 0;
      ring.gameObject.SetActive(true);
    }

  }

  public void FinishExploring(bool wasCancelled = false){
    if(!exploring){
      return;
    }
    exploring = false;

    sweep.gameObject.SetActive(false);
    foreach(var ring in rings){
      ring.gameObject.SetActive(false);
    }
  }
}
