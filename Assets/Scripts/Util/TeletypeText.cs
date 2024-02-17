using System.Collections;
using UnityEngine;
using TMPro;


[RequireComponent(typeof(TMP_Text))]
public class TeletypeText : MonoBehaviour {

  [Range(0.001f, 1f)]
  public float characterRevealTime = 0.1f;

  TMP_Text textComp;
  bool running = false;
  float accum = 0f;

  void Start(){
    textComp = GetComponent<TMP_Text>();
  }

  void Update(){
    if(!running || textComp == null || textComp.textInfo == null){
      return;
    }

    var totalVisibleCharacters = textComp.textInfo.characterCount;
    accum += Time.unscaledDeltaTime;

    var visibleCharCount = Mathf.RoundToInt(
      Mathf.Min(totalVisibleCharacters, accum / characterRevealTime)
    );

    textComp.maxVisibleCharacters = visibleCharCount;

    if(visibleCharCount >= totalVisibleCharacters){
      Stop();
    }

  }

  public void Play(){
    running = true;
    accum = 0f;
    Update();
  }

  public void Stop(){
    running = false;

    textComp.maxVisibleCharacters = textComp.textInfo.characterCount;
  }
}