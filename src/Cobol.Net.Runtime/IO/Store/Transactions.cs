// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers.Binary;

namespace CobolNet.Runtime.IO.Store;

/// <summary>⛔ THE KEYS AND VALUES OF THE THREE TRANSACTION TREES (docs/rearchitecture/DESIGN-runtime-library.md 2.2.1.4
/// and 2.2.1.10; kb/Work PB3337), written in ONE place so the writer of a holding and every reader of it agree:
/// <list type="bullet">
/// <item>the TRANSACTIONS tree (its root in the meta page): the 8-byte big-endian transaction id → the 12-byte POINTER to
/// the transaction's UNDO tree;</item>
/// <item>an UNDO tree, keyed (kind, identity): under <see cref="RecordKind"/> a record identity (the prime key's channel
/// bytes, or the 8-byte relative record number) → the record's BEFORE-IMAGE as of the run unit's last COMMIT (the whole
/// frame, ordinal vector included) or the ABSENT marker for a record the transaction created; under
/// <see cref="ValueKind"/> a unique alternate key value the transaction's change freed (the key's index and the value's
/// channel bytes) → nothing;</item>
/// <item>the HOLDINGS tree (its root in the meta page): the same (kind, identity) keys → the 8-byte id of the transaction
/// that holds them. It is the file's INDEX, by identity, of every open transaction's UNDO entries, and a transaction's
/// UNDO tree is its own list of its holdings: after every statement the HOLDINGS tree's key set equals the union of the
/// UNDO trees' key sets (finding R5-2).</item>
/// </list>
/// Every tree is ordered by the ordinal byte order: the keys are identities, compared for equality, never for a
/// collating sequence.</summary>
internal static class TransactionKeys
{
    /// <summary>The kind of a record holding: its identity.</summary>
    public const byte RecordKind = (byte)'R';

    /// <summary>The kind of a value holding: a unique alternate key value a change freed.</summary>
    public const byte ValueKind = (byte)'V';

    private const byte Absent = 0;
    private const byte Present = 1;

    /// <summary>The longest record identity a holding can carry: the tree's key bound less the kind byte. An INDEXED
    /// declaration whose prime key is longer cannot be put under commit control (<c>KeyedConnector.FormatHolds</c>).</summary>
    public const int MaxIdentityBytes = BTree.MaxKeyBytes - 1;

    /// <summary>The key of the holding of record <paramref name="identity"/>.</summary>
    public static byte[] Record(ReadOnlySpan<byte> identity)
    {
        var key = new byte[1 + identity.Length];
        key[0] = RecordKind;
        identity.CopyTo(key.AsSpan(1));
        return key;
    }

    /// <summary>The key of the holding of <paramref name="value"/> under alternate key <paramref name="keyIndex"/>.</summary>
    public static byte[] Value(int keyIndex, ReadOnlySpan<byte> value)
    {
        var key = new byte[3 + value.Length];
        key[0] = ValueKind;
        BinaryPrimitives.WriteUInt16BigEndian(key.AsSpan(1), checked((ushort)keyIndex));
        value.CopyTo(key.AsSpan(3));
        return key;
    }

    /// <summary>Whether <paramref name="key"/> is a record holding.</summary>
    public static bool IsRecord(ReadOnlySpan<byte> key) => key.Length > 0 && key[0] == RecordKind;

    /// <summary>The record identity of a record holding's key.</summary>
    public static byte[] IdentityOf(byte[] key) => key[1..];

    /// <summary>The TRANSACTIONS tree's key for transaction <paramref name="id"/>, and a HOLDINGS value naming it.</summary>
    public static byte[] Transaction(ulong id)
    {
        var bytes = new byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64BigEndian(bytes, id);
        return bytes;
    }

    /// <summary>The transaction a HOLDINGS value or a TRANSACTIONS key names.</summary>
    public static ulong TransactionOf(ReadOnlySpan<byte> bytes) => BinaryPrimitives.ReadUInt64BigEndian(bytes);

    /// <summary>A TRANSACTIONS value: the POINTER to an UNDO tree.</summary>
    public static byte[] Pointer(PagePointer pointer)
    {
        var bytes = new byte[PagePointer.Bytes];
        pointer.Write(bytes);
        return bytes;
    }

