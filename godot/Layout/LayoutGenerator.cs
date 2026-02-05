using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace CaveGen.Layout;

[Tool]
public partial class LayoutGenerator : Node
{
    private LSystem? system;

    [ExportGroup("L-System")]
    [Export]
    public required Godot.Collections.Array<string> macro_defs;

    [Export]
    public required Godot.Collections.Array<string> rules;

    [ExportToolButton("Rebuild L-System")]
    public Callable RebuildButton => Callable.From(RebuildLSystem);

    [Export]
    int numRuns = 10;

    [ExportGroup("Turtle")]
    [Export]
    Node3D? anchor;

    [Export]
    float step;

    [Export]
    float step_modifier;

    [Export]
    float angle;

    [Export]
    float angle_modifier;

    [Export]
    float joinProbability;

    [ExportToolButton("Generate")]
    public Callable GenerateButton => Callable.From(Run);

    [ExportGroup("Debug render")]
    [Export]
    PackedScene? normal_marker;

    [Export]
    PackedScene? tip_marker;

    [ExportToolButton("Delete Markers")]
    public Callable DeleteMarkerButton => Callable.From(DeleteMarkers);

    internal LSystem System
    {
        get
        {
            if (system is null)
            {
                RebuildLSystem();
            }
            return system;
        }
        set => system = value;
    }

    [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(system))]
    void RebuildLSystem()
    {
        var macroSystem = LSystemParser.Macros(macro_defs);
        var d = new Dictionary<NonTerminal, List<List<Symbol>>>();
        foreach (var prod in rules)
        {
            var (n, p) = LSystemParser.Production(prod);
            if (d.ContainsKey(n))
            {
                d[n].Add(p);
            }
            else
            {
                d.Add(n, [p]);
            }
        }
        system = new LSystem
        {
            Initial = new NonTerminal(NonTerminalSymbols.BranchTip),
            Productions = d,
            MacroSystem = macroSystem,
        };
    }

    void Run()
    {
        DeleteMarkers();
        var res = System.Run(numRuns);
        RunTurtle(res);
    }

    void DeleteMarkers()
    {
        foreach (var child in anchor?.GetChildren() ?? [])
        {
            child.QueueFree();
        }
    }

    void RunTurtle(List<Symbol> program)
    {
        var turtle = new Turtle(anchor!, step, step_modifier, angle, angle_modifier);
        List<StructureMarker> markers = [];
        void RunOn(List<Symbol> program)
        {
            foreach (var sym in program)
            {
                var point = Turtle.PointDrop.None;
                switch (sym)
                {
                    case NonTerminal n:
                        point = turtle.StepNonTerminal(n);
                        break;
                    case Terminal t:
                        point = turtle.StepTerminal(t);
                        break;
                    case MacroPlaceholder p:
                        RunOn(System.MacroSystem.Expand(p.Name));
                        break;
                    default:
                        throw new ArgumentException(
                            $"Unhandled Symbol subtype: {sym?.GetType().Name}",
                            nameof(sym)
                        );
                }
                DropPoint(turtle.OldTransform, point, markers);
            }
        }
        RunOn(program);

        var tips = markers.Where(m => m.isTip).ToHashSet();
        while (tips.Count > 0)
        {
            var mark = tips.First();
            tips.Remove(mark);
            // select an other tip
            var other = tips.MinBy(other =>
                mark.position.Origin.DistanceSquaredTo(other.position.Origin)
            );
            tips.Remove(other);
            var dst = mark.position.Origin.DistanceTo(other.position.Origin);
            var step = 1.0f;
            for (var f = 0.0f; f < 1.0f; f += step / dst)
            {
                var trans = mark.position.InterpolateWith(other.position, f);
                DropPoint(trans, Turtle.PointDrop.Normal, markers);
            }
        }
        foreach (var mark in tips)
        {
            if (RandomInstance.instance.NextDouble() < joinProbability)
            {
                // select an other tip
                var other = tips.MinBy(other =>
                    (other.position == mark.position)
                        ? float.PositiveInfinity
                        : mark.position.Origin.DistanceSquaredTo(other.position.Origin)
                );
                var dst = mark.position.Origin.DistanceTo(other.position.Origin);
                var step = 1.0f;
                for (var f = 0.0f; f < 1.0f; f += step / dst)
                {
                    var trans = mark.position.InterpolateWith(other.position, f);
                    DropPoint(trans, Turtle.PointDrop.Normal, markers);
                }
            }
        }
    }

    private void DropPoint(Transform3D trans, Turtle.PointDrop point, List<StructureMarker> markers)
    {
        switch (point)
        {
            case Turtle.PointDrop.Normal:
                DropPointScene(trans, normal_marker!);
                markers.Add(new StructureMarker(trans, false));
                break;
            case Turtle.PointDrop.Tip:
                DropPointScene(trans, tip_marker!);
                markers.Add(new StructureMarker(trans, true));
                break;
        }
    }

    void DropPointScene(Transform3D transform, PackedScene marker)
    {
        var node = marker.Instantiate<Node3D>();
        anchor!.AddChild(node);
        node.Transform = transform;
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        RebuildLSystem();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) { }
}

