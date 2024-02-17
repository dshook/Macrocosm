using UnityEngine;
using strange.extensions.mediation.impl;
using TMPro;

public class UpgradePanelView : View {
  [Inject] ResourceLoaderService loader {get; set;}

  public ShinyButton upgradeButton;

  public GameObject popDisplay;
  public GameObject moneyDisplay;
  public GameObject woodDisplay;
  public GameObject oreDisplay;

  public TMP_Text popText;
  public TMP_Text moneyText;
  public TMP_Text woodText;
  public TMP_Text oreText;

  public GameObject currentDamageDisplay;
  public GameObject currentRangeDisplay;
  public GameObject currentSpeedDisplay;

  public TMP_Text currentDamageText;
  public TMP_Text currentRangeText;
  public TMP_Text currentSpeedText;

  public TMP_Text nextStatsTitle;

  public GameObject nextDamageDisplay;
  public GameObject nextRangeDisplay;
  public GameObject nextSpeedDisplay;

  public TMP_Text nextDamageText;
  public TMP_Text nextRangeText;
  public TMP_Text nextSpeedText;

  protected override void Awake(){
    base.Awake();

  }

  public void UpdateText(TdTowerType type, int curTLevel, StageFiveDataModel stageFiveData){
    var towerStats = Tower.GetStats(loader, type);
    var canUpgrade = curTLevel < Tower.maxTowerLevel - 1;
    var nextLevel = curTLevel + 1;

    //Set base active status for the things that won't be there for max level towers
    popDisplay.SetActive(canUpgrade);
    moneyDisplay.SetActive(canUpgrade);
    woodDisplay.SetActive(canUpgrade);
    oreDisplay.SetActive(canUpgrade);

    nextStatsTitle.gameObject.SetActive(canUpgrade);
    nextDamageDisplay.SetActive(canUpgrade);
    nextRangeDisplay.SetActive(canUpgrade);
    nextSpeedDisplay.SetActive(canUpgrade);

    upgradeButton.gameObject.SetActive(canUpgrade);

    //current stats
    if(towerStats.hasDamage){
      currentDamageDisplay.SetActive(true);
      currentDamageText.text = towerStats.damage[curTLevel].ToString();
    }else{
      currentDamageDisplay.SetActive(false);
    }

    if(towerStats.hasRadius){
      currentRangeDisplay.SetActive(true);
      currentRangeText.text = towerStats.radius[curTLevel].ToString();
    }else{
      currentRangeDisplay.SetActive(false);
    }

    if(towerStats.hasSpeed){
      currentSpeedDisplay.SetActive(true);
      currentSpeedText.text = towerStats.speed[curTLevel].ToString();
    }else{
      currentSpeedDisplay.SetActive(false);
    }

    if(canUpgrade){

      upgradeButton.interactable = Tower.CanAffordTower(loader, type, nextLevel, stageFiveData);

      //next stats
      var damageDiff = towerStats.hasDamage ? towerStats.damage[nextLevel] - towerStats.damage[curTLevel] : 0;
      if(damageDiff > 0){
        nextDamageDisplay.SetActive(true);
        nextDamageText.text = string.Format("+{0}", damageDiff);
      }else{
        nextDamageDisplay.SetActive(false);
      }

      var radiusDiff = towerStats.hasRadius ? towerStats.radius[nextLevel] - towerStats.radius[curTLevel] : 0;
      if(radiusDiff > 0){
        nextRangeDisplay.SetActive(true);
        nextRangeText.text = string.Format("+{0:0.0}", radiusDiff);
      }else{
        nextRangeDisplay.SetActive(false);
      }

      var speedDiff = towerStats.hasSpeed ? towerStats.speed[nextLevel] - towerStats.speed[curTLevel] : 0;
      if(speedDiff > 0){
        nextSpeedDisplay.SetActive(true);
        nextSpeedText.text = string.Format("+{0:0.0}", speedDiff);
      }else{
        nextSpeedDisplay.SetActive(false);
      }

      //next cost
      var popCost = towerStats.hasPopCost ? towerStats.populationCost[nextLevel] : 0;
      if(popCost > 0){
        popDisplay.SetActive(true);
        popText.text = popCost.ToString();
      }else{
        popDisplay.SetActive(false);
      }

      var moneyCost = towerStats.hasMoneyCost ? towerStats.moneyCost[nextLevel] : 0;
      if(moneyCost > 0){
        moneyDisplay.SetActive(true);
        moneyText.text = moneyCost.ToString();
      }else{
        moneyDisplay.SetActive(false);
      }

      var woodCost = towerStats.hasWoodCost ? towerStats.woodCost[nextLevel] : 0;
      if(woodCost > 0){
        woodDisplay.SetActive(true);
        woodText.text = woodCost.ToString();
      }else{
        woodDisplay.SetActive(false);
      }

      var oreCost = towerStats.hasOreCost ? towerStats.oreCost[nextLevel] : 0;
      if(oreCost > 0){
        oreDisplay.SetActive(true);
        oreText.text = oreCost.ToString();
      }else{
        oreDisplay.SetActive(false);
      }
    }


  }

}
