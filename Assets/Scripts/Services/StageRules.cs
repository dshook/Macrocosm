using UnityEngine;
using System;
using System.Collections.Generic;

public class StageRules : StageRulesService
{
  [Inject] StageTransitionModel stageData {get; set;}
  [Inject] MetaGameDataModel metaGameData {get; set;}

  //This is a bit of a weird data structure but when looking at it by a stage number it tells
  //you what sub stage you must finish in the previous stage to unlock that stage
  public int[] stageUnlockData{ get{ return _stageUnlockData; } }
  readonly int[] _stageUnlockData = {0, 0, 2, 3, 3, 1, 1, 1};

  Dictionary <int, StageOneRulesProps>   stage1cache = new Dictionary<int, StageOneRulesProps>();
  Dictionary <int, StageTwoRulesProps>   stage2cache = new Dictionary<int, StageTwoRulesProps>();
  Dictionary <int, StageThreeRulesProps> stage3cache = new Dictionary<int, StageThreeRulesProps>();
  Dictionary <int, StageFourRulesProps>  stage4cache = new Dictionary<int, StageFourRulesProps>();
  Dictionary <int, StageFiveRulesProps>  stage5cache = new Dictionary<int, StageFiveRulesProps>();
  Dictionary <int, StageSixRulesProps>   stage6cache = new Dictionary<int, StageSixRulesProps>();
  Dictionary <int, StageSevenRulesProps> stage7cache = new Dictionary<int, StageSevenRulesProps>();

  int GetCacheKey(int subStageProgression, uint victoryCount){
    return 1000 * (int)victoryCount + subStageProgression;
  }


  //Have to deal with some BS to avoid allocations when using Func's
  public StageOneRulesProps GetStageOneRules(int subStageProgression, uint victoryCount){
    var cache = stage1cache;
    var key = GetCacheKey(subStageProgression, victoryCount);
    if(!cache.ContainsKey(key)){
      cache[key] = _GetStageOneRules(subStageProgression, victoryCount);
    }

    return cache[key];
  }
  public StageTwoRulesProps GetStageTwoRules(int subStageProgression, uint victoryCount){
    var cache = stage2cache;
    var key = GetCacheKey(subStageProgression, victoryCount);
    if(!cache.ContainsKey(key)){
      cache[key] = _GetStageTwoRules(subStageProgression, victoryCount);
    }

    return cache[key];
  }
  public StageThreeRulesProps GetStageThreeRules(int subStageProgression, uint victoryCount){
    var cache = stage3cache;
    var key = GetCacheKey(subStageProgression, victoryCount);
    if(!cache.ContainsKey(key)){
      cache[key] = _GetStageThreeRules(subStageProgression, victoryCount);
    }

    return cache[key];
  }
  public StageFourRulesProps GetStageFourRules(int subStageProgression, uint victoryCount){
    var cache = stage4cache;
    var key = GetCacheKey(subStageProgression, victoryCount);
    if(!cache.ContainsKey(key)){
      cache[key] = _GetStageFourRules(subStageProgression, victoryCount);
    }

    return cache[key];
  }
  public StageFiveRulesProps GetStageFiveRules(int subStageProgression, uint victoryCount){
    var cache = stage5cache;
    var key = GetCacheKey(subStageProgression, victoryCount);
    if(!cache.ContainsKey(key)){
      cache[key] = _GetStageFiveRules(subStageProgression, victoryCount);
    }

    return cache[key];
  }
  public StageSixRulesProps GetStageSixRules(int subStageProgression, uint victoryCount){
    var cache = stage6cache;
    var key = GetCacheKey(subStageProgression, victoryCount);
    if(!cache.ContainsKey(key)){
      cache[key] = _GetStageSixRules(subStageProgression, victoryCount);
    }

    return cache[key];
  }
  public StageSevenRulesProps GetStageSevenRules(int subStageProgression, uint victoryCount){
    var cache = stage7cache;
    var key = GetCacheKey(subStageProgression, victoryCount);
    if(!cache.ContainsKey(key)){
      cache[key] = _GetStageSevenRules(subStageProgression, victoryCount);
    }

    return cache[key];
  }

  public StageOneRulesProps StageOneRules
  {
    get{
      return GetStageOneRules(stageData.activeSubStage[1], metaGameData.victoryCount);
    }
  }

