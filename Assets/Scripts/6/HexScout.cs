using System;
using System.Collections;
using strange.extensions.mediation.impl;
using UnityEngine;

[Serializable]
public class HexScoutData {
  public HexCoordinates source;
  public HexCoordinates dest;
  public HexCoordinates position;
  public int movePoints;
  public bool canCrossWater = false;

  public int currentMovePoints;
  public bool turnedBack = false;
}

public class HexScout : View {
  [Inject] AudioService audioService {get; set;}

  public System.Action<HexCoordinates> OnScoutCompleted;

  public HexGrid grid {get; set;}
  public StageSixDataModel stageSixData;
  public HexPathFollower follower;
  public FilledBar moveBar;
  public SpriteRenderer boatRenderer;

  public AudioClip scoutFinishedClip;

  public HexScoutData data;

  public void Init(){
    follower.grid = grid;

    if(!data.turnedBack){
      follower.source = data.source;
      follower.dest = data.dest;
    }else{
      follower.source = data.dest;
      follower.dest = data.source;
    }

    follower.position = data.position;
    follower.OnPathCompleted = PathComplete;
    follower.OnMoveStepCompleted = MoveStepCompleted;
    follower.canCrossWater = data.canCrossWater;
    UpdateMoveBar();
    follower.Move();
  }

  void PathComplete(){
    if(!data.turnedBack){
      //time to turn back
      TurnBack();
      follower.Move();
    }else{
      StartCoroutine(Finished());
    }
  }

  IEnumerator Finished(){
    //Wait for next frame so that when we're loading and a scout is finished for whatever reason it doesn't modify
    //the collection it's looping over
    yield return new WaitForSeconds(0f);
    OnScoutCompleted(data.source);
    Destroy(this.gameObject);
  }

  void Update(){

    var overCell = grid.GetCell(transform.position);
    //Get in or out of the boat
    boatRenderer.gameObject.SetActive(overCell != null && overCell.IsUnderwater);
  }

  void MoveStepCompleted(){
    //explore some tiles
    data.position = follower.position;
    var middleCell = grid.GetCell(data.position);

    var viewRange = 1;
    var partialRange = 1;

    if(stageSixData.ResearchedTech(HexTechId.Astronomy)){
      viewRange++;
    }

    if(middleCell.HexFeature == HexFeature.Hills){
      partialRange++;
    }
    if(middleCell.HexFeature == HexFeature.Mountains){
      partialRange += 2;
    }

    //Explore around the scout, partial range is extended on top of the view range
    for(var v = 1; v <= viewRange + partialRange; v++){
      var exploreStatus = v <= viewRange ? HexExploreStatus.Explored : HexExploreStatus.Partial;

      foreach(var cell in grid.GetRing(middleCell, v)){
        cell.Explore(exploreStatus);
      }
    }

    //update our move points
    data.currentMovePoints -= middleCell.MoveCost(follower.pathfindOptions);
    UpdateMoveBar();
    if(data.currentMovePoints <= 0 && !data.turnedBack){
      TurnBack();
    }
  }

  void UpdateMoveBar(){
    moveBar.fillAmt = Mathf.Clamp01((float)data.currentMovePoints / (float)data.movePoints);
  }

  void TurnBack(){
    follower.source = data.dest;
    follower.dest = data.source;
    data.turnedBack = true;
    audioService.PlaySfx(scoutFinishedClip);
  }
}
