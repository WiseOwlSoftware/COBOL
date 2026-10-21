// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using CobolNet.Runtime;
using CobolNet.Runtime.IO;
using CobolNet.Runtime.IO.Store;
using Xunit;
using static CobolNet.Tests.Shared.FileHarness;
using static CobolNet.Tests.Unit.Store.TransactionTrees;

namespace CobolNet.Tests.Unit.Store;

/// <summary>
/// ⛔ THE TRANSACTIONS' HOLDINGS ARE THE FILE'S KEYS, NOT THEIR BYTES (DESIGN-runtime-library.md 2.2.1.10, in
/// docs/rearchitecture; kb/Work PB3485). An INDEXED file whose collating sequence makes two key images one key (here
/// <c>ALPHABET … ALSO</c>: every upper-case letter shares its position with its lower-case twin) has records whose
/// identity no byte compare can decide, and every expected value below is the standard's:
/// <list type="bullet">
///   <item>§12.4.5.12.4 GR1: <i>"The equality or inequality is based on the collating sequence used for the file according
///   to the rules for a relation condition."</i> — "ABCD" and "abcd" are the SAME prime key, so one record, one holding;</item>
///   <item>§14.9.51.4 GR35: <i>"The comparison used for ensuring uniqueness, for ordering records, or for suppressing
///   records in the physical file is based on the collating sequence for the file according to the rules for a relation
///   condition."</i> — the same for a unique alternate key's value (the '22' of a duplicate, the '51' of a value a change
///   freed);</item>
///   <item>§9.1.16: <i>"The implementor may specify circumstances other than a locked logical record that result in the
///   return of a locked record status."</i> — a record a live transaction changed answers '51' to every other connector,
///   by whichever image of its key the other connector names it (DOC-A.1-152);</item>
///   <item>§14.9.36.4 GR3: <i>"All files and data-items referenced in active APPLY COMMIT clauses are restored to the state
///   they were in at the execution of the last COMMIT statement or the start of the run unit if no COMMIT statements had
///   previously been executed."</i> — whichever images the run unit's changes named the record by, and the record's own
///   key image is back as it was.</item>
/// </list>
/// The abnormal paths are the transaction machinery's own, run under that sequence: a dead owner rolled back by the next
/// writer and by the next OPEN, a COMMIT refused at every operation it makes (its reversal included), and the order itself.
/// </summary>
[Trait("Host", "ReFS")]
public sealed class HoldingsKeyOrderTests
{
    private static readonly IOException Refusal = new("refused by the host");

    /// <summary><c>ALPHABET … "A" ALSO "a" … "Z" ALSO "z"</c>: each letter shares one collating position with its
    /// lower-case twin (§12.3.7.4 GR7 ALSO), so "ABCD" and "abcd" are equal keys.</summary>
    internal static AlphanumericCollation CaseFold()
    {
        var codes = new List<ushort>();
        var positions = new List<ushort>();
        for (int i = 0; i < 26; i++) { codes.Add((ushort)('A' + i)); positions.Add((ushort)i); }
        for (int i = 0; i < 26; i++) { codes.Add((ushort)('a' + i)); positions.Add((ushort)i); }
        var representatives = Enumerable.Range(0, 26).Select(i => (ushort)('A' + i)).ToArray();
        return new AlphanumericCollation([.. codes], [.. positions], representatives, 26);
    }

    /// <summary>An 8-character record: the prime key in positions 1-4 and a unique alternate key in 5-8, both under
    /// <see cref="CaseFold"/>.</summary>
    private static FileRegistry Open(string host, bool commit, FileOpenMode mode = FileOpenMode.IO, string name = "F",
        Func<IPageMedium, IPageMedium>? medium = null)
    {
        var reg = new FileRegistry();
        reg.RegisterIndexed(name, host, recordWidth: 8, optional: false, accessMode: (int)KeyedAccess.Dynamic,
            primeKey: [new(0, 4)], varyMin: -1, varyMax: -1, primeCollation: CaseFold());
        reg.AddAlternateKey(name, [new(4, 4)], duplicates: false, collation: CaseFold());
        reg.RegisterSharing(name, FileSharing.AllOther, LockModeRequiredBy(FileSharing.AllOther), multiple: false);
        if (commit) reg.RegisterCommitControl(name);
        KeyedStore.MediumObserver.Value = medium;
        try { reg.OpenStatic(name, mode); }
        finally { KeyedStore.MediumObserver.Value = null; }
        Assert.Equal("00", reg.Status(name));
        return reg;
    }

