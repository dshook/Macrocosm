using UnityEngine;
using strange.extensions.mediation.impl;
using System.Collections.Generic;

public class GalaxyPanelManager : View {

  public GameObject galaxyPanel;
  public GameObject buildingPanel;
  public GameObject starPanel;
  public GameObject celestialPanel;
  public GameObject factoryPanel;
  public GameObject marketPanel;

  public Dictionary<GalaxyPanel, GameObject> panels;

  public GameObject gameWorldView;

  protected override void Awake() {
    base.Awake();

    panels = new Dictionary<GalaxyPanel, GameObject>() {
      {GalaxyPanel.Galaxy, galaxyPanel},
      {GalaxyPanel.Building, buildingPanel},
      {GalaxyPanel.Star, starPanel},
      {GalaxyPanel.Celestial, celestialPanel},
      {GalaxyPanel.Factory, factoryPanel},
      {GalaxyPanel.Market, marketPanel},
    };

  }

  public void SwitchTo(GalaxyPanel newPanel){
    foreach(var kv in panels){
      if(kv.Key == newPanel){
        kv.Value.SetActive(true);
      }else{
        kv.Value.SetActive(false);
      }
    }

    if(newPanel == GalaxyPanel.Market ||
       newPanel == GalaxyPanel.Factory ||
       newPanel == GalaxyPanel.Building
    ){
      gameWorldView.SetActive(false);
    }else{
      gameWorldView.SetActive(true);
    }
  }

}

public enum GalaxyPanel{
  None,
  Galaxy,
  Building,
  Star,
  Celestial,
  Factory,
  Market
}
