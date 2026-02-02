using Godot;

namespace CaveGen.Voxel;

readonly record struct VoxelCoord(int X, int Y, int Z);

record VoxelState(float value = 0);

[Tool]
public partial class VoxelArea : Node3D
{
    VoxelState[,,] Voxels = new VoxelState[,,] { };

    [Export]
    float SizeX,
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

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() { }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) { }
}
