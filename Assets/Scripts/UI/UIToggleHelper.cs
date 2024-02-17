using UnityEngine;
using UnityEngine.UI;

//A bar with contents and faders that appear if the contents have too many items
[ExecuteInEditMode]
[RequireComponent(typeof(Toggle))]
public class UIToggleHelper : MonoBehaviour
{

  Toggle toggle;

  void Awake(){
    toggle = GetComponent<Toggle>();
  }

  //You had one job toggle
  public void Toggle(){
    toggle.isOn = !toggle.isOn;
  }

}