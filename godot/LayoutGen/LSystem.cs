using System;
using System.Collections.Generic;
using System.Linq;

namespace LayoutGen;

enum TerminalSymbols
{
    StartBranch, // Start branch
    EndBranch, // End branch
}

enum NonTerminalSymbols
{
    Forward, // Move forward
    YawClockwise, // Yaw clockwise
    YawCounterClockwise, // Yaw counterclockwise
    PitchUp, // Pitch up
    PitchDown, // Pitch down
    IncreaseAngle, // Increase the angle
    DecreaseAngle, // Decrease the angle
    IncreaseStep, // Step increase
    DecreaseStep, // Step decrease
    BranchTip, // The tip of a branch
    BranchEnd, // Stop connecting other branches
}

abstract class Symbol { }

class Terminal : Symbol
{
    public TerminalSymbols Self { get; set; }

    public Terminal(TerminalSymbols self)
    {
        this.Self = self;
    }
}

class NonTerminal : Symbol
{
    public NonTerminalSymbols Self { get; set; }

    public NonTerminal(NonTerminalSymbols self)
    {
        this.Self = self;
    }
}

class LSystem
{
    public NonTerminal Initial { get; set; }

    public Dictionary<NonTerminal, List<List<Symbol>>> Productions { get; set; }

    HashSet<List<Symbol>> empty = new();

    Random r = new();

    List<Symbol> RunIter(List<Symbol> generation)
    {
        var next = new List<Symbol>();
        foreach (var sym in generation)
        {
            List<Symbol> productions = sym switch
            {
                NonTerminal n => r.InList(
                    this.Productions.TryGetValue(n, out var p)
                        ? p
                        :
                        [
                            [sym],
                        ]
                ),

                Terminal t => [t],

                _ => throw new ArgumentException(
                    $"Unhandled Symbol subtype: {sym?.GetType().Name}",
                    nameof(sym)
                ),
            };
            next.AddRange(productions);
        }
        return next;
    }

    public List<Symbol> Run(int runs) => (runs == 0) ? [Initial] : RunIter(Run(runs - 1));
}

static class LSystemFormatter
{
    public static string Symbol(Symbol s) =>
        s switch
        {
            Terminal t => t.Self switch
            {
                TerminalSymbols.StartBranch => "[",
                TerminalSymbols.EndBranch => "]",
                _ => throw new ArgumentOutOfRangeException(
                    nameof(t.Self),
                    t.Self,
                    "Unhandled TerminalSymbols value"
                ),
            },

            NonTerminal n => n.Self switch
            {
                NonTerminalSymbols.Forward => "F",
                NonTerminalSymbols.YawClockwise => "R",
                NonTerminalSymbols.YawCounterClockwise => "L",
                NonTerminalSymbols.PitchUp => "U",
                NonTerminalSymbols.PitchDown => "D",
                NonTerminalSymbols.IncreaseAngle => "O",
                NonTerminalSymbols.DecreaseAngle => "A",
                NonTerminalSymbols.IncreaseStep => "B",
                NonTerminalSymbols.DecreaseStep => "S",
                NonTerminalSymbols.BranchTip => "Z",
                _ => throw new ArgumentOutOfRangeException(
                    nameof(n.Self),
                    n.Self,
                    "Unhandled NonTerminalSymbols value"
                ),
            },

            _ => throw new ArgumentException(
                $"Unhandled Symbol subtype: {s?.GetType().Name}",
                nameof(s)
            ),
        };

    public static string Form(List<Symbol> l) => string.Concat(l.Select(Symbol));

    public static string Production(NonTerminal from, List<Symbol> to) =>
        $"{Symbol(from)} -> {Form(to)}";
}

static class LSystemParser
{
    public static Symbol Symbol(char c) =>
        c switch
        {
            '[' => new Terminal(TerminalSymbols.StartBranch),
            ']' => new Terminal(TerminalSymbols.EndBranch),

            'F' => new NonTerminal(NonTerminalSymbols.Forward),
            'R' => new NonTerminal(NonTerminalSymbols.YawClockwise),
            'L' => new NonTerminal(NonTerminalSymbols.YawCounterClockwise),
            'U' => new NonTerminal(NonTerminalSymbols.PitchUp),
            'D' => new NonTerminal(NonTerminalSymbols.PitchDown),
            'O' => new NonTerminal(NonTerminalSymbols.IncreaseAngle),
            'A' => new NonTerminal(NonTerminalSymbols.DecreaseAngle),
            'B' => new NonTerminal(NonTerminalSymbols.IncreaseStep),
            'S' => new NonTerminal(NonTerminalSymbols.DecreaseStep),
            'Z' => new NonTerminal(NonTerminalSymbols.BranchTip),

            _ => throw new FormatException($"Unknown symbol '{c}'"),
        };

    public static List<Symbol> Form(string s) => s.Select(Symbol).ToList();

    public static (NonTerminal from, List<Symbol> to) Production(string s)
    {
        var parts = s.Split("->", StringSplitOptions.TrimEntries);

        if (parts.Length != 2)
            throw new FormatException("Invalid production format");

        var fromSymbol = Symbol(parts[0][0]);

        if (fromSymbol is not NonTerminal from)
            throw new FormatException("Production LHS must be a NonTerminal");

        var to = Form(parts[1]);

        return (from, to);
    }
}
