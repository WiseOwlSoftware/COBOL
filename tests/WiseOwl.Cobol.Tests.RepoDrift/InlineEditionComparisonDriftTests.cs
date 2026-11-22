// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.RepoDrift;

/// <summary>
/// ⛔ AN EDITION DIFFERENCE IS WRITTEN IN A REGISTER, NEVER AS AN INLINE COMPARISON WITH AN EDITION YEAR (kb/Work PB6031).
/// <para>An edition difference of the compiler is one of two kinds, and each has its register: a construct an edition LACKS
/// (refused with COBOL0900) is a row of <c>tests/version-matrix/constructs/</c>, generated into <c>ConstructRegistry</c>; a
/// RULE whose answer or screen differs between editions is a member of <c>WiseOwl.Cobol.Runtime.DialectBehavior</c>, asked
/// through the one predicate <c>DialectBehaviors.IsActive</c>, whose row carries the edition the rule changed in, both edges'
/// citations and its <c>docs/VERSION_CHANGE_REFERENCE.md</c> row (held to it by <c>DialectBehaviorRegistryDriftTests</c> and
/// the DOC AUDITS). A site that writes <c>Edition.DialectLevel &lt; 2002</c> (or <c>edition.Year &gt;= 2014</c>, a switch arm
/// <c>&lt; 2002 =&gt;</c>, <c>Year is 2002 or 2014</c>) writes the edge where it is used and nowhere else: no ledger names it and
/// nothing fails when a second site writes the same edge differently.</para>
/// <para>PB6031 measured 155 such comparisons in 61 source files on 2026-11-21 and moved the Report Writer's into
/// <c>DialectBehaviors</c> with the first data, file, statement, comparison and currency edges; <see cref="Unmoved"/> lists the rest per file. It RATCHETS DOWN ONLY: a file may not gain a
/// comparison and a file not listed may hold none (move the edge into the register instead), and a file whose count fell
/// fails until its entry is lowered in the same change, so the list is never stale.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class InlineEditionComparisonDriftTests
{
    /// <summary>The compiler's sources; a generated file (<c>*.g.cs</c>) is its generator's, and the registers' own
    /// predicates compare with <c>ChangedIn</c> and the edition year, not with a literal year.</summary>
    private static readonly TreeScope Sources = new(Under: ["src/"], Kinds: [".cs"]);

    /// <summary>A comparison of anything with an edition year (85, 2002, 2014, 2023): an operator before the year
    /// (<c>DialectLevel &lt; 2002</c>, a switch arm <c>&lt; 2002 =&gt;</c>), the year before an operator, or a pattern
    /// (<c>Year is 2002 or 2014</c>, <c>is &gt;= 2014</c>).</summary>
    private static readonly Regex Comparison = new(
        @"(?<![\w.])(?:(?:<|<=|>|>=|==|!=)\s*(?:85|2002|2014|2023)\b|\b(?:85|2002|2014|2023)\s*(?:<|<=|>|>=|==|!=)(?!=)"
        + @"|\bis\s+(?:not\s+)?(?:\(?\s*[<>]=?\s*)?(?:85|2002|2014|2023)\b)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex StringLiteral = new(@"""(?:[^""\\]|\\.)*""", RegexOptions.Compiled);

    /// <summary>The comparisons a source line writes: none on a comment line, and none inside a string literal or after
    /// <c>//</c>.</summary>
    internal static int ComparisonsOn(string line)
    {
        string trimmed = line.TrimStart();
        if (trimmed.StartsWith("//", StringComparison.Ordinal) || trimmed.StartsWith('*')) return 0;
        string code = StringLiteral.Replace(line, "\"\"");
        int comment = code.IndexOf("//", StringComparison.Ordinal);
        if (comment >= 0) code = code[..comment];
        return Comparison.Matches(code).Count;
    }

    /// <summary>The files that still hold inline edition comparisons, and how many. ⛔ RATCHET DOWN ONLY (see the summary).</summary>
    private static readonly Dictionary<string, int> Unmoved = new(StringComparer.Ordinal)
    {
        ["src/WiseOwl.Cobol.Compiler/Binding/ClassConditionModel.cs"] = 1,
        ["src/WiseOwl.Cobol.Compiler/Binding/CommunicationSectionBinder.cs"] = 1,
        ["src/WiseOwl.Cobol.Compiler/Binding/DataBinder.ClausePlacement.cs"] = 1,
        ["src/WiseOwl.Cobol.Compiler/Binding/DataBinder.cs"] = 2,
        ["src/WiseOwl.Cobol.Compiler/Binding/DataBinder.SignClause.cs"] = 1,
        ["src/WiseOwl.Cobol.Compiler/Binding/DataBinder.SynchronizedClause.cs"] = 1,
        ["src/WiseOwl.Cobol.Compiler/Binding/ExternalFileConnectorRules.cs"] = 4,
        ["src/WiseOwl.Cobol.Compiler/Binding/ExternalRecordDescriptionRules.cs"] = 3,
        ["src/WiseOwl.Cobol.Compiler/Binding/ExternalRecordNameRules.cs"] = 1,
        ["src/WiseOwl.Cobol.Compiler/Binding/FileControlBinder.cs"] = 1,
        ["src/WiseOwl.Cobol.Compiler/Binding/FileControlBinder.FileSection.cs"] = 2,
        ["src/WiseOwl.Cobol.Compiler/Binding/FileControlBinder.IoControl.cs"] = 3,
        ["src/WiseOwl.Cobol.Compiler/Binding/FileControlKeyRules.cs"] = 2,
        ["src/WiseOwl.Cobol.Compiler/Binding/FileDescriptionFormats.cs"] = 2,
        ["src/WiseOwl.Cobol.Compiler/Binding/IntrinsicCatalog.cs"] = 1,
        ["src/WiseOwl.Cobol.Compiler/Binding/IntrinsicCitations.cs"] = 1,
        ["src/WiseOwl.Cobol.Compiler/Binding/OptionsBinder.cs"] = 1,
        ["src/WiseOwl.Cobol.Compiler/Binding/Passes/CheckCodeSetRecordItemsPass.cs"] = 1,
        ["src/WiseOwl.Cobol.Compiler/Binding/Passes/CheckSignClausesPass.cs"] = 1,
        ["src/WiseOwl.Cobol.Compiler/Binding/Passes/OccursKeyResolvePass.cs"] = 2,
        ["src/WiseOwl.Cobol.Compiler/Binding/PictureAnalyzer.cs"] = 3,
        ["src/WiseOwl.Cobol.Compiler/Binding/PictureComposition.cs"] = 2,
        ["src/WiseOwl.Cobol.Compiler/Binding/Procedure/ExceptionPhraseRequirement.cs"] = 1,
        ["src/WiseOwl.Cobol.Compiler/Binding/Procedure/ExpressionBinder.cs"] = 1,
        ["src/WiseOwl.Cobol.Compiler/Binding/Procedure/Verbs/CallBinder.cs"] = 1,
        ["src/WiseOwl.Cobol.Compiler/Binding/Procedure/Verbs/CommunicationBinder.cs"] = 1,
        ["src/WiseOwl.Cobol.Compiler/Binding/Procedure/Verbs/CorrespondingBinder.cs"] = 1,
        ["src/WiseOwl.Cobol.Compiler/Binding/Procedure/Verbs/IntrinsicBinder.cs"] = 2,
        ["src/WiseOwl.Cobol.Compiler/Binding/Procedure/Verbs/MoveBinder.cs"] = 1,
        ["src/WiseOwl.Cobol.Compiler/Binding/Procedure/Verbs/SortBinder.cs"] = 1,
        ["src/WiseOwl.Cobol.Compiler/Binding/Procedure/Verbs/SortKeyAdmission.cs"] = 1,
        ["src/WiseOwl.Cobol.Compiler/Binding/SourceUnitShape.cs"] = 1,
        ["src/WiseOwl.Cobol.Compiler/Binding/SpecialNamesBinder.Alphabets.cs"] = 1,
        ["src/WiseOwl.Cobol.Compiler/Binding/SpecialNamesBinder.Currency.cs"] = 4,
        ["src/WiseOwl.Cobol.Compiler/Binding/SubscriptBinder.cs"] = 1,
        ["src/WiseOwl.Cobol.Compiler/Binding/SymbolTable/SymbolTable.Reports.cs"] = 1,
        ["src/WiseOwl.Cobol.Compiler/Binding/Validation/RecordAreaRule.cs"] = 1,
        ["src/WiseOwl.Cobol.Compiler/Binding/Validation/StatementValidation.cs"] = 6,
        ["src/WiseOwl.Cobol.Compiler/Binding/ValueSubject.cs"] = 1,
        ["src/WiseOwl.Cobol.Compiler/CodeGen/DataDivision/ValueInitializer.cs"] = 2,
        ["src/WiseOwl.Cobol.Compiler/CodeGen/DispatchEmitter.cs"] = 1,
        ["src/WiseOwl.Cobol.Compiler/CodeGen/Emit/ConditionRenderer.cs"] = 2,
        ["src/WiseOwl.Cobol.Compiler/CodeGen/ProgramEmitter.cs"] = 1,
        ["src/WiseOwl.Cobol.Compiler/CodeGen/Verbs/CommunicationEmitter.cs"] = 1,
        ["src/WiseOwl.Cobol.Compiler/CodeGen/Verbs/MoveEmitter.cs"] = 1,
        ["src/WiseOwl.Cobol.Compiler/CodeGen/Verbs/SequentialIoEmitter.cs"] = 1,
        ["src/WiseOwl.Cobol.Compiler/Validation/VersionConformancePass.cs"] = 6,
        ["src/WiseOwl.Cobol.Editions/CobolCharacterRepertoire.cs"] = 4,
        ["src/WiseOwl.Cobol.Editions/EditionInfo.cs"] = 20,
        ["src/WiseOwl.Cobol.Frontend/Common/CobolWordRule.cs"] = 4,
        ["src/WiseOwl.Cobol.Frontend/Common/CompilerDirectiveWords.cs"] = 1,
        ["src/WiseOwl.Cobol.Frontend/Pipeline/Frontend.cs"] = 1,
        ["src/WiseOwl.Cobol.Frontend/Preprocessor/CopyProcessor.cs"] = 5,
        ["src/WiseOwl.Cobol.Frontend/Preprocessor/ReferenceFormatDiagnostics.cs"] = 1,
        ["src/WiseOwl.Cobol.Runtime/CobolNames.cs"] = 2,
    };

    [Fact]
    public void NoSourceWritesAnEditionEdge_OutsideTheRegisters()
    {
        var files = TrackedTree.Files(Sources).Where(f => !f.EndsWith(".g.cs", StringComparison.Ordinal)).ToList();
        Assert.True(files.Count > 500, $"the source listing found only {files.Count} C# files; it is broken");
        var found = files.Select(f => (File: f, Count: File.ReadLines(Path.Combine(TestRepo.Root, f)).Sum(ComparisonsOn)))
            .Where(x => x.Count > 0).ToDictionary(x => x.File, x => x.Count, StringComparer.Ordinal);

        var over = found.Where(kv => kv.Value > Unmoved.GetValueOrDefault(kv.Key))
            .Select(kv => $"{kv.Key}: {kv.Value} (allowed {Unmoved.GetValueOrDefault(kv.Key)})").ToList();
        Assert.True(over.Count == 0, "a source writes an edition edge as an inline comparison with an edition year; ask the "
            + "register instead (DialectBehaviors.IsActive with a DialectBehavior member and its VERSION_CHANGE_REFERENCE row, "
            + "or a constructs/ row for a construct an edition lacks; kb/Work PB6031):\n  " + string.Join("\n  ", over));

        var stale = Unmoved.Where(kv => found.GetValueOrDefault(kv.Key) < kv.Value)
            .Select(kv => $"[\"{kv.Key}\"] = {found.GetValueOrDefault(kv.Key)},   // was {kv.Value}").ToList();
        Assert.True(stale.Count == 0, "a file holds fewer inline edition comparisons than Unmoved allows; lower its entry (remove "
            + "it at 0) so the ratchet stays tight:\n  " + string.Join("\n  ", stale));
    }

    [Theory]
    [InlineData("        if (Edition.DialectLevel < 2002) return;", 1)]
    [InlineData("        bool is2023 = edition.DialectLevel >= 2023;", 1)]
    [InlineData("        if ((is2023 || edition.DialectLevel < 2002) && f.Linage is { } lin)", 1)]
    [InlineData("        < 2014 => Cobol2002,", 1)]
    [InlineData("    public bool SubscriptedClauseOperands => Year == 2002;", 1)]
    [InlineData("    public bool Restricted => Year is 2002 or 2014 || (Year == 2023 && Permissive);", 2)]
    [InlineData("        if (2002 <= year) return;", 1)]
    [InlineData("        if (!DialectBehaviors.IsActive(DialectBehavior.ReportCodeClauseOperand, Edition.DialectLevel)) return;", 0)]
    [InlineData("        // COBOL-2002 and later: DialectLevel >= 2002 is what the 1985 rule asks", 0)]
    [InlineData("        Error($\"... (§2023:13.18.16.3 SR6) when DialectLevel >= 2002\");", 0)]
    [InlineData("        int width = 132;   // the edition's DialectLevel < 2002 is the register's", 0)]
    public void TheCount_SeesEachSpelling_AndNoComment(string line, int expected) =>
        Assert.Equal(expected, ComparisonsOn(line));
}
