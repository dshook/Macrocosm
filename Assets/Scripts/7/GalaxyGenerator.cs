using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Shapes;
using System.Collections;

public class GalaxyGenerator : MonoBehaviour {

  public bool useFixedSeed;
  public int seed;

  public int armStarCount = 300;
  public float galaxyRadius = 30f;

  public int minArms = 3;
  public int maxArms = 7;

  [Tooltip("Percent of galaxy radius")]
  [Range(0, 1f)]
  public float minArmLength = 0.5f;
  [Range(0, 1f)]
  public float maxArmLength = 1f;

  //how many times we stack each arm and how much smaller to make subsequent stacks
  public int armIterations = 3;
  public float armIterationMultiplier = 0.6f;

  public float minArmWidth = 1f;
  public float maxArmWidth = 2f;

  public float minStarSize = 0.06f;
  public float maxStarSize = 0.3f;

  [Range(0, 1f)]
  public float bgStarRatio = 0.5f;

  public int minPlanetMoons = 0;
  public int maxPlanetMoons = 2;

  public float minMoonDistance = 0.05f;
  public float maxMoonDistance = 0.15f;

  //How does the density of the stars change from the middle of the galaxy going out
  //Smaller min dist = more density
  public AnimationCurve minStarDistCurve;

  //How does the arm width vary from the center to end
  public AnimationCurve armWidthCurve;

  [Tooltip("Percent of galaxy radius")]
  [Range(0, 1f)]
  public float minStartingWorldDistance = 0.5f;
  [Range(0, 1f)]
  public float maxStartingWorldDistance = 0.8f;

  public float swirl = Mathf.PI * 4;

  uint objectId = 1;

  Galaxy galaxy = null;

  //Perf accumulation counters
  float starPositioningTime;
  float starEvolutionTime;
  float cbEvolutionTime;

  public Galaxy GenerateMap () {
    var startTime = Time.realtimeSinceStartup;
    Random.State originalRandomState = Random.state;
    if (!useFixedSeed) {
      seed = NumberExtensions.GenerateNewSeed();
    }
    Random.InitState(seed);


    objectId = 1;
    galaxy = new Galaxy();
    galaxy.radius = galaxyRadius;

    starPositioningTime = 0f;
    starEvolutionTime = 0f;
    cbEvolutionTime = 0f;

    CreateStars(galaxy);

    Random.state = originalRandomState;
    var endTime = Time.realtimeSinceStartup;
    Debug.Log(string.Format("Galaxy Gen Time: {0}, Star Positioning {1}, Star Creation {2}, Cb Creation {3}",
      endTime - startTime,
      //Subtract because the top times are inclusive
      starPositioningTime - starEvolutionTime,
      starEvolutionTime - cbEvolutionTime,
      cbEvolutionTime
    ));

#if UNITY_EDITOR
    //Some stats for design
    Dictionary<CelestialBodyType, int> typesTotals = new Dictionary<CelestialBodyType, int>();
    Dictionary<CelestialBodySubType, int> subTypesTotals = new Dictionary<CelestialBodySubType, int>();
    Dictionary<int, int> habitabilityTotals = new Dictionary<int, int>();
    Dictionary<CelestialBodyHabitability, int> habitabilityTypeTotals = new Dictionary<CelestialBodyHabitability, int>();
    Dictionary<ResourceAbundance, int> resourceAbundancesTotals = new Dictionary<ResourceAbundance, int>();
    Dictionary<CelestialBodySizeClass, int> gasGiantSizes = new Dictionary<CelestialBodySizeClass, int>();
    Dictionary<CelestialBodySizeClass, int> terrestrialSizes = new Dictionary<CelestialBodySizeClass, int>();
    Dictionary<float, int> starMassTotals = new Dictionary<float, int>();
    Dictionary<int, int> starTempTotals = new Dictionary<int, int>();
    Dictionary<float, int> starLuminosityTotals = new Dictionary<float, int>();
    Dictionary<float, int> starAgeTotals = new Dictionary<float, int>();
    Dictionary<string, int> starTypeTotals = new Dictionary<string, int>();

    foreach(var star in galaxy.generatedStars){
      var data = star.Value;
      starAgeTotals.AddOrUpdate((float)System.Math.Round(data.age, 1), 1, i => i + 1 );
      starMassTotals.AddOrUpdate((float)System.Math.Round(data.solarMass, 1), 1, i => i + 1 );
      starLuminosityTotals.AddOrUpdate((float)System.Math.Round(data.luminosity, 2), 1, i => i + 1 );
      starTempTotals.AddOrUpdate(data.tempK, 1, i => i + 1 );
      starTypeTotals.AddOrUpdate(data.type.ToString() + " " + data.luminosityClass.ToString(), 1, i => i + 1 );
    }

    foreach(var cb in galaxy.celestials){
      habitabilityTotals.AddOrUpdate(cb.Value.habitabilityScore, 1, i => i + 1 );
      habitabilityTypeTotals.AddOrUpdate(cb.Value.habitability, 1, i => i + 1);

      typesTotals.AddOrUpdate(cb.Value.type, 1, i => i + 1);
      subTypesTotals.AddOrUpdate(cb.Value.subType, 1, i => i + 1);

      if(cb.Value.type == CelestialBodyType.GasGiant){
        gasGiantSizes.AddOrUpdate(cb.Value.sizeClass, 1, i => i + 1);
      }else if(cb.Value.type == CelestialBodyType.Terrestrial){
        terrestrialSizes.AddOrUpdate(cb.Value.sizeClass, 1, i => i + 1);
      }

      if(cb.Value.resourceDeposits != null){
        foreach(var resource in cb.Value.resourceDeposits){
          resourceAbundancesTotals.AddOrUpdate(resource.abundance, 1, i => i + 1);
        }
      }
    }
    starMassTotals.LogKeyValues("Star masses");
    starAgeTotals.LogKeyValues("Star ages");
    starTempTotals.LogKeyValues("Star temps");
    starLuminosityTotals.LogKeyValues("Star luminosity");
    starTypeTotals.LogKeyValues("Star types");
    typesTotals.LogKeyValues("Cb Types");
    subTypesTotals.LogKeyValues("Cb SubTypes");
    habitabilityTotals.LogKeyValues("Cb Habitability");
    habitabilityTypeTotals.LogKeyValues("Cb Habitability Types");
    resourceAbundancesTotals.LogKeyValues("Resource Abundance");
    gasGiantSizes.LogKeyValues("Gas Giant Size");
    terrestrialSizes.LogKeyValues("Terrestrial Size");

#endif

    return galaxy;
  }

  //Just setting up the data
  void CreateStars(Galaxy galaxy){
    var starCreateTime = Time.realtimeSinceStartup;
    var armPositions = GenerateArms().Shuffle();

    var armPositionTime = Time.realtimeSinceStartup;
    Debug.Log("Arm positioning time " + (armPositionTime - starCreateTime));

    //create all the bg stars first without constraints
    var bgStarCount = Mathf.RoundToInt(bgStarRatio * armPositions.Count);
    for(var i = 0; i < bgStarCount; i++){
      galaxy.bgStarData.Add(new BgStarData(){
        position = armPositions[i],
        size = Random.Range(minStarSize, maxStarSize)
      });
    }

    //chop off the used positions
    armPositions = armPositions.Skip(bgStarCount).ToList();

    var bgStarTime = Time.realtimeSinceStartup;
    Debug.Log("Bg Star time " + (bgStarTime - armPositionTime));

    //then create the real stars that aren't too close to other real stars
    for(var i = armPositions.Count - 1; i >= 0; i--){
      var point = armPositions[i];
      var pctToEdge = point.magnitude / galaxyRadius; //because everything should be centered on the origin
      var minStarDist = minStarDistCurve.Evaluate(pctToEdge);

      if(armPositions.Any((p => p != point && Vector2.Distance(point, p) < minStarDist )) ){
        armPositions.RemoveAt(i);
        continue;
      }

      CreateStar(galaxy, point);
    }

    starPositioningTime += Time.realtimeSinceStartup - starCreateTime;
  }

  private List<Vector3> GenerateArms()
  {
    var retList = new List<Vector3>();
    int arms = Random.Range(minArms, maxArms);
    float armAngle = (float)(360f / arms);

    //For each arm go over multiple overlapping passes that get shorter each time so the stars are more dense in the center and less at the tips
    for (int arm = 0; arm < arms; arm++){
      var thisArmAngle = arm * armAngle;
      var armWidth = Random.Range(minArmWidth, maxArmWidth);
      var armLength = Random.Range(minArmLength * galaxyRadius, maxArmLength * galaxyRadius);
      int starsPerArmOverlap = Mathf.RoundToInt((float)armStarCount / armIterations);

      for(var armOverlap = 0; armOverlap < armIterations; armOverlap++){
        var armOverlapMult = Mathf.Pow(armIterationMultiplier, armOverlap);

        for(var starI = 0; starI < starsPerArmOverlap; starI++){
          //Distance along this arm
          float dist = Random.Range(0, armLength * armOverlapMult);

          var destPoint = Quaternion.AngleAxis(thisArmAngle, Vector3.forward) * Vector3.right * dist;

          //move it way from the arm spine by a random amount, which narrows as you get closer to the tip
          var localArmWidth = armWidth * armWidthCurve.Evaluate((dist / armLength)) * armOverlapMult;
          destPoint += (Vector3)destPoint.PerpendicularLeft() * Random.Range(-localArmWidth, localArmWidth);

          destPoint = destPoint.Swirl(Vector3.forward, swirl);

          retList.Add(destPoint);
        }
      }
    }

    return retList;
  }

  class StarOrbitData {
    public float orbitRadiusAU;
    public CelestialBodyData cbData;
  }
  List<StarOrbitData> starOrbits = new List<StarOrbitData>();

