// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A §2023:15.3 FORMAT LITERAL IS PARSED ONCE, NOT AT EVERY EVALUATION (kb/Work PB2637). §2023:15.40.3 AR1 and its six siblings
/// make argument-1 a literal, so <c>CobolDate</c> reads every format through <c>FormatOf</c>, which parses a literal on its first
/// use and holds the result; the emitter and the analyzer used to call the tokenizer themselves on every call, and the emitter
/// allocated a string per field. This class refuses a call of the tokenizer from anywhere but <c>FormatOf</c>, and a per-field
/// string in the emitter.
/// </summary>
public sealed class DateFormatParseOnceDriftTests
{
    private const string Home = "src/WiseOwl.Cobol.Runtime/Intrinsics/CobolDate.cs";

    [Fact]
    public void TheTokenizer_IsCalledOnlyByFormatOf()
    {
        string[] lines = File.ReadAllLines(TestRepo.Within("src/WiseOwl.Cobol.Runtime/", Home));
        var calls = lines.Select((l, i) => (Line: l, Number: i + 1))
            .Where(x => !x.Line.TrimStart().StartsWith("//", StringComparison.Ordinal)
                        && Regex.IsMatch(x.Line, @"\bTokenize\b(?!\s*\(string)"))
            .ToList();
        Assert.True(calls.Count >= 1, "the sweep found no use of Tokenize: the detector or the tokenizer's name moved");
        var outside = calls.Where(c => !c.Line.Contains("FormatOf", StringComparison.Ordinal)
                                       && !Regex.IsMatch(c.Line, @"GetOrAdd\(format, Tokenize\)|:\s*Tokenize\(format\);"))
            .Select(c => $"{Home}:{c.Number}: {c.Line.Trim()}").ToList();
        Assert.True(outside.Count == 0,
            "a date/time format is tokenized outside CobolDate.FormatOf, so it is parsed again at every call (kb/Work PB2637):\n"
            + string.Join("\n", outside));
    }

    [Fact]
    public void TheFormatEngine_BuildsNoStringPerField()
    {
        string text = File.ReadAllText(TestRepo.Within("src/WiseOwl.Cobol.Runtime/", Home));
        Assert.DoesNotContain("new StringBuilder", text, StringComparison.Ordinal);
        Assert.DoesNotContain(".ToString(\"00\")", text, StringComparison.Ordinal);
    }
}
