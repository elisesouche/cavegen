using System;
using System.Collections.Generic;
using Godot;

namespace LayoutGen;

public partial class LayoutGenerator : Node
{
    LSystem system;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        system.Initial = new NonTerminal(NonTerminalSymbols.BranchTip);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) { }
}