  public StageTwoRulesProps StageTwoRules
  {
    get {
      return GetStageTwoRules(stageData.activeSubStage[2], metaGameData.victoryCount);
    }
  }

  public StageThreeRulesProps StageThreeRules
  {
    get {
      return GetStageThreeRules(stageData.activeSubStage[3], metaGameData.victoryCount);
    }
  }

  public StageFourRulesProps StageFourRules
  {
    get {
      return GetStageFourRules(stageData.activeSubStage[4], metaGameData.victoryCount);
    }
  }

  public StageFiveRulesProps StageFiveRules
  {
    get {
      return GetStageFiveRules(stageData.activeSubStage[5], metaGameData.victoryCount);
    }
  }

  public StageSixRulesProps StageSixRules
  {
    get {
      return GetStageSixRules(stageData.activeSubStage[6], metaGameData.victoryCount);
    }
  }

  public StageSevenRulesProps StageSevenRules
  {
    get {
      return GetStageSevenRules(stageData.activeSubStage[7], metaGameData.victoryCount);
    }
  }


  StageOneRulesProps _GetStageOneRules(int subStageProgression, uint victoryCount){
    var additionalGoalNumber = (int)victoryCount;
    switch(subStageProgression){
      case 0:
        return new StageOneRulesProps{
          maxSize = 4,
          minSplitSize = 3,
          goalSize = 2,
          goalNumber = 10 + additionalGoalNumber,
          minSpawnSize = 1,
          maxSpawnSize = 1,
          startingAtomCount = 15,
          maxChargeUp = 0,
          shootCooldown = 0.4f,
        };
      case 1:
        return new StageOneRulesProps{
          maxSize = 8,
          minSplitSize = 6,
          goalSize = 4,
          goalNumber = 10 + additionalGoalNumber,
          minSpawnSize = 1,
          maxSpawnSize = 2,
          startingAtomCount = 20,
          maxChargeUp = 0,
          shootCooldown = 0.35f,
        };
      case 2:
        return new StageOneRulesProps{
          maxSize = 11,
          minSplitSize = 9,
          goalSize = 6,
          goalNumber = 10 + additionalGoalNumber,
          minSpawnSize = 1,
          maxSpawnSize = 3,
          startingAtomCount = 35,
          maxChargeUp = 0,
          shootCooldown = 0.30f,
        };
      case 3:
        return new StageOneRulesProps{
          maxSize = 14,
          minSplitSize = 11,
          goalSize = 7,
          goalNumber = 15 + additionalGoalNumber,
          minSpawnSize = 1,
          maxSpawnSize = 3,
          startingAtomCount = 40,
          minShakeupTime = 20f,
          maxShakeupTime = 40f,
          maxChargeUp = 1,
          shootCooldown = 0.25f,
        };
      case 4:
        return new StageOneRulesProps{
          maxSize = 15,
          minSplitSize = 12,
          goalSize = 8,
          goalNumber = 15 + additionalGoalNumber,
          minSpawnSize = 1,
          maxSpawnSize = 4,
          startingAtomCount = 45,
          minShakeupTime = 20f,
          maxShakeupTime = 40f,
          maxChargeUp = 2,
          shootCooldown = 0.2f,
        };
      case 5:
        return new StageOneRulesProps{
          maxSize = 16,
          minSplitSize = 13,
          goalSize = 9,
          goalNumber = 15 + additionalGoalNumber,
          minSpawnSize = 2,
          maxSpawnSize = 5,
          startingAtomCount = 50,
          minShakeupTime = 20f,
          maxShakeupTime = 40f,
          maxChargeUp = 2,
          shootCooldown = 0.15f,
        };
      default:
        var capSize = 26; //Iron
        var goalSize = Mathf.Min(subStageProgression + 4, capSize);
        return new StageOneRulesProps{
          maxSize = Mathf.Min(goalSize + 8, capSize + 1),
          minSplitSize = Mathf.Min(goalSize + 5, capSize + 1),
          goalSize = goalSize,
          goalNumber = 15 + additionalGoalNumber,
          minSpawnSize = Mathf.RoundToInt((float)goalSize / 4f),
          maxSpawnSize = Mathf.RoundToInt((float)goalSize / 2f),
          startingAtomCount = 50,
          minShakeupTime = 30f,
          maxShakeupTime = 50f,
          maxChargeUp = 3,
          shootCooldown = 0.15f,
        };
    }
  }

