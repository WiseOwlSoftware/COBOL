// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using WiseOwl.Cobol;
using WiseOwl.Cobol.Frontend;
using WiseOwl.Cobol.Frontend.Diagnostics;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Listing;

/// <summary>
/// ⛔ EVERY COMPILED STATEMENT HAS ITS SEQUENCE POINT, AND NOTHING ELSE HAS ONE (DESIGN-listing.md section 6.9). The PDB the backend writes
/// beside the assembly holds a visible sequence point at the place each COBOL statement is written, which is what a .NET debugger stops at
/// and what the listing's object-code map reads; every other line of the backend's code is hidden. The test holds that by readings none of
/// which is the backend's or the listing's: the statements are the ones the PARSE built (each <c>statement</c> node of the tree the front
/// end the driver builds returns, <see cref="CompilerDriver.NewFrontend"/>), placed through the parse's own line map; the sequence points
/// are read from the PDB here with <c>System.Reflection.Metadata</c>. It runs against the backend the driver compiles with, the only one
/// the backend factory offers (<c>BackendFactory.For</c>); a second backend joins the same corpus.
/// </summary>
/// <remarks>
/// <para><b>What compiles to no code.</b> A statement has no sequence point when it has no object code, for one of four reasons, each
/// exempt here and nothing else:</para>
/// <list type="bullet">
/// <item>the standard gives it nothing to execute: "The CONTINUE statement is a no-operation statement" (ISO §2023:14.9.9.1; its AFTER
/// phrase's pause has code, and is exempt with it), "The EXIT statement provides a common end point" (ISO §2023:14.9.14.1), "The USE
/// statement specifies" the procedures a condition runs (ISO §2023:14.9.49.1) and is no statement of the sequence, and NEXT SENTENCE
/// at a sentence's end transfers to where control goes anyway (every EXIT and NEXT SENTENCE is exempt, sound as below);</item>
/// <item>no execution reaches it: it follows, in its paragraph, a statement that transfers control (GO TO, STOP, EXIT, GOBACK, NEXT
/// SENTENCE). The rule is the flat order of the paragraph's statements, so a reached statement after a GO TO nested in an IF is exempt
/// too: sound (a reached statement left unasked is only a statement this check does not ask about), never a false red;</item>
/// <item>the compiler evaluated it: an IF, EVALUATE or inline PERFORM whose condition the backend compiled to a constant (the backend's
/// own source shows the condition as literals, or as <c>true ||</c> or <c>false &amp;&amp;</c> before the rest) has no code of its own,
/// and the statements after it in its paragraph are exempt with it;</item>
/// <item>it has nothing to do in this program: a MOVE, ADD or SUBTRACT CORRESPONDING whose groups have no corresponding items has no
/// pair to move. Every such statement is exempt (sound, as above), read by its own CORRESPONDING phrase (<see cref="Corresponding"/>),
/// never by a data name that holds CORR or by a nested statement's phrase.</item>
/// </list>
/// <para><b>The corpus</b> is every NIST CCVS program the golden run compiles and runs (a green or divergent row of
/// <c>tests/nist/corpus.tsv</c>), compiled as that run compiles it (its X-card name, <c>--std 85</c>): every verb of every module of
/// the 1985 standard, nested, copied and conditional.</para>
/// </remarks>
public sealed class SequencePointCoverageDriftTests : WiseOwlCobolTestBase
{
    private static readonly HashSet<string> NoCode = new(StringComparer.Ordinal) { "CONTINUE", "EXIT", "USE", "NEXT" };
    private static readonly HashSet<string> Transfers = new(StringComparer.Ordinal) { "GO", "STOP", "EXIT", "GOBACK", "NEXT" };

    // A span directive of the Roslyn backend's source and the condition on the line after it, when that line is an if or while statement.
    private static readonly Regex Header = new(
        @"^#line \((\d+), (\d+)\) - \(\d+, \d+\) \d+ ""[^""]*""\n\s*(?:if|while) \((.*)\)\s*$",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);

    /// <summary>Whether the C# compiler evaluates <paramref name="condition"/> when it compiles it: literals and operators alone, or a
    /// <c>true ||</c> or <c>false &amp;&amp;</c> that decides it whatever follows.</summary>
    private static bool Folded(string condition)
    {
        string bare = Regex.Replace(condition, @"[\s()]", "");
        if (bare.StartsWith("true||", StringComparison.Ordinal) || bare.StartsWith("false&&", StringComparison.Ordinal)) return true;
        return bare.Replace("true", "", StringComparison.Ordinal).Replace("false", "", StringComparison.Ordinal)
            .All(c => char.IsAsciiDigit(c) || "L.=!<>+-*/&|".Contains(c));
    }

