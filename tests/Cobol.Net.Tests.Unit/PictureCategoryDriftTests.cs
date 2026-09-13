// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Editions;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE TWO CLOSURE PROPERTIES OF <see cref="PictureAnalyzer"/>, MEASURED OVER THE WHOLE FORMAT-1 SYMBOL
/// ALPHABET RATHER THAN OVER A LIST OF CASES (kb/Work PB535).
/// <para>Every picture character-string the compiler ACCEPTS shall satisfy both:</para>
/// <list type="number">
///   <item><b>ISO §13.18.40.4 GR3 — it defines a category</b>, and the category the analyzer assigned is the one
///     whose defining general rule (GR5 alphabetic, GR6 alphanumeric, GR7 alphanumeric-edited, GR8 boolean,
///     GR9 national, GR10 national-edited, GR11 fixed-point numeric, GR13 numeric-edited) the character-string
///     actually satisfies. The defect this pins is the FALL-THROUGH: <c>Analyze</c> reaches "pure numeric" by
///     exhaustion, so before the composition validator existed <c>PIC S</c>, <c>PIC PPP</c>, <c>PIC SV</c> and
///     <c>PIC SPPP</c> each bound a ZERO-LENGTH category-numeric item at every edition, silently.</item>
///   <item><b>ISO §13.18.40.4 GR4 — its size is the symbol count</b>: "the number of symbols in character-string-1
///     that represent either boolean positions or character positions". GR14 names the three symbols that
///     represent none — 'P' ("not counted in the size of the item"), 'S' (counted "only when the subject of the
///     entry is described with a SIGN clause with the SEPARATE phrase") and 'V' ("not counted in the size of the
///     item") — so with no SIGN SEPARATE and a one-character currency string the size is exactly the count of
///     symbols outside {'P','S','V'}, for EVERY category. The defect this pins is a category arm spelling that
///     count as its own whitelist: the alphanumeric arm counted <c>X A 9</c> plus the insertion symbols, so any
///     symbol outside that list vanished from the size instead of the picture being rejected — <c>PIC XX,XX</c>
///     bound LENGTH 4, <c>PIC XXCR</c> bound 2 — and an item SHORTER than its picture shifts every following
///     member of a group image.</item>
/// </list>
/// <para>⛔ Both are stated as properties of the ACCEPTED set, which is what makes the next symbol automatic: a
/// prohibition relaxed in <see cref="PictureComposition"/>, or a category arm widened here, changes the accepted
/// set and is measured against the standard's own rules rather than against a list someone remembered to
/// extend. Rejection is not asserted — that is <see cref="PictureCompositionTests"/>'s job; this says only that
/// whatever survives is well-defined.</para>
/// </summary>
public sealed class PictureCategoryDriftTests
{
    /// <summary>The Format-1 picture symbols of ISO §13.18.40.3 SR2 / §13.18.40.6 Table 10, one representative of
    /// each row: the two-character symbols 'CR' and 'DB' enter through <see cref="LongerShapes"/> because an
    /// enumeration over single characters cannot spell them.</summary>
    private const string Alphabet = "AXN91ZP SV0B/,.+-*$";

    /// <summary>Shapes an exhaustive 1..3 walk cannot reach: the two-character editing sign control symbols, the
    /// floating strings (§13.18.40.5 rule 6 needs two adjacent occurrences), the repeat form, the currency-string
    /// widening of GR14 and the floating-point form of GR13 b.</summary>
    private static readonly string[] LongerShapes =
    [
        "999CR", "999DB", "$$,$$9.99", "++++9", "----9", "**,**9.99", "ZZ,ZZ9.99-",
        "X(12)", "9(18)", "S9(9)V9(9)", "PPP999", "999PPP", "N(4)", "1(8)", "AAAABAAAA",
        "XXBXX/XX", "NNBNN", "9(5)E+99", "ZZZ.ZZ", "$999.99CR", "+$$$,$$9.99", "9,999.99",
    ];

    private static (PicInfo Pic, string[] Diagnostics) Analyze(string picture, bool blankWhenZero = false)
    {
        var ed = new EditionContext(2023);
        PicInfo pic = PictureAnalyzer.Analyze(picture, Usage.Display, ed, "data item 'T'",
            blankWhenZero: blankWhenZero);
        return (pic, ed.Diagnostics.ToArray());
    }

