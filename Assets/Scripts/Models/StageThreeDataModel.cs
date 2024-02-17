using System;
using System.Collections.Generic;

[System.Serializable]
[Singleton]
public class StageThreeDataModel
{
  public int songIndex;

  public Dictionary<BeatType, BeatTypeData> beatData = new Dictionary<BeatType, BeatTypeData>(new BeatTypeComparer()){
    {BeatType.Single, new BeatTypeData()},
    {BeatType.Slide, new BeatTypeData()},
    {BeatType.Multi, new BeatTypeData()},
    {BeatType.SlideReverse, new BeatTypeData()},
    {BeatType.Double, new BeatTypeData()},
  };

  public int livesEarned;
  public int creatureLifeAccum = 0;
}


[System.Serializable]
public class BeatTypeData
{
  public bool bonus {get; set;}
  public bool completedTutorial {get; set;}
}