    /// <summary>The UNDO tree a TRANSACTIONS value names.</summary>
    public static PagePointer PointerOf(ReadOnlySpan<byte> value) => PagePointer.Read(value);

    /// <summary>An UNDO value of a record holding: the before-image <paramref name="frame"/>, or the ABSENT marker
    /// (null: the transaction created the record).</summary>
    public static byte[] BeforeImage(byte[]? frame)
    {
        if (frame is null) return [Absent];
        var value = new byte[1 + frame.Length];
        value[0] = Present;
        frame.CopyTo(value, 1);
        return value;
    }

    /// <summary>The before-image an UNDO value holds: the frame, or null for the ABSENT marker.</summary>
    public static byte[]? ImageOf(byte[] value) => value.Length > 0 && value[0] == Present ? value[1..] : null;
}

/// <summary>⛔ ONE WALK OF A TRANSACTION'S UNDO TREE, AND EVERYTHING THAT RETIRES IT (DESIGN-runtime-library.md 2.2.1.10,
/// finding R5-2): every key the walk meets — R and V alike — removed from the HOLDINGS tree, the transaction's entry
/// removed from the TRANSACTIONS tree, and the UNDO tree emptied (every page of it superseded, so the statement releases
/// it). A COMMIT's retirement (<c>PagedFile</c>'s retirement statement, which the slot write decided) walks with no
/// restore; a ROLLBACK, a dead owner's rollback and a COMMIT's reversal re-apply every R before-image through
/// <c>restore</c> on the way. It is the ONE enumeration of a transaction's holdings, so whoever retires or rolls back a
/// transaction finds every lock it holds. The edits are in memory; the caller's statement flushes them
/// (<see cref="Trees"/>, the UNDO tree first: nothing else references it).</summary>
internal sealed class TransactionRetirement
{
    private TransactionRetirement(BTree undo, BTree transactions, BTree holdings)
    {
        Undo = undo;
        Transactions = transactions;
        Holdings = holdings;
    }

    /// <summary>The emptied UNDO tree.</summary>
    public BTree Undo { get; }

    /// <summary>The TRANSACTIONS tree without the transaction's entry.</summary>
    public BTree Transactions { get; }

    /// <summary>The HOLDINGS tree without the transaction's keys.</summary>
    public BTree Holdings { get; }

    /// <summary>The three trees, in the order the statement flushes them.</summary>
    public BTree[] Trees => [Undo, Transactions, Holdings];

    /// <summary>The retirement of transaction <paramref name="id"/> over the trees <paramref name="meta"/> names: null
    /// when the TRANSACTIONS tree holds no such entry (it was retired already: a rollback is idempotent). A torn page
    /// answers '30'.</summary>
    public static StoreStatus Of(PagedFile file, MetaPage meta, ulong id, Func<byte[], byte[]?, StoreStatus>? restore,
        out TransactionRetirement? retirement)
    {
        retirement = null;
        if (meta.TransactionsRoot.IsNone) return StoreStatus.Ok;
        var transactions = new BTree(file, KeyOrders.Ordinal, meta.TransactionsRoot);
        var status = transactions.TryGet(TransactionKeys.Transaction(id), out var entry);
        if (status != StoreStatus.Ok || entry is null) return status;
        var undo = new BTree(file, KeyOrders.Ordinal, TransactionKeys.PointerOf(entry));
        var holdings = new BTree(file, KeyOrders.Ordinal, meta.HoldingsRoot);
        var keys = new List<byte[]>();
        if ((status = undo.First(out var cursor)) != StoreStatus.Ok) return status;
        while (cursor.HasCurrent)
        {
            byte[] key = cursor.Key;
            if (restore is not null && TransactionKeys.IsRecord(key))
            {
                if ((status = cursor.ReadValue(out var value)) != StoreStatus.Ok) return status;
                if ((status = restore(TransactionKeys.IdentityOf(key), TransactionKeys.ImageOf(value))) != StoreStatus.Ok)
                    return status;
            }
            keys.Add(key);
            if ((status = cursor.Next()) != StoreStatus.Ok) return status;
        }
        foreach (var key in keys)
            if ((status = holdings.Delete(key, out _)) != StoreStatus.Ok) return status;
        if ((status = transactions.Delete(TransactionKeys.Transaction(id), out _)) != StoreStatus.Ok) return status;
        if ((status = undo.Clear()) != StoreStatus.Ok) return status;
        retirement = new TransactionRetirement(undo, transactions, holdings);
        return StoreStatus.Ok;
    }

