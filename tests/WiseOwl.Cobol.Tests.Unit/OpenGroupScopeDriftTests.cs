// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.CodeGen;
using WiseOwl.Cobol.Frontend.Diagnostics;
using WiseOwl.Cobol.Frontend.Preprocessor;
using Xunit;
using WiseOwl.Cobol.Tests.Shared;
using CnFrontend = WiseOwl.Cobol.Frontend.Frontend;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ISO §14.9.27.2's REPEATED GROUP kept structural (kb/Work PB316). The OPEN statement's general format has an
/// outer brace pair — verified against the printed page — around the whole group
/// <c>{open-mode} [sharing-phrase] [retry-phrase] {file-name-1 [WITH NO REWIND]} …</c>, carrying its own
/// trailing ellipsis; §14.9.27.4 GR20 then makes the statement equal to "a separate OPEN statement … for each
/// file-name … each hav[ing] the same open mode specification, the sharing-phrase, retry-phrase, and REWIND
/// phrase as specified in the OPEN statement". The open mode in that list is unarguably per group, so the
/// phrases beside it are too.
///
/// <para>⛔ WHAT THIS GATE EXISTS TO CATCH, AND WHY A CONFORMANCE GOLDEN CANNOT. The defect was a SCALAR
/// standing in for a per-group value: <c>BoundOpen</c> carried <c>SharingOverride</c> and <c>Retry</c> as
/// statement properties, so one group's phrase governed every file — and the next phrase added to the format
/// (the NO REWIND phrase, kb/Work PB317; the per-file-name syntax rules, kb/Work PB318) would have been added
/// the same way, because that was the shape on offer. <c>conformance:cases/14.9.27-OPEN/pb316_open_group_scope</c> proves the
/// SHARING phrase is scoped correctly TODAY (its RETRY parts stopped discriminating once every OPEN was arbitrated,
/// kb/Work PB675: <see cref="EachOpenEntry_CarriesOnlyItsOwnGroupsPhrases"/> is the RETRY phrase's witness); only
/// a check on the SHAPE makes the next phrase automatic.
/// The rule enforced here is exact: the statement node carries nothing but its file list, and every per-group
/// value lives on <see cref="BoundOpenFile"/>.</para>
///
/// <para>⛔ <see cref="ThePredicate_ActuallyFails_OnAStatementScopedPhrase"/> is not a formality
/// (<c>feedback_green_gates_arent_evidence</c>): this gate will spend its life green, and a green gate that
/// never looked at anything is indistinguishable from one that works. It drives the same pure function over a
/// fabricated node shaped like the pre-PB316 <c>BoundOpen</c>, and over one shaped like today's, so the failure
/// proves discrimination rather than blanket rejection.</para>
/// </summary>
public sealed class OpenGroupScopeDriftTests
{
    /// <summary>THE PREDICATE. The public instance properties of a statement node other than its repeated-group
    /// list — every one of them is a value scoped to the whole statement, which for OPEN is exactly what
    /// §14.9.27.2 forbids.</summary>
    private static string[] StatementScopedMembers(Type statement, string groupListProperty) =>
        [.. statement.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            // The BoundStatement base's own members (OperandEvaluation, kb/Work PB1432) belong to every node, not
            // to this statement's phrase set.
            .Where(p => p.DeclaringType != typeof(BoundStatement))
            .Where(p => p.Name != groupListProperty)
            .Select(p => p.Name)
            .Order()];

    /// <summary>The statement node holds the repeated group and nothing else — no phrase may be hoisted out of
    /// a group onto the statement (§14.9.27.2; §14.9.27.4 GR20).</summary>
    [Fact]
    public void BoundOpen_CarriesTheGroupListAndNothingElse()
    {
        Assert.Equal([], StatementScopedMembers(typeof(BoundOpen), nameof(BoundOpen.Files)));
    }

    /// <summary>The group list's ELEMENT is the per-file-name record — GR20's own normal form, one entry per
    /// file-name rather than a nested group whose members a consumer could re-widen.</summary>
    [Fact]
    public void BoundOpen_Files_IsAListOfPerFileNameEntries()
    {
        Type files = typeof(BoundOpen).GetProperty(nameof(BoundOpen.Files))!.PropertyType;
        Assert.True(files.IsGenericType, $"BoundOpen.Files is {files.Name}, not a generic list");
        Assert.Equal(typeof(IReadOnlyList<>), files.GetGenericTypeDefinition());
        Assert.Equal(typeof(BoundOpenFile), files.GetGenericArguments()[0]);
    }

