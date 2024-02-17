[System.Serializable]
public class GameDataModel
{
  [Inject] public StageTransitionModel stageData { get; set; }
  [Inject] public TutorialModel tutorialData {get; set;}
  [Inject] public SettingsDataModel settings {get; set;}
  [Inject] public StatsModel statsModel {get; set;}
  [Inject] public DemoDataModel demoData {get; set;}

  [Inject] public StageOneDataModel stage1Data {get; set;}
  [Inject] public StageTwoDataModel stage2Data {get; set;}
  [Inject] public StageThreeDataModel stage3Data {get; set;}
  [Inject] public StageFourDataModel stage4Data {get; set;}
  [Inject] public StageFiveDataModel stage5Data {get; set;}
  [Inject] public StageSixDataModel stage6Data {get; set;}
  [Inject] public StageSevenDataModel stage7Data {get; set;}

  [Inject] public MetaGameDataModel metagameData {get; set;}

  public string versionNumber;
  public string savedAtUTC;

  public bool requestedReview;
}