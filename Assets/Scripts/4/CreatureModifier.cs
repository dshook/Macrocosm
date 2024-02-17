using System.Collections.Generic;

public class CreatureModifier
{
  public string name = string.Empty;
  public string descrip = string.Empty;
  public string partPath = null;
  public bool showInCombat = false;
  public CreatureModifierType type = CreatureModifierType.General;
  public CreatureModifierId id = CreatureModifierId.None;

  public int powerChange = 0;
  public int speedChange = 0;
  public int foodConsumptionChange = 0;
  public float eatBonusChange = 0f;
  public int childFoodConsumptionChange = 0;
  public int maxFoodChange = 0;
  public float enemyPctChanceChange = 0f;
  public float matePctChanceChange = 0f;
  public float wheelSpeedPctChange = 0f;
  public int theyLoseFoodChange = 0;
  public int mateSliceBonusChange = 0;
  public int tribePopulationBonus = 0;

  public int? appearsAfter = null; //have to be this many steps through the stage to see it
  public int? guaranteedAfterRuns = null; //After completing this many runs, guarantee the mod gets shown
  public bool reduceAppearsAfterByRuns = false; //Reduce the appears after by the number of runs completed

  public bool repeatable = false;
  public bool persistent = false; //mod always gets passed to the next generation
  public bool restrictFromEnemies = false; //When set to true prevent enemies from getting this mod
  public bool restrictFromMates = false; //When set to true prevent mates from getting this mod
  public bool restrictFromPlayer = false; //Prevent the player from getting this mod or passing it on

  public CreatureModifierId[] prereqMods; //must have all of the mods to get this mod
  public CreatureModifierId[] excludeMods; //can't have any of the mods to get this mod
  public CreatureModifierId[] replaceMods; //remove all these mods from the creature

