// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Frontend.Diagnostics;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;
using CnFrontend = WiseOwl.Cobol.Frontend.Frontend;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE STORAGE-CLAIM OPERANDS OF A COMPILATION ARE FOUND BY ONE WALK OF ITS PARSE TREE AND SLICED PER SOURCE ELEMENT
/// (kb/Work PB4302). A container asks which <c>ADDRESS OF</c> heads and which BY REFERENCE operands every program it
/// contains takes (ISO §13.18.27.4 GR2: a contained program references a GLOBAL name "without describing it again", so the
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

    private static CobolParserCore.ProgramUnitContext Parse()
    {
        string path = Path.Combine(Path.GetTempPath(), "cn_claimscan_" + Guid.NewGuid().ToString("N")[..8] + ".cob");
        File.WriteAllText(path, Source);
        try
        {
            var diags = new DiagnosticBag();
            var tree = new CnFrontend { InitialFormat = InitialReferenceFormat.Auto }.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            Assert.NotNull(tree);
            return tree!.compilationGroup().SelectMany(g => g.programUnit()).Single();
        }
        finally { FileHarness.TryDelete(path); }
    }

    private static string Show(AddressTarget t) => string.Join(" OF ", t.Qualifiers.Prepend(t.Name));

    private static string Show(ByReferenceOperand o) =>
        string.Join(" OF ", o.Qualifiers.Prepend(o.Name)) + (o.SlotsOnly ? "[slots]" : "") + (o.Invoke ? "[invoke]" : "");

    [Fact]
    public void AnAddressOfHeadIsFoundUnderTheRegionThatWritesIt_AndUnderEveryRegionAboveIt()
    {
        var top = Parse();
        var mid = top.nestedProgram().Single();
        var inner = mid.nestedProgram().Single();
        var scan = new StorageClaimScan();

        // §8.4.3.11 — a data-address-identifier is ADDRESS OF's own surface; SET PTR TO ADDRESS OF GA is TOPP's, and
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

        Assert.Equal(["GB"], scan.ByReferenceOperands(top.procedureDivision(), _ => false).Select(Show));

        // MIDP: §14.9.4.4 GR5 — a BY CONTENT operand (claimed only when its values ride managed slots), then BY REFERENCE GA;
        // BY VALUE GC GB passes neither (the phrase is transitive); GA, then the RETURNING receiver GB (§14.6.5); and — with
        // UF a user-defined function — the function argument GA. INNP's CALL (inside MIDP's subtree): GA OF GB, then TBL (the
        // subscript IDX is a sending operand, never passed), and no operand for the arithmetic expression GA + GB.
        Assert.Equal(
            ["GC[slots]", "GA", "GA", "GB[slots]", "GA OF GB", "TBL"],
            scan.ByReferenceOperands(mid, _ => false).Select(Show));
        Assert.Equal(
            ["GC[slots]", "GA", "GA", "GB[slots]", "GA", "GA OF GB", "TBL"],
            scan.ByReferenceOperands(mid, name => name == "UF").Select(Show));
    }

    [Fact]
    public void TheUserFunctionPredicateIsTheAskingElements_NotTheWalks()
    {
        var top = Parse();
        var mid = top.nestedProgram().Single();
        var scan = new StorageClaimScan();

        // One recorded walk answers two elements that disagree about which function names are user-defined.
        int withUf = scan.ByReferenceOperands(mid, n => n == "UF").Count();
        int withoutUf = scan.ByReferenceOperands(mid, _ => false).Count();
        int otherName = scan.ByReferenceOperands(mid, n => n == "OTHER").Count();
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
        _ = scan.ByReferenceOperands(mid, _ => false).ToList();
        _ = scan.AddressTargets(top.procedureDivision()).ToList();
        _ = scan.ByReferenceOperands(mid.procedureDivision(), _ => false).ToList();
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

        string driver = StripComments(File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Compiler", "Binding", "BinderDriver.cs")));
        Assert.Matches(@"ClaimScans\s*=\s*session\.ClaimScans", driver);
    }

    private static string StripComments(string s) =>
        Regex.Replace(Regex.Replace(s, @"/\*.*?\*/", " ", RegexOptions.Singleline), @"//[^\n]*", " ");
}
