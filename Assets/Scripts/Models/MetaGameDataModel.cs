using System;
using System.Collections.Generic;

[System.Serializable]
[Singleton]
public class MetaGameDataModel
{
  public uint victoryCount = 0;

  public List<VictoryData> victoryData = new List<VictoryData>();
}


[Serializable]
public class VictoryData {
  //Total play time for this victory
  public float playTime = 0;

  public string victoryDateTime;

  public float stage7year;

  public bool usedCheat = false;

  //track how much time is spent in each stage
  public float[] stageTime = new float[StageTransitionModel.lastStage + 1];

  //how many touches on the screen
  public uint[] tapCount = new uint[StageTransitionModel.lastStage + 1];

  //total playtime when the stage was unlocked
  public float[] stageUnlockedTime = new float[StageTransitionModel.lastStage + 1];
}