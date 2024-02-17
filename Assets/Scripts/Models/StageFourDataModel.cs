using System;
using System.Collections.Generic;

[System.Serializable]
[Singleton]
public class StageFourDataModel
{
  public int mapSeed;

  public int creatureLives {get; set;}

  //Mods that are passed from one generation to the next
  public List<CreatureModifierId> savedCreatureMods {get;set;}

  public List<CreatureModifierId> childrenBonusMods = new List<CreatureModifierId>();

  public CreatureData creatureData;

  public CreatureSceneData currentSceneData;

  public int sceneMoveCount = 0;

  //How many scenes have you moved across all your lives
  public int totalSceneMoveCount = 0;

  public int runsCompleted = 0;

  //How many total children you've successfully reared
  public int totalChildCount = 0;

  public int totalEnemiesEncountered = 0;

  public int totalMatesEncountered = 0;
  public int totalMatesSuccess = 0;

  public List<int> runLengths = new List<int>();
}