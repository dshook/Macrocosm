using UnityEngine;
using strange.extensions.mediation.impl;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class StageTwoManager : View, IStageManager {
  public UIFilledBar scoreBar;
  public GameObject atomPrefab;
  public RectTransform sequenceIndicator;
  public Transform atomHolder;
  public Transform enemyHolder;
  public RectTransform initRegionBounds;
  public Snake snake;
  public OrthographicZoom zoomComp;
  public Transform displaySeqHolder;
  public UIContentLooper snakeSequenceLooper;
  public JoystickControl joystickControl;
  public BoostDisplay boostDisplay;
  public ObjectRepositioner objectRepositioner;
  public BgSpawner[] bgSpawners;
  public StageOneBigText successText;
  public StageOneBigText cellUnlockedText;

  public float substageTransitionAnimateTime = 2.5f;

  class AtomDisplay{
    public AtomRenderer atomRenderer {get; set;}
    public GameObject gameObject {get; set;}
    public RectTransform rectTransform {get; set;}
  }
  AtomDisplay[] atomDisplays;

  public AudioClip substageCompleteSound;
  public AudioClip moleculeBonusSound;

  public AudioClip music;

  [Inject] StageTransitionService stageTransition { get; set; }
  [Inject] StageRulesService stageRules { get; set; }
  [Inject] StageTwoDataModel stageTwoData { get; set; }
  [Inject] StageThreeDataModel stageThreeData { get; set; }
  [Inject] TutorialSystem tutorialSystem {get; set;}
  [Inject] ResourceLoaderService loader {get; set;}
  [Inject] FloatingText floatingNumbers {get; set;}
  [Inject] SpawnService spawner {get; set;}
  [Inject] StringChanger stringChanger {get; set;}
  [Inject] FloatingText floatingText { get; set; }
  [Inject] GameSavingSignal gameSavingSignal { get; set; }
  [Inject] AudioService audioService {get; set;}
  [Inject] BeatTypeUnlockedSignal beatTypeUnlockedSignal {get; set;}
  [Inject] InputService input {get; set;}
  [Inject] CameraService cameraService {get; set;}

  int prevSnakeLen = 0;
  bool finishedSubStage = false;
  int[] startingAtomCounts;
  Follow sequenceIndicatorFollower;

  //Track all the free floating atoms so the snake AI can find them without GC allocing with GetComponent
  HashSet<AtomRenderer> freeAtoms = new HashSet<AtomRenderer>();

  List<Snake> enemySnakes = new List<Snake>();

  protected override void Awake () {
    base.Awake();

    gameSavingSignal.AddListener(OnSaving);
    beatTypeUnlockedSignal.AddListener(OnBeatTypeUnlocked);
    sequenceIndicatorFollower = sequenceIndicator.GetComponent<Follow>();
  }

  public void Init(bool isInitialCall){
    if(!isInitialCall){
      stageTwoData.scoreAccum = 0;
    }
    finishedSubStage = false;

    Cleanup();

    foreach(var bgSpawner in bgSpawners){
      bgSpawner.Spawn();
    }

    //recreate atom display sequence
    displaySeqHolder.DestroyChildren();
    var usingScrollingSnakeSequence = stageRules.StageTwoRules.eatSequence.Length > snakeSequenceLooper.MinItems;
    var displayEatSequenceIndex = (usingScrollingSnakeSequence && isInitialCall) ? stageTwoData.eatSequenceIndex : 0;
    atomDisplays = new AtomDisplay[stageRules.StageTwoRules.eatSequence.Length];
    for(int i = 0; i < stageRules.StageTwoRules.eatSequence.Length; i++)
    {
      var newAtom = GameObject.Instantiate(atomPrefab, Vector3.one, Quaternion.identity );
      newAtom.transform.SetParent(displaySeqHolder, false);
      var rectTransform = newAtom.GetComponent<RectTransform>();

      var renderer = newAtom.GetComponentInChildren<AtomRenderer>();
      renderer.size = stageRules.StageTwoRules.eatSequence[displayEatSequenceIndex];
      atomDisplays[displayEatSequenceIndex] = new AtomDisplay(){
        atomRenderer = renderer,
        gameObject = newAtom,
        rectTransform = rectTransform
      };

      displayEatSequenceIndex = (displayEatSequenceIndex + 1) % stageRules.StageTwoRules.eatSequence.Length;
    }

    if(stageRules.StageTwoRules.startingAtomCounts != null){
      startingAtomCounts = stageRules.StageTwoRules.startingAtomCounts;
    }else{
      //dynamically create atom counts based on some rules
      //TODO: also save the starting atom counts
      var seq = stageRules.StageTwoRules.eatSequence;
      var totalAtomCount = Mathf.RoundToInt((float)stageRules.StageTwoRules.goalAmount / 3f);
      var maxAtomSize = seq.Max();
      startingAtomCounts = new int[maxAtomSize + 1];

      var goalPortionPct = 0.7f; //What pct of the total atoms should be ones that are in the sequence

      //allocate the ones for the eat sequence first based on their weight in the sequence
      foreach(var sz in seq.Distinct()){
        startingAtomCounts[sz] += Mathf.RoundToInt(
          ((float)seq.Count(x => x == sz) / seq.Length) * goalPortionPct * totalAtomCount
        );
      }

      //allocate atoms not in the sequence as filler
      var remainingCount = totalAtomCount - startingAtomCounts.Sum();
      while(remainingCount > 0){
        //hacky way to find a random atom that's not in the sequence
        var fillerAtom = Random.Range(1, maxAtomSize);
        if(seq.Contains(fillerAtom)){ continue; }

        //TODO: prolly should be a better allocation since this could allocate everything on the first go instead of spreading it out
        var toAllocate = Random.Range(1, Mathf.Max(1, (remainingCount + 1) / 2));

        startingAtomCounts[fillerAtom] += toAllocate;
        remainingCount -= toAllocate;
      }
    }

    //spawn atoms
    for(int i = 0; i < startingAtomCounts.Length; i++){
      stageTwoData.atomSaturation[i] = (ushort)Mathf.Max(stageTwoData.atomSaturation[i], stageRules.StageTwoRules.minAtomSaturation);

      UpdateAtomDisplaySat();
      SpawnAtoms(i, RequiredAtomCount(i));
    }

    //spawn enemy snakes
    var snakePrefab = loader.Load<GameObject>("Prefabs/2/snake");
    spawner.SpawnObjects(snakePrefab, stageRules.StageTwoRules.enemySnakeCount, 25f, 50f, cameraService.Cam.transform.position, enemyHolder, (GameObject g) => {
      var newSnake = g.GetComponent<Snake>();
      newSnake.playerControlled = false;
      newSnake.aggressiveAI = stageRules.StageTwoRules.aggressiveSnakes;
      newSnake.speed = stageRules.StageTwoRules.snakeSpeed * stageRules.StageTwoRules.enemySnakeSpeedPct;
      newSnake.freeAtoms = freeAtoms;
      newSnake.objectRepositioner = objectRepositioner;
      newSnake.SetupSnake(isInitialCall);
      enemySnakes.Add(newSnake);
    });

    //show or hide boost button
    var hasBoost = stageRules.StageTwoRules.boostAmount > 0;
    boostDisplay.gameObject.SetActive(hasBoost);
    if(hasBoost){
      tutorialSystem.ShowPopoutTutorial("2-boost", "Boost unlocked!<br>Hold the button for a burst of speed!");
    }

    snake.playerJoystickControl = joystickControl;
    snake.rotationSpeed = stageRules.StageTwoRules.snakeRotationSpeed;
    snake.speed = stageRules.StageTwoRules.snakeSpeed;
    snake.freeAtoms = freeAtoms;
    snake.objectRepositioner = objectRepositioner;
    snake.SetupSnake(isInitialCall);
    if(isInitialCall){
      snake.eatSequenceIndex = stageTwoData.eatSequenceIndex;
    }

    zoomComp.snake = snake;
    zoomComp.ResetZoom();

    prevSnakeLen = snake.snakeMembers.Count;

  }

  public void OnTransitionTo(StageTransitionData data){
    zoomComp.snake = snake;
    zoomComp.ResetZoom();
    UpdateAtomDisplaySat();

    successText.Hide();
    cellUnlockedText.Hide();

    //Check all the eat sequence to see if we need to spawn any more atoms if saturation was increased from stage 1
    if(!data.isLoad){
      for(int s = 0; s < stageRules.StageTwoRules.eatSequence.Length; s++){
        CheckAndSpawnAtoms(stageRules.StageTwoRules.eatSequence[s]);
      }
    }

    audioService.PlayMusic(music);
  }

  public void OnTransitionAway(bool toMenu){
    zoomComp.snake = null;
    zoomComp.ResetZoom();

    audioService.StopMusic();
  }

  void OnSaving(){
    stageTwoData.eatSequenceIndex = snake.eatSequenceIndex;
    stageTwoData.snakeAtoms = snake.snakeMembers.Select(s => s.size).ToList();
  }

  public void Cleanup(){
    sequenceIndicator.gameObject.SetActive(false);

    foreach(var enemySnake in enemySnakes){
      Destroy(enemySnake.gameObject);
    }
    enemySnakes.Clear();
    // enemyHolder.transform.DestroyChildren();

    snake.Cleanup();

    atomHolder.DestroyChildren();
    snakeSequenceLooper.Reset();
    joystickControl.Reset();

    foreach(var bgSpawner in bgSpawners){
      bgSpawner.Cleanup();
    }

    //clean up atom display sequence
    if(atomDisplays != null){
      foreach(var atomRenderer in atomDisplays){
        Destroy(atomRenderer.gameObject);
      }
      atomDisplays = null;
    }
    freeAtoms.Clear();

    zoomComp.snake = null;
    zoomComp.ResetZoom();
  }

  void Update () {
    if(snake == null) return;

    //When we eat, add score when the sequence resets
    if(snake.snakeMembers.Count != prevSnakeLen){
      if(snake.snakeMembers.Count > prevSnakeLen ){
        snakeSequenceLooper.Next();
        stageTwoData.scoreAccum += snake.snakeMembers.Count;
        floatingNumbers.Create(snake.snakeMembers[0].transform.position, Colors.greenText, text: "+" + snake.snakeMembers.Count);

        //bump up atom saturations when a sequence is completed for all atoms that the snake contains
        if(snake.eatSequenceIndex == 0){
          var eatSequence =  stageRules.StageTwoRules.eatSequence;

          bool completedSequence = eatSequence.Length > 1; //Don't count bonus for carbon chain

          //walk the eat sequence backwards to match the order of the snake members
          //Check to see if we have the full sequence, and bump up saturations of the members
          for(int i = 0; i < eatSequence.Length; i++){
            var seq = eatSequence[i];

            var snakeIndex = eatSequence.Length - i; //walk the sequence backwards
            if(snake.snakeMembers.Count > snakeIndex && snake.snakeMembers[snakeIndex].size == seq){
              stageTwoData.atomSaturation[seq] = (ushort)Mathf.Clamp(stageTwoData.atomSaturation[seq] + 1, 0, 100);
              UpdateAtomDisplaySat();
              CheckAndSpawnAtoms(seq);
            }else{
              completedSequence = false;
            }
          }

          if(completedSequence){
            var bonusScore = eatSequence.Sum();
            stageTwoData.scoreAccum += bonusScore;
            floatingNumbers.Create(snake.snakeMembers[0].transform.position, Colors.green,
              text: string.Format("+{0}<br>Complete Molecule Bonus!", bonusScore),
              delay: 0.4f,
              ttl: 3,
              fontSize: 3f
            );
            audioService.PlaySfx(moleculeBonusSound);
          }
        }
      }
      prevSnakeLen = snake.snakeMembers.Count;
    }

    var mouseBoostDown = input.ButtonIsDown("Fire2", false);
    if(boostDisplay.boostButton.IsDown || mouseBoostDown){
      //manually update the button position by selecting it when using the mouse boost
      if(mouseBoostDown){
        boostDisplay.boostButton.isSelected = true;
      }else if(boostDisplay.boostButton.isSelected){
        boostDisplay.boostButton.isSelected = false;
      }

      snake.SetIsBoosting(true);
    }else{

      snake.SetIsBoosting(false);
    }

    if(atomDisplays != null && atomDisplays.Length > 0){
      // update sequence indicator to be a child of the right item in the sequence and set to first sibling so it displays first (under)
      sequenceIndicator.gameObject.SetActive(true);
      sequenceIndicatorFollower.target = atomDisplays[snake.eatSequenceIndex].gameObject.transform;
    }else{
      sequenceIndicator.gameObject.SetActive(false);
    }

    stringChanger.UpdateString(scoreBar.labelText, "StageTwoScoreAccum", stageTwoData.scoreAccum, "{0}/{1}", stageTwoData.scoreAccum, stageRules.StageTwoRules.goalAmount);
    scoreBar.fillAmt = Mathf.Clamp01( (float)stageTwoData.scoreAccum / (float)stageRules.StageTwoRules.goalAmount);

    if(stageTwoData.scoreAccum >= stageRules.StageTwoRules.goalAmount && !finishedSubStage){
      finishedSubStage = true;

      audioService.PlaySfx(substageCompleteSound);
      successText.Show(stageRules.StageTwoRules.moleculeName, "CREATED");

      //Start transition to next substage
      stageTransition.UnlockNextSubStage(stageTransition.stageData.activeStage);
      StartCoroutine(FinishTransitionToNextSubstage());
    }
  }

  IEnumerator FinishTransitionToNextSubstage(){
    yield return new WaitForSeconds(substageTransitionAnimateTime);

    stageTransition.TransitionTo(stageTransition.stageData.activeStage);
  }

  void UpdateAtomDisplaySat(){
    for(int i = 0; i < atomDisplays.Length; i++){
      var disp = atomDisplays[i];
      var sat = stageTwoData.atomSaturation[disp.atomRenderer.size];
      disp.atomRenderer.overrideColor = sat == 100 ? Colors.golden : Color.white;
      disp.atomRenderer.overrideText = string.Format("{0:0}%", sat);
    }
  }


  int RequiredAtomCount(int size){
    return (int)(((float)stageTwoData.atomSaturation[size] / 100f) * startingAtomCounts[size]);
  }

  void SpawnAtoms(int size, int count){
    if(count == 0){ return; }

    var stageRectTransform = GameObject.Find("Stages/2/RegionBounds").GetComponent<RectTransform>();
    var snakePart = loader.Load<GameObject>("Prefabs/2/snake_part");

    //Have smaller elements move faster, and bigger ones slower
    var moveForce = stageRules.StageTwoRules.baseAtomMoveForce + (size - 6) * -0.05f;

    //Spawn atoms in the donut area, little bigger than they'll eventually be with the object repositioner
    //Give the snake some room to breathe too
    var innerRadius = 3f;
    var outerRadius = 30f;
    var center = cameraService.Cam.transform.position;

    spawner.SpawnObjects(snakePart, count, innerRadius, outerRadius, center, atomHolder, (GameObject g) => {
      var atomRenderer = g.GetComponentInChildren<AtomRenderer>();
      atomRenderer.size = size;
      var randomMove = g.GetComponentInChildren<RandomMovement>();
      randomMove.moveForce = moveForce;

      freeAtoms.Add(atomRenderer);
    });
  }

  //see if we have enough of the atoms of a size, including the ones that are in the snake
  void CheckAndSpawnAtoms(int size){

    int sum = 0;
    foreach(var freeAtom in freeAtoms){
      if(freeAtom.size == size){ sum++; }
    }
    sum += snake.snakeMembers.Count(m => !m.isHead && m.size == size);
    foreach(var enemySnake in enemySnakes){
      sum += enemySnake.snakeMembers.Count(m => !m.isHead && m.size == size);
    }

    var req = RequiredAtomCount(size);

    if(req > sum){
      Debug.Log("Spawning Extra Atoms:" + size + " #" + (req - sum));
      SpawnAtoms(size, req - sum);
    }
  }

  //Little back and forth between stage 2 & 3 here.  Stage two progress triggers the beat unlock
  //In stage 3 which then calls this.  Want to keep the logic synced up
  void OnBeatTypeUnlocked(BeatType type){
    if(stageTransition.stageData.stagesUnlocked[3]){
      cellUnlockedText.Show("NEW CELL", "BONUS!");
    }
  }
}