    /// <summary>Every phrase the general format nests inside the repeated group has a home ON THE ENTRY, one
    /// property each: the open mode (§14.9.27.2's inner braces), the sharing-phrase (§14.9.27.4 GR22/GR23) and
    /// the retry-phrase (§14.7.9). Each is asserted by TYPE, not by name, so renaming a property cannot quietly
    /// remove a phrase's carrier.</summary>
    [Theory]
    [InlineData(typeof(BoundOpenMode))]
    [InlineData(typeof(SharingMode?))]
    [InlineData(typeof(RetrySpec))]
    public void BoundOpenFile_CarriesOneSlotPerGroupScopedPhrase(Type phrase)
    {
        var carriers = typeof(BoundOpenFile).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == phrase)
            .Select(p => p.Name)
            .ToArray();
        Assert.True(carriers.Length == 1,
            $"BoundOpenFile carries {carriers.Length} properties of type {phrase.Name} "
            + $"({string.Join(", ", carriers)}); §14.9.27.2 gives each group exactly one.");
    }

    // ── the bound shape, observed: each file-name's entry carries ITS OWN group's phrases ────────────────

    /// <summary>The four file connectors every fixture below opens. None carries a SHARING or LOCK MODE clause, so
    /// the only sharing an entry can show is the one its OPEN group wrote (§14.9.27.4 GR23).</summary>
    private const string Fixture = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. PB675OGS.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT F-SEED ASSIGN TO "pb675a.dat" ORGANIZATION IS SEQUENTIAL.
            SELECT F-E ASSIGN TO "pb675b.dat" ORGANIZATION IS SEQUENTIAL.
            SELECT F-F ASSIGN TO "pb675c.dat" ORGANIZATION IS SEQUENTIAL.
            SELECT F-G ASSIGN TO "pb675d.dat" ORGANIZATION IS SEQUENTIAL.
        DATA DIVISION.
        FILE SECTION.
        FD F-SEED.
        01 SEED-REC PIC X(5).
        FD F-E.
        01 E-REC PIC X(5).
        FD F-F.
        01 F-REC PIC X(5).
        FD F-G.
        01 G-REC PIC X(5).
        PROCEDURE DIVISION.
        MAIN.
            {open}.
            STOP RUN.
        """;

    /// <summary>Bind <paramref name="openStatement"/> at COBOL-2023 (the SHARING phrase is a 2002 introduction) and
    /// return the statement's per-file-name entries in source order — the implicit-statement series flattened, which
    /// is GR20's own normal form. ⛔ Asserting that the bind produced exactly the entries the source names is what
    /// stops a binder that dropped a group, or stopped binding the statement at all, from reading as a pass.</summary>
    private static List<BoundOpenFile> BindOpenEntries(string openStatement)
    {
        string path = Path.Combine(Path.GetTempPath(), $"pb675ogs_{Guid.NewGuid():N}.cob");
        File.WriteAllText(path, Fixture.Replace("{open}", openStatement, StringComparison.Ordinal));
        try
        {
            var diags = new DiagnosticBag();
            var frontend = new CnFrontend { InitialFormat = InitialReferenceFormat.Auto, DialectLevel = 2023 };
            var tree = frontend.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            var edition = new EditionContext(2023);
            var bound = new CSharpEmitter().Bind(tree!, edition, frontend.Directives);
            Assert.Empty(edition.Diagnostics);
            var entries = new List<BoundOpenFile>();
            foreach (BoundStatement statement in bound.Units.SelectMany(u => u.Bound.Paragraphs).SelectMany(p => p.Statements))
            {
                Collect(statement, entries);
            }

            return entries;
        }
        finally { FileHarness.TryDelete(path); }
    }

    private static void Collect(BoundStatement statement, List<BoundOpenFile> into)
    {
        switch (statement)
        {
            case BoundImplicitSeries series:
                foreach (BoundStatement member in series.Members) { Collect(member, into); }
                break;
            case BoundOpen open:
                into.AddRange(open.Files);
                break;
        }
    }

    /// <summary>⛔ THE WITNESS PB316's GOLDEN CANNOT BE FOR THE RETRY PHRASE (kb/Work PB675). Once every OPEN is
    /// arbitrated against Table 19 (kb/Work PB321) a RETRY phrase leaking from one group into its sibling no longer
    /// moves any printed I-O status — the leaked phrase only routes the sibling through the phrase-bearing runtime
    /// entry, whose posture equals the one an unregistered connector already arbitrates under — so
    /// <c>conformance:cases/14.9.27-OPEN/pb316_open_group_scope</c> parts (1) and (2) are green whether or not the
    /// phrase leaks. The defect PB316 fixed lives in the BOUND TREE, so it is asserted there: each entry of the
    /// statement carries the retry-phrase and sharing-phrase of the group that wrote it (§14.9.27.4 GR20: "These
    /// separate OPEN statements would each have the same open mode specification, the sharing-phrase,
    /// retry-phrase, and REWIND phrase as specified in the OPEN statement"). A leak runs forward (a group inherits
    /// an earlier group's phrase) under one hoisting and backward (a statement-scope phrase reaches an earlier
    /// group) under another, so the rows split the two directions: a phrase-less file AFTER a phrase-bearing group
    /// witnesses the forward leak (rows 2 to 7, counting from 1), a phrase-less file BEFORE one witnesses the
    /// backward leak (rows 1, 3, 6 and 7), and rows 3, 6 and 7 carry both. Row 1 alone does not catch a
    /// forward-only leak and rows 2, 4 and 5 alone do not catch a backward-only one: trim the theory only to rows
    /// that keep both directions.</summary>
    [Theory]
    [InlineData("OPEN INPUT F-SEED INPUT RETRY 2 TIMES F-E",
        new[] { "F-SEED||", "F-E|Times|" })]
    [InlineData("OPEN INPUT RETRY 2 TIMES F-E INPUT F-SEED",
        new[] { "F-E|Times|", "F-SEED||" })]
    [InlineData("OPEN INPUT F-SEED INPUT RETRY FOREVER F-E INPUT F-F",
        new[] { "F-SEED||", "F-E|Forever|", "F-F||" })]
    [InlineData("OPEN INPUT RETRY 2 TIMES F-E F-F INPUT F-SEED",
        new[] { "F-E|Times|", "F-F|Times|", "F-SEED||" })]
    [InlineData("OPEN INPUT RETRY 2 TIMES F-E INPUT RETRY FOREVER F-F INPUT F-SEED",
        new[] { "F-E|Times|", "F-F|Forever|", "F-SEED||" })]
    [InlineData("OPEN INPUT F-SEED INPUT SHARING WITH NO OTHER RETRY 2 TIMES F-E INPUT F-F",
        new[] { "F-SEED||", "F-E|Times|NoOther", "F-F||" })]
    [InlineData("OPEN INPUT SHARING WITH READ ONLY F-E INPUT RETRY FOREVER F-F INPUT F-SEED",
        new[] { "F-E||ReadOnly", "F-F|Forever|", "F-SEED||" })]
    public void EachOpenEntry_CarriesOnlyItsOwnGroupsPhrases(string open, string[] expected)
    {
        var entries = BindOpenEntries(open);

        // file | RETRY kind (empty for none) | SHARING mode (empty for none)
        Assert.Equal(expected, entries.Select(e => $"{e.File.SelectName}|{e.Retry?.Kind}|{e.Sharing}"));
    }

    // ── the discrimination proof ─────────────────────────────────────────────────────────────────────

    /// <summary>The pre-PB316 shape: the phrases hoisted onto the statement, where one group's SHARING governed
    /// every file in it.</summary>
    private sealed record HoistedOpen(IReadOnlyList<BoundOpenFile> Files)
    {
        public SharingMode? SharingOverride { get; init; }
        public RetrySpec? Retry { get; init; }
    }

    /// <summary>Today's shape, fabricated, so the predicate is shown to ACCEPT as well as reject.</summary>
    private sealed record GroupScopedOpen(IReadOnlyList<BoundOpenFile> Files);

    /// <summary>⛔ Drive the predicate over both fabricated nodes: it names the hoisted phrases and passes the
    /// group-scoped node. Without this, a predicate that returned an empty array unconditionally would look
    /// exactly like a working gate.</summary>
    [Fact]
    public void ThePredicate_ActuallyFails_OnAStatementScopedPhrase()
    {
        Assert.Equal(["Retry", "SharingOverride"],
            StatementScopedMembers(typeof(HoistedOpen), nameof(HoistedOpen.Files)));
        Assert.Equal([], StatementScopedMembers(typeof(GroupScopedOpen), nameof(GroupScopedOpen.Files)));
    }
}
