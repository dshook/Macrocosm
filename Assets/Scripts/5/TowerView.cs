using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using strange.extensions.mediation.impl;
using UnityEngine;

public class TowerView : View {

  public Transform shotParent;
  public Transform upgradeBar;
  public Transform creepHolder;
  public TdTowerStats stats;
  public TdGrid grid;
  public SpriteRenderer towerGraphic;
  public ParticleSystem particleEmitter;

  //For remembering where you built towers last time
  public bool isGhost = false;

  //Includes the actual level the tower is
  public SavedTower data;
  //tower level to use when calculating the stats, this gets bumped up to towerLevel when the friendly creep arrives
  public int statsTowerLevel;

  public int towerLevel { get{ return data.towerLevel; } }

  public Transform rotationParent;
  public GameObject shootingAmmo;
  public Transform shootPoint;
  public GameObject upgradePrefab;

  public List<TdCreep> creeps; //synced from stage 5 manager

  [Inject] ObjectPool objectPool {get; set;}
  [Inject] ResourceLoaderService loader {get; set;}
  [Inject] WaveCompletedSignal waveCompleted {get; set;}
  [Inject] FloatingText floatingNumbers {get; set;}
  [Inject] StageFiveDataModel stageFiveData {get; set;}
  [Inject] PaletteService paletteService {get; set;}

  float shootAccum = 0f;
  float shootInterval = 0f;

  protected override void Awake() {
    base.Awake();

    if(isGhost){
      towerGraphic.color = Colors.transparentWhite15;
    }else{
      towerGraphic.color = paletteService.stage5.getColorAtIndex(0);
    }
  }

  protected override void Start(){
    base.Start();

    waveCompleted.AddListener(OnWaveCompleted);

    objectPool.CreatePool(shootingAmmo, 0);
  }

  protected override void OnDestroy(){
    if(waveCompleted != null){
      waveCompleted.RemoveListener(OnWaveCompleted);
    }
  }

  public void Init(){
    if(isGhost){
      towerGraphic.color = Colors.transparentWhite15;
    }
  }

  void Update () {
    //all the resource towers don't need to do anything
    if(!stats.hasDamage){
      return;
    }
    //Also shouldn't do anything
    if(isGhost){
      return;
    }

    UnityEngine.Profiling.Profiler.BeginSample("Targeting");
    var target = FindTarget();
    UnityEngine.Profiling.Profiler.EndSample();

    UnityEngine.Profiling.Profiler.BeginSample("Shooting");
    if(target.HasValue && stats.type != TdTowerType.AOE){
      rotationParent.rotation = PointTowards(target.Value);
    }

    shootInterval = 1f / stats.speed[statsTowerLevel];

    shootAccum += Time.deltaTime;

    if(shootAccum >= shootInterval && target.HasValue){
      shootAccum = 0;
      Shoot(target.Value);
    }
    UnityEngine.Profiling.Profiler.EndSample();
  }

  Vector2? FindTarget(){
    Vector2? bestFind = null;

    float minPathDist = float.MaxValue;

    foreach(var creep in creeps){
      var creepTransform = creep.transform;

      if(creep.type == TdCreepType.Friendly) continue;

      //slowing tower special rules
      if(stats.type == TdTowerType.Slowing ){
        if(creep.statsType == TdCreepType.Immune || creep.IsSlowed){
          continue;
        }
      }

      if(Vector2.Distance(creepTransform.transform.position, transform.position) > stats.radius[statsTowerLevel]){
        continue;
      }

      if(creep.statsType == TdCreepType.Flying){
        if(!stats.attacksFlying){ continue; }

        var destToTarget = Vector2.Distance(creep.transform.position, creep.FlyingDest.go.transform.position);
        if(destToTarget < minPathDist){
          minPathDist = destToTarget;
          bestFind = creepTransform.transform.position;
        }

      }else{
        if(!stats.attacksLand){ continue; }

        var gridPathDist = grid.tiles[creep.currentGridPos.x, creep.currentGridPos.y].pathDist[creep.pathIdx];
        if(gridPathDist < minPathDist){
          minPathDist = gridPathDist;
          bestFind = creepTransform.transform.position;
        }
      }
    }

    return bestFind;
  }

  Quaternion PointTowards(Vector2 target){
    var dir = target - (Vector2)transform.position;
    var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
    return Quaternion.AngleAxis(angle, Vector3.forward);
  }

