using UnityEngine;
using strange.extensions.mediation.impl;
using System.Linq;

public class GalaxyTransitioner : View {
  [Inject] protected StageSevenDataModel stageSevenData {get; set;}
  [Inject] GalaxyTransitionSignal galaxyTransitionSignal {get; set;}
  [Inject] GalaxyTransitionCompleteSignal galaxyTransitionCompleteSignal {get; set;}
  [Inject] GalaxyBgStarsFinishedCreatingSignal galaxyBgStarsFinishedCreatingSignal { get; set; }
  [Inject] CameraService cameraService { get; set; }

  public CameraPanner camPanner;
  public GalaxySpawner galaxySpawner;
  public RectTransform starZoomAnchorPoint;
  public RectTransform cbZoomAnchorPoint;
  public Galaxy galaxy;
  public GalaxyPanelManager panelManager;
  public ColorFader lineFader;
  public ColorFader bgStarFader;

  public static float transitionTime = 0.8f;
  public static float fadeTime = 0.5f;
  public float systemZoomSize = 0.3f;
  public float planetZoomSize = 0.1f;

  Vector3? moveToAnchorWorldPoint;
  Vector3? moveFromAnchorWorldPoint;
  float? zoomFromSize;
  float? zoomToSize;

  float galaxyZoomLevel;
  Camera cam;
  Vector2 starZoomAnchorScreenPos;
  Vector2 cbZoomAnchorScreenPos;
  float timeAccum = 0f;

  Star selectedStar;
  CelestialBody selectedCb;
  public Star SelectedStar { get{ return selectedStar; }}
  public CelestialBody SelectedCb { get{ return selectedCb; }}
  Star prevSelectedStar;
  public Star PrevSelectedStar { get{ return prevSelectedStar; }}

  //Move to positions
  Vector2 starMoveToAnchorScreenPct = new Vector2(0.5f, 0.15f);
  Vector2 cbMoveToAnchorScreenPct = new Vector2(0.5f, 0.2f);

  float pctComplete = 1f;
  public bool inProgress {
    get{ return pctComplete < 1f; }
  }

  protected override void Awake () {
    base.Awake();

    galaxyTransitionSignal.AddListener(OnTransitionStarted);
    galaxyTransitionCompleteSignal.AddListener(OnTransitionComplete);
    galaxyBgStarsFinishedCreatingSignal.AddListener(OnBgStarsFinishedCreating);

    cam = cameraService.Cam;
    galaxyZoomLevel = cam.orthographicSize;

  }

  public void Cleanup(){
    //Finish any in progress transitions
    timeAccum = transitionTime;
    Update();
  }

  //For re-initialization
  public void GoToStarAndCb(uint? selectedStarId, uint? selectedCbId, Vector3 cameraPosition){
    bgStarFader.UpdateComponents();

    Star goToStar = null;
    if(selectedStarId.HasValue){
      goToStar = galaxy.stars[selectedStarId.Value];
    }

    if(goToStar != null){
      bool goDirectToStar = false;
      bool goDirectToCb = false;

      if(goToStar != selectedStar){
        cam.transform.localPosition = cam.transform.localPosition.Set(goToStar.transform.position);
        //Reset the view mode to galaxy so we transition in correctly
        stageSevenData.viewMode = GalaxyViewMode.Galaxy;
        TransitionToSystem(goToStar, true);
      }else if(zoomToSize == null && cameraPosition != Vector3.zero){
        goDirectToStar = true;
      }

      CelestialBody goToCb = null;
      if(selectedCbId.HasValue){
        goToCb = SelectedStar.celestialBodies.FirstOrDefault(p => p.data.id == selectedCbId.Value);
      }

      if(goToCb != null && goToCb != selectedCb){
        TransitionToCelestialBody(goToCb, true);
      }else if(goToCb != null && zoomToSize == null && cameraPosition != Vector3.zero){
        goDirectToCb = true;
      }

      //hacky, but this handles coming back to a CB or Star after going between stages when
      //all that's needed is to reset the camera position & zoom
      if(goDirectToStar || goDirectToCb){
        if(goDirectToCb){
          cam.transform.localPosition = cameraPosition;
          zoomToSize = zoomFromSize = planetZoomSize;
        }else{
          cam.transform.localPosition = cameraPosition;
          zoomToSize = zoomFromSize = systemZoomSize;
        }
      }

    }else{
      //Reset the view mode to system so we transition in correctly
      cam.transform.localPosition = cameraPosition;
      stageSevenData.viewMode = GalaxyViewMode.System;
      TransitionToGalaxy(null, true);
    }

    Update(); //Force an update now so the skipped animations can complete their update, some of which is in Update
  }

