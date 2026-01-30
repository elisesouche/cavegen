using System.Collections.Generic;
using Godot;

namespace LayoutGen;

[Tool]
public partial class LayoutGenerator : Node
{
    private LSystem system;

    [Export]
    Godot.Collections.Array<string> macro_defs;

    [Export]
    Godot.Collections.Array<string> rules;

    [ExportToolButton("Rebuild L-System")]
    public Callable RebuildButton => Callable.From(RebuildLSystem);

    [Export]
    int numRuns = 10;

    [ExportToolButton("Generate")]
    public Callable GenerateButton => Callable.From(Run);

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
            Initial = new NonTerminal(NonTerminalSymbols.Forward),
            Productions = d,
            MacroSystem = macroSystem,
        };
        GD.Print(LSystemFormatter.LSystem(system));
    }

    void Run()
    {
        var res = system.Run(numRuns);
        GD.Print(LSystemFormatter.Form(res));
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        RebuildLSystem();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) { }
}
