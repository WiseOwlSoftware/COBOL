// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Compiler.Binding;
using WiseOwl.Cobol.Frontend.Diagnostics;
using WiseOwl.Cobol.Frontend.Parsing;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;
using CnFrontend = WiseOwl.Cobol.Frontend.Frontend;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE STORAGE-CLAIM OPERANDS OF A COMPILATION ARE FOUND BY ONE WALK OF ITS PARSE TREE AND SLICED PER SOURCE ELEMENT
/// (kb/Work PB4302). A container asks which <c>ADDRESS OF</c> heads and which BY REFERENCE operands every program it
/// contains takes (ISO §2023:13.18.27.4 GR2: a contained program references a GLOBAL name "without describing it again", so the
/// storage it names is the container's), and the question used to be a fresh recursive walk of each contained subtree from
/// every container above it — N(N+1)/2 program walks for a chain of N, each charging a node its depth in MoveNext calls
/// (bind 558 ms at depth 100, 3,640 ms at 200). The answers below are the operands the standard's rules name, in document
/// order; the walk count is the structural guard — a count, never a clock.
/// </summary>
public sealed class StorageClaimScanTests
{
    // TOPP contains MIDP, which contains INNP. Each element's own operands are in its own procedure division.
    private const string Source =
        "       IDENTIFICATION DIVISION.\r\n"
        + "       PROGRAM-ID. TOPP.\r\n"
        + "       DATA DIVISION.\r\n"
        + "       WORKING-STORAGE SECTION.\r\n"
        + "       01 GA PIC X GLOBAL.\r\n"
        + "       01 GB PIC X GLOBAL.\r\n"
        + "       01 GC PIC X GLOBAL.\r\n"
        + "       01 PTR USAGE POINTER.\r\n"
        + "       PROCEDURE DIVISION.\r\n"
        + "           SET PTR TO ADDRESS OF GA\r\n"
        + "           CALL \"EXT\" USING GB\r\n"
        + "           GOBACK.\r\n"
        + "       IDENTIFICATION DIVISION.\r\n"
        + "       PROGRAM-ID. MIDP.\r\n"
        + "       PROCEDURE DIVISION.\r\n"
        + "           CALL \"EXT\" USING BY CONTENT GC BY REFERENCE GA\r\n"
        + "           CALL \"EXT\" USING BY VALUE GC GB\r\n"
        + "           CALL \"EXT\" USING GA RETURNING GB\r\n"
        + "           MOVE FUNCTION UF (GA) TO GB\r\n"
        + "           GOBACK.\r\n"
        + "       IDENTIFICATION DIVISION.\r\n"
        + "       PROGRAM-ID. INNP.\r\n"
        + "       PROCEDURE DIVISION.\r\n"
        + "           SET PTR TO ADDRESS OF GC\r\n"
        + "           CALL \"EXT\" USING GA OF GB TBL (IDX) GA + GB\r\n"
        + "           GOBACK.\r\n"
        + "       END PROGRAM INNP.\r\n"
        + "       END PROGRAM MIDP.\r\n"
        + "       END PROGRAM TOPP.\r\n";

    // One program whose CALLs are Format 2 (AS NESTED; a bare target word the asking element may name a program prototype) and
    // Format 1 (a literal target with no AS phrase).
    private const string FormatsSource =
        "       IDENTIFICATION DIVISION.\r\n"
        + "       PROGRAM-ID. FMTP.\r\n"
        + "       DATA DIVISION.\r\n"
        + "       WORKING-STORAGE SECTION.\r\n"
        + "       01 GA PIC X.\r\n"
        + "       01 GB PIC X.\r\n"
        + "       01 GC PIC X.\r\n"
        + "       PROCEDURE DIVISION.\r\n"
        + "           CALL \"CHILD\" AS NESTED USING BY VALUE GC GA BY CONTENT GC GB\r\n"
        + "           CALL PROTO USING BY CONTENT GC GA\r\n"
        + "           CALL \"EXT\" USING BY CONTENT GC GA\r\n"
        + "           CALL \"CHILD\" AS NESTED USING GB BY CONTENT GB B-AND GC\r\n"
        + "           INVOKE OBJ \"M\" USING GC BY CONTENT GB B-AND GC\r\n"
        + "           GOBACK.\r\n"
        + "       END PROGRAM FMTP.\r\n";

