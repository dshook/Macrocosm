using UnityEngine;
using strange.extensions.mediation.impl;
using System.Linq;
using System;
using System.Net.Http;
using UnityEngine.Networking;
using System.Collections.Generic;
using Unity.Cloud.UserReporting.Plugin.SimpleJson;

public class VictoryManager : View {
  [Inject] VictorySignal victorySignal {get; set;}
  [Inject] TimeService time { get; set; }

  [Inject] StageSevenDataModel stageSevenData { get; set; }
  [Inject] StageTransitionModel stageData { get; set; }
  [Inject] MetaGameDataModel metagameData { get; set; }
  [Inject] StatsModel statsData { get; set; }
  [Inject] GameSaverService gameSaverService { get; set; }
  [Inject] StageTransitionService stageTransition { get; set; }
  [Inject] AudioService audioService { get; set; }
  [Inject] CameraService cameraService {get; set;}
  [Inject] CreateHighScoreSignal highScoreSignal {get; set;}
  [Inject] UserReportSubmittedSignal reportSubmittedSignal {get; set;}

  public GameObject victoryCrawlPrefab;
  public GameObject victoryDisplayPrefab;
  public GameObject creditsPrefab;
  public GameObject highScoreDisplayPrefab;

  public Transform victoryHolder;
  private CreditsView creditsView;

  private VictoryCrawlView victoryCrawlView;
  private VictoryView victoryView;

  public AudioClip victoryClip;

  protected override void Awake () {
    base.Awake();
    victorySignal.AddListener(OnVictory);
    reportSubmittedSignal.AddListener(OnReportSubmitted);
  }

  void OnVictory(bool isNewVictory){
    if(!time.Paused){
      time.Pause();
    }

    if(isNewVictory){
      UpdateVictoryGameData();
    }

    CreateVictoryCrawlView();

    //Getting a reserved source here to avoid the pitch shifting messing with the victory sound from the fireworks
    var audioSource = audioService.GetReservedSfxSource();
    audioService.PlaySfx(victoryClip, 1, audioSource);
  }


  void CreateVictoryCrawlView(){
    victoryCrawlView = GameObject.Instantiate(victoryCrawlPrefab, victoryHolder).GetComponent<VictoryCrawlView>();
    victoryCrawlView.continueButton.onClick.AddListener(ClickCrawlContinue);
  }