    /// <summary>Flush the three trees through <paramref name="statement"/> and set the two meta roots: '30' at a refused
    /// write (the statement is refused; nothing it wrote is reachable, R1).</summary>
    public StoreStatus Flush(PageStatement statement)
    {
        if (Undo.Flush(statement, out _) is var undone && undone != StoreStatus.Ok) return undone;
        if (Transactions.Flush(statement, out var transactionsRoot) is var t && t != StoreStatus.Ok) return t;
        if (Holdings.Flush(statement, out var holdingsRoot) is var h && h != StoreStatus.Ok) return h;
        statement.SetTransactionsRoot(transactionsRoot);
        statement.SetHoldingsRoot(holdingsRoot);
        return StoreStatus.Ok;
    }

    /// <summary>Drop the pending edits.</summary>
    public void Discard()
    {
        foreach (var tree in Trees) tree.Discard();
    }
}

/// <summary>The data trees one ROLLBACK statement edits (DESIGN-runtime-library.md 2.2.1.10, the ROLLBACK bullet): the
/// organization's, over the roots of the meta the statement starts from. <see cref="Restore"/> writes a before-image
/// back under its identity — a frame with its OLD ordinals, the alternate trees' composite entries rebuilt from the
/// frame's ordinal vector, the ABSENT marker a delete — in memory; <see cref="Edited"/> lists every tree it changed with
/// the meta root it sets (<see cref="PrimeRoot"/>, or an alternate key's index). No uniqueness check and no lock test
/// runs: every identity and unique value a rollback re-inserts is a holding of the transaction, which no writer passed
/// while its owner lived and none after its death without this rollback having run first (findings R3-1, R4-1).</summary>
internal interface IRestoringTrees
{
    /// <summary>The root code of the prime (relative: record-number) tree.</summary>
    public const int PrimeRoot = -1;

    /// <summary>Re-apply <paramref name="image"/> (null: the record is absent) under <paramref name="identity"/>.</summary>
    StoreStatus Restore(byte[] identity, byte[]? image);

    /// <summary>Every data tree a restore changed, with the meta root it sets.</summary>
    IReadOnlyList<(BTree Tree, int Root)> Edited { get; }
}

/// <summary>Makes the organization's <see cref="IRestoringTrees"/> over <paramref name="meta"/>'s roots — handed to the
/// page layer by the connector so a COMMIT whose second sync is refused can REVERSE itself (2.2.1.10 region 3) without
/// the page layer knowing an organization.</summary>
internal delegate IRestoringTrees RestoringTreesFactory(PagedFile file, MetaPage meta);

/// <summary>What a COMMIT's promotion did (2.2.1.10's three regions; finding R5-1).</summary>
internal enum CommitOutcome
{
    /// <summary>Sync 2 landed: committed, whatever became of the retirement (the next entry completes it).</summary>
    Committed,

    /// <summary>Refused BEFORE the slot write (the claim, sync 1 or the slot write): the transaction is intact, and the
    /// claim is unwound by the next entry's restart rule — the owner's own ROLLBACK's included.</summary>
    Refused,

    /// <summary>Sync 2 refused, the UNACKNOWLEDGED COMMIT, and the reversal LANDED: the transaction is rolled back and the
    /// rollback promoted and synced, so every future reads it; the COMMIT answers unsuccessful.</summary>
    Reversed,

    /// <summary>Sync 2 refused and the reversal refused too: the transaction's fate on the medium is unknown; the store
    /// MARKS the file UNACKNOWLEDGED and every ROLLBACK answers by a promotion's sync.</summary>
    Unacknowledged,
}
