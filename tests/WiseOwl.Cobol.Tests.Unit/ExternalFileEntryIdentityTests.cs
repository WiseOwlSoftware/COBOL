// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Binding;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// kb/Work PB1079 — an external file connector's entry identity (<c>FileEntryIdentity.Of</c>, the string two
/// describers compare for EC-EXTERNAL-FILE-MISMATCH) is the WHOLE of ISO §2023:12.4.5.3 GR1: §2023:14.8.4.4 says "the rules
/// specified in 12.4.5, File control entry General rule 1 apply", and GR1 lists a) through m). The fingerprint used to
/// omit c) RECORD DELIMITER, d) RESERVE, g) COLLATING SEQUENCE and k)'s SUPPRESS WHEN phrase, so an entry differing
/// only in one of them activated with no condition. Each row below changes ONE clause-level GR1 item on an otherwise
/// identical entry and must change the identity; the key items' descriptions and relative locations (j/k), which need
/// a bound record, are held by the golden <c>wiseowl/cases/12.4.5-File-control/w68r_pb1079_external_entry_identity</c> (each called program its own compilation unit).
/// </summary>
public sealed class ExternalFileEntryIdentityTests
{
    private static FileModel Entry()
    {
        var f = new FileModel
        {
            CobolName = "XF", AssignTarget = "x.dat", Organization = FileOrganization.Indexed,
            OrganizationWritten = true, AccessMode = FileAccessMode.Dynamic, RecordKeyOperand = new WrittenDataName("XK"),
        };
        f.AlternateKeyNames.Add(new AlternateKeyClause { Operand = new WrittenDataName("XA"), Duplicates = true });
        return f;
    }

    /// <summary>A key-level clause naming one key by its bare data-name, unresolved (no record items in this model),
    /// so the fingerprint keys the operand by its spelling.</summary>
    private static KeyLevelCollatingClause KeyClause(string key, string alphabet) =>
        new([new CollatingKeyOperand { Operand = new WrittenDataName(key) }], alphabet, default);

    public static TheoryData<string, Action<FileModel>> Items => new()
    {
        { "a) OPTIONAL", f => f.Optional = true },
        { "b) ASSIGN literal-1", f => f.AssignTarget = "y.dat" },
        { "b) ASSIGN USING data-name-1", f => f.AssignUsingOperand = new WrittenDataName("FNAME") },
        // c) RECORD DELIMITER is not an equality item, so it has its own facts below (RecordDelimiter_*).
        { "d) RESERVE integer-1", f => f.ReserveAreas = 3 },
        { "e) organization", f => f.Organization = FileOrganization.Relative },
        { "f) access mode", f => f.AccessMode = FileAccessMode.Random },
        { "g) file-level COLLATING SEQUENCE", f => f.FileLevelCollating = ("REV", null) },
        { "g) key-level COLLATING SEQUENCE", f => f.KeyLevelCollating.Add(KeyClause("XA", "REV")) },
        { "k) DUPLICATES phrase", f => f.AlternateKeyNames[0] = new AlternateKeyClause { Operand = new WrittenDataName("XA"), Duplicates = false } },
        { "k) number of alternate record keys", f => f.AlternateKeyNames.Clear() },
        { "k) SUPPRESS WHEN phrase", f => f.AlternateKeyNames[0].SuppressWhen =
            new SuppressWhenOperand("\"XX\"", SuppressWhenForm.Literal, WiseOwl.Cobol.Common.LiteralClass.Alphanumeric, "XX", null) },
        // j)/k) "each data-name-6" / "each data-name-3": the SOURCE phrase's operands, in order, are the key (kb/Work
        // PB1099; review R1-3 found no witness that the identity carries them).
        { "j) RECORD KEY SOURCE phrase", f => f.RecordKeySource = Source("XK", "XB", "XC") },
        { "k) ALTERNATE RECORD KEY SOURCE phrase", f => f.AlternateKeyNames[0] =
            new AlternateKeyClause { Operand = new WrittenDataName("XA"), Duplicates = true, Source = Source("XA", "XB") } },
        { "l) sharing mode", f => f.Sharing = SharingMode.NoOther },
        { "m) lock mode", f => f.LockMode = new LockModeInfo(LockKind.Manual, false) },
    };

    /// <summary>Whether two entries agree on every GR1 item, each by its own relation (<c>FileEntrySegment.Agrees</c>): the
    /// question the compile-time check asks of each pair and the run time asks of the fingerprint and the RECORD DELIMITER field.</summary>
    private static bool Agree(FileModel a, FileModel b)
    {
        var x = FileEntryIdentity.Segments(a);
        var y = FileEntryIdentity.Segments(b);
        return x.Count == y.Count && x.Zip(y).All(p => p.First.Agrees(p.Second));
    }

    /// <summary>A record sequential entry writing <paramref name="delimiter"/> (null: no RECORD DELIMITER clause).</summary>
    private static FileModel Sequential(string? delimiter)
    {
        var f = new FileModel { CobolName = "SF", AssignTarget = "s.dat", RecordDelimiter = delimiter };
        if (delimiter == "LINE-SEQUENTIAL") { f.Organization = FileOrganization.LineSequential; f.LineSequentialByDelimiter = true; }
        return f;
    }

