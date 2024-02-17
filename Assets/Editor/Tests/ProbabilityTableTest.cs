using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
  public class ProbabilityTableTest
  {
    const int runCount = 100;
    const string A = "A";
    const string B = "B";
    const string C = "C";
    const string D = "D";
    const string E = "E";
    const string F = "F";

    [Test]
    public void ProbabilityTableSimplePasses()
    {
      var table = new ProbabilityTable<string>();
      table.Add(0.5f, A);
      table.Add(0.5f, B);

      var results = new Dictionary<string, int>(){{A, 0}, {B, 0}};
      for(var i = 0; i < runCount; i++){
        results[table.GetNext()]++;
      }

      // Debug.Log(string.Join(";", results.Select(x => x.Key + "=" + x.Value).ToArray()) );

      Assert.GreaterOrEqual(results[A], 40, "A was " + results[A]);
      Assert.GreaterOrEqual(results[B], 40, "B was " + results[B]);
    }

    [Test]
    public void ProbabilityTableThreePasses()
    {
      var table = new ProbabilityTable<string>();
      table.Add(0.33f, A);
      table.Add(0.33f, B);
      table.Add(0.33f, C);

      var results = new Dictionary<string, int>(){{A, 0}, {B, 0}, {C, 0}};
      for(var i = 0; i < runCount; i++){
        results[table.GetNext()]++;
      }

      // Debug.Log(string.Join(";", results.Select(x => x.Key + "=" + x.Value).ToArray()) );

      Assert.GreaterOrEqual(results[A], 20, "A was " + results[A]);
      Assert.GreaterOrEqual(results[B], 20, "B was " + results[B]);
      Assert.GreaterOrEqual(results[C], 20, "C was " + results[C]);
    }

    [Test]
    public void ProbabilityTableFourPasses()
    {
      var table = new ProbabilityTable<string>(){
        table = new Dictionary<string, float>(){
          {A, 0.1f},
          {B, 0.01f},
          {C, 0.15f},
          {D, 0.74f},
        }
      };

      var results = new Dictionary<string, int>(){{A, 0}, {B, 0}, {C, 0}, {D, 0}};
      for(var i = 0; i < runCount; i++){
        results[table.GetNext()]++;
      }

      // Debug.Log(string.Join(";", results.Select(x => x.Key + "=" + x.Value).ToArray()) );

      Assert.GreaterOrEqual(results[A], 5, "A was " + results[A]);
      Assert.LessOrEqual(results[B], 3, "B was " + results[B]);
      Assert.GreaterOrEqual(results[C], 10, "C was " + results[C]);
      Assert.GreaterOrEqual(results[D], 60, "D was " + results[D]);
    }

    [Test]
    public void DiceProbabilityTablePasses()
    {
      var table = new DiceProbabilityTable<string>(){
        table = new DiceProbEntry<string>[]{
          new DiceProbEntry<string>() { max = 2, value = A },
          new DiceProbEntry<string>() { min = 3, max = 4,  value = B },
          new DiceProbEntry<string>() { min = 5, max = 6,  value = C },
          new DiceProbEntry<string>() { min = 7, max = 8,  value = D },
          new DiceProbEntry<string>() { min = 9, max = 10, value = E },
          new DiceProbEntry<string>() { min = 11,          value = F },
        }
      };

      var results = new Dictionary<string, int>(){{A, 0}, {B, 0}, {C, 0}, {D, 0}, {E, 0}, {F, 0}};
      var minRoll = 100;
      var maxRoll = -1;
      for(var i = 0; i < runCount; i++){
        var roll = RandomExtensions.RollDice(2, 6);
        minRoll = minRoll < roll ? minRoll : roll;
        maxRoll = maxRoll > roll ? maxRoll : roll;
        results[table.Get(roll)]++;
      }

      // Debug.Log(string.Join(";", results.Select(x => x.Key + "=" + x.Value).ToArray()) );

      //Really need to up the roll count to make these less shaky
      Assert.AreEqual(2, minRoll, "Min roll is 2");
      Assert.AreEqual(12, maxRoll, "Max roll is 12");
      var sum = results.Sum(k => k.Value);
      Assert.AreEqual(sum, runCount, "All rolls went into a bucket");
      Assert.GreaterOrEqual(results[A], 1, "A was " + results[A]);
      Assert.GreaterOrEqual(results[B], 10, "B was " + results[B]);
      Assert.GreaterOrEqual(results[C], 10, "C was " + results[C]);
      Assert.GreaterOrEqual(results[D], 10, "D was " + results[D]);
      Assert.GreaterOrEqual(results[E], 10, "E was " + results[E]);
      Assert.GreaterOrEqual(results[F], 1, "F was " + results[F]);
    }
  }
}
