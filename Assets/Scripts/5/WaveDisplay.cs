using System;
using UnityEngine;
using UnityEngine.UI;
using strange.extensions.mediation.impl;

using TMPro;

public delegate void ClickWaveIndicatorCb(int waveIndicator);

public class WaveDisplay : View {
  public GameObject waveIndicatorPrefab;
  [Inject] StageRulesService stageRules { get; set; }
  [Inject] StageFiveDataModel stageFiveData {get; set;}
  [Inject] MetaGameDataModel metaGameData {get; set;}
  [Inject] GameLoadedSignal gameLoadedSignal { get; set; }
  [Inject] ResourceLoaderService loader { get; set; }
  public ClickWaveIndicatorCb OnClickWaveIndicator;
  public Transform displayTransform;

  const int numWaveIndicators = 8;
  WaveIndicator[] waveIndicators = new WaveIndicator[numWaveIndicators];
  const float displayMargin = 6.5f;

  float indicatorWidth;
  float totalIndicatorWidth; //including margin
  float timeAccum;
  float waveTime;
  bool playing;
  int frontWaveIdx;

  //how much we need to move the indicators now to finish what's playing
  float destinationOffset = 0f;
  float accumulatedMovementWhilePlaying = 0f;

  protected override void Awake(){
    base.Awake();

    gameLoadedSignal.AddListener(OnLoad);
  }

  public void Init(){
    displayTransform.DestroyChildren();
    indicatorWidth = -1;
    timeAccum = 0f;
    waveTime = 0f;
    playing = false;
    frontWaveIdx = 0;
    destinationOffset = 0f;
    accumulatedMovementWhilePlaying = 0f;

    for(int i = 0; i < numWaveIndicators; i++){
      var newWaveIndicator = GameObject.Instantiate(
        waveIndicatorPrefab,
        Vector3.one,
        Quaternion.identity
      );
      newWaveIndicator.transform.SetParent(displayTransform, false);

      waveIndicators[i] = new WaveIndicator() {
        go = newWaveIndicator,
        rectTransform = newWaveIndicator.GetComponent<RectTransform>(),
        background = newWaveIndicator.GetComponent<Unity.VectorGraphics.SVGImage>(),
        button = newWaveIndicator.GetComponent<Button>(),
        waveNumberText = newWaveIndicator.transform.Find("WaveNumber").GetComponent<TMP_Text>(),
        waveTypeText = newWaveIndicator.transform.Find("WaveType").GetComponent<TMP_Text>(),
        waveNumber = stageFiveData.lastCompletedWave + i + 1,
      };

      if(indicatorWidth < 0){
        indicatorWidth = waveIndicators[i].rectTransform.sizeDelta.x;
        totalIndicatorWidth = indicatorWidth + displayMargin;
      }
      UpdateWaveIndicator(waveIndicators[i]);

      int waveIndIdx = i;
      waveIndicators[i].button.onClick.AddListener(() => ClickWaveIndicator(waveIndicators[waveIndIdx]));

      waveIndicators[i].rectTransform.anchoredPosition = new Vector2(i * (totalIndicatorWidth), 0);
    }

    playing = false;
  }

  void OnLoad(){
    Init();
  }

  void ClickWaveIndicator(WaveIndicator wi){
    if(OnClickWaveIndicator != null){
      OnClickWaveIndicator(wi.waveNumber);
    }
  }


  void UpdateWaveIndicator(WaveIndicator wi){
    var waveNumber = wi.waveNumber;
    var waveRules = stageRules.GetStageFiveRules(waveNumber, metaGameData.victoryCount);

    wi.waveNumberText.text = (waveNumber).ToString();
    wi.waveTypeText.text = waveRules.creepType.ToString();

    var creepType = waveRules.creepType;
    if(creepType == TdCreepType.Boss){
      creepType = TdCreep.GetBossSubType(waveNumber, metaGameData.victoryCount, stageRules);
    }
    var creepStats = loader.Load<TdCreepStats>(StageFiveManager.GetCreepStatsPath(creepType));
    wi.background.color = creepStats.color.DesaturateColor(0.1f);
  }

  void Update(){
    if(!playing) return;

    timeAccum += Time.deltaTime;

    // var positionChange = Time.deltaTime / waveTime * (totalIndicatorWidth);
    var positionChange = (totalIndicatorWidth / waveTime) * Time.deltaTime;
    //speed up when we have more than one indicator that needs to scroll by
    if(destinationOffset > totalIndicatorWidth){
      positionChange = 6f;
    }

    for(int i = 0; i < numWaveIndicators; i++){
      waveIndicators[i].rectTransform.anchoredPosition = waveIndicators[i].rectTransform.anchoredPosition3D.AddX(-positionChange);
    }
    destinationOffset -= positionChange;
    accumulatedMovementWhilePlaying += positionChange;

    //Once we have gone one indicator length, loop the indicator to the back
    if(accumulatedMovementWhilePlaying > totalIndicatorWidth){
      //let the first indicator be the last etc
      var lastWaveIdx = frontWaveIdx - 1 < 0 ? numWaveIndicators - 1 : frontWaveIdx - 1;
      var frontWaveIndicator = waveIndicators[frontWaveIdx];
      var lastWaveIndicator = waveIndicators[lastWaveIdx];

      frontWaveIndicator.waveNumber = lastWaveIndicator.waveNumber + 1;
      UpdateWaveIndicator(frontWaveIndicator);
      frontWaveIndicator.rectTransform.anchoredPosition = lastWaveIndicator.rectTransform.anchoredPosition.AddX(totalIndicatorWidth);

      frontWaveIdx = (frontWaveIdx + 1) % numWaveIndicators;
      accumulatedMovementWhilePlaying = 0f;
    }

    //Waves have stopped
    if(destinationOffset <= 0){
      playing = false;

    }
  }

  public void PlayNextWave(float waveTime){
    timeAccum = 0f;
    this.waveTime = waveTime;
    playing = true;
    destinationOffset += totalIndicatorWidth;
  }
}

class WaveIndicator{
  public GameObject go {get; set;}
  public RectTransform rectTransform {get; set;}
  public Unity.VectorGraphics.SVGImage background {get; set;}
  public Button button {get; set;}
  public TMP_Text waveNumberText {get; set;}
  public TMP_Text waveTypeText {get; set;}
  public int waveNumber {get; set;}
}