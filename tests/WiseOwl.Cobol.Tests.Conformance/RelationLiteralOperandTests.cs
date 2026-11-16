// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using System.Text.RegularExpressions;
using WiseOwl.Cobol;
using Xunit;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// ISO §2023:8.8.4.2.1 — "A relation condition shall contain at least one reference to an operand that is not a literal"
/// (kb/Work PB1470). The sentence is neither a general format nor a syntax rule, so §2023:4.2.2's warning mechanism
/// indicates it: COBOL3146, a WARNING at every edition (docs/CONFORMANCE.md D-RELLITERAL), the relation compiling and
/// evaluating as written (<c>conformance:cases/8.8.4.2-Simple-relation-conditions/pb1470_relation_of_two_literals_runs</c> pins the values).
/// <para>The EXPECTED verdict is computed here from the rule over each operand kind's one property — is it a literal as
/// the source wrote it? A figurative constant is one (§2023:8.3.3.6 is a clause of §2023:8.3.3 Literals), a symbolic-character is
/// "a user-defined figurative constant" (§2023:8.3.2.2.29), and a constant-name's effect "is as if literal-1 … were written
/// where constant-name-1 is written" (§2023:13.10.4 GR1). A data item, an arithmetic expression and a function-identifier
/// (folded at bind time or not) are not. Adding an operand kind is one row of <see cref="Kinds"/>; every pair it forms
/// is then checked automatically.</para>
/// </summary>
public sealed class RelationLiteralOperandTests
{
    /// <param name="Name">The kind's key (used in the program-id and the failure message).</param>
    /// <param name="Operand">The operand as written in the relation.</param>
    /// <param name="Literal">A literal as the source wrote it (the rule's one question).</param>
    private sealed record Kind(string Name, string Operand, bool Literal);

    private static readonly Kind[] Kinds =
    [
        new("LI", "1", Literal: true),                         // numeric literal
        new("LA", "\"A\"", Literal: true),                     // alphanumeric literal
        new("LX", "X\"41\"", Literal: true),                   // hexadecimal alphanumeric literal (§2023:8.3.3.2 Format 2)
        new("SP", "SPACE", Literal: true),                     // figurative constant
        new("ZR", "ZERO", Literal: true),
        new("HV", "HIGH-VALUE", Literal: true),
        new("AL", "ALL \"A\"", Literal: true),                 // ALL literal (§2023:8.3.3.6.2 Format 6)
        new("SB", "SB", Literal: true),                        // symbolic-character (§2023:8.3.2.2.29)
        new("KT", "KT", Literal: true),                        // constant-name of an alphanumeric literal (§2023:13.10.4 GR1)
        new("KN", "KN", Literal: true),                        // constant-name of a numeric literal
        new("XA", "XA", Literal: false),                       // alphanumeric data item
        new("ND", "ND", Literal: false),                       // numeric data item
        new("EX", "ND + 1", Literal: false),                   // arithmetic expression of a data item
        new("EL", "1 + 1", Literal: false),                    // arithmetic expression of literals: an expression
        new("FL", "FUNCTION LENGTH(XA)", Literal: false),      // a function the binder folds (BoundNumericLiteral.FunctionValue)
        new("FU", "FUNCTION UPPER-CASE(XA)", Literal: false),  // a function it does not fold
    ];

    private const string Head = """
               ENVIRONMENT DIVISION.
               CONFIGURATION SECTION.
               SPECIAL-NAMES.
                   SYMBOLIC CHARACTERS SB IS 67.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01 KT CONSTANT AS "B".
               01 KN CONSTANT AS 7.
               01 XA PIC X(4) VALUE "ABCD".
               01 ND PIC 9(4) VALUE 12.
               01 T1.
                  05 E1 PIC X OCCURS 3 INDEXED BY I1.
               PROCEDURE DIVISION.
               MAIN-PARA.

        """;

    public static TheoryData<string> Subjects() => [.. Kinds.Select(k => k.Name)];

