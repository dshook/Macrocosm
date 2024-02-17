using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityPackages.UI;

//Remember this is used for the full selection display as well as the small queue display
public class BuildingSelectionDisplay : MonoBehaviour {
  public TMP_Text descripComp;
  public TMP_Text orderComp;

  public RectTransform resourceDisplayHolder;
  public GameObject resourceDisplayPrefab;
  public Transform iconHolder;
  public ShinyButton button;

  public int buildId;

  public ResourceLoaderService loader {get; set;}

  int? _quantity;
  public int? quantity {
    get { return _quantity; }
    set {
      _quantity = value;
      if(_quantity.HasValue){
        //hide the building display if we can't make any more
        if(quantity.Value == 0){
          gameObject.SetActive(false);
        }else{
          gameObject.SetActive(true);
        }
      }
    }
  }
  public int? totalQuantity {get; set;}
  public string title {get; set;}
  public string descrip {get; set;}
  public string order {get; set;}
  public bool isQueueDisplay {get; set;}
  public bool disabled {get; set;}

  public GameResource[] resourceCosts {get; set;}

  //Should ideally be GameResource as the value but some weird typing issue with that
  public Dictionary<GameResourceType, GalaxyResource> availableResources {get; set;}

  public string[] iconPaths {get; set;}

  public Action OnClick {get; set;}

  HorizontalLayoutGroup hlg;
  UIFlexbox resourceFlexbox;

  void Awake(){
    if(button != null){
      button.onClick.AddListener(ButtonClick);
    }

    hlg = resourceDisplayHolder.GetComponent<HorizontalLayoutGroup>();
    resourceFlexbox = resourceDisplayHolder.GetComponent<UIFlexbox>();
  }

  public void Init(){
    if(!string.IsNullOrEmpty(title)){
      UpdateText();
    }

    button.interactable = !disabled;

    resourceDisplayHolder.DestroyChildren();
    foreach(var resourceCost in resourceCosts){
      var newResourceDisplay = GameObject.Instantiate(resourceDisplayPrefab, Vector3.zero, Quaternion.identity);
      newResourceDisplay.transform.SetParent(resourceDisplayHolder, false);

      var resourceDisplay = newResourceDisplay.GetComponent<ResourceDisplay>();
      resourceDisplay.loader = loader;
      resourceDisplay.resource = resourceCost;

      if(availableResources != null){
        if(availableResources.ContainsKey(resourceCost.type)){
          resourceDisplay.availableAmount = availableResources[resourceCost.type].amount;
        }else{
          resourceDisplay.availableAmount = 0;
        }
      }

      if(isQueueDisplay){
        resourceDisplay.numberFormat = resourceCosts.Length > 1 ? NumberFormatLength.SuperShort : NumberFormatLength.Short;
      }else{
        resourceDisplay.numberFormat = resourceCosts.Length > 2 ? NumberFormatLength.Short : NumberFormatLength.Normal;
      }
      resourceDisplay.Init();
    }

    if(iconPaths != null && iconPaths.Length > 0){
      UpdateIcons(iconHolder, iconPaths, loader);
    }

    if(hlg != null){
      LayoutRebuilder.ForceRebuildLayoutImmediate(resourceDisplayHolder);
      hlg.CalculateLayoutInputHorizontal();
      hlg.ForceUpdate();
    }
    if(resourceFlexbox != null){
      resourceFlexbox.Draw();
    }

  }

  public void UpdateText(){
    button.label = title;
    if(descripComp != null){
      descripComp.text = descrip;
    }
    if(orderComp != null){
      orderComp.text = order;
    }
    for(int c = 0; c < resourceDisplayHolder.childCount; c++){
      var resourceDisplay = resourceDisplayHolder.GetChild(c).GetComponent<ResourceDisplay>();
      resourceDisplay.UpdateText();
    }
  }

  public static void UpdateIcons(Transform iconHolder, string[] iconPaths, ResourceLoaderService loader){
    if(iconHolder == null){
      return;
    }
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

    //and show
    for(var i = 0; i < iconHolder.childCount; i++){
      var icon = iconHolder.GetChild(i);
      if(i >= iconPaths.Length){
        icon.gameObject.SetActive(false);
      }else{
        var iconPath = iconPaths[i];
        var iconImage = icon.GetComponent<Unity.VectorGraphics.SVGImage>();

        iconImage.sprite = loader.Load<Sprite>(iconPath);
        icon.gameObject.SetActive(true);
      }
    }
  }

  void ButtonClick(){
    if(OnClick != null){
      OnClick();
    }
  }
}