    private static string Write(FileRegistry reg, string image, string name = "F") =>
        reg.WriteShared(name, image, -1, FileRecordLock.None, FileRetryKind.None, 0, page: null);

    private static string Rewrite(FileRegistry reg, string image, string name = "F") =>
        reg.RewriteShared(name, image, -1, FileRecordLock.None, FileRetryKind.None, 0);

    private static string Delete(FileRegistry reg, string key, string name = "F") =>
        reg.DeleteShared(name, key + "    ", FileRetryKind.None, 0);

    private static string Read(FileRegistry reg, string key, out string image, bool ignoringLock = false,
        FileRecordLock phrase = FileRecordLock.None, string name = "F") =>
        reg.ReadKeyedShared(name, keyIndex: -1, key + "    ", phrase, ignoringLock, FileRetryKind.None, 0, out image);

    private static string ReadByAlternate(FileRegistry reg, string value, out string image) =>
        reg.ReadKeyedShared("F", keyIndex: 0, "    " + value, FileRecordLock.None, ignoringLock: false, FileRetryKind.None, 0,
            out image);

    /// <summary>Two records, committed: "abcd" (lower case) with the unique alternate value "wxyz", "EFGH" with "ijkl".</summary>
    private static void Seed(string host)
    {
        var seed = Open(host, commit: false, FileOpenMode.Output);
        Assert.Equal("00", Write(seed, "abcdwxyz"));
        Assert.Equal("00", Write(seed, "EFGHijkl"));
        seed.Close("F");
        Assert.Equal("00", seed.Status("F"));
    }

    /// <summary>The prime tree's keys as a fresh INPUT run unit reads them: the spelling each record is STORED under, in
    /// the tree's order.</summary>
    private static string[] StoredKeys(string host)
    {
        var reader = Open(host, commit: false, FileOpenMode.Input, name: "R");
        var file = reader.PagedStoreOf("R")!;
        var keys = new List<string>();
        Assert.Equal(StoreStatus.Ok, new BTree(file, KeyOrders.Ordinal, file.Meta.PrimeRoot).First(out var cursor));
        while (cursor.HasCurrent)
        {
            keys.Add(Encoding.Latin1.GetString(cursor.Key));
            Assert.Equal(StoreStatus.Ok, cursor.Next());
        }
        reader.CloseAll();
        return [.. keys];
    }

    /// <summary>What a fresh run unit's WRITABLE OPEN (whose dead-owner step rolls back every transaction left without a
    /// live owner) reads of the three keys the scenarios use (absent: null), with no transaction left in the file.</summary>
    private static string?[] Settled(string host)
    {
        var reader = Open(host, commit: false, name: "S");
        var records = new string?[3];
        for (int i = 0; i < 3; i++)
            records[i] = Read(reader, new[] { "ABCD", "efgh", "mnop" }[i], out var image, name: "S") == "00" ? image : null;
        AssertNoTransactionLeft(reader, "S");
        reader.CloseAll();
        return records;
    }

    private static readonly string?[] SeededRecords = ["abcdwxyz", "EFGHijkl", null];

    /// <summary>The transaction every fault row runs: it DELETEs "abcd" by the image "ABCD", REWRITEs "EFGH" by "efgh" (freeing
    /// its unique value "ijkl") and WRITEs "MNOP".</summary>
    private static void Change(FileRegistry a)
    {
        Assert.Equal("00", Delete(a, "ABCD"));
        Assert.Equal("00", Rewrite(a, "efghqqqq"));
        Assert.Equal("00", Write(a, "MNOPmnop"));
        Assert.NotEqual(0ul, a.TransactionOf("F"));
        AssertHoldingsAreTheUndoKeys(a);
    }

    private static readonly string?[] CommittedRecords = [null, "efghqqqq", "MNOPmnop"];

    // ── The lock test: a holding is the file's key, whichever image names it ────────────────────────────────────────

