using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System;
using System.IO;
using System.Reflection;
using System.Linq;
using strange.extensions.mediation.impl;
using System.Collections.Generic;
using MiniJSON;

public class GameSaverService : View {

  [Inject] StageTransitionService stageTransition { get; set; }
  [Inject] StageTransitionEndSignal stageTransitionEnd { get; set; }
  [Inject] GameDataModel gameData {get; set; }
  [Inject] GameSavingSignal gameSavingSignal { get; set; }
  [Inject] GameLoadedSignal gameLoadedSignal { get; set; }
  [Inject] FloatingText floatingText { get; set; }

  public string overrideSaveName = "";

  //Editor only setting
  public bool skipSave = false;

  //Safeguard to avoid overwriting a save
  bool disableSave = false;


  const string defaultSaveName = "savedGames.gd";

  Dictionary<string, object> buildManifest;

  string savePath {
    get{
      var saveName = defaultSaveName;
#if UNITY_EDITOR
      if(!string.IsNullOrEmpty(overrideSaveName)){
        saveName = overrideSaveName;
        if(!saveName.EndsWith(".gd")){
          saveName += ".gd";
        }
      }
#endif
      return Application.persistentDataPath + "/" + saveName;
    }
  }

  protected override void Awake () {
    base.Awake();

    var buildManifestAsset = (TextAsset) Resources.Load("UnityCloudBuildManifest.json");
    if(buildManifestAsset != null){
      buildManifest = Json.Deserialize(buildManifestAsset.text) as Dictionary<string,object>;
    }else{
      Debug.LogWarning("Missing build manifest asset");
    }
  }

  //Script execution order should be set so this fires after everyone else has a chance to register signals
  protected override void Start () {
    base.Start();

    Logger.LogWithFrame("Game Started. Loading.");
    //Reset the scene to the first stage before loading so whatever state the editor left the game in it'll be consistent
    stageTransition.SetGameObjectsActive(1);
    Load();

    //Add listener to transition (so we can save) after the load has completed so we don't save on startup
    stageTransitionEnd.AddListener(TransitionFinished);
  }

  void Update () { }

  public void Save() {
#if UNITY_EDITOR
    if(skipSave){
      return;
    }
#endif

    if(disableSave){
      Debug.Log("Saved Disabled");
      return;
    }

    // Debug.Log("Saving State");
    gameSavingSignal.Dispatch();

    //attach save metadata
    gameData.versionNumber = GetVersionNumber();
    gameData.savedAtUTC = DateTime.UtcNow.ToString("o");

    var tmpFilePath = savePath + ".tmp";

    try{

      // Save to a temp file first to avoid corrupting the save if it fails for whatever reason
      var startTime = Time.realtimeSinceStartup;
      BinaryFormatter bf = new BinaryFormatter();
      FileStream file = File.Create(tmpFilePath);
      bf.Serialize(file, gameData);
      file.Close();

      var tmpSaveExists = File.Exists(tmpFilePath);
      // Assuming all went well, replace the save with the temp one
      if (File.Exists(savePath) && tmpSaveExists)
      {
        File.Copy(tmpFilePath, savePath, true);
        File.Delete(tmpFilePath);
      }else if(tmpSaveExists){
        File.Move(tmpFilePath, savePath);
      }

      var endTime = Time.realtimeSinceStartup;
      Debug.Log(string.Format("Saved State {0}", endTime - startTime));
    }catch(Exception e){
      Debug.LogError("Error Saving: " + e.Message);
    }
  }

  public MemoryStream GetSaveData(){
    BinaryFormatter bf = new BinaryFormatter();
    MemoryStream stream = new MemoryStream();
    bf.Serialize(stream, gameData);
    return stream;
  }