  void CreateStar(Galaxy galaxy, Vector3 localPosition){
    var starCreationStartTime = Time.realtimeSinceStartup;
    starOrbits.Clear();

    var solarMass = GetSolarMass();
    var solarAge = GetSolarAge();
    var evolutionData = GetStellarEvolution(solarMass);
    var luminosityClass = GetSolarLuminosityClass(evolutionData, solarAge);
    var luminosity = GetSolarLuminosity(evolutionData, solarAge, luminosityClass);
    var tempK = GetSolarTemp(evolutionData, luminosityClass, solarAge);
    var solarRadiusAU = GetSolarRadiusAU(luminosity, tempK);

    //Special white dwarf handling
    if(luminosityClass == LuminosityClass.D){
      solarMass = Random.Range(0.9f, 1.4f);
    }

    var innerPlanetRadiusAU = Mathf.Max(0.15f * solarMass, 0.015f * Mathf.Sqrt(luminosity));
    //Adding a cap to outer AU so CB's aren't too high on the screen, but also limit how many are placed
    var outerPlanetRadiusAU = Mathf.Min(18f, 30f * solarMass);
    var snowLineAU = 4.85f * Mathf.Sqrt(evolutionData.lMin);

    var generatedData = new GeneratedStarData(){
      id = objectId++,
      type = evolutionData.type,
      subType = evolutionData.subType,
      solarMass = solarMass,
      age = solarAge,
      luminosityClass = luminosityClass,
      luminosity = luminosity,
      tempK = tempK,
      solarRadiusAU = solarRadiusAU,
      solarRadius = Galaxy.GetStarSizeScale(solarRadiusAU),
      innerPlanetRadiusAU = innerPlanetRadiusAU,
      outerPlanetRadiusAU = outerPlanetRadiusAU,
      snowLineAU = snowLineAU,
      position = localPosition,
      resourceProbabilities = GetSolarResourceProbabilities(localPosition),
      name = StarName.Generate()
    };

    var gasGiantRoll = RandomExtensions.RollDice(3, 6);
    var gasGiantArrangement = GasGiantArrangement.None;
    if(gasGiantRoll >= 11 && gasGiantRoll <= 12){
      gasGiantArrangement = GasGiantArrangement.Conventional;
    }else if(gasGiantRoll >= 13 && gasGiantRoll <= 14){
      gasGiantArrangement = GasGiantArrangement.Eccentric;
    }else if(gasGiantRoll >= 15){
      gasGiantArrangement = GasGiantArrangement.Episteller;
    }

    //place first gas giant
    float firstOrbitAU = 0f;
    if(gasGiantArrangement == GasGiantArrangement.Conventional){
      firstOrbitAU = ((0.05f * (RandomExtensions.RollDice(2, 6) - 2)) + 1f) * snowLineAU;
    }else if(gasGiantArrangement == GasGiantArrangement.Eccentric){
      //Tweaked from the book
      firstOrbitAU = (0.127f * RandomExtensions.RollDice(1, 6)) * snowLineAU;
    }else if(gasGiantArrangement == GasGiantArrangement.Episteller){
      firstOrbitAU = (0.1f * RandomExtensions.RollDice(3, 6)) * innerPlanetRadiusAU;
    }else if(gasGiantArrangement == GasGiantArrangement.None){
      //no gas giant
      firstOrbitAU = outerPlanetRadiusAU / (0.05f * RandomExtensions.RollDice(1, 6) + 1f);
    }

    firstOrbitAU = Mathf.Clamp(firstOrbitAU, innerPlanetRadiusAU, outerPlanetRadiusAU);

    if(gasGiantArrangement == GasGiantArrangement.Conventional){
      var size = GetGasGiantSize(false, true);
      var planet = CreatePlanetData(CelestialBodyType.GasGiant, size, firstOrbitAU, generatedData);
      starOrbits.Add(new StarOrbitData{ orbitRadiusAU = firstOrbitAU, cbData = planet });
    }else if(gasGiantArrangement == GasGiantArrangement.Eccentric){
      var size = GetGasGiantSize(true, false);
      var planet = CreatePlanetData(CelestialBodyType.GasGiant, size, firstOrbitAU, generatedData);
      starOrbits.Add(new StarOrbitData{ orbitRadiusAU = firstOrbitAU, cbData = planet });
    }else if(gasGiantArrangement == GasGiantArrangement.Episteller){
      var size = GetGasGiantSize(true, false);
      var planet = CreatePlanetData(CelestialBodyType.GasGiant, size, firstOrbitAU, generatedData);
      starOrbits.Add(new StarOrbitData{ orbitRadiusAU = firstOrbitAU, cbData = planet });
    }else if(gasGiantArrangement == GasGiantArrangement.None){
      starOrbits.Add(new StarOrbitData{ orbitRadiusAU = firstOrbitAU });
    }


    //Determine orbital spacing
    //First working inward from first orbit placed
    var currentOrbitAU = firstOrbitAU / GetOrbitalSpacing();
    while(currentOrbitAU > innerPlanetRadiusAU){
      starOrbits.Insert(0, new StarOrbitData{ orbitRadiusAU = currentOrbitAU });

      var nextOrbitAU = currentOrbitAU / GetOrbitalSpacing();
      //Skip orbits that are too close together
      if(currentOrbitAU - nextOrbitAU < 0.15f){
        nextOrbitAU = nextOrbitAU / GetOrbitalSpacing();
      }
      currentOrbitAU = nextOrbitAU;
    }
    //Then work outward
    currentOrbitAU = firstOrbitAU * GetOrbitalSpacing();
    while(currentOrbitAU < outerPlanetRadiusAU){
      starOrbits.Insert(0, new StarOrbitData{ orbitRadiusAU = currentOrbitAU });
      currentOrbitAU = currentOrbitAU * GetOrbitalSpacing();
    }

    //placing gas giants
    foreach(var orbit in starOrbits){
      if(orbit.cbData != null){ continue; }
      if(gasGiantArrangement == GasGiantArrangement.None){ continue; }

      var roll = RandomExtensions.RollDice(3, 6);
      var insideSnowLine = orbit.orbitRadiusAU < snowLineAU;
      var outsideSnowLine = orbit.orbitRadiusAU > snowLineAU;
      //This is first Gas giant beyond snow line, but we don't have to check type because only gas giants have been placed so far
      var firstBeyondSnowLine = !starOrbits.Any(so => so.orbitRadiusAU >= snowLineAU && so.cbData != null);

      if(gasGiantArrangement == GasGiantArrangement.Conventional){
        if(outsideSnowLine && roll <= 10){
          var size = GetGasGiantSize(insideSnowLine, firstBeyondSnowLine);
          orbit.cbData = CreatePlanetData(CelestialBodyType.GasGiant, size, orbit.orbitRadiusAU, generatedData);
        }
      }else if(gasGiantArrangement == GasGiantArrangement.Eccentric){
        if(insideSnowLine && roll <= 6){
          var size = GetGasGiantSize(insideSnowLine, firstBeyondSnowLine);
          orbit.cbData = CreatePlanetData(CelestialBodyType.GasGiant, size, orbit.orbitRadiusAU, generatedData);
        }else if(outsideSnowLine && roll <= 9){
          var size = GetGasGiantSize(insideSnowLine, firstBeyondSnowLine);
          orbit.cbData = CreatePlanetData(CelestialBodyType.GasGiant, size, orbit.orbitRadiusAU, generatedData);
        }
      }else if(gasGiantArrangement == GasGiantArrangement.Episteller){
        if(insideSnowLine && roll <= 5){
          var size = GetGasGiantSize(insideSnowLine, firstBeyondSnowLine);
          orbit.cbData = CreatePlanetData(CelestialBodyType.GasGiant, size, orbit.orbitRadiusAU, generatedData);
        }else if(outsideSnowLine && roll <= 9){
          var size = GetGasGiantSize(insideSnowLine, firstBeyondSnowLine);
          orbit.cbData = CreatePlanetData(CelestialBodyType.GasGiant, size, orbit.orbitRadiusAU, generatedData);
        }
      }
    }

    //placing the rest of the celestial bodies
    for(var i = 0; i < starOrbits.Count; i++){
      var orbit = starOrbits[i];
      if(orbit.cbData != null){ continue; }

      var nextOrbitOutIsGasGiant = (i + 1 < starOrbits.Count) &&
        starOrbits[i+1].cbData != null &&
        starOrbits[i+1].cbData.type == CelestialBodyType.GasGiant;
      var nextOrbitInIsGasGiant = (i - 1 >= 0) &&
        starOrbits[i-1].cbData != null &&
        starOrbits[i-1].cbData.type == CelestialBodyType.GasGiant;
      var isOnEdge = i == 0 || i == starOrbits.Count - 1;
      var modifier = 0;
      if(nextOrbitOutIsGasGiant){ modifier -= 6; }
      if(nextOrbitInIsGasGiant){ modifier -= 3; }
      if(isOnEdge){ modifier -= 3; }

      var roll = RandomExtensions.RollDice(3, 6) + modifier;

      CelestialBodyType type = CelestialBodyType.AsteroidBelt;
      CelestialBodySizeClass size = CelestialBodySizeClass.Standard;
      //Bumped up probability of empty orbit
      if(roll <= 6){
        continue; //empty orbit
      }else if(roll >= 8 && roll <= 9){
        type = CelestialBodyType.AsteroidBelt;
      }else if(roll >= 10 && roll <= 10){
        type = CelestialBodyType.Terrestrial;
        size = CelestialBodySizeClass.Tiny;
      }else if(roll >= 11 && roll <= 12){
        type = CelestialBodyType.Terrestrial;
        size = CelestialBodySizeClass.Small;
      }else if(roll >= 13 && roll <= 15){
        type = CelestialBodyType.Terrestrial;
        size = CelestialBodySizeClass.Standard;
      }else if(roll >= 16){
        type = CelestialBodyType.Terrestrial;
        size = CelestialBodySizeClass.Large;
      }
      orbit.cbData = CreatePlanetData(type, size, orbit.orbitRadiusAU, generatedData);
    }

    generatedData.childCelestialData = starOrbits
      .Where(so => so.cbData != null)
      .OrderBy(so => so.orbitRadiusAU)
      .Select(so => so.cbData).ToList();

    //set the parent index of all the bodies
    for(ushort i = 0; i < generatedData.childCelestialData.Count; i++){
      generatedData.childCelestialData[i].parentIndex = i;
    }

    starEvolutionTime += Time.realtimeSinceStartup - starCreationStartTime;

    galaxy.AddGeneratedStar(generatedData);
  }

  float GetSolarMass(){
    var rolls = 2;
    var sides = 8;
    var firstRoll = RandomExtensions.RollDice(rolls, sides);

    //simplified this from gurps a lot, just use the distribution from the roll and remap to
    //desired range of masses
    var minDesiredMass = 0.1f;
    var maxDesiredMass = 2.0f;
    return Utils.Remap(firstRoll, rolls, rolls * sides, minDesiredMass, maxDesiredMass);
  }

  //In Billions of years
  float GetSolarAge(){
    var rolls = 3;
    var sides = 6;
    var firstRoll = RandomExtensions.RollDice(3, 6);

    //simplified this from gurps a lot, just use the distribution from the roll and remap to
    //desired range of masses
    var minDesiredAge = 0.1f;
    var maxDesiredAge = 9.0f;
    return Utils.Remap(firstRoll, rolls, rolls * sides, minDesiredAge, maxDesiredAge);
  }

  StellarEvolutionData GetStellarEvolution(float mass){
    for(var i = 0; i < Star.stellarEvolutionTable.Count; i++){
      var evd = Star.stellarEvolutionTable[i];
      if(mass <= evd.mass){
        return evd;
      }
    }

    return Star.stellarEvolutionTable[Star.stellarEvolutionTable.Count - 1];
  }

  //TODO: Generate the other luminosity classes?
  LuminosityClass GetSolarLuminosityClass(StellarEvolutionData sed, float age){
    if(age < sed.mSpan){
      return LuminosityClass.V;
    }else if(age < sed.mSpan + sed.sSpan){
      return LuminosityClass.IV;
    }else if(age < sed.mSpan + sed.sSpan + sed.gSpan){
      return LuminosityClass.III;
    }else{
      return LuminosityClass.D;
    }
  }