    /// <summary>§12.4.5.12.4 GR1, §9.1.16 (finding R2-3, R4-1): A under commit control DELETEs "abcd" by the image "ABCD";
    /// B, outside commit control, meets the holding by EVERY image of the key — a WRITE answers '51', a READ finds the
    /// record gone ('23', the invalid key before any lock). A's ROLLBACK restores it, and B's WRITE then answers '22' (the
    /// key is taken) and its READ finds the record, which keeps the image it was WRITTEN with.</summary>
    [Fact]
    public void A_deleted_record_is_held_against_every_image_of_its_key()
    {
        string host = Tmp("pb3485-delete");
        try
        {
            Seed(host);
            var a = Open(host, commit: true);
            var b = Open(host, commit: false);
            Assert.Equal("00", Delete(a, "ABCD"));
            AssertHoldingsAreTheUndoKeys(a);
            foreach (var image in new[] { "abcd", "ABCD", "aBcD" })
            {
                Assert.Equal("51", Write(b, image + "new1"));
                Assert.Equal("23", Read(b, image, out _));   // the record is gone: GR3's invalid key, before any lock
            }
            Assert.Null(a.Rollback());
            AssertNoTransactionLeft(a);
            Assert.Equal("22", Write(b, "ABCDnew1"));
            Assert.Equal("00", Read(b, "AbCd", out var restored));
            Assert.Equal("abcdwxyz", restored);
            a.CloseAll();
            b.CloseAll();
            Assert.Equal(["abcd", "EFGH"], StoredKeys(host));   // the record is back under the image it was written with
        }
        finally { TryDelete(host); }
    }

    /// <summary>§12.4.5.12.4 GR1: A REWRITEs the record "abcd" by the image "ABCD" and B's REWRITE, DELETE and WRITE by any
    /// image of the key answer '51'; the record's key image in the tree is the one it was WRITTEN with, not the one A's
    /// statement named it by.</summary>
    [Fact]
    public void A_rewritten_record_is_held_against_every_image_of_its_key()
    {
        string host = Tmp("pb3485-rewrite");
        try
        {
            Seed(host);
            var a = Open(host, commit: true);
            var b = Open(host, commit: false);
            Assert.Equal("00", Rewrite(a, "ABCD1111"));
            AssertHoldingsAreTheUndoKeys(a);
            foreach (var image in new[] { "abcd", "ABCD", "aBcD" })
            {
                Assert.Equal("51", Rewrite(b, image + "2222"));
                Assert.Equal("51", Delete(b, image));
                Assert.Equal("51", Read(b, image, out _));
            }
            Assert.Equal("00", Read(b, "abcd", out var seen, ignoringLock: true));   // the file as it stands: A's change
            Assert.Equal("ABCD1111", seen);
            Assert.Null(a.Rollback());
            Assert.Equal("00", Rewrite(b, "aBcD3333"));   // nothing is held any more
            a.CloseAll();
            b.CloseAll();
            Assert.Equal(["abcd", "EFGH"], StoredKeys(host));
        }
        finally { TryDelete(host); }
    }

    /// <summary>§14.9.51.4 GR35 (finding R2-4): A REWRITEs "abcd" and frees its unique alternate value "wxyz". B's WRITE of
    /// that value in any image answers '51' while A lives, and '22' (the value is taken again) after A's ROLLBACK; after A's
    /// COMMIT the value is free, and B's WRITE of it answers '00'.</summary>
    [Fact]
    public void A_unique_alternate_value_a_change_freed_is_held_in_every_image()
    {
        string host = Tmp("pb3485-value");
        try
        {
            Seed(host);
            var a = Open(host, commit: true);
            var b = Open(host, commit: false);
            Assert.Equal("00", Rewrite(a, "abcdqqqq"));
            AssertHoldingsAreTheUndoKeys(a);
            Assert.Equal("51", Write(b, "MNOPWXYZ"));
            Assert.Equal("51", Write(b, "MNOPwXyZ"));
            Assert.Equal("22", Write(b, "MNOPqqqq"));   // A's new value is taken by a record of the file (GR35)
            Assert.Null(a.Rollback());
            Assert.Equal("22", Write(b, "MNOPWXYZ"));
            Assert.Equal("00", ReadByAlternate(b, "WxYz", out var byValue));
            Assert.Equal("abcdwxyz", byValue);
            Assert.Equal("00", Rewrite(a, "abcdqqqq"));
            Assert.Null(a.Commit());
            AssertNoTransactionLeft(a);
            Assert.Equal("00", Write(b, "MNOPWXYZ"));
            a.CloseAll();
            b.CloseAll();
        }
        finally { TryDelete(host); }
    }

