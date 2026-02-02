using System;
using Godot;

namespace CaveGen.Voxel;

readonly record struct VoxelCoord(int X, int Y, int Z);

record struct VoxelState(float value = 0);

[Tool]
[GlobalClass]
public partial class VoxelArea : Node3D
{
    VoxelState[,,] voxels = new VoxelState[,,] { };
    VoxelState[,,] Voxels
    {
        get
        {
            if (voxels is null)
                voxels = new VoxelState[SizeX, SizeY, SizeZ];
            return voxels;
        }
    }

    [Export]
    int SizeX,
        SizeY,
        SizeZ;

    [Export]
    float VoxelWidth;

    float WorldSizeX => SizeX * VoxelWidth;
    float WorldSizeY => SizeY * VoxelWidth;
    float WorldSizeZ => SizeZ * VoxelWidth;
    Vector3 CenterOffset => new Vector3(WorldSizeX / 2, WorldSizeY / 2, WorldSizeZ / 2);

    Vector3 Voxel2World(VoxelCoord coord)
    {
        var offsetx = (coord.X + .5f) * this.VoxelWidth;
        var offsety = (coord.Y + .5f) * this.VoxelWidth;
        var offsetz = (coord.Z + .5f) * this.VoxelWidth;
        var offset = new Vector3(offsetx, offsety, offsetz) - CenterOffset;
        return this.Position + offset;
    }

    VoxelCoord World2Voxel(Vector3 coord)
    {
        var offset = coord - this.Position + CenterOffset;
        var x = (int)(offset.X / VoxelWidth - .5f);
        var y = (int)(offset.Y / VoxelWidth - .5f);
        var z = (int)(offset.Z / VoxelWidth - .5f);
        return new VoxelCoord(x, y, z);
    }

    void ApplyBrush(Brush brush)
    {
        // Find the bounding box
        var center = ToLocal(brush.GlobalPosition);
        var center_vox = World2Voxel(center);
        var bounds = World2Voxel(brush.Bounds);
        for (int x = -bounds.X; x <= bounds.X; x++)
        {
            for (int y = -bounds.Y; y <= bounds.Y; y++)
            {
                for (int z = -bounds.Z; z <= bounds.Z; z++)
                {
                    var voxel = new VoxelCoord(
                        center_vox.X + x,
                        center_vox.Y + y,
                        center_vox.Z + z
                    );
                    if (IsInBounds(voxel))
                    {
                        var voxel_loc = ToGlobal(Voxel2World(voxel));
                        var value = brush.GetValueAtWorld(voxel_loc);
                        ModifyVoxelValue(voxel, v => v - value);
                    }
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

    float GetVoxelValue(VoxelCoord voxel) => this.Voxels[voxel.X, voxel.Y, voxel.Z].value;

    void SetVoxelValue(VoxelCoord voxel, float value) =>
        this.Voxels[voxel.X, voxel.Y, voxel.Z].value = value;

    void ModifyVoxelValue(VoxelCoord voxel, Func<float, float> func) =>
        SetVoxelValue(voxel, func(GetVoxelValue(voxel)));

    [Export]
    PackedScene? VoxelMarker { get; set; }

    [ExportToolButton("Reset field")]
    Callable __reset => Callable.From(Reset);

    [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(voxels))]
    void Reset()
    {
        voxels = new VoxelState[SizeX, SizeY, SizeZ];
    }

    [ExportToolButton("Redraw voxel field")]
    Callable __redraw => Callable.From(RedrawField);

    [Export]
    Brush? Brush { get; set; }

    [ExportToolButton("Apply brush")]
    Callable __apply => Callable.From(() => ApplyBrush(Brush!));

    void IterVoxels(Action<VoxelCoord> action)
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
