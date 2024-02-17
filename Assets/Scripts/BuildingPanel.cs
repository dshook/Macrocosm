using System;
using System.Collections.Generic;
using PygmyMonkey.ColorPalette;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildingPanel : MonoBehaviour {
  public Transform queueTransform;
  public Transform buildListTransform;
  public Transform buildCompletedListTransform;
  public ScrollRect scrollRect;
  public TMP_Text emptyStateText;

  public UITabView tabs;

  public bool useCompletedList = false;

  public ResourceLoaderService loader {get; set;}

  public GalaxyPanel comingFromPanel;

  List<BuildingSelectionDisplay> selectionDisplays = new List<BuildingSelectionDisplay>();

  public int selectionDisplayCount {
    get{ return selectionDisplays.Count; }
  }

  void Awake(){

    if(!useCompletedList && tabs != null){
      tabs.HideTabs();
    }
  }

  //create building list display with some shortcuts for setting up the building selection display
  public void CreateBuildListItem(Action<BuildingSelectionDisplay> displayInitalizer){
    var upgradePrefab = loader.Load<GameObject>("Prefabs/BuildingSelection");
    var newMod = GameObject.Instantiate(
      upgradePrefab,
      Vector3.zero,
      Quaternion.identity
    );
    newMod.transform.SetParent(buildListTransform, false);

    var display = newMod.GetComponent<BuildingSelectionDisplay>();

    displayInitalizer(display);
    display.loader = loader;
    display.isQueueDisplay = false;
    display.Init();

    selectionDisplays.Add(display);

    emptyStateText.gameObject.SetActive(false);
  }

  public void CreateOrUpdateBuildListItem(int index, Action<BuildingSelectionDisplay> displayInitalizer){
    if(index >= selectionDisplays.Count){
      CreateBuildListItem(displayInitalizer);
      return;
    }

    var display = selectionDisplays[index];

    displayInitalizer(display);
    display.Init();
  }

  public void RemoveBuildListItem(int index){
    if(index >= selectionDisplays.Count){
      Debug.LogWarning(string.Format("Tried to remove idx {0} when we only have {0} displays", index, selectionDisplays.Count));
      return;
    }

    GameObject.Destroy(selectionDisplays[index].gameObject);
    selectionDisplays.RemoveAt(index);
  }

  //create completed building list item similar to above
  public void CreateCompletedListItem(Action<BuildingCreatedDisplay> displayInitalizer){
    var upgradePrefab = loader.Load<GameObject>("Prefabs/BuildingCreatedSelection");
    var newMod = GameObject.Instantiate(
      upgradePrefab,
      Vector3.zero,
      Quaternion.identity
    );
    newMod.transform.SetParent(buildCompletedListTransform, false);

    var display = newMod.GetComponent<BuildingCreatedDisplay>();

    displayInitalizer(display);
    display.loader = loader;
    display.Init();

  }

  //create building selection in queue with some shortcuts for setting up the building selection display
  public void CreateQueueSelection(Action<BuildingSelectionDisplay> displayInitalizer){
    var queuePrefab = loader.Load<GameObject>("Prefabs/BuildingQueueSelection");

    var newMod = GameObject.Instantiate(
      queuePrefab,
      Vector3.zero,
      Quaternion.identity
    );
    newMod.transform.SetParent(queueTransform, false);

    var display = newMod.GetComponent<BuildingSelectionDisplay>();

    displayInitalizer(display);
    display.order = (queueTransform.childCount).ToString();
    display.loader = loader;
    display.isQueueDisplay = true;
    display.Init();

  }


  public void RemoveQueueDisplay(int index){

    //renumber the remaining queue
    for(int i = index + 1; i < queueTransform.childCount; i++){
      var display = queueTransform.GetChild(i).GetComponent<BuildingSelectionDisplay>();
      display.order = (i).ToString();
      display.UpdateText();
    }

    var queuePanel = queueTransform.GetChild(index);
    DestroyImmediate(queuePanel.gameObject);
  }

  public int QueueCount(){
    return queueTransform.childCount;
  }

  public BuildingSelectionDisplay GetQueueSelectionDisplayAt(int index){
    return queueTransform.GetChild(index).GetComponent<BuildingSelectionDisplay>();
  }

  public void Clear(){
    if(buildListTransform != null){
      buildListTransform.DestroyChildren();
    }
    if(buildCompletedListTransform != null){
      buildCompletedListTransform.DestroyChildren();
    }
    if(queueTransform != null){
      queueTransform.DestroyChildren();
    }

    selectionDisplays.Clear();
  }


  public void ClickGalaxyBuildingUpgrade(
    GalaxyBuildingInfo buildingInfo,
    BuildingSelectionDisplay display,
    List<GalaxyBuildingData> buildQueue,
    Dictionary<GameResourceType, GalaxyResource> availableResources,
    StageSevenRulesProps stageSevenRules
  ){
    var building = buildingInfo.building;

    var buildingData = new GalaxyBuildingData(){
      buildingId = building.id,
      progress = 0f,
      started = false,
      finished = false,
    };
    buildQueue.Add(buildingData);


    if(display.quantity.HasValue){
      display.quantity--;
    }

    CreateGalaxyBuildQueueSelection(buildingInfo.building, buildingData, buildQueue, availableResources, stageSevenRules);

  }

  public void CreateGalaxyBuildQueueSelection(
    GalaxyBuilding building,
    GalaxyBuildingData buildData,
    List<GalaxyBuildingData> buildQueue,
    Dictionary<GameResourceType, GalaxyResource> availableResources,
    StageSevenRulesProps stageSevenRules
  ){

    CreateQueueSelection((BuildingSelectionDisplay bsd) => {
      bsd.buildId = (int)building.id;
      bsd.title = building.name;
      bsd.resourceCosts = building.resourceCosts(stageSevenRules);
      bsd.availableResources = availableResources;
      bsd.iconPaths = building.iconPaths;
      bsd.OnClick = () => RemoveGalaxyBuildingFromQueue(bsd.transform, buildQueue);
    });

  }

  void RemoveGalaxyBuildingFromQueue(Transform queueTransform, List<GalaxyBuildingData> buildQueue){
    var index = queueTransform.GetSiblingIndex();

    buildQueue.RemoveAt(index);

    BuildingSelectionDisplay queueDisplay = queueTransform.GetComponent<BuildingSelectionDisplay>();

    //possibly re-enable a building display button if we just brought it back to 1 quantity
    for(int i = 0; i < buildListTransform.childCount; i++){
      var display = buildListTransform.GetChild(i).GetComponent<BuildingSelectionDisplay>();
      if(display.buildId == queueDisplay.buildId){
        if(display.quantity.HasValue){
          display.quantity++;
        }

        break;
      }
    }

    RemoveQueueDisplay(index);
  }

  public void ShowEmptyMessage(string message){
    emptyStateText.gameObject.SetActive(true);
    emptyStateText.text = message;
  }
}