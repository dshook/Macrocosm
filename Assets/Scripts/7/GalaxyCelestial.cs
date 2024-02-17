using System;
using UnityEngine;
using strange.extensions.mediation.impl;
using System.Collections.Generic;
using System.Linq;
using Shapes;

//Base class for stars and celestial bodies
public abstract class GalaxyCelestial : View {
  [Inject] protected StageRulesService stageRules { get; set; }
  [Inject] protected StageSevenDataModel stageSevenData {get; set;}
  [Inject] protected StageSixDataModel stageSixData {get; set;}
  [Inject] protected ResourceLoaderService loader {get; set;}

  public ColorFader colorFader;
  public ShapeRenderer baseShapeRenderer;
  public GameObject display;
}

[Serializable]
public abstract class GalaxyCelestialData{
  //Should be readonly but can't set that in a base class for some dumb reason
  public uint id;
}

//Data that's generated from the map but not saved
[System.Serializable]
public class GeneratedStarData : GalaxyCelestialData {
  public string name;
  public SpectralType type;
  public ushort subType;
  public float solarMass; //in standard solar mass units
  public float age; //in billions of years
  public LuminosityClass luminosityClass;
  public float luminosity;
  public int tempK;
  public float solarRadiusAU;
  public float solarRadius;
  public float innerPlanetRadiusAU;
  public float outerPlanetRadiusAU;
  public float snowLineAU;
  public Vector2 position;
  public List<CelestialBodyData> childCelestialData;
  public List<GalaxyResource.ResourceProbability> resourceProbabilities;
  public bool inhabited;
}

//Data that's generated but not saved
[System.Serializable]
public class CelestialBodyData : GalaxyCelestialData{
  public CelestialBodyType type;
  public CelestialBodySizeClass sizeClass;
  public CelestialBodySubType subType;
  public uint parentStarId;
  public uint parentId; //Either the star or other CB
  public ushort parentIndex; //index of the orbit from parent body from near to far
  public float radius;
  public float parentDistance; //In world coords, as an override
  public float parentDistanceAU;
  public float diameterM;
  public float blackbodyTempK;
  public float atmosphericMass;
  public CelestialBodyAtmospherePressure atmospherePressure = CelestialBodyAtmospherePressure.None;
  public CelestialBodyAtmosphereType atmosphereType = CelestialBodyAtmosphereType.None;
  public float hydrographicCoverage; //percentage
  public float averageSurfaceTempK;
  public CelestialBodyClimate climate;
  public float densityEarthUnits;
  public float surfaceGravityGs;
  public float earthMasses;
  public CelestialBodyPropertyActivity volcanicActivity;
  public CelestialBodyPropertyActivity tectonicActivity;
  public CelestialBodyPropertyActivity radiationActivity;
  public int habitabilityScore;
  public CelestialBodyHabitability habitability;

  //ring data
  public CelestialRing[] rings;
  public Vector3 ringRotation;

  public CelestialResourceDeposit[] resourceDeposits;

  //Hack for unity nested serialization weirdness.
  //You can only have 7 levels and unity will always serialize 7 levels even if things are null
  //https://forum.unity.com/threads/4-5-serialization-depth.248321

  //Doing it this way since we should have a fairly limited number of moons on a planet
  [SerializeField] CelestialBodyData child0;
  [SerializeField] CelestialBodyData child1;
  [SerializeField] CelestialBodyData child2;
  [SerializeField] CelestialBodyData child3;
  [SerializeField] CelestialBodyData child4;
  [SerializeField] CelestialBodyData child5;
  [SerializeField] CelestialBodyData child6;