    /// <summary>One program per SUBJECT kind, one relation per line against every object kind; the COBOL3146 warnings
    /// by line are the pairs' verdicts — exactly one where both operands are literals, none otherwise.</summary>
    [Theory]
    [MemberData(nameof(Subjects))]
    public void EveryPair_WarnsExactlyWhenBothOperandsAreLiterals(string subject)
    {
        var l = Kinds.Single(k => k.Name == subject);
        var sb = new StringBuilder($"       IDENTIFICATION DIVISION.\n       PROGRAM-ID. RLIT{subject}.\n{Head}");
        int line = sb.ToString().Split('\n').Length;          // the 1-based line the first relation lands on
        var lineOf = new Dictionary<int, Kind>();
        foreach (var r in Kinds)
        {
            sb.Append($"           IF {l.Operand} = {r.Operand} CONTINUE END-IF\n");
            lineOf[line++] = r;
        }
        sb.Append("           STOP RUN.\n");

        var warned = WarningLines(CheckOnly(sb.ToString()).Warnings);
        var wrong = new List<string>();
        foreach (var (ln, r) in lineOf)
        {
            int want = l.Literal && r.Literal ? 1 : 0;
            int got = warned.GetValueOrDefault(ln);
            if (got != want) wrong.Add($"{l.Operand} = {r.Operand}: expected {want} COBOL3146, got {got}");
        }
        Assert.True(wrong.Count == 0 && warned.Keys.All(lineOf.ContainsKey),
            "ISO §2023:8.8.4.2.1 verdicts differ:\n  " + string.Join("\n  ", wrong));
    }

    /// <summary>⛔ EVERY SURFACE THAT WRITES A RELATION CONDITION asks the rule (<c>ConditionBinder.WrittenRelational</c>):
    /// PERFORM UNTIL, SEARCH WHEN, an abbreviated relation's inserted subject (§2023:8.8.4.12 — <c>1 = ND OR 2</c> holds the
    /// relation <c>1 = 2</c>), and an EVALUATE partial-expression, which §2023:14.9.13.3 SR8 treats "as though it were
    /// specified as condition-2". The complement: an EVALUATE subject/object pair and a range are governed by
    /// §2023:14.9.13.3 SR10's Table 15, which permits a range-expression against a literal subject, and an abbreviated
    /// relation whose inserted subject is a data item compares a data item.</summary>
    [Theory]
    [InlineData("RLITS1", "PERFORM UNTIL 1 = 1 CONTINUE END-PERFORM", 1)]
    [InlineData("RLITS2", "SET I1 TO 1 SEARCH E1 AT END CONTINUE WHEN \"A\" = SPACE CONTINUE END-SEARCH", 1)]
    [InlineData("RLITS3", "IF 1 = ND OR 2 CONTINUE END-IF", 1)]
    [InlineData("RLITS4", "EVALUATE 1 WHEN > 0 CONTINUE END-EVALUATE", 1)]
    [InlineData("RLITS5", "IF ND = 1 AND KN = 7 CONTINUE END-IF", 1)]
    [InlineData("RLITS6", "IF ND = 1 OR 2 CONTINUE END-IF", 0)]
    [InlineData("RLITS7", "EVALUATE 3 WHEN 1 THRU 5 CONTINUE END-EVALUATE", 0)]
    [InlineData("RLITS8", "EVALUATE ND WHEN 1 CONTINUE WHEN > 2 CONTINUE END-EVALUATE", 0)]
    public void EveryWrittenRelationSurface_AsksTheRule(string pid, string statement, int warnings)
    {
        var (errors, found) = CheckOnly($"       IDENTIFICATION DIVISION.\n       PROGRAM-ID. {pid}.\n{Head}"
            + $"           {statement}.\n           STOP RUN.\n");
        Assert.True(errors.Count == 0, string.Join(" | ", errors));
        Assert.Equal(warnings, WarningLines(found).Values.Sum());
    }

