// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using System.Collections.Generic;
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE SIMPLE-INSERTION DRIFT PIN. ISO §13.18.40.5 rule 3 names the simple insertion editing symbols once —
/// "the symbols 'B', '0', '/', ',' and, if literal=1 is specified, character-1 are used as the simple insertion
/// editing symbols" — and rules 6 and 7 both spend that set again, in identical words: "Any of the simple
/// insertion editing symbols embedded in this string or to the immediate right of this string are part of the
/// string". Four places in <see cref="CobolEdit"/> consume it (pass 1 places the insertion character, pass 2
/// replaces it inside a zero-suppression string, <c>FindFloatingPlacement</c> lands the floating character on it,
/// and <c>FormatAlphanumeric</c> inserts it), and while each spelled the set out for itself two of the copies had
/// drifted to the pair {',', 'B'} — so <c>PIC ZZ/ZZ</c> rendered "  /12" for 12 and <c>PIC ++/++9</c> put its
/// sign four positions early (kb/Work PB490).
/// <para>These facts DERIVE their masks from <see cref="CobolEdit.SimpleInsertionSymbols"/> plus an IS-form
/// PICTURE EDITING character-1, so a symbol added to the set that a consumer has not been taught FAILS here, and
/// a consumer that narrows its own copy of the set fails too. They are not a sample of the four symbols; they are
/// the set, whatever it becomes.</para>
/// </summary>
public class CobolEditSimpleInsertionDriftTests
{
    /// <summary>An IS-form PICTURE EDITING phrase (§13.18.40.3 SR12 "If literal-1 is specified") — rule 3's
    /// fifth simple insertion symbol, which is why it is enumerated beside the four fixed ones.</summary>
    private static readonly CobolEdit.EditRule[] IsForm = [new('T', ':', ':', SimpleInsertion: true)];

    /// <summary>Every simple insertion symbol of a picture, with the character it inserts (§13.18.40.4 GR14:
    /// 'B' the space, '0' the zero, '/' the slant, ',' the comma; rule 3: character-1 inserts literal-1) and the
    /// rules that declare it. The four fixed symbols come from the runtime's own set — never a literal list.</summary>
    public static IEnumerable<object[]> Symbols()
    {
        // NOT an iterator: a ReadOnlySpan local cannot live in one, and reading the runtime's own set is the
        // whole point of this file.
        var rows = new List<object[]>();
        foreach (char s in CobolEdit.SimpleInsertionSymbols) rows.Add([s, s == 'B' ? ' ' : s, false]);
        rows.Add(['T', ':', true]);   // an IS-form character-1
        return rows;
    }

    [Theory]
    [MemberData(nameof(Symbols))]
    public void Pass1_PlacesTheInsertionCharacterAtTheSymbolsOwnPosition(char symbol, char inserted, bool editing)
    {
        // Rule 3: "Simple insertion editing results in the insertion character occupying the same character
        // position in the edited item as the associated symbol occupies in character-string-1." No suppression
        // string is present, so the symbol is nothing but its insertion.
        string image = CobolEdit.Format(1245, 0, $"99{symbol}99", edits: editing ? IsForm : null);
        Assert.Equal($"12{inserted}45", image);
    }

    [Theory]
    [MemberData(nameof(Symbols))]
    public void Pass2_ReplacesTheSymbolEmbeddedInAZeroSuppressionString(char symbol, char inserted, bool editing)
    {
        _ = inserted;
        // Rule 7: the symbol embedded in the 'Z' string is part of the string, and rule 7 a) puts the replacement
        // character "into any character position immediately preceding … the first nonzero numeric character in
        // the item" — every position left of the '1' of 12, the embedded insertion included.
        Assert.Equal("   12", CobolEdit.Format(12, 0, $"ZZ{symbol}ZZ", edits: editing ? IsForm : null));
        // The '*' twin replaces with the asterisk instead ("if the symbol '*' is used, the replacement character
        // is the character asterisk").
        Assert.Equal("***12", CobolEdit.Format(12, 0, $"**{symbol}**", edits: editing ? IsForm : null));
    }

    [Theory]
    [MemberData(nameof(Symbols))]
    public void FloatingPlacement_LandsOnTheSymbolEmbeddedInAFloatingString(char symbol, char inserted, bool editing)
    {
        _ = inserted;
        // Rule 6: the embedded symbol is part of the floating string; rule 6 a) lands "a single occurrence of the
        // replacement character(s) … immediately preceding … the first nonzero numeric character in the item" —
        // index 4 for the value 5 — and "Any character positions preceding this (these) insertion character(s)
        // will contain the space character".
        Assert.Equal("    +5", CobolEdit.Format(5, 0, $"++{symbol}++9", edits: editing ? IsForm : null));
        Assert.Equal("    -5", CobolEdit.Format(-5, 0, $"++{symbol}++9", edits: editing ? IsForm : null));
        // 123 stops the walk one position earlier: the sign lands ON the embedded insertion position.
        Assert.Equal("  +123", CobolEdit.Format(123, 0, $"++{symbol}++9", edits: editing ? IsForm : null));
        Assert.Equal("  $123", CobolEdit.Format(123, 0, $"$${symbol}$$9", edits: editing ? IsForm : null));
    }

