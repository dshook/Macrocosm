using UnityEngine;
using strange.extensions.mediation.impl;
using System.Collections.Generic;
using System.Linq;

public class CreatureSceneManager : View {

  [Inject] ResourceLoaderService loader {get; set;}
  [Inject] CreatureDoneMovingSignal creatureDoneMoving {get; set;}
  [Inject] StageRulesService stageRules { get; set; }
  [Inject] StageFourDataModel stageFourData { get; set; }
  [Inject] CameraService cameraService {get; set;}

  public CreatureSceneLayer[] layers;

  public GameObject enemiesHolder;
  public GameObject effectsHolder;

  [Tooltip("Time is currently in sync with walk sound")]
  public float moveTime = 2f;

  private bool _moving;
  public bool moving {
    get{ return _moving; }
  }

  List<CreatureScene> scenes = new List<CreatureScene>();
  public CreatureScene currentScene {
    get{
      if(scenes.Count == 0 || stageFourData.sceneMoveCount > scenes.Count){
        return null;
      }
      return scenes[stageFourData.sceneMoveCount];
    }
  }

  float sceneSize = 7;
  float prevSceneSize = 0;

  public float enemyOffset = 1.8f;

  GameObject[] bg1Prefabs;
  GameObject[] bg2Prefabs;
  GameObject[] cloudPrefabs;
  GameObject[] groundPrefabs;
  GameObject[] foregroundPrefabs;
  public GameObject creaturePrefab;


  CreatureSceneLayer groundLayer;
  CreatureSceneLayer groundFoodLayer;
  CreatureSceneLayer cloudsLayer;
  CreatureSceneLayer bg2Layer;
  CreatureSceneLayer bg1Layer;
  CreatureSceneLayer foregroundLayer;
  CreatureSceneLayer skySwoopLayer;
  CreatureSceneLayer groundSwoopLayer;

  float timeAccum = 0f;

  RandomStepIncreaser mateProbabilityCheck;

  //Synced from stage 4 manager
  public CreatureData playerCreatureData;

  protected override void Awake(){
    base.Awake();

    bg1Prefabs = loader.LoadAll<GameObject>("Prefabs/4/bg1");
    bg2Prefabs = loader.LoadAll<GameObject>("Prefabs/4/bg2");
    cloudPrefabs = loader.LoadAll<GameObject>("Prefabs/4/clouds");
    groundPrefabs = loader.LoadAll<GameObject>("Prefabs/4/ground");
    foregroundPrefabs = loader.LoadAll<GameObject>("Prefabs/4/foreground");
    creaturePrefab = loader.Load<GameObject>("Prefabs/4/creature");

    mateProbabilityCheck = new RandomStepIncreaser(
      stageRules.StageFourRules.mateInitialProbability,
      stageRules.StageFourRules.mateStepIncreaseProbability
    );

    groundLayer = layers.FirstOrDefault(x => x.name == "ground");
    groundFoodLayer = layers.FirstOrDefault(x => x.name == "groundFood");
    cloudsLayer = layers.FirstOrDefault(x => x.name == "clouds");
    bg2Layer = layers.FirstOrDefault(x => x.name == "bg2");
    bg1Layer = layers.FirstOrDefault(x => x.name == "bg1");
    foregroundLayer = layers.FirstOrDefault(x => x.name == "foreground");
    skySwoopLayer = layers.FirstOrDefault(x => x.name == "skySwoop");
    groundSwoopLayer = layers.FirstOrDefault(x => x.name == "groundSwoop");
  }

