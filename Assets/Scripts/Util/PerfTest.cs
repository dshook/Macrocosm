using TMPro;
using UnityEngine;
using strange.extensions.mediation.impl;
using UnityEngine.UI;

public class PerfTest : View {

  public GameObject atomPrefab;
  public GameObject particleHolder;

  public int startingCount;
  public int startingSize;

  public Button addMoreButton;

  [Inject] SpawnService spawner {get; set;}
  RectTransform stageRectTransform;


  protected override void Awake () {
    stageRectTransform = transform.parent.GetComponent<RectTransform>();
    addMoreButton.onClick.AddListener(() => AddMore());

    spawner.SpawnObjects(atomPrefab, startingCount, stageRectTransform, particleHolder.transform, null, (GameObject g) => {
      var atomInstance = g.GetComponentInChildren<AtomRenderer>();
      atomInstance.size = startingSize;
    });
  }


  void Update () {
  }

  void AddMore(){
    spawner.SpawnObjects(atomPrefab, 10, stageRectTransform, particleHolder.transform, null, (GameObject g) => {
      var atomInstance = g.GetComponentInChildren<AtomRenderer>();
      atomInstance.size = startingSize;
    });
  }


}
