using System;
using System.Collections.Generic;
using UnityEngine;

public enum TdCreepType{
  Normal,
  Fast,
  Group,
  Immune,
  Spawn,
  Flying,
  Boss,
  Friendly
}

public class TdCreep : MonoBehaviour {

  public GridPathFollower gridPathFollower;
  public FlyingFollower flyingFollower;
  public SinglePathFollower singlePathFollower;
  public FilledBar hpBar;
  public SpriteRenderer svgRenderer;
  public TdCreepType type;
  public TdCreepType statsType;
  public ObjectPool objectPool;

  public TowerView goingToTower = null; //Only applies for friendly creeps going to their towers

  public int health;
  public int maxHealth;
  public float speed;
  public int popDamage;
  public int money;
  public Color color;

  public int waveNumber = -1;

  //Bool is whether or not it completed the path
  public System.Action<TdCreep, bool> OnDead;

  bool isSlowed = false;
  float slowTimer = 0f;
  const float thawTime = 1.5f;
  public bool IsSlowed { get{ return isSlowed; } }


  public void Reset(){
    isSlowed = false;
    slowTimer = 0f;
    speed = 0.75f;
    popDamage = 1;
    money = 1;
    waveNumber = -1;
    goingToTower = null;

    //Clear OnDead listeners
    if(OnDead != null){
      foreach(Delegate d in OnDead.GetInvocationList())
      {
        OnDead -= (System.Action<TdCreep, bool>)d;
      }
    }

    if(gridPathFollower != null){
      gridPathFollower.Reset();
    }else if(flyingFollower != null){
      flyingFollower.Reset();
    }else if(singlePathFollower != null){
      singlePathFollower.Reset();
    }else{
      Debug.LogWarning("No Follower to Reset");
    }
  }

  void Awake(){
    gridPathFollower = GetComponent<GridPathFollower>();
    flyingFollower = GetComponent<FlyingFollower>();
    singlePathFollower = GetComponent<SinglePathFollower>();
  }

  public void SetBaseStats(TdCreepStats creepStats, bool isBoss, int subStageProgression, StageRulesService stageRules){
    maxHealth = creepStats.hp + Mathf.RoundToInt(subStageProgression * creepStats.hpPerWave);
    if(creepStats.speed != 0){
      speed = creepStats.speed;
    }
    if(creepStats.popDamage != 0){
      popDamage = creepStats.popDamage;
    }
    if(creepStats.money != 0){
      money = creepStats.money;
    }

    if(isBoss){
      maxHealth = Mathf.RoundToInt(creepStats.bossHpMultiplier * (float)maxHealth);
      speed = creepStats.bossSpeedMultiplier * speed;
      money = creepStats.bossMoney;
      popDamage = Mathf.RoundToInt(creepStats.bossPopDamageMultiplier * (float)popDamage);
      transform.localScale = transform.localScale * 1.3f;
    }

    color = creepStats.color;
    svgRenderer.color = creepStats.color;
    health = maxHealth;
  }

  public static TdCreepType GetBossSubType(int subStageProgression, uint victoryCount, StageRulesService stageRules){
    //Cycle bosses through the different creep types by dividing the stage number by # of creeps
    //See the switch statement in stage rules to understand
    int bossCycle = subStageProgression / 7;
    return stageRules.GetStageFiveRules(bossCycle, victoryCount).creepType;
  }

  void Update()
  {
    UnityEngine.Profiling.Profiler.BeginSample("Health and slow check");
    if(health <= 0){
      Die(false);
      return;
    }

    if(isSlowed){
      slowTimer += Time.deltaTime;
      if(slowTimer > thawTime){
        isSlowed = false;
      }
    }
    UnityEngine.Profiling.Profiler.EndSample();

    UnityEngine.Profiling.Profiler.BeginSample("Color and speed");
    svgRenderer.color = isSlowed ? Colors.teal : color;

    var moveSpeed = isSlowed ? speed * 0.5f : speed;

    if(gridPathFollower != null){
      gridPathFollower.speed = moveSpeed;
    }
    if(flyingFollower != null){
      flyingFollower.speed = moveSpeed;
    }
    if(singlePathFollower != null){
      singlePathFollower.speed = moveSpeed;
    }

    UnityEngine.Profiling.Profiler.EndSample();
    UnityEngine.Profiling.Profiler.BeginSample("Hp bar");
    if(health < maxHealth){
      hpBar.gameObject.SetActive(true);
      hpBar.fillAmt = (float)health / (float)maxHealth;
    }else{
      hpBar.gameObject.SetActive(false);
    }
    UnityEngine.Profiling.Profiler.EndSample();
  }

  public void Die(bool pathCompleted){
    if(OnDead != null){
      OnDead(this, pathCompleted);
    }else{
      Debug.LogWarning("Creep OnDead null");
    }
    if(objectPool != null){
      objectPool.Recycle(this.gameObject);
    }else{
      Debug.LogWarning("Creap objectPool null");
      Destroy(this.gameObject);
    }
  }

  public void OnPathCompleted(GameObject g){
    Die(true);
  }

  public void Freeze(){
    if(type == TdCreepType.Immune) return;

    isSlowed = true;
    slowTimer = 0f;
  }

  public Int2 currentGridPos {
    get{
      return gridPathFollower.currentGridPos;
    }
  }

  public int pathIdx {
    get{
      return gridPathFollower.pathIdx;
    }
  }

  public TdTile FlyingDest {
    get {
      return flyingFollower.destination;
    }
  }

}