  //With Sol's luminosity = 1
  float GetSolarLuminosity(StellarEvolutionData sed, float age, LuminosityClass luminosityClass){
    if(sed.mSpan == 0 || sed.lMax == 0){
      return sed.lMin * Random.Range(0.9f, 1.1f);
    }else if (luminosityClass == LuminosityClass.IV){
      return sed.lMax * Random.Range(0.9f, 1.1f);
    }else if(luminosityClass == LuminosityClass.III || luminosityClass == LuminosityClass.II){
      return sed.lMax * Random.Range(20f, 25f);
    }else if(luminosityClass == LuminosityClass.D){
      return Random.Range(0.001f, 0.002f);
    }
    return sed.lMin + ((age / sed.mSpan) * (sed.lMax - sed.lMin));
  }

  public int GetSolarTemp(StellarEvolutionData sed, LuminosityClass luminosityClass, float age){
    var temp = sed.tempK;

    if (luminosityClass == LuminosityClass.IV){
      var subGiantAge = age - sed.mSpan;
      temp = Mathf.RoundToInt(temp - ((subGiantAge / sed.sSpan) * (temp - 4800)));
    }else if(luminosityClass == LuminosityClass.III || luminosityClass == LuminosityClass.II){
      temp = (RandomExtensions.RollDice(2, 6) - 2) * 200 + 3000;
    }
    return temp;
  }

  public float GetSolarRadiusAU(float luminosity, int tempK){
    //Might need special handling for white dwarfs?
    return (155000f * Mathf.Sqrt(luminosity)) / (tempK * tempK);
  }

  public List<GalaxyResource.ResourceProbability> GetSolarResourceProbabilities(Vector2 position){
    var ret = new List<GalaxyResource.ResourceProbability>();

    foreach(var resourceGen in GalaxyResource.GalaxyResourceGen){
      var probability = RandomExtensions.SamplePerlinOctaves(
        resourceGen.Value.perlinOrigin,
        position,
        resourceGen.Value.perlinOctaves
      ) + resourceGen.Value.boost;

      ret.Add(new GalaxyResource.ResourceProbability(){
        type = resourceGen.Key,
        probability = probability
      });
    }

    ret.Sort((a, b) => a.probability > b.probability ? -1 : 1);

    return ret;
  }

  CelestialBodyData CreatePlanetData(
    CelestialBodyType type,
    CelestialBodySizeClass size,
    float parentDistanceAU,
    GeneratedStarData starData
  ){
    var planetCreateStartTime = Time.realtimeSinceStartup;
    uint id = objectId++;

    var blackbodyTempK = CalculateBlackbodyTempK(starData, parentDistanceAU);

    var planetData = new CelestialBodyData(){
      id = id,
      parentStarId = starData.id,
      parentId = starData.id,
      type = type,
      sizeClass = size,
      blackbodyTempK = blackbodyTempK,
      parentDistanceAU = parentDistanceAU,
    };

    SetCbDetails(planetData, starData, null);

    var numRings = GetNumberOfRings(type, size, parentDistanceAU);
    if(numRings > 0){
      planetData.ringRotation = new Vector3(
        Random.Range(-45f, 9f),
        Random.Range(-20f , 20f),
        Random.Range(-85f, 85f)
      );
      planetData.rings = new CelestialRing[numRings];

      float radius = 1.45f;

      for(var r = 0; r < numRings; r++){
        var newRing = new CelestialRing(){
          radius = radius,
          thickness = Random.Range(0.04f, 0.36f) + (r * 0.05f),
          color = StageSevenManager.PlanetRingPalette.getRandomColor(),
        };

        radius += Random.Range(2.16f * newRing.thickness, 2.16f * newRing.thickness + Random.Range(0, 0.6f));
        planetData.rings[r] = newRing;


        if(radius >= 2.5f){
          break;
        }
      }
    }

    var numMoons = GetNumberOfMoons(type, size, parentDistanceAU, numRings);
    List<CelestialBodyData> moons = null;
    if(numMoons > 0){
      moons = new List<CelestialBodyData>();
      for(ushort i = 0; i < numMoons; i++){
        var moonData = CreateSubCelestialBodyData(i, numMoons, planetData, starData);
        if(moonData != null){
          moons.Add(moonData);
        }
      }
    }
    planetData.childCelestialData = moons;

    galaxy.AddCbd(planetData);

    if(starData.childCelestialData == null){
      starData.childCelestialData = new List<CelestialBodyData>();
    }
    starData.childCelestialData.Add(planetData);

    cbEvolutionTime += Time.realtimeSinceStartup - planetCreateStartTime;
    return planetData;
  }

  CelestialBodyData CreateSubCelestialBodyData(
    ushort index,
    int planetMoonCount,
    CelestialBodyData parentCbData,
    GeneratedStarData starData
  ){
    var size = GetMoonSize(parentCbData.type, parentCbData.sizeClass);
    if(size == null){
      return null;
    }

    float minOrbitParentDiameterMultiplier = 5;
    float maxOrbitParentDiameterMultiplier = 40;
    if(parentCbData.type == CelestialBodyType.GasGiant){
      minOrbitParentDiameterMultiplier = 3;
      maxOrbitParentDiameterMultiplier = 25;
    }
    float minOrbitAU = (minOrbitParentDiameterMultiplier * parentCbData.diameterM) / Constants.AU_M;
    float maxOrbitAU = (maxOrbitParentDiameterMultiplier * parentCbData.diameterM) / Constants.AU_M;

    var blackbodyTempK = CalculateBlackbodyTempK(starData, parentCbData.parentDistanceAU);

    var moonSideSwap = index % 2 == 0 ? 1 : -1;

    var moonData = new CelestialBodyData(){
      id = objectId++,
      parentStarId = starData.id,
      parentId = parentCbData.id,
      parentIndex = index,
      type = CelestialBodyType.Terrestrial,
      sizeClass = size.Value,
      blackbodyTempK = blackbodyTempK,
      parentDistance = maxMoonDistance * moonSideSwap,
      parentDistanceAU = maxOrbitAU,
    };
    SetCbDetails(moonData, starData, parentCbData);

    galaxy.AddCbd(moonData);
    return moonData;
  }

  //Needs blackbodyTempK set already
  void SetCbDetails(CelestialBodyData cbData, GeneratedStarData starData, CelestialBodyData parentCbData){
    SetCbSubtype(cbData, starData, parentCbData);
    SetCbAtmosphere(cbData);
    SetHydrographicCoverage(cbData);
    SetClimate(cbData);
    SetCbSize(cbData);
    cbData.radius = Galaxy.GetCbSizeScale(cbData.diameterM / Constants.EARTH_DIAMETER_M);
    SetCbAtmospherePressure(cbData);
    SetCbGeologicActivity(cbData, starData, parentCbData);
    SetCbRadiationActivity(cbData, starData, parentCbData);
    SetCbHabitability(cbData);
    SetCbResources(cbData, starData);
  }

  CelestialBodySizeClass GetGasGiantSize(bool insideSnowLine, bool firstBeyondSnowLine){
    var modifier = insideSnowLine || firstBeyondSnowLine ? 4 : 0;
    var roll = RandomExtensions.RollDice(3, 6) + modifier;

    if(roll <= 10){
      return CelestialBodySizeClass.Small;
    }else if(roll >= 11 && roll <= 16){
      return CelestialBodySizeClass.Standard;
    }else{
      return CelestialBodySizeClass.Large;
    }
  }

  //Increased all values by 0.1f from GURPS
  static DiceProbabilityTable<float> orbitalSpacingTable = new DiceProbabilityTable<float>(){
    table = new DiceProbEntry<float>[] {
      new DiceProbEntry<float>() { max = 4,            value = 1.5f  },
      new DiceProbEntry<float>() { min = 5,  max = 6,  value = 1.6f  },
      new DiceProbEntry<float>() { min = 7,  max = 8,  value = 1.7f  },
      new DiceProbEntry<float>() { min = 9,  max = 12, value = 1.8f  },
      new DiceProbEntry<float>() { min = 13, max = 14, value = 1.9f  },
      new DiceProbEntry<float>() { min = 15, max = 16, value = 2.0f  },
      new DiceProbEntry<float>() { min = 17,           value = 2.1f  },
    }
  };

  float GetOrbitalSpacing(){
    var roll = RandomExtensions.RollDice(3, 6);

    return orbitalSpacingTable.Get(roll);
  }


  //Similar to number of moons logic
  ushort GetNumberOfRings(CelestialBodyType parentType, CelestialBodySizeClass parentSize, float starOrbitDistanceAU){
    var maxRings = 3;
    switch(parentType){
      case CelestialBodyType.AsteroidBelt:
        return 0;
      case CelestialBodyType.GasGiant:
      {
        if(starOrbitDistanceAU < 0.1f){ return 0;}
        var modifier = 0;
        if(starOrbitDistanceAU >= 0.1f && starOrbitDistanceAU <= 0.75f){
          modifier = -4;
        }else if(starOrbitDistanceAU >= 0.75f && starOrbitDistanceAU <= 1.5f){
          modifier = -4;
        }else{
          modifier = -5;
        }
        var roll = RandomExtensions.RollDice(1, 6) + modifier;

        return (ushort)Mathf.Clamp(roll, 0, maxRings);
      }
      case CelestialBodyType.Terrestrial:
      {
        if(starOrbitDistanceAU < 0.5f){ return 0;}
        var modifier = 0;
        if(starOrbitDistanceAU >= 0.5f && starOrbitDistanceAU <= 0.75f){
          modifier = -5;
        }else if(starOrbitDistanceAU >= 0.75f && starOrbitDistanceAU <= 1.5f){
          modifier = -4;
        }
        if(parentSize == CelestialBodySizeClass.Small){
          modifier -= 4;
        }
        if(parentSize == CelestialBodySizeClass.Large){
          modifier += -1;
        }

        var roll = RandomExtensions.RollDice(1, 6) + modifier;

        //Terrestrial planets can't have as many rings as gas giants
        return (ushort)Mathf.Clamp(roll, 0, maxRings - 1);
      }
    }

    return 0;
  }

  ushort GetNumberOfMoons(CelestialBodyType parentType, CelestialBodySizeClass parentSize, float starOrbitDistanceAU, ushort numRings){
    switch(parentType){
      case CelestialBodyType.AsteroidBelt:
        return 0;
      case CelestialBodyType.GasGiant:
      {
        if(starOrbitDistanceAU < 0.1f){ return 0;}
        var modifier = 0;
        if(starOrbitDistanceAU >= 0.1f && starOrbitDistanceAU <= 0.75f){
          modifier = -5;
        }else if(starOrbitDistanceAU >= 0.75f && starOrbitDistanceAU <= 1.5f){
          modifier = -4;
        }else{
          modifier = -3;
        }
        var roll = RandomExtensions.RollDice(1, 6) + modifier;

        return (ushort)Mathf.Clamp(roll, 0, maxPlanetMoons + (numRings > 0 ? -1 : 0));
      }
      case CelestialBodyType.Terrestrial:
      {
        if(numRings > 0){ return 0;}
        if(starOrbitDistanceAU < 0.5f){ return 0;}
        var modifier = 0;
        if(starOrbitDistanceAU >= 0.5f && starOrbitDistanceAU <= 0.75f){
          modifier = -5;
        }else if(starOrbitDistanceAU >= 0.75f && starOrbitDistanceAU <= 1.5f){
          modifier = -3;
        }
        if(parentSize == CelestialBodySizeClass.Small){
          modifier -= 3;
        }
        if(parentSize == CelestialBodySizeClass.Large){
          modifier += -1;
        }

        var roll = RandomExtensions.RollDice(1, 6) + modifier;

        //Terrestrial planets can't have as many moons as gas giants
        return (ushort)Mathf.Clamp(roll, 0, maxPlanetMoons - 1);
      }
    }

    return 0;
  }

