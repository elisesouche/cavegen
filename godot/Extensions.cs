using System;
using System.Collections.Generic;
using Godot;

static class Extensions
{
    public static T InList<T>(this Random r, List<T> l)
    {
        var n = l.Count;
        var i = r.Next(n);
        return l[i];
    }

    public static Vector3 XYZ(this Vector4 vec) => new(vec.X, vec.Y, vec.Z);
}

public static class RandomInstance
{
    public static Random instance = new();
}
