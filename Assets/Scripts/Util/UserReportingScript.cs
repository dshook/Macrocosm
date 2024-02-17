using System;
using Unity.Cloud.UserReporting;
using Unity.Cloud.UserReporting.Client;
using Unity.Cloud.UserReporting.Plugin;
using UnityEngine;
using UnityEngine.UI;
using strange.extensions.mediation.impl;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Represents a behavior for working with the user reporting client.
/// </summary>
/// <remarks>
/// This script is provided as a sample and isn't necessarily the most optimal solution for your project.
/// You may want to consider replacing with this script with your own script in the future.
/// </remarks>
public class UserReportingScript : View
{
  [Inject] GameSaverService gameSaver { get; set; }
  [Inject] MenuToggledSignal menuToggleSignal {get; set;}
  [Inject] StageTransitionModel stageTransitionData { get; set; }
  [Inject] public StatsModel statsModel {get; set;}
  [Inject] ResourceLoaderService loader { get; set; }
  [Inject] CreateHighScoreSignal highScoreSignal {get; set;}
  [Inject] UserReportSubmittedSignal reportSubmittedSignal {get; set;}
  [Inject] SendErrorUserReportSignal sendErrorUserReport {get; set;}


  /// <summary>
  /// Creates a new instance of the <see cref="UserReportingScript"/> class.
  /// </summary>
  public UserReportingScript()
  {
    this.unityUserReportingUpdater = new UnityUserReportingUpdater();
  }

  [Tooltip("The description input on the user report form.")]
  public TMP_InputField DescriptionInput;

  private bool isCreatingUserReport;

  [Tooltip("A value indicating whether the user report client reports metrics about itself.")]
  public bool IsSelfReporting;

  private bool isSubmitting;

  [Tooltip("A value indicating whether the user report client send events to analytics.")]
  public bool SendEventsToAnalytics;

  [Tooltip("The thumbnail viewer on the user report form.")]
  public Image ThumbnailViewer;

  private UnityUserReportingUpdater unityUserReportingUpdater;

  [Tooltip("The user report button used to create a user report.")]
  public ShinyButton UserReportButton;

  public ShinyButton CancelReportButton;

  [Tooltip("The UI for the user report form. Shown after a user report is created.")]
  public GameObject UserReportForm;

  public GameObject ThanksText;

  public TMP_Text statusText;
  public TMP_Text errorText;

  [System.Serializable]
  public class CategoryEntry
  {
    public FeedbackCategory category;
    public Toggle toggle;
  }

  public CategoryEntry[] categoryToggles;

  [System.Serializable]
  public class SubmitEntry
  {
    public SubmitEmotion emotion;
    public Button button;
  }

  public SubmitEntry[] submitButtons;

  public GameObject SubmitButtonsHolder;

  public GameObject SubmitStatusHolder;

  /// <summary>
  /// Gets the current user report.
  /// </summary>
  public UserReport CurrentUserReport { get; private set; }

  protected override void Awake () {
    base.Awake();

    Application.logMessageReceivedThreaded += HandleException;

    UnityUserReporting.Configure(Constants.reportUrl, Constants.reportProjectId, this.GetConfiguration());

    UserReportButton.onClick.AddListener(ShowFeedBackForm);
    CancelReportButton.onClick.AddListener(() => CloseUserReport(false, false));
    menuToggleSignal.AddListener(OnMenuToggle);
    highScoreSignal.AddListener(SendHighscore);
    sendErrorUserReport.AddListener(SendErrorReport);

    foreach(var submitButton in submitButtons){
      var emotion = submitButton.emotion;
      submitButton.button.onClick.AddListener(() => SubmitUserReport(emotion, false));
    }

  }

  protected override void Start()
  {
    base.Start();
  }

  //Note this is for the entire menu opening and closing, not just the user report overlay
  //If the user submits a report, then submits another without closing the menu we need to retain
  //The current user report since it will still have the screenshot.
  //Any feedback specific data should be cleared in ClearForm
  void OnMenuToggle(bool isOpening){
    if(isOpening){
      CreateUserReport();
    }else{
      //clear the current report when the menu fully closes
      this.CurrentUserReport = null;
      isCreatingUserReport = false;
    }
  }