  public void Load() {
    // Debug.Log("Loading State");
    var beforeTime = Time.realtimeSinceStartup;
    var prevGameData = gameData;
    try{
      if(File.Exists(savePath)) {
        UnityEngine.Profiling.Profiler.BeginSample("Loading Save");
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Open(savePath, FileMode.Open);
        var loadedGameData = (GameDataModel)bf.Deserialize(file);
        file.Close();
        UnityEngine.Profiling.Profiler.EndSample();

        UnityEngine.Profiling.Profiler.BeginSample("Restoring Save");
        GameSaverService.RestoreProperties(loadedGameData, gameData);
        UnityEngine.Profiling.Profiler.EndSample();
      }
    }catch(Exception e){
      Debug.LogError("Error Loading Save: " + e.Message);

      disableSave = true;
      floatingText.CreateUI("Error loading save.\n\nSaving will be disabled till fixed.\n\nThe problem has been automatically reported.", Color.white, false, 10f);

      throw e;
      // ResetGameData();
    }
    DebugExtensions.DebugWithTime("Loaded data from: " + savePath.Split('/').Last(), beforeTime);
    beforeTime = Time.realtimeSinceStartup;
    GC.Collect();
    PostRestoreInit(gameData);
    gameLoadedSignal.Dispatch();
    stageTransition.TransitionTo(stageTransition.stageData.activeStage, true);
    DebugExtensions.DebugWithTime("Load Transition", beforeTime);
  }

  public void ResetData(){
    RemoveSave();
    ResetGameData();
    gameLoadedSignal.Dispatch();
  }

  public void RemoveSave(){
    File.Delete(savePath);
  }

  public void ResetGameData(){
    Debug.LogWarning("RESETTING GAME DATA");
    var newGameData = new GameDataModel();

    //create new objects for all the base props in the game data model
    Type typeSrc = newGameData.GetType();
    PropertyInfo[] srcProps = typeSrc.GetProperties();
    foreach (PropertyInfo srcProp in srcProps)
    {
      if (!srcProp.CanRead) { continue; }
      if(!srcProp.PropertyType.IsClass) { continue; }

      // Passed all tests, lets create a new one
      var newOne = Activator.CreateInstance(srcProp.PropertyType);

      srcProp.SetValue(newGameData, newOne, null);
    }

    RestoreProperties(newGameData, gameData, true);
    PostRestoreInit(gameData);
  }

  public void ResetStageData(int stage){
    switch(stage){
      case 1:
        RestoreProperties(new StageOneDataModel(), gameData.stage1Data, true);
        break;
      case 2:
        RestoreProperties(new StageTwoDataModel(), gameData.stage2Data, true);
        break;
      case 3:
        RestoreProperties(new StageThreeDataModel(), gameData.stage3Data, true);
        break;
      case 4:
        RestoreProperties(new StageFourDataModel(), gameData.stage4Data, true);
        break;
      case 5:
        RestoreProperties(new StageFiveDataModel(), gameData.stage5Data, true);
        break;
      case 6:
        RestoreProperties(new StageSixDataModel(), gameData.stage6Data, true);
        break;
      case 7:
        RestoreProperties(new StageSevenDataModel(), gameData.stage7Data, true);
        break;
    }
    PostRestoreInit(gameData);
  }

  public void ResetDataForVictory(){
    RestoreProperties(new StageTransitionModel(), gameData.stageData, true);
    for(var i = 1; i <= StageTransitionModel.lastStage; i++){
      ResetStageData(i);
    }
    //Post reset setup to get new maps
    gameData.stage6Data.mapSeed = NumberExtensions.GenerateNewSeed();
    gameData.stage7Data.mapSeed = NumberExtensions.GenerateNewSeed();

    gameLoadedSignal.Dispatch();
  }

  void PostRestoreInit(GameDataModel gameData){
    //make sure these arrays have the right # of elements. Should only be a factor when the last stage is updated
    //and the save data has the old number
    if(gameData.stageData.stagesUnlocked.Length != StageTransitionModel.lastStage + 1){
      Array.Resize(ref gameData.stageData.stagesUnlocked, StageTransitionModel.lastStage + 1);
    }
    if(gameData.stageData.stageProgression.Length != StageTransitionModel.lastStage + 1){
      Array.Resize(ref gameData.stageData.stageProgression, StageTransitionModel.lastStage + 1);
    }
    if(gameData.stageData.activeSubStage.Length != StageTransitionModel.lastStage + 1){
      Array.Resize(ref gameData.stageData.activeSubStage, StageTransitionModel.lastStage + 1);
    }

    gameData.stageData.stagesUnlocked[1] = true; //make sure stage 1 is always unlocked
    gameData.stageData.activeStage = Math.Max(gameData.stageData.activeStage, 1); //Min stage is 1

    //Fully saturate the starting atom sizes
    for(var i = 1; i <= 6; i++){
      gameData.stage2Data.atomSaturation[i] = 100;
    }

    //Make sure we're not paused from a tutorial being open when the game shut down
    gameData.tutorialData.paused = false;

    //make sure we're on a valid stage
    stageTransition.stageData.activeStage = Mathf.Clamp(stageTransition.stageData.activeStage, 1, 7);
  }


