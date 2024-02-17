using System;

[System.Serializable]
[Singleton]
public class StageTransitionModel
{
  public static int lastStage = 7;

  //the sub stage progression for each stage. Indexed by the actual stage number so the 0th element is always empty
  public int[] stageProgression = new int[lastStage + 1];

  //indexing same as above
  public bool[] stagesUnlocked = new bool[lastStage + 1];

  //ditto
  public int[] activeSubStage = new int[lastStage + 1];

  public int activeStage {get; set;}

  //Tracking for dev builds using EditorCheats
  public bool usedCheat = false;

}