using UnityEngine;
using UnityEngine.UI;
using strange.extensions.mediation.impl;
using TMPro;
using System.Collections;

public class StageThreeManager : View, IStageManager {

  public GameObject lifeAccumDisplay;
  public UIFilledBar lifeProgressBar;
  public TextMeshProUGUI comboText;
  public TextMeshProUGUI finishedText;
  public ShinyButton tryAgainButton;
  public ShinyButton nextSongButton;

  [Range(0f, 1f)]
  public float scoreMinimum = 0.7f;
  public BeatManager beatManager;

  [Inject] StageTransitionService stageTransition { get; set; }
  [Inject] StageTransitionModel stageData {get; set;}
  [Inject] StageRulesService stageRules { get; set; }
  [Inject] StageThreeDataModel stageThreeData { get; set; }
  [Inject] StageFourDataModel stageFourData { get; set; }
  [Inject] BeatHitSignal beatHitSignal { get; set; }
  [Inject] FloatingText floatingNumbers {get; set;}
  [Inject] SubStageUnlockedSignal subStageUnlockedSignal {get; set;}
  [Inject] BeatTypeUnlockedSignal beatTypeUnlockedSignal {get; set;}
  [Inject] StringChanger stringChanger {get; set;}
  [Inject] SongFinishedSignal songFinishedSignal {get; set;}
  [Inject] BeatTutorialFinishedSignal beatTutorialFinishedSignal { get; set; }
  [Inject] AudioService audioService { get; set; }

  public AudioClip beatHitClip;
  public AudioClip beatMissedClip;
  public AudioClip gainLifeClip;

  float hitScore = 1f;
  int numHits = 0;
  int numMisses = 0;

  float stageTimer = 0f;

  int comboCount = 0;
  Color scoreNumberColor;

  protected override void Awake () {
    base.Awake();

    beatHitSignal.AddListener(OnBeatHit);
    tryAgainButton.onClick.AddListener(ClickTryAgainButton);
    nextSongButton.onClick.AddListener(OnNextSongButtonClick);
    subStageUnlockedSignal.AddListener(OnSubStageUnlocked);
    songFinishedSignal.AddListener(OnSongFinished);
    beatTutorialFinishedSignal.AddListener(OnTutorialFinished);
    scoreNumberColor = Colors.UI.getColorFromName("DarkPurplePrimary").color;

  }

  protected override void OnDestroy(){
    beatHitSignal.RemoveListener(OnBeatHit);
    tryAgainButton.onClick.RemoveListener(ClickTryAgainButton);
    subStageUnlockedSignal.RemoveListener(OnSubStageUnlocked);
    songFinishedSignal.RemoveListener(OnSongFinished);
    beatTutorialFinishedSignal.RemoveListener(OnTutorialFinished);
  }

  public void Init(bool isInitialCall){
    HideFinishedScreen();

    stageTimer = 0f;
    hitScore = 1f;
    numHits = 0;
    numMisses = 0;

    comboCount = 0;
    beatManager.Init();
    var skipStartButton = !isInitialCall;
    beatManager.Play(skipStartButton);
  }

  public void OnTransitionTo(StageTransitionData data){
    beatManager.Play();

    //Hide finished screen if coming back from a stage.  Beat manager will resume
    if(finishedText.gameObject.activeInHierarchy){
      HideFinishedScreen();
    }
  }

  public void OnTransitionAway(bool toMenu){
    beatManager.Pause();
  }

  public void Cleanup(){
    beatManager.Cleanup();
  }

  void OnApplicationFocus(bool hasFocus)
  {
    //Pause when losing focus so all the beats don't spawn at once
    if(!hasFocus){
      beatManager.Pause();
    }else{
      if(beatManager.State == BeatManager.BeatManagerState.Paused){
        beatManager.Play();
      }
    }
  }

  void Update () {


    var inTutorial = beatManager.inTutorial;

    //pause timer during tutorials
    if(!inTutorial){
      stageTimer += Time.deltaTime;

    }

    if(gainingCreatureLives){
      lifeAccumDisplay.SetActive(true);
      stringChanger.UpdateString(lifeProgressBar.labelText, "StageThreeLives", stageThreeData.creatureLifeAccum, "{0:#,0}/{1:#,0}", stageThreeData.creatureLifeAccum, scoreForALife);
      lifeProgressBar.fillAmt = (float)stageThreeData.creatureLifeAccum / (float)scoreForALife;
    }else{
      lifeAccumDisplay.SetActive(false);
    }

    //TODO: maybe color the numbers based on how big they are?
    stringChanger.UpdateString(comboText, "StageThreeCombo", comboCount, "{0}x");

  }

  public void OnBeatHit(BeatHitData beatHit){
    if(beatHit.hit){
      audioService.PlaySfx(beatHitClip);
    }else{
      audioService.PlaySfx(beatMissedClip);
    }

    if( beatManager.inTutorial){ return; }

    if(beatHit.hit){
      numHits++;
      comboCount++;
      var isBonus = beatHit.beat.bonus;
      var bonusMultiplier = isBonus ? 2 : 1;
      var scoreIncrease = comboCount * beatHit.baseScore * bonusMultiplier;
      var color = isBonus ? Colors.golden : scoreNumberColor;
      var text = $"+{scoreIncrease}";

      floatingNumbers.Create(beatHit.beat.transform.position, color, text: text);

      if(gainingCreatureLives){
        stageThreeData.creatureLifeAccum += scoreIncrease;
      }

      if(stageThreeData.creatureLifeAccum >= scoreForALife){
        EarnALife();
      }
    }else{
      numMisses++;
      comboCount = 0;
    }

    hitScore = (float)numHits / (float)(numHits + numMisses);
  }