  public static Dictionary<CreatureModifierId, CreatureModifier> allModifiers = new Dictionary<CreatureModifierId, CreatureModifier>{
    //General
    {
      CreatureModifierId.TribalNature,
      new CreatureModifier(){
        name = "Tribal Nature",
        descrip = "Your tribes population takes one less wave to grow. Persistent",
        type = CreatureModifierType.StageProgression,
        id = CreatureModifierId.TribalNature,
        persistent = true,
        restrictFromEnemies = true,
        restrictFromMates = true,
        appearsAfter = 17,
        guaranteedAfterRuns = 5,
        reduceAppearsAfterByRuns = true,
        tribePopulationBonus = 1,
      }
    },
    {
      CreatureModifierId.TribalNature2,
      new CreatureModifier(){
        name = "Tribal Nature II",
        descrip = "Your tribes population takes one less wave to grow. Persistent",
        type = CreatureModifierType.StageProgression,
        id = CreatureModifierId.TribalNature2,
        persistent = true,
        restrictFromEnemies = true,
        tribePopulationBonus = 1,
        prereqMods = new CreatureModifierId[] { CreatureModifierId.TribalNature },
        replaceMods = new CreatureModifierId[] { CreatureModifierId.TribalNature },
      }
    },
    {
      CreatureModifierId.TribalNature3,
      new CreatureModifier(){
        name = "Tribal Nature III",
        descrip = "Your tribes population takes one less wave to grow. Persistent",
        type = CreatureModifierType.StageProgression,
        id = CreatureModifierId.TribalNature3,
        persistent = true,
        restrictFromEnemies = true,
        tribePopulationBonus = 1,
        prereqMods = new CreatureModifierId[] { CreatureModifierId.TribalNature2 },
        replaceMods = new CreatureModifierId[] {
          CreatureModifierId.TribalNature,
          CreatureModifierId.TribalNature2,
        },
      }
    },
    {
      CreatureModifierId.TribalNature4,
      new CreatureModifier(){
        name = "Tribal Nature IV",
        descrip = "Your tribes population takes one less wave to grow. Persistent",
        type = CreatureModifierType.StageProgression,
        id = CreatureModifierId.TribalNature4,
        persistent = true,
        restrictFromEnemies = true,
        tribePopulationBonus = 1,
        appearsAfter = 15,
        prereqMods = new CreatureModifierId[] { CreatureModifierId.TribalNature3 },
        replaceMods = new CreatureModifierId[] {
          CreatureModifierId.TribalNature,
          CreatureModifierId.TribalNature2,
          CreatureModifierId.TribalNature3,
        },
      }
    },
    {
      CreatureModifierId.TribalNature5,
      new CreatureModifier(){
        name = "Tribal Nature V",
        descrip = "Your tribes population takes one less wave to grow. Persistent",
        type = CreatureModifierType.StageProgression,
        id = CreatureModifierId.TribalNature5,
        persistent = true,
        restrictFromEnemies = true,
        tribePopulationBonus = 1,
        appearsAfter = 15,
        prereqMods = new CreatureModifierId[] { CreatureModifierId.TribalNature4 },
        replaceMods = new CreatureModifierId[] {
          CreatureModifierId.TribalNature,
          CreatureModifierId.TribalNature2,
          CreatureModifierId.TribalNature3,
          CreatureModifierId.TribalNature4
        },
      }
    },
    {
      CreatureModifierId.TribalNature6,
      new CreatureModifier(){
        name = "Tribal Nature VI",
        descrip = "Your tribes population takes one less wave to grow. Persistent",
        type = CreatureModifierType.StageProgression,
        id = CreatureModifierId.TribalNature6,
        persistent = true,
        restrictFromEnemies = true,
        tribePopulationBonus = 1,
        appearsAfter = 15,
        prereqMods = new CreatureModifierId[] { CreatureModifierId.TribalNature5 },
        replaceMods = new CreatureModifierId[] {
          CreatureModifierId.TribalNature,
          CreatureModifierId.TribalNature2,
          CreatureModifierId.TribalNature3,
          CreatureModifierId.TribalNature4,
          CreatureModifierId.TribalNature5
        },
      }
    },
    {
      CreatureModifierId.Scavenger,
      new CreatureModifier(){
        name = "Scavenger",
        descrip = "Find 10% more food when eating. Food Consumption +1",
        type = CreatureModifierType.General,
        id = CreatureModifierId.Scavenger,
        eatBonusChange = 0.10f,
        foodConsumptionChange = 1,
      }
    },
    {
      CreatureModifierId.Scavenger2,
      new CreatureModifier(){
        name = "Scavenger II",
        descrip = "Find 10% more food when eating. Food Consumption +2",
        type = CreatureModifierType.General,
        id = CreatureModifierId.Scavenger2,
        eatBonusChange = 0.20f,
        foodConsumptionChange = 3,
        prereqMods = new CreatureModifierId[] { CreatureModifierId.Scavenger },
        replaceMods = new CreatureModifierId[] { CreatureModifierId.Scavenger },
      }
    },
    {
      CreatureModifierId.NurturingParent,
      new CreatureModifier(){
        name = "Nurturing Parent",
        descrip = "Reduce food each child needs by 2.",
        type = CreatureModifierType.General,
        id = CreatureModifierId.NurturingParent,
        childFoodConsumptionChange = -2,
        restrictFromEnemies = true,
      }
    },
    {
      CreatureModifierId.NurturingParent2,
      new CreatureModifier(){
        name = "Nurturing Parent II",
        descrip = "Reduce food each child needs by 2.",
        type = CreatureModifierType.General,
        id = CreatureModifierId.NurturingParent2,
        childFoodConsumptionChange = -4,
        restrictFromEnemies = true,
        prereqMods = new CreatureModifierId[] { CreatureModifierId.NurturingParent },
        replaceMods = new CreatureModifierId[] { CreatureModifierId.NurturingParent }
      }
    },
    {
      CreatureModifierId.NurturingParent3,
      new CreatureModifier(){
        name = "Nurturing Parent III",
        descrip = "Reduce food each child needs by 2.",
        type = CreatureModifierType.General,
        id = CreatureModifierId.NurturingParent3,
        childFoodConsumptionChange = -6,
        restrictFromEnemies = true,
        prereqMods = new CreatureModifierId[] { CreatureModifierId.NurturingParent2 },
        replaceMods = new CreatureModifierId[] {
          CreatureModifierId.NurturingParent,
          CreatureModifierId.NurturingParent2
        }
      }
    },
    //Make sure to sync the text with the CreatureCanRun logic
    {
      CreatureModifierId.Ambush,
      new CreatureModifier(){
        name = "Ambush",
        descrip = "Creatures with speed less than your power can't run away",
        showInCombat = true,
        type = CreatureModifierType.General,
        id = CreatureModifierId.Ambush,
      }
    },
    {
      CreatureModifierId.Stubborn,
      new CreatureModifier(){
        name = "Stubborn",
        descrip = "Convert \"You run away\" to \"Win\" in combat.",
        showInCombat = true,
        appearsAfter = 10,
        type = CreatureModifierType.General,
        id = CreatureModifierId.Stubborn,
      }
    },
    {
      CreatureModifierId.Hunter,
      new CreatureModifier(){
        name = "Hunter",
        descrip = "Find 10% more enemy creatures. Food Consumption +1",
        restrictFromEnemies = true,
        type = CreatureModifierType.General,
        id = CreatureModifierId.Hunter,
        foodConsumptionChange = 1,
        enemyPctChanceChange = 0.1f,
        prereqMods = new CreatureModifierId[] { CreatureModifierId.Carnivore }
      }
    },
    {
      CreatureModifierId.Hunter2,
      new CreatureModifier(){
        name = "Hunter II",
        descrip = "Find 10% more enemy creatures. Food Consumption +2",
        restrictFromEnemies = true,
        type = CreatureModifierType.General,
        id = CreatureModifierId.Hunter2,
        foodConsumptionChange = 3,
        enemyPctChanceChange = 0.2f,
        prereqMods = new CreatureModifierId[] { CreatureModifierId.Carnivore, CreatureModifierId.Hunter },
        replaceMods = new CreatureModifierId[] { CreatureModifierId.Hunter }
      }
    },
    {
      CreatureModifierId.Hunter3,
      new CreatureModifier(){
        name = "Hunter III",
        descrip = "Find 10% more enemy creatures. Food Consumption +3",
        restrictFromEnemies = true,
        type = CreatureModifierType.General,
        id = CreatureModifierId.Hunter3,
        foodConsumptionChange = 6,
        enemyPctChanceChange = 0.3f,
        prereqMods = new CreatureModifierId[] { CreatureModifierId.Carnivore, CreatureModifierId.Hunter2 },
        replaceMods = new CreatureModifierId[] {
          CreatureModifierId.Hunter,
          CreatureModifierId.Hunter2
        }
      }
    },
    {
      CreatureModifierId.Cardio,
      new CreatureModifier(){
        name = "Cardio",
        descrip = "Speed +1. Food Consumption +1",
        showInCombat = true,
        type = CreatureModifierType.General,
        id = CreatureModifierId.Cardio,
        foodConsumptionChange = 1,
        speedChange = 1,
      }
    },
    {
      CreatureModifierId.Cardio2,
      new CreatureModifier(){
        name = "Cardio II",
        descrip = "Speed +2. Food Consumption +2",
        showInCombat = true,
        type = CreatureModifierType.General,
        id = CreatureModifierId.Cardio2,
        foodConsumptionChange = 3,
        speedChange = 3,
        prereqMods = new CreatureModifierId[] { CreatureModifierId.Cardio },
        replaceMods = new CreatureModifierId[] { CreatureModifierId.Cardio },
      }
    },
    {
      CreatureModifierId.Cardio3,
      new CreatureModifier(){
        name = "Cardio III",
        descrip = "Speed +3. Food Consumption +3",
        showInCombat = true,
        type = CreatureModifierType.General,
        id = CreatureModifierId.Cardio3,
        foodConsumptionChange = 6,
        speedChange = 6,
        prereqMods = new CreatureModifierId[] { CreatureModifierId.Cardio },
        replaceMods = new CreatureModifierId[] {
          CreatureModifierId.Cardio,
          CreatureModifierId.Cardio2
        },
      }
    },
    {
      CreatureModifierId.LeanMetabolism,
      new CreatureModifier(){
        name = "Lean Metabolism",
        descrip = "Reduce food consumption by 5, and your food capacity by 25.",
        type = CreatureModifierType.General,
        id = CreatureModifierId.LeanMetabolism,
        foodConsumptionChange = -5,
        maxFoodChange = -25
      }
    },
    {
      CreatureModifierId.QuickReflexes,
      new CreatureModifier(){
        name = "Quick Reflexes",
        descrip = "Slow down combat wheel by 10%.",
        type = CreatureModifierType.General,
        id = CreatureModifierId.QuickReflexes,
        showInCombat = true,
        wheelSpeedPctChange = 0.10f,
        appearsAfter = 8,
      }
    },
    {
      CreatureModifierId.QuickReflexes2,
      new CreatureModifier(){
        name = "Quick Reflexes II",
        descrip = "Slow down combat wheel by 10%.",
        type = CreatureModifierType.General,
        id = CreatureModifierId.QuickReflexes2,
        showInCombat = true,
        wheelSpeedPctChange = 0.2f,
        appearsAfter = 10,
        prereqMods = new CreatureModifierId[]{ CreatureModifierId.QuickReflexes },
        replaceMods = new CreatureModifierId[] { CreatureModifierId.QuickReflexes },
      }
    },
    {
      CreatureModifierId.QuickReflexes3,
      new CreatureModifier(){
        name = "Quick Reflexes III",
        descrip = "Slow down combat wheel by 10%.",
        type = CreatureModifierType.General,
        id = CreatureModifierId.QuickReflexes3,
        showInCombat = true,
        wheelSpeedPctChange = 0.3f,
        appearsAfter = 12,
        prereqMods = new CreatureModifierId[]{ CreatureModifierId.QuickReflexes2 },
        replaceMods = new CreatureModifierId[] {
          CreatureModifierId.QuickReflexes,
          CreatureModifierId.QuickReflexes2
        },
      }
    },
    {
      CreatureModifierId.QuickReflexes4,
      new CreatureModifier(){
        name = "Quick Reflexes IV",
        descrip = "Slow down combat wheel by 10%.",
        type = CreatureModifierType.General,
        id = CreatureModifierId.QuickReflexes4,
        showInCombat = true,
        wheelSpeedPctChange = 0.4f,
        appearsAfter = 12,
        prereqMods = new CreatureModifierId[]{ CreatureModifierId.QuickReflexes3 },
        replaceMods = new CreatureModifierId[] {
          CreatureModifierId.QuickReflexes,
          CreatureModifierId.QuickReflexes2,
          CreatureModifierId.QuickReflexes3
        },
      }
    },
    {
      CreatureModifierId.EnhancedGenome,
      new CreatureModifier(){
        name = "Enhanced Genome",
        descrip = "Save one more adaptation each successful child rearing. Persistent.",
        type = CreatureModifierType.General,
        id = CreatureModifierId.EnhancedGenome,
        repeatable = true,
        persistent = true,
        //Disable this from everyone now, don't want to remove because all of the dictionary lookups that assume it's here
        restrictFromEnemies = true,
        restrictFromPlayer = true,
        restrictFromMates = true,
        appearsAfter = 8,
      }
    },
    {
      CreatureModifierId.SmoothMoves,
      new CreatureModifier(){
        name = "Smooth Moves",
        descrip = "Slices in mating are 1 larger",
        type = CreatureModifierType.General,
        id = CreatureModifierId.SmoothMoves,
        mateSliceBonusChange = 1,
        restrictFromEnemies = true,
      }
    },
    {
      CreatureModifierId.SmoothMoves2,
      new CreatureModifier(){
        name = "Smooth Moves II",
        descrip = "Slices in mating are 1 larger",
        type = CreatureModifierType.General,
        id = CreatureModifierId.SmoothMoves2,
        mateSliceBonusChange = 2,
        restrictFromEnemies = true,
        prereqMods = new CreatureModifierId[]{ CreatureModifierId.SmoothMoves },
        replaceMods = new CreatureModifierId[] { CreatureModifierId.SmoothMoves },
      }
    },
    {
      CreatureModifierId.SmoothMoves3,
      new CreatureModifier(){
        name = "Smooth Moves III",
        descrip = "Slices in mating are 1 larger",
        type = CreatureModifierType.General,
        id = CreatureModifierId.SmoothMoves3,
        mateSliceBonusChange = 5,
        restrictFromEnemies = true,
        prereqMods = new CreatureModifierId[]{ CreatureModifierId.SmoothMoves2 },
        replaceMods = new CreatureModifierId[] {
          CreatureModifierId.SmoothMoves,
          CreatureModifierId.SmoothMoves2
        },
      }
    },

    //Body
    {
      CreatureModifierId.SmallBody,
      new CreatureModifier(){
        name = "Small Body",
        descrip = string.Empty,
        partPath = "small",
        type = CreatureModifierType.Body,
        id = CreatureModifierId.SmallBody,
        appearsAfter = 9999, //hack to never show
      }
    },
    {
      CreatureModifierId.MediumBody,
      new CreatureModifier(){
        name = "Medium Body",
        descrip = "Power +2. Food Consumption +4. Speed -1. Food Capacity +25",
        partPath = "medium",
        type = CreatureModifierType.Body,
        id = CreatureModifierId.MediumBody,
        powerChange = 2,
        foodConsumptionChange = 4,
        speedChange = -1,
        maxFoodChange = 25,
        replaceMods = new CreatureModifierId[] { CreatureModifierId.SmallBody },
      }
    },
    {
      CreatureModifierId.LargeBody,
      new CreatureModifier(){
        name = "Large Body",
        descrip = "Power +2. Food Consumption +4. Speed -2. Food Capacity +25",
        partPath = "large",
        type = CreatureModifierType.Body,
        id = CreatureModifierId.LargeBody,
        powerChange = 4,
        foodConsumptionChange = 8,
        speedChange = -2,
        maxFoodChange = 50,
        prereqMods = new CreatureModifierId[]{ CreatureModifierId.MediumBody },
        replaceMods = new CreatureModifierId[] {
          CreatureModifierId.SmallBody,
          CreatureModifierId.MediumBody
        },
      }
    },
    {
      CreatureModifierId.LongNeck,
      new CreatureModifier(){
        name = "Long Neck",
        descrip = "Can now eat tall foliage. Speed -1.",
        type = CreatureModifierType.BodyAccessory,
        id = CreatureModifierId.LongNeck,
        speedChange = -1,
        prereqMods = new CreatureModifierId[]{ CreatureModifierId.Herbivore },
        excludeMods = new CreatureModifierId[]{ CreatureModifierId.SmallBody }
      }
    },
    {
      CreatureModifierId.LongerNeck,
      new CreatureModifier(){
        name = "Longer Neck",
        descrip = "Find 40% more food. Speed -2.",
        //Special case for this one
        partPath = "tall body",
        type = CreatureModifierType.BodyAccessory,
        id = CreatureModifierId.LongerNeck,
        speedChange = -3,
        eatBonusChange = 0.40f,
        prereqMods = new CreatureModifierId[]{ CreatureModifierId.Herbivore, CreatureModifierId.LongNeck, CreatureModifierId.LargeBody },
        replaceMods = new CreatureModifierId[] { CreatureModifierId.LongNeck },
      }
    },
    {
      CreatureModifierId.ThickHide,
      new CreatureModifier(){
        name = "Thick Hide",
        descrip = "Power +2. Speed -1. Food Consumption +2",
        type = CreatureModifierType.BodyAccessory,
        id = CreatureModifierId.ThickHide,
        powerChange = 2,
        speedChange = -1,
        foodConsumptionChange = 2,
      }
    },
    {
      CreatureModifierId.ArmorHide,
      new CreatureModifier(){
        name = "Armor Hide",
        descrip = "Power +2. Speed -2. Food Consumption +4",
        type = CreatureModifierType.BodyAccessory,
        id = CreatureModifierId.ArmorHide,
        powerChange = 4,
        speedChange = -3,
        foodConsumptionChange = 6,
        prereqMods = new CreatureModifierId[]{ CreatureModifierId.ThickHide },
        replaceMods = new CreatureModifierId[] { CreatureModifierId.ThickHide },
      }
    },
    {
      CreatureModifierId.DorsalSail,
      new CreatureModifier(){
        name = "Dorsal Sail",
        descrip = "Reduce Food Consumption by 2. Find 8% more mates",
        type = CreatureModifierType.Back,
        id = CreatureModifierId.DorsalSail,
        partPath = "DorsalSail",
        foodConsumptionChange = -2,
        matePctChanceChange = 0.08f,
        excludeMods = new CreatureModifierId[]{
          CreatureModifierId.Bipedalism,
          CreatureModifierId.LargeBody,
          CreatureModifierId.DorsalSpines
        },
      }
    },
    {
      CreatureModifierId.DorsalSail2,
      new CreatureModifier(){
        name = "Dorsal Sail II",
        descrip = "Reduce Food Consumption by 2. Find 8% more mates",
        type = CreatureModifierType.Back,
        id = CreatureModifierId.DorsalSail2,
        partPath = "DorsalSail2",
        foodConsumptionChange = -4,
        matePctChanceChange = 0.16f,
        excludeMods = new CreatureModifierId[]{
          CreatureModifierId.Bipedalism,
          CreatureModifierId.LargeBody,
          CreatureModifierId.DorsalSpines
        },
        prereqMods = new CreatureModifierId[]{ CreatureModifierId.DorsalSail },
        replaceMods = new CreatureModifierId[]{ CreatureModifierId.DorsalSail }
      }
    },
    {
      CreatureModifierId.DorsalSpines,
      new CreatureModifier(){
        name = "Dorsal Spines",
        descrip = "Power +2",
        type = CreatureModifierType.Back,
        id = CreatureModifierId.DorsalSpines,
        partPath = "DorsalSpines",
        powerChange = 2,
        excludeMods = new CreatureModifierId[]{
          CreatureModifierId.Bipedalism,
          CreatureModifierId.LargeBody,
          CreatureModifierId.DorsalSail,
          CreatureModifierId.DorsalSail2,
        },
      }
    },
    {
      CreatureModifierId.DoubleStomach,
      new CreatureModifier(){
        name = "Double Stomach",
        descrip = "+50% food from foliage. Food Consumption +4",
        type = CreatureModifierType.BodyAccessory,
        id = CreatureModifierId.DoubleStomach,
        eatBonusChange = 0.5f,
        foodConsumptionChange = 4,
        prereqMods = new CreatureModifierId[]{ CreatureModifierId.Herbivore }
      }
    },
    {
      CreatureModifierId.FatReserves,
      new CreatureModifier(){
        name = "Fat Reserves",
        descrip = "Food capacity +50. Speed -2.",
        type = CreatureModifierType.BodyAccessory,
        id = CreatureModifierId.FatReserves,
        maxFoodChange = 50,
        speedChange = -2,
      }
    },
    {
      CreatureModifierId.TailSpikes,
      new CreatureModifier(){
        name = "Tail Spikes",
        descrip = "Power +1",
        type = CreatureModifierType.Tail,
        id = CreatureModifierId.TailSpikes,
        powerChange = 1,
        partPath = "TailSpikes"
      }
    },
    {
      CreatureModifierId.TailClub,
      new CreatureModifier(){
        name = "Tail Club",
        descrip = "Power +1.  Food Consumption +2",
        partPath = "TailClub",
        type = CreatureModifierType.Tail,
        id = CreatureModifierId.TailClub,
        powerChange = 2,
        foodConsumptionChange = 2,
        prereqMods = new CreatureModifierId[]{ CreatureModifierId.TailSpikes },
        replaceMods = new CreatureModifierId[] { CreatureModifierId.TailSpikes },
      }
    },

    //Head
    {
      CreatureModifierId.Omnivore,
      new CreatureModifier(){
        name = "Omnivore",
        descrip = "Can eat foliage and other creatures.",
        partPath = "Omnivore Head",
        type = CreatureModifierType.Head,
        id = CreatureModifierId.Omnivore,
        appearsAfter = 9999,
      }
    },
    {
      CreatureModifierId.Herbivore,
      new CreatureModifier(){
        name = "Herbivore",
        descrip = "Power +1. Food consumption +3. Unlocks eating medium foliage. Can no longer eat creatures.",
        partPath = "Herbivore Head",
        type = CreatureModifierType.Head,
        id = CreatureModifierId.Herbivore,
        powerChange = 1,
        foodConsumptionChange = 3,
        excludeMods = new CreatureModifierId[]{ CreatureModifierId.Carnivore },
        replaceMods = new CreatureModifierId[] { CreatureModifierId.Omnivore },
      }
    },
    {
      CreatureModifierId.Carnivore,
      new CreatureModifier(){
        name = "Carnivore",
        descrip = "Power +1. Food consumption +5. +10 food eating creatures. Can no longer eat foliage.",
        partPath = "Carnivore Head",
        type = CreatureModifierType.Head,
        id = CreatureModifierId.Carnivore,
        powerChange = 1,
        foodConsumptionChange = 5,
        excludeMods = new CreatureModifierId[]{ CreatureModifierId.Herbivore },
        replaceMods = new CreatureModifierId[] { CreatureModifierId.Omnivore },
      }
    },
    {
      CreatureModifierId.SmallHorns,
      new CreatureModifier(){
        name = "Small Horns",
        descrip = "Power +1. Add +10 to \"They Lose Food\"",
        type = CreatureModifierType.HeadAccessory,
        id = CreatureModifierId.SmallHorns,
        partPath = "SmallHorns",
        powerChange = 1,
        theyLoseFoodChange = 10,
        excludeMods = new CreatureModifierId[]{ CreatureModifierId.Plume }
      }
    },
    {
      CreatureModifierId.MediumHorns,
      new CreatureModifier(){
        name = "Medium Horns",
        descrip = "Power +1. Add +10 to \"They Lose Food\"",
        type = CreatureModifierType.HeadAccessory,
        id = CreatureModifierId.MediumHorns,
        partPath = "MediumHorns",
        powerChange = 2,
        theyLoseFoodChange = 20,
        prereqMods = new CreatureModifierId[] { CreatureModifierId.SmallHorns },
        replaceMods = new CreatureModifierId[] { CreatureModifierId.SmallHorns },
      }
    },
    {
      CreatureModifierId.LargeHorns,
      new CreatureModifier(){
        name = "Large Horns",
        descrip = "Power +1. Add +10 to \"They Lose Food\"",
        type = CreatureModifierType.HeadAccessory,
        id = CreatureModifierId.LargeHorns,
        partPath = "LargeHorns",
        powerChange = 3,
        theyLoseFoodChange = 30,
        prereqMods = new CreatureModifierId[] { CreatureModifierId.MediumHorns },
        replaceMods = new CreatureModifierId[] {
          CreatureModifierId.SmallHorns,
          CreatureModifierId.MediumHorns
        },
      }
    },
    {
      CreatureModifierId.ImprovedHearing,
      new CreatureModifier(){
        name = "Improved Hearing",
        descrip = "Speed +1. Find 10% fewer enemy creatures",
        type = CreatureModifierType.HeadAccessory,
        id = CreatureModifierId.ImprovedHearing,
        speedChange = 1,
        enemyPctChanceChange = -0.1f,
      }
    },
    {
      CreatureModifierId.EnhancedHearing,
      new CreatureModifier(){
        name = "Enhanced Hearing",
        descrip = "Speed +1. Find 10% fewer enemy creatures",
        type = CreatureModifierType.HeadAccessory,
        id = CreatureModifierId.EnhancedHearing,
        speedChange = 2,
        enemyPctChanceChange = -0.2f,
        prereqMods = new CreatureModifierId[] { CreatureModifierId.ImprovedHearing },
        replaceMods = new CreatureModifierId[] { CreatureModifierId.ImprovedHearing },
      }
    },
    {
      CreatureModifierId.IncredibleHearing,
      new CreatureModifier(){
        name = "Incredible Hearing",
        descrip = "Speed +1. Find 10% fewer enemy creatures",
        type = CreatureModifierType.HeadAccessory,
        id = CreatureModifierId.IncredibleHearing,
        speedChange = 3,
        enemyPctChanceChange = -0.3f,
        prereqMods = new CreatureModifierId[] { CreatureModifierId.EnhancedHearing },
        replaceMods = new CreatureModifierId[] {
          CreatureModifierId.ImprovedHearing,
          CreatureModifierId.EnhancedHearing
        },
      }
    },
    {
      CreatureModifierId.Plume,
      new CreatureModifier(){
        name = "Plume",
        descrip = "Find 15% more mates.",
        partPath = "SmallPlume",
        type = CreatureModifierType.HeadAccessory,
        id = CreatureModifierId.Plume,
        matePctChanceChange = 0.15f,
        excludeMods = new CreatureModifierId[] { CreatureModifierId.SmallHorns }
      }
    },
    {
      CreatureModifierId.Plume2,
      new CreatureModifier(){
        name = "Plume II",
        descrip = "Find 15% more mates.",
        partPath = "MediumPlume",
        type = CreatureModifierType.HeadAccessory,
        id = CreatureModifierId.Plume2,
        matePctChanceChange = 0.3f,
        prereqMods = new CreatureModifierId[] { CreatureModifierId.Plume },
        replaceMods = new CreatureModifierId[] { CreatureModifierId.Plume },
      }
    },
    {
      CreatureModifierId.Plume3,
      new CreatureModifier(){
        name = "Plume III",
        descrip = "Find 15% more mates.",
        partPath = "LargePlume",
        type = CreatureModifierType.HeadAccessory,
        id = CreatureModifierId.Plume3,
        matePctChanceChange = 0.45f,
        prereqMods = new CreatureModifierId[] { CreatureModifierId.Plume2 },
        replaceMods = new CreatureModifierId[] {
          CreatureModifierId.Plume,
          CreatureModifierId.Plume2
        },
      }
    },
    {
      CreatureModifierId.SharpTeeth,
      new CreatureModifier(){
        name = "Sharp Teeth",
        descrip = "Power +1",
        type = CreatureModifierType.HeadAccessory,
        id = CreatureModifierId.SharpTeeth,
        powerChange = 1,
        prereqMods = new CreatureModifierId[] { CreatureModifierId.Carnivore }
      }
    },
    {
      CreatureModifierId.SharperTeeth,
      new CreatureModifier(){
        name = "Sharper Teeth",
        descrip = "Power +1",
        type = CreatureModifierType.HeadAccessory,
        id = CreatureModifierId.SharperTeeth,
        powerChange = 2,
        prereqMods = new CreatureModifierId[] { CreatureModifierId.SharpTeeth },
        replaceMods = new CreatureModifierId[] { CreatureModifierId.SharpTeeth },
      }
    },
    {
      CreatureModifierId.RazorTeeth,
      new CreatureModifier(){
        name = "Razor Teeth",
        descrip = "Power +1",
        type = CreatureModifierType.HeadAccessory,
        id = CreatureModifierId.RazorTeeth,
        powerChange = 3,
        prereqMods = new CreatureModifierId[] { CreatureModifierId.SharperTeeth },
        replaceMods = new CreatureModifierId[] {
          CreatureModifierId.SharpTeeth,
          CreatureModifierId.SharperTeeth
        },
      }
    },
    {
      CreatureModifierId.ImprovedSmell,
      new CreatureModifier(){
        name = "Improved Sense of Smell",
        descrip = "Speed +1. Find 5% more mates",
        type = CreatureModifierType.HeadAccessory,
        id = CreatureModifierId.ImprovedSmell,
        speedChange = 1,
        matePctChanceChange = 0.05f,
      }
    },
    {
      CreatureModifierId.EnhancedSmell,
      new CreatureModifier(){
        name = "Enhanced Sense of Smell",
        descrip = "Speed +1. Find 5% more mates",
        type = CreatureModifierType.HeadAccessory,
        id = CreatureModifierId.EnhancedSmell,
        speedChange = 2,
        matePctChanceChange = 0.10f,
        prereqMods = new CreatureModifierId[] { CreatureModifierId.ImprovedSmell },
        replaceMods = new CreatureModifierId[] { CreatureModifierId.ImprovedSmell },
      }
    },
    {
      CreatureModifierId.IncredibleSmell,
      new CreatureModifier(){
        name = "Incredible Sense of Smell",
        descrip = "Speed +1. Find 5% more mates",
        type = CreatureModifierType.HeadAccessory,
        id = CreatureModifierId.IncredibleSmell,
        speedChange = 3,
        matePctChanceChange = 0.15f,
        prereqMods = new CreatureModifierId[] { CreatureModifierId.EnhancedSmell },
        replaceMods = new CreatureModifierId[] {
          CreatureModifierId.ImprovedSmell,
          CreatureModifierId.EnhancedSmell
        },
      }
    },
    {
      CreatureModifierId.SharpEyesight,
      new CreatureModifier(){
        name = "Sharp Eyesight",
        descrip = "Speed +1",
        type = CreatureModifierType.HeadAccessory,
        id = CreatureModifierId.SharpEyesight,
        speedChange = 1
      }
    },

    //Appendages
    {
      CreatureModifierId.SharpClaws,
      new CreatureModifier(){
        name = "Sharp Claws",
        descrip = "Power +1",
        type = CreatureModifierType.Arm,
        id = CreatureModifierId.SharpClaws,
        powerChange = 1
      }
    },
    {
      CreatureModifierId.Talons,
      new CreatureModifier(){
        name = "Talons",
        descrip = "Power +1. Speed -1",
        type = CreatureModifierType.Arm,
        id = CreatureModifierId.Talons,
        powerChange = 2,
        speedChange = -1,
        prereqMods = new CreatureModifierId[]{ CreatureModifierId.SharpClaws },
        replaceMods = new CreatureModifierId[] { CreatureModifierId.SharpClaws },
      }
    },
    {
      CreatureModifierId.QuickLegs,
      new CreatureModifier(){
        name = "Quick Legs",
        descrip = "Speed +1",
        type = CreatureModifierType.Leg,
        id = CreatureModifierId.QuickLegs,
        speedChange = 1
      }
    },
    {
      CreatureModifierId.SpeedyLegs,
      new CreatureModifier(){
        name = "Speedy Legs",
        descrip = "Speed +2. Power -1",
        type = CreatureModifierType.Leg,
        id = CreatureModifierId.SpeedyLegs,
        speedChange = 3,
        powerChange = -1,
        prereqMods = new CreatureModifierId[]{ CreatureModifierId.QuickLegs },
        replaceMods = new CreatureModifierId[] { CreatureModifierId.QuickLegs },
      }
    },
    {
      CreatureModifierId.SwiftLegs,
      new CreatureModifier(){
        name = "Swift Legs",
        descrip = "Speed +2",
        type = CreatureModifierType.Leg,
        id = CreatureModifierId.SwiftLegs,
        speedChange = 5,
        powerChange = -1,
        prereqMods = new CreatureModifierId[]{ CreatureModifierId.SpeedyLegs },
        replaceMods = new CreatureModifierId[] {
          CreatureModifierId.QuickLegs,
          CreatureModifierId.SpeedyLegs
        },
        excludeMods = new CreatureModifierId[]{
          CreatureModifierId.MediumBody,
          CreatureModifierId.LargeBody
        },
      }
    },
    {
      CreatureModifierId.Bipedalism,
      new CreatureModifier(){
        name = "Bipedalism",
        descrip = "Speed +2. Food Consumption +2",
        type = CreatureModifierType.Leg,
        id = CreatureModifierId.Bipedalism,
        speedChange = 2,
        foodConsumptionChange = 2,
        excludeMods = new CreatureModifierId[]{ CreatureModifierId.ArmorHide }
      }
    },
    {
      CreatureModifierId.Climbing,
      new CreatureModifier(){
        name = "Climbing",
        descrip = "Can now eat tall foliage.",
        type = CreatureModifierType.Arm,
        id = CreatureModifierId.Climbing,
        prereqMods = new CreatureModifierId[]{ CreatureModifierId.Herbivore },
        excludeMods = new CreatureModifierId[]{ CreatureModifierId.MediumBody, CreatureModifierId.LargeBody }
      }
    },
  };

