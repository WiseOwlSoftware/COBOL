// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Diagnostics;
using WiseOwl.Cobol.Frontend.Preprocessor;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE FREE-FORM LINES OF AN EXPANSION'S TEXT (ISO §6.4.2 "Continuation of lines"; DESIGN-external-repository §10.5; kb/Work PB4456).
/// §6.4.2: "At least one alphanumeric character, national character, or hexadecimal digit of the literal content shall be specified on
/// the continued line and on each continuation line", and "A pair of quotation symbols indicating a single quotation symbol within a
/// literal shall be specified on the same line". The writer (<see cref="ExpansionAssemblies.FreeForm"/>) must therefore leave no continued
/// line without content at ANY column the literal opens at, for every kind of literal and every first content unit. The sweep puts the
/// literal at every column a line can hold, so every split position (the opening line's, a continuation line's, a doubled quotation
/// symbol straddling the fill point) occurs, and reads each result back through the reference-format stage that joins the lines.
/// </summary>
public sealed class ExpansionFreeFormTests
{
    /// <summary>One literal kind: the text before its body, the body (content units, doubled delimiters included) and the delimiter.</summary>
    public sealed record Kind(string Open, string Body, char Delimiter)
    {
        public string Written => Open + Body + Delimiter;
        public override string ToString() => Written.Length > 24 ? Written[..24] + "…" : Written;
    }

    private static string Repeat(string unit, int times) => string.Concat(Enumerable.Repeat(unit, times));

    /// <summary>The kinds the sweep covers: alphanumeric, national, boolean and the three hexadecimal forms (ISO §8.3.3.2, §8.3.3.4,
    /// §8.3.3.5), each in the quotation mark and the apostrophe where it has both, with a doubled delimiter first, in the middle and with
    /// a one-character first unit.</summary>
    public static TheoryData<Kind> Kinds()
    {
        var kinds = new TheoryData<Kind>();
        // Q stands for the literal's delimiter: a doubled one (QQ) is one content unit, two positions, and must stay on one line.
        string[] text = ["a", "QQ", "ab", "QQa", "aQQb", "QQQQ", "QQ" + Repeat("ab QQc", 40), Repeat("a QQbc d", 40)];
        foreach (char delimiter in (char[])['"', '\''])
            foreach (string open in (string[])["", "N", "n"])   // alphanumeric, national (the prefix letters are case-insensitive)
                foreach (string body in text)
                    kinds.Add(new Kind(open + delimiter, body.Replace('Q', delimiter), delimiter));
        // Boolean and hexadecimal digits: one unit each, and never a doubled delimiter.
        (string Open, string[] Bodies)[] digits =
        [
            ("B", ["1", "10", "101", Repeat("10", 120)]), ("X", ["4", "41", "414", Repeat("41", 120)]),
            ("BX", ["A", "A5", "A5A", Repeat("A5", 120)]), ("NX", ["0", "0041", Repeat("0041", 60)]),
        ];
        foreach (var (open, bodies) in digits)
            foreach (char delimiter in (char[])['"', '\''])
                foreach (string body in bodies)
                    kinds.Add(new Kind(open + delimiter, body, delimiter));
        return kinds;
    }

    [Theory]   // §6.4.2: content on the continued line and on each continuation line, for a literal opened at every column
    [MemberData(nameof(Kinds))]
    public void ALiteralOpenedAtEveryColumn_IsContinuedOnlyWithContentOnEveryLine(Kind kind)
    {
        for (int fill = 1; fill <= 199; fill++)
        {
            string text = $"{new string('W', fill)} {kind.Written} END.";
            string lines = ExpansionAssemblies.FreeForm(text);
            string where = $"{kind} after a {fill}-position word";

            Assert.All(lines.Split('\n'), line => Assert.True(line.Length <= 200, $"{where}: a line of {line.Length} positions"));

            // The reference-format stage joins a continued literal; it reports a continued or continuation line with no content
            // (COBOL2690, §6.4.2) and a continuation line that does not open with the matching quotation symbol (COBOL2684).
            var bag = new DiagnosticBag();
            string joined = ReferenceFormatProcessor.NormalizeToFreeForm(lines + "\n", new ReferenceFormatDiagnostics(2023, false, bag),
                "expansion.cob", InitialReferenceFormat.Free);
            Assert.True(bag.Diagnostics.Count == 0, $"{where}: {string.Join("; ", bag.Diagnostics)}");

            // …and the join reads back as the text the writer was given.
            Assert.True(text == joined.Replace('\n', ' ').TrimEnd(), $"{where}: the lines do not read back as the text");
        }
    }

    [Fact]   // The reported repro: a short literal that fits the line whole, opened where its FIRST unit does not fit before the "- reserve.
    public void ALiteralWhoseFirstUnitDoesNotFitItsOpeningLine_StartsALineOfItsOwn()
    {
        foreach (var (open, body, delimiter, delimiterAt) in (ReadOnlySpan<(string, string, char, int)>)
                 [("\"", "\"\"", '"', 197), ("N\"", "\"\"", '"', 197), ("'", "''", '\'', 197), ("N'", "''", '\'', 197),
                  ("\"", "A", '"', 198), ("N\"", "A", '"', 198)])
        {
            int before = delimiterAt - open.Length;   // the positions that precede the literal's first character: the word and a space
            string text = $"{new string('W', before - 1)} {open}{body}{delimiter}";
            string[] lines = ExpansionAssemblies.FreeForm(text).Split('\n');
            string which = $"{open}{body}{delimiter} opened at {delimiterAt}";
            Assert.True(lines.Length == 2, $"{which}: {lines.Length} lines");
            Assert.Equal($"{open}{body}{delimiter}", lines[1]);   // the whole literal, on a line of its own, uncontinued
        }
    }
}
