using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CaveGen.Voxel;
using Godot;

namespace CaveGen.Mesh;

[StructLayout(LayoutKind.Sequential)]
record struct Triangle(Vector4 a, Vector4 b, Vector4 c, Vector4 norm);

[GlobalClass, Tool]
public partial class MarchingCubes_GPU : Node
{
    [Export]
    VoxelArea Area { get; set; } = null!;

    [Export(PropertyHint.File)]
    string LUTPath { get; set; } = "";

    byte[] LoadLUT()
    {
        var file = FileAccess.Open(this.LUTPath, FileAccess.ModeFlags.Read);
        var text = file.GetAsText();
        file.Close();
        var index_strings = text.Split(',');
        List<byte> indices = [];
        foreach (var s in index_strings)
        {
            indices.AppendBytesFor(System.Convert.ToInt32(s));
        }
        return indices.ToArray();
    }

    const int WORKGROUP_SIZE_X = 10;
    const int WORKGROUP_SIZE_Y = 10;
    const int WORKGROUP_SIZE_Z = 10;

    RenderingDevice rd = null!;

    Rid shader;

    Rid uniformSet;
    Rid LUTBuffer;
    Rid AreaDataBuffer;
    Rid VoxelValueBuffer;
    Rid CounterBuffer;
    Rid MeshBuffer;

    Rid pipeline;

    void InitShader()
    {
        rd = RenderingServer.CreateLocalRenderingDevice();
        var shaderFile = GD.Load<RDShaderFile>("res://Mesh/MarchingCubes.glsl");
        var shaderBC = shaderFile.GetSpirV();
        shader = rd.ShaderCreateFromSpirV(shaderBC);
        pipeline = rd.ComputePipelineCreate(shader);
    }

    const int LUTDataBinding = 0;
    const int AreaDataBinding = 1;
    const int VoxelValueDataBinding = 2;
    const int CounterBinding = 3;
    const int MeshDataBinding = 4;
    private const int MAXIMUM_NUMBER_OF_TRIANGLES_PER_CUBE = 5;
    const int TRIANGLE_SIZE = 4 * 4 * sizeof(float);

    RDUniform BindBuffer(Rid buffer, int binding)
    {
        var uniform = new RDUniform
        {
            UniformType = RenderingDevice.UniformType.StorageBuffer,
            Binding = binding,
        };
        uniform.AddId(buffer);
        return uniform;
    }

    void BindUniforms()
    {
        var lut = LoadLUT();
        LUTBuffer = rd.StorageBufferCreate((uint)lut.Length, lut);

        List<byte> area = new();
        area.AppendBytesFor(Area.SizeX);
        area.AppendBytesFor(Area.SizeY);
        area.AppendBytesFor(Area.SizeZ);
        area.AppendBytesFor(Area.VoxelWidth);
        area.AppendBytesFor(Area.Position.X);
        area.AppendBytesFor(Area.Position.Y);
        area.AppendBytesFor(Area.Position.Z);
        AreaDataBuffer = rd.StorageBufferCreate((uint)area.Count, area.ToArray());

        List<byte> counter = new();
        counter.AppendBytesFor((uint)0);
        CounterBuffer = rd.StorageBufferCreate((uint)counter.Count, counter.ToArray());

        byte[] mesh = new byte[
            MAXIMUM_NUMBER_OF_TRIANGLES_PER_CUBE
                * TRIANGLE_SIZE
                * Area.SizeX
                * Area.SizeY
                * Area.SizeZ
        ];
        MeshBuffer = rd.StorageBufferCreate((uint)mesh.Length, mesh);

        byte[] voxels = MarshalUtils.VoxelStatesToBytes(Area.Voxels);
        VoxelValueBuffer = rd.StorageBufferCreate((uint)voxels.Length, voxels);

        uniformSet = rd.UniformSetCreate(
            [
                BindBuffer(LUTBuffer, LUTDataBinding),
                BindBuffer(AreaDataBuffer, AreaDataBinding),
                BindBuffer(VoxelValueBuffer, VoxelValueDataBinding),
                BindBuffer(CounterBuffer, CounterBinding),
                BindBuffer(MeshBuffer, MeshDataBinding),
            ],
            shader,
            0
        );
    }

    static uint CeilDiv(uint a, uint b) => (a + b - 1) / b;

    public void Init()
    {
        InitShader();
        BindUniforms();
        var computeList = rd.ComputeListBegin();
        rd.ComputeListBindComputePipeline(computeList, pipeline);
        rd.ComputeListBindUniformSet(computeList, uniformSet, 0);
        rd.ComputeListDispatch(
            computeList,
            CeilDiv((uint)Area.SizeX, WORKGROUP_SIZE_X),
            CeilDiv((uint)Area.SizeY, WORKGROUP_SIZE_Y),
            CeilDiv((uint)Area.SizeZ, WORKGROUP_SIZE_Z)
        );
        rd.ComputeListEnd();
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() { }

    void StartMeshGeneration()
    {
        GD.Print("Calling compute shader");
        rd.Submit();
    }

    ArrayMesh ProcessMesh()
    {
        StartMeshGeneration();
        rd.Sync();

        var counterBytes = rd.BufferGetData(CounterBuffer);
        uint triCount = BitConverter.ToUInt32(counterBytes);

        var bytes = rd.BufferGetData(MeshBuffer);
        var allTriangles = MemoryMarshal.Cast<byte, Triangle>(bytes);
        var triangles = allTriangles.Slice(0, (int)triCount);

        GD.Print($"Mesh built by compute shader. {triangles.Length} triangles.");
        SurfaceTool st = new();
        st.Begin(Godot.Mesh.PrimitiveType.Triangles);
        foreach (var tri in triangles)
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
        return st.Commit();
    }

    public void PutMesh()
    {
        var node = new MeshInstance3D();
        node.Mesh = this.ProcessMesh();
        GD.Print(node.Mesh.SurfaceGetArrays(0));
        foreach (var c in this.GetChildren())
            c.QueueFree();
        this.AddChild(node);
        GD.Print("Marching cubes all done.");
    }

    [ExportToolButton("Init")]
    Callable _init => Callable.From(Init);

    [ExportToolButton("Put mesh")]
    Callable _mesh => Callable.From(PutMesh);

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) { }
}
