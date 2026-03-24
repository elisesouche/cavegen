using System;
using Godot;

namespace CaveGen.Voxel;

[GlobalClass, Tool]
public partial class NoisyBrush : Brush
{
    [Export]
    Curve Density { get; set; } = null!;

    [Export]
    Noise Noise { get; set; } = null!;

    [Export]
    float NoiseStrength { get; set; } = 1f;

    public override Vector3 Bounds => new(Density.MaxDomain, Density.MaxDomain, Density.MaxDomain);

    public override float GetValueAtLocal(Vector3 offset)
    {
        var v = Mathf.Clamp(
            Density.SampleBaked(offset.DistanceTo(Position))
                + NoiseStrength * Mathf.Remap(Noise.GetNoise3Dv(offset), -1, 1, 0, 1),
            0,
            1
        );
        return v;
    }
}
