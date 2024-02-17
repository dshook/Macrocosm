//https://github.com/martindevans/MarvellousMarkovModel
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public class MarkovModel
{
  private readonly int _order;
  private readonly KeyValuePair<string, float>[] _startingStrings;
  private readonly Dictionary<string, KeyValuePair<string, float>[]> _productions;

  public MarkovModel(int order, KeyValuePair<string, float>[] startingStrings, Dictionary<string, KeyValuePair<string, float>[]> productions)
  {
    _order = order;
    _startingStrings = startingStrings;
    _productions = productions;
  }

  public string Generate()
  {
    string builder = string.Empty;

    string lastSelected = WeightedRandom(_startingStrings);

    do
    {
      //Extend string
      builder += lastSelected;
      if (builder.Length < _order)
        break;

      //Key to use to find next production
      var key = builder.Substring(builder.Length - _order);

      //Find production rules for this key
      KeyValuePair<string, float>[] prod;
      if (!_productions.TryGetValue(key, out prod))
        break;

      //Produce next expansion
      lastSelected = WeightedRandom(prod);

    } while (lastSelected != string.Empty);

    return builder;
  }

  public static string WeightedRandom(KeyValuePair<string, float>[] items)
  {
    var num = UnityEngine.Random.Range(0, 1f);

    for (int i = 0; i < items.Length; i++)
    {
      num -= items[i].Value;
      if (num <= 0)
        return items[i].Key;
    }

    throw new InvalidOperationException();
  }
}