    /// <summary>§14.9.36.4 GR3: the FIRST before-image is the one a ROLLBACK restores, whichever images the transaction's
    /// later changes of the record named it by. A REWRITEs the record "abcd" by "ABCD" and then by "abcd": one holding and
    /// one before-image, so the ROLLBACK restores the record as it was at the start of the run unit, never as A's first
    /// change left it, and its key image is the stored one.</summary>
    [Fact]
    public void A_record_changed_under_two_images_of_its_key_is_restored_to_its_first_image()
    {
        string host = Tmp("pb3485-twice");
        try
        {
            Seed(host);
            var a = Open(host, commit: true);
            Assert.Equal("00", Rewrite(a, "ABCD1111"));
            Assert.Equal("00", Rewrite(a, "abcd2222"));
            Assert.Equal("00", Delete(a, "aBCd"));
            Assert.Equal("00", Write(a, "AbCdnew1"));   // the key is free again for A itself
            AssertHoldingsAreTheUndoKeys(a);
            Assert.Null(a.Rollback());
            AssertNoTransactionLeft(a);
            Assert.Equal("00", Read(a, "ABCD", out var restored));
            Assert.Equal("abcdwxyz", restored);
            Assert.Equal("00", ReadByAlternate(a, "WXYZ", out var byValue));
            Assert.Equal("abcdwxyz", byValue);
            Assert.Equal("23", ReadByAlternate(a, "1111", out _));
            a.CloseAll();
            Assert.Equal(["abcd", "EFGH"], StoredKeys(host));
            Assert.Equal(SeededRecords, Settled(host));
        }
        finally { TryDelete(host); }
    }

    /// <summary>§9.1.16 (kb/Work PB3485): the record lock a READ WITH LOCK takes is the record's, not its key image's. B,
    /// another run unit, READs "abcd" WITH LOCK; A's READ WITH LOCK, REWRITE and DELETE by the images "ABCD" and "aBcD" find
    /// it locked ('51'); B's own REWRITE by another image succeeds, and the lock goes with its COMMIT-less UNLOCK.</summary>
    [Fact]
    public void A_record_lock_is_the_records_whichever_image_of_its_key_names_it()
    {
        string host = Tmp("pb3485-lock");
        try
        {
            Seed(host);
            var a = Open(host, commit: false);
            var b = Open(host, commit: false);
            Assert.Equal("00", Read(b, "abcd", out _, phrase: FileRecordLock.WithLock));
            foreach (var image in new[] { "ABCD", "aBcD", "abcd" })
            {
                Assert.Equal("51", Read(a, image, out _, phrase: FileRecordLock.WithLock));
                Assert.Equal("51", Rewrite(a, image + "9999"));
                Assert.Equal("51", Delete(a, image));
            }
            Assert.Equal("00", Rewrite(b, "ABCD9999"));   // the lock holder's own REWRITE, by another image of the key
            b.CloseAll();
            Assert.Equal("00", Read(a, "ABCD", out var image9, phrase: FileRecordLock.WithLock));
            Assert.Equal("ABCD9999", image9);
            a.CloseAll();
        }
        finally { TryDelete(host); }
    }

    // ── The abnormal paths, under that sequence ─────────────────────────────────────────────────────────────────────

