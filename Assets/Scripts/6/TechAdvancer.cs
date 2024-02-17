using UnityEngine;
using strange.extensions.mediation.impl;

//Take care of just the part of advancing tech progress once stage 6 is unlocked
public class TechAdvancer : View {

  //Certain things public so hex tech on apply can use
  [Inject] ResourceLoaderService loader {get; set;}
  [Inject] HexTechFinishedSignal techFinishedSignal { get; set; }
  [Inject] HexBonusResourceRevealedSignal bonusResourceRevealedSignal {get;set;}
  [Inject] StageFiveDataModel stageFiveData {get; set;}
  [Inject] StageTransitionModel stageTransitionData { get; set; }

  //The public ones are used in HexTech onApply calls
  [Inject] public StageSixDataModel stageSixData {get; set;}
  [Inject] public StageRulesService stageRules { get; set; }
  [Inject] public StageTransitionService stageTransition { get; set; }

  [Inject] AudioService audioService {get; set;}


  public AudioClip techFinishedClip;

  protected override void Awake() {
    base.Awake();

    Debug.Log("Tech advancer awake");
  }

  public float TribeSciencePerSecond{
    get {
      return stageFiveData.stageHighScore;
    }
  }

  public float CitySciencePerSecond{
    get {
      float citySciencePerSecond = 0;
      foreach(var cityData in stageSixData.cities){
        citySciencePerSecond += cityData.calculatedSciencePerSecond;
      }
      return citySciencePerSecond;
    }
  }

  public float TotalSciencePerSecond{
    get{
      float scienceProgressionRate = stageTransitionData.activeStage == 6 ? 1f : stageRules.StageSixRules.idleScienceRate;
      float totalSciencePerSecond = (TribeSciencePerSecond + CitySciencePerSecond) * scienceProgressionRate;

      return totalSciencePerSecond;
    }
  }

  void Update(){
    if(!stageTransitionData.stagesUnlocked[6] || stageSixData.cities == null || stageSixData.cities.Count == 0){
      return;
    }

    if(stageSixData.techQueue.Count > 0){

      //check for now invalid techs being researched.  Should only happen for testing
      if(!HexTech.allTechs.ContainsKey(stageSixData.techQueue[0].techId)){
        stageSixData.techQueue.RemoveAt(0);
        return; //Let the next update pick up the new tech in case there are multiple invalid
      }

      var researching = stageSixData.techQueue[0];
      var curTech = HexTech.allTechs[researching.techId];

      if(researching.progress < 1f){
        var scienceCollected = TotalSciencePerSecond * Time.deltaTime;
        researching.progress += scienceCollected / curTech.techCost(stageRules.StageSixRules);
      }
      if(researching.progress >= 1f && !researching.finished){
        TechResearchFinished();
      }

      //pop from the queue and onto the next thing if there's something next
      if(stageSixData.techQueue.Count > 1 && researching.finished){
        stageSixData.techQueue.RemoveAt(0);
      }

    }
  }

  void TechResearchFinished(){
    var researching = stageSixData.techQueue[0];
    var curTech = HexTech.allTechs[researching.techId];

    researching.finished = true;

    stageSixData.techs[researching.techId] = true;

    if(curTech.onApply != null){
      curTech.onApply(this);
    }

    audioService.PlaySfx(techFinishedClip);
    techFinishedSignal.Dispatch(researching.techId);
  }

  public void RevealResource(HexBonusResource resource){
    stageSixData.bonusResourceRevealed[resource] = true;
    bonusResourceRevealedSignal.Dispatch(resource);
  }


}