  void Update () {
    sceneSize = cameraService.cameraWorldRect.width;
    if(sceneSize != prevSceneSize && !moving){
      RepositionSceneBasedOnScreenSize();
      GoToScene(stageFourData.sceneMoveCount, true);

      prevSceneSize = sceneSize;
    }

    if(_moving){
      timeAccum += Time.deltaTime;
    }

    if(_moving && timeAccum >= moveTime){
      _moving = false;
      timeAccum = 0;
      creatureDoneMoving.Dispatch();
    }

    foreach(var layer in layers){
      if(!_moving && layer.autoScrollSpeed != 0){
        layer.ScrollOffset = layer.ScrollOffset += layer.autoScrollSpeed * Time.deltaTime;

        //Logic duped with go to scene movement logic
        var moveAmt = -sceneSize / layer.moveRatio;
        var pos = moveAmt * stageFourData.sceneMoveCount + layer.ScrollOffset;
        layer.holder.transform.position = layer.holder.transform.position.SetX(pos);
      }
    }

  }

  public void Reset(){
    foreach(var layer in layers){
      LeanTween.cancel(layer.holder);
      layer.holder.transform.DestroyChildren(true);
      layer.holder.transform.position = layer.holder.transform.position.SetX(0);
      layer.ScrollOffset = 0f;
    }

    LeanTween.cancel(enemiesHolder);
    LeanTween.cancel(effectsHolder);

    enemiesHolder.transform.DestroyChildren();

    sceneSize = cameraService.cameraWorldRect.width;

    enemiesHolder.transform.position = enemiesHolder.transform.position.SetX(0);
    effectsHolder.transform.position = effectsHolder.transform.position.SetX(0);
    _moving = false;
    mateProbabilityCheck.Reset();
    scenes.Clear();

    GenerateScenes();

  }

  public void MoveForward()
  {
    stageFourData.sceneMoveCount++;
    CreateSceneObjects(stageFourData.sceneMoveCount + 1);
    GoToScene(stageFourData.sceneMoveCount, false);

    //Check for enemy creatures and mates that have been passed to keep them cleaned up
    var sceneIndexToCheck = stageFourData.sceneMoveCount - 2;
    if(sceneIndexToCheck >= 0 && scenes[sceneIndexToCheck] != null){
      if(scenes[sceneIndexToCheck].enemyCreature != null){
        Destroy(scenes[sceneIndexToCheck].enemyCreature.gameObject);
        scenes[sceneIndexToCheck].enemyCreature = null;
      }

      if(scenes[sceneIndexToCheck].mate != null){
        Destroy(scenes[sceneIndexToCheck].mate.gameObject);
        scenes[sceneIndexToCheck].mate = null;
      }
    }
  }

