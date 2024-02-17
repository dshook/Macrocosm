using UnityEngine;
using strange.extensions.mediation.impl;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class TechManager : View {

  [Inject] ResourceLoaderService loader {get; set;}
  [Inject] StringChanger stringChanger {get; set;}

  [Inject] StageSixDataModel stageSixData {get; set;}
  [Inject] StageRulesService stageRules { get; set; }

  public TechAdvancer techAdvancer;

  public ShinyButton techButton;
  public ShinyButton techCancelButton;
  public Transform techSelectionTransform;
  public Transform techLineTransform;

  public BuildingPanel buildingPanel;

  public TMP_Text currentResearching;
  public UIFilledBar techProgress;

  public HexPanelManager panelManager;
  public TMP_Text scienceValue;

  //synced from stage six manager
  public List<HexCity> cityList;

  Dictionary<HexTechId, TechSelectionDisplay> techDisplays = new Dictionary<HexTechId, TechSelectionDisplay>();

  public Transform detailOverlay;
  public BuildingSelectionDisplay detailDisplay;
  public ShinyButton detailCancelButton;
  HexTech selectedTech = null;

  protected override void Awake() {
    base.Awake();

    techButton.onClick.AddListener(StartResearching);
    techCancelButton.onClick.AddListener(EndResearching);

    //set up detail panel
    detailDisplay.OnClick = ResearchFromDetail;
    detailDisplay.loader = loader;
    buildingPanel.loader = loader;
    // researchButton.onClick.AddListener(ResearchFromDetail);
    detailCancelButton.onClick.AddListener(HideDetail);
  }

  public void Reset(){
    stageSixData.techs = new Dictionary<HexTechId, bool>();
    stageSixData.techQueue = new List<TechBuildingData>();
    techDisplays.Clear();
    buildingPanel.Clear();
    techLineTransform.DestroyChildren();
  }

  public void OnLoad(){
    BuildTechPanel();
    //recreate the build queue if we need to
    for(var i = 0; i < stageSixData.techQueue.Count; i++){
      var tech = stageSixData.techQueue[i];
      if(!tech.finished){
        CreateTechQueueSelection(HexTech.allTechs[tech.techId]);
      }
    }
  }

  string scienceFormat = $"{{0:#,0}} + <#{Colors.goldenHex}>{{1}}</color> /s";

  void Update(){
    // remove all queue displays for finished techs
    while(buildingPanel.QueueCount() > 0){
      var firstQueueDisplay = buildingPanel.GetQueueSelectionDisplayAt(0);
      var queueTechId = (HexTechId)firstQueueDisplay.buildId;
      if(stageSixData.techs.ContainsKey(queueTechId) && stageSixData.techs[queueTechId]){
        //remove finished
        buildingPanel.RemoveQueueDisplay(0);
      }else{
        //otherwise we're done checking
        break;
      }
    }


    stringChanger.UpdateString(
      scienceValue,
      "totalSciencePerSecond",
      techAdvancer.TotalSciencePerSecond,
      scienceFormat,
      techAdvancer.CitySciencePerSecond,
      techAdvancer.TribeSciencePerSecond
    );

    if(stageSixData.techQueue.Count > 0){

      techProgress.gameObject.SetActive(true);
      var researching = stageSixData.techQueue[0];
      var curTech = HexTech.allTechs[researching.techId];

      techProgress.fillAmt = researching.progress;
      currentResearching.text = HexTech.allTechs[researching.techId].name;
      if(researching.finished){
        techProgress.color = Colors.green;
      }else{
        techProgress.color = Colors.blue;
      }
    }else{
      techProgress.gameObject.SetActive(false);
      currentResearching.text = "<- Press button to research";
    }
  }

  void BuildTechPanel(){
    var rowPrefab = loader.Load<GameObject>("Prefabs/6/TechRow");
    var techPrefab = loader.Load<GameObject>("Prefabs/6/TechSelection");
    var linePrefab = loader.Load<GameObject>("Prefabs/6/TechLine");

    var techLineTransformRt = techLineTransform.GetComponent<RectTransform>();

    var techRows = HexTech.allTechs.Max(x => x.Value.techLevel);
    var rowIndex = HexTech.allTechs.Min(x => x.Value.techLevel);
    do{
      var rowTechs = HexTech.allTechs.Where(t => t.Value.techLevel == rowIndex).ToList();
      if(rowTechs.Count == 0){
        Debug.LogWarning("No techs for level: " + rowIndex);
        rowIndex++;
        continue;
      }
      var newRow = GameObject.Instantiate(rowPrefab, Vector3.zero, Quaternion.identity);
      newRow.transform.SetParent(techSelectionTransform, false);

      foreach(var tech in rowTechs){
        var newTech = GameObject.Instantiate(techPrefab, Vector3.zero, Quaternion.identity);
        newTech.transform.SetParent(newRow.transform, false);

        var display = newTech.GetComponent<TechSelectionDisplay>();
        display.techId = tech.Key;
        display.title = tech.Value.name;
        display.cost = tech.Value.techCost(stageRules.StageSixRules).ToThousandsFormat();
        display.researchable = tech.Value.prereqTechs == null;
        display.completed = false;
        display.visible = false;
        display.OnClick = () => ClickTechUpgrade(tech.Value, display);
        display.loader = loader;
        var rectTransform = newTech.GetComponent<RectTransform>();
        techDisplays[tech.Key] = display;

        //create prereq lines
        if(tech.Value.prereqTechs != null){
          foreach(var prereq in tech.Value.prereqTechs){
            var newLine = GameObject.Instantiate(linePrefab, Vector3.zero, Quaternion.identity);
            newLine.transform.SetParent(techLineTransform, false);

            var lineAnchorer = newLine.GetComponent<LineAnchorer>();
            if(!techDisplays.ContainsKey(prereq)){
              Debug.LogWarning(string.Format("Missing tech display prereq {0} when setting up {1}", prereq, tech.Value.name));
              continue;
            }
            lineAnchorer.start = techDisplays[prereq].transform.GetComponent<RectTransform>();
            lineAnchorer.end = rectTransform;
            lineAnchorer.relativeTo = techLineTransformRt;

            display.prereqLines.Add(newLine);
          }
        }

        display.Init();
      }

      rowIndex++;
    }while(rowIndex <= techRows);

  }

  bool scrolledToLastTech = false;

  void StartResearching(){
    UpdateAvailableTechs();

    //Scroll to first unresearched tech when starting up
    if(!scrolledToLastTech){
      StartCoroutine(ScrollToLastResearchedTech());
    }
    //and show
    panelManager.SwitchTo(HexPanel.Tech);
  }

  //This needs to be a coroutine to give the flexbox time to update itself before scrolling
  IEnumerator ScrollToLastResearchedTech(){
    yield return new WaitForSecondsRealtime(0.1f);

    var techScrollContent = buildingPanel.scrollRect.content;
    var techRowHeight = techSelectionTransform.GetChild(0).GetComponent<RectTransform>().rect.height;
    var techToScrollToLevel = 1;
    foreach(var techDispKV in techDisplays){
      var techDisp = techDispKV.Value;
      if(techDisp.researchable == true && techDisp.visible == true && techDisp.completed == false){
        //note to self, don't screw up the tech levels
        techToScrollToLevel = HexTech.allTechs[techDispKV.Key].techLevel;
        break;
      }
    }

    var scrollAddition = (techToScrollToLevel - 1) * techRowHeight;
    techScrollContent.offsetMin = techScrollContent.offsetMin.AddY(scrollAddition);
    techScrollContent.offsetMax = techScrollContent.offsetMax.AddY(scrollAddition);

    scrolledToLastTech = true;
  }

  public struct TechResearchInfo{
    public HexTechId techId;
    public bool researchable;
    public bool completed;
  }

  void UpdateAvailableTechs(){
    foreach(var techId in techDisplays.Keys){
      var techDisp = techDisplays[techId];
      var tech = HexTech.allTechs[techId];

      var visible = false;
      var researchable = false;
      var completed = false;
      var inQueue = false;

      if(stageSixData.ResearchedTech(techId)){
        //Already researched techs
        completed = true;
        visible = true;
      }else if(stageSixData.InQueue(techId)){
        //In da queue
        visible = true;
        inQueue = true;
      }else if(tech.prereqTechs == null){
        //beginning techs
        researchable = true;
        visible = true;
      }

      techDisp.visible = visible;
      techDisp.researchable = researchable;
      techDisp.completed = completed;
      techDisp.inQueue = inQueue;
    }

    //second pass to find the techs 2 levels removed from the already researched techs
    var toAdd = new List<HexTechId>();
    foreach(var techId in techDisplays.Keys){
      var techDisp = techDisplays[techId];
      var tech = HexTech.allTechs[techId];

      if( tech.prereqTechs != null && tech.prereqTechs.Any(pt => techDisplays[pt].completed )){
        toAdd.Add(techId);

        toAdd.AddRange(HexTech.allTechs
          .Where(ht => ht.Value.prereqTechs != null && ht.Value.prereqTechs.Contains(techId))
          .Select(ht => ht.Key)
        );
      }
    }
    //Add them separately so you don't accidently add too many in the previous loop
    foreach(var techId in toAdd){
      var techDisp = techDisplays[techId];
      techDisp.visible = true;
      techDisp.researchable = true;
    }

    //another pass to find any visible techs that have prereqs that still aren't visible because
    //they might be on a side branch that hasn't been researched at all but joins together
    //Have to follow all the way back to one that's visible
    foreach(var techDispKV in techDisplays){
      var techDisp = techDispKV.Value;
      var tech = HexTech.allTechs[techDispKV.Key];

      if(!techDisp.visible){ continue; }

      if(tech.prereqTechs != null ){
        toAdd.Clear();

        foreach(var prereq in tech.prereqTechs){
          if(!techDisplays[prereq].visible){ toAdd.Add(prereq); }
        }
        for(var i = 0; i < toAdd.Count; i++){
          var techIdToAdd = toAdd[i];
          techDisplays[techIdToAdd].visible = true;

          var techToAddPrereqs = HexTech.allTechs[techIdToAdd].prereqTechs;
          if(techToAddPrereqs != null){
            foreach(var prereq in techToAddPrereqs){
              if(!techDisplays[prereq].visible){ toAdd.Add(prereq); }
            }
          }
        }
      }
    }

    //hide all the tech rows that don't have any active children to limit the scroll height
    for(var techRowIndex = 0 ; techRowIndex < techSelectionTransform.childCount; techRowIndex++){
      var techRow = techSelectionTransform.GetChild(techRowIndex);
      var hasActiveChildren = false;
      for(var techIndex = 0; techIndex < techRow.childCount; techIndex++){
        if(techRow.GetChild(techIndex).GetComponent<TechSelectionDisplay>().visible){
          hasActiveChildren = true;
          break;
        }
      }
      techRow.gameObject.SetActive(hasActiveChildren);
    }
  }

  void ClickTechUpgrade(HexTech tech, TechSelectionDisplay display){

    selectedTech = tech;

    ShowDetail();
  }

  void CreateTechQueueSelection(HexTech tech){
    buildingPanel.CreateQueueSelection((BuildingSelectionDisplay bsd) => {
      bsd.buildId = (int)tech.id;
      bsd.title = tech.name;
      bsd.resourceCosts = new GameResource[]{ new GameResource{type = GameResourceType.Science, amount = tech.techCost(stageRules.StageSixRules)}};
      bsd.iconPaths = tech.iconPaths;
      bsd.OnClick = () => RemoveTechFromQueue(bsd.transform);
    });

    //mark the tech in the tree as in queue
    techDisplays[tech.id].inQueue = true;
  }

  void RemoveTechFromQueue(Transform queueTransform){
    var index = queueTransform.GetSiblingIndex();
    if(index < 0 || index >= stageSixData.techQueue.Count){
      Debug.LogWarning("Queue index invalid to remove");
      return;
    }
    stageSixData.techQueue.RemoveAt(index);

    var queueDisplay = queueTransform.GetComponent<BuildingSelectionDisplay>();
    if(queueDisplay == null){
      Debug.LogWarning("Queue display to remove null");
      return;
    }
    var techId = (HexTechId)queueDisplay.buildId;

    buildingPanel.RemoveQueueDisplay(index);

    //also remove any dependent techs from queue
    var toRemove = new List<Transform>();
    foreach(var qt in stageSixData.techQueue){
      var prereqs = HexTech.allTechs[qt.techId].prereqTechs;
      if(prereqs == null) continue;

      if(!prereqs.Any(pr => pr == techId)){
        continue;
      }

      for(int i = buildingPanel.queueTransform.childCount - 1; i >= 0; i--){
        var display = buildingPanel.queueTransform.GetChild(i).GetComponent<BuildingSelectionDisplay>();
        if(display != null && display.buildId == (int)qt.techId){
          toRemove.Add(display.transform);
        }
      }
    }
    foreach(var toR in toRemove){
      RemoveTechFromQueue(toR);
    }

    //mark the tech in the tree as no longer in queue
    UpdateAvailableTechs();
  }

  void EndResearching(){
    panelManager.SwitchTo(HexPanel.Map);
  }

  void ShowDetail(){
    detailOverlay.gameObject.SetActive(true);
    // researchButton.gameObject.SetActive(techDisplays[selectedTech.id].researchable);

    detailDisplay.buildId = (int)selectedTech.id;
    detailDisplay.title = selectedTech.name;
    detailDisplay.descrip = selectedTech.descrip;
    detailDisplay.resourceCosts = new GameResource[]{ new GameResource{type = GameResourceType.Science, amount = selectedTech.techCost(stageRules.StageSixRules)}};
    detailDisplay.iconPaths = selectedTech.iconPaths;
    detailDisplay.Init();
  }

  void HideDetail(){
    detailOverlay.gameObject.SetActive(false);
    selectedTech = null;
  }

  void ResearchFromDetail(){
    if(!stageSixData.ResearchedTech(selectedTech.id)){
      ResearchTech(selectedTech);
    }
    HideDetail();
  }

  void ResearchTech(HexTech tech){
    if(!techDisplays[tech.id].researchable){
      return;
    }

    AddPrereqTechsToQueueRecursively(tech.id);
    AddTechToQueue(tech.id);

    UpdateAvailableTechs();
  }

  void AddPrereqTechsToQueueRecursively(HexTechId techId){
    var tech = HexTech.allTechs[techId];
    if(tech.prereqTechs == null){ return; }

    foreach(var prereq in tech.prereqTechs){
      //Add tech to queue if it's not been researched and it's not in the queue yet
      if(!stageSixData.ResearchedTech(prereq) && !stageSixData.InQueue(prereq)
      ){
        AddPrereqTechsToQueueRecursively(prereq);
        AddTechToQueue(prereq);
      }
    }
  }

  void AddTechToQueue(HexTechId techId){
    stageSixData.techQueue.Add(new TechBuildingData(){
      techId = techId,
      progress = 0f,
      finished = false,
    });

    CreateTechQueueSelection(HexTech.allTechs[techId]);
  }


}
