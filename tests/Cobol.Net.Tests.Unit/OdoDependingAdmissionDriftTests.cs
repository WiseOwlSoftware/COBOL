// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Preprocessor;
using CobolNet.Tests.Shared;
using Xunit;
using CnFrontend = CobolNet.Frontend.Frontend;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ AN OCCURS DEPENDING TABLE RECORDS ONLY AN ADMITTED data-name-1 — AN INTEGER, NOT SUBSCRIPTED, OUTSIDE THE TABLE'S
/// RANGE — SO NO CONSUMER OF <see cref="OccursSpec.Depending"/> CAN CYCLE (kb/Work PB3510). ISO §13.18.38.3 SR17:
/// "Data-name-1 shall describe an integer"; SR2: "Data-name-1 and data-name-2 shall not be subscripted"; SR20: "The
/// data item defined by data-name-1 shall not occupy a byte position within the range of the first byte position
/// defined by the data description entry containing the OCCURS clause and the last byte position defined by the record
/// description entry containing that OCCURS clause."
/// <para><b>The defect.</b> <c>DataBinder.OdoResolve</c> recorded the resolved data-name-1 first and reported the rules
/// after, so a GROUP data-name-1 reached every consumer. The §13.18.38.4 GR8 group-operand slice
/// (<c>ReferenceResolver.WrapIfOdoGroup</c>) resolves data-name-1's own place, and resolving a GROUP holding an
/// occurs-depending table wraps it again: <c>DEPENDING ON</c> the record that holds the table (fuzz program fz01530),
/// or two records each depending on the other, recursed without end and the compiler died of a stack overflow before
/// COBOLNET0852 or COBOLNET0857 reached the user. The SR20 test also looked for a group data-name-1 among the record's
/// LEAVES, found none, and passed it.</para>
/// <para><b>What this pins.</b> (1) The only writers of <see cref="OccursSpec.Depending"/> are the admission in
/// <c>OdoResolve</c>, guarded by the three predicates, and the extent freeze in <c>SendingValueTemp</c>, which repoints
/// a clone at a temporary copied from an admitted (so elementary) data-name-1. (2) Behaviourally, over every shape that
/// names an ancestor of the table, the table itself, an item within it, a group elsewhere or a legal counter: binding
/// ends, every recorded link is an elementary integer outside its table, and the rules report.</para>
/// <para>The conformance witnesses are <c>conformance-suite/cases/13.18.38-OCCURS/pb3510-depending-on-*</c> (refused)
/// and <c>pb3510_depending_counters_admitted</c> (accepted, GR8 slices by an admitted counter).</para>
/// </summary>
public sealed class OdoDependingAdmissionDriftTests
{
    private static string CompilerSrc(params string[] parts) =>
        Path.Combine([TestRepo.Root, "src", "Cobol.Net.Compiler", .. parts]);

