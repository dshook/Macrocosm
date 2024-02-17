using TMPro;
using UnityEngine;
using strange.extensions.mediation.impl;

public class VictoryCrawlView : View {
  [Inject] StageSevenDataModel stageSevenData { get; set; }
  [Inject] MetaGameDataModel metagameData { get; set; }

  public float buttonRevealTime = 15f;
  float accum = 0f;

  public TMP_Text victoryText;
  public ShinyButton continueButton;
  public TeletypeText teleText;

  new void Awake(){
    base.Awake();

    continueButton.gameObject.SetActive(false);

    victoryText.text = (VictoryText.VictoryTexts[metagameData.victoryCount - 1])
      .Replace("<>", string.Format("{0:#,0}", stageSevenData.year));

    teleText.Play();
  }

  void Update(){

    accum += Time.unscaledDeltaTime;

    if(accum >= buttonRevealTime){
      continueButton.gameObject.SetActive(true);
    }
  }
}
