using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PygmyMonkey.ColorPalette;
using strange.extensions.mediation.impl;
using TMPro;
using UnityEngine;

public class BeatManager : View {

  [Inject] SpawnService spawner {get; set;}
  [Inject] StageTransitionModel stageData { get; set; }
  [Inject] StageThreeDataModel stageThreeData { get; set; }
  [Inject] BeatHitSignal beatHitSignal { get; set; }
  [Inject] BeatTutorialFinishedSignal beatTutorialFinishedSignal { get; set; }
  [Inject] StageRulesService stageRules { get; set; }
  [Inject] ResourceLoaderService loader { get; set; }
  [Inject] Canvas mainCanvas { get; set; }
  [Inject] ObjectPool objectPool {get; set;}
  [Inject] AudioService audioService {get; set;}
  [Inject] SongFinishedSignal songFinishedSignal {get; set;}
  [Inject] OpenTutorialFinishedSignal openTutorialFinishedSignal { get; set; }
  [Inject] CameraService cameraService { get; set; }

  public BgCellSpawner bgCellSpawner;

  public ShinyButton startCountdownButton;

  public GameObject singleBeatPrefab;
  public GameObject doubleBeatPrefab;
  public GameObject multiBeatPrefab;
  public GameObject multiBeatCellPrefab;
  public GameObject slideBeatPrefab;

  public RectTransform gameplayRectTransform; //The total area for gameplay
  public RectTransform spawnRectTransform; //Inset from the gameplay area

  public TMP_Text countdownText;

  public AudioClip primaryClick;
  public AudioClip secondaryClick;
  public AudioClip countdownClip;
  public bool playClick = false;
  public bool showDebugText = true;

  [Tooltip("Make the game play itself")]
  public bool demoMode = false;

  #if UNITY_EDITOR
  double clickTimeToNextBeat = 0;
  #endif

  public TMP_Text beatDebugText;

  public SongTemplate templateToTest = null;

  public enum BeatManagerState {
    Stopped,
    CountingDown,
    Playing,
    Paused
  }

  BeatManagerState state = BeatManagerState.Stopped;

  public BeatManagerState State{
    get{ return state; }
  }

  int? pausedTimeSamples = null;

  //Total number of beats that have been played by previously played templates
  float templateAccumulatedBeats = 0f;
  double prevMusicTimeElapsed = 0d;

  bool resetTemplateAccumOnSongLoop = false;

  float beatAnimateTime = 0f;

  float zPos = 0f;
  const float zPosChange = 0.01f;

  const float countdownTime = 3f;
  Coroutine countdownRef;

  SongTemplate[] songTemplates;
  BeatTemplate[] beatTemplates;

  SongTemplate songPlaying;
  BeatTemplate templatePlaying;
  BeatTypeData tutorialBeatData = null;


  public bool inTutorial{
    get{ return tutorialBeatData != null; }
  }

  public int totalSongCount{
    get{ return songTemplates.Length; }
  }

  public Dictionary<BeatType, SongTemplate> tutorialTemplates = new Dictionary<BeatType, SongTemplate>(new BeatTypeComparer());
  Color blipColor;
  Color blipCompleteColor;

  protected override void Awake () {
    base.Awake();

    beatHitSignal.AddListener(OnBeatHit);
    startCountdownButton.onClick.AddListener(ClickStartButton);
    openTutorialFinishedSignal.AddListener(OnOpenTutorialFinished);

    objectPool.CreatePool(singleBeatPrefab, 1);
    objectPool.CreatePool(multiBeatCellPrefab, 0);
    // objectPool.CreatePool(multiBeatPrefab, 1);
    // objectPool.CreatePool(slideBeatPrefab, 1);

    beatTemplates = loader.LoadAll<BeatTemplate>("Prefabs/3/Templates");

    songTemplates = loader.LoadAll<SongTemplate>("Prefabs/3/SongTemplates");
#if UNITY_EDITOR
    if(templateToTest != null){
      Debug.Log("Using TemplatesTest " + templateToTest.name);
      songTemplates = new SongTemplate[1]{ templateToTest };
    }
#else
    //Make sure we don't allow demo mode outside editor
    demoMode = false;
#endif

    var colorPalette = ColorPaletteData.Singleton.fromName("Stage3");

    foreach(var type in System.Enum.GetValues(typeof(BeatType)).Cast<BeatType>()){
      var typeStr = type.ToString();
      tutorialTemplates[type] = loader.Load<SongTemplate>("Prefabs/3/TemplatesTutorials/" + typeStr);
    }
    blipColor = colorPalette.getColorFromName("Blip").color;
    blipCompleteColor = colorPalette.getColorFromName("BlipComplete").color;
  }