  //Copy non injected properties from source to destination so object references don't get clobbered
  //Recurse through any injected props
  //Hard reset:  Normally when loading a game when the loaded game from disk has a null property we want to skip
  //That and keep the destination property that is non null.  This happens when adding a new reference type to the save
  //With a default value.  However, when we're resetting the game data "hardReset" is passed as true which means
  //This check is skipped and all the source properties are restored into destination including the null ones
  public static void RestoreProperties(object source, object destination, bool hardReset = false)
  {
    if (source == null){
      return; //let the object initializer do the work if our source is null (probably meaning we added a new field)
    }
    if (destination == null){
      throw new Exception("Destination Object is null");
    }
    // Getting the Types of the objects
    Type typeDest = destination.GetType();
    Type typeSrc = source.GetType();

    // Iterate the Properties of the source instance and
    // populate them from their desination counterparts
    PropertyInfo[] srcProps = typeSrc.GetProperties();
    foreach (PropertyInfo srcProp in srcProps)
    {
      if (!srcProp.CanRead)
      {
        continue;
      }
      PropertyInfo targetProperty = typeDest.GetProperty(srcProp.Name);
      if (targetProperty == null) { continue; }
      if (!targetProperty.CanWrite) { continue; }
      if (targetProperty.GetSetMethod(true) != null && targetProperty.GetSetMethod(true).IsPrivate) { continue; }
      if ((targetProperty.GetSetMethod().Attributes & MethodAttributes.Static) != 0) { continue; }
      if (!targetProperty.PropertyType.IsAssignableFrom(srcProp.PropertyType)) { continue; }

      //recurse setting injected props
      if(srcProp.GetCustomAttribute(typeof(Inject)) != null && targetProperty.GetCustomAttribute(typeof(Inject)) != null){
        GameSaverService.RestoreProperties(srcProp.GetValue(source), targetProperty.GetValue(destination), hardReset );
        continue;
      }

      var srcValue = srcProp.GetValue(source, null);

      //See note about hard reset
      if(!hardReset && srcValue == null){
        continue;
      }

      // Passed all tests, lets set the value
      targetProperty.SetValue(destination, srcValue, null);
    }


    var srcFields = typeSrc.GetFields();
    foreach(var srcField in srcFields)
    {
      var targetField = typeDest.GetField(srcField.Name);
      if (targetField == null) { continue; }
      if (targetField.IsPrivate || targetField.IsStatic){ continue; }
      if (!targetField.FieldType.IsAssignableFrom(srcField.FieldType)) { continue; }

      var srcValue = srcField.GetValue(source);

      //See note about hard reset
      if(!hardReset && srcValue == null){
        continue;
      }

      // Passed all tests, lets set the value
      targetField.SetValue(destination, srcValue);
    }
  }

  void OnApplicationQuit()
  {
    Debug.Log("Application ending after " + Time.unscaledTime + " seconds");
    Save();
  }

  void OnApplicationFocus(bool hasFocus)
  {
    Debug.Log(string.Format("Application {0} focus at {1} seconds", hasFocus ? "gaining" : "losing", Time.unscaledTime));
    if(!hasFocus){
      Save();
    }
  }

  void OnApplicationPause(bool pauseStatus)
  {
    Debug.Log(string.Format("Application {0} at {1} seconds", pauseStatus ? "pausing" : "unpausing", Time.unscaledTime));
  }

  void TransitionFinished(StageTransitionData data){
    Save();
  }


  public string GetVersionNumber(){
    // Set version field
    string version = Application.version;
    string buildNumber = (buildManifest != null && buildManifest.ContainsKey("buildNumber"))
      ? buildManifest["buildNumber"].ToString()
      : "unknown";

    return string.Format("{0}.{1}", version, buildNumber);
  }

}
