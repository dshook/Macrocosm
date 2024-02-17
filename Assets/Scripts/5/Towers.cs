
public class Tower{
  public const int maxTowerLevel = 3;

  public static TdTowerStats GetStats(ResourceLoaderService loader, TdTowerType type){
    return loader.Load<TdTowerStats>("Prefabs/5/TowerStats/" + type);
  }

  //Assumes that tower level is either 0, or only one more than the current tower level in the upgrade case
  public static bool CanAffordTower(ResourceLoaderService loader, TdTowerType type, int nextTowerLevel, StageFiveDataModel stageFiveData){
    var towerStats = Tower.GetStats(loader, type);
    if(
      ( towerStats.hasMoneyCost && stageFiveData.money < towerStats.moneyCost[nextTowerLevel])
      || ( towerStats.hasWoodCost && stageFiveData.wood < towerStats.woodCost[nextTowerLevel])
      || ( towerStats.hasOreCost && stageFiveData.ore < towerStats.oreCost[nextTowerLevel])
      || ( towerStats.hasPopCost && (stageFiveData.population - stageFiveData.assignedPopulation) < towerStats.populationCost[nextTowerLevel])
    ){
      return false;
    }

    return true;
  }
}

public enum TdTowerType {
  None,
  Cheap,
  Fast,
  Slowing,
  Piercing,
  AOE,
  Lumber,
  Quarry,
  Farm
}
