using UnityEngine;
using System.Collections.Generic;
using System.Linq;

//Works as a kind of cumulative probability going through each entry and seeing if the
//Rand number meets that criteria
public class ProbabilityTable<T>{

  public Dictionary<T, float> table = new Dictionary<T, float>();

  public void Add(float probability, T value){
    table[value] = probability;
  }

  public void Clear(){
    table.Clear();
  }

  public T GetNext(){
    float accum = 0f;
    var rand = Random.Range(0, 1f);
    foreach(var kv in table){
      accum += kv.Value;
      if(rand < accum){
        return kv.Key;
      }
    }

    //Should never hit unless the probabilities are invalid
    return table.Last().Key;
  }
}

//Takes all the entries probabilities and weights them against each other
public class WeightedProbabilityTable<T>{

  Dictionary<T, float> table = new Dictionary<T, float>();
  float sum = 0f;

  public void Add(float probability, T value){
    table[value] = probability + sum;
    sum += probability;
  }

  public void Clear(){
    sum = 0f;
    table.Clear();
  }

  public T GetNext(){
    var rand = Random.Range(0, sum);
    foreach(var kv in table){
      if(rand <= kv.Value){
        return kv.Key;
      }
    }

    //Should never hit unless the probabilities are invalid
    return table.Last().Key;
  }
}

//Has Min & Max rules for the entries
public class DiceProbabilityTable<T>{
  public DiceProbEntry<T>[] table;

  public T Get(int roll){
    if(table == null){
      Debug.LogWarning("Missing probability table");
      return default(T);
    }
    foreach(var entry in table){
      if(
        (!entry.min.HasValue || (entry.min.Value <= roll)) &&
        (!entry.max.HasValue || (entry.max.Value >= roll))
      ){
        return entry.value;
      }
    }

    return default(T);
  }
}

public struct DiceProbEntry<T> {
  public int? min;
  public int? max;
  public T value;
}