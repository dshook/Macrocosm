using System.Collections.Generic;
using System;

public static class TupleListExtensions
{
    public static void Add<T1, T2>(this IList<Tuple<T1, T2>> list,
            T1 item1, T2 item2)
    {
        list.Add(Tuple.Create(item1, item2));
    }

    public static void Add<T1, T2, T3>(this IList<Tuple<T1, T2, T3>> list,
            T1 item1, T2 item2, T3 item3)
    {
        list.Add(Tuple.Create(item1, item2, item3));
    }

    // and so on...
}