    /// <summary>The statements of <paramref name="options"/>'s source that compile to code but have no sequence point at their position, and
    /// the visible sequence points at no statement's position.</summary>
    internal static (List<string> Missing, List<string> Stray) Coverage(CompilerDriver.Options options)
    {
        var frontend = CompilerDriver.NewFrontend(options, new CompilationInputs(), recordsListing: true);
        var tree = frontend.Parse(options.SourcePath, new DiagnosticBag());
        Assert.NotNull(tree);

        var result = CompilerDriver.Compile(options);
        Assert.True(result.Success, string.Join("\n", result.Errors));
        var points = SequencePoints(Path.ChangeExtension(options.OutputDll(), ".pdb"), SourceDocuments.RootOf(options.SourcePath));
        var folded = Header.Matches(File.ReadAllText(result.GeneratedCsPath!)).Where(m => Folded(m.Groups[3].Value))
            .Select(m => (int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value))).ToHashSet();

        var expected = new HashSet<(string, int, int)>();
        var missing = new List<string>();
        foreach (var paragraph in Paragraphs(tree!))
        {
            bool unreached = false;
            foreach (var statement in paragraph)
            {
                string verb = statement.Start.Text.ToUpperInvariant();
                var at = frontend.LineMap.Locate(statement.Start.Line, statement.Start.Column, options.SourcePath);
                var position = (Path.GetFullPath(at.FileName), at.Line + 1, at.Column + 1);
                expected.Add(position);
                bool constant = folded.Contains((position.Item2, position.Item3));
                bool corresponding = Corresponding(statement, verb);
                if (!unreached && !constant && !corresponding && !NoCode.Contains(verb) && !points.Contains(position))
                    missing.Add($"{verb} at {position}");
                unreached |= constant || Transfers.Contains(verb);
            }
        }
        var stray = points.Where(p => !expected.Contains(p)).Select(p => p.ToString()).ToList();
        return (missing, stray);
    }

    /// <summary>Whether <paramref name="statement"/> is itself a MOVE, ADD or SUBTRACT CORRESPONDING: its verb, then the CORRESPONDING
    /// phrase as the very next word, as the corresponding format of each writes it (MOVE's format 2, ADD's and SUBTRACT's format 3), whose
    /// items "are selected to be moved" (ISO §2023:14.9.25.4 GR11), "added" (ISO §2023:14.9.2.4 GR5) or "subtracted" (ISO
    /// §2023:14.9.44.4 GR5) pair by pair, so a pair of groups with no corresponding items has no code. Never a statement that only names a data item whose name holds CORR (the NIST programs' CORRECT-A), and
    /// never an IF, EVALUATE or PERFORM whose nested statement is one (kb/Work PB5925 review R1-2).</summary>
    internal static bool Corresponding(ParserRuleContext statement, string verb)
    {
        if (verb is not ("MOVE" or "ADD" or "SUBTRACT")) return false;
        var terminals = new List<ITerminalNode>(2);
        void Collect(IParseTree node)
        {
            if (terminals.Count == 2) return;
            if (node is ITerminalNode terminal) { if (terminal.Symbol.Type > 0) terminals.Add(terminal); return; }
            for (int i = 0; i < node.ChildCount && terminals.Count < 2; i++) Collect(node.GetChild(i));
        }
        Collect(statement);
        return terminals.Count == 2 && terminals[1].GetText().ToUpperInvariant() is "CORRESPONDING" or "CORR";
    }

    /// <summary>The statements of each paragraph of <paramref name="tree"/> in the order written, nested ones included: every
    /// <c>statement</c> node under each <c>paragraphDefinition</c>, and those of a procedure division with no paragraph.</summary>
    private static IEnumerable<List<ParserRuleContext>> Paragraphs(IParseTree tree)
    {
        var paragraphs = new List<List<ParserRuleContext>>();
        var outside = new List<ParserRuleContext>();
        void Walk(IParseTree node, List<ParserRuleContext> current)
        {
            if (node is CobolParserCore.ParagraphDefinitionContext)
            {
                var own = new List<ParserRuleContext>();
                paragraphs.Add(own);
                current = own;
            }
            if (node is CobolParserCore.StatementContext { Start.Type: > 0 } statement) current.Add(statement);
            for (int i = 0; i < node.ChildCount; i++) Walk(node.GetChild(i), current);
        }
        Walk(tree, outside);
        return [outside, .. paragraphs];
    }

    /// <summary>The position of every visible sequence point of the PDB at <paramref name="pdb"/>: the document's full path (a name the PDB
    /// gives relative to the main source's directory, <paramref name="root"/>, resolved against it), the 1-based line and column.</summary>
    internal static HashSet<(string File, int Line, int Column)> SequencePoints(string pdb, string root)
    {
        using var stream = File.OpenRead(pdb);
        using var provider = MetadataReaderProvider.FromPortablePdbStream(stream);
        var reader = provider.GetMetadataReader();
        var points = new HashSet<(string, int, int)>();
        foreach (var method in reader.MethodDebugInformation)
            foreach (var point in reader.GetMethodDebugInformation(method).GetSequencePoints())
                if (!point.IsHidden)
                    points.Add((SourceDocuments.PathOf(reader.GetString(reader.GetDocument(point.Document).Name), root), point.StartLine, point.StartColumn));
        return points;
    }

    /// <summary>The CORRESPONDING exemption is the statement's own phrase (kb/Work PB5925 review R1-2): a MOVE to an item named CORRECT-X
    /// and an IF that holds a MOVE CORRESPONDING are asked about; the MOVE CORR in the IF, and an ADD CORRESPONDING, are exempt.</summary>
    [Fact]
    public void TheCorrespondingExemption_IsTheStatementsOwnPhrase()
    {
        string source = Path.Combine(TempDir, "corrprobe.cob");
        File.WriteAllText(source, string.Join("\n",
            "       IDENTIFICATION DIVISION.",
            "       PROGRAM-ID. CORRPROBE.",
            "       DATA DIVISION.",
            "       WORKING-STORAGE SECTION.",
            "       01 G1. 02 N PIC 9.",
            "       01 G2. 02 N PIC 9.",
            "       01 CORRECT-X PIC X.",
            "       PROCEDURE DIVISION.",
            "       P1.",
            "           MOVE SPACE TO CORRECT-X",
            "           MOVE CORR G1 TO G2",
            "           IF CORRECT-X = SPACE MOVE CORRESPONDING G1 TO G2 END-IF",
            "           ADD CORRESPONDING G1 TO G2",
            "           SUBTRACT 1 FROM N OF G2",
            "           STOP RUN."));
        var options = new CompilerDriver.Options(source, Path.Combine(TempDir, "corrprobe.dll"));
        var frontend = CompilerDriver.NewFrontend(options, new CompilationInputs(), recordsListing: false);
        var tree = frontend.Parse(source, new DiagnosticBag());
        Assert.NotNull(tree);
        var statements = Paragraphs(tree!).SelectMany(p => p).ToList();
        Assert.Equal([false, true, false, true, true, false, false],
            statements.Select(s => Corresponding(s, s.Start.Text.ToUpperInvariant())).ToList());
    }

    public static TheoryData<string> NistPrograms()
    {
        var data = new TheoryData<string>();
        // tests/nist/corpus.tsv: name, suite, status, ...; a green or divergent row is a program the golden run compiles and runs.
        foreach (string row in File.ReadLines(TestRepo.Nist("corpus.tsv")))
            if (!row.StartsWith('#') && row.Split('\t') is [var name, _, "green" or "divergent", ..]) data.Add(name);
        return data;
    }

    [Theory]
    [MemberData(nameof(NistPrograms))]
    public void EveryCompiledStatement_HasItsSequencePoint_AndNothingElseHasOne(string program)
    {
        string dll = Path.Combine(TempDir, program + ".dll");
        var options = new CompilerDriver.Options(TestRepo.Nist("programs", program + ".cob"), dll, NistTestName: program, DialectLevel: 85);
        var (missing, stray) = Coverage(options);
        Assert.True(missing.Count == 0 && stray.Count == 0,
            $"{program}: {missing.Count} statements with no sequence point, {stray.Count} sequence points at no statement\n"
            + string.Join("\n", missing.Take(20).Concat(stray.Take(20))));
    }
}