  public void GoToScene(int newSceneIndex, bool skipAnimation)
  {
    if(newSceneIndex >= scenes.Count || newSceneIndex < 0){
      Debug.LogWarning("Trying to go to invalid scene index " + newSceneIndex);
      return;
    }

    var nextScene = scenes[newSceneIndex];


    //Do we need to make an enemy?
    if(!nextScene.data.rolledForEnemyCreature){
      nextScene.data.rolledForEnemyCreature = true;
      if(Random.Range(0f, 1f) <= stageRules.StageFourRules.enemyProbThreshold + playerCreatureData.enemyPctChanceBonus){
        nextScene.data.enemyCreatureData = CreateNewEnemyCreature(newSceneIndex);
        stageFourData.totalEnemiesEncountered++;
      }
    }

    //create an enemy creature creatures
    if(nextScene.data.enemyCreatureData != null && nextScene.enemyCreature == null){
      var newCreatureGO = GameObject.Instantiate(
        creaturePrefab,
        Vector3.zero,
        Quaternion.identity,
        enemiesHolder.transform
      );
      newCreatureGO.transform.transform.localPosition = new Vector3((newSceneIndex * sceneSize) + enemyOffset, -3f, 0);

      var newCreature = newCreatureGO.GetComponent<Creature>();
      newCreature.Reset();
      newCreature.data = nextScene.data.enemyCreatureData;

      //Re-kill if coming back from a save
      if(newCreature.data.dead){
        newCreature.Die(skipAnimation);
      }
      newCreature.UpdateDisplay(true);

      //flip the enemies so they're facing us
      newCreature.bodyDisplay.transform.localScale = newCreature.bodyDisplay.transform.localScale
        .SetX(-newCreature.bodyDisplay.transform.localScale.x);

      nextScene.enemyCreature = newCreature;
    }

    //Should there be a mate?
    if(!nextScene.data.rolledForMate &&
        newSceneIndex >= stageRules.StageFourRules.minMateSteps &&
        nextScene.data.enemyCreatureData == null
    ){
      nextScene.data.rolledForMate = true;

      if( mateProbabilityCheck.Check(playerCreatureData.matePctChanceBonus) ){
        var newMateData = new CreatureData();
        newMateData.Reset();

        //copy the creatures modifiers to the mate so they look the same, and add in one as a bonus
        newMateData.modifiers.Clear();
        newMateData.modifiers.AddRange(stageFourData.creatureData.modifiers);
        var extraMod = newMateData.getAvailableMods(
          stageFourData,
          newSceneIndex,
          stageFourData.mapSeed + stageFourData.sceneMoveCount,
          1,
          false,
          true
        );
        if(extraMod != null && extraMod.Length > 0){
          newMateData.AddMod(extraMod[0]);
        }

        nextScene.data.mateData = newMateData;
        stageFourData.totalMatesEncountered++;
      }
    }else{
      //Mark as rolled for mate here anyways so we don't retry after killing an enemy creature then rebooting the game
      nextScene.data.rolledForMate = true;
    }

    nextScene.data.currentMateProbability = mateProbabilityCheck.CurrentProbability;

    //create a mate
    if(nextScene.data.mateData != null && nextScene.mate == null){
      var newMateGO = GameObject.Instantiate(
        creaturePrefab,
        Vector3.zero,
        Quaternion.identity,
        enemiesHolder.transform
      );
      newMateGO.transform.transform.localPosition = new Vector3((newSceneIndex * sceneSize) + enemyOffset, -3f, 0);

      var newMate = newMateGO.GetComponent<Creature>();
      newMate.data = nextScene.data.mateData;
      newMate.Reset();

      //copy the creatures modifiers to the mate so they look the same, and add in one as a bonus
      var extraMod = nextScene.data.mateData.modifiers.Except(stageFourData.creatureData.modifiers).FirstOrDefault();
      if(extraMod != CreatureModifierId.None){
        newMate.ShowMateDisplay(CreatureModifier.allModifiers[extraMod]);
      }

      //flip so they're facing us
      newMate.bodyDisplay.transform.localScale = newMate.bodyDisplay.transform.localScale
        .SetX(-newMate.bodyDisplay.transform.localScale.x);

      nextScene.mate = newMate;
    }

    _moving = !skipAnimation;
    var animationTime = skipAnimation ? 0f : moveTime;

    //move the layers
    foreach(var layer in layers){
      var moveAmt = -sceneSize / layer.moveRatio;
      var pos = moveAmt * newSceneIndex + layer.ScrollOffset;
      LeanTween.move(layer.holder, layer.holder.transform.position.SetX(pos), animationTime).setEase(LeanTweenType.easeInOutSine);
    }

    float groundMoveAmt = -sceneSize;
    var groundXPos = groundMoveAmt * newSceneIndex;

    //Maybe these should be layers too?
    LeanTween.move(enemiesHolder, enemiesHolder.transform.position.SetX(groundXPos), animationTime).setEase(LeanTweenType.easeInOutSine);
    LeanTween.move(effectsHolder, effectsHolder.transform.position.SetX(groundXPos), animationTime).setEase(LeanTweenType.easeInOutSine);
  }


  int GetMaxSceneIndexForLayer(Transform parent){
    if(parent.childCount > 0){
      int result = 0;
      if(System.Int32.TryParse(parent.GetChild(parent.childCount - 1).name, out result)){
        return result;
      }
    }
    return 0;
  }

