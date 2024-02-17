using UnityEngine;
using TMPro;

public class WarningText : MonoBehaviour {
  public TMP_Text warningText;
  public ColorPulser warningPulse;

  bool showing = false;
  const float showTime = 3f;

  float showingTime = 0f;

  void Update(){
    if(!showing){
      return;
    }

    showingTime += Time.deltaTime;

    if(showingTime > showTime){
      Hide();
    }
  }

  public void Show(string text){
    showing = true;
    showingTime = 0f;
    warningText.text = text;
    warningText.gameObject.SetActive(true);
    warningPulse.Play();
  }

  public void Hide(){
    warningText.gameObject.SetActive(false);
    warningPulse.Pause();
    showing = false;
  }

}