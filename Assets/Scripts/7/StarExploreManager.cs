using TMPro;
using UnityEngine;
using strange.extensions.mediation.impl;
using System.Collections.Generic;
using System.Linq;

public class StarExploreManager : View {
  [Inject] StageSevenDataModel stageSevenData {get; set;}
  [Inject] AudioService audioService { get; set; }

  public StarManager starManager;
  public GameObject exploreStarOverlay;
  public TMP_Text exploreStarText;

  public TeletypeText starMainDescriptionTeletype;
  public TeletypeText starDescriptionTeletype;
  public StarExploreDisplay starExploreDisplay;

  public AudioClip celestialRevealClip;
  public AudioClip scanningClip;

  Star star {get; set;}

  float exploreTimePerCb = 0.5f;
  float timeAccum = 0f;
  float nextActionTime = 0.0f;
  bool exploring = false;
  int cbIndex = 0;
  List<CelestialBody> orderedCbs = new List<CelestialBody>();

  protected override void Awake () {
    base.Awake();
  }

  void Update () {
    if(!exploring){ return; }

    timeAccum += Time.unscaledDeltaTime;
    nextActionTime += Time.unscaledDeltaTime;

    if(cbIndex >= orderedCbs.Count){
      FinishExploring();
    }

    if( nextActionTime > exploreTimePerCb){
      exploreStarText.text += ".";
      nextActionTime = 0f;

      var curCb = orderedCbs[cbIndex];
      ExploreCb(curCb);

      cbIndex++;

      // Logger.LogWithFrame($"Revealing Next: {Time.realtimeSinceStartup} index {cbIndex}" );
    }

  }

  void ExploreCb(CelestialBody cb){
    //modify the clip pitch by how habitable the world is.
    float pitchDelta = 0.01f;
    float pitch = 1 + ((int)cb.data.habitability - (int)CelestialBodyHabitability.Moderate) * pitchDelta;

    audioService.PlaySfx(celestialRevealClip, pitch);

    cb.UpdateDisplay(true);
    if(cb.childCelestialBodies != null){
      foreach(var childCb in cb.childCelestialBodies){
        childCb.UpdateDisplay(true);
      }
    }
  }

  public void StartExploring(Star s){
    star = s;
    timeAccum = 0;
    nextActionTime = 0;
    cbIndex = 0;
    exploring = true;
    exploreStarText.text = "Scanning";
    exploreStarOverlay.SetActive(true);

    starDescriptionTeletype.Play();
    starMainDescriptionTeletype.Play();

    orderedCbs = star.celestialBodies.OrderBy(cb => cb.data.parentIndex).ToList();

    var totalExploreTime = scanningClip.length;
    exploreTimePerCb = totalExploreTime / orderedCbs.Count;

    starExploreDisplay.exploreTime = totalExploreTime;
    starExploreDisplay.StartExploring(s);

    audioService.PlaySfx(scanningClip);
  }

  public void FinishExploring(bool wasCancelled = false){
    if(!exploring){
      return;
    }
    exploring = false;
    exploreStarOverlay.SetActive(false);
    star.data = new StarData(){ exploreStatus = StarExploreStatus.Seen };

    if(wasCancelled){
      foreach(var cb in star.celestialBodies){
        ExploreCb(cb);
      }
      starDescriptionTeletype.Stop();
      starMainDescriptionTeletype.Stop();
    }

    star = null;
    starExploreDisplay.FinishExploring(wasCancelled);
    starManager.OnFinishedExploring(wasCancelled);
  }
}