    /// <summary>An omitted RESERVE clause is the implementor's one area and an omitted RECORD DELIMITER clause the implementor's
    /// method, which RESERVE 1 AREA writes, BINARY-SEQUENTIAL names and the declined STANDARD-1 leaves in force (§1985:VII.2.9.3 GR1,
    /// §1985:VII.2.8.4 GR4; DOC-A.1-164, DOC-A.1-149, DOC-A.1-151), so the entries specify the same connector (kb/Work PB5311,
    /// PB5844); a clause that specifies something else still differs.</summary>
    [Theory]
    [InlineData("STANDARD-1")]
    [InlineData("BINARY-SEQUENTIAL")]
    public void An_omitted_clause_is_its_implementor_value(string delimiter)
    {
        var plain = Sequential(null);
        var written = Sequential(delimiter);
        written.ReserveAreas = 1;
        Assert.True(Agree(plain, written));
        Assert.True(Agree(written, plain));
        written.ReserveAreas = 3;
        Assert.False(Agree(plain, written));
    }

    /// <summary>§1985:VII.2.8.4 GR6: "all RECORD DELIMITER clauses in the run unit which are associated with that file connector
    /// must have the same specifications" (kb/Work PB5844, review R1-1). Two WRITTEN clauses agree only when they say the same
    /// thing: STANDARD-1 differs from each implementor-name, and the two implementor-names, which name two methods, differ.
    /// The pairs are disagreements in both orders, because the relation is checked pairwise in both directions.</summary>
    [Theory]
    [InlineData("STANDARD-1", "BINARY-SEQUENTIAL")]
    [InlineData("STANDARD-1", "LINE-SEQUENTIAL")]
    [InlineData("BINARY-SEQUENTIAL", "LINE-SEQUENTIAL")]
    public void RecordDelimiter_TwoWrittenArmsThatDiffer_Disagree(string a, string b)
    {
        Assert.False(Agree(Sequential(a), Sequential(b)), $"{a} against {b}");
        Assert.False(Agree(Sequential(b), Sequential(a)), $"{b} against {a}");
    }

    /// <summary>The same written arm agrees with itself; the relation is not transitive (omitted agrees with both STANDARD-1 and
    /// BINARY-SEQUENTIAL, which disagree), which is why the compile-time check compares every pair.</summary>
    [Theory]
    [InlineData("STANDARD-1")]
    [InlineData("BINARY-SEQUENTIAL")]
    [InlineData("LINE-SEQUENTIAL")]
    public void RecordDelimiter_TheSameWrittenArm_Agrees(string a) => Assert.True(Agree(Sequential(a), Sequential(a)));

    /// <summary>The run time asks the same relation (separately compiled programs, EC-EXTERNAL-FILE-MISMATCH): an entry that
    /// writes no clause registers beside STANDARD-1 and beside BINARY-SEQUENTIAL, and those two refuse each other.</summary>
    [Fact]
    public void RecordDelimiter_TheRunTimeCheck_IsPairwise()
    {
        var table = new WiseOwl.Cobol.Runtime.ExternalTable();
        var gate = WiseOwl.Cobol.Runtime.Exceptions.ExternalChecks.FileMismatch;
        WiseOwl.Cobol.Runtime.ExternalDescriptor Desc(string? delimiter) => new("file", SelectFingerprint: "F", RecordDelimiter: delimiter);
        table.Describe("P0", "XF", Desc(null), gate);
        table.Describe("P1", "XF", Desc("STANDARD-1"), gate);
        var x = Assert.Throws<WiseOwl.Cobol.Runtime.CobolCallException>(() => table.Describe("P2", "XF", Desc("BINARY-SEQUENTIAL"), gate));
        Assert.Contains("EC-EXTERNAL-FILE-MISMATCH", x.Message, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(Items))]
    public void Each_GR1_item_is_part_of_the_identity(string item, Action<FileModel> change)
    {
        var changed = Entry();
        change(changed);
        Assert.True(FileEntryIdentity.Of(Entry()) != FileEntryIdentity.Of(changed),
            $"§2023:12.4.5.3 GR1 {item}: an entry differing only in it has the same identity '{FileEntryIdentity.Of(changed)}'");
        Assert.False(Agree(Entry(), changed), $"§2023:12.4.5.3 GR1 {item}: an entry differing only in it agrees");
    }

    [Fact]
    public void Identical_entries_have_one_identity()
        => Assert.Equal(FileEntryIdentity.Of(Entry()), FileEntryIdentity.Of(Entry()));

    [Fact]
    public void Key_level_collating_is_compared_by_key_position_not_clause_order()
    {
        // GR1 g) "The same specification of COLLATING SEQUENCE clauses": the same key-to-alphabet assignments written
        // as two clauses in either order specify the same thing.
        var a = Entry(); a.KeyLevelCollating.Add(KeyClause("XK", "ALPHA1")); a.KeyLevelCollating.Add(KeyClause("XA", "ALPHA2"));
        var b = Entry(); b.KeyLevelCollating.Add(KeyClause("XA", "alpha2")); b.KeyLevelCollating.Add(KeyClause("XK", "alpha1"));
        Assert.Equal(FileEntryIdentity.Of(a), FileEntryIdentity.Of(b));
    }

    /// <summary>A SOURCE phrase as written, its operands unresolved (no record items in this model), so the
    /// fingerprint keys each operand by its spelling.</summary>
    private static RecordKeySource Source(string recordKeyName, params string[] operands) => new()
    {
        Name = recordKeyName,
        Operands = [.. operands.Select(o => new KeySourceOperand { Operand = new WrittenDataName(o) })],
    };

    [Fact]
    public void Source_operand_order_is_part_of_the_identity()
    {
        // GR1 j) "each data-name-6 as well as their relative location": the key IS the concatenation in the order
        // specified (§2023:12.4.5.12.4 GR2), so XB XC and XC XB are different keys.
        var a = Entry(); a.RecordKeySource = Source("XK", "XB", "XC");
        var b = Entry(); b.RecordKeySource = Source("XK", "XC", "XB");
        Assert.NotEqual(FileEntryIdentity.Of(a), FileEntryIdentity.Of(b));
    }
}
