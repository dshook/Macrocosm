using System;
using System.Collections.Generic;

[System.Serializable]
[Singleton]
public class StageTwoDataModel
{
  //store 0-100 for percentages of each atom size
  public ushort[] atomSaturation = new ushort[30];

  public int scoreAccum = 0;

  //current state of snake
  public List<int> snakeAtoms = new List<int>();

  public int eatSequenceIndex = 0;
}