using System.Collections;
using UnityEngine;

public class GalaxySpawner : MonoBehaviour {
  public Transform starHolder;
  public Transform bgStarHolder;

  public GameObject starPrefab;
  public GameObject bgStarPrefab;
  public GameObject planetPrefab;
  public GameObject asteroidBeltPrefab;

  public ObjectPool objectPool {get; set;}
  public GalaxyBgStarsFinishedCreatingSignal galaxyBgStarsFinishedCreatingSignal;

  YieldInstruction eof;

  void Awake(){
    eof = new WaitForEndOfFrame();
  }

  //Call after galaxy has been generated to actually pump unity full of game objects
  //Doing this outside of the main generation process since it takes so long
  float starCreateStartTime;
  public void StartCreatingStars(Galaxy galaxy, StageSevenDataModel stageSevenData){
    if(objectPool != null){
      objectPool.CreatePool(planetPrefab, 0);
      objectPool.CreatePool(asteroidBeltPrefab, 0);
    }

    starCreateStartTime = Time.realtimeSinceStartup;
    StartCoroutine(SpawnStars(galaxy, stageSevenData));
  }


  //Actually instanciating the game objects
  IEnumerator SpawnStars(Galaxy galaxy, StageSevenDataModel stageSevenData){

    //Spawn the star Settlements first
    var time = Time.realtimeSinceStartup;
    foreach(var starSettlement in stageSevenData.starSettlements){
      var starSettlementStarId = starSettlement.Key;

      SpawnStar(galaxy, galaxy.generatedStars[starSettlementStarId]);


      // if(Time.realtimeSinceStartup - time > spawnTimeFrameBudget){
      //   yield return eof;
      // }
      // time = Time.realtimeSinceStartup;
    }
    Debug.Log(string.Format("Settled Stars Spawn Time: {0}", Time.realtimeSinceStartup - time));

    //Then spawn stars where ships are going from or to, so that ship init can work
    var shipSpawnTime = Time.realtimeSinceStartup;
    foreach(var ship in stageSevenData.ships){
      SpawnStar(galaxy, galaxy.generatedStars[ship.sourceStarId]);
      SpawnStar(galaxy, galaxy.generatedStars[ship.destStarId]);
    }
    Debug.Log(string.Format("Ship Stars Spawn Time: {0}", Time.realtimeSinceStartup - shipSpawnTime));

    time = Time.realtimeSinceStartup;
    foreach(var starData in galaxy.generatedStars){
      SpawnStar(galaxy, starData.Value);

      if(Time.realtimeSinceStartup - time > Constants.spawnTimeFrameBudget){
        yield return eof;
        time = Time.realtimeSinceStartup;
      }
    }
    var endTime = Time.realtimeSinceStartup;
    Debug.Log(string.Format("Total Star Spawn Time: {0}", Time.realtimeSinceStartup - starCreateStartTime));
  }

  public void SpawnStar(Galaxy galaxy, GeneratedStarData starData){
    if(galaxy.stars.ContainsKey(starData.id)){
      //no-op if we've already spawned this star
      return;
    }

    var star = GameObject.Instantiate<GameObject>(starPrefab, Vector3.one, Quaternion.identity, starHolder);
    star.transform.localPosition = starData.position;
    var starComp = star.GetComponent<Star>();

    starComp.generatedData = starData;
    star.name = starComp.generatedData.name;

    galaxy.AddStar(starComp);

    starComp.UpdateDisplay(StageSevenManager.StarPalette);

  }

  //Call after galaxy has been generated to actually pump unity full of game objects
  //Doing this outside of the main generation process since it takes so long
  float bgStarCreateStartTime;
  public void StartCreatingBgStars(Galaxy galaxy){
    bgStarCreateStartTime = Time.realtimeSinceStartup;
    StartCoroutine(SpawnBgStars(galaxy));
  }

  IEnumerator SpawnBgStars(Galaxy galaxy){
    if(galaxy.bgStarData == null){
      Debug.LogWarning("No bg stars to spawn");
      yield return null;
    }


    var time = Time.realtimeSinceStartup;
    foreach(var bgStarData in galaxy.bgStarData){

      CreateBgStar(galaxy, bgStarData);


      //skip while calling from editor outside of play mode for testing since it gets stuck
      if (Time.realtimeSinceStartup - time > Constants.spawnTimeFrameBudget && (!Application.isEditor || Application.isPlaying))
      {
        yield return eof;
        time = Time.realtimeSinceStartup;
      }
    }

    Debug.Log(string.Format("Total bg star Spawn Time: {0}", Time.realtimeSinceStartup - bgStarCreateStartTime));

    if(galaxyBgStarsFinishedCreatingSignal != null){
      galaxyBgStarsFinishedCreatingSignal.Dispatch();
    }
  }


  void CreateBgStar(Galaxy galaxy, BgStarData bgStarData){
    var star = Instantiate<GameObject>(bgStarPrefab);
    star.transform.SetParent(bgStarHolder, false);
    star.transform.localPosition = bgStarData.position;
    star.transform.localScale *= bgStarData.size;

    var bgComp = star.GetComponent<BgStar>();
    galaxy.bgStars.Add(bgComp);
  }

  //TODO: prolly should recurse so it can go more than 2 levels deep
  public void CreateSystemPlanets(Galaxy galaxy, Star star){
    if(star.generatedData.childCelestialData == null) return;

    var cbsExplored = star.data != null;
    foreach(var pd in star.generatedData.childCelestialData){

      var parentBody = SetupCelestialBody(galaxy, star, pd, null, cbsExplored);

      if(pd.childCelestialData != null){
        foreach(var md in pd.childCelestialData){
          SetupCelestialBody(galaxy, star, md, parentBody, cbsExplored);
        }
      }
    }
  }

  CelestialBody SetupCelestialBody(Galaxy galaxy, Star star, CelestialBodyData data, CelestialBody parentCelestialBody, bool isExplored){
    var isRootLevel = parentCelestialBody == null;
    var prefab = data.type == CelestialBodyType.AsteroidBelt ? asteroidBeltPrefab : planetPrefab;
    var parent = parentCelestialBody != null ? parentCelestialBody.transform : star.planetHolder;

    GameObject planet;
    if(objectPool != null){
      planet = objectPool.Spawn(prefab, parent);
    }else{
      planet = Instantiate<GameObject>(prefab, parent);
    }

    planet.transform.position = (Vector2)star.transform.position + data.GetPositionOffsetFromStar(galaxy);

    var cbComp = planet.GetComponent<CelestialBody>();
    cbComp.data = data;
    cbComp.star = star;

    if(parentCelestialBody != null){
      parentCelestialBody.childCelestialBodies.Add(cbComp);
      cbComp.parentBody = parentCelestialBody;
    }else{
      cbComp.parentBody = null;
    }

    cbComp.UpdateDisplay(isExplored);

    if(isRootLevel){
      star.celestialBodies.Add(cbComp);
    }

    return cbComp;
  }

  public void CleanUpSystemPlanets(Star star){
    if(star == null) return;

    star.celestialBodies.Clear();

    if(objectPool != null){
      //TODO: right way to do this?
      objectPool.RecycleAll(planetPrefab);
      objectPool.RecycleAll(asteroidBeltPrefab);
    }else{
      star.planetHolder.DestroyChildren();
    }
  }

  public void Clear(){
    starHolder.DestroyChildren(true);
    bgStarHolder.DestroyChildren(true);
  }
}