  public void Init(){
    songPlaying = null;
    templateAccumulatedBeats = 0f;
    bgCellSpawner.Init();
  }

  protected override void OnDestroy(){
    beatHitSignal.RemoveListener(OnBeatHit);
  }

  public void Cleanup(){
    if(objectPool != null){
      objectPool.RecycleAll(singleBeatPrefab);
      objectPool.RecycleAll(multiBeatCellPrefab);
      // objectPool.RecycleAll(multiBeatPrefab);
      // objectPool.RecycleAll(slideBeatPrefab);
    }
    transform.DestroyChildren();
  }

  public void Play(bool skipStartButton = false){
    if(state == BeatManagerState.Playing || state == BeatManagerState.CountingDown){
      return;
    }

    //Coming back from a pause
    if(state == BeatManagerState.Paused && songPlaying != null){
      Logger.Log("UnPausing");

      if(pausedTimeSamples != null){
        audioService.SetMusicTime(pausedTimeSamples.Value);
        pausedTimeSamples = null;
      }

      ShowStartButton("Resume");
      return;
    }

    //Otherwise start a new song or tutorial
    tutorialBeatData = null;
    templateAccumulatedBeats = 0f;
    audioService.SetMusicTime(0);
    bgCellSpawner.Cleanup();

    //Select the song based off saved data, but cap at length for test song
    //TODO: have this loop back through the songs when you finish the last song
    var nextSongTemplate = songTemplates[Mathf.Min(stageThreeData.songIndex, songTemplates.Length - 1)];

    var nextBeatTypesUsed = nextSongTemplate.beatTypesUsed;

    //Check for tutorials that need to be completed
    foreach(var beatData in stageThreeData.beatData){
      //Play tutorial if we haven't completed the tutorial and the next song is using that beat type
      if(!beatData.Value.completedTutorial &&
        nextBeatTypesUsed != null &&
        nextBeatTypesUsed.Contains(beatData.Key)
      ){
        StartNewSong(tutorialTemplates[beatData.Key]);
        tutorialBeatData = beatData.Value;
        Logger.Log("Playing Tutorial " + beatData.Key);
        break;
      }
    }
    //Regular song template selection
    if(songPlaying == null){
      StartNewSong(nextSongTemplate);
      Logger.Log("Playing Song " + songPlaying.musicClip.name);
    }
    beatAnimateTime = (float)bpmQuarterNoteTime;

    if(skipStartButton){
      ClickStartButton();
    }else{
      ShowStartButton("Start");
    }
  }

  public void Pause(){
    //Only can pause from these states
    if(state == BeatManagerState.Playing || state == BeatManagerState.CountingDown){
      Logger.Log("Pausing");
      pausedTimeSamples = audioService.MusicTimeSamples;
      audioService.StopMusic();
      if(state == BeatManagerState.CountingDown){
        CancelCountdown();
      }

      state = BeatManagerState.Paused;
    }
  }

  IEnumerator Countdown(){
    Logger.Log("Counting down " + audioService.MusicTimeSamples);
    state = BeatManagerState.CountingDown;

    startCountdownButton.gameObject.SetActive(false);
    var thirdCountdowntime = countdownTime / 3f;
    var countdownStartTime = Time.time;

    //Add in extra padding to the audio starting for unity editor startup time mainly, but also to give the player extra time
    var dspTimeToStart = AudioSettings.dspTime + (double)countdownTime + (double)thirdCountdowntime;
    var looping = inTutorial;
    audioService.PlayMusicScheduled(songPlaying.musicClip, dspTimeToStart, looping);

    #if UNITY_EDITOR
      if(playClick){
        audioService.PlaySfxScheduled( primaryClick, dspTimeToStart );
      }
    #endif

    audioService.PlaySfx(countdownClip);

    ShowCountdownText("3", thirdCountdowntime);
    yield return new WaitForSeconds(thirdCountdowntime);

    ShowCountdownText("2", thirdCountdowntime);
    yield return new WaitForSeconds(thirdCountdowntime);

    ShowCountdownText("1", thirdCountdowntime);
    yield return new WaitForSeconds(thirdCountdowntime);

    state = BeatManagerState.Playing;
    Logger.Log("Playing");
  }

