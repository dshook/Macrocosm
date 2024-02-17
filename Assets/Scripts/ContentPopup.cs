using UnityEngine;

public class ContentPopup : MonoBehaviour {

  public RectTransform content;

  public CityPopupDisplay popupDisplay;

  void Update () {
  }

  //Event system click
  public void OnPopupClick(){
    Destroy(this.gameObject);
  }

}