    /// <summary>Every accepted character-string of one, two or three symbols, plus the shapes that need more.
    /// 19^1 + 19^2 + 19^3 = 7 239 strings walked; the accepted ones are what the two properties bind.</summary>
    public static IEnumerable<string> EveryShortPicture()
    {
        foreach (char a in Alphabet)
        {
            if (a == ' ') continue;
            yield return a.ToString();
            foreach (char b in Alphabet)
            {
                if (b == ' ') continue;
                yield return string.Concat(a, b);
                foreach (char c in Alphabet)
                {
                    if (c == ' ') continue;
                    yield return string.Concat(a, b, c);
                }
            }
        }
    }

    /// <summary>⛔ THE POPULATION GUARD. Both properties below are stated over the ACCEPTED set and say nothing
    /// about the rejected one, so a walk that accepted nothing — <c>Analyze</c> throwing, the alphabet
    /// mistyped, the composition validator widened into a blanket refusal — would pass both of them while
    /// measuring no picture at all. This asserts what the walk SAW: every one of ISO 13.18.40.4 GR3's eight
    /// categories is represented among the accepted strings, and the accepted count is in the thousands.</summary>
    [Fact]
    public void TheWalk_AcceptsEveryOneOfTheEightCategories()
    {
        var seen = new SortedDictionary<string, int>();
        int accepted = 0;
        foreach (string picture in EveryPicture())
        {
            var (pic, diags) = Analyze(picture);
            if (diags.Length > 0) continue;
            accepted++;
            seen[CategoryName(pic)] = seen.TryGetValue(CategoryName(pic), out int n) ? n + 1 : 1;
        }
        string histogram = string.Join(", ", seen.Select(kv => $"{kv.Key}={kv.Value}"));
        Assert.True(accepted >= 1000, $"the walk accepted only {accepted} picture character-strings ({histogram})");
        foreach (string category in EightCategories)
            Assert.True(seen.ContainsKey(category),
                $"no accepted picture character-string bound category {category}: {histogram}");
    }

    /// <summary>ISO 13.18.40.4 GR3's eight categories, named as <see cref="CategoryName"/> spells them.</summary>
    private static readonly string[] EightCategories =
    [
        "alphabetic", "alphanumeric", "alphanumeric-edited", "boolean",
        "national", "national-edited", "numeric", "numeric-edited",
    ];

    private static string CategoryName(PicInfo pic) => pic.Category switch
    {
        PicCategory.Alphanumeric when pic.IsAlphabetic => "alphabetic",
        PicCategory.Alphanumeric when pic.EditMask is not null => "alphanumeric-edited",
        PicCategory.Alphanumeric => "alphanumeric",
        PicCategory.National when pic.EditMask is not null => "national-edited",
        PicCategory.National => "national",
        PicCategory.Boolean => "boolean",
        PicCategory.Numeric => "numeric",
        PicCategory.NumericEdited => "numeric-edited",
        _ => pic.Category.ToString(),
    };

    /// <summary>The whole walk: every short character-string plus the shapes three symbols cannot spell.</summary>
    private static IEnumerable<string> EveryPicture() => EveryShortPicture().Concat(LongerShapes);

