﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LambdaLifter.Utility
{
    public static class ForEachExtensions
    {
        public static void ForEachWithIndex<T>(this IEnumerable<T> enumerable, Action<T, int> handler)
        {
            int idx = 0;
            foreach (T item in enumerable)
                handler(item, idx++);
        }
    }
}
