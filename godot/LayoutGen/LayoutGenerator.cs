using System;
using System.Collections.Generic;
using Godot;

namespace LayoutGen;

[Tool]
public partial class LayoutGenerator : Node
{
    private LSystem system;

    [ExportGroup("L-System")]
    [Export]
    Godot.Collections.Array<string> macro_defs;

    [Export]
    Godot.Collections.Array<string> rules;

    [ExportToolButton("Rebuild L-System")]
    public Callable RebuildButton => Callable.From(RebuildLSystem);

    [Export]
    int numRuns = 10;

    [ExportGroup("Turtle")]
    [Export]
    Node3D anchor;

    [Export]
    float step;

    [Export]
    float step_modifier;

    [Export]
    float angle;

    [Export]
    float angle_modifier;

    [ExportToolButton("Generate")]
    public Callable GenerateButton => Callable.From(Run);

    [ExportGroup("Debug render")]
    [Export]
    PackedScene normal_marker;

    [Export]
    PackedScene tip_marker;

    [ExportToolButton("Delete Markers")]
    public Callable DeleteMarkerButton => Callable.From(DeleteMarkers);

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
        GD.Print(LSystemFormatter.LSystem(system));
    }

    void Run()
    {
        DeleteMarkers();
        var res = system.Run(numRuns);
        RunTurtle(res);
    }

    void DeleteMarkers()
    {
        foreach (var child in anchor.GetChildren())
        {
            child.QueueFree();
        }
    }

    void RunTurtle(List<Symbol> program)
    {
        var turtle = new Turtle(anchor, step, step_modifier, angle, angle_modifier);
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
                        RunOn(system.MacroSystem.Expand(p.Name));
                        break;
                    default:
                        throw new ArgumentException(
                            $"Unhandled Symbol subtype: {sym?.GetType().Name}",
                            nameof(sym)
                        );
                }
                switch (point)
                {
                    case Turtle.PointDrop.Normal:
                        turtle.DropPoint(normal_marker);
                        break;
                    case Turtle.PointDrop.Tip:
                        turtle.DropPoint(tip_marker);
                        break;
                }
            }
        }
        RunOn(program);
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        RebuildLSystem();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) { }
}

class Turtle
{
    Stack<Transform3D> stack;
    Node3D anchor;
    float step;
    float step_modifier;
    float angle;
    float angle_modifier;

    Transform3D currentTransform;
    Transform3D OldTransform { get; set; }

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

    public void DropPoint(PackedScene marker)
    {
        var node = marker.Instantiate<Node3D>();
        anchor.AddChild(node);
        node.Transform = OldTransform;
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
