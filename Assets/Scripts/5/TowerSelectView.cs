using UnityEngine;
using UnityEngine.UI;
using strange.extensions.mediation.impl;
using TMPro;
using Unity.VectorGraphics;

public class TowerSelectView : View {
  [Inject] ResourceLoaderService loader {get; set;}

  public TdTowerType towerType;
  public int towerLevel;

  public ShinyButton button;
  public TMP_Text towerName;
  public TMP_Text descrip;

  public SVGImage iconRenderer;

  public GameObject moneyDisplay;
  public GameObject woodDisplay;
  public GameObject oreDisplay;

  public TMP_Text moneyText;
  public TMP_Text woodText;
  public TMP_Text oreText;

  //I'm lazy, little braindead, and its late, but this works as a hack
  bool skipAwakeUpdate = false;

  protected override void Awake() {
    base.Awake();

    if(!skipAwakeUpdate){
      UpdateDescrip();
    }
  }

  public void UpdateDescrip(){
    if(loader == null){
      bubbleToContext(this, BubbleType.Add, false);
    }
    var towerStats = Tower.GetStats(loader, towerType);
    if(button != null){
      button.label = towerStats.name;
    }
    if(towerName != null){
      towerName.text = towerStats.name;
    }
    descrip.text = towerStats.descrip;

    iconRenderer.sprite = towerStats.sprite;

    if(towerStats.hasMoneyCost && towerStats.moneyCost[towerLevel] > 0){
      moneyDisplay.SetActive(true);
      moneyText.text = towerStats.moneyCost[towerLevel].ToString();
    }else{
      moneyDisplay.SetActive(false);
    }

    if(towerStats.hasWoodCost && towerStats.woodCost[towerLevel] > 0){
      woodDisplay.SetActive(true);
      woodText.text = towerStats.woodCost[towerLevel].ToString();
    }else{
      woodDisplay.SetActive(false);
    }

    if(towerStats.hasOreCost && towerStats.oreCost[towerLevel] > 0){
      oreDisplay.SetActive(true);
      oreText.text = towerStats.oreCost[towerLevel].ToString();
    }else{
      oreDisplay.SetActive(false);
    }
  }

  //Overriding for special cases like the tower sell view
  public void UpdateCosts(int moneyCost, int woodCost, int oreCost){
    skipAwakeUpdate = true;

    if(moneyCost > 0){
      moneyDisplay.SetActive(true);
      moneyText.text = moneyCost.ToString();
    }else{
      moneyDisplay.SetActive(false);
    }

    if(woodCost > 0){
      woodDisplay.SetActive(true);
      woodText.text = woodCost.ToString();
    }else{
      woodDisplay.SetActive(false);
    }

    if(oreCost > 0){
      oreDisplay.SetActive(true);
      oreText.text = oreCost.ToString();
    }else{
      oreDisplay.SetActive(false);
    }
  }

}