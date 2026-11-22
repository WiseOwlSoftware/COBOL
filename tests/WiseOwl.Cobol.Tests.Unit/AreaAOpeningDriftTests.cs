// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ EVERY OPERAND YIELDS TO THE NEXT ENTRY OR THE NEXT PROCEDURE HEADER (kb/Work PB6011, PB6018). In the COBOL-85 reference
/// format a sentence or entry is continued in area B (§1985:IV.7.2.2: "Any sentence, entry, phrase, or clause may be continued
/// by starting subsequent line(s) in area B"), so a token that begins its fixed-form line in area A is no more of what stands
/// before it: in the data division an integer there is the next entry's level-number (§1985:IV.7.4), and in a sentence of
/// the procedure division a word or integer there that writes a header is the next paragraph's or section's name
/// (§1985:IV.7.3.3, §1985:IV.7.3.2). The parser asks <c>CobolParserCoreBase.nextEntryOpensHere</c> at the left edge of every
/// grammar alternative that BEGINS with an operand token (an INTEGERLIT, an IDENTIFIER, a reservation-gated word) and of
/// every alternative of the generated <c>cobolWord</c> rule, which every other word operand reaches, so a separator period
/// forgotten before the next entry or header is reported once, where it is missing. A new alternative that takes such a token
/// without the predicate would quietly read the next entry or header as an operand again (<c>VALUE "A" 01 B</c>,
/// <c>MOVE "B" TO A P1.</c>), so this scrape holds the inventory: every such alternative carries the predicate, and the rules
/// holding one are exactly the reviewed set, so a new one fails until it is decided here.
/// </summary>
public sealed class AreaAOpeningDriftTests
{
    /// <summary>The rules whose operand-led alternatives are guarded (each alternative, or the rule at its left edge).</summary>
    private static readonly string[] GuardedRules =
        ["cobolWord", "enterOperand", "integerLiteral", "numericLiteralCore", "procedureName", "residueInteger", "routineName",
         "signedIntegerLiteral"];

    /// <summary>The rules that take an operand token that is no operand of an entry or a sentence, and why the predicate is not
    /// theirs: <c>levelNumber</c> IS the level-number the predicate yields to; <c>formatWord</c> is a keyword the format
    /// recognizes by its TEXT, behind a predicate that names the text; <c>residueOpening</c> is the closed-format residue of
    /// the identification and environment divisions' paragraphs, which stand outside every sentence and the data division;
    /// the rest are DEFINITION slots of a reservation-gated word, outside every sentence and taking no integer, where the
    /// predicate is false (a header's own name, a data-name, a file-name, a program-name, a computer-name, a
    /// communication name).</summary>
    private static readonly string[] ExemptRules =
        ["cdName", "computerName", "dataName", "fileControlClauseGroup", "formatWord", "levelNumber",
         "programName", "residueOpening"];

    private const string Guard = "!nextEntryOpensHere()";

    private static string Body(string path) =>
        Regex.Replace(File.ReadAllText(path), @"//[^\r\n]*", string.Empty);

    /// <summary>The top-level alternatives of a rule body: split on the <c>|</c> outside every parenthesis and brace.</summary>
    private static IEnumerable<string> Alternatives(string body)
    {
        int depth = 0, start = 0;
        for (int i = 0; i < body.Length; i++)
        {
            switch (body[i])
            {
                case '(' or '{': depth++; break;
                case ')' or '}': depth--; break;
                case '|' when depth == 0:
                    yield return body[start..i];
                    start = i + 1;
                    break;
            }
        }
        yield return body[start..];
    }

    private const string OperandToken = @"\b(?:INTEGERLIT|IDENTIFIER|reservedGatedWord)\b";

    /// <summary>Does the alternative BEGIN with an operand token: before its first one stand only predicates, an optional sign,
    /// element labels and the opening of a group?</summary>
    private static bool LeadsWithAnOperand(string alternative)
    {
        var m = Regex.Match(alternative, OperandToken);
        if (!m.Success) return false;
        string before = alternative[..m.Index];
        // An IDENTIFIER behind a predicate of its own is a KEYWORD the format recognizes by its text (the wordAhead family,
        // UserWordSlotDriftTests' reading), never an operand word.
        if (m.Value == "IDENTIFIER" && Regex.IsMatch(before.Replace("{" + Guard + "}?", " ", StringComparison.Ordinal), @"\}\?"))
            return false;
        before = Regex.Replace(before, @"\{[^{}]*\}\?", " ");
        before = Regex.Replace(before, @"\(\s*PLUS\s*\|\s*MINUS\s*\)\?", " ");
        before = Regex.Replace(before, @"\b\w+\s*=\s*$", " ");
        before = Regex.Replace(before, @"^[\s(]*", string.Empty);
        return before.Trim().Length == 0;
    }