  CelestialBodySizeClass? GetMoonSize(CelestialBodyType parentType, CelestialBodySizeClass parentSize){
    var effectiveParentSize = parentType == CelestialBodyType.GasGiant ? CelestialBodySizeClass.Large : parentSize;
    var roll = RandomExtensions.RollDice(3, 6);
    var sizePenalty = 0;
    if(roll <= 11){
      sizePenalty = -3;
    }else if(roll >= 12 && roll <= 14){
      sizePenalty = -2;
    }else{
      sizePenalty = -1;
    }

    var newSizeClassInt = (int)effectiveParentSize + sizePenalty;

    if(newSizeClassInt >= 0){
      return (CelestialBodySizeClass)newSizeClassInt;
    }

    return null;
  }

  float CalculateBlackbodyTempK(GeneratedStarData starData, float distanceToStarAU){

    return 278f * (Mathf.Pow(starData.luminosity, 0.25f) / Mathf.Sqrt(distanceToStarAU));
  }

  void SetCbSubtype(CelestialBodyData cbData, GeneratedStarData starData, CelestialBodyData parentCbData = null){
    if(cbData.type == CelestialBodyType.GasGiant){
      //Add fudge factor here to the blackbody temp to account for other temp factors
      //https://astronomy.stackexchange.com/questions/28889/gas-giant-temperatures
      var temp = cbData.blackbodyTempK * Random.Range(1.1f, 1.4f);
      if(temp < 250){
        if(Random.Range(0, 1f) < 0.5){
          cbData.subType = CelestialBodySubType.Ice;
        }else{
          cbData.subType = CelestialBodySubType.Ammonia;
        }
      }else if(temp < 350){
        cbData.subType = CelestialBodySubType.WaterClouds;
      }else if(temp < 800){
        cbData.subType = CelestialBodySubType.Cloudless;
      }else if(temp < 1200){
        cbData.subType = CelestialBodySubType.AlkaliMetal;
      }else{
        cbData.subType = CelestialBodySubType.Silicate;
      }

    }else if(cbData.type == CelestialBodyType.Terrestrial){

      switch(cbData.sizeClass){
        case CelestialBodySizeClass.Tiny:
          if(cbData.blackbodyTempK <= 140){
            if(parentCbData != null && parentCbData.type == CelestialBodyType.GasGiant){
              var roll = RandomExtensions.RollDice(1, 6);
              if(roll > 4){
                cbData.subType = CelestialBodySubType.Sulfur;
              }else{
                cbData.subType = CelestialBodySubType.Ice;
              }
            }else{
              cbData.subType = CelestialBodySubType.Ice;
            }
          }else{
            cbData.subType = CelestialBodySubType.Rock;
          }
          break;
        case CelestialBodySizeClass.Small:
          if(cbData.blackbodyTempK <= 80){
            cbData.subType = CelestialBodySubType.Hadean;
          }else if(cbData.blackbodyTempK <= 140){
            cbData.subType = CelestialBodySubType.Ice;
          }else{
            cbData.subType = CelestialBodySubType.Rock;
          }
          break;
        case CelestialBodySizeClass.Standard:
          if(cbData.blackbodyTempK <= 80){
            cbData.subType = CelestialBodySubType.Hadean;
          }else if(cbData.blackbodyTempK <= 150){
            cbData.subType = CelestialBodySubType.Ice;
          }else if(cbData.blackbodyTempK <= 230){
            if(starData.solarMass <= 0.65f){
              cbData.subType = CelestialBodySubType.Ammonia;
            }else{
              cbData.subType = CelestialBodySubType.Ice;
            }
          }else if(cbData.blackbodyTempK <= 240){
            cbData.subType = CelestialBodySubType.Ice;
          }else if(cbData.blackbodyTempK <= 320){
            var bonus = Mathf.Min(10, Mathf.FloorToInt(starData.age / 0.5f ) );
            var roll = RandomExtensions.RollDice(3, 6) + bonus;
            if(roll >= 18){
              cbData.subType = CelestialBodySubType.Garden;
            }else{
              cbData.subType = CelestialBodySubType.Ocean;
            }
          }else if(cbData.blackbodyTempK <= 500){
            cbData.subType = CelestialBodySubType.Greenhouse;
          }else{
            cbData.subType = CelestialBodySubType.Chthonian;
          }
          break;
        case CelestialBodySizeClass.Large:
          if(cbData.blackbodyTempK <= 150){
            cbData.subType = CelestialBodySubType.Ice;
          }else if(cbData.blackbodyTempK <= 230){
            if(starData.solarMass <= 0.65f){
              cbData.subType = CelestialBodySubType.Ammonia;
            }else{
              cbData.subType = CelestialBodySubType.Ice;
            }
          }else if(cbData.blackbodyTempK <= 240){
            cbData.subType = CelestialBodySubType.Ice;
          }else if(cbData.blackbodyTempK <= 320){
            var bonus = Mathf.Min(5, Mathf.FloorToInt(starData.age / 0.5f ) );
            var roll = RandomExtensions.RollDice(3, 6) + bonus;
            if(roll >= 18){
              cbData.subType = CelestialBodySubType.Garden;
            }else{
              cbData.subType = CelestialBodySubType.Ocean;
            }
          }else if(cbData.blackbodyTempK <= 500){
            cbData.subType = CelestialBodySubType.Greenhouse;
          }else{
            cbData.subType = CelestialBodySubType.Chthonian;
          }
          break;
      }
    }else{
      cbData.subType = CelestialBodySubType.None;
    }

  }


  void SetCbAtmosphere(CelestialBodyData cbData){
    cbData.atmosphericMass = 0f;
    if(
      cbData.type == CelestialBodyType.AsteroidBelt ||
      (cbData.sizeClass == CelestialBodySizeClass.Tiny) ||
      (cbData.sizeClass == CelestialBodySizeClass.Small && cbData.subType == CelestialBodySubType.Hadean)
    ){
      cbData.atmosphereType = CelestialBodyAtmosphereType.None;
      return;
    }

    if(
      (cbData.sizeClass == CelestialBodySizeClass.Small && cbData.subType == CelestialBodySubType.Rock) ||
      (cbData.sizeClass == CelestialBodySizeClass.Standard && cbData.subType == CelestialBodySubType.Hadean) ||
      (cbData.subType == CelestialBodySubType.Chthonian)
    ){
      if(RandomExtensions.RollDice(1, 6) > 4){
        cbData.atmosphereType = CelestialBodyAtmosphereType.None;
      }else{
        cbData.atmosphereType = CelestialBodyAtmosphereType.None;
      }
      return;
    }

    var atmosphericMass = (float)RandomExtensions.RollDice(3, 6) / 10f;
    cbData.atmosphericMass = atmosphericMass;

    if(cbData.sizeClass == CelestialBodySizeClass.Small && cbData.subType == CelestialBodySubType.Ice){
      cbData.atmosphereType = CelestialBodyAtmosphereType.Toxic;
    }
    if(cbData.sizeClass == CelestialBodySizeClass.Large &&
      (cbData.subType == CelestialBodySubType.Ice || cbData.subType == CelestialBodySubType.Ocean)
    ){
      cbData.atmosphereType = CelestialBodyAtmosphereType.Toxic;
    }

    if(cbData.subType == CelestialBodySubType.Ammonia){
      cbData.atmosphereType = CelestialBodyAtmosphereType.DeadlyToxic;
    }

    if(cbData.sizeClass == CelestialBodySizeClass.Standard &&
      (cbData.subType == CelestialBodySubType.Ice || cbData.subType == CelestialBodySubType.Ocean)
    ){
      if(RandomExtensions.RollDice(3, 6) <= 12){
        cbData.atmosphereType = CelestialBodyAtmosphereType.Suffocating;
      }else{
        cbData.atmosphereType = CelestialBodyAtmosphereType.Toxic;
      }
    }

    if(cbData.subType == CelestialBodySubType.Garden){
      if(RandomExtensions.RollDice(3, 6) <= 11){
        cbData.atmosphereType = CelestialBodyAtmosphereType.Standard;
      }else{
        cbData.atmosphereType = CelestialBodyAtmosphereType.Marginal;
      }
    }

    if(cbData.subType == CelestialBodySubType.Greenhouse){
      cbData.atmosphereType = CelestialBodyAtmosphereType.Corrosive;
    }
  }

  void SetHydrographicCoverage(CelestialBodyData cbData){
    if(
      cbData.type == CelestialBodyType.AsteroidBelt ||
      (cbData.sizeClass == CelestialBodySizeClass.Tiny && cbData.subType == CelestialBodySubType.Rock) ||
      (cbData.sizeClass == CelestialBodySizeClass.Small && cbData.subType == CelestialBodySubType.Rock) ||
      (cbData.sizeClass == CelestialBodySizeClass.Tiny && cbData.subType == CelestialBodySubType.Ice) ||
      (cbData.sizeClass == CelestialBodySizeClass.Tiny && cbData.subType == CelestialBodySubType.Sulfur) ||
      (cbData.sizeClass == CelestialBodySizeClass.Small && cbData.subType == CelestialBodySubType.Hadean) ||
      (cbData.subType == CelestialBodySubType.Chthonian) ||
      (cbData.sizeClass == CelestialBodySizeClass.Standard && cbData.subType == CelestialBodySubType.Hadean)
    ){
      cbData.hydrographicCoverage = 0f;
    }

    if(
      (cbData.sizeClass == CelestialBodySizeClass.Small && cbData.subType == CelestialBodySubType.Ice)
    ){
      cbData.hydrographicCoverage = 0.1f * (RandomExtensions.RollDice(1, 6) + 2);
    }

    if(
      (cbData.sizeClass == CelestialBodySizeClass.Standard || cbData.sizeClass == CelestialBodySizeClass.Large) &&
      cbData.subType == CelestialBodySubType.Ammonia
    ){
      cbData.hydrographicCoverage = 0.1f * RandomExtensions.RollDice(2, 6);
    }

    if(
      (cbData.sizeClass == CelestialBodySizeClass.Standard || cbData.sizeClass == CelestialBodySizeClass.Large) &&
      cbData.subType == CelestialBodySubType.Ice
    ){
      cbData.hydrographicCoverage = 0.1f * (RandomExtensions.RollDice(2, 6) - 10);
    }

    if(
      (cbData.sizeClass == CelestialBodySizeClass.Standard || cbData.sizeClass == CelestialBodySizeClass.Large) &&
      (cbData.subType == CelestialBodySubType.Ocean || cbData.subType == CelestialBodySubType.Garden)
    ){
      cbData.hydrographicCoverage = 0.1f * (RandomExtensions.RollDice(1, 6) + cbData.sizeClass == CelestialBodySizeClass.Large ? 6 : 4);
    }

    if(
      (cbData.sizeClass == CelestialBodySizeClass.Standard || cbData.sizeClass == CelestialBodySizeClass.Large) &&
      cbData.subType == CelestialBodySubType.Greenhouse
    ){
      cbData.hydrographicCoverage = 0.1f * (RandomExtensions.RollDice(2, 6) - 7);
    }

    cbData.hydrographicCoverage = Mathf.Clamp(cbData.hydrographicCoverage, 0f, 1f);
  }

