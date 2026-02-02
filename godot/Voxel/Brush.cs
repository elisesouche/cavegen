using Godot;

namespace CaveGen.Voxel;

public abstract partial class Brush : Node3D
{
    public abstract float GetValueAtLocal(Vector3 offset);

    public float GetValueAtWorld(Vector3 world)
    {
        return GetValueAtLocal(ToLocal(world));
    }

    // Assumed to be symmetric
    public abstract Vector3 Bounds { get; }
}