    private static CobolParserCore.ProgramUnitContext Parse(string source = Source)
    {
        string path = Path.Combine(Path.GetTempPath(), "cn_claimscan_" + Guid.NewGuid().ToString("N")[..8] + ".cob");
        File.WriteAllText(path, source);
        try
        {
            var diags = new DiagnosticBag();
            var tree = new CnFrontend { InitialFormat = TestSourceFormat.Inline }.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            Assert.NotNull(tree);
            return tree!.compilationGroup().SelectMany(g => g.programUnit()).Single();
        }
        finally { FileHarness.TryDelete(path); }
    }

    private static string Show(AddressTarget t) => string.Join(" OF ", t.Written.QualifierWords.Prepend(t.Written.Name));

    private static string Show(ByReferenceOperand o) =>
        string.Join(" OF ", o.Written.QualifierWords.Prepend(o.Written.Name)) + (o.SlotsOnly ? "[slots]" : "") + (o.Invoke ? "[invoke]" : "");

    [Fact]
    public void AnAddressOfHeadIsFoundUnderTheRegionThatWritesIt_AndUnderEveryRegionAboveIt()
    {
        var top = Parse();
        var mid = top.nestedProgram().Single();
        var inner = mid.nestedProgram().Single();
        var scan = new StorageClaimScan();

        // §2023:8.4.3.11 — a data-address-identifier is ADDRESS OF's own surface; SET PTR TO ADDRESS OF GA is TOPP's, and
        // SET PTR TO ADDRESS OF GC is INNP's, which MIDP contains.
        Assert.Equal(["GA"], scan.AddressTargets(top.procedureDivision()).Select(Show));
        Assert.Empty(scan.AddressTargets(mid.procedureDivision()));
        Assert.Equal(["GC"], scan.AddressTargets(mid).Select(Show));
        Assert.Equal(["GC"], scan.AddressTargets(inner).Select(Show));
        Assert.Equal(["GC"], scan.AddressTargets(inner.procedureDivision()).Select(Show));
    }

    [Fact]
    public void ByReferenceOperandsFollowTheTransitiveByPhraseRule_InDocumentOrder()
    {
        var top = Parse();
        var mid = top.nestedProgram().Single();
        var scan = new StorageClaimScan();

        Assert.Equal(["GB"], scan.ByReferenceOperands(top.procedureDivision(), _ => false, _ => false).Select(Show));

        // MIDP: §2023:14.9.4.4 GR5 — a BY CONTENT operand (claimed only when its values ride managed slots), then BY REFERENCE GA;
        // BY VALUE GC GB passes neither (a Format 1 CALL carries the phrase); GA, then the RETURNING receiver GB (§2023:14.6.5); and — with
        // UF a user-defined function — the function argument GA. INNP's CALL (inside MIDP's subtree): GA OF GB, then TBL (the
        // subscript IDX is a sending operand, never passed), and no operand for the arithmetic expression GA + GB.
        Assert.Equal(
            ["GC[slots]", "GA", "GA", "GB[slots]", "GA OF GB", "TBL"],
            scan.ByReferenceOperands(mid, _ => false, _ => false).Select(Show));
        Assert.Equal(
            ["GC[slots]", "GA", "GA", "GB[slots]", "GA", "GA OF GB", "TBL"],
            scan.ByReferenceOperands(mid, name => name == "UF", _ => false).Select(Show));
    }

