using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

static class DictionaryExtentions
{
  //Note, be careful with this since often the add/update can cause lots of GC with lambda closures or newing up vals for new
  //Wish there were macros...
  public static void AddOrUpdate<K, V>(this Dictionary<K, V> dict, K key, V value, Func<V, V> update)
  {
    V existing;
    if (!dict.TryGetValue(key, out existing))
    {
      dict.Add(key, value);
    }
    else
    {
      dict[key] = update(existing);
    }
  }

  public static V TryGet<K, V>(this Dictionary<K, V> dict, K key){
    if(dict.ContainsKey(key)){
      return dict[key];
    }
    return default(V);
  }

  public static void LogKeyValues<K, V>(this Dictionary<K, V> dict, string label){
    Debug.Log(label + "\n" + string.Join("\n", dict.OrderBy(x => x.Key).Select(x => x.Key + "=" + x.Value).ToArray()));
  }
}

public struct DualKey<T> : IEquatable<DualKey<T>> where T : IEquatable<T>
{
  public T Key0 { get; set; }
  public T Key1 { get; set; }

  public DualKey(T key0, T key1)
  {
    Key0 = key0;
    Key1 = key1;
  }

  public override int GetHashCode()
  {
    return Key0.GetHashCode() ^ Key1.GetHashCode();
  }

  public bool Equals(DualKey<T> obj)
  {
    return (this.Key0.Equals(obj.Key0) && this.Key1.Equals(obj.Key1))
        || (this.Key0.Equals(obj.Key1) && this.Key0.Equals(obj.Key0));
  }
}