  StageTwoRulesProps _GetStageTwoRules(int subStageProgression, uint victoryCount){

    int goalAmount = 700;
    int additionalGoalAmount = (int)victoryCount * 100;

    int snakeStartLength = 0;
    float snakeRotationSpeed = 3.8f;
    float snakeSpeed = 2.3f;
    float boostAmount = 2.0f;
    float boostSpeed = 0.9f;

    //Start slow and ramp up a little each stage capping off at max force
    float baseAtomMoveForce = Mathf.Min(1.8f + (subStageProgression * 0.1f), 2.5f);

    const ushort minAtomSaturation = 5;
    int[] startingAtomCounts = null;

    int[] eatSequence;
    string moleculeName;

    int enemySnakeCount = 0;
    float enemySnakeSpeedPct = 0.9f;
    bool aggressiveSnakes = false;

    switch(subStageProgression){
      case 0:
        goalAmount = 200;
        snakeStartLength = 3;
        // snakeRotationSpeed = snakeRotationSpeed * 0.90f;
        snakeSpeed = snakeSpeed * 0.8f;
        boostAmount = 0;
        startingAtomCounts = new int[]{0, 0, 0, 0, 0, 0, 50 };
        eatSequence = new int[]{ 6 };
        moleculeName = "Carbon Chain";
        break;
      case 1:
        goalAmount = 200;
        // snakeRotationSpeed = snakeRotationSpeed * 0.95f;
        snakeSpeed = snakeSpeed * 0.85f;
        boostAmount = 0;
        startingAtomCounts = new int[]{0, 40, 0, 0, 0, 0, 0, 40 };
        eatSequence = new int[]{ 1, 1, 1, 7 };
        moleculeName = "Ammonia";
        break;
      case 2:
        goalAmount = 300;
        snakeSpeed = snakeSpeed * 0.9f;
        boostAmount = boostAmount * 0.5f;
        boostSpeed = boostSpeed * 0.8f;
        eatSequence = new int[]{ 8, 6, 8 };
        moleculeName = "Carbon Dioxide";
        break;
      case 3:
        goalAmount = 350;
        eatSequence = new int[]{ 1, 9, 1, 8, 1 };
        moleculeName = "Hydrofluoric Acid";
        boostAmount = boostAmount * 0.75f;
        boostSpeed = boostSpeed * 0.85f;
        enemySnakeCount = 1;
        enemySnakeSpeedPct = 0.80f;
        snakeStartLength = 1;
        break;
      case 4:
        goalAmount = 400;
        eatSequence = new int[]{ 11, 1, 8, 6, 8, 8};
        moleculeName = "Sodium Bicarbonate";
        boostAmount = boostAmount * 0.85f;
        boostSpeed = boostSpeed * 0.9f;
        snakeStartLength = 1;
        enemySnakeCount = 2;
        enemySnakeSpeedPct = 0.85f;
        break;
      case 5:
        goalAmount = 450;
        eatSequence = new int[]{ 12, 8, 1, 8, 1};
        moleculeName = "Magnesium Hydroxide";
        snakeStartLength = 1;
        enemySnakeCount = 3;
        break;
      case 6:
        goalAmount = 500;
        eatSequence = new int[]{ 13, 8, 11, 8 };
        moleculeName = "Sodium Aluminate";
        snakeStartLength = 1;
        enemySnakeCount = 3;
        aggressiveSnakes = true;
        break;
      case 7:
        goalAmount = 550;
        eatSequence = new int[]{ 7, 14, 7, 14, 7, 14, 7};
        moleculeName = "Silicon nitride";
        snakeStartLength = 1;
        enemySnakeCount = 4;
        aggressiveSnakes = true;
        break;
      case 8:
        goalAmount = 600;
        eatSequence = new int[]{8, 1, 15, 8, 1, 8, 1, 8 };
        moleculeName = "Phosphoric acid";
        snakeStartLength = 1;
        enemySnakeCount = 4;
        aggressiveSnakes = true;
        break;
      case 9:
        eatSequence = new int[]{16, 11, 16, 16, 11, 16};
        moleculeName = "Sodium sulfide";
        snakeStartLength = 1;
        enemySnakeCount = 4;
        aggressiveSnakes = true;
        break;
      case 10:
        eatSequence = new int[]{6, 1, 17, 17, 17};
        moleculeName = "Chloroform";
        snakeStartLength = 1;
        enemySnakeCount = 4;
        aggressiveSnakes = true;
        break;
      case 11:
        eatSequence = new int[]{6, 1, 6, 8, 1, 8, 1, 19};
        moleculeName = "Potassium Acetate";
        snakeStartLength = 1;
        enemySnakeCount = 4;
        aggressiveSnakes = true;
        break;
      case 12:
        eatSequence = new int[]{20, 6, 8, 8, 8};
        moleculeName = "Calcium Carbonate";
        snakeStartLength = 1;
        enemySnakeCount = 4;
        aggressiveSnakes = true;
        break;
      case 13:
        eatSequence = new int[]{8, 21, 8, 21, 8};
        moleculeName = "Scandium Oxide";
        snakeStartLength = 1;
        enemySnakeCount = 4;
        aggressiveSnakes = true;
        break;
      case 14:
        eatSequence = new int[]{8, 1, 8, 1, 22, 8 };
        moleculeName = "Titanic Acid";
        snakeStartLength = 1;
        enemySnakeCount = 4;
        aggressiveSnakes = true;
        break;
      case 15:
        eatSequence = new int[]{8, 8, 23, 8, 23, 8, 8};
        moleculeName = "Vanadium Pentoxide";
        snakeStartLength = 1;
        enemySnakeCount = 4;
        aggressiveSnakes = true;
        break;
      case 16:
        eatSequence = new int[]{17, 24, 17, 1, 8, 1};
        moleculeName = "Chromium Chloride";
        snakeStartLength = 1;
        enemySnakeCount = 4;
        aggressiveSnakes = true;
        break;
      case 17:
        eatSequence = new int[]{8, 8, 19, 25, 8, 8};
        moleculeName = "Potassium Permanganate";
        snakeStartLength = 1;
        enemySnakeCount = 4;
        aggressiveSnakes = true;
        break;
      case 18:
        eatSequence = new int[]{16, 26, 16, 26, 16, 26, 16};
        moleculeName = "Iron Sulfide";
        snakeStartLength = 1;
        enemySnakeCount = 4;
        aggressiveSnakes = true;
        break;
      case 19:
        goalAmount = 350;
        snakeStartLength = 1;
        eatSequence = new int[]{6, 1, 6, 1, 6, 7, 1, 1, 7, 6, 8, 7, 1};
        moleculeName = "Cytosine";
        enemySnakeCount = 4;
        aggressiveSnakes = true;
        break;
      case 20:
        goalAmount = 350;
        snakeStartLength = 1;
        eatSequence = new int[]{8, 6, 7, 1, 6, 1, 7, 1, 7, 6, 7, 1, 6, 1, 7, 6};
        moleculeName = "Guanine";
        enemySnakeCount = 4;
        aggressiveSnakes = true;
        break;
      case 21:
        goalAmount = 350;
        snakeStartLength = 1;
        eatSequence = new int[]{6, 6, 1, 7, 1, 7, 6, 1, 7, 6, 7, 1, 6, 1, 7};
        moleculeName = "Adenine";
        enemySnakeCount = 4;
        aggressiveSnakes = true;
        break;
      default:
        goalAmount = 350;
        snakeStartLength = 1;
        eatSequence = new int[]{6, 8, 7, 1, 6, 8, 7, 1, 6, 1, 6, 1, 6, 1, 1};
        moleculeName = "Thymine";
        enemySnakeCount = 4;
        aggressiveSnakes = true;
        break;

    }


    return new StageTwoRulesProps{
      goalAmount = goalAmount + additionalGoalAmount,
      snakeStartLength = snakeStartLength,
      snakeRotationSpeed = snakeRotationSpeed,
      snakeSpeed = snakeSpeed,
      boostAmount = boostAmount,
      boostSpeed = boostSpeed,
      baseAtomMoveForce = baseAtomMoveForce,
      minAtomSaturation = minAtomSaturation,
      startingAtomCounts = startingAtomCounts,
      eatSequence = eatSequence,
      moleculeName = moleculeName,
      enemySnakeCount = enemySnakeCount,
      enemySnakeSpeedPct = enemySnakeSpeedPct,
      aggressiveSnakes = aggressiveSnakes
    };
  }

