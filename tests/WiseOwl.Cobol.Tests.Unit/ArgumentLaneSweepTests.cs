// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ EVERY ARGUMENT SHAPE UNDER EVERY RECEIVER KIND COMPILES WITHOUT AN INTERNAL COMPILER ERROR (kb/Work PB5893 review R2-1, R2-2).
/// </summary>
/// <remarks>
/// <para>The lane of a numeric node is one function of the bound node (<c>NumericLanes.LaneOf</c>), and the renderer asserts every
/// operation it renders against it (<c>NumericRenderer.AssertLane</c>, an internal compiler error on a disagreement). A disagreement is
/// found only by a program that has the shape: review R2-1 wrote <c>FUNCTION ABS (FUNCTION ABS (A / 3) + 1)</c> into a COMP-2, whose
/// inner call's lane ignored the argument's significance, and R2-2 <c>FUNCTION ABS (- FUNCTION ABS (A / 3))</c>, an internal compiler
/// error on main too. This sweep GENERATES the shapes rather than waiting for a reviewer to write them: each receiver kind (a fixed
/// item, the same ROUNDED, COMP-1, COMP-2, FLOAT-LONG, a floating-point numeric-edited item) times each kind of argument position
/// (a numeric position, an integer position, a position whose function is integer only by its result type) times a small shape
/// grammar two levels deep (an atom: an item, a quotient that does not terminate, one that does, a negated quotient, a nested call, a
/// negated nested call; each under + 1, - 1, * 2, / 2, / 3, negation, a nested call and a two-argument selection), compiled at 2023,
/// and at 85 where the receiver and functions exist (a fixed receiver; MAX for the nested call, ABS being a 2002 function).</para>
/// <para>One program per receiver kind and edition, compile only (no run): the assertion is a count of compiled statements, never a
/// clock. On a failure the statements are compiled one by one so the message names each shape that fails.</para>
/// </remarks>
public sealed class ArgumentLaneSweepTests : WiseOwlCobolTestBase
{
    /// <summary>The receiver kinds: the description of R, and whether the statement is ROUNDED.</summary>
    private static readonly (string Name, string Picture, bool Rounded, int[] Editions)[] Receivers =
    [
        ("fixed", "PIC S9(5)V9(5)", false, [2023, 85]),
        ("fixed-rounded", "PIC S9(5)V9(5)", true, [2023, 85]),
        ("comp-1", "COMP-1", false, [2023]),
        ("comp-2", "COMP-2", false, [2023]),
        ("float-long", "FLOAT-LONG", false, [2023]),
        ("float-edited", "PIC +9.9(16)E+99", false, [2023]),
    ];

    public static TheoryData<string, int> Cases()
    {
        var data = new TheoryData<string, int>();
        foreach (var r in Receivers)
            foreach (int edition in r.Editions) data.Add(r.Name, edition);
        return data;
    }

    /// <summary>The nested call of the grammar: ABS from 2002 on, MAX (t, 0) at 85.</summary>
    private static string Call(string t, int edition) => edition >= 2002 ? $"FUNCTION ABS ({t})" : $"FUNCTION MAX ({t}, 0)";

    /// <summary>The shapes, in a stable order: six atoms, then each atom under eight operations.</summary>
    internal static List<string> Shapes(int edition)
    {
        var atoms = new List<string> { "A", "A / 3", "A / 4", "- (A / 3)", Call("A / 3", edition), Call("- " + Call("A / 3", edition), edition) };
        var shapes = new List<string>(atoms);
        foreach (var t in atoms)
        {
            shapes.Add($"{Paren(t)} + 1");
            shapes.Add($"{Paren(t)} - 1");
            shapes.Add($"{Paren(t)} * 2");
            shapes.Add($"{Paren(t)} / 2");
            shapes.Add($"{Paren(t)} / 3");
            shapes.Add($"- {Paren(t)}");
            shapes.Add(Call(t, edition));
            shapes.Add($"FUNCTION MAX ({t}, 1)");
        }
        return shapes;
    }

    private static string Paren(string t) => t.StartsWith("FUNCTION", StringComparison.Ordinal) || t == "A" ? t : $"({t})";

    /// <summary>Each shape at each kind of argument position: numeric (ABS or MAX, REM, MEAN), integer (MOD over an integer function of
    /// the shape) and integer by the result type (MAX over an integer function of the shape).</summary>
    internal static List<string> Expressions(int edition) =>
        [.. Shapes(edition).SelectMany(s => new[]
        {
            Call(s, edition),
            $"FUNCTION REM ({s}, 7)",
            $"FUNCTION MEAN ({s}, 1)",
            $"FUNCTION MOD (FUNCTION INTEGER ({s}), 7)",
            $"FUNCTION MAX (FUNCTION INTEGER ({s}), 1)",
        })];

    private string Program(string receiver, int edition, IEnumerable<string> expressions)
    {
        var r = Receivers.Single(x => x.Name == receiver);
        var proc = new StringBuilder();
        foreach (string e in expressions) proc.AppendLine($"    COMPUTE R{(r.Rounded ? " ROUNDED" : "")} = {e}.");
        return $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ARGSWEEP.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 A PIC 9 VALUE 7.
            01 R {r.Picture}.
            PROCEDURE DIVISION.
            P1.
            {proc}    STOP RUN.
            """;
    }

    /// <summary>The compiler's answer for one program: null when it compiled, else the errors or the exception.</summary>
    private string? Compile(string name, string source, int edition)
    {
        string path = Path.Combine(TempDir, name + ".cob");
        File.WriteAllText(path, source);
        try
        {
            var result = CompilerDriver.Compile(new CompilerDriver.Options(path, Path.Combine(TempDir, name + ".dll"),
                DialectLevel: edition, SourceFormat: InitialReferenceFormat.Free));
            return result.Errors.Count == 0 ? null : string.Join("; ", result.Errors);
        }
        catch (InvalidOperationException e)
        {
            return e.Message;
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void EveryArgumentShape_CompilesUnderTheReceiver(string receiver, int edition)
    {
        var expressions = Expressions(edition);
        // ⛔ A RUN ASSERTS ITS POPULATION: 6 atoms and 48 compound shapes, at 5 positions each.
        Assert.Equal(54 * 5, expressions.Count);
        string? whole = Compile($"sweep-{receiver}-{edition}", Program(receiver, edition, expressions), edition);
        if (whole is null) return;
        var failing = new List<string>();
        for (int i = 0; i < expressions.Count; i++)
            if (Compile($"one-{receiver}-{edition}-{i}", Program(receiver, edition, [expressions[i]]), edition) is { } why)
                failing.Add($"{expressions[i]}: {why}");
        Assert.Fail($"{failing.Count} of {expressions.Count} argument shapes do not compile under a {receiver} receiver at {edition} "
            + "(kb/Work PB5893 review R2-1):\n  " + string.Join("\n  ", failing.DefaultIfEmpty(whole)));
    }
}
