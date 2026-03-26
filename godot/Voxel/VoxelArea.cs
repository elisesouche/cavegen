using System;
using Godot;

namespace CaveGen.Voxel;

public readonly record struct VoxelCoord(int X, int Y, int Z);

public record struct VoxelState(float value = 0);

[Tool, GlobalClass]
public partial class VoxelArea : Node3D
{
    VoxelState[,,] voxels = new VoxelState[,,] { };
    public VoxelState[,,] Voxels
    {
        get
        {
            if (voxels is null)
                voxels = new VoxelState[SizeX, SizeY, SizeZ];
            return voxels;
        }
    }

    [Export]
    public int SizeX,
        SizeY,
        SizeZ;

    [Export]
    public float VoxelWidth;

    float WorldSizeX => SizeX * VoxelWidth;
    float WorldSizeY => SizeY * VoxelWidth;
    float WorldSizeZ => SizeZ * VoxelWidth;
    Vector3 CenterOffset => new Vector3(WorldSizeX / 2, WorldSizeY / 2, WorldSizeZ / 2);

    public Vector3 Voxel2World(VoxelCoord coord)
    {
        var offsetx = (coord.X + .5f) * this.VoxelWidth;
        var offsety = (coord.Y + .5f) * this.VoxelWidth;
        var offsetz = (coord.Z + .5f) * this.VoxelWidth;
        var offset = new Vector3(offsetx, offsety, offsetz) - CenterOffset;
        return this.Position + offset;
    }

    public VoxelCoord World2Voxel(Vector3 coord)
    {
        var offset = coord - this.Position + CenterOffset;
        var x = (int)(offset.X / VoxelWidth - .5f);
        var y = (int)(offset.Y / VoxelWidth - .5f);
        var z = (int)(offset.Z / VoxelWidth - .5f);
        return new VoxelCoord(x, y, z);
    }

    // In the last minutes of this project, trying desperately to optimize, I
    // asked an LLM to optimize this function. It is a rewrite of the previous
    // implementation.
    public void ApplyBrush(Brush brush)
    {
        // compute center in VoxelArea local space as before
        var center = ToLocal(brush.GlobalPosition);
        var center_vox = World2Voxel(center);

        // compute bounding extents as integer radii (in voxels)
        var bounds_vox = World2Voxel(brush.Bounds);

        // pre-clamp bounding box to voxel array indices
        int minX = Math.Max(0, center_vox.X - bounds_vox.X);
        int maxX = Math.Min(SizeX - 1, center_vox.X + bounds_vox.X);
        int minY = Math.Max(0, center_vox.Y - bounds_vox.Y);
        int maxY = Math.Min(SizeY - 1, center_vox.Y + bounds_vox.Y);
        int minZ = Math.Max(0, center_vox.Z - bounds_vox.Z);
        int maxZ = Math.Min(SizeZ - 1, center_vox.Z + bounds_vox.Z);

        // cache frequently used values
        float vw = VoxelWidth;
        var centerOffset = CenterOffset;
        var areaGlobal = this.GlobalTransform; // copy once
        // Precompute transform from Area-local -> Brush-local to avoid ToGlobal + ToLocal per voxel
        var areaToBrush = brush.GlobalTransform.AffineInverse() * this.GlobalTransform;

        // Precompute base local position for the voxel at (minX,minY,minZ)
        var baseLocal =
            new Vector3((minX + 0.5f) * vw, (minY + 0.5f) * vw, (minZ + 0.5f) * vw) - centerOffset;
        // baseLocal is in Area-local coordinates (matching Voxel2World's local output)
        // We'll increment by vw across loops

        // local reference to internal voxels array to avoid property overhead
        var localVoxels = voxels; // field
        // iterate using ints, update voxels directly, and perform a single Xform per voxel
        for (int xi = minX; xi <= maxX; xi++)
        {
            float offsetX = (xi - minX) * vw;
            for (int yi = minY; yi <= maxY; yi++)
            {
                float offsetY = (yi - minY) * vw;
                for (int zi = minZ; zi <= maxZ; zi++)
                {
                    float offsetZ = (zi - minZ) * vw;
                    // build area-local position of this voxel (as Voxel2World would)
                    var localPos = new Vector3(
                        baseLocal.X + offsetX,
                        baseLocal.Y + offsetY,
                        baseLocal.Z + offsetZ
                    );
                    // transform directly into brush-local coordinates (one transform)
                    var brushLocal = areaToBrush * localPos;
                    // evaluate brush at brush-local position (call GetValueAtLocal directly)
                    var value = brush.GetValueAtLocal(brushLocal);
                    // update voxels directly (no delegate allocations, no helper calls)
                    localVoxels[xi, yi, zi].value -= value;
                }
            }
        }
    }

    bool IsInBounds(VoxelCoord voxel) =>
        voxel.X >= 0
        && voxel.X < SizeX
        && voxel.Y >= 0
        && voxel.Y < SizeY
        && voxel.Z >= 0
        && voxel.Z < SizeZ;

    public float GetVoxelValue(VoxelCoord voxel) => this.Voxels[voxel.X, voxel.Y, voxel.Z].value;

    public void SetVoxelValue(VoxelCoord voxel, float value) =>
        this.Voxels[voxel.X, voxel.Y, voxel.Z].value = value;

    public void ModifyVoxelValue(VoxelCoord voxel, Func<float, float> func) =>
        SetVoxelValue(voxel, func(GetVoxelValue(voxel)));

    [Export]
    PackedScene? VoxelMarker { get; set; }

    [ExportToolButton("Reset field")]
    Callable __reset => Callable.From(Reset);

    [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(voxels))]
    public void Reset()
    {
        voxels = new VoxelState[SizeX, SizeY, SizeZ];
    }

    [ExportToolButton("Redraw voxel field")]
    Callable __redraw => Callable.From(RedrawField);

    [Export]
    Brush? Brush { get; set; }

    [ExportToolButton("Apply brush")]
    Callable __apply => Callable.From(() => ApplyBrush(Brush!));

    public void IterVoxels(Action<VoxelCoord> action)
    {
        for (int x = 0; x < SizeX; x++)
        {
            for (int y = 0; y < SizeY; y++)
            {
                for (int z = 0; z < SizeZ; z++)
                {
                    action(new(x, y, z));
                }
            }
        }
    }

    void RedrawField()
    {
        foreach (var c in GetChildren())
        {
            c.QueueFree();
        }
        IterVoxels(voxel =>
        {
            if (GetVoxelValue(voxel) >= 0)
            {
                var node = VoxelMarker!.Instantiate<Node3D>();
                this.AddChild(node);
                node.Position = Voxel2World(voxel);
            }
        });
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() { }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) { }
}