public record struct StructureMarker(Transform3D position, bool isTip);

class Turtle
{
    Stack<Transform3D> stack;
    Node3D anchor;
    float step;
    float step_modifier;
    float angle;
    float angle_modifier;

    Transform3D currentTransform;
    public Transform3D OldTransform { get; set; }

    public Transform3D CurrentTrans
    {
        get => currentTransform;
        set
        {
            OldTransform = currentTransform;
            currentTransform = value;
        }
    }

    public enum PointDrop
    {
        None,
        Normal,
        Tip,
    }

    public Turtle(Node3D anchor, float step, float step_modifier, float angle, float angle_modifier)
    {
        this.stack = [];
        this.anchor = anchor;
        this.step = step;
        this.step_modifier = step_modifier;
        this.angle = angle;
        this.angle_modifier = angle_modifier;

        this.CurrentTrans = anchor.Transform;
    }

    public PointDrop StepNonTerminal(NonTerminal n)
    {
        switch (n.Self)
        {
            case NonTerminalSymbols.Forward:
                CurrentTrans = CurrentTrans.Translated(CurrentTrans.Basis.Z.Normalized() * step);
                return PointDrop.Normal;
            case NonTerminalSymbols.YawClockwise:
                CurrentTrans = CurrentTrans.RotatedLocal(CurrentTrans.Basis.Y.Normalized(), -angle);
                return PointDrop.None;
            case NonTerminalSymbols.YawCounterClockwise:
                CurrentTrans = CurrentTrans.RotatedLocal(CurrentTrans.Basis.Y.Normalized(), +angle);
                return PointDrop.None;
            case NonTerminalSymbols.PitchUp:
                CurrentTrans = CurrentTrans.RotatedLocal(CurrentTrans.Basis.X.Normalized(), -angle);
                return PointDrop.None;
            case NonTerminalSymbols.PitchDown:
                CurrentTrans = CurrentTrans.RotatedLocal(CurrentTrans.Basis.X.Normalized(), +angle);
                return PointDrop.None;
            case NonTerminalSymbols.IncreaseAngle:
                angle += angle_modifier;
                return PointDrop.None;
            case NonTerminalSymbols.DecreaseAngle:
                angle -= angle_modifier;
                return PointDrop.None;
            case NonTerminalSymbols.IncreaseStep:
                step += step_modifier;
                return PointDrop.None;
            case NonTerminalSymbols.DecreaseStep:
                step -= step_modifier;
                return PointDrop.None;
            case NonTerminalSymbols.BranchEnd:
                return PointDrop.None;
            case NonTerminalSymbols.BranchTip:
                return PointDrop.None;
            default:
                // unreachable but C# is a shitty language
                return PointDrop.None;
        }
    }

    public PointDrop StepTerminal(Terminal t)
    {
        switch (t.Self)
        {
            case TerminalSymbols.StartBranch:
                stack.Push(CurrentTrans);
                return PointDrop.None;
            case TerminalSymbols.EndBranch:
                CurrentTrans = stack.Pop();
                return PointDrop.Tip;
            default:
                // unreachable but C# is a shitty language
                return PointDrop.None;
        }
    }
}
