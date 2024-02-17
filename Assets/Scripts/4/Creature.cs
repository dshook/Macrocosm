using System.Collections;
using System.Collections.Generic;
using strange.extensions.mediation.impl;
using UnityEngine;
using TMPro;
using System.Linq;
using Anima2D;

[System.Serializable]
public class CreatureData{
  public int food; //0-100 fed
  public int power;
  public int speed;
  public int maxFood;
  public int foodConsumption;
  public float eatBonus;
  public int childFoodConsumptionChange;
  public float enemyPctChanceBonus;
  public float matePctChanceBonus;
  public float wheelSpeedPctChange;
  public int theyLoseFoodBonus;
  public int mateSliceBonus;

  public bool dead = false;

  public List<CreatureModifierId> modifiers = new List<CreatureModifierId>();

  public List<ChildCreatureData> children = new List<ChildCreatureData>();

  //Base stats for all creatures
  public void Reset(){
    dead = false;
    food = 70;
    power = 1;
    speed = 5;
    foodConsumption = 10; //only the base, use the function for everything calculated
    eatBonus = 0f;
    maxFood = 100;

    childFoodConsumptionChange = 0;
    enemyPctChanceBonus = 0f;
    matePctChanceBonus = 0f;
    wheelSpeedPctChange = 0f;
    theyLoseFoodBonus = 0;
    mateSliceBonus = 0;

    modifiers.Clear();
    children.Clear();

    modifiers.Add(CreatureModifierId.Omnivore);
    modifiers.Add(CreatureModifierId.SmallBody);
  }

  public void ChangeFood(int amt){
    food = Mathf.Clamp(food + amt, 0, maxFood);
  }

  public CreatureModifier[] getAvailableMods(
    StageFourDataModel stageFourData,
    int moveCount,
    int randomSeed,
    int limit,
    bool forEnemy = false,
    bool forMate = false
  ){
    Random.State originalRandomState = Random.state;
    Random.InitState(randomSeed);

    var filteredMods = CreatureModifier.allModifiers
      .Where(modkv => {
        var mod = modkv.Value;
        return (mod.repeatable || !modifiers.Any(cm => cm == mod.id))
        && (mod.type != CreatureModifierType.StageProgression || stageFourData.totalChildCount > 0) // roundabout way of not showing stage 5 bonuses until it's unlocked
        && (!mod.appearsAfter.HasValue || moveCount >= mod.appearsAfter.Value || (mod.reduceAppearsAfterByRuns && moveCount > mod.appearsAfter - stageFourData.runsCompleted))
        && (mod.prereqMods == null || mod.prereqMods.All(pmod => modifiers.Any(cm => cm == pmod) ) )
        && (mod.excludeMods == null || !mod.excludeMods.Any(pmod => modifiers.Any(cm => cm == pmod) ) )
        //make sure we don't already have a mod that replaces this one
        && (!modifiers.Any(cm => CreatureModifier.allModifiers[cm].replaceMods != null && CreatureModifier.allModifiers[cm].replaceMods.Contains(mod.id)))
        && ((forEnemy || forMate) || !mod.restrictFromPlayer)
        && (!forEnemy || !mod.restrictFromEnemies)
        && (!forMate || !mod.restrictFromMates);
      })
      .ToArray();

    filteredMods.Shuffle();
    Random.state = originalRandomState;

    // Debug.Log("Possible mods: " + string.Join("\n", filteredMods.Select(t => t.Key)));

    //Find any guaranteed mods in the list and put them to the front
    var modGuaranteedIndex = System.Array.FindIndex(filteredMods, modkv =>
      modkv.Value.guaranteedAfterRuns != null &&
      stageFourData.runsCompleted >= modkv.Value.guaranteedAfterRuns.Value
    );
    if(modGuaranteedIndex > 0){
      //swap the guaranteed mod into the 0th slot
      var modtoSwap = filteredMods[0];
      filteredMods[0] = filteredMods[modGuaranteedIndex];
      filteredMods[modGuaranteedIndex] = modtoSwap;
    }

    // filteredMods[0] = new KeyValuePair<CreatureModifierId, CreatureModifier>(
    //   CreatureModifierId.LargeBody,
    //   CreatureModifier.allModifiers[CreatureModifierId.LargeBody]
    // );

    return filteredMods.Select(k => k.Value).Take(limit).ToArray();
  }