  void CancelCountdown(){
    Logger.Log("Stopping Countdown");
    HideCountdownText();
    StopCoroutine(countdownRef);
  }

  void ShowCountdownText(string t, float time){
    countdownText.gameObject.SetActive(true);
    countdownText.transform.localScale = Vector3.one;
    countdownText.text = t;
    LeanTween.scale(countdownText.gameObject, Vector3.zero, time).setEase(LeanTweenType.easeInQuint);
  }

  void HideCountdownText(){
    countdownText.gameObject.SetActive(false);
  }

  void ShowStartButton(string label){
    startCountdownButton.label = label;
    // startCountdownButton.label = fromPause ? "Resume" : "Start";
    startCountdownButton.gameObject.SetActive(true);
  }

  public void ClickStartButton(){
    countdownRef = StartCoroutine(Countdown());
  }

  public double bpmQuarterNoteTime
  {
    get {
      if(songPlaying == null){ return 0f; }

      return 60d / songPlaying.bpm;
    }
  }

  public double SongBeats{
    get {
      if(bpmQuarterNoteTime == 0){
        return 0d;
      }

      return audioService.MusicTimeElapsed / bpmQuarterNoteTime;
    }
  }

  void Update () {
    if(state != BeatManagerState.Playing) return;

    //Handle tutorial looping
    if(resetTemplateAccumOnSongLoop && audioService.MusicTimeElapsed < prevMusicTimeElapsed){
      resetTemplateAccumOnSongLoop = false;
      templateAccumulatedBeats = 0;
    }

    double songBeats = SongBeats; //Prolly a dumb optimization but only need to calc once here
    prevMusicTimeElapsed = audioService.MusicTimeElapsed;
    var templateBeats = songBeats - templateAccumulatedBeats;

    if(templatePlaying == null){
      UnityEngine.Profiling.Profiler.BeginSample("Create new template");
      var templateIndex = songPlaying.templateIndex % songPlaying.templates.Length; //Loop the templates for testing
      templatePlaying = GameObject.Instantiate(songPlaying.templates[templateIndex], spawnRectTransform, false);
      templatePlaying.name = string.Format("{0} {1}", songPlaying.templates[templateIndex].name, templateIndex);
      UnityEngine.Profiling.Profiler.EndSample();
    }

    #if UNITY_EDITOR
      if(showDebugText){
        UnityEngine.Profiling.Profiler.BeginSample("Editor debug text");
        beatDebugText.gameObject.SetActive(true);
        var songBeatsDisplay = Mathf.CeilToInt((float)songBeats);
        var songTotalBeats = Mathf.FloorToInt((float)(songPlaying.length / bpmQuarterNoteTime));
        var templateBeatsDisplay = Mathf.FloorToInt((float)templateBeats) + 1;
        var templateTotalBeats = templatePlaying.beatLength;
        beatDebugText.text = string.Format("{4} {0}/{1} {2}/{3}", templateBeatsDisplay, templateTotalBeats, songBeatsDisplay, songTotalBeats, templatePlaying.name);
        UnityEngine.Profiling.Profiler.EndSample();
      }else{
        beatDebugText.gameObject.SetActive(false);
      }

      if(playClick && AudioSettings.dspTime > clickTimeToNextBeat){
        double barDuration = bpmQuarterNoteTime * 4f;
        double beatRemainder = audioService.MusicTimeElapsed % bpmQuarterNoteTime;
        double barRemainder = audioService.MusicTimeElapsed % barDuration;
        double clickTimeToNextBar = AudioSettings.dspTime + (barDuration - barRemainder);
        clickTimeToNextBeat = AudioSettings.dspTime + (bpmQuarterNoteTime - beatRemainder);
        audioService.PlaySfxScheduled( clickTimeToNextBar == clickTimeToNextBeat ? primaryClick : secondaryClick, clickTimeToNextBeat );
      }
    #endif

    UnityEngine.Profiling.Profiler.BeginSample("Creating beats");
    foreach(var beatItem in templatePlaying.items){
      if(!beatItem.fired && templateBeats > (beatItem.beat - beatAnimateTime)){
        beatItem.fired = true;
        if(beatItem.position == null){
          Logger.LogWarning(string.Format("Unset beat position for {0} beat {1}", templatePlaying.name, beatItem.beat));
        }

        switch(beatItem.type){
          case BeatType.Single:
          case BeatType.Double:
            NewSingleBeat((float)bpmQuarterNoteTime * 2f, beatItem, beatItem.position);
            break;
          case BeatType.Slide:
            NewSlider((float)bpmQuarterNoteTime, false, beatItem, beatItem.position, MapSplinePoints(beatItem));
            break;
          case BeatType.SlideReverse:
            NewSlider((float)bpmQuarterNoteTime, true, beatItem, beatItem.position, MapSplinePoints(beatItem));
            break;
          case BeatType.Multi:
            NewMultiBeat((float)bpmQuarterNoteTime, beatItem, beatItem.position, MapSplinePoints(beatItem));
            break;
        }
      }
    }
    UnityEngine.Profiling.Profiler.EndSample();

    //Start template switch early to allow the next template to have its beats animate in at the right time
    if(templateBeats >= templatePlaying.beatLength - beatAnimateTime){
      templateAccumulatedBeats += templatePlaying.beatLength;
      songPlaying.templateIndex++;

      if(tutorialBeatData != null){
        //Check for completing tutorial, should check for all templates in the song, but only using one for now
        tutorialBeatData.completedTutorial = templatePlaying.items.All(d => d.completed);

        if(tutorialBeatData.completedTutorial){
          beatTutorialFinishedSignal.Dispatch(new BeatTutorialData(){ subStage = stageData.stageProgression[3]});
          CleanupSongPlaying();
          resetTemplateAccumOnSongLoop = false;
        }else{
          resetTemplateAccumOnSongLoop = true;
        }
      }

      CleanupTemplatePlaying();
      // Debug.Log("Template finished");

    }

    if(songPlaying != null &&
      state == BeatManagerState.Playing &&
      //Audio is weird and it's possible to miss the check for the song finishing by the end time, in which case
      //The music time elapsed will be back to 0
      (audioService.MusicTimeElapsed >= songPlaying.length || !audioService.musicSource.isPlaying )
    ){
      state = BeatManagerState.Stopped;
      songFinishedSignal.Dispatch(songPlaying);
      CleanupSongPlaying();
      CleanupTemplatePlaying();
      Cleanup();
      Debug.Log("Song finished");
    }
  }