  void SetClimate(CelestialBodyData cbData){
    float absorptionFactor = 0f;
    float greenhouseFactor = 0;

    if(cbData.type == CelestialBodyType.AsteroidBelt){
      absorptionFactor = 0.97f;
    }else if(cbData.type == CelestialBodyType.Terrestrial){
      switch(cbData.sizeClass){
        case CelestialBodySizeClass.Tiny:
          switch(cbData.subType){
            case CelestialBodySubType.Ice:
              absorptionFactor = 0.86f; break;
            case CelestialBodySubType.Rock:
              absorptionFactor = 0.87f; break;
            case CelestialBodySubType.Sulfur:
              absorptionFactor = 0.77f; break;
          }
          break;
        case CelestialBodySizeClass.Small:
          switch(cbData.subType){
            case CelestialBodySubType.Hadean:
              absorptionFactor = 0.67f; break;
            case CelestialBodySubType.Ice:
              absorptionFactor = 0.93f;
              greenhouseFactor = 0.1f;
              break;
            case CelestialBodySubType.Rock:
              absorptionFactor = 0.96f; break;
          }
          break;
        default:
          switch(cbData.subType){
            case CelestialBodySubType.Hadean:
              absorptionFactor = 0.67f; break;
            case CelestialBodySubType.Ammonia:
              absorptionFactor = 0.84f;
              greenhouseFactor = 0.2f;
              break;
            case CelestialBodySubType.Ice:
              absorptionFactor = 0.86f;
              greenhouseFactor = 0.2f;
              break;
            case CelestialBodySubType.Ocean:
            case CelestialBodySubType.Garden:
              if(cbData.hydrographicCoverage <= 0.2f){
                absorptionFactor = 0.95f;
                greenhouseFactor = 0.16f;
              }else if(cbData.hydrographicCoverage <= 0.5f){
                absorptionFactor = 0.92f;
                greenhouseFactor = 0.16f;
              }else if(cbData.hydrographicCoverage <= 0.9f){
                absorptionFactor = 0.88f;
                greenhouseFactor = 0.16f;
              }else{
                absorptionFactor = 0.84f;
                greenhouseFactor = 0.16f;
              }
              break;
            case CelestialBodySubType.Greenhouse:
              absorptionFactor = 0.77f;
              greenhouseFactor = 2.0f;
              break;
            case CelestialBodySubType.Chthonian:
              absorptionFactor = 0.97f;
              break;
            case CelestialBodySubType.Rock:
              absorptionFactor = 0.96f; break;
          }
          break;
      }
    }else if(cbData.type == CelestialBodyType.GasGiant){
      float albedo = 1f;
      switch(cbData.subType){
        case CelestialBodySubType.Ice:
          albedo = Random.Range(0.34f, 0.57f);
          break;
        case CelestialBodySubType.Ammonia:
          albedo = Random.Range(0.28f, 0.5f);
          break;
        case CelestialBodySubType.WaterClouds:
          albedo = Random.Range(0.75f, 0.85f);
          break;
        case CelestialBodySubType.Cloudless:
          albedo = Random.Range(0.05f, 0.15f);
          break;
        case CelestialBodySubType.AlkaliMetal:
          albedo = Random.Range(0f, 0.05f);
          break;
        case CelestialBodySubType.Silicate:
          albedo = Random.Range(0.5f, 0.6f);
          break;
      }
      absorptionFactor = Mathf.Pow(1f - albedo, 0.25f);
    }

    var blackbodyCorrection = absorptionFactor * (1 + (cbData.atmosphericMass * greenhouseFactor));

    cbData.averageSurfaceTempK = cbData.blackbodyTempK * blackbodyCorrection;
    var astk = cbData.averageSurfaceTempK;
    if(astk <= 244f){
      cbData.climate = CelestialBodyClimate.Frozen;
    }else if(astk <= 255f){
      cbData.climate = CelestialBodyClimate.VeryCold;
    }else if(astk <= 266f){
      cbData.climate = CelestialBodyClimate.Cold;
    }else if(astk <= 278f){
      cbData.climate = CelestialBodyClimate.Chilly;
    }else if(astk <= 289f){
      cbData.climate = CelestialBodyClimate.Cool;
    }else if(astk <= 300f){
      cbData.climate = CelestialBodyClimate.Normal;
    }else if(astk <= 311f){
      cbData.climate = CelestialBodyClimate.Warm;
    }else if(astk <= 322f){
      cbData.climate = CelestialBodyClimate.Tropical;
    }else if(astk <= 333f){
      cbData.climate = CelestialBodyClimate.Hot;
    }else if(astk <= 344f){
      cbData.climate = CelestialBodyClimate.VeryHot;
    }else{
      cbData.climate = CelestialBodyClimate.Infernal;
    }
  }

  struct GasGiantSizeEntry{
    public float mass;
    public float density;
  }

  static DiceProbabilityTable<GasGiantSizeEntry> smallGasGiantSizeTable = new DiceProbabilityTable<GasGiantSizeEntry>(){
    table = new DiceProbEntry<GasGiantSizeEntry>[] {
      new DiceProbEntry<GasGiantSizeEntry>() { max = 8,            value = new GasGiantSizeEntry() { mass = 10f, density = 0.42f } },
      new DiceProbEntry<GasGiantSizeEntry>() { min = 9,  max = 10, value = new GasGiantSizeEntry() { mass = 15f, density = 0.26f } },
      new DiceProbEntry<GasGiantSizeEntry>() { min = 11, max = 11, value = new GasGiantSizeEntry() { mass = 20f, density = 0.22f } },
      new DiceProbEntry<GasGiantSizeEntry>() { min = 12, max = 12, value = new GasGiantSizeEntry() { mass = 30f, density = 0.19f } },
      new DiceProbEntry<GasGiantSizeEntry>() { min = 13, max = 13, value = new GasGiantSizeEntry() { mass = 40f, density = 0.17f } },
      new DiceProbEntry<GasGiantSizeEntry>() { min = 14, max = 14, value = new GasGiantSizeEntry() { mass = 50f, density = 0.17f } },
      new DiceProbEntry<GasGiantSizeEntry>() { min = 15, max = 15, value = new GasGiantSizeEntry() { mass = 60f, density = 0.17f } },
      new DiceProbEntry<GasGiantSizeEntry>() { min = 16, max = 16, value = new GasGiantSizeEntry() { mass = 70f, density = 0.17f } },
      new DiceProbEntry<GasGiantSizeEntry>() { min = 17,           value = new GasGiantSizeEntry() { mass = 80f, density = 0.17f } },
    }
  };

  static DiceProbabilityTable<GasGiantSizeEntry> standardGasGiantSizeTable = new DiceProbabilityTable<GasGiantSizeEntry>(){
    table = new DiceProbEntry<GasGiantSizeEntry>[] {
      new DiceProbEntry<GasGiantSizeEntry>() { max = 8,            value = new GasGiantSizeEntry() { mass = 100f, density = 0.18f } },
      new DiceProbEntry<GasGiantSizeEntry>() { min = 9,  max = 10, value = new GasGiantSizeEntry() { mass = 150f, density = 0.19f } },
      new DiceProbEntry<GasGiantSizeEntry>() { min = 11, max = 11, value = new GasGiantSizeEntry() { mass = 200f, density = 0.20f } },
      new DiceProbEntry<GasGiantSizeEntry>() { min = 12, max = 12, value = new GasGiantSizeEntry() { mass = 250f, density = 0.22f } },
      new DiceProbEntry<GasGiantSizeEntry>() { min = 13, max = 13, value = new GasGiantSizeEntry() { mass = 300f, density = 0.24f } },
      new DiceProbEntry<GasGiantSizeEntry>() { min = 14, max = 14, value = new GasGiantSizeEntry() { mass = 350f, density = 0.25f } },
      new DiceProbEntry<GasGiantSizeEntry>() { min = 15, max = 15, value = new GasGiantSizeEntry() { mass = 400f, density = 0.26f } },
      new DiceProbEntry<GasGiantSizeEntry>() { min = 16, max = 16, value = new GasGiantSizeEntry() { mass = 450f, density = 0.27f } },
      new DiceProbEntry<GasGiantSizeEntry>() { min = 17,           value = new GasGiantSizeEntry() { mass = 500f, density = 0.29f } },
    }
  };

  static DiceProbabilityTable<GasGiantSizeEntry> largeGasGiantSizeTable = new DiceProbabilityTable<GasGiantSizeEntry>(){
    table = new DiceProbEntry<GasGiantSizeEntry>[] {
      new DiceProbEntry<GasGiantSizeEntry>() { max = 8,            value = new GasGiantSizeEntry() { mass = 600f,  density = 0.31f } },
      new DiceProbEntry<GasGiantSizeEntry>() { min = 9,  max = 10, value = new GasGiantSizeEntry() { mass = 800f,  density = 0.35f } },
      new DiceProbEntry<GasGiantSizeEntry>() { min = 11, max = 11, value = new GasGiantSizeEntry() { mass = 1000f, density = 0.40f } },
      new DiceProbEntry<GasGiantSizeEntry>() { min = 12, max = 12, value = new GasGiantSizeEntry() { mass = 1500f, density = 0.60f } },
      new DiceProbEntry<GasGiantSizeEntry>() { min = 13, max = 13, value = new GasGiantSizeEntry() { mass = 2000f, density = 0.80f } },
      new DiceProbEntry<GasGiantSizeEntry>() { min = 14, max = 14, value = new GasGiantSizeEntry() { mass = 2500f, density = 1.00f } },
      new DiceProbEntry<GasGiantSizeEntry>() { min = 15, max = 15, value = new GasGiantSizeEntry() { mass = 3000f, density = 1.20f } },
      new DiceProbEntry<GasGiantSizeEntry>() { min = 16, max = 16, value = new GasGiantSizeEntry() { mass = 3500f, density = 1.40f } },
      new DiceProbEntry<GasGiantSizeEntry>() { min = 17,           value = new GasGiantSizeEntry() { mass = 4000f, density = 1.60f } },
    }
  };