  public void AddMod(CreatureModifier modifier, bool skipPrereqCheck = false)
  {
    //Can't add a duplicate of a non repeatable mod
    if(!modifier.repeatable && modifiers.Contains(modifier.id)){
      return;
    }

    //Check to make sure prereqs are met
    if(!skipPrereqCheck && modifier.prereqMods != null){
      if(modifier.prereqMods.Any(m => !modifiers.Contains(m))){
        return;
      }
    }

    if(modifier.replaceMods != null){
      foreach(var replaceMod in modifier.replaceMods){
        if(modifiers.Contains(replaceMod)){
          RemoveMod(CreatureModifier.allModifiers[replaceMod]);
        }
      }
    }

    //Remove any mods that are excluded by this new mod
    var incompatibleMods = GetIncompatibleMods(modifier.id);
    foreach(var incompat in incompatibleMods){
      RemoveMod(CreatureModifier.allModifiers[incompat]);
    }

    power           = Mathf.Max(1, power + modifier.powerChange);
    speed           = Mathf.Max(0, speed + modifier.speedChange);
    foodConsumption = Mathf.Max(1, foodConsumption + modifier.foodConsumptionChange);
    eatBonus        = Mathf.Max(0f, eatBonus + modifier.eatBonusChange);
    maxFood         = Mathf.Max(1, maxFood + modifier.maxFoodChange);
    food            = Mathf.Clamp(food, 0, maxFood);
    childFoodConsumptionChange += modifier.childFoodConsumptionChange;
    enemyPctChanceBonus += modifier.enemyPctChanceChange;
    matePctChanceBonus  += modifier.matePctChanceChange;
    wheelSpeedPctChange += modifier.wheelSpeedPctChange;
    theyLoseFoodBonus   += modifier.theyLoseFoodChange;
    mateSliceBonus      += modifier.mateSliceBonusChange;

    modifiers.Add(modifier.id);
  }

  //Get the list of mods that don't work with a new modifier
  //Note this doesn't handle the cascading/recursive case but hopefully shouldn't need to
  public List<CreatureModifierId> GetIncompatibleMods(CreatureModifierId newModId){
    return modifiers.Where(modId => {
      var mod = CreatureModifier.allModifiers[modId];
      return mod.excludeMods != null && mod.excludeMods.Contains(newModId);
    }).ToList();
  }

  public void RemoveMod(CreatureModifier modifier){
    power           = Mathf.Max(1, power - modifier.powerChange);
    speed           = Mathf.Max(0, speed - modifier.speedChange);
    foodConsumption = Mathf.Max(1, foodConsumption - modifier.foodConsumptionChange);
    eatBonus        = Mathf.Max(0f, eatBonus - modifier.eatBonusChange);
    maxFood         = Mathf.Max(1, maxFood - modifier.maxFoodChange);
    food            = Mathf.Clamp(food, 0, maxFood);
    childFoodConsumptionChange -= modifier.childFoodConsumptionChange;
    enemyPctChanceBonus -= modifier.enemyPctChanceChange;
    matePctChanceBonus  -= modifier.matePctChanceChange;
    wheelSpeedPctChange -= modifier.wheelSpeedPctChange;
    theyLoseFoodBonus   -= modifier.theyLoseFoodChange;
    mateSliceBonus      -= modifier.mateSliceBonusChange;

    modifiers.Remove(modifier.id);
  }

  //Business logic for speed and ambush to see if a creature can run from another
  public static bool CreatureCanRun(CreatureData antagonist, CreatureData tryingToRun){
    if(tryingToRun.dead){
      return false;
    }
    if(antagonist.dead){
      return true;
    }

    if( antagonist.modifiers.Any(m => m == CreatureModifierId.Ambush) && antagonist.power > tryingToRun.speed){
      return false;
    }

    return tryingToRun.speed > antagonist.speed;
  }

  public int FoodConsumedPerStep(StageFourRulesProps stageFourRules) {
    var childFoodAmount = Mathf.Max(0, children.Count * (stageFourRules.childFoodPerStep + childFoodConsumptionChange));
    return foodConsumption + childFoodAmount;
  }

