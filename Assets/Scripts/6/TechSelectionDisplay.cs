using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TechSelectionDisplay : MonoBehaviour {
  public GameObject display;
  public TMP_Text titleComp;
  public TMP_Text resourceCostComp;
  public Unity.VectorGraphics.SVGImage underline;
  public Unity.VectorGraphics.SVGImage resourceIcon;
  public Transform iconHolder;
  public Button button;

  public HexTechId techId {get; set;}
  public string title {get; set;}
  public string cost {get; set;}
  public List<GameObject> prereqLines = new List<GameObject>();

  public ResourceLoaderService loader;

  bool _visible;
  public bool visible {
    get{ return _visible; }
    set{
      _visible = value;
      UpdateActive();
    }
  }

  public bool researchable;

  bool _completed;
  public bool completed {
    get { return _completed; }
    set {
      _completed = value;
      UpdateOutlineColor();
    }
  }

  bool _inQueue;
  public bool inQueue {
    get { return _inQueue; }
    set {
      _inQueue = value;
      UpdateOutlineColor();
    }
  }

  public Action OnClick {get; set;}

  public void Init(){
    UpdateText();
    UpdateActive();
    UpdateIcons();
    if(button != null && OnClick != null){
      button.onClick.AddListener(() => OnClick());
    }
  }

  public void UpdateText(){
    if(titleComp != null){
      titleComp.text = title;
    }
    if(resourceCostComp != null){
      resourceCostComp.text = cost;
    }
  }

  void UpdateActive(){
    if(display != null){
      display.SetActive(_visible);
    }
    foreach(var line in prereqLines){
      line.SetActive(_visible);
    }
  }

  void UpdateOutlineColor(){
    if(underline == null){ return; }

    underline.color = Colors.transparent;

    if(_inQueue){
      underline.color = Colors.mint;
    }
    if(_completed){
      underline.color = Colors.blue;
    }
  }

  void UpdateIcons(){
    if(!HexTech.allTechs.ContainsKey(techId)){
      return;
    }
    var iconPaths = HexTech.allTechs[techId].iconPaths;
    var iconsPresent = iconHolder.childCount;

    if(iconPaths.Length > iconsPresent){
      //duplicate the tech icon if we need more
      var dupeTemplate = iconHolder.GetChild(0);
      var needToCreate = iconPaths.Length - iconsPresent;
      for(int i = 0; i < needToCreate; i++){
        var newIcon = GameObject.Instantiate(dupeTemplate, Vector3.zero, Quaternion.identity);
        newIcon.SetParent(iconHolder, false);
      }
    }

    for(var i = 0; i < iconPaths.Length; i++){
      var iconPath = iconPaths[i];
      var iconImage = iconHolder.GetChild(i).GetComponent<Unity.VectorGraphics.SVGImage>();

      iconImage.sprite = loader.Load<Sprite>(iconPath);

    }
  }
}