using UnityEngine;
using UnityEngine.UI;
using strange.extensions.mediation.impl;
using Unity.VectorGraphics;

public class GalaxyRouteButton : View {
  [Inject] SelectGalaxyResourceSignal selectResource {get; set;}
  [Inject] GalaxyResourceSelectedSignal resourceSelected {get; set;}
  [Inject] GalaxyRouteResourceAssignedSignal resouceAssigned {get; set;}
  [Inject] GalaxyResourceSelectCancelSignal resourceSelectCancel {get; set;}

  [Inject] ResourceLoaderService loader {get; set;}
  [Inject] StageSevenDataModel stageSevenData {get; set;}
  [Inject] SelectGalaxyRouteSignal selectGalaxyRoute {get; set;}

  public uint routeId;
  bool iconNeedsUpdate = true;

  public Button routeSelectButton;
  public Button resourceSelectButton;

  public SVGImage routeSelectDisplay;
  public SVGImage resourceSelectDisplay;

  public Sprite emptyResourceSprite;

  protected override void Awake () {
    base.Awake();

    iconNeedsUpdate = true;

    resourceSelectButton.onClick.AddListener(ClickResourceButton);
    routeSelectButton.onClick.AddListener(ClickRouteSelect);

    resourceSelectCancel.AddListener(() => resourceSelected.RemoveListener(OnResourceSelected));
  }

  void Update(){
    if(iconNeedsUpdate){
      if(loader != null && stageSevenData.routeResources.ContainsKey(routeId)){
        resourceSelectDisplay.sprite = loader.Load<Sprite>(GalaxyResource.resourceIconPaths[stageSevenData.routeResources[routeId]]);
        iconNeedsUpdate = false;
      }else{
        resourceSelectDisplay.sprite = emptyResourceSprite;
      }
    }
  }

  void ClickResourceButton(){
    resourceSelected.AddListener(OnResourceSelected);
    selectResource.Dispatch((t) => GalaxyResource.canExportResource(t), true);
  }

  void OnResourceSelected(GameResourceType? type){
    if(type.HasValue){
      stageSevenData.routeResources[routeId] = type.Value;
    }else{
      stageSevenData.routeResources.Remove(routeId);
    }
    iconNeedsUpdate = true;
    resourceSelected.RemoveListener(OnResourceSelected);
    resouceAssigned.Dispatch(routeId);
    selectGalaxyRoute.Dispatch(routeId);
  }

  void ClickRouteSelect(){
    selectGalaxyRoute.Dispatch(routeId);
  }
}