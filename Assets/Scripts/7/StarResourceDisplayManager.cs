using UnityEngine;
using strange.extensions.mediation.impl;
using System.Linq;
using System.Collections.Generic;

public class StarResourceDisplayManager : View {

  [Inject] GalaxyTransitionSignal galaxyTransitionSignal { get; set; }
  [Inject] GalaxyTransitionCompleteSignal galaxyTransitionCompleteSignal { get; set; }

  public GalaxyResourceStack galaxyResourceStack;


  public Star star {get; set;}

  List<GalaxyResource> exportableResources = new List<GalaxyResource>();

  protected override void Awake () {
    base.Awake();

    galaxyTransitionSignal.AddListener(OnTransition);
    galaxyTransitionCompleteSignal.AddListener(OnTransitionComplete);
  }

  void Update () {
    if(star == null){ return; }

    if(star.generatedData.inhabited){
      exportableResources.Clear();
      foreach(var ssr in star.settlementData.resources){
        if(GalaxyResource.canExportResource(ssr.Key)){
          exportableResources.Add(ssr.Value);
        }
      }
      galaxyResourceStack.UpdateResourceStack(ref exportableResources, null);

    }else{
      galaxyResourceStack.TearDownResourceStack();
    }

  }


  public void GotoStar(Star s){
    star = s;

    //Tear down the stack when going to a star as well so we bust the string cache when going between stars directly
    galaxyResourceStack.TearDownResourceStack();

    Update(); //manually call update so we avoid a one frame pop of UI

    galaxyResourceStack.ScrollToBeginning();
  }

  public void GofromStar(){
    galaxyResourceStack.TearDownResourceStack();
    star = null;

  }

  void OnTransition(GalaxyTransitionInfo info){
    if(info.to == GalaxyViewMode.Galaxy){
      GofromStar();
    }
    if(info.to == GalaxyViewMode.System && info.from != GalaxyViewMode.Planet){
      GotoStar(info.transitioner.SelectedStar);
    }
  }

  void OnTransitionComplete(GalaxyTransitionCompleteInfo info){
    // if(info.to == GalaxyViewMode.System){
    //   GotoStar(info.transitioner.SelectedStar);
    // }
  }
}