  //find some random modifiers to pass to the next generation, that satisfy reqs
  public List<CreatureModifierId> GetModsToPassOn(StageFourDataModel stageFourData, bool procreated){
    //Create a new data holder for the mods to pass on that can check validity of the combinations added
    var creatureDataForModsToPass = new CreatureData();


    //pass all the persistent mods
    foreach(var modId in modifiers){
      var mod = CreatureModifier.allModifiers[modId];
      if(!mod.persistent || mod.restrictFromPlayer){ continue; }

      creatureDataForModsToPass.AddMod(mod, true);
    }

    //If they didn't procreate they only get the persistent mods and the previous generations non persistent mods
    if(!procreated){
      if(stageFourData.savedCreatureMods != null){
        foreach(var mod in
          stageFourData.savedCreatureMods
          .Where(savedModId => !CreatureModifier.allModifiers[savedModId].persistent)
          .Select(savedModId => CreatureModifier.allModifiers[savedModId])
        ){
          creatureDataForModsToPass.AddMod(mod, true);
        }
      }
    }else{

      //find a random mod from the childrenBonusMods if applicable
      if(stageFourData != null && stageFourData.childrenBonusMods.Count > 0){
        creatureDataForModsToPass.AddMod( CreatureModifier.allModifiers[stageFourData.childrenBonusMods.Shuffle().First()] );
      }

      //Add all valid mods the creature has when procreating
      foreach(var mod in
        modifiers
          .Where(modId => {
            var mod = CreatureModifier.allModifiers[modId];

            return !mod.persistent
            && modId != CreatureModifierId.SmallBody
            && modId != CreatureModifierId.Omnivore;
          })
          .Select(modId => CreatureModifier.allModifiers[modId])
      ){
        creatureDataForModsToPass.AddMod(mod);
      }
    }

    return creatureDataForModsToPass.modifiers;
  }
}

[ExecuteInEditMode]
public class Creature : View {

  [Inject] public StageRulesService stageRules { get; set; }
  [Inject] public ResourceLoaderService loader {get; set;}

  public CreatureData data;

  [Tooltip("Adjust the size of the body display for all creatures")]
  public float bodyDisplayScaleMultiplier = 1f;

  public TextMeshPro speedText;
  public TextMeshPro sizeText;
  public GameObject modsDisplay;
  public GameObject bodyDisplay;
  public GameObject body;
  public GameObject modDisplayPrefab;
  public GameObject childrenGO;

  public Animator animator;

  public List<ChildCreature> children = new List<ChildCreature>();

  CreatureBody creatureBody;

  //synced from manager
  public bool walking;
  public bool fighting;
  public bool foraging;
  public bool mating;
  public bool isPlayerCreature;

  public void Reset(){
    LeanTween.cancel(body);
    body.transform.rotation = Quaternion.identity;
    UpdateDisplay(true);
    childrenGO.transform.DestroyChildren(); //actually destroy all the children
    children.Clear();
  }

  void Update () {
    CheckDeath();

    UpdateAnimation();
  }

  public void UpdateAnimation(){
    if(animator != null){
      animator.SetBool("walking", walking);
      animator.SetBool("dead", data.dead);
    }
    if(children != null && children.Count > 0){
      foreach(var child in children){
        if(child.animator != null){
          child.animator.SetBool("walking", walking);
          child.animator.SetBool("dead", data.dead);
        }
      }
    }
  }

  public void MoveForward()
  {
    data.ChangeFood(-data.FoodConsumedPerStep(stageRules.StageFourRules));
    foreach(var child in children){
      child.data.age++;
    }
    CheckDeath();
  }

  public struct EatReturnVal{
    public int foodEaten;
    public CFoodType foodTypeEaten;
    public Creature creatureEaten;
  }

  public EatReturnVal Eat(CFoodType availableFoods, Creature otherCreature)
  {
    var foodAmt = 0;
    var foodTypeEaten = CFoodType.None;
    Creature creatureEaten = null;

    if(otherCreature != null && !data.modifiers.Any(m => m == CreatureModifierId.Herbivore)){
      foodAmt += 10 + (otherCreature.data.power * 10);
      creatureEaten = otherCreature;
      if(data.modifiers.Any(m => m == CreatureModifierId.Carnivore)){
        foodAmt += 10;
      }
    }
    else if((availableFoods & CFoodType.Grass) != 0 && !data.modifiers.Any(m => m == CreatureModifierId.Carnivore)){
      foodAmt += 15;
      foodTypeEaten = CFoodType.Grass;
    }
    else if((availableFoods & CFoodType.Bushes) != 0 && data.modifiers.Any(m => m == CreatureModifierId.Herbivore)){
      foodAmt += 25;
      foodTypeEaten = CFoodType.Bushes;
    }
    else if((availableFoods & CFoodType.TallTrees) != 0 && data.modifiers.Any(m => m == CreatureModifierId.LongNeck || m == CreatureModifierId.Climbing)){
      foodAmt += 35;
      foodTypeEaten = CFoodType.TallTrees;
    }

    foodAmt = foodAmt + (int)Mathf.Ceil(data.eatBonus * (float)foodAmt);
    data.ChangeFood(foodAmt);

    return new EatReturnVal(){
      foodEaten = foodAmt,
      foodTypeEaten = foodTypeEaten,
      creatureEaten = creatureEaten
    };
  }