  /// <summary>
  /// Creates a user report.
  /// </summary>
  public void CreateUserReport(Action<UserReport> reportCreatedCallback = null)
  {
    // Check Creating Flag
    if (this.isCreatingUserReport)
    {
      return;
    }

    // Set Creating Flag
    this.isCreatingUserReport = true;

    ClearForm();

    // Take Main Screenshot
    UnityUserReporting.CurrentClient.TakeScreenshot(1024, 1024, s => { });

    // Create Report
    UnityUserReporting.CurrentClient.CreateUserReport((br) =>
    {
      // Ensure Project Identifier
      if (string.IsNullOrEmpty(br.ProjectIdentifier))
      {
        Debug.LogWarning("The user report's project identifier is not set. Please setup cloud services using the Services tab or manually specify a project identifier when calling UnityUserReporting.Configure().");
      }

      using(var saveStream = gameSaver.GetSaveData()){
        br.Attachments.Add(
          new UserReportAttachment(
            "Save Game",
            "userReportSave.gd",
            "application/octet-stream",
            saveStream.GetBuffer()
          )
        );
      }

      // Set Current Report
      this.CurrentUserReport = br;

      // Set Creating Flag
      this.isCreatingUserReport = false;

      if(reportCreatedCallback != null){
        reportCreatedCallback(this.CurrentUserReport);
      }

    });
  }

  private UserReportingClientConfiguration GetConfiguration()
  {
    return new UserReportingClientConfiguration();
  }

  public bool IsSubmitting()
  {
    return this.isSubmitting;
  }

