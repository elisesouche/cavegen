using System;
using Godot;

namespace CaveGen.Voxel;

public partial class SphereBrush : Brush
{
    [Export]
    float radius;

    public override Vector3 Bounds => new(radius, radius, radius);

    public override float GetValueAtLocal(Vector3 offset) =>
        (offset.LengthSquared() <= radius * radius) ? 0.0f : 1.0f;
}
