using System.Collections.Generic;

public static class ListExtensions
{
  public static bool IsValidIndex<T>(this List<T> list, int index)
  {
    return index >= 0 && list != null && index < list.Count;
  }
}