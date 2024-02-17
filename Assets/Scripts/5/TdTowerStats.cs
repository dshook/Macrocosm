using UnityEngine;

[CreateAssetMenu(fileName = "TdTowerStats", menuName = "ScriptableObjects/New TdTowerStats")]
public class TdTowerStats : ScriptableObject {
  public TdTowerType type;

  public string towerName;
  public string descrip;

  public Sprite sprite;

  public bool attacksFlying;
  public bool attacksLand;

  public int[] moneyCost;
  public int[] woodCost;
  public int[] oreCost;
  //population cost to build/upgrade
  public int[] populationCost;

  public int[] damage;
  public float[] speed;
  public float[] radius;
  public float[] shotSpeed;

  public bool hasMoneyCost{ get{ return moneyCost != null && moneyCost.Length > 0; }}
  public bool hasWoodCost{ get{ return woodCost != null && woodCost.Length > 0; }}
  public bool hasOreCost{ get{ return oreCost != null && oreCost.Length > 0; }}
  public bool hasPopCost{ get{ return populationCost != null && populationCost.Length > 0; }}

  public bool hasDamage{ get{ return damage != null && damage.Length > 0; }}
  public bool hasSpeed{ get{ return speed != null && speed.Length > 0; }}
  public bool hasRadius{ get{ return radius != null && radius.Length > 0; }}
  public bool hasShotSpeed{ get{ return shotSpeed != null && shotSpeed.Length > 0; }}
}