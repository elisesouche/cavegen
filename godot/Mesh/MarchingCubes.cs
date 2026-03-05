using System.Collections.Generic;
using CaveGen.Voxel;
using Godot;

namespace CaveGen.Mesh;

// This code is heavily based on Sebastian Lague's.
// https://github.com/SebLague/Godot-Marching-Cubes
[GlobalClass, Tool]
public partial class MarchingCubes : Node
{
    [Export]
    VoxelArea? Area { get; set; }

    static readonly int[] offsets =
    {
        0,
        0,
        3,
        6,
        12,
        15,
        21,
        27,
        36,
        39,
        45,
        51,
        60,
        66,
        75,
        84,
        90,
        93,
        99,
        105,
        114,
        120,
        129,
        138,
        150,
        156,
        165,
        174,
        186,
        195,
        207,
        219,
        228,
        231,
        237,
        243,
        252,
        258,
        267,
        276,
        288,
        294,
        303,
        312,
        324,
        333,
        345,
        357,
        366,
        372,
        381,
        390,
        396,
        405,
        417,
        429,
        438,
        447,
        459,
        471,
        480,
        492,
        507,
        522,
        528,
        531,
        537,
        543,
        552,
        558,
        567,
        576,
        588,
        594,
        603,
        612,
        624,
        633,
        645,
        657,
        666,
        672,
        681,
        690,
        702,
        711,
        723,
        735,
        750,
        759,
        771,
        783,
        798,
        810,
        825,
        840,
        852,
        858,
        867,
        876,
        888,
        897,
        909,
        915,
        924,
        933,
        945,
        957,
        972,
        984,
        999,
        1008,
        1014,
        1023,
        1035,
        1047,
        1056,
        1068,
        1083,
        1092,
        1098,
        1110,
        1125,
        1140,
        1152,
        1167,
        1173,
        1185,
        1188,
        1191,
        1197,
        1203,
        1212,
        1218,
        1227,
        1236,
        1248,
        1254,
        1263,
        1272,
        1284,
        1293,
        1305,
        1317,
        1326,
        1332,
        1341,
        1350,
        1362,
        1371,
        1383,
        1395,
        1410,
        1419,
        1425,
        1437,
        1446,
        1458,
        1467,
        1482,
        1488,
        1494,
        1503,
        1512,
        1524,
        1533,
        1545,
        1557,
        1572,
        1581,
        1593,
        1605,
        1620,
        1632,
        1647,
        1662,
        1674,
        1683,
        1695,
        1707,
        1716,
        1728,
        1743,
        1758,
        1770,
        1782,
        1791,
        1806,
        1812,
        1827,
        1839,
        1845,
        1848,
        1854,
        1863,
        1872,
        1884,
        1893,
        1905,
        1917,
        1932,
        1941,
        1953,
        1965,
        1980,
        1986,
        1995,
        2004,
        2010,
        2019,
        2031,
        2043,
        2058,
        2070,
        2085,
        2100,
        2106,
        2118,
        2127,
        2142,
        2154,
        2163,
        2169,
        2181,
        2184,
        2193,
        2205,
        2217,
        2232,
        2244,
        2259,
        2268,
        2280,
        2292,
        2307,
        2322,
        2328,
        2337,
        2349,
        2355,
        2358,
        2364,
        2373,
        2382,
        2388,
        2397,
        2409,
        2415,
        2418,
        2427,
        2433,
        2445,
        2448,
        2454,
        2457,
        2460,
    };
    static readonly int[] lengths =
    {
        0,
        3,
        3,
        6,
        3,
        6,
        6,
        9,
        3,
        6,
        6,
        9,
        6,
        9,
        9,
        6,
        3,
        6,
        6,
        9,
        6,
        9,
        9,
        12,
        6,
        9,
        9,
        12,
        9,
        12,
        12,
        9,
        3,
        6,
        6,
        9,
        6,
        9,
        9,
        12,
        6,
        9,
        9,
        12,
        9,
        12,
        12,
        9,
        6,
        9,
        9,
        6,
        9,
        12,
        12,
        9,
        9,
        12,
        12,
        9,
        12,
        15,
        15,
        6,
        3,
        6,
        6,
        9,
        6,
        9,
        9,
        12,
        6,
        9,
        9,
        12,
        9,
        12,
        12,
        9,
        6,
        9,
        9,
        12,
        9,
        12,
        12,
        15,
        9,
        12,
        12,
        15,
        12,
        15,
        15,
        12,
        6,
        9,
        9,
        12,
        9,
        12,
        6,
        9,
        9,
        12,
        12,
        15,
        12,
        15,
        9,
        6,
        9,
        12,
        12,
        9,
        12,
        15,
        9,
        6,
        12,
        15,
        15,
        12,
        15,
        6,
        12,
        3,
        3,
        6,
        6,
        9,
        6,
        9,
        9,
        12,
        6,
        9,
        9,
        12,
        9,
        12,
        12,
        9,
        6,
        9,
        9,
        12,
        9,
        12,
        12,
        15,
        9,
        6,
        12,
        9,
        12,
        9,
        15,
        6,
        6,
        9,
        9,
        12,
        9,
        12,
        12,
        15,
        9,
        12,
        12,
        15,
        12,
        15,
        15,
        12,
        9,
        12,
        12,
        9,
        12,
        15,
        15,
        12,
        12,
        9,
        15,
        6,
        15,
        12,
        6,
        3,
        6,
        9,
        9,
        12,
        9,
        12,
        12,
        15,
        9,
        12,
        12,
        15,
        6,
        9,
        9,
        6,
        9,
        12,
        12,
        15,
        12,
        15,
        15,
        6,
        12,
        9,
        15,
        12,
        9,
        6,
        12,
        3,
        9,
        12,
        12,
        15,
        12,
        15,
        9,
        12,
        12,
        15,
        15,
        6,
        9,
        12,
        6,
        3,
        6,
        9,
        9,
        6,
        9,
        12,
        6,
        3,
        9,
        6,
        12,
        3,
        6,
        3,
        3,
        0,
    };