  public static Dictionary<CreatureModifierType, string> modifierIcons = new Dictionary<CreatureModifierType, string>(){
    {CreatureModifierType.General,          "Art/stage4/parts/icons/general_icon"},
    {CreatureModifierType.Body,             "Art/stage4/parts/icons/body_icon"},
    {CreatureModifierType.BodyAccessory,    "Art/stage4/parts/icons/body_icon"},
    {CreatureModifierType.Back,             "Art/stage4/parts/icons/body_icon"},
    {CreatureModifierType.Tail,             "Art/stage4/parts/icons/body_icon"},
    {CreatureModifierType.Head,             "Art/stage4/parts/icons/head_icon"},
    {CreatureModifierType.HeadAccessory,    "Art/stage4/parts/icons/head_icon"},
    {CreatureModifierType.Arm,              "Art/stage4/parts/icons/arm_icon"},
    {CreatureModifierType.Leg,              "Art/stage4/parts/icons/leg_icon"},
    {CreatureModifierType.StageProgression, "Art/stage4/parts/icons/stage_4_icon"},
  };
}

public enum CreatureModifierType
{
  General,
  Body,
  BodyAccessory,
  Back,
  Tail,
  Head,
  HeadAccessory,
  Arm,
  Leg,
  StageProgression
}


