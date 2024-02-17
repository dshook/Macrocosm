using UnityEngine;
using UnityEngine.UI;

public class UISwitcher : MonoBehaviour {
  public GameObject trueObject;
  public GameObject falseObject;
  public bool condition;

  bool lastCondition = false;


  void Awake()
  {
    lastCondition = condition;
    UpdateObjects();
  }

  void Update(){
    if(lastCondition != condition){
      lastCondition = condition;

      UpdateObjects();
    }
  }

  void UpdateObjects(){
    trueObject.SetActive(condition);
    falseObject.SetActive(!condition);
  }

}