  static DiceProbabilityTable<float> icyCoreTable = new DiceProbabilityTable<float>(){
    table = new DiceProbEntry<float>[] {
      new DiceProbEntry<float>() { max = 6,            value = 0.3f  },
      new DiceProbEntry<float>() { min = 7,  max = 10, value = 0.4f  },
      new DiceProbEntry<float>() { min = 11, max = 14, value = 0.5f  },
      new DiceProbEntry<float>() { min = 15, max = 17, value = 0.6f  },
      new DiceProbEntry<float>() { min = 18,           value = 0.7f  },
    }
  };

  static DiceProbabilityTable<float> smallIronCoreTable = new DiceProbabilityTable<float>(){
    table = new DiceProbEntry<float>[] {
      new DiceProbEntry<float>() { max = 6,            value = 0.6f  },
      new DiceProbEntry<float>() { min = 7,  max = 10, value = 0.7f  },
      new DiceProbEntry<float>() { min = 11, max = 14, value = 0.8f  },
      new DiceProbEntry<float>() { min = 15, max = 17, value = 0.9f  },
      new DiceProbEntry<float>() { min = 18,           value = 1.0f  },
    }
  };

  static DiceProbabilityTable<float> largeIronCoreTable = new DiceProbabilityTable<float>(){
    table = new DiceProbEntry<float>[] {
      new DiceProbEntry<float>() { max = 6,            value = 0.8f  },
      new DiceProbEntry<float>() { min = 7,  max = 10, value = 0.9f  },
      new DiceProbEntry<float>() { min = 11, max = 14, value = 1.0f  },
      new DiceProbEntry<float>() { min = 15, max = 17, value = 1.1f  },
      new DiceProbEntry<float>() { min = 18,           value = 1.2f  },
    }
  };

  void SetCbSize(CelestialBodyData cbData){
    var roll = RandomExtensions.RollDice(3, 6);
    float density = 0f; //In earth scaled units
    float earthDiameters = 0f;

    if(cbData.type == CelestialBodyType.GasGiant){
      DiceProbabilityTable<GasGiantSizeEntry> sizeTable;
      switch(cbData.sizeClass){
        case CelestialBodySizeClass.Small:
          sizeTable = smallGasGiantSizeTable;
          break;
        case CelestialBodySizeClass.Standard:
          sizeTable = standardGasGiantSizeTable;
          break;
        default:
          sizeTable = largeGasGiantSizeTable;
          break;
      }
      var tableValue = sizeTable.Get(roll);
      cbData.earthMasses = tableValue.mass;
      cbData.densityEarthUnits = tableValue.density;

      earthDiameters = Mathf.Pow((cbData.earthMasses / cbData.densityEarthUnits), 1f/3f);
      cbData.diameterM = earthDiameters * Constants.EARTH_DIAMETER_M;
      cbData.surfaceGravityGs = earthDiameters * cbData.densityEarthUnits;
      return;

    } else if(cbData.type == CelestialBodyType.AsteroidBelt){
      density = 0.1f;
    }
    //icy core
    else if(
      (cbData.sizeClass == CelestialBodySizeClass.Tiny && cbData.subType == CelestialBodySubType.Ice) ||
      (cbData.sizeClass == CelestialBodySizeClass.Tiny && cbData.subType == CelestialBodySubType.Sulfur) ||
      (cbData.sizeClass == CelestialBodySizeClass.Small && cbData.subType == CelestialBodySubType.Hadean) ||
      (cbData.sizeClass == CelestialBodySizeClass.Small && cbData.subType == CelestialBodySubType.Ice) ||
      (cbData.sizeClass == CelestialBodySizeClass.Standard && cbData.subType == CelestialBodySubType.Hadean) ||
      (cbData.sizeClass == CelestialBodySizeClass.Standard && cbData.subType == CelestialBodySubType.Ammonia) ||
      (cbData.sizeClass == CelestialBodySizeClass.Large && cbData.subType == CelestialBodySubType.Ammonia)
    ){
      density = icyCoreTable.Get(roll);
    }
    //small iron core
    else if(
      (cbData.sizeClass == CelestialBodySizeClass.Tiny && cbData.subType == CelestialBodySubType.Rock) ||
      (cbData.sizeClass == CelestialBodySizeClass.Small && cbData.subType == CelestialBodySubType.Rock)
    ){
      density = smallIronCoreTable.Get(roll);
    }
    //Large iron core
    else {
      density = largeIronCoreTable.Get(roll);
    }

    cbData.densityEarthUnits = density;

    float minSizeConstraint = 0f;
    float maxSizeConstraint = 0f;

    switch(cbData.sizeClass){
      case CelestialBodySizeClass.Large:
        minSizeConstraint = 0.065f;
        maxSizeConstraint = 0.091f;
        break;
      case CelestialBodySizeClass.Standard:
        minSizeConstraint = 0.030f;
        maxSizeConstraint = 0.065f;
        break;
      case CelestialBodySizeClass.Small:
        minSizeConstraint = 0.024f;
        maxSizeConstraint = 0.030f;
        break;
      case CelestialBodySizeClass.Tiny:
        minSizeConstraint = 0.004f;
        maxSizeConstraint = 0.024f;
        break;
    }

    earthDiameters = Mathf.Sqrt(cbData.blackbodyTempK / cbData.densityEarthUnits) * Random.Range(minSizeConstraint, maxSizeConstraint);
    cbData.diameterM = earthDiameters * Constants.EARTH_DIAMETER_M;
    cbData.surfaceGravityGs = earthDiameters * cbData.densityEarthUnits;

    cbData.earthMasses = cbData.densityEarthUnits * Mathf.Pow(earthDiameters, 3f);
  }

  void SetCbAtmospherePressure(CelestialBodyData cbData){
    cbData.atmospherePressure = CelestialBodyAtmospherePressure.None;

    //Worlds with no atmosphere
    if(
      cbData.type == CelestialBodyType.AsteroidBelt ||
      (cbData.sizeClass == CelestialBodySizeClass.Tiny) ||
      (cbData.sizeClass == CelestialBodySizeClass.Small && cbData.subType == CelestialBodySubType.Hadean)
    ){
      return;
    }

    //Worlds with automatically trace atmospheres
    if(
      (cbData.sizeClass == CelestialBodySizeClass.Small && cbData.subType == CelestialBodySubType.Rock) ||
      (cbData.subType == CelestialBodySubType.Chthonian)
    ){
      cbData.atmospherePressure = CelestialBodyAtmospherePressure.Trace;
      return;
    }

    var pressureFactor = 0f;
    if(cbData.type == CelestialBodyType.Terrestrial){
      if(cbData.sizeClass == CelestialBodySizeClass.Small && cbData.subType == CelestialBodySubType.Ice){
        pressureFactor = 10f;
      }
      if(cbData.sizeClass == CelestialBodySizeClass.Standard && cbData.subType != CelestialBodySubType.Greenhouse){
        pressureFactor = 1f;
      }
      if(cbData.sizeClass == CelestialBodySizeClass.Standard && cbData.subType == CelestialBodySubType.Greenhouse){
        pressureFactor = 100f;
      }
      if(cbData.sizeClass == CelestialBodySizeClass.Large && cbData.subType != CelestialBodySubType.Greenhouse){
        pressureFactor = 5f;
      }
      if(cbData.sizeClass == CelestialBodySizeClass.Large && cbData.subType == CelestialBodySubType.Greenhouse){
        pressureFactor = 500f;
      }
    }

    var pressureAtm = cbData.atmosphericMass * pressureFactor * cbData.surfaceGravityGs;

    //TODO: this is all wrong and not based off the mass yet
    if(pressureAtm <= 0.4f){
      cbData.atmospherePressure = CelestialBodyAtmospherePressure.Trace;
    }else if (pressureAtm <= 0.8f){
      cbData.atmospherePressure = CelestialBodyAtmospherePressure.Thin;
    }else if (pressureAtm <= 1.2f){
      cbData.atmospherePressure = CelestialBodyAtmospherePressure.Standard;
    }else if (pressureAtm <= 1.8f){
      cbData.atmospherePressure = CelestialBodyAtmospherePressure.Dense;
    }else{
      cbData.atmospherePressure = CelestialBodyAtmospherePressure.Superdense;
    }

  }

  static DiceProbabilityTable<CelestialBodyPropertyActivity> volcanicActivityTable = new DiceProbabilityTable<CelestialBodyPropertyActivity>(){
    table = new DiceProbEntry<CelestialBodyPropertyActivity>[] {
      new DiceProbEntry<CelestialBodyPropertyActivity>() { max = 16,           value = CelestialBodyPropertyActivity.None },
      new DiceProbEntry<CelestialBodyPropertyActivity>() { min = 17, max = 20, value = CelestialBodyPropertyActivity.Light },
      new DiceProbEntry<CelestialBodyPropertyActivity>() { min = 21, max = 26, value = CelestialBodyPropertyActivity.Moderate  },
      new DiceProbEntry<CelestialBodyPropertyActivity>() { min = 27, max = 70, value = CelestialBodyPropertyActivity.Heavy  },
      new DiceProbEntry<CelestialBodyPropertyActivity>() { min = 71,           value = CelestialBodyPropertyActivity.Extreme  },
    }
  };
  static DiceProbabilityTable<CelestialBodyPropertyActivity> tectonicActivityTable = new DiceProbabilityTable<CelestialBodyPropertyActivity>(){
    table = new DiceProbEntry<CelestialBodyPropertyActivity>[] {
      new DiceProbEntry<CelestialBodyPropertyActivity>() { max =  6,           value = CelestialBodyPropertyActivity.None },
      new DiceProbEntry<CelestialBodyPropertyActivity>() { min =  7, max = 10, value = CelestialBodyPropertyActivity.Light },
      new DiceProbEntry<CelestialBodyPropertyActivity>() { min = 11, max = 14, value = CelestialBodyPropertyActivity.Moderate  },
      new DiceProbEntry<CelestialBodyPropertyActivity>() { min = 15, max = 18, value = CelestialBodyPropertyActivity.Heavy  },
      new DiceProbEntry<CelestialBodyPropertyActivity>() { min = 19,           value = CelestialBodyPropertyActivity.Extreme  },
    }
  };

