using strange.extensions.mediation.impl;
using UnityEngine;

public class ElementSaturationWatcher : View {
  [Inject] StageTwoElementNeededSignal elementNeededSignal {get; set;}
  [Inject] StageRulesService stageRules { get; set; }
  [Inject] StageTwoDataModel stageTwoData { get; set; }

  public Snake snake;

  float maxStruggleTime = 10f;
  float minSaturation = 15;
  float timer = 0f;
  bool fired = false;

  int prevElementNeeded = -1;

  void Update () {

    var elementNeeded = stageRules.StageTwoRules.eatSequence[snake.eatSequenceIndex];

    //Reset if snake has eaten
    if(elementNeeded != prevElementNeeded){
      prevElementNeeded = elementNeeded;
      timer = 0f;
    }

    timer += Time.deltaTime;

    if(!fired && timer > maxStruggleTime && stageTwoData.atomSaturation[elementNeeded] < minSaturation){
      elementNeededSignal.Dispatch(elementNeeded);
      //Just to prevent over signalling even though tutorial shouldn't come up more than once
      fired = true;
    }
  }


}