  //Returns true if scene holder already exists
  bool CreateOrGetSceneHolder(int sceneIndex, Transform parent, out GameObject holder){
    var sceneName = sceneIndex.ToString();
    var holderTransform = parent.Find(sceneName);

    if(holderTransform){
      holder = holderTransform.gameObject;
      return true;
    }

    holder = new GameObject();
    holder.name = sceneName;
    holder.transform.SetParent(parent, false);
    holder.transform.localPosition = new Vector3(sceneIndex * sceneSize, 0, 0);

    return false;
  }

  //Dynamically create some stuff for the next scene
  void GenerateScenes(){
    Random.State originalRandomState = Random.state;
    Random.InitState(stageFourData.mapSeed);
    var scenesToGen = stageRules.StageFourRules.maxSteps + 1;

    //Generate data objects first to prefill later ones
    for(int i = 0; i < scenesToGen; i++){
      var newScene = new CreatureScene();
      var newSceneData = new CreatureSceneData();
      newScene.data = newSceneData;

      newSceneData.canUpgrade = i > 0 && i % stageRules.StageFourRules.creatureUpgradeInterval == 0;

      scenes.Add(newScene);
    }

    for(int i = 0; i < scenesToGen; i++){
      var newScene = scenes[i];
      var newSceneData = newScene.data;

      //bg 2
      //Backgrounds are spawned a little differently where they're place in a row next to each other
      //But the scene index won't correspond to when you actually see them in game on that scene due to the parallax defined by the ratio
      if(i * bg2Layer.moveRatio < scenesToGen){
        var bg2Prefab = bg2Prefabs[Random.Range(0, bg2Prefabs.Length)];

        //mark the next scenes as having this background so bg1 & ground can sync
        for(var n = i * (int)bg2Layer.moveRatio; n < scenes.Count && n < ((i + 1) * (int)bg2Layer.moveRatio); n++){
          scenes[n].bg2Type = bg2Prefab.name;
        }
      }

      //ground food
      foreach(var food in foodToName){
        if(Random.Range(0f, 1f) >= 0.70f){
          newSceneData.groundFood |= food.Key;
        }
      }

      //Sky & Ground Swoop, just generate 4 of them, they will be looped around by RepositionToNextCreatureScene script
      if(i < 4){
        var skySwoopPrefab = loader.Load<GameObject>("Prefabs/4/skySwoop");
        var newSwoop = GameObject.Instantiate(
          skySwoopPrefab,
          new Vector3(i * sceneSize, 0, 0),
          Quaternion.identity
        );
        newSwoop.transform.SetParent(skySwoopLayer.holder.transform, false);
        newScene.sceneTransforms.Add(newSwoop.transform);

        var groundSwoopPrefab = loader.Load<GameObject>("Prefabs/4/groundSwoop");
        var newGroundSwoop = GameObject.Instantiate(
          groundSwoopPrefab,
          new Vector3(i * sceneSize, 0, 0),
          Quaternion.identity
        );
        newGroundSwoop.transform.SetParent(groundSwoopLayer.holder.transform, false);
        newScene.sceneTransforms.Add(newGroundSwoop.transform);
      }
    }

    Random.state = originalRandomState;

    //restore current scene data after generation so generation is deterministic
    if(stageFourData.currentSceneData != null){
      scenes[stageFourData.sceneMoveCount].data = stageFourData.currentSceneData;

      if(stageFourData.currentSceneData.currentMateProbability > 0){
        mateProbabilityCheck.SetProbability(stageFourData.currentSceneData.currentMateProbability);
      }

      //remove ground food if it's been eaten in the save
      var savedFood = stageFourData.currentSceneData.groundFood;
      foreach(var foodName in CreatureSceneManager.foodToName){
        if((savedFood & foodName.Key) == 0 ){
          RemoveGroundFoodDisplay(foodName.Value, stageFourData.sceneMoveCount, true);
        }
      }
    }
  }