    [Theory]
    [MemberData(nameof(Symbols))]
    public void Alphanumeric_InsertsTheSameCharacterTheNumericPathDoes(char symbol, char inserted, bool editing)
    {
        // §13.18.40.5 Table 7 gives category alphanumeric-edited SIMPLE INSERTION, and §13.18.40.4 GR7 admits
        // character-1 as one of its constituents — the same set, so the same inserted character.
        string image = CobolEdit.FormatAlphanumeric("ABCD", $"XX{symbol}XX", editing ? IsForm : null);
        Assert.Equal($"AB{inserted}CD", image);
    }

    [Fact]
    public void TheSetIsRule3s_AndNothingElseIsSimpleInsertion()
    {
        // Rule 3's four fixed symbols, and no other PICTURE symbol: the currency symbol and '+ - CR DB' are FIXED
        // insertion (rule 5), '.' is SPECIAL insertion (rule 4), 'Z' and '*' are suppression symbols (rule 7),
        // and the rest are digit / character / scaling positions (§13.18.40.4 GR14).
        foreach (char s in "B0/,") Assert.True(CobolEdit.IsSimpleInsertionSymbol(s), $"'{s}' is a rule-3 symbol");
        foreach (char s in "9ZAXSVPEN1CD$+-*.") Assert.False(CobolEdit.IsSimpleInsertionSymbol(s), $"'{s}' is not");
        Assert.Equal(4, CobolEdit.SimpleInsertionSymbols.Length);
    }

    [Fact]
    public void TheSeparatorPairFollowsWhicheverCharacterIsTheGroupingSeparator()
    {
        // §13.18.40.2 SR13: "The rules for the symbol period apply to the symbol comma, and the rules for the
        // symbol comma apply to the symbol period." Whichever character is the GROUPING separator is the simple
        // insertion symbol; the other is the decimal point, whose editing is special insertion (rule 4).
        Assert.True(CobolEdit.IsSimpleInsertionSymbol(',', grouping: ','));
        Assert.False(CobolEdit.IsSimpleInsertionSymbol('.', grouping: ','));
        Assert.True(CobolEdit.IsSimpleInsertionSymbol('.', grouping: '.'));
        Assert.False(CobolEdit.IsSimpleInsertionSymbol(',', grouping: '.'));
    }

    [Fact]
    public void ASimpleInsertionSymbolLeftOfTheStringIsNotPartOfIt()
    {
        // Rules 6 and 7 say EMBEDDED or "to the immediate right" — never to the left. §13.18.40.6 Table 10 admits
        // 'B 0 /' and ',' before a 'Z' (both columns carry an 'x' against the left-of-point 'Z *' row), and
        // §13.18.40.4 GR15 confirms the render from the VALIDATE side: "the character zero if the symbol '0' is
        // neither part of floating insertion editing nor of zero suppression with replacement editing".
        Assert.Equal("0  1", CobolEdit.Format(1, 0, "0ZZ9"));
        Assert.Equal(",  1", CobolEdit.Format(1, 0, ",ZZ9"));
        Assert.Equal("/  1", CobolEdit.Format(1, 0, "/ZZ9"));
        Assert.Equal("   1", CobolEdit.Format(1, 0, "BZZ9"));   // 'B' inserts a space either way
    }

    [Fact]
    public void AForPhraseCharacter1IsFixedInsertionAndSurvivesTheSuppressionWalk()
    {
        // Rule 5: "Character-1, the currency symbol and the editing sign control symbols '+', '-', 'CR' and 'DB'
        // are used as the fixed insertion editing symbols. … When character-1 is used, and is not a simple
        // insertion character, it represents literal-2, or literal-3 as the insertion characters." A FOR phrase
        // is that form (§13.18.40.3 SR12), so it is NOT part of the 'Z' string beside it — and the distinction
        // is not readable off the rendered characters: this phrase renders ':' for either sign, exactly like the
        // IS form, and only the FORM separates them.
        CobolEdit.EditRule[] forForm = [new('U', ':', ':', SimpleInsertion: false)];
        Assert.Equal(" :5", CobolEdit.Format(5, 0, "ZU9", edits: forForm));
        Assert.Equal("  5", CobolEdit.Format(5, 0, "ZT9", edits: IsForm));
    }
}