    /// <summary>§9.1.18.5 (finding R3-1): A changes the file by the variant images and dies. B's WRITE of "abcd" by an
    /// image A never named finds the holding, rolls A back, and evaluates the WRITE again: '22', never '00'; the record is
    /// as at the start of the run unit under its stored image.</summary>
    [Fact]
    public void A_dead_owners_holding_is_found_by_the_next_writer_under_any_image()
    {
        string host = Tmp("pb3485-dead-writer");
        try
        {
            Seed(host);
            DyingPageMedium? dying = null;
            var a = Open(host, commit: true, medium: inner => dying = new DyingPageMedium(inner));
            var b = Open(host, commit: false);
            Change(a);
            dying!.DieAfter = dying.Writes;   // A dies here: nothing it does afterwards reaches the file
            a.CloseAll();
            Assert.Equal("22", Write(b, "aBcDnew1"));
            AssertNoTransactionLeft(b);
            b.CloseAll();
            Assert.Equal(SeededRecords, Settled(host));
            Assert.Equal(["abcd", "EFGH"], StoredKeys(host));
        }
        finally { TryDelete(host); }
    }

    /// <summary>§9.1.18.5, 2.2.1.6 R6 step 5 (finding R3-1): A changes the file by the variant images and dies; the next
    /// writable OPEN — its fence's dead-owner step, which retires the transaction through the OPEN's own key orders — finds
    /// the file as at the start of A's run unit and no transaction in it.</summary>
    [Fact]
    public void A_dead_owner_is_rolled_back_by_the_next_open_through_the_files_key_orders()
    {
        string host = Tmp("pb3485-dead-open");
        try
        {
            Seed(host);
            DyingPageMedium? dying = null;
            var a = Open(host, commit: true, medium: inner => dying = new DyingPageMedium(inner));
            Change(a);
            dying!.DieAfter = dying.Writes;
            a.CloseAll();
            Assert.Equal(SeededRecords, Settled(host));
            Assert.Equal(["abcd", "EFGH"], StoredKeys(host));
        }
        finally { TryDelete(host); }
    }

    /// <summary>§14.9.7.4 GR3 (2.2.1.16): A's COMMIT refused at EVERY read, write and sync it makes, the transaction having
    /// named its records by variant images. Whatever the region, the file is the previous COMMIT's or the new one's, never a
    /// mixture; before the slot write A's holdings are intact (a WRITE by another image answers '51'); a sync 2 refused
    /// reverses the COMMIT (the reversal restores through the file's key orders: '22'); after it the COMMIT stands. No
    /// transaction is left in the file once A's unconditional ROLLBACK has run.</summary>
    [Fact]
    public void A_commit_refused_at_every_operation_answers_by_its_region_under_variant_images()
    {
        var ops = new List<string>();
        string measured = Tmp("pb3485-commit-measure");
        try
        {
            Seed(measured);
            bool measuring = false;
            Exception? Log(string op)
            {
                if (measuring) ops.Add(op);
                return null;
            }
            var a = Open(measured, commit: true, medium: inner => new RefusingPageMedium(inner,
                write: p => Log($"W{p}"), sync: () => Log("S"), read: p => Log($"R{p}")));
            Change(a);
            measuring = true;
            Assert.Null(a.Commit());
            measuring = false;
            a.CloseAll();
        }
        finally { TryDelete(measured); }
        int slot = ops.FindIndex(op => op is "W0" or "W1"), sync2 = ops.FindIndex(slot, op => op == "S");
        Assert.True(slot > 0 && sync2 > slot, string.Join(' ', ops));
        for (int k = 0; k < ops.Count; k++)
        {
            string host = Tmp($"pb3485-commit-{k}");
            try
            {
                Seed(host);
                int seen = -1, at = int.MaxValue;
                bool armed = false;
                Exception? Seam(string op) => armed && ++seen == at ? Refusal : null;
                var a = Open(host, commit: true, medium: inner => new RefusingPageMedium(inner,
                    write: p => Seam($"W{p}"), sync: () => Seam("S"), read: p => Seam($"R{p}")));
                var b = Open(host, commit: false);
                Change(a);
                (armed, at) = (true, k);
                string? refused = a.Commit();
                armed = false;
                if (k > sync2)
                {
                    Assert.Null(refused);
                    Assert.Equal(0ul, a.TransactionOf("F"));
                    Assert.Equal("00", Write(b, "ABCDnew1"));   // committed: "abcd" is gone and nobody holds the key
                }
                else
                {
                    Assert.Equal("F", refused);
                    Assert.Equal("30", a.Status("F"));
                    Assert.Equal(k == sync2 ? "22" : "51", Write(b, "aBcDnew1"));
                }
                Assert.Null(a.Rollback());   // §14.6.13.1.3 item 8's unconditional ROLLBACK
                Assert.Equal(0ul, a.TransactionOf("F"));
                a.CloseAll();
                b.CloseAll();
                string?[] expected = k > sync2 ? [.. CommittedRecords] : [.. SeededRecords];
                if (k > sync2) expected[0] = "ABCDnew1";
                Assert.Equal(expected, Settled(host));
            }
            finally { TryDelete(host); }
        }
    }