  public void CreateSceneObjects(int stepCount){
    var newScene = scenes[stepCount];
    var newSceneData = newScene.data;

    //bg 2
    //Backgrounds are spawned a little differently where they're place in a row next to each other
    //But the scene index won't correspond to when you actually see them in game on that scene due to the parallax defined by the ratio
    var maxBg2LayerIndex = GetMaxSceneIndexForLayer(bg2Layer.holder.transform);
    var neededBg2LayerIndex = Mathf.CeilToInt((stepCount) / bg2Layer.moveRatio);
    var minVisibleBg2LayerIndex = neededBg2LayerIndex - 1;
    for(int i = Mathf.Max(maxBg2LayerIndex, minVisibleBg2LayerIndex); i <= neededBg2LayerIndex; i++){
      var exists = CreateOrGetSceneHolder(i, bg2Layer.holder.transform, out var holder);

      if(exists){ continue; }

      var bg2Prefab = bg2Prefabs.FirstOrDefault(p => p.name == scenes[i * (int)bg2Layer.moveRatio].bg2Type);

      var newBg2 = GameObject.Instantiate(
        bg2Prefab,
        Vector3.zero,
        Quaternion.identity
      );
      newBg2.transform.SetParent(holder.transform, false);
      scenes[i].sceneTransforms.Add(holder.transform);
    }

    //bg 1
    //See above about bg2, but to match bg1 to the bg2 we have to adjust for the different scene ratios
    var maxBg1LayerIndex = GetMaxSceneIndexForLayer(bg1Layer.holder.transform);
    var neededBg1LayerIndex = Mathf.CeilToInt((stepCount) / bg1Layer.moveRatio);
    var minVisibleBg1LayerIndex = neededBg1LayerIndex - 1;
    for(int i = Mathf.Max(maxBg1LayerIndex, minVisibleBg1LayerIndex); i <= neededBg1LayerIndex; i++){
      var exists = CreateOrGetSceneHolder(i, bg1Layer.holder.transform, out var holder);

      if(exists){ continue; }

      var bg2SceneToMatch = scenes[i * (int)bg1Layer.moveRatio];
      // var bg2SceneToMatch = scenes[Mathf.FloorToInt((float)i / bg1Layer.moveRatio)];
      var bg1Prefab = bg1Prefabs.FirstOrDefault(pf => pf.name == bg2SceneToMatch.bg2Type);
      if(bg1Prefab != null){
        var newBg1 = GameObject.Instantiate(
          bg1Prefab,
          Vector3.zero,
          Quaternion.identity
        );
        newBg1.transform.SetParent(holder.transform, false);
        scenes[i].sceneTransforms.Add(holder.transform);
      }else{
        Debug.LogWarning("Could not generate bg1 from bg2 type " + newScene.bg2Type);
      }
    }


    //ground
    var maxGroundLayerIndex = GetMaxSceneIndexForLayer(groundLayer.holder.transform);
    var groundHolderExists = CreateOrGetSceneHolder(stepCount, groundLayer.holder.transform, out var groundHolder);

    if(maxGroundLayerIndex <= stepCount && !groundHolderExists){
      var groundPrefab = groundPrefabs.FirstOrDefault(pf => pf.name == newScene.bg2Type);
      if(groundPrefab != null){
        var newGround = GameObject.Instantiate(
          groundPrefab,
          Vector3.zero,
          Quaternion.identity
        );
        newGround.transform.SetParent(groundHolder.transform, false);
        newScene.sceneTransforms.Add(groundHolder.transform);
      }else{
        Debug.LogWarning("Could not generate ground from bg2 type " + newScene.bg2Type);
      }


      //ground food
      GameObject sceneGroundFoodHolder = null;
      foreach(var food in foodToName){
        if(newSceneData.groundFood.HasFlag(food.Key)){
          if(sceneGroundFoodHolder == null){
            CreateOrGetSceneHolder(stepCount, groundFoodLayer.holder.transform, out sceneGroundFoodHolder);
          }

          //Try to use the environment specific food if it exists
          var foodPrefab = loader.Load<GameObject>(string.Format("Prefabs/4/ground_food/{0}/{1}", newScene.bg2Type, food.Value));
          if(foodPrefab == null){
            foodPrefab = loader.Load<GameObject>(string.Format("Prefabs/4/ground_food/{0}", food.Value));
          }

          var newGroundFood = GameObject.Instantiate(
            foodPrefab,
            Vector3.zero,
            Quaternion.identity
          );
          newGroundFood.transform.SetParent(sceneGroundFoodHolder.transform, false);
          newScene.sceneTransforms.Add(sceneGroundFoodHolder.transform);

          newGroundFood.name = food.Value;
        }
      }

      //Clouds
      GameObject sceneCloudHolder = null;
      var numClouds = Random.Range(0, 4);
      for(var c = 0; c < numClouds; c++){
        if(sceneCloudHolder == null){
          CreateOrGetSceneHolder(stepCount, cloudsLayer.holder.transform, out sceneCloudHolder);
        }
        var newCloud = GameObject.Instantiate(
          cloudPrefabs[Random.Range(0, cloudPrefabs.Length)],
          new Vector3(Random.Range(-sceneSize / 2f, sceneSize / 2f), Random.Range(0.8f, 3f), 0),
          Quaternion.identity
        );
        newCloud.transform.SetParent(sceneCloudHolder.transform, false);
        newScene.sceneTransforms.Add(sceneCloudHolder.transform);
      }

    }

    //foreground
    //See above about bg2, but to match bg1 to the bg2 we have to adjust for the different scene ratios
    //Also have to cap at the number of scenes so the last couple won't have foreground elements because of the array bounds
    var neededForeLayerIndex = Mathf.CeilToInt((stepCount) / foregroundLayer.moveRatio);
    var minVisibleForeLayerIndex = neededForeLayerIndex - 1;
    for(int i = Mathf.Max(stepCount, minVisibleForeLayerIndex); i <= Mathf.Min(scenes.Count - 1, neededForeLayerIndex); i++){
      var exists = CreateOrGetSceneHolder(i, foregroundLayer.holder.transform, out var holder);
      if(exists){ continue; }

      var bg2SceneToMatch = scenes[Mathf.RoundToInt((float)i * foregroundLayer.moveRatio)];
      var foregroundPrefab = foregroundPrefabs.FirstOrDefault(pf => pf.name == bg2SceneToMatch.bg2Type);
      if(foregroundPrefab != null){
        var newBg1 = GameObject.Instantiate(
          foregroundPrefab,
          Vector3.zero,
          Quaternion.identity
        );
        newBg1.transform.SetParent(holder.transform, false);
        scenes[i].sceneTransforms.Add(holder.transform);
      }else{
        Debug.LogWarning("Could not generate foreground from bg2 type " + newScene.bg2Type);
      }
    }
  }