  public EatReturnVal Forage(){

    //End up with 1 food after foraging and moving
    var foodAmt = data.FoodConsumedPerStep(stageRules.StageFourRules) + 1 - data.food;
    data.ChangeFood(foodAmt);

    return new EatReturnVal(){
      foodEaten = foodAmt,
      foodTypeEaten = CFoodType.None,
      creatureEaten = null
    };
  }

  public bool CanEat(CreatureSceneData cScene){
    if(cScene.enemyCreatureData != null){
      if(!cScene.enemyCreatureData.dead){
        return false;
      }else{
        //Only non herbivores can eat dead creatures
        if(!data.modifiers.Any(m => m == CreatureModifierId.Herbivore)){
          return true;
        }
      }
    }

    if((cScene.groundFood & CFoodType.Grass) != 0 && !data.modifiers.Any(m => m == CreatureModifierId.Carnivore)){
      return true;
    }

    if((cScene.groundFood & CFoodType.Bushes) != 0 && data.modifiers.Any(m => m == CreatureModifierId.Herbivore)){
      return true;
    }

    if((cScene.groundFood & CFoodType.TallTrees) != 0 && data.modifiers.Any(m => m == CreatureModifierId.LongNeck || m == CreatureModifierId.Climbing)){
      return true;
    }

    return false;
  }

  public void Fight(Creature otherCreature){
    bool success = false;
    if(data.power > otherCreature.data.power){
      success = true;
    }else if(data.power == otherCreature.data.power){
      success = Random.Range(0f, 1f) > 0.5f;
    }

    if(success){
      otherCreature.Die();
    }else{
      Die();
    }
  }

  void CheckDeath(){
    if(!data.dead && data.food <= 0){
      Die();
    }
  }

  public void Die(bool skipAnimation = false){
    data.dead = true;
    // LeanTween.rotate(body, new Vector3(0, 0, -180f), skipAnimation ? 0 : 1f).setEase(LeanTweenType.easeInOutExpo);
  }

  public void ShowCombatDisplay(){
    speedText.text = "Speed " + data.speed;
    sizeText.text = "Power " + data.power;
    modsDisplay.transform.DestroyChildren();

    foreach(var modId in data.modifiers){
      var mod = CreatureModifier.allModifiers[modId];
      if(!mod.showInCombat){ continue; }
      var modDisp = GameObject.Instantiate(
        modDisplayPrefab,
        Vector3.zero,
        Quaternion.identity
      );
      modDisp.transform.SetParent(modsDisplay.transform, false);

      var title = modDisp.GetComponentInChildren<TextMeshPro>();

      title.text = mod.name;
    }

    speedText.gameObject.SetActive(true);
    sizeText.gameObject.SetActive(true);
    modsDisplay.SetActive(true);
  }

  public void HideCombatDisplay(){
    speedText.gameObject.SetActive(false);
    sizeText.gameObject.SetActive(false);

    modsDisplay.SetActive(false);
  }

  //Specialized version of show combat display
  public void ShowMateDisplay(CreatureModifier extraMod){
    modsDisplay.transform.DestroyChildren();

    var modDisp = GameObject.Instantiate(
      modDisplayPrefab,
      Vector3.zero,
      Quaternion.identity
    );
    modDisp.transform.SetParent(modsDisplay.transform, false);

    var title = modDisp.GetComponentInChildren<TextMeshPro>();

    title.text = extraMod.name;
    modsDisplay.SetActive(true);
  }

  public void HideMateDisplay(){
    HideCombatDisplay();
  }

  const string partsFolder = "Prefabs/4/parts/";