  void Update () {
    timeAccum += Time.smoothDeltaTime;
    pctComplete = Mathf.Clamp01(timeAccum / transitionTime);
    var easing = Easings.easeOutQuint(0, 1f, pctComplete);

    //Trigger once actions when we're done transitioning.  Hacky to use the zoom to size as a single fire check that gets unset but I'm lazy
    if(pctComplete >= 1f && zoomToSize != null){
      galaxyTransitionCompleteSignal.Dispatch(new GalaxyTransitionCompleteInfo(){
        transitioner = this,
        to = stageSevenData.viewMode,
      });
    }

    if(moveToAnchorWorldPoint.HasValue){

      var dest = moveFromAnchorWorldPoint.Value + (moveToAnchorWorldPoint.Value - moveFromAnchorWorldPoint.Value) * easing;
      cam.transform.position = dest;

      if(pctComplete >= 1f){
        moveFromAnchorWorldPoint = null;
        moveToAnchorWorldPoint = null;
      }
    }

    if(zoomToSize.HasValue){
      cam.orthographicSize = zoomFromSize.Value + (zoomToSize.Value - zoomFromSize.Value) * easing;

      if(pctComplete >= 1){
        zoomFromSize = null;
        zoomToSize = null;
      }
    }
  }

  public void TransitionToCelestialBody(CelestialBody cb, bool skipAnimation = false){
    if(inProgress){
      return; // Prevent double transitions
    }
    selectedCb = cb;
    moveFromAnchorWorldPoint = cam.transform.position;
    moveToAnchorWorldPoint = CalculateMoveToPoint(planetZoomSize, cb.transform.position, true);

    zoomFromSize = cam.orthographicSize;
    zoomToSize = planetZoomSize;

    timeAccum = skipAnimation ? transitionTime : 0f;
    galaxyTransitionSignal.Dispatch(new GalaxyTransitionInfo(){
      transitioner = this,
      from = stageSevenData.viewMode,
      to = GalaxyViewMode.Planet,
      skipAnimation = skipAnimation
    });
    stageSevenData.viewMode = GalaxyViewMode.Planet;

    if(skipAnimation){
      //Call now if we're skipping animation to make sure it's called when transitioning stages
      Update();
    }
  }

  public void TransitionToSystem(uint starId, bool skipAnimation = false){
    TransitionToSystem(galaxy.stars[starId], skipAnimation);
  }

  public void TransitionToSystem(Star star, bool skipAnimation = false){
    if(inProgress){
      return; // Prevent double transitions
    }
    camPanner.Disable();

    //Save the galaxy zoom level if we're coming from it
    if(stageSevenData.viewMode == GalaxyViewMode.Galaxy ){
      TransitionFromGalaxy(star, skipAnimation);
    }
    if(stageSevenData.viewMode == GalaxyViewMode.Planet){
      panelManager.SwitchTo(GalaxyPanel.None);
    }

    if(selectedStar != null && selectedStar != star){
      //Clean up planets when going between systems with the arrows
      galaxySpawner.CleanUpSystemPlanets(selectedStar);
    }

    selectedCb = null;
    selectedStar = star;

    moveFromAnchorWorldPoint = cam.transform.position;
    moveToAnchorWorldPoint = CalculateMoveToPoint(systemZoomSize, star.transform.position, false);

    zoomFromSize = cam.orthographicSize;
    zoomToSize = systemZoomSize;

    //Have to set the star we're going to active here so that the system planets created below will get awoken/injected properly
    //Have to do it in this order so the CB's will get the transition signal
    star.gameObject.SetActive(true);

    //Handle going from one system to another if using the previous and next buttons
    if(stageSevenData.viewMode == GalaxyViewMode.System){
      galaxySpawner.CreateSystemPlanets(galaxy, star);
    }

    galaxyTransitionSignal.Dispatch(new GalaxyTransitionInfo(){
      transitioner = this,
      from = stageSevenData.viewMode,
      to = GalaxyViewMode.System,
      skipAnimation = skipAnimation
    });

    timeAccum = skipAnimation ? transitionTime : 0f;
    stageSevenData.viewMode = GalaxyViewMode.System;

    if(skipAnimation){
      //Call now if we're skipping animation to make sure it's called when transitioning stages
      Update();
    }
  }