    // ── The order itself ────────────────────────────────────────────────────────────────────────────────────────────

    private static HoldingsKeyOrder FoldedOrder() =>
        new(new WeightedKeyOrder(null, CaseFold(), 4), [new WeightedKeyOrder(null, CaseFold(), 4), KeyOrders.Ordinal]);

    private static byte[] B(string text) => Encoding.Latin1.GetBytes(text);

    /// <summary>A record holding compares through the prime key's order and a value holding through its alternate key's:
    /// two images of one key are one holding; a record holding sorts below a value holding; value holdings order by key
    /// number first.</summary>
    [Fact]
    public void Holdings_compare_by_kind_then_by_the_files_order_of_the_key()
    {
        var order = FoldedOrder();
        Assert.Equal(0, order.Compare(TransactionKeys.Record(B("abcd")), TransactionKeys.Record(B("ABCD"))));
        Assert.True(order.Compare(TransactionKeys.Record(B("abcd")), TransactionKeys.Record(B("ABCE"))) < 0);
        Assert.True(order.Compare(TransactionKeys.Record(B("zzzz")), TransactionKeys.Record(B("AAAA"))) > 0);   // z is above A
        Assert.Equal(0, order.Compare(TransactionKeys.Value(0, B("wxyz")), TransactionKeys.Value(0, B("WXYZ"))));
        Assert.True(order.Compare(TransactionKeys.Record(B("zzzz")), TransactionKeys.Value(0, B("AAAA"))) < 0);
        Assert.True(order.Compare(TransactionKeys.Value(0, B("zzzz")), TransactionKeys.Value(1, B("AAAA"))) < 0);
        Assert.True(order.Compare(TransactionKeys.Value(1, B("AAAA")), TransactionKeys.Value(0, B("zzzz"))) > 0);
        // Key number 1 is ordered by bytes in this order, and a key number past the table's (the COBOLNET_KEYCHECK
        // opt-out) is too: "abcd" is above "ABCD".
        Assert.True(order.Compare(TransactionKeys.Value(1, B("abcd")), TransactionKeys.Value(1, B("ABCD"))) > 0);
        Assert.True(order.Compare(TransactionKeys.Value(7, B("abcd")), TransactionKeys.Value(7, B("ABCD"))) > 0);
    }

    /// <summary>A relative file's holdings are record numbers, big-endian: the bytes are the numeric order.</summary>
    [Fact]
    public void A_relative_files_holdings_compare_as_record_numbers()
    {
        var order = HoldingsKeyOrder.RecordNumbers;
        Assert.True(order.Compare(TransactionKeys.Record(KeyOrders.RecordNumberKey(9)), TransactionKeys.Record(KeyOrders.RecordNumberKey(10))) < 0);
        Assert.Equal(0, order.Compare(TransactionKeys.Record(KeyOrders.RecordNumberKey(7)), TransactionKeys.Record(KeyOrders.RecordNumberKey(7))));
    }

    /// <summary>A holding is looked up whole: the order of a START's partial key is not defined on it, and says so.</summary>
    [Fact]
    public void A_holding_has_no_partial_key()
    {
        var order = FoldedOrder();
        var key = TransactionKeys.Record(B("abcd"));
        Assert.Throws<NotSupportedException>(() => order.ComparePrefix(key, key));
        Assert.Throws<NotSupportedException>(() => order.PrefixNarrows(1));
        Assert.Throws<NotSupportedException>(() => order.BoundOf(key, default));
        Assert.Throws<NotSupportedException>(() => order.CompareBound(key, []));
        Assert.Equal(default, order.MarksOf(key));
    }
}
