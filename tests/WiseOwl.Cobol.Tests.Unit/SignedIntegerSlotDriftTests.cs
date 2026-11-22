// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE SIGNED-INTEGER SLOT INVENTORY (kb/Work PB553). The grammar carries TWO integer-literal rules and the
/// difference between them is a RULE, not an oversight:
/// <list type="bullet">
/// <item><c>integerLiteral</c> is the metalanguage <c>integer-n</c> of a printed general format.
/// ISO §2023:5.5 SR1 — "When the term 'integer-n' (n = 1, 2, …) is used in a general format and associated rules, it
/// refers to a fixed-point integer literal that shall be unsigned and nonzero unless otherwise specified in
/// the associated rules." So <c>01 T PIC X OCCURS +3.</c> is non-conforming. Since kb/Work PB6021 the rule PARSES the
/// sign (one character-string with its digits) so that the one integer-n screen, <c>IntegerOperandPass</c>, refuses it
/// BY THAT RULE (COBOL7737) instead of a parse error naming a clause list; the sign is still never accepted.</item>
/// <item><c>signedIntegerLiteral : (PLUS | MINUS)? INTEGERLIT</c> is the other slot: one the standard prints
/// as something other than <c>integer-n</c> and constrains as an INTEGER NUMERIC LITERAL, which routes through
/// §2023:5.5 SR2a to §2023:8.3.3.3.2 SR2 and its signed leftmost character. §2023:13.18.63.2's <c>subscript-1</c> /
/// <c>subscript-2</c> under §2023:13.18.63.3 SR19 is the one such slot in the whole grammar.</item>
/// </list>
/// <para>PB553's own note proposed the opposite fix — sign <c>integerLiteral</c> once and let every
/// integer-literal position inherit it — which would have ACCEPTED the OCCURS spelling above. This scrape
/// pins the decision in BOTH directions so neither can be undone silently: the integer-n rule's sign is parsed only
/// to be refused (its one shape, and the screen that refuses it, are both pinned), and a new signed slot may not be
/// added without landing here.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class SignedIntegerSlotDriftTests
{
    private static string CompilerSource(params string[] parts) =>
        System.IO.File.ReadAllText(System.IO.Path.Combine([TestRepo.Src("WiseOwl.Cobol.Compiler"), .. parts]));

    private static string GrammarDir() => TestRepo.Src("WiseOwl.Cobol.Frontend", "Grammar");

    private static string[] GrammarFiles() =>
        System.IO.Directory.GetFiles(GrammarDir(), "*.g4", System.IO.SearchOption.AllDirectories);

    /// <summary>A grammar file's text with its line comments removed, so a rule NAMED in a comment (this
    /// grammar documents its rules heavily) cannot be mistaken for a reference.</summary>
    private static string Body(string path) =>
        Regex.Replace(System.IO.File.ReadAllText(path), @"//[^\r\n]*", string.Empty);

    [Fact]
    public void IntegerLiteral_ParsesItsSignOnlyForTheOneScreenToRefuse()
    {
        string src = Body(System.IO.Path.Combine(GrammarDir(), "Core", "CobolExpressions.g4"));
        var m = Regex.Match(src, @"^\s*integerLiteral\s*:(?<body>.*?);", RegexOptions.Multiline | RegexOptions.Singleline);
        Assert.True(m.Success, "the integerLiteral rule is gone from Core/CobolExpressions.g4 — this guard is blind");
        // The left-edge predicates admit no token, so they are set aside before the shape is read: the one that keeps the
        // next entry's level-number out of every integer operand (kb/Work PB6011, AreaAOpeningDriftTests), and the one that
        // takes a sign only as the literal's own leftmost character (§2023:8.3.3.3.2; kb/Work PB6021).
        Assert.Contains("{signedIntegerAhead()}?", m.Groups["body"].Value);
        string body = Regex.Replace(Regex.Replace(m.Groups["body"].Value, @"\{[^{}]*\}\?", string.Empty), @"\s+", " ").Trim();
        Assert.Equal("( (PLUS | MINUS))? INTEGERLIT", body);
        // ⛔ THE SIGN IS PARSED TO BE REFUSED, NEVER ACCEPTED (§2023:5.5 SR1; §1985:III.2 p142): the one screen asks it of
        // every integerLiteral with no table (no format permits it), and the binder reads the digits only.
        string pass = CompilerSource("Validation", "IntegerOperandPass.cs");
        Assert.Matches(@"if \(ctx\.Sign\(\) is not null\)\s*\{[^}]*DiagnosticCatalog\.IntegerOperandSigned", pass);
        Assert.Contains("public static string Digits(this CobolParserCore.IntegerLiteralContext literal) => literal.INTEGERLIT().GetText();",
            System.IO.File.ReadAllText(System.IO.Path.Combine(TestRepo.Src("WiseOwl.Cobol.Frontend"), "Parsing", "IntegerLiteralParts.cs")));
    }

    [Fact]
    public void IntegerLiteral_IsReadByItsDigitsOnly()
    {
        // ⛔ GetText() of an integerLiteral includes a refused sign; a binder that read '-3' as a value would raise a second
        // diagnostic from a range rule (or lay out a negative table). Every reader goes through IntegerLiteralParts.Digits or
        // IntegerOperandRules.HostValue (kb/Work PB6021); a GetText() of the node is only ever a quotation of what was written, in a message.
        string[] sources = System.IO.Directory.GetFiles(
            TestRepo.Src("WiseOwl.Cobol.Compiler"), "*.cs", System.IO.SearchOption.AllDirectories);
        var offenders = new System.Collections.Generic.List<string>();
        int readers = 0;
        foreach (string f in sources)
        {
            string src = System.IO.File.ReadAllText(f);
            readers += Regex.Matches(src, @"integerLiteral\s*\(\s*\)[^;\r\n]*?\.Digits\b|\bIntegerOperandRules\.HostValue\s*\(").Count;
            foreach (Match m in Regex.Matches(src, @"integerLiteral\s*\(\s*\)[^;\r\n]*?\.GetText\s*\("))
                offenders.Add($"{System.IO.Path.GetFileName(f)}: {m.Value}");
        }
        Assert.True(readers > 5, $"only {readers} reader call(s) of IntegerLiteralParts.Digits / IntegerOperandRules.HostValue — the scan is blind");
        Assert.Empty(offenders);
    }

    [Fact]
    public void SignedIntegerLiteral_IsReferencedByExactlyTheSlotsTheStandardSigns()
    {
        // The §2023:13.18.63.2 Format-2 VALUE clause's FROM/TO subscript lists — and nothing else: valueTableSubscript is
        // that one slot, a signed integer literal or the integer constant-name standing for it (§2023:13.10.3 SR2, kb/Work
        // PB780). A NEW entry here is a claim that the standard prints that slot as something other than `integer-n`;
        // make the claim in the rule's comment, cite the syntax rule, and then add the name below.
        string[] expected = ["valueTableSubscript"];

        var referencing = new System.Collections.Generic.SortedSet<string>(System.StringComparer.Ordinal);
        foreach (string f in GrammarFiles())
        {
            string src = Body(f);
            // Split into rules on a left-edge `name :` header; the rule that OWNS a reference is the last
            // header before it.
            foreach (Match rule in Regex.Matches(src, @"^(?<name>[a-z]\w*)\s*:(?<body>.*?)^\s*;",
                                                 RegexOptions.Multiline | RegexOptions.Singleline))
            {
                string name = rule.Groups["name"].Value;
                if (name == "signedIntegerLiteral") continue;      // the declaration itself
                if (Regex.IsMatch(rule.Groups["body"].Value, @"\bsignedIntegerLiteral\b"))
                    referencing.Add(name);
            }
        }

        Assert.Equal(string.Join(" ", expected), string.Join(" ", referencing));
    }

    [Fact]
    public void SignedIntegerLiteral_HasExactlyOneReader()
    {
        // ⛔ ANTLR's GetText() concatenates a node's tokens with the whitespace stripped, so `+ 1` and `+1`
        // both read back as "+1" and §2023:8.3.3.3.2 SR2's adjacency violation is INVISIBLE to any caller that goes
        // straight to the text. SignedIntegerLiteral.Read measures it on the token stream's own indices
        // (LiteralScreen reports it, kb/Work PB1445); this assertion keeps it the only door (kb/Work PB553).
        string[] sources = System.IO.Directory.GetFiles(
            TestRepo.Src("WiseOwl.Cobol.Compiler"), "*.cs", System.IO.SearchOption.AllDirectories);
        var offenders = new System.Collections.Generic.List<string>();
        int callers = 0;
        foreach (string f in sources)
        {
            if (System.IO.Path.GetFileName(f) == "SignedIntegerLiteral.cs") continue;
            string src = System.IO.File.ReadAllText(f);
            callers += Regex.Matches(src, @"\bSignedIntegerLiteral\.Read\s*\(").Count;
            foreach (Match m in Regex.Matches(src, @"signedIntegerLiteral\s*\([^)]*\)[^;\r\n]*?\.GetText\s*\("))
                offenders.Add($"{System.IO.Path.GetFileName(f)}: {m.Value}");
        }
        // A ZERO-population "clean" result is the failure this suite exists to prevent: assert the screen is
        // actually CALLED before asserting nobody bypasses it (feedback green_test_can_hold_a_gap_open).
        Assert.True(callers > 0, "no caller of SignedIntegerLiteral.Read was found — the reader is dead and "
            + "the bypass assertion below would pass vacuously");
        Assert.Empty(offenders);
    }
}
