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
    PackedScene marker;

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
        GD.Print(LSystemFormatter.Form(res));
        Turtle(res);
    }

    void DeleteMarkers()
    {
        foreach (var child in anchor.GetChildren())
        {
            child.QueueFree();
        }
    }

    void Turtle(List<Symbol> program)
    {
        var turtle = new Turtle(anchor, step, step_modifier, angle, angle_modifier);
        void RunOn(List<Symbol> program)
        {
            foreach (var sym in program)
            {
                turtle.DropPoint(marker);
                switch (sym)
                {
                    case NonTerminal n:
                        turtle.StepNonTerminal(n);
                        break;
                    case Terminal t:
                        turtle.StepTerminal(t);
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

    Transform3D current_trans;

    public Turtle(Node3D anchor, float step, float step_modifier, float angle, float angle_modifier)
    {
        this.stack = [];
        this.anchor = anchor;
        this.step = step;
        this.step_modifier = step_modifier;
        this.angle = angle;
        this.angle_modifier = angle_modifier;

        this.current_trans = anchor.Transform;
    }

    public void StepNonTerminal(NonTerminal n)
    {
        switch (n.Self)
        {
            case NonTerminalSymbols.Forward:
                current_trans = current_trans.Translated(current_trans.Basis.Z * step);
                break;
            case NonTerminalSymbols.YawClockwise:
                current_trans = current_trans.Rotated(current_trans.Basis.Y, angle);
                break;
            case NonTerminalSymbols.YawCounterClockwise:
                current_trans = current_trans.Rotated(current_trans.Basis.Y, -angle);
                break;
            case NonTerminalSymbols.PitchUp:
                current_trans = current_trans.Rotated(current_trans.Basis.X, angle);
                break;
            case NonTerminalSymbols.PitchDown:
                current_trans = current_trans.Rotated(current_trans.Basis.X, -angle);
                break;
            case NonTerminalSymbols.IncreaseAngle:
                angle += angle_modifier;
                break;
            case NonTerminalSymbols.DecreaseAngle:
                angle -= angle_modifier;
                break;
            case NonTerminalSymbols.IncreaseStep:
                step += step_modifier;
                break;
            case NonTerminalSymbols.DecreaseStep:
                step -= step_modifier;
                break;
            default:
                // remains the case of the branch tips and ends
                break;
        }
    }

    public void DropPoint(PackedScene marker)
    {
        var node = marker.Instantiate<Node3D>();
        anchor.AddChild(node);
        node.Transform = current_trans;
    }

    public void StepTerminal(Terminal t)
    {
        switch (t.Self)
        {
            case TerminalSymbols.StartBranch:
                stack.Push(current_trans);
                break;
            case TerminalSymbols.EndBranch:
                current_trans = stack.Pop();
                break;
        }
    }
}
