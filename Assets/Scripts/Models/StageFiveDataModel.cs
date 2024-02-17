using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
[Singleton]
public class StageFiveDataModel
{
  public int population {get; set;}
  public int assignedPopulation {get; set;}
  public int money {get; set;}
  public int ore {get; set;}
  public int wood {get; set;}
  public List<SavedTower> savedTowers {get;set;}
  public List<SavedTower> previousRunSavedTowers {get;set;}

  public int lastWaveToIncreasePop {get; set;}
  public int populationRecruited {get; set;}
  public int lastCompletedWave {get; set;}

  public int stageHighScore {get; set;}

  public int creaturePopulationBonus { get; set; }

  //Gets how many waves are between each population increase adjusted for farms and other progression
  public int GetAdjustedWavesPerPop(StageRulesService stageRules) {
    var numFarms = 0;
    if(savedTowers != null){
      foreach(var tower in savedTowers){
        if(tower.towerType == TdTowerType.Farm){
          numFarms += tower.towerLevel + 1;
        }
      }
    }

    var numTribalUpgrades = creaturePopulationBonus;
    return Mathf.Max(stageRules.StageFiveRules.baseWavesPerPop - numFarms - numTribalUpgrades, 1);
  }
}

[System.Serializable]
public class SavedTower {
  public TdTowerType towerType {get; set;}
  public Int2 towerPosition {get; set;}
  public int towerLevel {get; set;}
  //Has this tower seen the glory of battle? If so they're worth less when selling
  public bool hasBeenUsed {get; set;}
}