    static readonly int[] cornerIndexAFromEdge = { 0, 1, 2, 3, 4, 5, 6, 7, 0, 1, 2, 3 };
    static readonly int[] cornerIndexBFromEdge = { 1, 2, 3, 0, 5, 6, 7, 4, 4, 5, 6, 7 };

    [Export(PropertyHint.File)]
    string LUTPath { get; set; } = "";

    record struct Triangle(Vector4 a, Vector4 b, Vector4 c, Vector4 norm);

    List<int> LoadLUT()
    {
        var file = FileAccess.Open(this.LUTPath, FileAccess.ModeFlags.Read);
        var text = file.GetAsText();
        file.Close();
        var index_strings = text.Split(',');
        List<int> indices = [];
        foreach (var s in index_strings)
            indices.Add(System.Convert.ToInt32(s));

        return indices;
    }

    Vector4 InterpolateVerts(Vector4 v1, Vector4 v2, float isoLevel)
    {
        //return (v1 + v2) * 0.5;
        float t = (isoLevel - v1.W) / (v2.W - v1.W);
        return v1 + t * (v2 - v1);
    }

    Vector4 Evaluate(int x, int y, int z)
    {
        System.Diagnostics.Debug.Assert(Area is not null);
        var coord = new VoxelCoord(x, y, z);
        var pos = Area.Voxel2World(coord);
        var value = Area.GetVoxelValue(coord);
        return new(pos.X, pos.Y, pos.Z, value);
    }

