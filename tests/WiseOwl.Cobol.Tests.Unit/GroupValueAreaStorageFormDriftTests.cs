// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Frontend.Diagnostics;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;
using CnFrontend = WiseOwl.Cobol.Frontend.Frontend;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A GROUP VALUE AREA REACHES EVERY STORAGE FORM BENEATH IT (kb/Work PB5851). §2023:13.18.63.4 GR5: "the group area
/// is initialized without consideration for the individual elementary or group items contained within this group"
/// (§1985:VI.5.15.6 GR3 says the same at 1985). The defect was a silent fall-through: when the subtree held a
/// shared-storage member the record-struct lane (<c>GroupValueSlicer.ComposedInit</c>) took the member-wise default,
/// so one REDEFINES anywhere under the group, even a level down, lost the VALUE for EVERY member at every edition.
///
/// <para>Each case plants a group VALUE over ONE storage form the data model has and reads every member back; the
/// expected text is the area's positions under that member, worked from the rule by hand. Each case also states the
/// REDEFINES class it exercises (its tier and where the class is rooted), and the binder is asked whether the program
/// really builds that class — so a case cannot drift into testing a different form than it names. The coverage fact
/// then asks that every tier the model can store (<see cref="RedefinesTier"/> but <c>Rejected</c>) is planted rooted
/// at a MEMBER of the VALUE group, the shape of the defect, in each storage unit (character, national, bit): a new
/// tier or unit fails here until it has a case.</para>
///
/// <para>Two storage forms carry no VALUE-seeded initial state and are therefore not cases: a plain EXTERNAL record
/// (§2023:13.18.63.4 GR4 a — its VALUE takes effect only during INITIALIZE) and a BASED record, whose ALLOCATE
/// INITIALIZED content is that of INITIALIZE … ALL TO VALUE (§2023:14.9.3.4 GR7), which a group-level VALUE is not a
/// sender of (§2023:14.9.20.4 GR5 c) 1. b).</para>
/// </summary>
public sealed class GroupValueAreaStorageFormDriftTests : WiseOwlCobolTestBase
{
    /// <summary>Where a case's REDEFINES class is rooted relative to the group that carries the VALUE.</summary>
    public enum ClassRoot { None, TheValueGroup, AMember, AMemberOneLevelDown }

    /// <summary>The storage unit of the VALUE group's area.</summary>
    public enum AreaUnit { Character, National, Bit }

    /// <param name="ClassMember">The canonical of the REDEFINES class the case plants (null for the control).</param>
    public sealed record Case(string Id, int Edition, AreaUnit Unit, ClassRoot Root, string? ClassMember, RedefinesTier? Tier,
        string WorkingStorage, string LocalStorage, string Displays, string Expected);