  void Shoot(Vector2 target)
  {
    if(stats.type != TdTowerType.AOE){

      var newParticle = objectPool.Spawn(shootingAmmo, shootPoint.position, Quaternion.identity);
      if(shotParent != null){
        newParticle.transform.SetParent(shotParent, true);
      }
      newParticle.transform.localScale = Vector3.one;

      var newShot = newParticle.GetComponent<TdShot>();

      var shootDirection = target - (Vector2)transform.position;
      shootDirection.Normalize();
      newShot.objectPool = objectPool;
      newShot.velocity = shootDirection * stats.shotSpeed[statsTowerLevel];
      newShot.damage = stats.damage[statsTowerLevel];
      newShot.isSlowing = stats.type == TdTowerType.Slowing;
      newShot.maxDistance = stats.radius[statsTowerLevel] * 2f;
      newShot.shotFrom = transform.position;
      newParticle.transform.rotation = Quaternion.LookRotation(Vector3.forward, shootDirection);

    }else{
      //AOE special handling
      var creepsInRadius = GetCreepsInRadius(transform.position, stats.radius[statsTowerLevel]);
      foreach(var creep in creepsInRadius){
        creep.health -= stats.damage[statsTowerLevel];
      }

      // var main = particleEmitter.main;
      // main.startLifetime = particleEmitter.main.startSpeed * stats.radius[statsTowerLevel];
      particleEmitter.Emit(14);

    }

  }

  IEnumerable<TdCreep> GetCreepsInRadius(Vector2 pos, float radius){
    return creeps.Where(c => Vector2.Distance(pos, c.gameObject.transform.position) < radius);
  }

  void OnWaveCompleted(int waveNumber){
    //Don't let ghost towers get resources lol
    if(isGhost){ return; }

    //Ignore when losing
    if(stageFiveData.population <= 0){
      return;
    }

    var randDelay = UnityEngine.Random.Range(0f, 2f);
    if(stats.type == TdTowerType.Lumber){
      int woodAmt = 2 * (statsTowerLevel + 1);
      stageFiveData.wood += woodAmt;
      floatingNumbers.Create(transform.position, Colors.woodBrown, text: "+" + woodAmt, fontSize: 5, moveUpPct: 0.4f, punchSize: 0.5f, ttl: 1.5f, delay: randDelay);
    }

    if(stats.type == TdTowerType.Quarry){
      int oreAmt = 1 * (statsTowerLevel + 1);
      stageFiveData.ore += oreAmt;
      floatingNumbers.Create(transform.position, Colors.lightGray, text: "+" + oreAmt, fontSize: 5, moveUpPct: 0.4f, punchSize: 0.5f, ttl: 1.5f, delay: randDelay);
    }
  }

  public void StartUpgrade(){
    data.towerLevel++;
  }

  public void Upgrade(){
    statsTowerLevel++;

    //new upgrade dot
    var newDot = GameObject.Instantiate(
      upgradePrefab,
      Vector3.one,
      Quaternion.identity
    );
    newDot.transform.SetParent(upgradeBar, false);
  }

  public void Downgrade(){
    //Prevent underleveling in the case of bosses taking out more population than you have
    data.towerLevel = Mathf.Max(0, data.towerLevel - 1);
    statsTowerLevel = Mathf.Max(0, statsTowerLevel - 1);
    if(upgradeBar.childCount > 0){
      Destroy(upgradeBar.GetChild(0).gameObject);
    }
  }

  public int AccumulatedPopCost(){
    var sum = 0;
    for(int l = data.towerLevel; l >= 0; l--){
      sum += stats.hasPopCost ? stats.populationCost[l] : 0;
    }

    return sum;
  }

  public int AccumulatedMoneyCost(){
    var sum = 0;
    for(int l = data.towerLevel; l >= 0; l--){
      sum += stats.hasMoneyCost ? stats.moneyCost[l] : 0;
    }

    return sum;
  }

  public int AccumulatedWoodCost(){
    var sum = 0;
    for(int l = data.towerLevel; l >= 0; l--){
      sum += stats.hasWoodCost ? stats.woodCost[l] : 0;
    }

    return sum;
  }

  public int AccumulatedOreCost(){
    var sum = 0;
    for(int l = data.towerLevel; l >= 0; l--){
      sum += stats.hasOreCost ? stats.oreCost[l] : 0;
    }

    return sum;
  }

  int SellVal(int val){
    return data.hasBeenUsed ? Mathf.RoundToInt(0.7f * val) : val;
  }

  public int MoneySellValue {
    get {
      return SellVal(AccumulatedMoneyCost());
    }
  }
  public int WoodSellValue {
    get {
      return SellVal(AccumulatedWoodCost());
    }
  }
  public int OreSellValue {
    get {
      return SellVal(AccumulatedOreCost());
    }
  }
  public int PopulationSellValue {
    get {
      return AccumulatedPopCost();
    }
  }


  void OnDrawGizmosSelected () {
    if(stats != null){
      Gizmos.color = Color.yellow;
      Gizmos.DrawWireSphere(transform.position, stats.radius[statsTowerLevel]);
    }
  }

}
