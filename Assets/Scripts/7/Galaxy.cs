using System.Collections.Generic;
using UnityEngine;

public class Galaxy{

  public float radius;
  public Dictionary<uint, Star> stars = new Dictionary<uint, Star>();

  public Dictionary<uint, GeneratedStarData> generatedStars = new Dictionary<uint, GeneratedStarData>();

  public Dictionary<uint, CelestialBodyData> celestials = new Dictionary<uint, CelestialBodyData>();

  public List<BgStarData> bgStarData = new List<BgStarData>();
  public HashSet<BgStar> bgStars = new HashSet<BgStar>();

  public List<GalaxyShip> ships = new List<GalaxyShip>();

  //World coord bounds
  public Bounds GetBounds(){

    var center = Vector2.zero;
    var size = 2 * radius * Vector2.one;

    return new Bounds(center, size);
  }

  public void AddStar(Star star){
    stars[star.generatedData.id] = star;
  }

  public void AddGeneratedStar(GeneratedStarData star){
    generatedStars[star.id] = star;
  }

  public void AddCbd(CelestialBodyData data){
    celestials[data.id] = data;
  }

  /// Conversion from unity world scale to light years at each view scale
  public static Dictionary<GalaxyViewMode, float> distanceScale = new Dictionary<GalaxyViewMode, float>(){
    {GalaxyViewMode.Galaxy, 16f},
    {GalaxyViewMode.System, 0.00112845f},
    {GalaxyViewMode.Planet, 0.00000016252528f},
  };

  //Converts from a distance in AU to a world coordinate amount in the system view
  public static float GetSystemViewScale(float distanceAU){
    float minWorldDist = 0.15f;
    // float maxWorldDist = 0.5f;

    const float maxAU = 40f;

    var logDist = Mathf.Log(distanceAU + 0.04f, 1.38f) + 8f;

    if(float.IsNaN(logDist) || logDist < 0){
      return minWorldDist;
    }

    return logDist / maxAU;
  }

  //Get the stars world size based on the star's radius in AU
  public static float GetStarSizeScale(float radiusAU){
    float minSize = 0.1f;

    var logDist = (Mathf.Log(radiusAU + 0.2f, 1.2f) / 30f) + 0.42f;

    if(float.IsNaN(logDist) || logDist < minSize){
      return minSize;
    }

    return logDist;
  }

  public static float GetCbSizeScale(float earthDiameters){
    float minSize = 0.2f;

    var logDist = (Mathf.Log(earthDiameters, 1.5f) + 8) / 10f;

    if(float.IsNaN(logDist) || logDist < minSize){
      return minSize;
    }

    return logDist;
  }

  //Scale the star radius to this % when viewing at the system or planet level
  public static float SystemViewStarScalePct = 0.5f;
}

public struct StarPlanet{
  public GeneratedStarData star;
  public CelestialBodyData planetData;
}

public struct BgStarData {
  public Vector3 position;
  public float size;
}

public enum GalaxyViewMode{
  Galaxy,
  System,
  Planet
}