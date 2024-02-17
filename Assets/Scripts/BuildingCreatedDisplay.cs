using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildingCreatedDisplay : MonoBehaviour {
  public TMP_Text titleComp;
  public TMP_Text descripComp;
  public TMP_Text productionLabel;
  public TMP_Text upkeepLabel;

  public Transform productionResourceDisplayHolder;
  public Transform upkeepResourceDisplayHolder;
  public Transform iconHolder;

  public GameObject resourceDisplayPrefab;

  public int buildId;
  public int quantity;

  public ResourceLoaderService loader {get; set;}

  public string title {get; set;}
  public string descrip {get; set;}
  public string order {get; set;}

  public string[] iconPaths {get; set;}

  HorizontalLayoutGroup hlg;

  void Awake(){
    hlg = productionResourceDisplayHolder.GetComponent<HorizontalLayoutGroup>();
  }

  public void Init(){
    if(!string.IsNullOrEmpty(title)){
      UpdateText();
    }

    productionResourceDisplayHolder.DestroyChildren();
    productionResourceDisplayHolder.gameObject.SetActive(false);
    productionLabel.gameObject.SetActive(false);

    upkeepResourceDisplayHolder.DestroyChildren();
    upkeepResourceDisplayHolder.gameObject.SetActive(false);
    upkeepLabel.gameObject.SetActive(false);

    if(iconPaths != null && iconPaths.Length > 0){
      BuildingSelectionDisplay.UpdateIcons(iconHolder, iconPaths, loader);
    }

    hlg.ForceUpdate();

  }

  public void UpdateText(){
    if(titleComp != null){
      if(quantity > 0){
        titleComp.text = quantity + " " + title;
      }else{
        titleComp.text = title;
      }
    }
    if(descripComp != null){
      descripComp.text = descrip;
    }
    for(int c = 0; c < productionResourceDisplayHolder.childCount; c++){
      var resourceDisplay = productionResourceDisplayHolder.GetChild(c).GetComponent<ResourceDisplay>();
      resourceDisplay.UpdateText();
    }
  }

}