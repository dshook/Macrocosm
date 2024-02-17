using System;
using UnityEngine;
using System.Collections.Generic;
using static HexGrid;

public class HexPathCache {
  const int maxCacheSize = 50;

  private struct CacheData {
    public List<HexCoordinates> route;
    public int frameAdded;
  }
  private Dictionary<PathfindOptions, CacheData> cache =
    new Dictionary<PathfindOptions, CacheData>();

  public void AddPath(PathfindOptions options, List<HexCoordinates> route){
    //clear out the old if we're at capacity
    if(cache.Keys.Count >= maxCacheSize){
      var minFrame = Int32.MaxValue;
      PathfindOptions? minKey = null;
      foreach(var cacheKV in cache){
        if(cacheKV.Value.frameAdded < minFrame){
          minKey = cacheKV.Key;
        }
      }
      if(minKey.HasValue){ cache.Remove(minKey.Value); }
    }

    cache[options] = new CacheData() {
      route = route,
      frameAdded = Time.frameCount
    };
  }

  public bool HasPath(PathfindOptions options){
    return cache.ContainsKey(options);
  }

  public List<HexCoordinates> GetPath(PathfindOptions options){
    if(cache.ContainsKey(options)){
      return cache[options].route;
    }

    return null;
  }


  public void Clear(){
    cache.Clear();
  }

}