  int scoreForALife{
    get{
      var diff = stageRules.StageThreeRules.maxScoreForALife - stageRules.StageThreeRules.minScoreForALife;
      var perLifeIncrease = (float)diff / stageRules.StageThreeRules.livesTillMaxScoreForALife;
      return Mathf.Clamp(
        stageRules.StageThreeRules.minScoreForALife + Mathf.RoundToInt((float)stageThreeData.livesEarned * perLifeIncrease),
        stageRules.StageThreeRules.minScoreForALife,
        stageRules.StageThreeRules.maxScoreForALife
      );
    }
  }

  bool gainingCreatureLives{
    get{
      return stageData.stagesUnlocked.Length > 4 && stageData.stagesUnlocked[4];
    }
  }

  void EarnALife(){
    stageThreeData.creatureLifeAccum -= scoreForALife;
    stageFourData.creatureLives++;
    stageThreeData.livesEarned++;
    floatingNumbers.Create(new Vector3(5, -100, 0), parent: lifeAccumDisplay.transform, prefabPath: "Prefabs/3/GotAHeart");
    audioService.PlaySfx(gainLifeClip);
  }

  void OnSongFinished(SongTemplate templateData){
    //Check if we're done
    var metGoal = false;
    if(hitScore >= scoreMinimum){
      metGoal = true;
      var skipUnlock = false;
#if UNITY_EDITOR
      skipUnlock = beatManager.templateToTest != null;
#endif
      if(!skipUnlock){
        stageTransition.UnlockNextSubStage(stageData.activeStage);
      }

      stageThreeData.songIndex++;
      if(stageThreeData.songIndex >= beatManager.totalSongCount){
        //When looping back to the beginning song, skip the intro song
        stageThreeData.songIndex = 1;
      }
      nextSongButton.gameObject.SetActive(true);
    }else{
      tryAgainButton.gameObject.SetActive(true);
    }
    ShowFinishedText(metGoal, hitScore);

    //Reset combo here as well to fix the bug where going into the menu
    //and back on the finished screen keeps the combo
    comboCount = 0;
  }

  void OnNextSongButtonClick(){
    stageTransition.TransitionTo(stageData.activeStage);
  }

  void OnTutorialFinished(BeatTutorialData tutorialData){
    beatManager.Pause();
    //Move on to the next song/tutorial
    StartCoroutine(FinishTutorial());
  }

  IEnumerator FinishTutorial(){
    yield return new WaitForSeconds(1f);
    beatManager.Play();
  }

  void ShowFinishedText(bool metGoal, float hitScore){
    finishedText.gameObject.SetActive(true);

    string finishText;
    if(hitScore == 1){
      finishText = "100% Perfect!";
    }else if(hitScore > 0.95f){
      finishText = string.Format("{0:0%} Wow!", hitScore);
    }else if(hitScore > 0.9f){
      finishText = string.Format("{0:0%} Great Job!", hitScore);
    }else if(hitScore > 0.8f){
      finishText = string.Format("{0:0%} Nice!", hitScore);
    }else if(hitScore > 0.75f){
      finishText = string.Format("{0:0%} Decent!", hitScore);
    }else if(hitScore >= 0.70f){
      finishText = string.Format("{0:0%} Just Made It!", hitScore);
    }else if(hitScore > 0.60f){
      finishText = string.Format("{0:0%} So Close...", hitScore);
    }else{
      finishText = string.Format("{0:0%} Practice Makes Perfect", hitScore);
    }

    finishedText.text = finishText;

    finishedText.gameObject.transform.localScale = Vector3.zero;
    LeanTween.scale(finishedText.gameObject, Vector3.one, 1f).setEase(LeanTweenType.easeOutBack);
    LeanTween.rotate(finishedText.gameObject, new Vector3(0, 0, 728f), 1f).setEase(LeanTweenType.easeOutBack);
  }

  void HideFinishedScreen(){
    finishedText.gameObject.SetActive(false);
    tryAgainButton.gameObject.SetActive(false);
    nextSongButton.gameObject.SetActive(false);
  }

  void ClickTryAgainButton(){
    Init(false);
  }

  void OnSubStageUnlocked(StageUnlockedData data){
    if(data.stage == 2){
      //check to update the stage 3 cell unlock data
      foreach(var beatUnlockData in stageThreeData.beatData){
        if(
          data.subStage > stageRules.StageThreeRules.beatUnlockStage2Substage[beatUnlockData.Key]
          && !beatUnlockData.Value.bonus
        ){
          Debug.Log("Unlocked bonus for " + beatUnlockData.Key);
          beatUnlockData.Value.bonus = true;
          beatTypeUnlockedSignal.Dispatch(beatUnlockData.Key);
        }
      }
    }
  }

}
