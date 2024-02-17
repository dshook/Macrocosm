using UnityEngine;
using UnityEngine.UI;
using strange.extensions.mediation.impl;
using System;
using MoreMountains.NiceVibrations;

public class GalaxyResourceSelect : View {
  [Inject] SelectGalaxyResourceSignal selectResource {get; set;}

  //Only will get dispatched if the resource changes, not on cancel
  [Inject] GalaxyResourceSelectedSignal resourceSelected {get; set;}

  //This gets fired on cancel
  [Inject] GalaxyResourceSelectCancelSignal resourceSelectCancel {get; set;}

  public GameObject resourceSelection;
  public Transform content;
  public GameObject resourceSelectionPrefab;
  public GameObject clearResourceSelectionPrefab;
  public ShinyButton cancelButton;

  protected override void Awake () {
    base.Awake();

    selectResource.AddListener(OnSelectResource);
    cancelButton.onClick.AddListener(OnCancel);
  }

  void OnSelectResource(Func<GameResourceType, bool> resourceFilter, bool clearable){
    //set up all possible resources to select
    CreateResourceDisplays(resourceFilter, clearable);

    resourceSelection.SetActive(true);
  }

  void CreateResourceDisplays(Func<GameResourceType, bool> resourceFilter, bool clearable){

    if(clearable){
      var clearDisplay = GameObject.Instantiate(clearResourceSelectionPrefab, content);
      var clearButton = clearDisplay.GetComponent<Button>();
      clearButton.onClick.AddListener(() => SelectResource(null));
    }

    foreach(var resource in GalaxyResource.GalaxyResourceTypes){
      if(resourceFilter != null){
        if(!resourceFilter(resource)){
          continue;
        }
      }

      var newResourceDisplay = GameObject.Instantiate(resourceSelectionPrefab);
      newResourceDisplay.transform.SetParent(content, false);

      var resourceDisplay = newResourceDisplay.GetComponent<GalaxyResourceDisplay>();
      resourceDisplay.resource = new GalaxyResource(){ type = resource };

      var closureResource = resource;
      var resourceButton = newResourceDisplay.GetComponent<Button>();
      resourceButton.onClick.AddListener(() => SelectResource(closureResource));
    }
  }

  void SelectResource(GameResourceType? type){
    resourceSelection.SetActive(false);
    //Always clear out the resource list for now since the filter could be different
    content.DestroyChildren();

    MMVibrationManager.Haptic(HapticTypes.LightImpact);
    resourceSelected.Dispatch(type);
  }

  void OnCancel(){
    resourceSelection.SetActive(false);
    content.DestroyChildren();
    resourceSelectCancel.Dispatch();
  }

}