public enum CreatureModifierId
{
  None = 0,
  //General
  Scavenger = 1,
  Scavenger2 = 2,
  Ambush = 3,
  Hunter = 4,
  Hunter2 = 5,
  Hunter3 = 6,
  Cardio = 7,
  Cardio2 = 8,
  Cardio3 = 9,
  LeanMetabolism = 10,
  QuickReflexes = 11,
  QuickReflexes2 = 12,
  QuickReflexes3 = 13,
  EnhancedGenome = 14,
  SmoothMoves = 15,
  SmoothMoves2 = 16,
  SmoothMoves3 = 17,
  NurturingParent = 18,
  NurturingParent2 = 19,
  NurturingParent3 = 20,
  TribalNature = 30,
  TribalNature2 = 31,
  TribalNature3 = 32,
  TribalNature4 = 33,
  TribalNature5 = 34,
  TribalNature6 = 35,
  Stubborn = 40,
  QuickReflexes4 = 41,

  //Body
  SmallBody = 100,
  MediumBody = 101,
  LargeBody = 102,
  LongNeck = 103,
  LongerNeck = 104,
  // Camoflauge = 105,
  ThickHide = 106,
  ArmorHide = 107,
  DoubleStomach = 108,
  FatReserves = 109,

  //Back
  DorsalSail = 110,
  DorsalSail2 = 111,
  DorsalSpines = 112,
  // DorsalSpines2 = 113,

  //Tail
  TailSpikes = 114,
  TailClub = 115,
  // DetachableTail = 116,

  //Head
  Herbivore = 200,
  Carnivore = 201,
  Omnivore = 202,
  // DuckBill = 202,
  // Piscivore = 203,
  SmallHorns = 204,
  MediumHorns = 205,
  LargeHorns = 206,
  ImprovedHearing = 207,
  EnhancedHearing = 208,
  IncredibleHearing = 209,
  Plume = 210,
  Plume2 = 211,
  Plume3 = 212,
  SharpTeeth = 213,
  SharperTeeth = 214,
  RazorTeeth = 215,
  ImprovedSmell = 216,
  EnhancedSmell = 217,
  IncredibleSmell = 218,
  SharpEyesight = 219,

  //Appendages
  SharpClaws = 300,
  Talons = 301,
  QuickLegs = 302,
  SpeedyLegs = 303,
  SwiftLegs = 304,
  Bipedalism = 305,
  Climbing = 306,
}
