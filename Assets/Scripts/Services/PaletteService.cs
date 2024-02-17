using System.Collections.Generic;
using System.Linq;
using PygmyMonkey.ColorPalette;
using UnityEngine;

public class PaletteService
{

  Dictionary<string, ColorPalette> paletteCache = new Dictionary<string, ColorPalette>();

  ColorPalette CacheGet(string name){
    if(!paletteCache.ContainsKey(name)){
      paletteCache[name] = ColorPaletteData.Singleton.fromName(name);
    }

    return paletteCache[name];
  }

  public ColorPalette primary {
    get {
      return CacheGet("Primary");
    }
  }

  public ColorPalette topBg {
    get {
      return CacheGet("TopBg");
    }
  }

  public ColorPalette cameraBackground {
    get {
      return CacheGet("Camera Background");
    }
  }

  public ColorPalette stage5 {
    get {
      return CacheGet("Stage 5");
    }
  }

  public ColorPalette resourceAbundances {
    get {
      return CacheGet("Stage 7 Resource Abundances");
    }
  }

  public ColorPalette stage7 {
    get {
      return CacheGet("Stage 7");
    }
  }

  public ColorPalette stage7Planets {
    get {
      return CacheGet("Stage 7 Planets");
    }
  }

  public ColorPalette stage7Atmospheres {
    get {
      return CacheGet("Stage 7 Atmospheres");
    }
  }

  public ColorPalette PlanetPalette(CelestialBodySubType subType, uint id) {
    var planetPalettes = PlanetTypePalettes(subType);
    if(planetPalettes.Count == 0){
      Debug.LogWarning("Missing planet palettes for " + subType);
      return stage7Planets;
    }
    return planetPalettes[(int)id % planetPalettes.Count];
  }

  Dictionary<CelestialBodySubType, List<ColorPalette>> planetTypePaletteCache = new Dictionary<CelestialBodySubType, List<ColorPalette>>();

  public List<ColorPalette> PlanetTypePalettes(CelestialBodySubType subType){
    if(!planetTypePaletteCache.ContainsKey(subType)){
      var subTypeString = subType.ToString().ToLower();
      planetTypePaletteCache[subType] = ColorPaletteData.Singleton.colorPaletteList.Where(cp => cp.name.StartsWith(subTypeString)).ToList();
    }
    return planetTypePaletteCache[subType];
  }
}

