using Godot;

namespace CaveGen.Voxel;

[GlobalClass, Tool]
public partial class SphereBrush : Brush
{
    [Export]
    float radius;

    public override Vector3 Bounds => new(radius, radius, radius);

    public override float GetValueAtLocal(Vector3 offset) =>
        (offset.LengthSquared() <= radius * radius) ? 1.0f : 0.0f;
}
