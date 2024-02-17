using UnityEngine;
using strange.extensions.mediation.impl;
using System.Linq;
using System.Collections.Generic;

public class StarMarketDisplay : View {
  [Inject] SelectGalaxyResourceSignal selectResource {get; set;}
  [Inject] GalaxyStarImportExportChangedSignal importExportChangedSignal {get; set;}
  [Inject] GalaxyResourceSelectedSignal resourceSelected {get; set;}
  [Inject] GalaxyResourceSelectCancelSignal resourceSelectCancel {get; set;}

  [Inject] TimeService time { get; set; }

  public GalaxyPanelManager panelManager;
  public ShinyButton cancelButton;
  public ShinyButton addResourceButton;

  public GalaxyResourceStack galaxyResourceStack;

  List<GalaxyResource> exportableResources = new List<GalaxyResource>();

  protected override void Awake () {
    base.Awake();

    cancelButton.onClick.AddListener(OnCancel);
    addResourceButton.onClick.AddListener(ClickAddResource);

    resourceSelectCancel.AddListener(() => resourceSelected.RemoveListener(OnResourceSelectedToAdd));
  }

  StarSettlementData selectedStarData = null;

  public void Open(StarSettlementData data){
    selectedStarData = data;

    panelManager.SwitchTo(GalaxyPanel.Market);

    time.Pause();
    Update();
  }

  void Update(){
    exportableResources.Clear();
    foreach(var ssr in selectedStarData.resources){
      if(GalaxyResource.canExportResource(ssr.Key)){
        exportableResources.Add(ssr.Value);
      }
    }
    galaxyResourceStack.UpdateResourceStack(ref exportableResources, null);
    galaxyResourceStack.OnImportExportChanged += OnResourceImportExportChanged;
  }

  void OnResourceImportExportChanged(){
    if(selectedStarData != null){
      importExportChangedSignal.Dispatch(selectedStarData.starId);
    }
  }

  void OnCancel(){
    selectedStarData = null;
    galaxyResourceStack.TearDownResourceStack();
    panelManager.SwitchTo(GalaxyPanel.Star);
    time.Resume();
  }

  void ClickAddResource(){

    // panelManager.SwitchTo(GalaxyPanel.Star);
    resourceSelected.AddListener(OnResourceSelectedToAdd);
    selectResource.Dispatch((t) => GalaxyResource.canExportResource(t), false);
  }

  void OnResourceSelectedToAdd(GameResourceType? type){
    // marketDisplayGO.SetActive(true);
    // panelManager.SwitchTo(GalaxyPanel.Market);
    resourceSelected.RemoveListener(OnResourceSelectedToAdd);

    if(type == null){
      return;
    }

    if(selectedStarData.resources.ContainsKey(type.Value)){
      return;
    }
    selectedStarData.resources.Add(type.Value, new GalaxyResource(){ type = type.Value, amount = 0, importing = true});
  }


}