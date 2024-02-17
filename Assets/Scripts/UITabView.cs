using UnityEngine;
using UnityEngine.UI;

public class UITabView : MonoBehaviour {
  public Button[] tabButtons;
  public Graphic[] tabPanels;

  public Color focusColor;
  public Color blurColor;

  void Awake()
  {
    if(tabButtons != null && tabButtons.Length > 0){
      if(tabPanels == null || tabPanels.Length != tabButtons.Length){
        Debug.LogWarning("Must have same number of tab buttons and panels");
        return;
      }

      for(var b = 0; b < tabButtons.Length; b++){
        int idx = b;
        tabButtons[b].onClick.AddListener(() => FocusTab(idx));
        tabButtons[b].targetGraphic.color = b == 0 ? focusColor : blurColor;
      }
    }
  }

  void Update(){
  }

  public void FocusTab(int idx){
    for(var b = 0; b < tabPanels.Length; b++){
      tabButtons[b].targetGraphic.color = b == idx ? focusColor : blurColor;
      tabPanels[b].color = b == idx ? focusColor : blurColor;

      tabPanels[b].gameObject.SetActive(b == idx);
    }
  }

  public void HideTabs(){
    for(var b = 0; b < tabButtons.Length; b++){
      tabButtons[b].gameObject.SetActive(false);
    }
  }

}