  void CleanupTemplatePlaying(){
    Destroy(templatePlaying.gameObject);
    templatePlaying = null;
  }

  void CleanupSongPlaying(){
    audioService.StopMusic();
    Destroy(songPlaying.gameObject);
    songPlaying = null;
  }

  //Convert from screen space anchored stuff to world coords for the spline points
  Vector3[] MapSplinePoints(BeatTemplateItem beatItem){
    var beatPosition = beatItem.position.position;
    var spawnAreaScaleComparedToGameplayArea = new Vector2(spawnRectTransform.rect.width / gameplayRectTransform.rect.width, spawnRectTransform.rect.height / gameplayRectTransform.rect.height);
    Vector2 scaleFactor = mainCanvas.scaleFactor * spawnAreaScaleComparedToGameplayArea;
    var originWorld = cameraService.Cam.ScreenToWorldPoint(beatItem.position.localPosition * scaleFactor);

    var worldPoints = beatItem.spline.Points.Select(p => {
      return cameraService.Cam.ScreenToWorldPoint((p + beatItem.position.localPosition) * scaleFactor) - originWorld;
    }).ToArray();


    // var testWorld = beatItem.position.TransformPoint(beatItem.position.localPosition);
    // var testWorld2 = testWorld * scaleFactor;

    // var testtt = beatItem.position.TransformVector(beatItem.position.localPosition);
    // var testtt2 = testtt * scaleFactor;

    // var testlocal = beatItem.position.InverseTransformPoint(beatItem.position.position);

    // var localToWorld = cam.ScreenToWorldPoint(beatItem.position.localPosition);
    // var posToWorld = cam.ScreenToWorldPoint(beatItem.position.position);
    // var anchorToWorld = cam.ScreenToWorldPoint(beatItem.position.anchoredPosition);

    // var worldPoints = beatItem.spline.Points.Select(p => {
    //   return cam.ScreenToWorldPoint((p + beatItem.position.localPosition) ) ;
    // }).ToArray();

#if UNITY_EDITOR

    for(var i = 1; i < worldPoints.Length; i++){
      Debug.DrawLine(worldPoints[i] + beatPosition, worldPoints[i-1] + beatPosition, Color.red, 10f);
    }
#endif

    return worldPoints;
  }