  CreatureData CreateNewEnemyCreature(int stepCount){
    var newCreatureData = new CreatureData();
    newCreatureData.Reset();

    newCreatureData.food = Random.Range(20, 90);

    //Give the enemy some random mods based on how many steps we're in and how many saved creature mods the player has
    var modBonus = stageFourData.savedCreatureMods != null ?
      Mathf.RoundToInt((float)stageFourData.savedCreatureMods.Count / stageRules.StageFourRules.enemyUpgradeInterval) :
      0;
    var enemyModCount = Mathf.RoundToInt((float)stepCount / stageRules.StageFourRules.enemyUpgradeInterval) + 1 + modBonus;
    for(int u = 0; u < enemyModCount; u++){
      //Make the mods be repeatably random for this mapSeed (the run you're on) and the particular step
      var availMods = newCreatureData.getAvailableMods(
        stageFourData,
        stepCount,
        stageFourData.mapSeed + stepCount + u,
        1,
        true
      );
      if(availMods.Length > 0){
        newCreatureData.AddMod(availMods[0]);
      }
    }
    return newCreatureData;
  }

  void RepositionSceneBasedOnScreenSize(){
    //TODO: inside each layer create a new transform for each scene and save on the scene obj
    //That way objects can be positioned relative within them, and the below can just reposition the scene transforms based on scene size

    for(var i = 0; i < scenes.Count; i++){
      var scene = scenes[i];
      foreach(var t in scene.sceneTransforms){
        t.localPosition = t.localPosition.SetX(i * sceneSize);
      }
    }

    //reposition enemy, assuming one has spawned.
    for(var i = 0; i < enemiesHolder.transform.childCount; i++){
      var child = enemiesHolder.transform.GetChild(i);
      child.localPosition = child.localPosition.SetX(stageFourData.sceneMoveCount * sceneSize + enemyOffset);
    }

  }

