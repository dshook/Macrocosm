
using UnityEngine;

public interface IBeat{
  BeatManager beatManager {get; set;}

  BeatTemplateItem beatTemplateItem {get;}

  Transform transform {get;}

  bool bonus {get; set; }
}