    [Fact]
    public void AFormat2KeywordLessIdentifierIsClaimedAsGr9PassesIt_WhateverPhraseCameBefore()
    {
        var program = Parse(FormatsSource);
        var scan = new StorageClaimScan();

        // §2023:14.9.4.4 GR5 is Format 1's rule; in Format 2, GR9 takes a keyword-less argument's mode from its formal, which is
        // bound after the claim, so GA and GB are claimed as a) 1. passes them to a BY REFERENCE formal (kb/Work PB3227). The
        // bare word PROTO makes the second CALL Format 2 only when the asking element names it a program prototype; the literal
        // target of the third keeps Format 1, where BY CONTENT carries to GA. In the last two statements the later argument's
        // B-AND steers the keyword-less GB and GC into the boolean node, and §2023:14.9.4.4 GR8 (§2023:14.9.23.4 GR4) still makes each
        // the identifier it consists of; the boolean expression itself is passed by content and claims nothing.
        Assert.Equal(
            ["GA", "GC[slots]", "GB", "GC[slots]", "GA[slots]", "GC[slots]", "GA[slots]", "GB", "GC[invoke]"],
            scan.ByReferenceOperands(program.procedureDivision(), _ => false, _ => false).Select(Show));
        Assert.Equal(
            ["GA", "GC[slots]", "GB", "GC[slots]", "GA", "GC[slots]", "GA[slots]", "GB", "GC[invoke]"],
            scan.ByReferenceOperands(program.procedureDivision(), _ => false, word => word == "PROTO").Select(Show));
    }

    [Fact]
    public void TheUserFunctionPredicateIsTheAskingElements_NotTheWalks()
    {
        var top = Parse();
        var mid = top.nestedProgram().Single();
        var scan = new StorageClaimScan();

        // One recorded walk answers two elements that disagree about which function names are user-defined.
        int withUf = scan.ByReferenceOperands(mid, n => n == "UF", _ => false).Count();
        int withoutUf = scan.ByReferenceOperands(mid, _ => false, _ => false).Count();
        int otherName = scan.ByReferenceOperands(mid, n => n == "OTHER", _ => false).Count();
        Assert.Equal(withoutUf + 1, withUf);
        Assert.Equal(withoutUf, otherName);
    }

    [Fact]
    public void TheTreeIsWalkedOnce_HoweverManyRegionsAskAndInWhateverOrder()
    {
        var top = Parse();
        var mid = top.nestedProgram().Single();
        var inner = mid.nestedProgram().Single();
        var scan = new StorageClaimScan();

        // The nested program is asked first, before the program that contains it: the root is the top of the parent chain,
        // not the element that asked, so no element's question starts a walk of its own.
        _ = scan.AddressTargets(inner).ToList();
        _ = scan.ByReferenceOperands(mid, _ => false, _ => false).ToList();
        _ = scan.AddressTargets(top.procedureDivision()).ToList();
        _ = scan.ByReferenceOperands(mid.procedureDivision(), _ => false, _ => false).ToList();
        _ = scan.AddressTargets(mid).ToList();

        Assert.Equal(1, scan.TreesWalked);
    }

    [Fact]
    public void ARegionIsAProcedureDivisionOrAContainedProgram_NothingElse()
    {
        var top = Parse();
        var scan = new StorageClaimScan();

        // A program unit's own subtree includes the programs it contains, which is not a question anything asks (a unit's own
        // operands are its procedure division's): it is refused loudly, not answered with a wrong slice.
        Assert.Throws<InvalidOperationException>(() => scan.AddressTargets(top).ToList());
    }

    /// <summary>The structural pin: one scan per compilation, handed to every unit's binder. A second construction site is a
    /// second walk per unit, which is the quadratic walk this class replaced.</summary>
    [Fact]
    [Trait("Sweep", "tree")]
    public void TheScanIsConstructedOncePerCompilation_AndHandedToEveryUnitsBinder()
    {
        var sites = Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Compiler"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(f => (File: Path.GetFileName(f), Src: StripComments(File.ReadAllText(f))))
            .SelectMany(s => Regex.Matches(s.Src, @"new\s+StorageClaimScan\s*\(\s*\)|StorageClaimScan\s+\w+\s*\{[^}]*\}\s*=\s*new\s*\(\s*\)")
                .Select(_ => s.File))
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();
        Assert.Equal(["BindSession.cs", "DataBinder.Ptr.cs"], sites);

        // Every element's binder is seeded by BindSession.NewDataBinder (kb/Work PB2626), which hands it the session's one scan.
        string session = StripComments(File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Compiler", "Binding", "BindSession.cs")));
        Assert.Matches(@"NewDataBinder\([^)]*\)\s*=>\s*new\(Edition\)\s*\{[^}]*\bClaimScans\s*=\s*ClaimScans\b", session);
    }

    private static string StripComments(string s) =>
        Regex.Replace(Regex.Replace(s, @"/\*.*?\*/", " ", RegexOptions.Singleline), @"//[^\n]*", " ");
}
