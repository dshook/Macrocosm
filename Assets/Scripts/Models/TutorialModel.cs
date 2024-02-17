using System.Collections.Generic;
using System;
using UnityEngine;

[System.Serializable]
[Singleton]
public class TutorialModel
{
  //which tutorials have been completed indexed by their child position in Tutorials GO
  //TODO: maybe make this less janky having a fixed size
  public Dictionary<int, bool> tutorialsCompleted = new Dictionary<int, bool>();

  public Dictionary<string, bool> popoutTutorialsCompleted = new Dictionary<string, bool>();

  public bool paused {get; set;}

  public int activeTutorialIdx = -1;
}