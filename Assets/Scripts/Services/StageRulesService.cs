using System;
using System.Collections.Generic;

public interface StageRulesService{
  int[] stageUnlockData {get;}

  StageOneRulesProps GetStageOneRules(int subStageProgression, uint victoryCount);
  StageOneRulesProps StageOneRules {get;}

  StageTwoRulesProps GetStageTwoRules(int subStageProgression, uint victoryCount);
  StageTwoRulesProps StageTwoRules {get;}

  StageThreeRulesProps GetStageThreeRules(int subStageProgression, uint victoryCount);
  StageThreeRulesProps StageThreeRules {get;}

  StageFourRulesProps GetStageFourRules(int subStageProgression, uint victoryCount);
  StageFourRulesProps StageFourRules {get;}

  StageFiveRulesProps GetStageFiveRules(int subStageProgression, uint victoryCount);
  StageFiveRulesProps StageFiveRules {get;}

  StageSixRulesProps GetStageSixRules(int subStageProgression, uint victoryCount);
  StageSixRulesProps StageSixRules {get;}

  StageSevenRulesProps GetStageSevenRules(int subStageProgression, uint victoryCount);
  StageSevenRulesProps StageSevenRules {get;}
}

public struct StageOneRulesProps{
  public int maxSize;
  public int minSplitSize;
  public int goalSize;
  public int goalNumber;

  public int minSpawnSize;
  public int maxSpawnSize;

  public int startingAtomCount;

  public float minShakeupTime;
  public float maxShakeupTime;

  public float shootCooldown;

  public int maxChargeUp;
}

public struct StageTwoRulesProps{
  public int goalAmount;

  //Default to the eat sequence length, but can change it with this
  public int snakeStartLength;
  public float snakeSpeed;
  public float snakeRotationSpeed;

  public float boostAmount;
  public float boostSpeed;

  public float baseAtomMoveForce;

  public ushort minAtomSaturation;
  public int[] startingAtomCounts;

  //what size of atoms to eat
  public int[] eatSequence;
  public string moleculeName;

  public int enemySnakeCount;
  public float enemySnakeSpeedPct;
  public bool aggressiveSnakes;
}

public struct StageThreeRulesProps{
  public int minScoreForALife;
  public int maxScoreForALife;
  public int livesTillMaxScoreForALife;
  public Dictionary<BeatType, int> beatUnlockStage2Substage;
}

public struct StageFourRulesProps{
  public int creatureUpgradeInterval;
  public float enemyUpgradeInterval;
  public float enemyProbThreshold;
  public float mateInitialProbability;
  public float mateStepIncreaseProbability;
  public int minMateSteps;
  public int childIncubationSteps;
  public int childFoodPerStep;
  public int maxSteps;
  public int baseWheelSpeed;

  //How many persistent mods we should give to the player per run completed
  //to help get players unstuck when they're not reproducing early on
  public float persistentModsPerRunsCompleted;
}

public struct StageFiveRulesProps{
  public int waveToUnlockStageSix;
  public TdCreepType creepType;
  public int baseWavesPerPop;

  //For meta progression
  public int additionalCreepsPerWave;

  public int recruitMoneyCost;
  public int recruitWoodCost;
  public int recruitOreCost;

  //Amount to add for each recruit after the first
  public int additionalRecruitMoneyCost;
  public int additionalRecruitWoodCost;
  public int additionalRecruitOreCost;
}


public struct StageSixRulesProps{
  public int startingMapSeed;
  public int baseScoutRadius;
  public int baseScoutMovePoints;
  public float foodInCellFeedsPopCount;
  public float baseCityProductionRate;
  public float baseCityScienceRate;
  public int baseSettlerRadius;

  public float additionalTechLevel;

  // How fast % does science advance when playing other stages
  public float idleScienceRate;
}


public struct StageSevenRulesProps{
  public int startingMapSeed;
  public float basePopGrowthRate;

  public float baseYearTimeRate;
  public float fastYearTimeRate;
  public float extraFastYearTimeRate;

  public int additionalResourceCostMultiplier;
}
