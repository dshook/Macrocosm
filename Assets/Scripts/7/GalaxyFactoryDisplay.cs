using UnityEngine;
using strange.extensions.mediation.impl;

public class GalaxyFactoryDisplay : View {
  [Inject] StageSevenDataModel stageSevenData {get; set;}
  [Inject] GalaxyTransitionSignal galaxyTransitionSignal {get; set;}
  [Inject] InputService input {get; set;}
  [Inject] GalaxyBuildingFinishedSignal buildingFinishedSignal {get; set;}

  public GameObject display;
  public ColorFader colorFader;

  Star star;
  Vector3 factoryOffset = new Vector3(0.10f, 0.13f, 0f);

  protected override void Awake () {
    base.Awake();

    galaxyTransitionSignal.AddListener(OnTransition);
    buildingFinishedSignal.AddListener(OnBuildingFinishedSignal);
  }

  protected override void OnDestroy(){
    galaxyTransitionSignal.RemoveListener(OnTransition);
    buildingFinishedSignal.RemoveListener(OnBuildingFinishedSignal);
  }

  void Update () {
  }

  void OnTransition(GalaxyTransitionInfo transitionInfo){
    if(transitionInfo.from == GalaxyViewMode.System){
      if(display.activeInHierarchy){
        colorFader.toggleGameObjectActive = true;
        colorFader.FadeOut(GalaxyTransitioner.transitionTime, transitionInfo.skipAnimation);
      }
    }

    if(transitionInfo.to == GalaxyViewMode.System){
      star = transitionInfo.transitioner.SelectedStar;

      if(star != null && star.generatedData.inhabited){
        //find right position for it based on the stars position
        transform.position = star.transform.position + factoryOffset;

        if(star.settlementData.HasBuilding(GalaxyBuildingId.Factory1)){
          ShowFactory(transitionInfo.skipAnimation);
        }
      }else{
        HideFactory();
      }
    }
  }

  void ShowFactory(bool skipAnimation){
    display.SetActive(true);
    colorFader.toggleGameObjectActive = false;
    colorFader.FadeOut(0, true);
    colorFader.FadeIn(GalaxyTransitioner.transitionTime, skipAnimation);
  }

  void HideFactory(){
    display.SetActive(false);
  }

  void OnBuildingFinishedSignal(GalaxyBuildingData data, uint starId, uint? celestialId){
    //Show the factory if we're looking at the system when it finished
    if(data.buildingId == GalaxyBuildingId.Factory1 && star != null && star.generatedData.id == starId){
      ShowFactory(false);
    }
  }
}