  public void UpdateDisplay(bool skipAnimation){
    if(loader == null){
      Debug.LogWarning("Loader null for creature");
    }

    if(data == null){
      Debug.LogError("null data for creature");
      return;
    }

    if(data.modifiers == null){
      Debug.LogError("null modifiers for creature");
      return;
    }

    //Update body
    //Jank ass way to construct the body part name but it's what I got for now
    var bodyMod = data.modifiers.FirstOrDefault(m => CreatureModifier.allModifiers[m].type == CreatureModifierType.Body);
    if(bodyMod == CreatureModifierId.None){
      bodyMod = CreatureModifierId.SmallBody;
    }
    string bodyBasePartPath = CreatureModifier.allModifiers[bodyMod].partPath;
    string bodyPartPath = bodyBasePartPath;

    if(data.modifiers.Contains(CreatureModifierId.Bipedalism)){
      bodyPartPath += "/biped";
    }else{
      bodyPartPath += "/quadraped";
    }

    var bodyModifier = string.Empty;
    if(data.modifiers.Contains(CreatureModifierId.LongerNeck) && !data.modifiers.Contains(CreatureModifierId.Bipedalism)){
      bodyModifier = "tall ";
    }

    var bodyPartName = string.Format("{0}/{1}body", bodyPartPath, bodyModifier);

    SetUpBody(bodyPartName, bodyPartPath);

    SetupBodyModifiers(bodyPartPath, bodyBasePartPath);

    UpdateIK();

    if(isPlayerCreature){
      SetDisplaySortLayer(bodyDisplay, "Layer3");
    }
  }

  void SetUpBody(string bodyPartName, string bodyPartPath){
    if(bodyDisplay.name != bodyPartName){

      var bodyDisplayPrefab = loader.Load<GameObject>(partsFolder + bodyPartName);
      DestroyImmediate(bodyDisplay);

      bodyDisplay = GameObject.Instantiate(bodyDisplayPrefab);
      bodyDisplay.transform.SetParent(transform, false);
      bodyDisplay.transform.localPosition = Vector3.zero;
      bodyDisplay.name = bodyPartName;

      animator = bodyDisplay.GetComponentInChildren<Animator>();
    }
    body = bodyDisplay.FindInChildren("body");
    bodyDisplay.transform.localScale = bodyDisplay.transform.localScale * bodyDisplayScaleMultiplier;

    creatureBody = bodyDisplay.GetComponent<CreatureBody>();

    //update head
    var headPartId = data.modifiers.FirstOrDefault(modId => CreatureModifier.allModifiers[modId].type == CreatureModifierType.Head);
    if(headPartId == CreatureModifierId.None){
      headPartId = CreatureModifierId.Omnivore;
    }

    var headPart = CreatureModifier.allModifiers[headPartId];

    if(headPart.partPath != null){
      var fullHeadPartPath = string.Format("Art/stage4/parts/{0}/{1}", bodyPartPath, headPart.partPath);
      creatureBody.headRenderer.sprite = loader.Load<Sprite>(fullHeadPartPath);
    }
  }

  void SetupBodyModifiers(string bodyPartPath, string bodyBasePartPath){
    //create other modifier parts
    //Tracking all these individual bools might be a bad idea but going with it for now
    //Need to make sure to handle removing parts that we no longer have either after upgrading or dying and resetting
    bool hasBackPart = false;
    bool hasTailPart = false;
    bool hasHeadPart = false;
    foreach(var modId in data.modifiers){
      if(!CreatureModifier.allModifiers.ContainsKey(modId)){
        continue; //Check for obsolete mods
      }
      var mod = CreatureModifier.allModifiers[modId];
      if(mod.type == CreatureModifierType.Body || mod.type == CreatureModifierType.Head){
        continue;
      }
      if(string.IsNullOrEmpty(mod.partPath)){
        continue;
      }

      Transform partParent = null;
      switch(mod.type){
        case CreatureModifierType.Tail:
          partParent = creatureBody.tailPartHolder;
          hasTailPart = true;
          break;
        case CreatureModifierType.Back:
          partParent = creatureBody.backPartHolder;
          hasBackPart = true;
          break;
        case CreatureModifierType.HeadAccessory:
          partParent = creatureBody.headPartHolder;
          hasHeadPart = true;
          break;
      }

      if(partParent == null){
        Debug.LogWarning("Couldn't find part parent for mod type " + mod.type);
        continue;
      }

      //only allow one mod per parent right now so check to see if the mod has already been created
      if(partParent.childCount > 0){
        if(partParent.GetChild(0).name == mod.name){
          //already set up so can continue
          continue;
        }else{
          partParent.DestroyChildren();
        }
      }

      var fullPartPathsToTry = new string[]{
        string.Format("{0}{1}/{2}", partsFolder, bodyPartPath, mod.partPath),
        string.Format("{0}{1}/{2}", partsFolder, bodyBasePartPath, mod.partPath),
        partsFolder + mod.partPath
      };

      GameObject newPartPrefab = null;
      foreach(var partPath in fullPartPathsToTry){
        newPartPrefab = loader.Load<GameObject>(partPath);
        if(newPartPrefab != null){
          break;
        }
      }

      if(newPartPrefab == null){
        Debug.LogWarning("Couldn't find part prefab " + mod.type);
        continue;
      }

      var newPart = GameObject.Instantiate(newPartPrefab);
      newPart.transform.SetParent(partParent, false);
      newPart.transform.localPosition = Vector3.zero;
      newPart.name = mod.name;
    }
    if(!hasBackPart){
      creatureBody.backPartHolder.DestroyChildren(!Application.isPlaying);
    }
    if(!hasTailPart){
      creatureBody.tailPartHolder.DestroyChildren(!Application.isPlaying);
    }
    if(!hasHeadPart){
      creatureBody.headPartHolder.DestroyChildren(!Application.isPlaying);
    }

  }