  void NewSingleBeat(float lifeTime, BeatTemplateItem beatTemplateItem, RectTransform rtPosition){
    var prefab = beatTemplateItem.type == BeatType.Single ? singleBeatPrefab : doubleBeatPrefab;
    spawner.SpawnObjects(prefab, 1, spawnRectTransform, this.transform, 1.5f, (GameObject g) => {
      g.transform.position = rtPosition.transform.position;
      g.transform.rotation = Quaternion.AngleAxis(Random.Range(-90f, 90f), Vector3.forward);

      var newBeat = g.GetComponent<SingleBeat>();
      // newBeat.unlocked = stageThreeData.beatData[beatTemplateItem.type].unlocked;
      newBeat.bonus = stageThreeData.beatData[beatTemplateItem.type].bonus;
      newBeat.beatTemplateItem = beatTemplateItem;
      newBeat.numberOfHits = beatTemplateItem.type == BeatType.Double ? 2 : 1;
      newBeat.isClone = false;
      newBeat.lifeTime = lifeTime;
      newBeat.animateInTime = newBeat.animateInTime = beatAnimateTime;
      newBeat.beatManager = this;
      newBeat.Init();
    }, zPos);
    zPos += zPosChange;
  }

  void NewMultiBeat(float lifeTimePerCell, BeatTemplateItem beatTemplateItem, RectTransform rtPosition, Vector3[] points){
    var beat = GameObject.Instantiate(
      multiBeatPrefab,
      new Vector3( 0, 0, zPos),
      Quaternion.identity
    );
    beat.transform.SetParent(this.transform, true);
    beat.transform.position = rtPosition.transform.position;

    var newBeat = beat.GetComponent<MultiBeat>();
    // newBeat.unlocked = stageThreeData.beatData[beatTemplateItem.type].unlocked;
    newBeat.bonus = stageThreeData.beatData[beatTemplateItem.type].bonus;
    newBeat.beatTemplateItem = beatTemplateItem;

    newBeat.points = points;
    newBeat.lifeTimePerCell = lifeTimePerCell;
    newBeat.animateInTime = newBeat.animateInTime = beatAnimateTime;
    newBeat.beatManager = this;

    zPos += zPosChange;
  }

  void NewSlider(float lifeTimePerBlip, bool isReverse, BeatTemplateItem beatTemplateItem, RectTransform rtPosition, Vector3[] points){
    var beat = GameObject.Instantiate(
      slideBeatPrefab,
      new Vector3(
        0,
        0,
        zPos
      ),
      Quaternion.identity
    );
    beat.transform.SetParent(this.transform, true);
    beat.transform.position = rtPosition.transform.position;

    SlideBeat newBeat = beat.GetComponent<SlideBeat>();
    // newBeat.unlocked = stageThreeData.beatData[beatTemplateItem.type].unlocked;
    newBeat.bonus = stageThreeData.beatData[beatTemplateItem.type].bonus;
    newBeat.blipColor = blipColor;
    newBeat.blipCompleteColor = blipCompleteColor;
    newBeat.lifeTimePerBlip = lifeTimePerBlip;
    newBeat.isReverse = isReverse;
    newBeat.beatTemplateItem = beatTemplateItem;
    newBeat.points = points;
    // newBeat.animateInTime = beatAnimateTime;
    //No animate in for slides right now
    newBeat.animateInTime = 0;
    newBeat.beatManager = this;

    zPos += zPosChange;
  }

  public void OnBeatHit(BeatHitData beatHit){
    if(beatHit.beat.beatTemplateItem != null){
      beatHit.beat.beatTemplateItem.completed = beatHit.hit;
    }
  }

  void StartNewSong(SongTemplate songTemplate){
    spawnRectTransform.DestroyChildren();
    this.transform.DestroyChildren();
    songPlaying = GameObject.Instantiate(songTemplate, Vector3.zero, Quaternion.identity, spawnRectTransform);
  }

  void OnOpenTutorialFinished(int tutorialId){
    // Debug.Log("Finished tutorial " + tutorialId);
    //Currently this is only for handling the frozen tutorial that happens mid song
    if(tutorialId == 302){
      Play();
    }
  }
}