    private static readonly Case[] Cases =
    [
        new("typed-members", 2023, AreaUnit.Character, ClassRoot.None, null, null,
            """
            01 G VALUE "AB12".
               05 A PIC XX.
               05 N PIC 99.
            """, "", """DISPLAY "[" A "][" N "]".""", "[AB][12]"),
        new("class-at-the-value-group", 2023, AreaUnit.Character, ClassRoot.TheValueGroup, "G", RedefinesTier.StringCanonical,
            """
            01 G VALUE "ABCD".
               05 A PIC XX.
               05 B PIC XX.
            01 R REDEFINES G PIC X(4).
            """, "", """DISPLAY "[" A "][" B "][" R "]".""", "[AB][CD][ABCD]"),
        new("alias-at-a-member", 2023, AreaUnit.Character, ClassRoot.AMember, "A", RedefinesTier.Alias,
            """
            01 G VALUE "ABCDEF".
               05 A PIC X(4).
               05 B REDEFINES A PIC X(4).
               05 C PIC XX.
            """, "", """DISPLAY "[" A "][" B "][" C "][" G "]".""", "[ABCD][ABCD][EF][ABCDEF]"),
        new("backing-at-a-member", 2023, AreaUnit.Character, ClassRoot.AMember, "N", RedefinesTier.StringCanonical,
            """
            01 G VALUE "1234EF".
               05 N PIC 9(4).
               05 X REDEFINES N PIC X(4).
               05 C PIC XX.
            """, "", """DISPLAY "[" N "][" X "][" C "][" G "]".""", "[1234][1234][EF][1234EF]"),
        new("backing-at-a-member-85", 85, AreaUnit.Character, ClassRoot.AMember, "N", RedefinesTier.StringCanonical,
            """
            01 G VALUE "1234EF".
               05 N PIC 9(4).
               05 X REDEFINES N PIC X(4).
               05 C PIC XX.
            """, "", """DISPLAY "[" N "][" X "][" C "][" G "]".""", "[1234][1234][EF][1234EF]"),
        new("backing-a-level-down", 2023, AreaUnit.Character, ClassRoot.AMemberOneLevelDown, "A", RedefinesTier.StringCanonical,
            """
            01 G VALUE "12CD34".
               05 H.
                  10 A PIC XX.
                  10 B REDEFINES A PIC 99.
               05 C PIC XX.
               05 N PIC 99.
            """, "", """DISPLAY "[" A "][" B "][" C "][" N "][" G "]".""", "[12][12][CD][34][12CD34]"),
        new("group-backing-at-a-member", 2023, AreaUnit.Character, ClassRoot.AMember, "H", RedefinesTier.StringCanonical,
            """
            01 G VALUE "PQRSTU".
               05 H.
                  10 H1 PIC XX.
                  10 H2 PIC XX.
               05 R REDEFINES H PIC X(4).
               05 C PIC XX.
            """, "", """DISPLAY "[" H1 "][" H2 "][" R "][" C "]".""", "[PQ][RS][PQRS][TU]"),
        new("table-in-the-redefined-member", 2023, AreaUnit.Character, ClassRoot.AMember, "H", RedefinesTier.StringCanonical,
            """
            01 G VALUE "ABCDEF".
               05 H.
                  10 HE PIC XX OCCURS 2.
               05 R REDEFINES H PIC X(4).
               05 C PIC XX.
            """, "", """DISPLAY "[" HE (1) "][" HE (2) "][" R "][" C "]".""", "[AB][CD][ABCD][EF]"),
        // GR12-GR14: a format 2 group VALUE initializes every occurrence's area; the class is in the element.
        new("per-occurrence-group-value", 2023, AreaUnit.Character, ClassRoot.AMember, "A", RedefinesTier.StringCanonical,
            """
            01 T.
               05 E OCCURS 2 VALUE "12YZ" FROM (1) TO (2).
                  10 A PIC XX.
                  10 B REDEFINES A PIC 99.
                  10 C PIC XX.
            """, "", """DISPLAY "[" A (1) "][" B (2) "][" C (2) "][" T "]".""", "[12][12][YZ][12YZ12YZ]"),
        new("local-storage", 2023, AreaUnit.Character, ClassRoot.AMember, "N", RedefinesTier.StringCanonical, "",
            """
            01 G VALUE "1234EF".
               05 N PIC 9(4).
               05 X REDEFINES N PIC X(4).
               05 C PIC XX.
            """, """DISPLAY "[" N "][" X "][" C "]".""", "[1234][1234][EF]"),
        new("national-alias-at-a-member", 2023, AreaUnit.National, ClassRoot.AMember, "NA", RedefinesTier.Alias,
            """
            01 NG GROUP-USAGE NATIONAL VALUE N"KLMN".
               05 NA PIC N(2).
               05 NB REDEFINES NA PIC N(2).
               05 NC PIC N(2).
            """, "", """DISPLAY "[" NA "][" NB "][" NC "]".""", "[KL][KL][MN]"),
        new("national-backing-at-a-member", 2023, AreaUnit.National, ClassRoot.AMember, "NA", RedefinesTier.StringCanonical,
            """
            01 NG GROUP-USAGE NATIONAL VALUE N"12MN".
               05 NA PIC N(2).
               05 NB REDEFINES NA PIC 99 USAGE NATIONAL.
               05 NC PIC N(2).
            """, "", """DISPLAY "[" NA "][" NB "][" NC "]".""", "[12][12][MN]"),
        // The national arm of the image lane: the area's characters are two bytes each in the backing (D-N1).
        new("national-class-at-the-value-group", 2023, AreaUnit.National, ClassRoot.TheValueGroup, "NH", RedefinesTier.StringCanonical,
            """
            01 NH GROUP-USAGE NATIONAL VALUE N"KL".
               05 H1 PIC N.
               05 H2 PIC N.
            01 R REDEFINES NH PIC X(4).
            """, "", """DISPLAY "[" H1 "][" H2 "][" NH "]".""", "[K][L][KL]"),
        new("bit-alias-at-a-member", 2023, AreaUnit.Bit, ClassRoot.AMember, "B1", RedefinesTier.Alias,
            """
            01 BG GROUP-USAGE BIT VALUE B"101101".
               05 B1 PIC 1(2).
               05 B2 REDEFINES B1 PIC 1(2).
               05 B3 PIC 1(4).
            """, "", """DISPLAY "[" B1 "][" B2 "][" B3 "]".""", "[10][10][1101]"),
        new("bit-backing-at-a-member", 2023, AreaUnit.Bit, ClassRoot.AMember, "B1", RedefinesTier.StringCanonical,
            """
            01 BG GROUP-USAGE BIT VALUE B"101101".
               05 B1 PIC 1(2).
               05 B2 REDEFINES B1 PIC 1(1).
               05 B3 PIC 1(4).
            """, "", """DISPLAY "[" B1 "][" B2 "][" B3 "]".""", "[10][1][1101]"),
    ];

