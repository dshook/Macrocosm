using TMPro;
using UnityEngine;
using UnityEngine.UI;
using strange.extensions.mediation.impl;
using PygmyMonkey.ColorPalette;
using Unity.VectorGraphics;

public class GalaxyClock : View {
  [Inject] StageSevenDataModel stageSevenData {get; set;}
  [Inject] StringChanger stringChanger {get; set;}
  [Inject] TimeService time { get; set; }
  [Inject] StageTransitionEndSignal transitionEnd { get; set; }
  [Inject] StageRulesService stageRules { get; set; }
  [Inject] AudioService audioService { get; set; }

  public GameObject display;
  public SVGImage hand;
  public TMP_Text text;

  public Button toggleButton;
  public GameObject controlHolder;

  public Button pauseButton;
  public Button playButton;
  public Button ffButton;
  public Button fffButton;

  public AudioClip pauseClip;
  public AudioClip resumeClip;

  Color normalColor;
  Color highlightColor;

  protected override void Awake () {
    base.Awake();

    toggleButton.onClick.AddListener(OnToggleClick);
    pauseButton.onClick.AddListener(OnPauseClick);
    playButton.onClick.AddListener(OnPlayClick);
    ffButton.onClick.AddListener(OnFFClick);
    fffButton.onClick.AddListener(onFFFClick);

    normalColor = Color.white;
    highlightColor = ColorPaletteData.Singleton.fromName("Primary").getColorFromName("Gray 4").color;

    transitionEnd.AddListener((StageTransitionData data) => UpdateColors());

    UpdateColors();
  }

  void Update () {
    var truncated = (int)stageSevenData.year;
    stringChanger.UpdateString(text, "galaxyClock", truncated, "{0:#,0}", truncated);

    hand.transform.eulerAngles = new Vector3(0, 0, -360f * (stageSevenData.year - truncated));
  }

  void UpdateColors(){
    var rules = stageRules.StageSevenRules;

    pauseButton.targetGraphic.color = stageSevenData.timeRate == 0f                          ? highlightColor : normalColor;
    playButton.targetGraphic.color  = stageSevenData.timeRate == rules.baseYearTimeRate      ? highlightColor : normalColor;
    ffButton.targetGraphic.color    = stageSevenData.timeRate == rules.fastYearTimeRate      ? highlightColor : normalColor;
    fffButton.targetGraphic.color   = stageSevenData.timeRate == rules.extraFastYearTimeRate ? highlightColor : normalColor;

    if(stageSevenData.timeRate == 0f){
      toggleButton.targetGraphic.color = highlightColor;
      hand.color = highlightColor;
    }else{
      toggleButton.targetGraphic.color = normalColor;
      hand.color = normalColor;
    }
  }

  void OnToggleClick(){
    SetOpen(!controlHolder.activeInHierarchy);
  }

  void SetOpen(bool open){
    controlHolder.SetActive(open);
    UpdateColors();
  }

  void OnPauseClick(){
    stageSevenData.timeRate = 0f;

    audioService.PlaySfx(pauseClip);
    SetOpen(false);
  }

  void OnPlayClick(){
    stageSevenData.timeRate = stageRules.StageSevenRules.baseYearTimeRate;

    audioService.PlaySfx(resumeClip);
    SetOpen(false);
  }

  void OnFFClick(){
    stageSevenData.timeRate = stageRules.StageSevenRules.fastYearTimeRate;

    audioService.PlaySfx(resumeClip, 1.2f);
    SetOpen(false);
  }

  void onFFFClick(){
    stageSevenData.timeRate = stageRules.StageSevenRules.extraFastYearTimeRate;

    audioService.PlaySfx(resumeClip, 1.5f);
    SetOpen(false);
  }

  public void Show(){
    display.gameObject.SetActive(true);
  }

  public void Hide(){
    display.gameObject.SetActive(false);
  }
}
