using UnityEngine;
using strange.extensions.mediation.impl;
using System.Collections.Generic;

public class HexPanelManager : View {

  public GameObject cityPanel;
  public GameObject scoutPanel;
  public GameObject mapPanel;
  public GameObject buildingPanel;
  public GameObject techPanel;

  public Dictionary<HexPanel, GameObject> panels;

  public CameraPanner cameraPanner;
  public GameObject gameWorldView;
  [Inject] TimeService time { get; set; }

  protected override void Awake() {
    base.Awake();

    panels = new Dictionary<HexPanel, GameObject>() {
      {HexPanel.Map, mapPanel},
      {HexPanel.City, cityPanel},
      {HexPanel.Scout, scoutPanel},
      {HexPanel.Building, buildingPanel},
      {HexPanel.Tech, techPanel},
    };

  }

  public void SwitchTo(HexPanel newPanel){
    foreach(var kv in panels){
      if(kv.Key == newPanel){
        kv.Value.SetActive(true);
      }else{
        kv.Value.SetActive(false);
      }
    }

    //other logic depending what we're switching to
    if(newPanel == HexPanel.Map){
      time.Resume();
    }else{
      if(!time.Paused){
        time.Pause();
      }
    }

    if(newPanel == HexPanel.Building || newPanel == HexPanel.Tech){
      cameraPanner.Disable();
      gameWorldView.SetActive(false);
    }else{
      cameraPanner.Enable();
      gameWorldView.SetActive(true);
    }
  }

}

public enum HexPanel{
  Map,
  City,
  Scout,
  Building,
  Tech
}