  public IEnumerable<CelestialBodyData> childCelestialData{
    get{
      if(child0 != null){ yield return child0; }
      if(child1 != null){ yield return child1; }
      if(child2 != null){ yield return child2; }
      if(child3 != null){ yield return child3; }
      if(child4 != null){ yield return child4; }
      if(child5 != null){ yield return child5; }
      if(child6 != null){ yield return child6; }
    }
    set{
      if(value == null){
        child0 = null;
        child1 = null;
        child2 = null;
        child3 = null;
        child4 = null;
        child5 = null;
        child6 = null;
        return;
      }
      var valueList = value.ToList();
      if(valueList.Count > 0){ child0 = valueList[0]; } else { child0 = null; }
      if(valueList.Count > 1){ child1 = valueList[1]; } else { child1 = null; }
      if(valueList.Count > 2){ child2 = valueList[2]; } else { child2 = null; }
      if(valueList.Count > 3){ child3 = valueList[3]; } else { child3 = null; }
      if(valueList.Count > 4){ child4 = valueList[4]; } else { child4 = null; }
      if(valueList.Count > 5){ child5 = valueList[5]; } else { child5 = null; }
      if(valueList.Count > 6){ child6 = valueList[6]; } else { child6 = null; }

      if(valueList.Count > 7){
        Debug.LogError("Max child celestial bodies exceeded");
        return;
      }
    }
  }

  public static Dictionary<CelestialBodyType, string> cbTypeDescrip = new Dictionary<CelestialBodyType, string>(){
    {CelestialBodyType.AsteroidBelt, "Asteroid Belt"},
    {CelestialBodyType.Terrestrial, "Planet"},
    {CelestialBodyType.GasGiant, "Gas Giant"},
  };

  public static Dictionary<CelestialBodySubType, string> cbSubTypeDescrip = new Dictionary<CelestialBodySubType, string>(){
    {CelestialBodySubType.None, ""},
    {CelestialBodySubType.Ice, "Icy"},
    {CelestialBodySubType.Rock, "Rocky"},
    {CelestialBodySubType.Sulfur, "Sulfurous"},
    {CelestialBodySubType.Hadean, "Hadean"},
    {CelestialBodySubType.Ammonia, "Ammonia"},
    {CelestialBodySubType.Garden, "Garden"},
    {CelestialBodySubType.Ocean, "Ocean"},
    {CelestialBodySubType.Greenhouse, "Greenhouse"},
    {CelestialBodySubType.Chthonian, "Chthonian"},
    {CelestialBodySubType.WaterClouds, "Water Cloud"},
    {CelestialBodySubType.Cloudless, "Cloudless"},
    {CelestialBodySubType.AlkaliMetal, "Alkali Metal"},
    {CelestialBodySubType.Silicate, "Silicate"},
  };

  public static Dictionary<CelestialBodyClimate, string> cbClimateDescrip = new Dictionary<CelestialBodyClimate, string>(){
    {CelestialBodyClimate.None, "None" },
    {CelestialBodyClimate.Frozen, "Frozen" },
    {CelestialBodyClimate.VeryCold, "Very Cold" },
    {CelestialBodyClimate.Cold, "Cold" },
    {CelestialBodyClimate.Chilly, "Chilly" },
    {CelestialBodyClimate.Cool, "Cool" },
    {CelestialBodyClimate.Normal, "Normal" },
    {CelestialBodyClimate.Warm, "Warm" },
    {CelestialBodyClimate.Tropical, "Tropical" },
    {CelestialBodyClimate.Hot, "Hot" },
    {CelestialBodyClimate.VeryHot, "Very Hot" },
    {CelestialBodyClimate.Infernal,  "Infernal" }
  };

  public static Dictionary<CelestialBodyAtmospherePressure, string> cbAtmospherePressureDescrip = new Dictionary<CelestialBodyAtmospherePressure, string>(){
    {CelestialBodyAtmospherePressure.None, "None" },
    {CelestialBodyAtmospherePressure.Trace, "Trace" },
    {CelestialBodyAtmospherePressure.Thin, "Thin" },
    {CelestialBodyAtmospherePressure.Standard, "Standard" },
    {CelestialBodyAtmospherePressure.Dense, "Dense" },
    {CelestialBodyAtmospherePressure.Superdense, "Superdense" },
  };