  void SetCbGeologicActivity(CelestialBodyData cbData, GeneratedStarData starData, CelestialBodyData parentCbData){
    if(cbData.type != CelestialBodyType.Terrestrial){
      cbData.volcanicActivity = CelestialBodyPropertyActivity.None;
      cbData.tectonicActivity = CelestialBodyPropertyActivity.None;
      return;
    }

    var volcanicModifier = Mathf.RoundToInt((cbData.surfaceGravityGs / starData.age) * 40f);
    var moonCount = cbData.childCelestialData.Count();

    //Terrestrial planets with moons
    if(parentCbData == null && cbData.type == CelestialBodyType.Terrestrial){
      if(moonCount == 1){
        volcanicModifier += 5;
      }else if(moonCount > 1){
        volcanicModifier += 10;
      }
    }
    if(cbData.subType == CelestialBodySubType.Sulfur){
      volcanicModifier += 60;
    }
    if(parentCbData != null && parentCbData.type == CelestialBodyType.GasGiant){
      volcanicModifier += 5;
    }

    var roll = RandomExtensions.RollDice(3, 6) + volcanicModifier;

    cbData.volcanicActivity = volcanicActivityTable.Get(roll);

    var tectonicModifier = 0;

    switch(cbData.volcanicActivity){
      case CelestialBodyPropertyActivity.None:     tectonicModifier = -8; break;
      case CelestialBodyPropertyActivity.Light:    tectonicModifier = -4; break;
      case CelestialBodyPropertyActivity.Moderate: tectonicModifier =  0; break;
      case CelestialBodyPropertyActivity.Heavy:    tectonicModifier =  4; break;
      case CelestialBodyPropertyActivity.Extreme:  tectonicModifier =  8; break;
    }

    if(cbData.hydrographicCoverage == 0){
      tectonicModifier -= 4;
    }else if(cbData.hydrographicCoverage <= 0.5f){
      tectonicModifier -= 2;
    }

    if(parentCbData == null && cbData.type == CelestialBodyType.Terrestrial){
      if(moonCount == 1){
        tectonicModifier += 2;
      }else if(moonCount > 1){
        tectonicModifier += 4;
      }
    }

    var tectonicRoll = RandomExtensions.RollDice(3, 6) + tectonicModifier;

    cbData.tectonicActivity = tectonicActivityTable.Get(tectonicRoll);
  }

  static DiceProbabilityTable<CelestialBodyPropertyActivity> radiationActivityTable = new DiceProbabilityTable<CelestialBodyPropertyActivity>(){
    table = new DiceProbEntry<CelestialBodyPropertyActivity>[] {
      new DiceProbEntry<CelestialBodyPropertyActivity>() { max = 2,          value = CelestialBodyPropertyActivity.None },
      new DiceProbEntry<CelestialBodyPropertyActivity>() { min = 3, max = 4, value = CelestialBodyPropertyActivity.Light },
      new DiceProbEntry<CelestialBodyPropertyActivity>() { min = 5, max = 6, value = CelestialBodyPropertyActivity.Moderate  },
      new DiceProbEntry<CelestialBodyPropertyActivity>() { min = 7, max = 9, value = CelestialBodyPropertyActivity.Heavy  },
      new DiceProbEntry<CelestialBodyPropertyActivity>() { min = 10,         value = CelestialBodyPropertyActivity.Extreme  },
    }
  };

  void SetCbRadiationActivity(CelestialBodyData cbData, GeneratedStarData starData, CelestialBodyData parentCbData){
    var modifier = 0;

    if(cbData.blackbodyTempK >= 600){
      modifier += 8;
    }else if(cbData.blackbodyTempK >= 550){
      modifier += 7;
    }else if(cbData.blackbodyTempK >= 500){
      modifier += 6;
    }else if(cbData.blackbodyTempK >= 450){
      modifier += 5;
    }else if(cbData.blackbodyTempK >= 350){
      modifier += 4;
    }else if(cbData.blackbodyTempK >= 250){
      modifier += 3;
    }else if(cbData.blackbodyTempK >= 175){
      modifier += 2;
    }else if(cbData.blackbodyTempK > 100){
      modifier += 1;
    }


    //Gas giant moons in their radiation belts
    if(parentCbData != null && parentCbData.type == CelestialBodyType.GasGiant){
      if(cbData.parentIndex == 0){
        modifier += 5;
      }else if(cbData.parentIndex == 1){
        modifier += 4;
      }else{
        modifier += 3;
      }
    }

    switch(cbData.atmospherePressure){
      case CelestialBodyAtmospherePressure.Trace:
        modifier -= 1;
        break;
      case CelestialBodyAtmospherePressure.Thin:
        modifier -= 2;
        break;
      case CelestialBodyAtmospherePressure.Standard:
        modifier -= 3;
        break;
      case CelestialBodyAtmospherePressure.Dense:
        modifier -= 4;
        break;
      case CelestialBodyAtmospherePressure.Superdense:
        modifier -= 5;
        break;
    }

    var roll = RandomExtensions.RollDice(1, 6) + modifier;

    cbData.radiationActivity = radiationActivityTable.Get(roll);
  }

  void SetCbHabitability(CelestialBodyData cbData){
    var score = 0;
    //Geologic activity modifiers
    if(cbData.volcanicActivity == CelestialBodyPropertyActivity.Heavy ||
       cbData.tectonicActivity == CelestialBodyPropertyActivity.Extreme){
      score = -1;
    }
    if(cbData.volcanicActivity == CelestialBodyPropertyActivity.Extreme){
      score = -2;
    }

    //Atmosphere modifiers
    if(cbData.atmospherePressure != CelestialBodyAtmospherePressure.None &&
       cbData.atmospherePressure != CelestialBodyAtmospherePressure.Trace){
      if(cbData.atmosphereType == CelestialBodyAtmosphereType.Suffocating ){
        score -= 1;
      }

      if(cbData.atmosphereType == CelestialBodyAtmosphereType.Toxic){
        score -= 2;
      }

      if(
        cbData.atmosphereType == CelestialBodyAtmosphereType.DeadlyToxic ||
        cbData.atmosphereType == CelestialBodyAtmosphereType.Corrosive
      ){
        score -= 3;
      }

      if(
        cbData.atmosphereType == CelestialBodyAtmosphereType.Standard ||
        cbData.atmosphereType == CelestialBodyAtmosphereType.Marginal
      ){
        switch(cbData.atmospherePressure){
          case CelestialBodyAtmospherePressure.Thin:       score += 2; break;
          case CelestialBodyAtmospherePressure.Standard:   score += 3; break;
          case CelestialBodyAtmospherePressure.Dense:      score += 3; break;
          case CelestialBodyAtmospherePressure.Superdense: score += 1; break;
        }

        if(cbData.atmosphereType == CelestialBodyAtmosphereType.Standard){
          score += 1;
        }
      }
    }else if(cbData.atmospherePressure == CelestialBodyAtmospherePressure.Trace){
      if(cbData.atmosphereType == CelestialBodyAtmosphereType.Suffocating ||
         cbData.atmosphereType == CelestialBodyAtmosphereType.Toxic ||
         cbData.atmosphereType == CelestialBodyAtmosphereType.DeadlyToxic ||
         cbData.atmosphereType == CelestialBodyAtmosphereType.Corrosive
      ){
        score -= 1;
      }

      if(
        cbData.atmosphereType == CelestialBodyAtmosphereType.Standard ||
        cbData.atmosphereType == CelestialBodyAtmosphereType.Marginal
      ){
        score += 1;
      }
    }

    //Climate
    switch(cbData.climate){
      case CelestialBodyClimate.Frozen:
      case CelestialBodyClimate.Infernal:
        score += -1;
        break;
      case CelestialBodyClimate.VeryCold:
      case CelestialBodyClimate.VeryHot:
        score += 0;
        break;
      case CelestialBodyClimate.Cold:
      case CelestialBodyClimate.Hot:
        score += 1;
        break;
      case CelestialBodyClimate.Chilly:
      case CelestialBodyClimate.Cool:
      case CelestialBodyClimate.Warm:
      case CelestialBodyClimate.Tropical:
        score += 3;
        break;
      case CelestialBodyClimate.Normal:
        score += 4;
        break;
    }


    //Hydro modifiers
    if(
      cbData.subType == CelestialBodySubType.Ice ||
      cbData.subType == CelestialBodySubType.Rock ||
      cbData.subType == CelestialBodySubType.Garden ||
      cbData.subType == CelestialBodySubType.Ocean ||
      cbData.subType == CelestialBodySubType.Hadean //questionable if this is water ocean
    ){
      if(cbData.hydrographicCoverage > 0 && cbData.hydrographicCoverage < 0.6f){
        score += 2;
      }else if(cbData.hydrographicCoverage >= 0.6f && cbData.hydrographicCoverage < 0.9f){
        score += 3;
      }else if(cbData.hydrographicCoverage >= 0.9f){
        score += 1;
      }
    }

    //gravity, assuming gas giant settlement will be orbiting it
    if(cbData.type != CelestialBodyType.GasGiant){
      if(cbData.surfaceGravityGs > 0.5f && cbData.surfaceGravityGs < 1.5f){
        score += 1;
      }else if(cbData.surfaceGravityGs > 2f){
        score -= 1;
      }
    }

    //radiation level
    switch(cbData.radiationActivity){
      case CelestialBodyPropertyActivity.None:         score += 1; break;
      case CelestialBodyPropertyActivity.Moderate:     score -= 1; break;
      case CelestialBodyPropertyActivity.Heavy:        score -= 2; break;
      case CelestialBodyPropertyActivity.Extreme:      score -= 3; break;
    }

    //Rings
    if(cbData.rings != null && cbData.rings.Length > 0){
      score -= 1;
    }

    cbData.habitabilityScore = score;

    if(score <= -3){
      cbData.habitability = CelestialBodyHabitability.Atrocious;
    }else if (score < 0){
      cbData.habitability = CelestialBodyHabitability.Terrible;
    }else if (score == 0){
      cbData.habitability = CelestialBodyHabitability.Poor;
    }else if (score <= 2){
      cbData.habitability = CelestialBodyHabitability.Moderate;
    }else if (score <= 6){
      cbData.habitability = CelestialBodyHabitability.Wonderful;
    }else {
      cbData.habitability = CelestialBodyHabitability.Excellent;
    }
  }

  static DiceProbabilityTable<ResourceAbundance> asteroidBeltResourceTable = new DiceProbabilityTable<ResourceAbundance>(){
    table = new DiceProbEntry<ResourceAbundance>[] {
      new DiceProbEntry<ResourceAbundance>() { max =  3,           value = ResourceAbundance.Worthless },
      new DiceProbEntry<ResourceAbundance>() { min =  4, max = 5,  value = ResourceAbundance.Scant },
      new DiceProbEntry<ResourceAbundance>() { min =  6, max = 8,  value = ResourceAbundance.Poor },
      new DiceProbEntry<ResourceAbundance>() { min =  9, max = 11, value = ResourceAbundance.Average },
      new DiceProbEntry<ResourceAbundance>() { min = 12, max = 14, value = ResourceAbundance.Abundant },
      new DiceProbEntry<ResourceAbundance>() { min = 15, max = 16, value = ResourceAbundance.Rich },
      new DiceProbEntry<ResourceAbundance>() { min = 17,           value = ResourceAbundance.Motherlode },
    }
  };

  static DiceProbabilityTable<ResourceAbundance> normalResourceTable = new DiceProbabilityTable<ResourceAbundance>(){
    table = new DiceProbEntry<ResourceAbundance>[] {
      new DiceProbEntry<ResourceAbundance>() { max =  6,           value = ResourceAbundance.Scant },
      new DiceProbEntry<ResourceAbundance>() { min =  7, max = 9,  value = ResourceAbundance.Poor },
      new DiceProbEntry<ResourceAbundance>() { min = 10, max = 12, value = ResourceAbundance.Average },
      new DiceProbEntry<ResourceAbundance>() { min = 13, max = 15, value = ResourceAbundance.Abundant },
      new DiceProbEntry<ResourceAbundance>() { min = 16,           value = ResourceAbundance.Rich },
    }
  };