    /// <summary>Every assignment to an <c>OccursSpec.Depending</c>, member form or object initializer, is one of the two
    /// known writers, and the binder's is guarded by the SR17/SR2/SR20 admission.</summary>
    [Fact]
    [Trait("Sweep", "tree")]
    public void OccursSpecDepending_IsWrittenOnlyByTheAdmissionAndTheExtentFreeze()
    {
        var writers = new List<(string File, string Line)>();
        foreach (string file in Directory.EnumerateFiles(CompilerSrc(), "*.cs", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(file);
            foreach (Match m in Regex.Matches(text, @"^.*\.Depending\s*=(?![=>]).*$", RegexOptions.Multiline))
                writers.Add((Path.GetFileName(file), m.Value.Trim()));
            foreach (Match m in Regex.Matches(text, @"new\s+OccursSpec\s*\{[^}]*?\bDepending\s*=(?![=>])",
                         RegexOptions.Singleline))
                writers.Add((Path.GetFileName(file), m.Value.Trim()));
        }

        Assert.Equal(["DataBinder.Odo.cs", "SendingValueTemp.cs"], writers.Select(w => w.File).Order().ToArray());
        string admission = writers.Single(w => w.File == "DataBinder.Odo.cs").Line;
        Assert.Contains("!notInteger && !subscripted && !inTableRange", admission);
        Assert.Contains("clonedSpec.Depending = frozen", writers.Single(w => w.File == "SendingValueTemp.cs").Line);
    }

    public static TheoryData<string, string, string[]> Shapes => new()
    {
        // The fuzz reduction: the record that holds the table (SR17 + SR20).
        { "ancestor-record", """
            01 WS-G.
               05 WS-H.
                  10 WS-T OCCURS 1 TO 5 TIMES DEPENDING ON WS-G PIC X.
            """, ["COBOLNET0852", "COBOLNET0857"] },
        // An intermediate group between the record and the table.
        { "ancestor-group", """
            01 WS-G.
               05 WS-H.
                  10 WS-T OCCURS 1 TO 5 TIMES DEPENDING ON WS-H PIC X.
            """, ["COBOLNET0852", "COBOLNET0857"] },
        // fz01530 itself: the PICTURE'd "elementary" item is the group holding the table.
        { "ancestor-with-picture", """
            01 WS-G PIC S9(4).
               05 WS-T OCCURS 1 TO 5 TIMES DEPENDING ON WS-G PIC X.
            """, ["COBOLNET0852", "COBOLNET0857", "COBOLNET2191"] },
        // The table's own entry, elementary and group.
        { "table-itself", """
            01 WS-G.
               05 WS-H.
                  10 WS-T OCCURS 1 TO 5 TIMES DEPENDING ON WS-T PIC 9.
            """, ["COBOLNET0853", "COBOLNET0857"] },
        { "table-itself-group", """
            01 WS-G.
               05 WS-H.
                  10 WS-T OCCURS 1 TO 5 TIMES DEPENDING ON WS-T.
                     15 WS-E PIC 9.
            """, ["COBOLNET0852", "COBOLNET0853", "COBOLNET0857"] },
        // Two records each depending on the other: neither is in its table's range, both are groups (SR17).
        { "each-others-record", """
            01 WS-G.
               05 WS-T OCCURS 1 TO 5 TIMES DEPENDING ON WS-B PIC X.
            01 WS-B.
               05 WS-TB OCCURS 1 TO 5 TIMES DEPENDING ON WS-G PIC X.
            """, ["COBOLNET0852"] },
        // A group before the table in another record (SR17 only).
        { "group-elsewhere", """
            01 WS-K.
               05 WS-K1 PIC 9.
            01 WS-G.
               05 WS-T OCCURS 1 TO 5 TIMES DEPENDING ON WS-K PIC X.
            """, ["COBOLNET0852"] },
        // An integer after the table, in the same record (SR22 + SR20).
        { "integer-after", """
            01 WS-G.
               05 WS-T OCCURS 1 TO 5 TIMES DEPENDING ON WS-N PIC X.
               05 WS-N PIC 9.
            """, ["COBOLNET0856", "COBOLNET0857"] },
        // Legal: a counter inside the group before the table, and one in a record of its own.
        { "legal", """
            01 WS-G.
               05 WS-N PIC 9 VALUE 3.
               05 WS-T OCCURS 1 TO 5 TIMES DEPENDING ON WS-N PIC X.
            01 WS-C PIC 99 VALUE 2.
            01 WS-B.
               05 WS-TB OCCURS 1 TO 5 TIMES DEPENDING ON WS-C PIC X.
            """, [] },
    };

    /// <summary>Binding ends (a stack overflow would kill the test host), every recorded link is an elementary
    /// integer outside its table, and exactly the rules the shape violates report. The procedure division references
    /// the record as a group operand — the §13.18.38.4 GR8 slice that recursed — as a sender and a receiver.</summary>
    [Theory]
    [MemberData(nameof(Shapes))]
    public void EveryRecordedDependingLink_IsAnElementaryIntegerOutsideItsTable(string shape, string ws, string[] codes)
    {
        var (data, diagnostics) = Bind(ws);
        foreach (var table in data.Roots.SelectMany(Subtree).Where(i => i.OccursSpec is { DependingName: not null }))
        {
            if (table.OccursSpec!.Depending is not { } dep) continue;
            Assert.True(dep.IsElementary, $"{shape}: '{table.CobolName}' records the group '{dep.CobolName}'");
            Assert.True(dep.Pic is { IsIntegerDescription: true }, $"{shape}: '{dep.CobolName}' is not an integer");
            Assert.False(OdoModel.IsWithin(dep, table), $"{shape}: '{dep.CobolName}' lies within its table");
            Assert.False(OdoModel.IsWithin(table, dep), $"{shape}: '{dep.CobolName}' contains its table");
        }
        string[] reported = diagnostics.Select(d => Regex.Match(d, @"COBOLNET\d{4}").Value)
            .Where(c => c.Length > 0).Distinct().Order().ToArray();
        Assert.Equal(codes.Order().ToArray(), reported);
        if (shape == "legal")
            Assert.All(data.Roots.SelectMany(Subtree).Where(i => i.OccursSpec is { DependingName: not null }),
                t => Assert.NotNull(t.OccursSpec!.Depending));
    }

    private static IEnumerable<DataItem> Subtree(DataItem item) => [item, .. item.Children.SelectMany(Subtree)];

    private static (DataBinder Data, IReadOnlyList<string> Diagnostics) Bind(string ws)
    {
        string src = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ODA{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            {ws}
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY WS-G
                MOVE "A" TO WS-G
                STOP RUN.
            """;
        string path = Path.Combine(Path.GetTempPath(), "cn_oda_" + Guid.NewGuid().ToString("N")[..8] + ".cob");
        File.WriteAllText(path, src);
        try
        {
            var diags = new DiagnosticBag();
            var tree = new CnFrontend { InitialFormat = InitialReferenceFormat.Auto, DialectLevel = 2023 }.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            Assert.NotNull(tree);
            var edition = new EditionContext(2023);
            var bound = new BinderDriver().Bind(tree!, edition);
            return (bound.Units[0].Data, edition.Diagnostics);
        }
        finally { FileHarness.TryDelete(path); }
    }
}
