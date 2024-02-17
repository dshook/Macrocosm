using UnityEngine;
using System.Collections.Generic;

using System.Linq;

public class ResourceLoaderService
{
  public Dictionary<int, Sprite> particleSvgMap;

  public ResourceLoaderService()
  {
    particleSvgMap = new Dictionary<int, Sprite>(){
      {1, Load<Sprite>("Art/stage1/atoms/1") },
      {2, Load<Sprite>("Art/stage1/atoms/2") },
      {3, Load<Sprite>("Art/stage1/atoms/3") },
      {4, Load<Sprite>("Art/stage1/atoms/4") },
      {5, Load<Sprite>("Art/stage1/atoms/5") },
      {6, Load<Sprite>("Art/stage1/atoms/6") },
      {7, Load<Sprite>("Art/stage1/atoms/7") },
      {8, Load<Sprite>("Art/stage1/atoms/8") },
      {9, Load<Sprite>("Art/stage1/atoms/9") },
      {10, Load<Sprite>("Art/stage1/atoms/10") },
    };
  }

  private Dictionary<string, object> GOCache = new Dictionary<string, object>();
  public T Load<T>(string resource, bool cache = true) where T : class
  {
    if (GOCache.ContainsKey(resource))
    {
      return (T)GOCache[resource];
    }
    var objLoad = Resources.Load(resource, typeof(T));
    var go = objLoad as T;

    if(cache){
      GOCache[resource] = go;
    }

    return go;
  }

  //TODO: maybe come back and add cache someday
  public T[] LoadAll<T>(string resourceFolder) where T : class
  {
    return Resources.LoadAll(resourceFolder, typeof(T)).Cast<T>().ToArray();
  }

  public void Free<T>(string resource) where T: class
  {
    if (GOCache.ContainsKey(resource))
    {
      GOCache.Remove(resource);
    }
  }

  public void FreeAll(){
    GOCache.Clear();
  }

}

