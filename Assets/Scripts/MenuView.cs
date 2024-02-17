using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using strange.extensions.mediation.impl;
using System.Collections;
using MoreMountains.NiceVibrations;

public class MenuView : View {

  public GameObject display;
  public ShinyButton closeButton;
  public ShinyButton openButton;

  public ShinyButton learnMoreButton;
  public ShinyButton creditsButton;

  public GameObject creditsPrefab;
  public Transform creditsHolder;
  private CreditsView creditsView;

  public Switch tutorialEnabledToggle;
  public Switch hapticsEnabledToggle;
  public Switch highQualityToggle;

  public Slider musicVolumeSlider;
  public Slider sfxVolumeSlider;

  public GameObject hapticsRow;
  public TMP_Text versionText;

  [Inject] TimeService time { get; set; }
  [Inject] SettingsDataModel settings { get; set; }
  [Inject] GameLoadedSignal gameLoadedSignal { get; set; }
  [Inject] MenuToggledSignal menuToggleSignal {get; set;}
  [Inject] ToggleMenuVisibilitySignal toggleMenuVisibilitySignal {get; set;}
  [Inject] StageTransitionService stageTransitionService {get; set;}
  [Inject] LearningMoreSignal learningMore {get; set;}
  [Inject] GameSaverService gameSaver {get; set;}


  protected override void Awake () {
    base.Awake();

    openButton.onClick.AddListener(Toggle);
    closeButton.onClick.AddListener(Toggle);
    toggleMenuVisibilitySignal.AddListener(Toggle);
    learnMoreButton.onClick.AddListener(ClickLearnMore);
    creditsButton.onClick.AddListener(ClickCredits);
    tutorialEnabledToggle.onValueChanged.AddListener(ToggleTutorialEnabled);
    hapticsEnabledToggle.onValueChanged.AddListener(ToggleHapticsEnabled);
    highQualityToggle.onValueChanged.AddListener(ToggleQuality);

    gameLoadedSignal.AddListener(OnLoad);
  }

  void OnLoad(){
    //gotta flippy floppy so that we can use the default false bool value to not disable tutorials initially
    //but present the player an easier to understand option
    tutorialEnabledToggle.isOn = !settings.tutorialsDisabled;
    hapticsEnabledToggle.isOn = !settings.hapticsDisabled;
    highQualityToggle.isOn = !settings.lowQuality;
    UpdateQualityLevel();

    musicVolumeSlider.value = settings.musicVolume;
    sfxVolumeSlider.value = settings.sfxVolume;

    //Only show the option if its supported, and restore setting
    hapticsRow.SetActive(MMVibrationManager.HapticsSupported());
    MMVibrationManager.SetHapticsActive(hapticsEnabledToggle.isOn);

    versionText.text = gameSaver.GetVersionNumber();
  }

  void Update () {
    settings.musicVolume = Mathf.Clamp01(musicVolumeSlider.value);
    settings.sfxVolume = Mathf.Clamp01(sfxVolumeSlider.value);
  }

  void Toggle(){
    var newState = !display.activeSelf;
    Debug.Log(newState ? "Opening Menu" : "Closing Menu");

    //Dispatch the signal then wait till end of frame so the user report can grab the screenshot before the menu is open
    menuToggleSignal.Dispatch(newState);
    StartCoroutine(DoTheWork(newState));
  }

  IEnumerator DoTheWork(Boolean newState){
    yield return new WaitForEndOfFrame();
    display.SetActive(newState);
    if(display.activeSelf){
      //Disable the active stage manager so it's not running while the menu is open
      stageTransitionService.TransitionToMenu();
      time.Pause();
    }else{
      //And reenable
      stageTransitionService.TransitionFromMenu();
      time.Resume();
    }
  }

  void ToggleTutorialEnabled(bool newState){
    settings.tutorialsDisabled = !newState;
  }

  void ToggleHapticsEnabled(bool newState){
    settings.hapticsDisabled = !newState;
    MMVibrationManager.SetHapticsActive(newState);
  }

  void ToggleQuality(bool newState){
    settings.lowQuality = !newState;
    UpdateQualityLevel();
  }

  void UpdateQualityLevel(){
    QualitySettings.SetQualityLevel(settings.lowQuality ? 0 : 1, true);
  }

  void ClickLearnMore(){
    learningMore.Dispatch();
    var possibleLinks = LearnMoreModel.learnMoreLinks[stageTransitionService.stageData.activeStage];
    var chosenLink = possibleLinks[UnityEngine.Random.Range(0, possibleLinks.Length)];
    Application.OpenURL(chosenLink);
  }

  void ClickCredits(){
    var creditsGO = GameObject.Instantiate(creditsPrefab, creditsHolder);
    creditsView = creditsGO.GetComponent<CreditsView>();

    creditsView.onClose.AddListener(CloseCredits);
    Debug.Log("Showing Credits");
  }

  void CloseCredits(){
    creditsView.closeButton.onClick.RemoveListener(CloseCredits);

    GameObject.Destroy(creditsView.gameObject);
    Debug.Log("Closing Credits");
  }
}
