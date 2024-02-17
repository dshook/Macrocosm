using System;
using UnityEngine;
using UnityEngine.UI;
using strange.extensions.mediation.impl;
using TMPro;
using System.Linq;

public class HqPanelView : View {
  [Inject] StageRulesService stageRules { get; set; }
  [Inject] StageTransitionModel stageData {get; set;}
  [Inject] StageFourDataModel stageFourData {get; set;}
  [Inject] StageFiveDataModel stageFiveData {get; set;}
  [Inject] PopulationIncreasedSignal populationIncreased {get; set;}

  public TMP_Text population;
  public TMP_Text growsEvery;
  public TMP_Text baseGrowthNumber;
  public TMP_Text creatureProgressNumber;
  public TMP_Text farmsNumber;
  public TMP_Text total;
  public TMP_Text nextGrowthWave;

  public ShinyButton recruitButton;
  public TMP_Text recruitMoneyCost;
  public TMP_Text recruitWoodCost;
  public TMP_Text recruitOreCost;

  protected override void Awake(){
    base.Awake();

    recruitButton.onClick.AddListener(OnRecruit);
    UpdateRecruitButton();
  }

  void Update () {
  }

  public void UpdateText(TdGrid grid){
    var wave = stageData.activeSubStage[5] + 1;

    var numFarms = stageFiveData.savedTowers.Where(t =>
      t != null
      && t.towerType == TdTowerType.Farm
    ).Sum(t => t.towerLevel + 1);
    var numTribalUpgrades = stageFiveData.creaturePopulationBonus;
    var adjustedWavesPerPop = stageFiveData.GetAdjustedWavesPerPop(stageRules);

    population.text = "Population: " + stageFiveData.population;
    growsEvery.text = string.Format("Population grows every <size=+6><b>{0}</b></size> waves", adjustedWavesPerPop);
    baseGrowthNumber.text = stageRules.StageFiveRules.baseWavesPerPop.ToString();
    creatureProgressNumber.text = string.Format("- <#{0}>{1}</color>", Colors.goldenHex, numTribalUpgrades);
    farmsNumber.text = "- " + numFarms.ToString();
    total.text = "= " + adjustedWavesPerPop;

    nextGrowthWave.text = string.Format("Next growth after wave <size=+6><b>{0}</b></size>", adjustedWavesPerPop + stageFiveData.lastWaveToIncreasePop);

    UpdateRecruitButton();
  }

  void UpdateRecruitButton(){
    recruitButton.interactable = CanAffordRecruit();

    recruitMoneyCost.text = recruitMoney.ToString();
    recruitWoodCost.text = recruitWood.ToString();
    recruitOreCost.text = recruitOre.ToString();
  }

  void OnRecruit(){
    //shouldn't ever happen but just in case
    if(!CanAffordRecruit()){ return; }

    stageFiveData.money -= recruitMoney;
    stageFiveData.wood -= recruitWood;
    stageFiveData.ore -= recruitOre;
    stageFiveData.population++;
    stageFiveData.populationRecruited++;

    populationIncreased.Dispatch();
    UpdateRecruitButton();
  }

  bool CanAffordRecruit(){
    if(
      ( stageFiveData.money < recruitMoney)
      || ( stageFiveData.wood < recruitWood)
      || ( stageFiveData.ore < recruitOre)
    ){
      return false;
    }

    return true;
  }

  int recruitMoney {
    get{
      return stageRules.StageFiveRules.recruitMoneyCost +
        (stageFiveData.populationRecruited * stageRules.StageFiveRules.additionalRecruitMoneyCost);
    }
  }
  int recruitWood {
    get{
      return stageRules.StageFiveRules.recruitWoodCost +
        (stageFiveData.populationRecruited * stageRules.StageFiveRules.additionalRecruitWoodCost);
    }
  }
  int recruitOre {
    get{
      return stageRules.StageFiveRules.recruitOreCost +
        (stageFiveData.populationRecruited * stageRules.StageFiveRules.additionalRecruitOreCost);
    }
  }

}
