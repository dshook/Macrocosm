using UnityEngine;

[CreateAssetMenu(fileName = "TdCreepStats", menuName = "ScriptableObjects/New TdCreepStats")]
public class TdCreepStats : ScriptableObject {
  public TdCreepType creepType;

  public int hp;
  public float hpPerWave;
  //Leave as 0 to get creep default
  public float speed;
  public int popDamage;
  public int money;

  public float bossHpMultiplier;
  public float bossSpeedMultiplier;
  public float bossPopDamageMultiplier;
  public int bossMoney;

  public float perCreepDelay;
  public int creepsPerSpawnPoint;
  public float creepsPerWaveMultiplier;

  public Color color;

  public AudioClip music;
}