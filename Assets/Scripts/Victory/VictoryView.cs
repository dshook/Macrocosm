using System;
using TMPro;
using UnityEngine;
using strange.extensions.mediation.impl;
using System.Collections.Generic;
using System.Linq;

public class VictoryView : View {
  [Inject] AudioService audioService { get; set; }

  public ShinyButton continueButton;
  public TMP_Text statsDescrip;


  public TMP_Text rankText;
  public TMP_Text timeText;


  public TMP_InputField subscribeEmailInput;
  public ShinyButton subscribeButton;

  public ParticleSystem fireworkParticles;
  public AudioClip fireworkPopClip;

  private int _currentNumberOfParticles = 0;

  protected override void Awake(){
    base.Awake();

    rankText.gameObject.SetActive(false);
    timeText.gameObject.SetActive(false);
  }

  void Update(){
    subscribeButton.interactable = !String.IsNullOrEmpty(subscribeEmailInput.text) && subscribeEmailInput.text.Contains("@");

    var amount = Mathf.Abs(_currentNumberOfParticles - fireworkParticles.particleCount);

    if (fireworkParticles.particleCount < _currentNumberOfParticles)
    {
      audioService.PlaySfx(fireworkPopClip, UnityEngine.Random.Range(0.9f, 1.1f));
    }

    _currentNumberOfParticles = fireworkParticles.particleCount;
  }

  public void SetHighScoreData(List<HighScoreData> highScores, string playerId){
    if(highScores == null || rankText == null || timeText == null){
      return;
    }

    rankText.text = "<u>Rank</u>\n" + string.Join("\n", highScores.Select(hs => hs.playerId == playerId ? $"<#{Colors.orangeText.ToHex()}>{hs.rank}</color>" : hs.rank));

    timeText.text = "<u>Time</u>\n" + string.Join("\n", highScores.Select(hs => {
      var span = new TimeSpan(hs.time.hours, hs.time.minutes, hs.time.seconds).ToString();
      return hs.playerId == playerId ? $"<#{Colors.orangeText.ToHex()}>{span}</color>" : span;
    }));

    rankText.gameObject.SetActive(true);
    timeText.gameObject.SetActive(true);
  }
}
