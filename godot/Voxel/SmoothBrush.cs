using System;
using Godot;

namespace CaveGen.Voxel;

[GlobalClass, Tool]
public partial class SmoothBrush : Brush
{
    [Export]
    Curve Density { get; set; } = null!;

    public override Vector3 Bounds => new(Density.MaxDomain, Density.MaxDomain, Density.MaxDomain);

    public override float GetValueAtLocal(Vector3 offset) =>
        Density.SampleBaked(offset.DistanceTo(Position));
}