  public void TransitionToGalaxy(Star star, bool skipAnimation = false){
    // Specifically allow transitioning to the galaxy when another transition is in progress
    // if(inProgress){ return; }
    if(selectedCb != null){
      TransitionToSystem(star, skipAnimation);
    }
    prevSelectedStar = star;
    selectedStar = null;

    zoomFromSize = cam.orthographicSize;
    zoomToSize = Mathf.Clamp(galaxyZoomLevel, camPanner.minZoomSize, camPanner.maxZoomSize);

    galaxyTransitionSignal.Dispatch(new GalaxyTransitionInfo(){
      transitioner = this,
      from = stageSevenData.viewMode,
      to = GalaxyViewMode.Galaxy,
      skipAnimation = skipAnimation
    });

    FadeInObj(bgStarFader, skipAnimation);

    //and the lines
    lineFader.FadeIn(fadeTime, skipAnimation, true);

    timeAccum = 0f;
    stageSevenData.viewMode = GalaxyViewMode.Galaxy;

    if(skipAnimation){
      //Call now if we're skipping animation to make sure it's called when transitioning stages
      Update();
    }
  }

  void OnTransitionStarted(GalaxyTransitionInfo info){
    //Switch to no panel when starting the transition so you can't click buttons you shouldn't like the route button
    panelManager.SwitchTo(GalaxyPanel.None);
  }

  void OnTransitionComplete(GalaxyTransitionCompleteInfo info){
    if(info.to == GalaxyViewMode.Planet){
      panelManager.SwitchTo(GalaxyPanel.Celestial);
    }
    if(info.to == GalaxyViewMode.System){
      panelManager.SwitchTo(GalaxyPanel.Star);
    }
    if(info.to == GalaxyViewMode.Galaxy){
      galaxySpawner.CleanUpSystemPlanets(prevSelectedStar);
      panelManager.SwitchTo(GalaxyPanel.Galaxy);
      camPanner.Enable();
    }
  }

  void TransitionFromGalaxy(Star toStar, bool skipAnimation){
    galaxyZoomLevel = cam.orthographicSize;
    galaxySpawner.CreateSystemPlanets(galaxy, toStar);

    FadeOutObj(bgStarFader, skipAnimation);

    //and the lines
    lineFader.FadeOut(fadeTime, skipAnimation, true);
  }


  Vector3 CalculateMoveToPoint(float zoomInSize, Vector3 zoomToPosition, bool isCb){
    // starZoomAnchorScreenPos = RectTransformUtility.WorldToScreenPoint(cam, starZoomAnchorPoint.transform.position);
    // cbZoomAnchorScreenPos = RectTransformUtility.WorldToScreenPoint(cam, cbZoomAnchorPoint.transform.position);

    //Trying to get the screen positions of the rect transform anchors doesn't seem to be reliable at all with the method above
    //Might be a problem with the canvas scalar, or just as the camera is moving while transitioning stages
    //This seems to work well though
    var anchorScreenPos = (isCb ? cbMoveToAnchorScreenPct : starMoveToAnchorScreenPct) * new Vector2(cam.pixelWidth, cam.pixelHeight);

    //pop the camera in to the final zoom so the calculation here will be correct
    var currentZoom = cam.orthographicSize;
    cam.orthographicSize = zoomInSize;

    var point = starZoomAnchorPoint;
    if(isCb){
      point = cbZoomAnchorPoint;
    }
    RectTransformUtility.ScreenPointToWorldPointInRectangle(point, anchorScreenPos, cam, out Vector3 curWorldPosOfAnchor);

    var diff = (curWorldPosOfAnchor - cam.transform.position);
    var dest = (zoomToPosition - diff).SetZ(cam.transform.localPosition.z);

    //and then reset it so it can be smootly lerp'ed in
    cam.orthographicSize = currentZoom;

    return dest;
  }

  void FadeInObj(ColorFader colorFader, bool skipAnimation = false){
    colorFader.FadeIn(skipAnimation ? 0f : fadeTime);
  }

  void FadeOutObj(ColorFader colorFader, bool skipAnimation = false){
    colorFader.FadeOut(skipAnimation ? 0f : fadeTime);
  }

  //Update the fader to get all the shape components after all the bg stars have finished loading
  void OnBgStarsFinishedCreating(){
    bgStarFader.UpdateComponents();
  }

}

public struct GalaxyTransitionInfo{
  public GalaxyTransitioner transitioner;
  public GalaxyViewMode to;
  public GalaxyViewMode from;
  public bool skipAnimation;
}

public struct GalaxyTransitionCompleteInfo{
  public GalaxyTransitioner transitioner;
  public GalaxyViewMode to;
}