    void ProcessVoxel(List<Triangle> tris, List<int> lut, VoxelCoord coord)
    {
        System.Diagnostics.Debug.Assert(Area is not null);
        if (coord.X + 1 == Area.SizeX || coord.Y + 1 == Area.SizeY || coord.Z + 1 == Area.SizeZ)
            return;
        Vector4[] cubeCorners =
        {
            Evaluate(coord.X + 0, coord.Y + 0, coord.Z + 0),
            Evaluate(coord.X + 1, coord.Y + 0, coord.Z + 0),
            Evaluate(coord.X + 1, coord.Y + 0, coord.Z + 1),
            Evaluate(coord.X + 0, coord.Y + 0, coord.Z + 1),
            Evaluate(coord.X + 0, coord.Y + 1, coord.Z + 0),
            Evaluate(coord.X + 1, coord.Y + 1, coord.Z + 0),
            Evaluate(coord.X + 1, coord.Y + 1, coord.Z + 1),
            Evaluate(coord.X + 0, coord.Y + 1, coord.Z + 1),
        };
        uint cubeIndex = 0;
        float isoLevel = 0;
        if (cubeCorners[0].W < isoLevel)
            cubeIndex |= 1;
        if (cubeCorners[1].W < isoLevel)
            cubeIndex |= 2;
        if (cubeCorners[2].W < isoLevel)
            cubeIndex |= 4;
        if (cubeCorners[3].W < isoLevel)
            cubeIndex |= 8;
        if (cubeCorners[4].W < isoLevel)
            cubeIndex |= 16;
        if (cubeCorners[5].W < isoLevel)
            cubeIndex |= 32;
        if (cubeCorners[6].W < isoLevel)
            cubeIndex |= 64;
        if (cubeCorners[7].W < isoLevel)
            cubeIndex |= 128;
        //
        // Create triangles for current cube configuration
        int numIndices = lengths[cubeIndex];
        int offset = offsets[cubeIndex];

        for (int i = 0; i < numIndices; i += 3)
        {
            // Get indices of corner points A and B for each of the three edges
            // of the cube that need to be joined to form the triangle.
            var v0 = lut[offset + i];
            var v1 = lut[offset + 1 + i];
            var v2 = lut[offset + 2 + i];

            var a0 = cornerIndexAFromEdge[v0];
            var b0 = cornerIndexBFromEdge[v0];

            var a1 = cornerIndexAFromEdge[v1];
            var b1 = cornerIndexBFromEdge[v1];

            var a2 = cornerIndexAFromEdge[v2];
            var b2 = cornerIndexBFromEdge[v2];

            // Calculate vertex positions
            Triangle currTri = new();
            currTri.a = InterpolateVerts(cubeCorners[a0], cubeCorners[b0], isoLevel);
            currTri.b = InterpolateVerts(cubeCorners[a1], cubeCorners[b1], isoLevel);
            currTri.c = InterpolateVerts(cubeCorners[a2], cubeCorners[b2], isoLevel);

            var ab = currTri.b.XYZ() - currTri.a.XYZ();
            var ac = currTri.c.XYZ() - currTri.a.XYZ();
            var normal = ab.Cross(ac).Normalized();
            currTri.norm = new(normal.X, normal.Y, normal.Z, 0);

            tris.Add(currTri);
        }
    }

    ArrayMesh ProcessMesh()
    {
        List<Triangle> tris = [];
        System.Diagnostics.Debug.Assert(Area is not null);
        var lut = LoadLUT();
        Area.IterVoxels(vox => ProcessVoxel(tris, lut, vox));

        // ArrayMesh mesh = new();
        // mesh.AddSurfaceFromArrays(ArrayMesh.PrimitiveType.Triangles, tris);
        SurfaceTool st = new();
        st.Begin(Godot.Mesh.PrimitiveType.Triangles);
        foreach (var tri in tris)
        {
            var norm_ = tri.norm.XYZ();
            var norm = new Vector3(norm_.X, norm_.Z, norm_.Y);
            st.SetNormal(norm);
            st.AddVertex(tri.a.XYZ());
            st.SetNormal(norm);
            st.AddVertex(tri.b.XYZ());
            st.SetNormal(norm);
            st.AddVertex(tri.c.XYZ());
        }
        st.Index();
        // st.GenerateNormals();
        return st.Commit();
    }

    public void PutMesh()
    {
        var node = new MeshInstance3D();
        node.Mesh = this.ProcessMesh();
        foreach (var c in this.GetChildren())
            c.QueueFree();
        this.AddChild(node);
    }

    [ExportToolButton("Bake mesh")]
    Callable __mesh => Callable.From(PutMesh);

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() { }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) { }
}
