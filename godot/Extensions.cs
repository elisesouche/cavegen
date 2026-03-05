using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CaveGen.Voxel;
using Godot;

namespace CaveGen;

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

public static class MarshalUtils
{
    public static void AppendBytesFor<T>(this List<byte> list, in T value)
        where T : struct
    {
        var size = Marshal.SizeOf(value);
        Span<byte> bytes = stackalloc byte[size];
        MemoryMarshal.Write(bytes, in value);
        list.AddRange(bytes);
    }

    public static byte[] ToBytes<T>(this List<T> list)
        where T : struct
    {
        return MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(list)).ToArray();
    }

    public static byte[] VoxelStatesToBytes(VoxelState[,,] voxels)
    {
        if (voxels.Length == 0)
        {
            throw new Exception("ABORT");
        }
        int byteCount = voxels.Length * Unsafe.SizeOf<VoxelState>();
        byte[] result = new byte[byteCount];

        GCHandle handle = GCHandle.Alloc(voxels, GCHandleType.Pinned);
        try
        {
            Marshal.Copy(handle.AddrOfPinnedObject(), result, 0, byteCount);
        }
        finally
        {
            handle.Free();
        }

        return result;
    }
}