  public static Dictionary<CelestialBodyAtmosphereType, string> cbAtmosphereDescrip = new Dictionary<CelestialBodyAtmosphereType, string>(){
    {CelestialBodyAtmosphereType.None, "" },
    {CelestialBodyAtmosphereType.Standard, "Standard" },
    {CelestialBodyAtmosphereType.Marginal, "Marginal" },
    {CelestialBodyAtmosphereType.Suffocating, "Suffocating" },
    {CelestialBodyAtmosphereType.Toxic, "Toxic" },
    {CelestialBodyAtmosphereType.DeadlyToxic, "Deadly" },
    {CelestialBodyAtmosphereType.Corrosive, "Corrosive" },
  };

  public float GravityEfficiency{
    get{
      //Should be 1f (100%) efficient at 1g, less efficient for more g's, more efficient for less
      var eff = 1f + ((1f - surfaceGravityGs) * 0.7f);
      return Mathf.Clamp(eff, 0.1f, 2f);
    }
  }

  //Gets the total world coordinate offset from the star for this CB.  This is tricky because of the parenting setup
  //with moons.  It would be simpler to set local positions but that doesn't work when the CB's aren't instanciated and
  //Any galaxy ship needs to know the position
  public Vector2 GetPositionOffsetFromStar(Galaxy galaxy){
    var parentCelestialBody = parentId != parentStarId ? galaxy.celestials[parentId] : null;

    var worldDistance = Galaxy.GetSystemViewScale(parentDistanceAU);
    if(parentCelestialBody == null){
      return new Vector2(0, worldDistance);
    }else{
      var parentPosition = parentCelestialBody.GetPositionOffsetFromStar(galaxy);
      return parentPosition + new Vector2(parentDistance, 0);
    }
  }
}

//Serializable but not stored in the save
[System.Serializable]
public class CelestialResourceDeposit{
  public GameResourceType type {get; set;}
  public ResourceAbundance abundance {get;set;}
}


[System.Serializable]
public enum CelestialBodyType{
  AsteroidBelt,
  Terrestrial,
  GasGiant,
}

[System.Serializable]
public enum CelestialBodySubType{
  None,
  Ice, //also can be gas giant
  Rock,
  Sulfur,
  Hadean,
  Ammonia, //also can be gas giant
  Garden,
  Ocean,
  Greenhouse,
  Chthonian,

  //Gas giant specific types
  WaterClouds,
  Cloudless,
  AlkaliMetal,
  Silicate
}

[System.Serializable]
public enum CelestialBodyAtmospherePressure{
  None,
  Trace,
  Thin,
  Standard,
  Dense,
  Superdense
}

public enum CelestialBodyAtmosphereType{
  None,
  Standard,
  Marginal,
  Suffocating,
  Toxic,
  DeadlyToxic,
  Corrosive
}

[System.Serializable]
public enum CelestialBodyClimate{
  None,
  Frozen, // below -20
  VeryCold, //-20 to 0
  Cold, // 0 to 20
  Chilly, // 20 to 40
  Cool,  // 40 to 60
  Normal, // 60 to 80
  Warm, // 80 to 100
  Tropical, // 100 to 120
  Hot, // 120 to 140
  VeryHot, //140 to 160
  Infernal //over 160
}

[System.Serializable]
public enum CelestialBodySizeClass{
  Tiny = 0,
  Small = 1,
  Standard = 2,
  Large = 3
}


[System.Serializable]
public enum CelestialBodyPropertyActivity{
  None,
  Light,
  Moderate,
  Heavy,
  Extreme
}

[System.Serializable]
public enum CelestialBodyHabitability{
  Atrocious = 0,
  Terrible = 1,
  Poor = 2,
  Moderate = 3,
  Wonderful = 4,
  Excellent = 5
}

[System.Serializable]
public struct CelestialRing{
  public float radius;
  public float thickness;
  public Color color;
}