    [Fact]
    public void EveryAlternativeThatBeginsWithAnOperand_YieldsToTheNextEntryOrHeader()
    {
        var guarded = new SortedSet<string>(StringComparer.Ordinal);
        var unguarded = new List<string>();
        var holders = new SortedSet<string>(StringComparer.Ordinal);
        foreach (string file in Directory.GetFiles(TestRepo.Src("WiseOwl.Cobol.Frontend", "Grammar"), "*.g4", SearchOption.AllDirectories))
        {
            if (Path.GetFileName(file) == "CobolLexer.g4") continue;
            foreach (Match rule in Regex.Matches(Body(file), @"^(?<name>[a-z]\w*)\s*:(?<body>.*?)^\s*;",
                         RegexOptions.Multiline | RegexOptions.Singleline))
            {
                string name = rule.Groups["name"].Value;
                string body = rule.Groups["body"].Value;
                if (name == "reservedGatedWord" || !Regex.IsMatch(body, OperandToken)) continue;
                holders.Add(name);
                if (ExemptRules.Contains(name)) continue;
                // A guard at the rule's own left edge stands before every alternative.
                if (body.TrimStart().StartsWith("{" + Guard + "}?", StringComparison.Ordinal)) { guarded.Add(name); continue; }
                foreach (string alternative in Alternatives(body).Where(LeadsWithAnOperand))
                {
                    if (alternative.Contains(Guard, StringComparison.Ordinal)) guarded.Add(name);
                    else unguarded.Add($"{name}: {Regex.Replace(alternative.Trim(), @"\s+", " ")}");
                }
            }
        }
        Assert.True(unguarded.Count == 0, "an alternative begins with an operand token and does not ask "
            + $"{{{Guard}}}?, so it would read the next entry's level-number or the next header's name as an operand "
            + "(kb/Work PB6011, PB6018):\n" + string.Join("\n", unguarded));
        Assert.Equal(string.Join(" ", GuardedRules), string.Join(" ", guarded));
        // A scrape that found nothing cannot pass: the exempt rules must still be found holding their operand token.
        Assert.True(ExemptRules.All(holders.Contains),
            "the scrape no longer finds the exempt rule(s) " + string.Join(", ", ExemptRules.Where(r => !holders.Contains(r)))
            + ": it is blind, or the rule was renamed");
    }

    /// <summary>The generated <c>cobolWord</c> rule is the one every word operand reaches, so EVERY alternative of it asks the
    /// predicate (scripts/gen-cobol-words.ps1, kb/Work PB6018), IDENTIFIER included.</summary>
    [Fact]
    public void EveryCobolWordAlternative_YieldsToTheNextHeader()
    {
        string path = TestRepo.Src("WiseOwl.Cobol.Frontend", "Grammar", "Core", "CobolWords.g4");
        var m = Regex.Match(Body(path), @"^cobolWord\s*:(?<body>.*?)^\s*;", RegexOptions.Multiline | RegexOptions.Singleline);
        Assert.True(m.Success, "the cobolWord rule is gone from Core/CobolWords.g4 — this guard is blind");
        var alternatives = Alternatives(m.Groups["body"].Value).Select(a => a.Trim()).ToList();
        Assert.True(alternatives.Count > 40, $"only {alternatives.Count} cobolWord alternatives read — the parse broke");
        var bare = alternatives.Where(a => !a.StartsWith("{" + Guard, StringComparison.Ordinal)).ToList();
        Assert.True(bare.Count == 0, $"{bare.Count} cobolWord alternative(s) do not open with {Guard}: "
            + string.Join(", ", bare.Take(8)) + " — re-run scripts/gen-cobol-words.ps1 (kb/Work PB6018)");
    }
}