  //Have to manually update the IK's for some reason otherwise the limbs are all splayed
  void UpdateIK(){
    var ikLimbs = bodyDisplay.GetComponentsInChildren<IkLimb2D>();
    var ikCCD2D = bodyDisplay.GetComponentsInChildren<IkCCD2D>();

    foreach(var t in ikLimbs){
      t.UpdateIK();
    }
    foreach(var t in ikCCD2D){
      t.UpdateIK();
    }
  }

  //Bump up all the sprite renderer's sort layers for the player creature
  void SetDisplaySortLayer(GameObject bodyDisplay, string layer){
    var renderers = bodyDisplay.GetComponentsInChildren<SpriteRenderer>();
    foreach(var rend in renderers){
      rend.sortingLayerName = layer;
    }
  }

  public ChildCreatureData HaveAKid(ChildCreatureData savedChildData = null, CreatureModifierId? bonusModifierFromMate = null){
    var newKid = GameObject.Instantiate(
      bodyDisplay,
      Vector3.zero,
      Quaternion.identity,
      childrenGO.transform
    );
    // newKid.transform.SetParent(childrenGO.transform, false);
    newKid.transform.localScale = Vector3.zero;
    newKid.transform.localPosition = Creature.childrenPositions[children.Count % childrenPositions.Count];

    LeanTween.scale(newKid, Vector3.one * 0.4f, 1.5f).setEase(LeanTweenType.easeOutBack);
    SetDisplaySortLayer(newKid, "Layer4");

    if(savedChildData == null){

      savedChildData = new ChildCreatureData(){
        age = 0,
        powerStolen = 0, //deprecated
        bonusModifier = bonusModifierFromMate
      };
    }

    children.Add(new ChildCreature(){
      childGO = newKid,
      data = savedChildData,
      animator = newKid.GetComponentInChildren<Animator>()
    });

    return savedChildData;
  }

  public void ReleaseKid(ChildCreature child){

    LeanTween.move(child.childGO, child.childGO.transform.position.AddX(14), 3f).setEase(LeanTweenType.easeInOutSine);
    children.Remove(child);
    data.power += child.data.powerStolen;
    data.children.Remove(child.data);
    StartCoroutine(FinishReleasing(child));
  }

  IEnumerator FinishReleasing(ChildCreature child){
    yield return new WaitForSeconds(3);
    Destroy(child.childGO);

    //move the remaining children to their new place in the pecking order
    for(int i = 0; i < children.Count; i++){
      var newPos = Creature.childrenPositions[i % childrenPositions.Count];
      LeanTween.moveLocal(children[i].childGO, newPos, 1f).setEase(LeanTweenType.easeInOutSine);
    }
  }

  static List<Vector3> childrenPositions = new List<Vector3>(){
    new Vector3(-0.86f, -0.58f, 0f),
    new Vector3(-1.16f, -0.18f, 0f),
    new Vector3(-1.26f, -0.75f, 0f),
    new Vector3(-1.56f, -0.5f, 0f),
  };

  public enum DeathReason{
    Starvation,
    Combat,
    OldAge
  }
}

public class ChildCreature
{
  public GameObject childGO {get;set;}
  public ChildCreatureData data {get; set;}
  public Animator animator {get; set;}
}

[System.Serializable]
public class ChildCreatureData {
  public int age {get;set;}
  public CreatureModifierId? bonusModifier {get; set;}

  //Deprecated but leaving for serialization
  public int powerStolen {get; set;}
}