  StageThreeRulesProps _GetStageThreeRules(int subStageProgression, uint victoryCount){
    var additionalStages = (int)victoryCount;
    Dictionary<BeatType, int> stage2Unlock = new Dictionary<BeatType, int>(){
      {BeatType.Single, 4 + additionalStages},
      {BeatType.Double, 6 + additionalStages},
      {BeatType.Slide, 8 + additionalStages},
      {BeatType.Multi, 10 + additionalStages},
      {BeatType.SlideReverse, 12 + additionalStages},
    };

    var additionalMinMax = 1000 * (int)victoryCount;
    var rules = new StageThreeRulesProps{
      minScoreForALife = 3000 + additionalMinMax,
      maxScoreForALife = 60000 + additionalMinMax,
      livesTillMaxScoreForALife = 45,
      beatUnlockStage2Substage = stage2Unlock,
    };

    return rules;
  }

  StageFourRulesProps _GetStageFourRules(int subStageProgression, uint victoryCount){
    var additionalMinMateSteps = (int)victoryCount;
    var additionalIncubationSteps = (int)victoryCount;
    switch(subStageProgression){
      default:
        return new StageFourRulesProps{
          creatureUpgradeInterval = 7,
          enemyUpgradeInterval = 2.8f,
          enemyProbThreshold = 0.5f,
          mateInitialProbability = 0.20f,
          mateStepIncreaseProbability = 0.11f,
          minMateSteps = 9 + additionalMinMateSteps,
          childIncubationSteps = 4 + additionalIncubationSteps,
          childFoodPerStep = 8,
          maxSteps = 65,
          baseWheelSpeed = 150 + ((int)victoryCount * 50),
          persistentModsPerRunsCompleted = 1f / (4f + victoryCount)
        };
    }
  }

