using UnityEngine;
using strange.extensions.mediation.impl;

public class LowQualityDeactivator : View
{
  [Inject] SettingsDataModel settings { get; set; }

  public GameObject[] updateList;

  bool state;

  protected override void Awake () {
    base.Awake();

    UpdateAll();
  }

  void Update(){
    if(state != settings.lowQuality){
      UpdateAll();
    }
  }

  void UpdateAll(){
    foreach(var l in updateList){
      l.SetActive(!settings.lowQuality);
    }
    state = settings.lowQuality;
  }

}
