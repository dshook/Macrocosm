using UnityEngine;
using strange.extensions.mediation.impl;
using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine.UI;

public class GalaxyResourceStack : View {
  [Tooltip("For string changer namespace")]
  public string stackName;

  public GameObject resourceStack;
  public GameObject resourceDisplayPrefab;
  public GameObject emptyPlaceholder;
  public Transform contentParent;
  public ScrollRect scroll;

  public TMP_Text titleTMP;

  List<GalaxyResourceDisplayBase> resourceDisplays = new List<GalaxyResourceDisplayBase>();
  public Action OnImportExportChanged;

  protected override void Awake () {
    base.Awake();
  }

  void Update () {
  }

  public void ScrollToBeginning(){
    if(scroll != null){
      scroll.horizontalNormalizedPosition = 0;
    }else{
      Debug.LogWarning("ScrollRect not set");
    }
  }

  public void UpdateResourceStack(
    ref List<GalaxyResource> resources,
    Dictionary<GameResourceType, GameResource> resourceDeltas
  ){
    resourceStack.SetActive(true);

    if(resources == null || resources.Count == 0){
      if(emptyPlaceholder != null){
        emptyPlaceholder.SetActive(true);
      }
      return;
    }
    UnityEngine.Profiling.Profiler.BeginSample("UpdateResourceStack");
    if(emptyPlaceholder != null){
      emptyPlaceholder.SetActive(false);
    }

    resources.Sort((a, b) => b.amount - a.amount);
    var orderedResources = resources;

    UnityEngine.Profiling.Profiler.BeginSample("looping");

    int index = 0;
    foreach(var resource in orderedResources){

      if(resourceDisplays.Count <= index){
        UnityEngine.Profiling.Profiler.BeginSample("spawning");
        var newResourceDisplay = GameObject.Instantiate(resourceDisplayPrefab, Vector3.zero, Quaternion.identity, contentParent);

        var newDisplay = newResourceDisplay.GetComponent<GalaxyResourceDisplayBase>();
        newDisplay.stringNamespace = stackName;
        newDisplay.OnImportExportChanged = OnImportExportChanged;

        resourceDisplays.Add(newDisplay);
        UnityEngine.Profiling.Profiler.EndSample();
      }

      UnityEngine.Profiling.Profiler.BeginSample("assigning");
      var resourceDisplay = resourceDisplays[index];
      resourceDisplay.resource = resource;
      resourceDisplay.resourceDeltas = resourceDeltas;
      resourceDisplay.OnImportExportChanged = OnImportExportChanged;
      UnityEngine.Profiling.Profiler.EndSample();

      index++;
    }

    UnityEngine.Profiling.Profiler.EndSample();
    UnityEngine.Profiling.Profiler.BeginSample("removing");

    //remove displays that don't have resources
    for(int i = resourceDisplays.Count - 1; i >= orderedResources.Count; i--){

      Destroy(resourceDisplays[i].gameObject);
      resourceDisplays.RemoveAt(i);
    }
    UnityEngine.Profiling.Profiler.EndSample();
    UnityEngine.Profiling.Profiler.EndSample();
  }

  public void TearDownResourceStack(){
    resourceStack.SetActive(false);
    foreach(var display in resourceDisplays){
      Destroy(display.gameObject);
    }
    resourceDisplays.Clear();
  }
}
