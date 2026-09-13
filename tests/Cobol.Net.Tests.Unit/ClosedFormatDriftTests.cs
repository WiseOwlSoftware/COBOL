// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Cst;
using CobolNet.Frontend.Generated;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

using Core = CobolParserCore;

/// <summary>
/// ⛔ THE DRIFT GUARD FOR THE GRAMMAR'S LAST TOTAL SINK (kb/Work PB829).
///
/// <para><b>Why it exists.</b> <c>genericClause : IDENTIFIER (IDENTIFIER|literal)*</c> matches ANY run of words,
/// so wherever it sits in an alternative list it swallows everything the named alternatives missed. It was ONE
/// grammar rule reached from SIX sites spanning EIGHT closed general formats, and closing it one site at a time
/// is exactly how it survived: kb/Work PB487 closed §13.16.2 and swept for siblings, but read the grammar by rule
/// NAME — so it counted four of the remaining five and missed the I-O-CONTROL paragraph's INLINE alternative,
/// which is not a named <c>xxxClause : genericClause</c> wrapper. "One production, one pass, one table" is only
/// a claim until something derives it mechanically, which is what these facts do.</para>
///
/// <para><b>Both directions, and from two independent sources.</b> The GRAMMAR facts read the <c>.g4</c> files
/// and require that the word-run matcher is referenced from the single error production and nowhere else — a new
/// catch-all cannot be introduced under a new name. The REFLECTION facts read the generated parser and require
/// that every context type able to hold an <c>unrecognizedClause</c> has a <see cref="ClosedFormats"/> row, and
/// that every row names a context type the grammar still produces — a new closed format cannot parse into
/// silence, and a stale row (a dead lookup, which this repository has learned is never contradicted) cannot
/// linger.</para>
/// </summary>
public sealed class ClosedFormatDriftTests : CobolNetTestBase
{
    /// <summary>Every parser grammar fragment the frontend compiles. Globbed rather than listed: a new .g4 file
    /// is exactly where an unnoticed catch-all would land.</summary>
    private static string[] GrammarFiles() =>
        Directory.GetFiles(TestRepo.Src("Cobol.Net.Frontend", "Grammar"), "*.g4", SearchOption.AllDirectories)
                 .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                          && !p.Contains($"{Path.DirectorySeparatorChar}Generated{Path.DirectorySeparatorChar}"))
                 .ToArray();

    /// <summary>The file's text with line comments stripped, so a rule NAMED in prose does not count as a
    /// reference (the DeclinedFacilityDriftTests mechanic — and load-bearing here, because every site this
    /// closed carries a comment block that mentions <c>genericClause</c> by name).</summary>
    private static string CodeOf(string path) => Regex.Replace(File.ReadAllText(path), @"//[^\n]*", "");

    [Fact]
    public void TheGrammarProbe_FindsTheFilesAndTheRule()
    {
        // A probe that finds nothing would make every obligation below vacuously green
        // (feedback_verdict_evidence_invariant: a MISSING observation is not a NEGATIVE one).
        var files = GrammarFiles();
        Assert.True(files.Length >= 5, $"only {files.Length} .g4 file(s) found under Grammar/ — the glob is broken");
        Assert.Contains(files, f => CodeOf(f).Contains("genericClause", StringComparison.Ordinal));
        Assert.Contains(files, f => CodeOf(f).Contains("unrecognizedClause", StringComparison.Ordinal));
    }

    /// <summary>⛔ THE GRAMMAR OBLIGATION. <c>genericClause</c> — the unbounded word-run matcher — may be
    /// referenced ONLY from its own definition and from the body of <c>unrecognizedClause</c>, the single error
    /// production. Any other reference is a new total sink over some alternative list, which is the defect
    /// PB487 and PB829 each spent a landing closing.
    /// <para>⚠ THE COMMENT-ENTRY SINKS ARE NOT THIS. <c>dateCompiledContent</c>, <c>securityContent</c> and
    /// <c>remarksContent</c> are also unbounded token runs and are CORRECT: a comment-entry is arbitrary text by
    /// definition, which is why ISO/IEC 1989:2023 defines no syntax for one anywhere. The distinction is what the
    /// sink sits over — a COMMENT-ENTRY (right) or a CLAUSE / PARAGRAPH LIST (this defect) — so they are
    /// deliberately outside this fact's reach.</para></summary>
    [Fact]
    public void GenericClause_IsReferencedOnlyByTheOneErrorProduction()
    {
        var offenders = new List<string>();
        foreach (string path in GrammarFiles())
        {
            string code = CodeOf(path);
            foreach (Match m in Regex.Matches(code, @"\bgenericClause\b"))
            {
                // The definition line itself: `genericClause` alone on a line, followed by the rule body.
                int lineStart = code.LastIndexOf('\n', Math.Max(0, m.Index - 1)) + 1;
                string line = code[lineStart..code.IndexOf('\n', m.Index)].Trim();
                if (line == "genericClause") continue;                    // its own definition
                if (line.StartsWith(": genericClause", StringComparison.Ordinal)) continue;  // a rule body — checked below
                offenders.Add($"{Path.GetFileName(path)}: {line}");
            }
        }
        // Only ONE rule body may be `: genericClause`, and it must be unrecognizedClause.
        var bodies = new List<string>();
        foreach (string path in GrammarFiles())
            foreach (Match m in Regex.Matches(CodeOf(path), @"(?m)^([a-z][A-Za-z0-9_]*)\s*\r?\n\s*:\s*genericClause\s*\r?\n\s*;"))
                bodies.Add(m.Groups[1].Value);

        Assert.True(offenders.Count == 0,
            "genericClause — the unbounded `IDENTIFIER (IDENTIFIER|literal)*` word-run matcher — is referenced "
            + "outside the ONE error production, so some alternative list has a total sink again and whatever it "
            + "swallows will be accepted in silence (kb/Work PB829): " + string.Join(" | ", offenders));
        Assert.Equal(["unrecognizedClause"], bodies);
    }

    /// <summary>The context types the GENERATED parser says can hold an <c>unrecognizedClause</c>: ANTLR emits a
    /// no-argument accessor returning <c>UnrecognizedClauseContext</c> on the context class of every rule whose
    /// body references it.</summary>
    private static IReadOnlyList<Type> ContextsCarryingTheErrorProduction() =>
        typeof(Core).GetNestedTypes(BindingFlags.Public)
            .Where(t => typeof(ParserRuleContext).IsAssignableFrom(t)
                        && t != typeof(Core.UnrecognizedClauseContext)
                        && t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                            .Any(m => m.GetParameters().Length == 0
                                   && (m.ReturnType == typeof(Core.UnrecognizedClauseContext)
                                    || m.ReturnType == typeof(Core.UnrecognizedClauseContext[]))))
            .ToList();

    /// <summary>⛔ THE TABLE OBLIGATION, forward. A closed general format that gains the error production without
    /// a <see cref="ClosedFormats"/> row would refuse its residue with a message that cannot name the format —
    /// which is how the eight formats came to share one anonymous catch-all in the first place.</summary>
    [Fact]
    public void EveryContextCarryingTheErrorProduction_HasAClosedFormatRow()
    {
        var carriers = ContextsCarryingTheErrorProduction();
        Assert.True(carriers.Count >= 8,
            $"only {carriers.Count} context type(s) carry unrecognizedClause — the reflection probe is broken, "
            + "and both obligations below would be vacuously satisfied");
        var missing = carriers.Where(t => !ClosedFormats.ByContext.ContainsKey(t)).Select(t => t.Name).ToList();
        Assert.True(missing.Count == 0,
            "a closed general format ends in `unrecognizedClause` with no ClosedFormats.ByContext row, so "
            + "ClosedFormatPass cannot name the format it violates: " + string.Join(", ", missing)
            + ". Render the format's printed page, record its clause list above the alternative list in the .g4, "
            + "and add the row.");
    }

    /// <summary>⛔ THE TABLE OBLIGATION, backward. A row for a context type the grammar no longer produces is a
    /// dead lookup, and a dead lookup is never contradicted.</summary>
    [Fact]
    public void EveryClosedFormatRow_IsStillCarriedByTheGrammar()
    {
        var carriers = ContextsCarryingTheErrorProduction().ToHashSet();
        var dead = ClosedFormats.ByContext.Keys.Where(t => !carriers.Contains(t)).Select(t => t.Name).ToList();
        Assert.True(dead.Count == 0,
            "ClosedFormats.ByContext maps a context type that no longer ends in `unrecognizedClause`: "
            + string.Join(", ", dead));
    }

    /// <summary>Prove the guards can FAIL (feedback_green_gates_arent_evidence): a context type that does NOT
    /// carry the error production must be absent from the carrier set, and a fabricated rule name must not
    /// satisfy the grammar fact. Run against real-but-unrelated subjects rather than by mutating the files.</summary>
    [Fact]
    public void TheObligations_CanFail()
    {
        Assert.DoesNotContain(typeof(Core.ProcedureDivisionContext), ContextsCarryingTheErrorProduction());
        Assert.False(ClosedFormats.ByContext.ContainsKey(typeof(Core.ProcedureDivisionContext)));
        Assert.Null(ClosedFormats.Of(typeof(Core.ProcedureDivisionContext)));
        Assert.Null(ClosedFormats.Of(null));
    }

    /// <summary>Every row's message parts are non-empty and its code is one of the three the band owns. The §
    /// is checked as a shape, not by memory — <c>audit_code_citations.py</c> validates the clause numbers
    /// themselves against the standard.</summary>
    [Fact]
    public void EveryClosedFormatRow_IsWellFormed()
    {
        foreach (var (type, f) in ClosedFormats.ByContext)
        {
            Assert.Matches(@"^\d+(\.\d+)+$", f.Clause);
            Assert.False(string.IsNullOrWhiteSpace(f.Subject), $"{type.Name}: empty Subject");
            Assert.Contains(f.Noun, new[] { "clause", "paragraph" });
            Assert.Contains(f.Code, new[] { "COBOLNET1941", "COBOLNET1970", "COBOLNET1971" });
            Assert.True(f.FormatLabel.Length == 0 || f.FormatLabel.EndsWith(' '),
                $"{type.Name}: FormatLabel must carry its own trailing space so the message needs no second template");
            // The noun and the code agree: a PARAGRAPH list is 1971, a CLAUSE list is 1970 (or 1941, the code
            // §13.16.2 kept from kb/Work PB487). A mismatch sends the reader to the wrong subclause.
            if (f.Noun == "paragraph") Assert.Equal("COBOLNET1971", f.Code);
            else Assert.Contains(f.Code, new[] { "COBOLNET1941", "COBOLNET1970" });
        }
    }

    /// <summary>The band is ERROR severity at every edition and every strictness. §4.2.2 makes a general format
    /// the definition of what may be written; there is no permissive reading of an unrecognized clause that is
    /// not "compile something the programmer did not write", so it never downgrades to a warning.</summary>
    [Fact]
    public void TheClosedFormatBand_IsErrorSeverity_AndOwnsItsCodesExclusively()
    {
        foreach (var d in new[]
                 {
                     DiagnosticCatalog.DataClauseUnrecognized,
                     DiagnosticCatalog.ClosedFormatUnrecognizedClause,
                     DiagnosticCatalog.ClosedFormatUnrecognizedParagraph,
                 })
            Assert.Equal(Editions.EditionSeverity.Error, d.Severity);

        foreach (string code in new[] { "COBOLNET1941", "COBOLNET1970", "COBOLNET1971" })
            Assert.Single(DiagnosticCatalog.All, d => d.Code == code);
    }
}