  StageFiveRulesProps _GetStageFiveRules(int subStageProgression, uint victoryCount){
    const int baseWavesPerPop = 15;
    const int waveToUnlockStageSix = 21;
    var additionalCreepsPerWave = 2 * (int)victoryCount;

    const int recruitMoneyCost = 500;
    const int recruitWoodCost = 20;
    const int recruitOreCost = 10;

    const int additionalRecruitMoneyCost = 50;
    const int additionalRecruitWoodCost = 2;
    const int additionalRecruitOreCost = 1;
    // return new StageFiveRulesProps{
    //   creepType = TdCreepType.Spawn,
    //   perCreepDelay = 0.35f,
    //   creepsPerSpawnPoint = 3
    // };
    //Loop through the number of creep types
    switch(subStageProgression % 7){
      case 1:
        return new StageFiveRulesProps{
          creepType = TdCreepType.Normal,
          baseWavesPerPop = baseWavesPerPop,
          waveToUnlockStageSix = waveToUnlockStageSix,
          additionalCreepsPerWave = additionalCreepsPerWave,
          recruitMoneyCost = recruitMoneyCost,
          recruitWoodCost = recruitWoodCost,
          recruitOreCost = recruitOreCost,
          additionalRecruitMoneyCost = additionalRecruitMoneyCost,
          additionalRecruitWoodCost = additionalRecruitWoodCost,
          additionalRecruitOreCost = additionalRecruitOreCost,
        };
      case 2:
        return new StageFiveRulesProps{
          creepType = TdCreepType.Fast,
          baseWavesPerPop = baseWavesPerPop,
          waveToUnlockStageSix = waveToUnlockStageSix,
          additionalCreepsPerWave = additionalCreepsPerWave,
          recruitMoneyCost = recruitMoneyCost,
          recruitWoodCost = recruitWoodCost,
          recruitOreCost = recruitOreCost,
          additionalRecruitMoneyCost = additionalRecruitMoneyCost,
          additionalRecruitWoodCost = additionalRecruitWoodCost,
          additionalRecruitOreCost = additionalRecruitOreCost,
        };
      case 3:
        return new StageFiveRulesProps{
          creepType = TdCreepType.Group,
          baseWavesPerPop = baseWavesPerPop,
          waveToUnlockStageSix = waveToUnlockStageSix,
          additionalCreepsPerWave = additionalCreepsPerWave,
          recruitMoneyCost = recruitMoneyCost,
          recruitWoodCost = recruitWoodCost,
          recruitOreCost = recruitOreCost,
          additionalRecruitMoneyCost = additionalRecruitMoneyCost,
          additionalRecruitWoodCost = additionalRecruitWoodCost,
          additionalRecruitOreCost = additionalRecruitOreCost,
        };
      case 4:
        return new StageFiveRulesProps{
          creepType = TdCreepType.Immune,
          baseWavesPerPop = baseWavesPerPop,
          waveToUnlockStageSix = waveToUnlockStageSix,
          additionalCreepsPerWave = additionalCreepsPerWave,
          recruitMoneyCost = recruitMoneyCost,
          recruitWoodCost = recruitWoodCost,
          recruitOreCost = recruitOreCost,
          additionalRecruitMoneyCost = additionalRecruitMoneyCost,
          additionalRecruitWoodCost = additionalRecruitWoodCost,
          additionalRecruitOreCost = additionalRecruitOreCost,
        };
      case 5:
        return new StageFiveRulesProps{
          creepType = TdCreepType.Spawn,
          baseWavesPerPop = baseWavesPerPop,
          waveToUnlockStageSix = waveToUnlockStageSix,
          additionalCreepsPerWave = additionalCreepsPerWave,
          recruitMoneyCost = recruitMoneyCost,
          recruitWoodCost = recruitWoodCost,
          recruitOreCost = recruitOreCost,
          additionalRecruitMoneyCost = additionalRecruitMoneyCost,
          additionalRecruitWoodCost = additionalRecruitWoodCost,
          additionalRecruitOreCost = additionalRecruitOreCost,
        };
      case 6:
        return new StageFiveRulesProps{
          creepType = TdCreepType.Flying,
          baseWavesPerPop = baseWavesPerPop,
          waveToUnlockStageSix = waveToUnlockStageSix,
          additionalCreepsPerWave = additionalCreepsPerWave,
          recruitMoneyCost = recruitMoneyCost,
          recruitWoodCost = recruitWoodCost,
          recruitOreCost = recruitOreCost,
          additionalRecruitMoneyCost = additionalRecruitMoneyCost,
          additionalRecruitWoodCost = additionalRecruitWoodCost,
          additionalRecruitOreCost = additionalRecruitOreCost,
        };
      case 0:
        return new StageFiveRulesProps{
          creepType = TdCreepType.Boss,
          baseWavesPerPop = baseWavesPerPop,
          waveToUnlockStageSix = waveToUnlockStageSix,
          additionalCreepsPerWave = (int)victoryCount,
          recruitMoneyCost = recruitMoneyCost,
          recruitWoodCost = recruitWoodCost,
          recruitOreCost = recruitOreCost,
          additionalRecruitMoneyCost = additionalRecruitMoneyCost,
          additionalRecruitWoodCost = additionalRecruitWoodCost,
          additionalRecruitOreCost = additionalRecruitOreCost,
        };
    }

    //Shouldn't ever hit this
    return new StageFiveRulesProps{};
  }

  StageSixRulesProps _GetStageSixRules(int subStageProgression, uint victoryCount){
    return new StageSixRulesProps{
      startingMapSeed = 1621382297,
      baseScoutRadius = 4,
      baseScoutMovePoints = 20,
      foodInCellFeedsPopCount = 6f,
      baseCityProductionRate = 0.1f,
      baseCityScienceRate = 0.1f,
      baseSettlerRadius = 6,
      additionalTechLevel = 0.33f * (int)victoryCount,
      idleScienceRate = 0.2f
    };
  }

  StageSevenRulesProps _GetStageSevenRules(int subStageProgression, uint victoryCount){
    const float baseYearTimeRate = 0.25f;
    return new StageSevenRulesProps{
      startingMapSeed = 715827492,
      basePopGrowthRate = 1.05f,

      baseYearTimeRate = baseYearTimeRate,
      fastYearTimeRate = baseYearTimeRate * 10f,
      extraFastYearTimeRate = baseYearTimeRate * 25f,

      additionalResourceCostMultiplier = 1 + (int)victoryCount,
    };
  }

}