  //Assume this is always for current scene
  public void RemoveGroundFood(CFoodType foodType, Creature creatureEaten){
    currentScene.data.groundFood = (currentScene.data.groundFood & (~foodType));

    foreach(var foodName in CreatureSceneManager.foodToName){
      if((foodType & foodName.Key) != 0 ){
        RemoveGroundFoodDisplay(foodName.Value, stageFourData.sceneMoveCount);
      }
    }

    if(creatureEaten != null){
      Destroy(creatureEaten.gameObject);
      currentScene.data.enemyCreatureData = null;
    }
  }

  //PERF: find a better way to keep track of these so we don't have to do Find's
  void RemoveGroundFoodDisplay(string foodName, int sceneIndex, bool ignoreError = false){
    var sceneGroundFood = groundFoodLayer.holder.transform.Find(sceneIndex.ToString());
    if(sceneGroundFood == null){
      if(!ignoreError){
        Debug.LogWarning("Can't find any ground food at scene index " + sceneIndex);
      }
      return;
    }
    var foodTransform = sceneGroundFood.Find(foodName);
    if(foodTransform != null){
      bool hasEatables = false;
      for(var c = foodTransform.childCount - 1; c >= 0; c--){
        var child = foodTransform.GetChild(c);
        if(child.tag == "Eatable"){
          hasEatables = true;

          Destroy(child.gameObject);
        }
      }

      if(!hasEatables){
        currentScene.sceneTransforms.Remove(foodTransform);
        Destroy(foodTransform.gameObject);
      }
    }else if(!ignoreError){
      Debug.LogWarning($"Can't find {foodName} at scene index {sceneIndex}");
    }
  }

  public class CreatureScene
  {
    public CreatureSceneData data;

    public Creature enemyCreature {get;set;}
    public Creature mate {get;set;}

    public List<Transform> sceneTransforms = new List<Transform>();

    public string bg2Type;
  }


  static Dictionary<CFoodType, string> foodToName = new Dictionary<CFoodType, string>(){
    {CFoodType.Grass, "grass"},
    {CFoodType.Bushes, "bushes"},
    {CFoodType.TallTrees, "tall_trees"},
  };
}

[System.Serializable]
public class CreatureSceneLayer {
  public GameObject holder;
  //Note that the bg layers need to be integer multiples
  public float moveRatio;
  public string name;

  public float autoScrollSpeed;

  private float scrollOffset;
  public float ScrollOffset {
    get { return scrollOffset; }
    set { scrollOffset = value; }
  }
}

[System.Flags]
[System.Serializable]
public enum CFoodType {
  None      = 0,
  Grass     = 1 << 0,
  Bushes    = 1 << 1,
  TallTrees = 1 << 2,
}

[System.Serializable]
public class CreatureSceneData
{
  public CFoodType groundFood {get;set;}
  public CreatureData enemyCreatureData {get;set;}
  public bool rolledForEnemyCreature = false;

  public CreatureData mateData {get;set;}
  public bool rolledForMate = false;

  public float currentMateProbability = 0f;

  public bool canUpgrade = false;
  public bool usedUpgrade = false;
}