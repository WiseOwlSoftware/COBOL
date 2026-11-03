// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A COBOL-85 COMMENT-ENTRY CANDIDATE THE LOGICAL CONVERSION CANNOT PLACE IS DECIDED AFTER EVERY STAGE THAT CAN CHANGE
/// WHAT THE DIVISION IS (docs/CONFORMANCE.md D-CE2; kb/Work PB4281). §6.5 converts "before the application of replacing and
/// conditional compilation actions", so the conversion carries what it cannot place (<see cref="CommentEntryCarrier"/>) and
/// <see cref="CommentEntryResolver"/> decides it on the finished text, reading the words through the group's
/// <c>&gt;&gt;COBOL-WORDS</c> map. The order is a fact of <c>Frontend.Preprocess</c> that no behavioral test can see failing
/// until a stage is added or moved in a way the goldens do not cover: the code compiles and the shapes the corpus holds
/// still pass. These guard the source form: where the resolver runs, who may make a carrier and who may read one, and that
/// the stages between see a carrier as the comment it is.
/// </summary>
[Trait("Sweep", "tree")]
public sealed class CommentEntryResolutionOrderDriftTests
{
    private static string Strip(string code)
        => string.Join('\n', code.Split('\n').Where(l => !l.TrimStart().StartsWith("//", StringComparison.Ordinal)));

    private static string Frontend() => Strip(File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Frontend", "Pipeline", "Frontend.cs")));

    [Fact] // after the merged driver (conditional compilation, COPY, REPLACE) and the COBOL-WORDS stage, before the lexer reads the text
    public void ThePreprocessPipeline_ResolvesCommentEntries_AfterEveryStageThatChangesTheDivision()
    {
        string src = Frontend();
        int manipulate = src.IndexOf("ConditionalCompilationProcessor.Manipulate(", StringComparison.Ordinal);
        int words = src.IndexOf("CobolWordsDirectiveProcessor.Process(", StringComparison.Ordinal);
        int resolve = src.IndexOf("CommentEntryResolver.Resolve(", StringComparison.Ordinal);
        int endOfPreprocess = src.IndexOf("return (new MappedText(text, lineOrigins)", StringComparison.Ordinal);
        Assert.True(manipulate >= 0 && words > manipulate && resolve > words && endOfPreprocess > resolve,
            $"Frontend.Preprocess must run, in this order: ConditionalCompilationProcessor.Manipulate ({manipulate}) -> "
            + $"CobolWordsDirectiveProcessor.Process ({words}) -> CommentEntryResolver.Resolve ({resolve}) -> the end of Preprocess "
            + $"({endOfPreprocess}), which hands the text the parse reads. A comment-entry candidate is decided on the finished text "
            + "and the group's word map (docs/CONFORMANCE.md D-CE2, kb/Work PB4281).");
        Assert.Contains("text = resolved.Text.Text;", src);   // the text the parse reads is the resolver's
        Assert.Equal(src.IndexOf("CommentEntryResolver.Resolve(", StringComparison.Ordinal),
            src.LastIndexOf("CommentEntryResolver.Resolve(", StringComparison.Ordinal));   // once
        Assert.Matches(@"CommentEntryResolver\.Resolve\([^;]*\bcobolWordsMap\b", src);   // through the map the COBOL-WORDS stage built
    }

    [Fact] // a candidate that proves to be program text was hidden from the stages that must read it: they run again, to a fixed point
    public void TheTextStages_RunAgainWithEveryCandidateThatProvedToBeProgramText_UntilNoneIsNew()
    {
        string src = Frontend();
        int normalize = src.IndexOf("Normalize(raw, sourcePath, referenceFormatGates, formatOps, pins)", StringComparison.Ordinal);
        int preprocess = src.IndexOf("Preprocess(normalized, referenceFormats,", StringComparison.Ordinal);
        int grow = src.IndexOf("pins.With(programText)", StringComparison.Ordinal);
        int stop = src.IndexOf("ReferenceEquals(grown, pins)", StringComparison.Ordinal);
        Assert.True(normalize >= 0 && preprocess > normalize && grow > preprocess && stop > grow,
            "Frontend.RunTextStages must run Normalize -> Preprocess -> pins.With(programText) -> stop when the pins did not grow "
            + "(docs/CONFORMANCE.md D-CE2, kb/Work PB4281): a program-text candidate the resolver names is read as program text by "
            + "the run that follows, so conditional compilation, COPY and REPLACE see its lines.");
        Assert.Contains("RunTextStages(raw, sourcePath, implicitOps, formatOps)", src);   // the GR14 pass loop runs the iteration, never the bare stages
        Assert.Equal(1, System.Text.RegularExpressions.Regex.Count(src, @"\bNormalize\(raw,"));   // the iteration is Normalize's one caller
        Assert.Contains("commentEntryPins: pins", src);   // library text reads the pins too
        Assert.Contains("pins: pins", src);               // … and so does the main source
    }

    [Fact] // a pin is read by the converters only (never decided anywhere else) and made by Frontend only
    public void PinsAreMadeByFrontend_AndReadByTheConvertersOnly()
    {
        var made = new List<string>();
        var read = new List<string>();
        foreach (string file in Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Frontend"), "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)) continue;
            string name = Path.GetFileName(file), code = Strip(File.ReadAllText(file));
            if (code.Contains(".With(programText)", StringComparison.Ordinal)) made.Add(name);
            if (code.Contains("pins?.Contains(", StringComparison.Ordinal)) read.Add(name);
        }
        Assert.Equal(["Frontend.cs"], made);
        Assert.Equal(["ReferenceFormatProcessor.LogicalConversion.cs"], read);
    }

    [Fact] // the carrier is made in the two converters only and read in the resolver only; the resolver is the only reader given a word map
    public void CarriersAreMadeByTheConverters_AndReadByTheResolver_OnlyOnce()
    {
        var made = new List<string>();
        var read = new List<string>();
        var mapped = new List<string>();
        foreach (string file in Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Frontend"), "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)) continue;
            string name = Path.GetFileName(file), code = Strip(File.ReadAllText(file));
            if (code.Contains("CommentEntryCarrier.Encode(", StringComparison.Ordinal)) made.Add(name);
            if (code.Contains("CommentEntryCarrier.Decode(", StringComparison.Ordinal)) read.Add(name);
            if (name != "DivisionCursor.cs" && System.Text.RegularExpressions.Regex.IsMatch(code, @"new DivisionCursor\([^)]*,\s*\w+\)"))
                mapped.Add(name);
        }
        Assert.Equal(["ReferenceFormatProcessor.LogicalConversion.cs"], made);
        Assert.Equal(["CommentEntryResolver.cs"], read);
        Assert.Equal(["CommentEntryResolver.cs"], mapped);   // only the resolver reads the words through a map
    }

    [Fact] // a carrier is a comment to every stage between the conversion and the resolver: no text-word, no directive, no COPY
    public void ACarrier_IsInertToTheTextStages()
    {
        var physical = new PhysicalLine(7, "       COPY BOOK. >>IF X IS DEFINED ==", null);
        string carrier = CommentEntryCarrier.Encode(isHeader: false, isFixed: true, ccvsIndicators: false, physical);
        Assert.Empty(TextWordScanner.Scan(carrier));
        Assert.False(ReferenceFormatProcessor.HoldsNoProgramText(carrier));   // it is a line of text, to be decided, not a blank one
        Assert.False(carrier.TrimStart().StartsWith(">>", StringComparison.Ordinal));
    }
}
