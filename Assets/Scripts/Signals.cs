using System;
using strange.extensions.signal.impl;
using UnityEngine;

//General
[Singleton] public class SubStageUnlockedSignal : Signal<StageUnlockedData>{}
[Singleton] public class StageUnlockedSignal : Signal<StageUnlockedData>{}

[Singleton] public class StageTransitionStartSignal : Signal<StageTransitionData> {}
[Singleton] public class StageTransitionEndSignal : Signal<StageTransitionData> {}

[Singleton] public class ToggleMenuVisibilitySignal : Signal {}
[Singleton] public class MenuToggledSignal : Signal<bool> {}

[Singleton] public class LearningMoreSignal : Signal {}

//Game saving is when we're about to save if any data needs to be updated
[Singleton] public class GameSavingSignal : Signal {}
[Singleton] public class GameLoadedSignal : Signal {}

[Singleton] public class DragStartedSignal : Signal<Vector2> {}
[Singleton] public class DragEndedSignal : Signal<Vector2, bool> {}
[Singleton] public class ClickSignal : Signal<Vector2> {}

public class StageTransitionData{
  public int stage {get;set;}
  public int subStage {get;set;}
  public int previousActiveStage {get;set;}
  public int previousActiveSubStage {get;set;}
  public bool isLoad {get;set;}
  public bool fromMenu {get;set;}
}

public class StageUnlockedData{
  public int stage {get;set;}
  public int subStage {get;set;}
}

[Singleton] public class OpenTutorialFinishedSignal : Signal<int> {}

//Stage 1
[Singleton] public class AtomCreatedSignal : Signal<Atom> {}
[Singleton] public class AtomCombinedSignal : Signal<Atom> {}
[Singleton] public class AtomDestroyedSignal : Signal<Atom> {}

//Stage 2
[Singleton] public class StageTwoElementNeededSignal : Signal<int> {}

//Stage 3
[Singleton] public class BeatTypeUnlockedSignal : Signal<BeatType> {} //Co-opted for bonus for now
[Singleton] public class BeatHitSignal : Signal<BeatHitData> {}

[Singleton] public class SpawnBgCellSignal : Signal<
  BeatType,
  Vector2, //pos
  Quaternion, //rot
  float //delay
> {}

public class BeatHitData {
  public bool hit {get;set;}
  public IBeat beat {get; set;}
  public int baseScore {get; set;}
}

[Singleton] public class BeatTutorialFinishedSignal : Signal<BeatTutorialData> {}
public class BeatTutorialData{
  public int subStage {get;set;}
}
[Singleton] public class SongFinishedSignal : Signal<SongTemplate> {}

//Stage 4
[Singleton] public class CreatureDoneMovingSignal : Signal {}
[Singleton] public class SpinWheelFinishedSignal : Signal<SpinWheelOption> {}
[Singleton] public class CreatureDoneUpgradingSignal : Signal {}

//Stage 5
[Singleton] public class TowerBuiltSignal : Signal<TowerBuiltData> {}
[Singleton] public class TowerSellSignal : Signal<TowerBuiltData> {}
[Singleton] public class TowerUpgradeSignal : Signal<TowerBuiltData> {}
[Singleton] public class WaveCompletedSignal : Signal<int> {}
[Singleton] public class PopulationIncreasedSignal : Signal {}

public class TowerBuiltData{
  public TdTile tile {get;set;}
  public TdTowerType towerType {get;set;}
}

//Stage 6
[Singleton] public class CitySelectSignal : Signal<HexCity> {}
[Singleton] public class BuildCitySignal : Signal<HexCell> {}
[Singleton] public class CityBuiltSignal : Signal<HexCity> {}
[Singleton] public class HexBuildingFinishedSignal : Signal<HexCity, CityBuildingData> {}
[Singleton] public class HexTechFinishedSignal : Signal<HexTechId> {}
[Singleton] public class HexBuildQueueChangeSignal : Signal<HexCity, bool> {} //bool is if it was from a building finishing
[Singleton] public class HexCityBordersChangedSignal : Signal<HexCity> {}
[Singleton] public class HexBonusResourceRevealedSignal : Signal<HexBonusResource> {}
[Singleton] public class HexCellExploredSignal : Signal {}

//stage 7
[Singleton] public class ColonizeCelestialSignal : Signal<GalaxySettlementCreationData> {}
[Singleton] public class CelestialColonizedSignal : Signal<GalaxySettlementData> {}
[Singleton] public class GalaxyShipDoneSignal : Signal<GalaxyShip> {}
[Singleton] public class CreateShipSignal : Signal<GalaxyShipData, bool> {} //bool is if it's new
[Singleton] public class DestroyShipSignal : Signal<GalaxyShip> {}

[Singleton] public class SelectGalaxyResourceSignal : Signal<Func<GameResourceType, bool>, bool> {} //second param is if it's clearable
[Singleton] public class GalaxyResourceSelectedSignal : Signal<GameResourceType?> {}
[Singleton] public class GalaxyResourceSelectCancelSignal : Signal {}

[Singleton] public class GalaxyRouteResourceAssignedSignal : Signal<uint> {}
[Singleton] public class GalaxyRouteLinesUpdatedSignal : Signal {}
[Singleton] public class GalaxyBuildingFinishedSignal : Signal<GalaxyBuildingData, uint, uint?> {}
[Singleton] public class GalaxyStarImportExportChangedSignal : Signal<uint> {}
[Singleton] public class GalaxyTransitionSignal : Signal<GalaxyTransitionInfo> {}
[Singleton] public class GalaxyTransitionCompleteSignal : Signal<GalaxyTransitionCompleteInfo> {}
[Singleton] public class SelectGalaxyRouteSignal : Signal<uint> {}
[Singleton] public class GalaxyBgStarsFinishedCreatingSignal : Signal {}

//Bool for new victory, false if it's a load
[Singleton] public class VictorySignal : Signal<bool> {}

[Singleton] public class CreateHighScoreSignal : Signal<VictoryHighScore> {}

public class VictoryHighScore {
  public int victoryCount;
  public float playTimeSeconds;
}

[Singleton] public class UserReportSubmittedSignal : Signal<UserReportSubmittedData> {}
public class UserReportSubmittedData {
  public bool success;
  public Unity.Cloud.UserReporting.UserReport userReport;
}
[Singleton] public class SendErrorUserReportSignal : Signal<string> {}