  void CreateVictoryView(){
    cameraService.ResetPositionAndSize();

    victoryView = GameObject.Instantiate(victoryDisplayPrefab, victoryHolder).GetComponent<VictoryView>();
    victoryView.continueButton.onClick.AddListener(ClickVictoryContinue);
    victoryView.subscribeButton.onClick.AddListener(ClickSubscribe);

    var victoryData = metagameData.victoryData.Last();

    var playTimeSpan = TimeSpan.FromSeconds(victoryData.playTime);
    var taps = victoryData.tapCount.Sum(x => x);
    victoryView.statsDescrip.text = $"You achieved your {metagameData.victoryCount.OrdinalFormat()} victory in {FormatTimeSpan(playTimeSpan)}!";
    victoryView.statsDescrip.text += $"<br>It took {string.Format("{0:#,0}", taps)} taps and {string.Format("{0:#,0}", victoryData.stage7year)} galactic years for your civilization to achieve Ascension.";

    highScoreSignal.Dispatch(new VictoryHighScore(){
      victoryCount = (int)metagameData.victoryCount,
      playTimeSeconds = victoryData.playTime
    });
  }

  void ClickCrawlContinue(){
    CreateVictoryView();

    victoryCrawlView.continueButton.onClick.RemoveListener(ClickCrawlContinue);
    GameObject.Destroy(victoryCrawlView.gameObject);
  }

  void ClickVictoryContinue(){
    Debug.Log("Yaaaay! Victory!");
    gameSaverService.ResetDataForVictory();
    stageTransition.ResetAllStages();
    gameSaverService.Save();

    victoryView.continueButton.onClick.RemoveListener(ClickVictoryContinue);
    victoryView.subscribeButton.onClick.RemoveListener(ClickSubscribe);
    GameObject.Destroy(victoryView.gameObject);

    OpenCredits();
  }

  void UpdateVictoryGameData(){

    metagameData.victoryCount++;
    metagameData.victoryData.Add(new VictoryData(){
      //The the for this victory is the overall total play time minus any other victories play times
      playTime = statsData.totalPlayTime - metagameData.victoryData.Sum(vd => vd.playTime),
      victoryDateTime = DateTime.UtcNow.ToString("o"),
      stage7year = stageSevenData.year,
      usedCheat = stageData.usedCheat,
      //Copy stats arrays
      stageTime = statsData.stageTime.ToArray(),
      tapCount = statsData.tapCount.ToArray(),
      stageUnlockedTime = statsData.stageUnlockedTime.ToArray(),
    });
  }

  string FormatTimeSpan(TimeSpan ts){
    return string.Format("{0:0} hours and {1} minutes", ts.TotalHours, ts.Minutes);
  }


  void OpenCredits(){
    var creditsGO = GameObject.Instantiate(creditsPrefab, victoryHolder);
    creditsView = creditsGO.GetComponent<CreditsView>();

    creditsView.onClose.AddListener(CloseCredits);
    Debug.Log("Showing Credits");
  }

  void CloseCredits(){
    creditsView.closeButton.onClick.RemoveListener(CloseCredits);
    audioService.ReleaseReservedSfxSource();

    GameObject.Destroy(creditsView.gameObject);
    Debug.Log("Closing Credits");

    stageTransition.TransitionTo(1, false, 7);
    time.Resume();
  }

  void ClickSubscribe(){
    PostSubscribe();

    victoryView.subscribeEmailInput.text = "Thank you! Check your email to confirm";
  }

  async void PostSubscribe(){
    var platform = "";
    if(Application.platform == RuntimePlatform.Android){
      platform =  "Android";
    }else if(Application.platform == RuntimePlatform.IPhonePlayer){
      platform =  "iOS";
    }else{
      platform = Application.platform.ToString();
    }

    HttpClient client = new HttpClient();
    var values = new Dictionary<string, string>
    {
        { "api_key", "" },
        { "email",  victoryView.subscribeEmailInput.text },
        { "list", "" },
        { "referrer", "In Game" },
        { "gdpr", "true" },
        { "Platform", platform },
        { "ReleaseGroup", "Victory" },
    };

    var content = new FormUrlEncodedContent(values);

    var response = await client.PostAsync("https://sendy-url", content);

    var responseString = await response.Content.ReadAsStringAsync();
  }

  void OnReportSubmitted(UserReportSubmittedData data){
    if(data.success && data.userReport.Categories.Contains("High Score")){
      GetHighScore();
    }
  }

  async void GetHighScore(){
    string url = string.Empty;
    string responseString = string.Empty;

    try{
      var playerId = SystemInfo.deviceUniqueIdentifier;
      HttpClient client = new HttpClient();
      var parameters = new Dictionary<string, string>
      {
          { "ProjectIdentifier", Constants.reportProjectId },
          { "PlayerId",  playerId},
          { "Category", metagameData.victoryCount.ToString()},
          { "RankBy", "time" },
          { "ContextRows", "2" },
      };

      url = string.Format("{0}/api/highscore?{1}",
        Constants.reportUrl,
        string.Join("&", parameters.Select(kvp => $"{UnityWebRequest.EscapeURL(kvp.Key)}={UnityWebRequest.EscapeURL(kvp.Value)}"))
      );

      var response = await client.GetAsync(url);

      responseString = await response.Content.ReadAsStringAsync();

      var deserialized = SimpleJson.DeserializeObject<List<HighScoreData>>(responseString);

      victoryView.SetHighScoreData(deserialized, playerId);

    }catch(Exception e){
      Debug.LogError($"Unable to fetch high scores.\nUrl: {url}\nResponse: {responseString}");
      Debug.LogException(e);
    }
  }
}



public class HighScoreData {
  public string playerId { get; set; }
  public string score { get; set; }
  public HighScoreTime time { get; set; }
  public string rank { get; set; }
}

public class HighScoreTime
{
  public int hours { get; set; }
  public int minutes { get; set; }
  public int seconds { get; set; }
  public int milliseconds { get; set; }
}
