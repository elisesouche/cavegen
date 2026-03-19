using Godot;

namespace CaveGen;

[GlobalClass, Tool]
public partial class CaveManager : Node
{
    [Export]
    Layout.LayoutGenerator layout = null!;

    [Export]
    Voxel.VoxelArea area = null!;

    [Export]
    Mesh.MarchingCubes_GPU cubes = null!;

    [Export]
    Voxel.Brush brush = null!;

    [ExportToolButton("Generate cave")]
    Callable __make => Callable.From(GenerateCave);

    void GenerateCave()
    {
        var markers = layout.Run();
        area.Reset();

        foreach (var marker in markers)
        {
            brush.Transform = marker.position;
            area.ApplyBrush(brush);
        }

        cubes.Init();
        cubes.PutMesh();
        Print.TimestampedMillis("Cavegen done.");
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        GenerateCave();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) { }
}
