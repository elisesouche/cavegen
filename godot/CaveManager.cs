using Godot;

namespace CaveGen;

[Tool]
public partial class CaveManager : Node
{
    [Export]
    Layout.LayoutGenerator? layout;

    [Export]
    Voxel.VoxelArea? area;

    [Export]
    Mesh.MarchingCubes? cubes;

    [Export]
    Voxel.Brush? brush;

    [ExportToolButton("Generate cave")]
    Callable __make => Callable.From(GenerateCave);

    [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(layout))]
    [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(area))]
    [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(cubes))]
    [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(brush))]
    void EnsureNonNull()
    {
        if (layout is null || area is null | cubes is null || brush is null)
            throw new System.NullReferenceException();
    }

    void GenerateCave()
    {
        EnsureNonNull();

        var markers = layout.Run();
        area.Reset();

        foreach (var marker in markers)
        {
            brush.Transform = marker.position;
            area.ApplyBrush(brush);
        }

        cubes.PutMesh();
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() { }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) { }
}
