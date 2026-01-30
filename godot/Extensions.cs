using System;
using System.Collections.Generic;

static class Extensions {
    public static T InList<T>(this Random r, List<T> l) {
        var n = l.Count;
        var i = r.Next(n);
        return l[i];
    }
}