    [Fact]
    public void EveryAcceptedPicture_DefinesTheCategoryItsGeneralRuleDefines()
    {
        var wrong = new List<string>();
        foreach (string picture in EveryPicture())
        {
            var (pic, diags) = Analyze(picture);
            if (diags.Length > 0) continue;               // rejected, or staged — not part of the accepted set
            string? why = CategoryAntecedent(pic, PictureAnalyzer.ExpandRepeats(picture));
            if (why is not null) wrong.Add($"PIC {picture} → {Describe(pic)}: {why}");
        }
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} accepted picture character-string(s) bind a category whose defining general rule "
            + $"(ISO §13.18.40.4 GR5-GR13) they do not satisfy:{Environment.NewLine}"
            + string.Join(Environment.NewLine, wrong.Take(40)));
    }

    [Fact]
    public void EveryAcceptedPicture_HasTheSizeGeneralRule4Gives()
    {
        var wrong = new List<string>();
        foreach (string picture in EveryPicture())
        {
            var (pic, diags) = Analyze(picture);
            if (diags.Length > 0) continue;
            string expanded = PictureAnalyzer.ExpandRepeats(picture);
            // §13.18.40.4 GR4 with GR14: every symbol represents a character position except 'P', 'V', and 'S'
            // without SIGN SEPARATE. The currency string is the default one character here, so GR14's
            // "the first occurrence adds the number of characters in the currency string" adds nothing.
            int expect = expanded.Count(c => c is not ('P' or 'S' or 'V'));
            if (pic.Length != expect)
                wrong.Add($"PIC {picture} → {Describe(pic)}: size {pic.Length}, GR4 gives {expect}");
        }
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} accepted picture character-string(s) have a size other than ISO §13.18.40.4 GR4's "
            + $"count of the symbols representing a character position:{Environment.NewLine}"
            + string.Join(Environment.NewLine, wrong.Take(40)));
    }

    private static string Describe(PicInfo pic) =>
        $"{CategoryName(pic)} len {pic.Length} digits {pic.Digits}";

    /// <summary>The defining general rule of the category the analyzer assigned, asked of the character-string:
    /// null when it holds, else the sentence it fails. The EDITING character-1 set is empty here (no PICTURE
    /// EDITING phrase is passed), so the GR7 / GR10 insertion set is exactly 'B', '0', '/'.</summary>
    private static string? CategoryAntecedent(PicInfo pic, string s)
    {
        bool Insertion(char c) => c is 'B' or '0' or '/';
        switch (pic.Category)
        {
            case PicCategory.Alphanumeric when pic.IsAlphabetic:
                // GR5 — "To define an item as alphabetic, character-string-1 shall contain only one or more
                // occurrences of the symbol 'A'."
                return s.All(c => c == 'A') ? null : "GR5 admits only the symbol 'A' for category alphabetic";
            case PicCategory.Alphanumeric when pic.EditMask is not null:
                // GR7 — at least one 'A' or 'X', AND at least one instance of character-1 or one of 'B', '0', '/'.
                return s.Any(c => c is 'A' or 'X') && s.Any(Insertion)
                    ? null
                    : "GR7 needs at least one 'A' or 'X' and at least one of 'B', '0', '/'";
            case PicCategory.Alphanumeric:
                // GR6 — a combination of symbols from the set 'A', 'X', '9' including at least one 'X' or at
                // least two different symbols from that set.
                if (!s.All(c => c is 'A' or 'X' or '9'))
                    return "GR6 admits only the symbols 'A', 'X' and '9' for category alphanumeric";
                return s.Contains('X') || s.Distinct().Count() >= 2
                    ? null
                    : "GR6 needs at least one 'X' or at least two different symbols from 'A', 'X', '9'";
            case PicCategory.Boolean:
                // GR8 — only one or more occurrences of the symbol '1'.
                return s.All(c => c == '1') ? null : "GR8 admits only the symbol '1' for category boolean";
            case PicCategory.National when pic.EditMask is not null:
                // GR10 — at least one 'N', and at least one instance of character-1 or one of 'B', '0', '/'.
                return s.Contains('N') && s.Any(Insertion)
                    ? null
                    : "GR10 needs at least one 'N' and at least one of 'B', '0', '/'";
            case PicCategory.National:
                // GR9 — only one or more occurrences of the symbol 'N'.
                return s.All(c => c == 'N') ? null : "GR9 admits only the symbol 'N' for category national";
            case PicCategory.Numeric:
                // GR11 — at least one symbol '9', and a combination of symbols from the set 'P', 'S', 'V'.
                if (!s.Contains('9')) return "GR11 needs at least one symbol '9' for category numeric";
                return s.All(c => c is '9' or 'P' or 'S' or 'V')
                    ? null
                    : "GR11 admits beside the '9's only the symbols 'P', 'S' and 'V'";
            case PicCategory.NumericEdited:
                // GR13 a — at least one 'Z'; or at least one '*'; or at least two identical symbols from
                // '+', '-', currency symbol; or at least one '9' and at least one instance of character-1 or of
                // 'B', 'CR', 'DB', '0', '/', ',', '.', '+', '-', the currency symbol. GR13 b is the 'E' form.
                // A BLANK WHEN ZERO clause defines the item as numeric-edited too (GR3's closing sentence), but
                // this walk passes no such clause.
                if (s.Contains('E')) return null;
                if (s.Contains('Z') || s.Contains('*')) return null;
                if ("+-$".Any(c => s.Count(x => x == c) >= 2)) return null;
                return s.Contains('9') && s.Any(c => Insertion(c) || c is ',' or '.' or '+' or '-' or '$'
                                                     || c is 'C' or 'R' or 'D' or 'B')
                    ? null
                    : "GR13 a defines no numeric-edited item for this character-string";
            default:
                return $"category {pic.Category} is not one of ISO §13.18.40.4 GR3's eight";
        }
    }
}
