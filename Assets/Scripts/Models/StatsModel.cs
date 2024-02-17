using System;
using System.Collections.Generic;

[System.Serializable]
[Singleton]
public class StatsModel
{
  public float totalPlayTime = 0;

  //Reset every time the game boots up
  public float sessionTime = 0;

  //track how much time is spent in each stage
  public float[] stageTime = new float[StageTransitionModel.lastStage + 1];

  //how many touches on the screen
  public uint[] tapCount = new uint[StageTransitionModel.lastStage + 1];

  //total playtime when the stage was unlocked
  public float[] stageUnlockedTime = new float[StageTransitionModel.lastStage + 1];

  //times learn more was used
  public uint[] learnMoreCount = new uint[StageTransitionModel.lastStage + 1];

  public List<string> gameStartDates = new List<string>();

  public List<int> sessionStageSwitches = new List<int>();

  //TODO: session times
}