  private void SetThumbnail(UserReport userReport)
  {
    if (userReport != null && this.ThumbnailViewer != null && userReport.Screenshots.Count > 0)
    {
      byte[] data = Convert.FromBase64String(userReport.Screenshots[0].DataBase64);
      Texture2D texture = new Texture2D(1, 1);
      texture.LoadImage(data);
      this.ThumbnailViewer.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5F, 0.5F));
      this.ThumbnailViewer.preserveAspect = true;
    }
  }

  public void SubmitUserReport(SubmitEmotion emotion, bool autoGenerated)
  {
    // Preconditions
    if (this.isSubmitting || this.CurrentUserReport == null)
    {
      return;
    }

    // Set Submitting Flag
    this.isSubmitting = true;

    // Set version field
    this.CurrentUserReport.Fields.Add(new UserReportNamedValue("Version", gameSaver.GetVersionNumber()));

    const int summaryLength = 30;
    this.CurrentUserReport.Summary = DescriptionInput.text.Length > summaryLength ? DescriptionInput.text.Substring(0, summaryLength) : DescriptionInput.text;

    //Set Emotion
    var emotionValue = new UserReportNamedValue("Emotion", emotion.ToString());
    this.CurrentUserReport.Fields.Add(emotionValue);

    // Set Category
    foreach(var categoryOption in categoryToggles){
      if(categoryOption.toggle.isOn){

        this.CurrentUserReport.Categories.Add(categoryOption.category.ToString());
      }
    }

    var activeStage = $"{stageTransitionData.activeStage}-{stageTransitionData.activeSubStage[stageTransitionData.activeStage]}";
    var stageValue = new UserReportNamedValue("Stage", activeStage);
    this.CurrentUserReport.Fields.Add(stageValue);

    var totalPlayTime = new UserReportNamedValue("PlayTime", statsModel.totalPlayTime.ToString("0.0"));
    this.CurrentUserReport.Fields.Add(totalPlayTime);

    var sessionTime = new UserReportNamedValue("SessionTime", statsModel.sessionTime.ToString("0.0"));
    this.CurrentUserReport.Fields.Add(sessionTime);

#if DEMO
    var demoVersion = new UserReportNamedValue("Demo", "true");
    this.CurrentUserReport.Fields.Add(demoVersion);
#endif

    // Set Description
    // This is how you would add additional fields.
    if (this.DescriptionInput != null)
    {
      UserReportNamedValue userReportField = new UserReportNamedValue();
      userReportField.Name = "Description";
      userReportField.Value = this.DescriptionInput.text;
      this.CurrentUserReport.Fields.Add(userReportField);
    }

    Debug.Log("Sending User Report");
    // Send Report
    UnityUserReporting.CurrentClient.SendUserReport(this.CurrentUserReport, (uploadProgress, downloadProgress) =>
    {
      statusText.text = string.Format("{0:P}", uploadProgress);
    }, (success, br2) =>
    {
      reportSubmittedSignal.Dispatch(new UserReportSubmittedData(){
        success = success,
        userReport = CurrentUserReport
      });

      if (!success && !autoGenerated)
      {
        errorText.gameObject.SetActive(true);
        this.isSubmitting = false;
      }else{
        //Close the submit when all is good
        CloseUserReport(true, autoGenerated);
      }
    });
  }

  private void Update()
  {
    // Update Client
    UnityUserReporting.CurrentClient.IsSelfReporting = this.IsSelfReporting;
    UnityUserReporting.CurrentClient.SendEventsToAnalytics = this.SendEventsToAnalytics;

    var showSubmitButtons = !this.isSubmitting;

    SubmitButtonsHolder.SetActive(showSubmitButtons);
    SubmitStatusHolder.SetActive(!showSubmitButtons);

    // Update Client
    // The UnityUserReportingUpdater updates the client at multiple points during the current frame.
    this.unityUserReportingUpdater.Reset();
    this.StartCoroutine(this.unityUserReportingUpdater);
  }

  private void ShowFeedBackForm(){
    this.SetThumbnail(this.CurrentUserReport);
    this.UserReportForm.SetActive(true);
  }

  public void CloseUserReport(bool success, bool autoGenerated)
  {
    this.isSubmitting = false;
    this.UserReportForm.SetActive(false);
    this.ClearForm();

    if(success && !autoGenerated){
      if(ThanksText != null){
        //Have to do some extra work to move it into postition so it can be non zero scale to not screw up TMP
        ThanksText.gameObject.transform.localPosition = Vector3.zero;
        ThanksText.gameObject.transform.localScale = Vector3.zero;
        LeanTween.scale(ThanksText, Vector3.one, 0.5f).setEase(LeanTweenType.easeOutBack).setIgnoreTimeScale(true);
        LeanTween.scale(ThanksText, Vector3.zero, 0.5f).setDelay(1.5f).setIgnoreTimeScale(true);

        //move off screen and reset scale for next time
        LeanTween.moveLocal(ThanksText, new Vector3(1000, 0, 0), 0.1f).setDelay(2f).setIgnoreTimeScale(true);
        LeanTween.scale(ThanksText, Vector3.one, 0.1f).setDelay(2f).setIgnoreTimeScale(true);
      }
    }
  }

  private void ClearForm()
  {
    this.DescriptionInput.text = null;
    foreach(var category in categoryToggles){
      category.toggle.isOn = false;
    }
    statusText.text = string.Empty;
    errorText.gameObject.SetActive(false);
    UnityUserReporting.CurrentClient.ClearScreenshots();
    if(this.CurrentUserReport != null){
      this.CurrentUserReport.Fields.Clear();
    }

    SubmitButtonsHolder.SetActive(true);
    SubmitStatusHolder.SetActive(false);
  }

  void SendHighscore(VictoryHighScore highScoreData){
    CreateUserReport((br) =>
    {
      this.DescriptionInput.text = $"High Score!";
      this.CurrentUserReport.Categories.Add("High Score");

      var playTimeSpan = TimeSpan.FromSeconds(highScoreData.playTimeSeconds).ToString("c");
      this.CurrentUserReport.Fields.Add(new UserReportNamedValue(){Name = "High Score Time", Value = playTimeSpan });
      this.CurrentUserReport.Fields.Add(new UserReportNamedValue(){Name = "High Score Category", Value = highScoreData.victoryCount.ToString() });

      SubmitUserReport(SubmitEmotion.Happy, true);
    });
  }

  /*
    Hook into any exceptions thrown and automatically send them as user reports
    Only send one exception report per "session" though to avoid lots of spam if they're being thrown in a loop
  */
  bool sentExceptionLog = false;

  private void HandleException(string condition, string stackTrace, LogType type)
  {
    if (type == LogType.Exception && !sentExceptionLog)
    {
      CreateUserReport((br) =>
      {
        this.DescriptionInput.text = $"Exception: {condition}";
        this.CurrentUserReport.Categories.Add("Exception");

        SubmitUserReport(SubmitEmotion.Sad, true);
        sentExceptionLog = true;
      });
    }
  }

  private void SendErrorReport(string errorMessage)
  {
    CreateUserReport((br) =>
    {
      this.DescriptionInput.text = errorMessage;
      this.CurrentUserReport.Categories.Add("Bug");

      SubmitUserReport(SubmitEmotion.Sad, true);
    });
  }

  void OnApplicationFocus(bool hasFocus)
  {
    if(!hasFocus){
      sentExceptionLog = false;
    }
  }

  public enum FeedbackCategory{
    Gameplay,
    Bug,
    Performance,
    Suggestion,
  }

  public enum SubmitEmotion{
    Happy,
    Smile,
    Meh,
    Sad,
    Pain
  }

}