  static DiceProbabilityTable<ushort> resourceCountTable = new DiceProbabilityTable<ushort>(){
    table = new DiceProbEntry<ushort>[] {
      new DiceProbEntry<ushort>() { max =  4,           value = 0 },
      new DiceProbEntry<ushort>() { min =  5, max = 10, value = 1 },
      new DiceProbEntry<ushort>() { min = 11, max = 16, value = 2 },
      new DiceProbEntry<ushort>() { min = 17,           value = 3 },
    }
  };

  void SetCbResources(CelestialBodyData cbData, GeneratedStarData starData){
    var resourceCount = GetCelestialBodyNumberOfResources(cbData);

    //Cap the number of resources per system
    cbData.resourceDeposits = GetCelestialBodyResourceDeposits(resourceCount, cbData, starData.resourceProbabilities);
  }

  ushort GetCelestialBodyNumberOfResources(CelestialBodyData cbData){
    var resourceModifier = 0;
    if(cbData.type == CelestialBodyType.Terrestrial){
      switch(cbData.tectonicActivity){
        case CelestialBodyPropertyActivity.None:     resourceModifier = -2; break;
        case CelestialBodyPropertyActivity.Light:    resourceModifier = -1; break;
        case CelestialBodyPropertyActivity.Moderate: resourceModifier =  0; break;
        case CelestialBodyPropertyActivity.Heavy:    resourceModifier =  1; break;
        case CelestialBodyPropertyActivity.Extreme:  resourceModifier =  2; break;
      }
    }

    var resourceAbundanceRoll = RandomExtensions.RollDice(3, 6) + resourceModifier;
    return resourceCountTable.Get(resourceAbundanceRoll);
  }

  WeightedProbabilityTable<GameResourceType> resourceTable = new WeightedProbabilityTable<GameResourceType>();

  CelestialResourceDeposit[] GetCelestialBodyResourceDeposits(
    ushort number,
    CelestialBodyData cbData,
    List<GalaxyResource.ResourceProbability> resourceProbabilities
  ){
    if(number == 0){ return null; }

    resourceTable.Clear();
    for(var p = 0; p < resourceProbabilities.Count; p++){
      var resourceProb = resourceProbabilities[p];
      var probabilityWeight = resourceProb.probability;
      if(p > 2){
        //For the 4 and up resources, dramatically ramp off their probabilities
        //So it's rare to have a system with more than 3 different kinds of resources
        probabilityWeight = probabilityWeight / (p + 1);
      }

      resourceTable.Add(probabilityWeight, resourceProb.type);
    }

    var ret = new CelestialResourceDeposit[number];
    for(var i = 0; i < number; i++){
      GameResourceType type;
      //Get a new resource type making sure we haven't got it already
      do{
        type = resourceTable.GetNext();
      }while(ret.Any(x => x != null && x.type == type));

      //Find resource abundance
      var resourceModifier = 0;
      if(cbData.type == CelestialBodyType.Terrestrial){
        switch(cbData.volcanicActivity){
          case CelestialBodyPropertyActivity.None:     resourceModifier = -2; break;
          case CelestialBodyPropertyActivity.Light:    resourceModifier = -1; break;
          case CelestialBodyPropertyActivity.Moderate: resourceModifier =  0; break;
          case CelestialBodyPropertyActivity.Heavy:    resourceModifier =  1; break;
          case CelestialBodyPropertyActivity.Extreme:  resourceModifier =  2; break;
        }
      }

      //Bonus for being the 1st or 2nd in the solar resource probabilities, penalty for being above 3
      var resourceProbIndex = resourceProbabilities.FindIndex(x => x.type == type);
      resourceModifier += Mathf.Clamp( 2 - resourceProbIndex, -3, 2);

      var resourceAbundanceRoll = RandomExtensions.RollDice(3, 6) + resourceModifier;
      ResourceAbundance abundance = ResourceAbundance.Worthless;
      if(cbData.type == CelestialBodyType.AsteroidBelt){
        abundance = asteroidBeltResourceTable.Get(resourceAbundanceRoll);
      }else{
        abundance = normalResourceTable.Get(resourceAbundanceRoll);
      }

      ret[i] = new CelestialResourceDeposit(){
        type = type,
        abundance = abundance,
      };
    }

    //special sauce for silicate gas giants to make sure they always have silicon
    if(cbData.subType == CelestialBodySubType.Silicate && !ret.Any(r => r.type == GameResourceType.Silicon)){
      ret[0].type = GameResourceType.Silicon;
    }

    return ret;
  }

  public StarPlanet? FindStartingPlanet(Galaxy galaxy){
    const int minSystemPlanetCount = 3;
    const int minHabitabilityScore = 6;

    var filteredList = galaxy.generatedStars.Values.Where(star =>
      star.position.magnitude < maxStartingWorldDistance * galaxy.radius &&
      star.position.magnitude > minStartingWorldDistance * galaxy.radius
    );

    Logger.Log("Starting stars at correct distance: " + filteredList.Count());

    filteredList = filteredList.Where(star => star.childCelestialData.Count > minSystemPlanetCount);

    Logger.Log("Starting stars with enough cbs: " + filteredList.Count());

    var market1Range = GalaxyBuilding.allBuildings[GalaxyBuildingId.Market1].influenceRadiusLy;

    filteredList = filteredList.Where(star => {

      //make sure there are some other systems within the market level 1 range
      var systemsNeededInRange = 4;
      var systemsInRange = 0;
      foreach(var otherStar in galaxy.generatedStars.Values){
        if(otherStar.id == star.id){ continue; }

        var distLy = Vector2.Distance(otherStar.position, star.position) * Galaxy.distanceScale[GalaxyViewMode.Galaxy];
        if(distLy <= market1Range){ systemsInRange++; }

        if(systemsInRange >= systemsNeededInRange){
          break;
        }
      }

      return systemsInRange >= systemsNeededInRange;
    });

    Logger.Log("Starting stars with enough neighbors: " + filteredList.Count());


    filteredList = filteredList.Where(star =>
      star.childCelestialData.Any(cbData => cbData.habitabilityScore >= minHabitabilityScore) &&
      !star.childCelestialData.Any(ccd =>
        ccd.resourceDeposits != null && ccd.resourceDeposits.Any(ccdr => ccdr.type == GameResourceType.Promethium)
    ));

    Logger.Log("Starting stars with favorable conditions: " + filteredList.Count());

    foreach(var star in filteredList){

      foreach(var cbData in star.childCelestialData){
        if(
          cbData.habitabilityScore >= minHabitabilityScore
        ){
          return new StarPlanet(){
            star = star,
            planetData = cbData
          };
        }
      }
    }

    //should never happen lol
    return null;
  }

  //Starting CB always gets fixed resources to make sure the player can bootstrap
  public void SetStartingSystemSpecialSauce(StarPlanet startingInfo){
    startingInfo.planetData.resourceDeposits = new CelestialResourceDeposit[]{
      new CelestialResourceDeposit(){
        type = GameResourceType.Iron,
        abundance = ResourceAbundance.Average
      },
      new CelestialResourceDeposit(){
        type = GameResourceType.Phosphorus,
        abundance = ResourceAbundance.Average
      },
    };

    startingInfo.planetData.rings = null;

    //Whatever species developed would define it as 1g lol
    startingInfo.planetData.surfaceGravityGs = 1f;

    //set next orbit out
    var cbOrbitIndex = startingInfo.star.childCelestialData.IndexOf(startingInfo.planetData);
    var originalNextCb = startingInfo.star.childCelestialData[cbOrbitIndex + 1];

    var constructedCbData = new CelestialBodyData(){
      id = originalNextCb.id,
      type = CelestialBodyType.Terrestrial,
      sizeClass = CelestialBodySizeClass.Small,
      subType = CelestialBodySubType.Rock,
      parentStarId = startingInfo.star.id,
      parentId = startingInfo.star.id,

      parentIndex = (ushort)(cbOrbitIndex + 1),
      radius = 0.6f,
      parentDistanceAU = originalNextCb.parentDistanceAU,
      diameterM = Constants.MARS_EQUIV_DIAMETER_M,
      blackbodyTempK = 299,
      atmosphericMass = 0,
      atmospherePressure = CelestialBodyAtmospherePressure.Trace,
      atmosphereType = CelestialBodyAtmosphereType.None,
      hydrographicCoverage = 0,
      averageSurfaceTempK = 288,
      climate = CelestialBodyClimate.VeryCold,
      densityEarthUnits = 0.9f,
      surfaceGravityGs = 0.43f,
      earthMasses = 0.15f,
      volcanicActivity = CelestialBodyPropertyActivity.None,
      tectonicActivity = CelestialBodyPropertyActivity.None,
      radiationActivity = CelestialBodyPropertyActivity.Light,
      habitabilityScore = 0,
      habitability = CelestialBodyHabitability.Moderate,
      resourceDeposits = new CelestialResourceDeposit[]{
        new CelestialResourceDeposit(){
          type = GameResourceType.Titanium,
          abundance = ResourceAbundance.Scant
        }
      },
    };

    galaxy.celestials[constructedCbData.id] = constructedCbData;
    startingInfo.star.childCelestialData[cbOrbitIndex + 1] = constructedCbData;

    //Make sure all bodies in the system have only the starting resources
    foreach(var cbData in startingInfo.star.childCelestialData){
      SetStartingSystemResourceDeposits(cbData);

      foreach(var childCbData in cbData.childCelestialData){
        SetStartingSystemResourceDeposits(childCbData);
      }
    }

    startingInfo.star.name = "Sol";
  }

  void SetStartingSystemResourceDeposits(CelestialBodyData cbData){
    var startingSystemResources = new GameResourceType[]{
      GameResourceType.Iron,
      GameResourceType.Phosphorus,
      GameResourceType.Titanium,
    };

    //Deterministically set the resources to the starting resources if it's not a starting resource
    if(cbData.resourceDeposits != null){
      for(var i = 0; i < cbData.resourceDeposits.Length; i++){
        if(!startingSystemResources.Contains(cbData.resourceDeposits[i].type)){
          cbData.resourceDeposits[i].type = startingSystemResources[(i + cbData.id) % startingSystemResources.Length];
        }
      }
      //Make sure we don't end up with duplicates
      cbData.resourceDeposits = cbData.resourceDeposits.GroupBy(p => p.type).Select(g => g.First()).ToArray();
    }
  }

  public const float noiseScale = 0.003f;

  public float SampleNoise(Vector3 position){
    return Mathf.PerlinNoise(position.x * noiseScale, position.z * noiseScale);
  }

  public void OnDrawGizmos(){
    if(galaxy == null){
      return;
    }

    Draw.Thickness = 1f;
    Draw.ThicknessSpace = ThicknessSpace.Pixels;
    Draw.Ring(Vector3.zero, galaxy.radius, Color.magenta);

    Draw.Ring(Vector3.zero, minStartingWorldDistance * galaxy.radius, Color.yellow);
    Draw.Ring(Vector3.zero, maxStartingWorldDistance * galaxy.radius, Color.red);
  }
}

public enum GasGiantArrangement{
  None,
  Conventional,
  Eccentric,
  Episteller
}