    /// <summary>A WARNING at every edition, never an error: the program compiles strict and the relation is evaluated
    /// as written (§2023:4.2.2's warning mechanism; D-RELLITERAL).</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void IsAWarningAtEveryEdition(int edition)
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull($"""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. RLITED{edition}.
                   PROCEDURE DIVISION.
                   MAIN-PARA.
                       IF ZERO = ZERO CONTINUE END-IF
                       STOP RUN.
            """, edition);
        Assert.True(ok, string.Join(" | ", errors));
        // Each edition's own sentence: 1985's is "at least one reference to a variable" (COBOL7609), 2002's the "operand
        // that is not a literal" (COBOL3146) at that edition's clause.
        var (code, cite) = edition switch
        {
            85 => ("COBOL7609", "§1985:VI.6.3.1.1"),
            2002 => ("COBOL3146", "§2002:8.8.4.1.1"),
            2014 => ("COBOL3146", "§2014:8.8.4.1.1"),
            _ => ("COBOL3146", "§2023:8.8.4.2.1"),
        };
        Assert.Single(warnings, w => w.Contains($"warning {code}", StringComparison.Ordinal)
            && w.Contains($"ISO {cite} requires", StringComparison.Ordinal));
    }

    /// <summary>⛔ AT --std 85 THE RULE IS §1985:VI.6.3.1.1 — "The relation condition must contain at least one reference to
    /// a variable" — and §1985:VI.6.3.1.5 for the sign condition: "Arithmetic-expression-1 must contain at least one
    /// reference to a variable" (kb/Work PB5826). An arithmetic expression of literals references no variable, so
    /// <c>IF 2 * 2 = 4</c> is outside the 1985 text though it passes the 2002 "operand that is not a literal" sentence, and
    /// a sign condition of literals is outside it too, a sentence the 2002 and 2023 sign-condition texts do not have.
    /// Expected values come from those sentences: the count of COBOL7609 / COBOL7610 warnings at 85, and none at 2002.</summary>
    [Theory]
    [InlineData("IF 1 = 2 CONTINUE END-IF", 1, 0)]
    [InlineData("IF 2 * 2 = 4 CONTINUE END-IF", 1, 0)]
    [InlineData("IF 2 * 2 = ND CONTINUE END-IF", 0, 0)]
    [InlineData("IF ND = 4 CONTINUE END-IF", 0, 0)]
    [InlineData("IF 3 IS POSITIVE CONTINUE END-IF", 0, 1)]
    [InlineData("IF 2 * 2 IS NOT ZERO CONTINUE END-IF", 0, 1)]
    [InlineData("IF ND IS POSITIVE CONTINUE END-IF", 0, 0)]
    [InlineData("IF ND + 1 IS POSITIVE CONTINUE END-IF", 0, 0)]
    [InlineData("PERFORM UNTIL 1 = 1 CONTINUE END-PERFORM", 1, 0)]
    [InlineData("SET I1 TO 1 SEARCH E1 AT END CONTINUE WHEN 1 + 1 = 2 CONTINUE END-SEARCH", 1, 0)]
    [InlineData("SET I1 TO 1 SEARCH E1 AT END CONTINUE WHEN 3 IS POSITIVE CONTINUE END-SEARCH", 0, 1)]
    public void At85_AConditionWithNoVariableIsFlagged_AndFrom2002NotByTheseCodes(string statement, int relation, int sign)
    {
        // A 1985 head: the shared Head names a constant-name and a symbolic-character, which 1985 lacks.
        const string head85 = """
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 ND PIC 9(4) VALUE 12.
                   01 T1.
                      05 E1 PIC X OCCURS 3 INDEXED BY I1.
                   PROCEDURE DIVISION.
                   MAIN-PARA.

            """;
        string source = $"       IDENTIFICATION DIVISION.\n       PROGRAM-ID. RNOV85.\n{head85}           {statement}.\n           STOP RUN.\n";
        var (ok85, errors85, warnings85) = EditionHarness.CompileFull(source, 85);
        Assert.True(ok85, string.Join(" | ", errors85));
        Assert.Equal(relation, warnings85.Count(w => w.Contains("warning COBOL7609", StringComparison.Ordinal)));
        Assert.Equal(sign, warnings85.Count(w => w.Contains("warning COBOL7610", StringComparison.Ordinal)));
        // From 2002 the 1985 sentences are not the rule: no COBOL7609 or COBOL7610 at a later edition.
        var (ok2002, errors2002, warnings2002) = EditionHarness.CompileFull(source, 2002);
        Assert.True(ok2002, string.Join(" | ", errors2002));
        Assert.DoesNotContain(warnings2002, w => w.Contains("COBOL7609", StringComparison.Ordinal)
            || w.Contains("COBOL7610", StringComparison.Ordinal));
    }

    private static Dictionary<int, int> WarningLines(IEnumerable<string> warnings) =>
        warnings.Select(w => Regex.Match(w, @"\((\d+),\d+\): warning COBOL3146"))
            .Where(m => m.Success)
            .GroupBy(m => int.Parse(m.Groups[1].Value))
            .ToDictionary(g => g.Key, g => g.Count());

    /// <summary>Parse + bind only (no Roslyn emit): the rule is settled where the relation is bound.</summary>
    private static (IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings) CheckOnly(string source)
    {
        string dir = ScratchDirectory.Create("WiseOwlCobol_Rl_");
        try
        {
            string src = CompiledProgramCache.StageSource(Path.Combine(dir, "prog.cob"), source);
            var r = CompiledProgramCache.Compile(new CompilerDriver.Options(src, Path.Combine(dir, "prog.dll"),
                DialectLevel: 2023, CheckOnly: true, SourceFormat: InitialReferenceFormat.Auto));
            return (r.Success ? [] : [.. r.Errors.DefaultIfEmpty($"status {r.Status}")], r.Warnings);
        }
        finally { ScratchDirectory.Remove(dir); }
    }
}
