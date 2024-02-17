using strange.extensions.mediation.impl;
using UnityEngine;
using UnityPackages.UI;

public class GalaxyFactoryManager : View {
  [Inject] TimeService time { get; set; }
  [Inject] TutorialSystem tutorialSystem {get; set;}
  [Inject] StageSevenDataModel stageSevenData {get; set;}

  public GalaxyPanelManager panelManager;
  public ShinyButton doneButton;

  public Transform factoryContentArea;
  public GameObject factoryCombineRow;
  public GameObject factoryRowSelectionLabel;
  public UIFlexbox contentFlexbox;

  public GalaxyFactoryResourceEfficiencyDisplay[] resourceEfficiencyDisplays;


  StarSettlementData starSettlementData;

  protected override void Awake () {
    base.Awake();

    doneButton.onClick.AddListener(ClickDoneButton);
    factoryContentArea.DestroyChildren();
  }

  void ClickDoneButton(){
    panelManager.SwitchTo(GalaxyPanel.Star);
    time.Resume();
    starSettlementData = null;
    factoryContentArea.DestroyChildren();
  }

  public void OpenFactory(StarSettlementData settlement){
    panelManager.SwitchTo(GalaxyPanel.Factory);
    time.Pause();
    starSettlementData = settlement;
    CreateFactoryRows(settlement.GetFactory());
    SetUpResourceEfficiencies();

    tutorialSystem.ShowTutorial(707);

    if(stageSevenData.year > 500){
      tutorialSystem.ShowPopoutTutorial("7-factory-production", "If you don't need more of a resource, sometimes it's better to save the raw materials");
    }
  }

  void CreateFactoryRows(GalaxyBuilding factory){
    //create header
    GameObject.Instantiate(factoryRowSelectionLabel, Vector3.zero, Quaternion.identity, factoryContentArea);

    var rows = factory.factoryRows;
    for(var i = 0; i < rows; i++){
      if(i >= starSettlementData.factoryData.Count){
        starSettlementData.factoryData.Add(new StarFactoryData());
      }
      var data = starSettlementData.factoryData[i];

      var newRow = GameObject.Instantiate(factoryCombineRow, Vector3.zero, Quaternion.identity, factoryContentArea);
      var factoryRow = newRow.GetComponent<GalaxyFactoryRow>();
      factoryRow.starSettlementData = starSettlementData;
      factoryRow.factory = factory;
      factoryRow.factoryData = data;
      factoryRow.Init();

    }
    contentFlexbox.Draw();
  }

  void SetUpResourceEfficiencies(){
    if(resourceEfficiencyDisplays.Length != 6){
      Debug.LogWarning("Set up resource efficiency displays");
      return;
    }

    resourceEfficiencyDisplays[0].iconResourceType = GameResourceType.Iron;
    resourceEfficiencyDisplays[0].resource = starSettlementData.resources.TryGet(GameResourceType.IronSilicon);

    resourceEfficiencyDisplays[1].iconResourceType = GameResourceType.Phosphorus;
    resourceEfficiencyDisplays[1].resource = starSettlementData.resources.TryGet(GameResourceType.SiliconPhosphorus);

    resourceEfficiencyDisplays[2].iconResourceType = GameResourceType.Sodium;
    resourceEfficiencyDisplays[2].resource = starSettlementData.resources.TryGet(GameResourceType.SiliconSodium);

    resourceEfficiencyDisplays[3].iconResourceType = GameResourceType.Titanium;
    resourceEfficiencyDisplays[3].resource = starSettlementData.resources.TryGet(GameResourceType.SiliconTitanium);

    resourceEfficiencyDisplays[4].iconResourceType = GameResourceType.Xenon;
    resourceEfficiencyDisplays[4].resource = starSettlementData.resources.TryGet(GameResourceType.SiliconXenon);

    resourceEfficiencyDisplays[5].iconResourceType = GameResourceType.Promethium;
    resourceEfficiencyDisplays[5].resource = starSettlementData.resources.TryGet(GameResourceType.SiliconPromethium);

    foreach(var red in resourceEfficiencyDisplays){
      red.settlementEfficiencyBonus = starSettlementData.factoryEfficiencyBonus;
      red.Init();
    }
  }

}