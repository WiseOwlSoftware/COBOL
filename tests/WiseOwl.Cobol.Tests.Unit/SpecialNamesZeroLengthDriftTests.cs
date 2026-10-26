// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ EVERY SPECIAL-NAMES LITERAL OPERAND ASKS THE ONE ZERO-LENGTH QUESTION (kb/Work PB3218). ISO §12.3.7.3 SR11 bars a
/// zero-length literal from literal-1 through literal-6 and literal-9 (<c>cite.py --check 12.3.7.3</c> → OK 12.3.7.3 11)),
/// and §8.8.3.3 GR2/GR3 make a concatenation of zero-length literals one. The ALPHABET / CLASS operands tested the raw
/// token (<c>""&amp;""</c> is not zero-length as written) while the LOCALE / ORDER TABLE literals tested the folded
/// value, so the same operand was refused by one clause and compiled clean in the other.
/// <para>The literal operands of <c>DataBinder.Switches.cs</c> ask <c>IsZeroLengthOperand</c> (a literal) or
/// <c>IsZeroLengthConstant</c> (a constant-name) and nothing else: the file calls <c>CobolLiteral.IsZeroLength</c> only
/// inside those two predicates, so a NEXT SPECIAL-NAMES literal screen cannot test a token of its own.</para>
/// </summary>
public sealed class SpecialNamesZeroLengthDriftTests
{
    private static string Code(string line)
    {
        int at = line.IndexOf("//", StringComparison.Ordinal);
        return at < 0 ? line : line[..at];
    }

    [Fact]
    public void TheSpecialNamesBinder_TestsZeroLengthOnlyInTheTwoPredicates()
    {
        string path = TestRepo.Src("WiseOwl.Cobol.Compiler", "Binding", "DataBinder.Switches.cs");
        var lines = File.ReadAllLines(path);
        var outside = new List<string>();
        string? inside = null;
        int calls = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            string code = Code(lines[i]);
            if (code.Contains("private bool IsZeroLengthOperand(", StringComparison.Ordinal)) inside = "IsZeroLengthOperand";
            else if (code.Contains("private static bool IsZeroLengthConstant(", StringComparison.Ordinal)) inside = "IsZeroLengthConstant";
            else if (code.Contains("private ", StringComparison.Ordinal) && code.Contains('(')) inside = null;
            if (!code.Contains("CobolLiteral.IsZeroLength(", StringComparison.Ordinal)) continue;
            calls++;
            if (inside is null) outside.Add($"DataBinder.Switches.cs:{i + 1}: {lines[i].Trim()}");
        }
        Assert.True(outside.Count == 0,
            "a SPECIAL-NAMES literal screen tests zero length itself — ask IsZeroLengthOperand / IsZeroLengthConstant, "
            + "which decode a concatenation before they test (kb/Work PB3218):\n" + string.Join("\n", outside));
        Assert.True(calls == 2, $"expected the two predicates to hold the file's only CobolLiteral.IsZeroLength calls, found {calls}");
    }

    [Fact]
    public void BothLiteralScreens_AskThePredicate()
    {
        string text = File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Compiler", "Binding", "DataBinder.Switches.cs"));
        int phrase = text.IndexOf("private PhraseOperand? LiteralPhraseOperand(", StringComparison.Ordinal);
        int clause = text.IndexOf("private bool TryClauseTextLiteral(", StringComparison.Ordinal);
        Assert.True(phrase > 0 && clause > 0, "LiteralPhraseOperand or TryClauseTextLiteral is gone");
        string phraseBody = text[phrase..Math.Min(text.Length, phrase + 9000)];
        string clauseBody = text[clause..Math.Min(text.Length, clause + 6000)];
        Assert.Contains("IsZeroLengthOperand(lit)", phraseBody);
        Assert.Contains("IsZeroLengthOperand(lit)", clauseBody);
        Assert.Contains("IsZeroLengthConstant(", phraseBody);
        Assert.Contains("IsZeroLengthConstant(", clauseBody);
    }
}
