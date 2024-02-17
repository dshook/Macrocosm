
// Starts at a probability and then increases the probability each time you check
using UnityEngine;

//TODO if this becomes used more than once is to have a generic way to save the current probability
class RandomStepIncreaser {
  float initialProbability;
  float checkProbabilityIncreaseAmt;

  float prob;

  public float CurrentProbability{ get{ return prob; } }

  public RandomStepIncreaser(float initialProbability, float checkProbabilityIncreaseAmt){
    this.initialProbability = initialProbability;
    this.checkProbabilityIncreaseAmt = checkProbabilityIncreaseAmt;

    this.prob = initialProbability;
  }

  public void Reset(){
    prob = initialProbability;
  }

  //Only should be used to restore saved data
  public void SetProbability(float probability){
    prob = probability;
  }

  public bool Check(float probabilityModifier){
    if(Random.Range(0f, 1f) <= prob + probabilityModifier){
      Reset();
      return true;
    }else{
      prob += checkProbabilityIncreaseAmt;
      return false;
    }
  }
}