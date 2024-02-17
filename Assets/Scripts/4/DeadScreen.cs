using strange.extensions.mediation.impl;
using UnityEngine;
using UnityEngine.UI;

public class DeadScreen : View {

  public GameObject outOfLives;
  public ShinyButton deadContinueButton;

  [Inject] StageFourDataModel dataModel { get; set; }

  void Update () {
    outOfLives.SetActive(dataModel.creatureLives == 0);

    deadContinueButton.interactable = dataModel.creatureLives > 0;

  }
}