    public static TheoryData<string> CaseIds => new(Cases.Select(c => c.Id));

    private static string Program(Case c) => $"""
               IDENTIFICATION DIVISION.
               PROGRAM-ID. GVSF.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
        {Indent(c.WorkingStorage)}
        {(c.LocalStorage.Length > 0 ? "       LOCAL-STORAGE SECTION.\n" + Indent(c.LocalStorage) : "")}
               PROCEDURE DIVISION.
               MAIN-PARA.
                   {c.Displays}
                   STOP RUN.
        """;

    private static string Indent(string lines) =>
        string.Join("\n", lines.Split('\n').Select(l => "       " + l.TrimEnd('\r')));

    [Theory]
    [MemberData(nameof(CaseIds))]
    public void EveryMemberStartsAsTheAreaPositionsOverIt(string id)
    {
        var c = Cases.Single(k => k.Id == id);
        var (ok, stdout, detail) = CompileAndRun(Program(c), c.Edition);
        Assert.True(ok, detail);
        Assert.Equal(c.Expected, stdout);
    }

    /// <summary>The case builds the class it names: the binder classifies <see cref="Case.ClassMember"/> as the canonical
    /// of a class of <see cref="Case.Tier"/>, rooted where <see cref="Case.Root"/> says.</summary>
    [Theory]
    [MemberData(nameof(CaseIds))]
    public void EachCasePlantsTheStorageFormItNames(string id)
    {
        var c = Cases.Single(k => k.Id == id);
        var data = Bind(Program(c), c.Edition);
        var all = data.Forest.Roots.SelectMany(DataItem.SelfAndDescendantsOf).ToList();
        var valueGroup = all.Single(DataBinder.HasGroupValue);
        if (c.ClassMember is null)
        {
            Assert.DoesNotContain(DataItem.SelfAndDescendantsOf(valueGroup), i => i.Class is not null);
            return;
        }
        var canonical = all.Single(i => i.CobolName == c.ClassMember);
        Assert.NotNull(canonical.Class);
        Assert.Same(canonical, canonical.Class!.Canonical);
        Assert.Equal(c.Tier, canonical.Class.Tier);
        int depth = 0;
        for (var a = canonical; a is not null && !ReferenceEquals(a, valueGroup); a = a.Parent) depth++;
        var root = ReferenceEquals(canonical, valueGroup) ? ClassRoot.TheValueGroup
            : depth == 1 ? ClassRoot.AMember : ClassRoot.AMemberOneLevelDown;
        Assert.Equal(c.Root, root);
    }

    [Fact]
    public void EveryStoredTierIsPlantedAtAMemberInEveryUnit()
    {
        var missing = new List<string>();
        foreach (var tier in Enum.GetValues<RedefinesTier>().Where(t => t is not RedefinesTier.Rejected))
            foreach (var unit in Enum.GetValues<AreaUnit>())
                if (!Cases.Any(c => c.Tier == tier && c.Unit == unit && c.Root is ClassRoot.AMember))
                    missing.Add($"{tier} x {unit}");
        foreach (var root in Enum.GetValues<ClassRoot>())
            if (!Cases.Any(c => c.Root == root)) missing.Add($"root {root}");
        Assert.True(missing.Count == 0,
            "A group VALUE area is not planted over these storage forms (kb/Work PB5851): " + string.Join(", ", missing));
    }

    private static DataBindResult Bind(string src, int edition)
    {
        string path = Path.Combine(Path.GetTempPath(), "cn_gvsf_" + Guid.NewGuid().ToString("N")[..8] + ".cob");
        File.WriteAllText(path, src);
        try
        {
            var diags = new DiagnosticBag();
            var frontend = new CnFrontend { InitialFormat = TestSourceFormat.Inline, DialectLevel = edition };
            var tree = frontend.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            var bound = new BinderDriver().Bind(tree!, new EditionContext(edition, permissive: false), frontend.Directives);
            return bound.Units.Single().Data;
        }
        finally { FileHarness.TryDelete(path); }
    }
}
