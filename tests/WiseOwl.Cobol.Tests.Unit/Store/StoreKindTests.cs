// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers.Binary;
using WiseOwl.Cobol.Runtime.IO;
using WiseOwl.Cobol.Runtime.IO.Store;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Store;

/// <summary>
/// ⛔ THE ENGINE'S SECOND CLIENT: THE MESSAGE STORE IS A KIND OF PAGED STORE, AND ITS STATEMENT LANDS THROUGH THE SAME CORE AS
/// A KEYED FILE'S (docs/rearchitecture/DESIGN-communication-facility.md §8; DESIGN-runtime-library.md 2.2.1.19; kb/Work
/// PB4734). The meta page's organization byte is the store's <see cref="StoreKind"/>: <c>R</c> and <c>I</c> for the files of
/// ISO/IEC 1989:2023 §9.1.6's organizations and <c>M</c> for the message store, which records no fixed file attributes.
/// §14.9.27.4 GR10 makes a physical file whose attributes conflict with the description a conflict at OPEN, so a COBOL OPEN of
/// an <c>M</c> store answers '39' (<see cref="StoreFormat.Foreign"/>), and the engine refuses a store of the other family
/// rather than misread it. The shared core (<see cref="StatementLanding.Complete"/>) completes, refuses and discards for a
/// client with no transaction, and a fresh <see cref="PagedFile.Open"/> over the same medium after a refusal adopts the
/// statement's page 2 exactly when the statement landed (2.2.1.6 R6), which is the message control system's in-hold reopen.
/// </summary>
public sealed class StoreKindTests
{
    private static readonly IKeyOrder Order = HoldingsKeyOrder.RecordNumbers;

    /// <summary>A fresh medium holding a new empty MESSAGE store, created through unit A's handle.</summary>
    private static (MemoryPageMedium Medium, MemoryPageMedium.Handle Handle, PagedFile Store) NewMessageStore()
    {
        var medium = new MemoryPageMedium();
        var handle = medium.Open("A");
        Assert.Equal(StoreStatus.Ok, PagedFile.Create(handle, StoreKind.Message, coherent: true, Order, out var store, out _));
        return (medium, handle, store!);
    }

    /// <summary>One statement of a client with no transaction: enter, put record <paramref name="id"/> in the prime tree in
    /// memory, land it through the shared core.</summary>
    private static StoreStatus Statement(PagedFile store, int id, string text)
    {
        if (store.Enter() != StoreStatus.Ok) return StoreStatus.PermanentError;
        var tree = StoreKit.Prime(store);
        if (tree.Put(StoreKit.Key(id), StoreKit.Value(text, blobPages: 0)) != StoreStatus.Ok) return StoreStatus.PermanentError;
        return StatementLanding.Complete(store, [new TreeChange(tree, TreeChange.PrimeRoot)]);
    }

    /// <summary>Reseal <paramref name="page"/> after a test changed bytes inside it.</summary>
    private static byte[] Resealed(byte[] page)
    {
        PageHeader.Seal(page);
        return page;
    }

    [Fact]
    public void The_message_store_writes_M_F_zero_sizes_and_no_keys_at_the_offsets_a_files_meta_page_uses()
    {
        var (medium, _, _) = NewMessageStore();
        for (uint p = 0; p < MetaPage.MetaPages; p++)
        {
            var page = medium.Snapshot(p);
            Assert.Equal((byte)'M', page[40]);                        // the organization byte
            Assert.Equal((byte)'F', page[41]);                        // the record-type byte
            Assert.Equal(0, BinaryPrimitives.ReadInt32LittleEndian(page.AsSpan(42)));   // the minimum record size
            Assert.Equal(0, BinaryPrimitives.ReadInt32LittleEndian(page.AsSpan(46)));   // the maximum record size
            Assert.Equal(0, BinaryPrimitives.ReadUInt16LittleEndian(page.AsSpan(50)));  // the key count
            Assert.True(page.AsSpan(MetaPage.KeysOffset).IndexOfAnyExcept((byte)0) < 0, "no key descriptor and no alternate root");
            var decoded = MetaPage.Decode(page, p)!;
            Assert.True(decoded.Kind.IsMessage);
            Assert.Empty(decoded.AlternateRoots);
            Assert.Null(decoded.Kind.File);
        }
    }

    [Fact]
    public void A_file_store_keeps_its_organization_bytes()
    {
        var (medium, _, _) = StoreKit.NewStore();
        Assert.Equal((byte)'R', medium.Snapshot(MetaPage.SlotA)[40]);
        Assert.False(MetaPage.Decode(medium.Snapshot(MetaPage.SlotA), MetaPage.SlotA)!.Kind.IsMessage);
        var indexed = new MemoryPageMedium();
        Assert.Equal(StoreStatus.Ok, PagedFile.Create(indexed.Open("A"), StoreKind.Of(StoreKit.TwoKeyIndexed), true, Order, out _, out _));
        Assert.Equal((byte)'I', indexed.Snapshot(MetaPage.SlotA)[40]);
    }

    [Theory]
    [InlineData(41, (byte)'V')]   // a varying record type
    [InlineData(42, (byte)1)]     // a minimum record size
    [InlineData(46, (byte)1)]     // a maximum record size
    [InlineData(50, (byte)1)]     // a key count
    public void An_M_slot_whose_file_fields_are_not_the_message_stores_does_not_validate(int offset, byte value)
    {
        var (medium, _, _) = NewMessageStore();
        var page = medium.Snapshot(MetaPage.SlotA);
        Assert.NotNull(MetaPage.Decode(page, MetaPage.SlotA));
        page[offset] = value;
        Assert.Null(MetaPage.Decode(Resealed(page), MetaPage.SlotA));
    }

    [Fact]
    public void An_unknown_organization_byte_is_still_no_slot()
    {
        var (medium, _, _) = NewMessageStore();
        var page = medium.Snapshot(MetaPage.SlotA);
        page[40] = (byte)'X';
        Assert.Null(MetaPage.Decode(Resealed(page), MetaPage.SlotA));
    }

    [Fact]
    public void A_message_store_is_Foreign_to_a_COBOL_OPEN_and_a_keyed_store_is_Described()
    {
        string dir = ScratchDirectory.Create("pb4734-store-kind");
        try
        {
            string message = Path.Combine(dir, "mcs.store"), keyed = Path.Combine(dir, "keyed.dat");
            File.WriteAllBytes(message, NewMessageStore().Medium.Contents());
            File.WriteAllBytes(keyed, StoreKit.NewStore().Medium.Contents());
            Assert.Equal(StoreFormat.Foreign, MetaPage.Describes(message, out var none));
            Assert.Null(none);
            Assert.Equal(StoreFormat.Described, MetaPage.Describes(keyed, out var recorded));
            Assert.False(recorded!.Conflicts(StoreKit.Relative));
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    [Fact]
    public void Open_adopts_a_store_of_its_own_family_and_refuses_the_other_one_writing_nothing()
    {
        var (messages, _, _) = NewMessageStore();
        var keyed = StoreKit.ClosedStore("1=one");
        foreach (bool writable in new[] { false, true })
        {
            Assert.Equal(StoreStatus.Ok, PagedFile.Open(messages.Open("B"), writable, true, Order, out var adopted, messageStore: true));
            Assert.True(adopted!.Meta.Kind.IsMessage);
            Assert.Equal(StoreStatus.Ok, PagedFile.Open(keyed.Open("B"), writable, true, Order, out var file));
            Assert.False(file!.Meta.Kind.IsMessage);
            // The other family: '30' (the MCS answers it with 92 until `cobol mcs reset`), and nothing is written.
            long beforeRefusals = messages.LastSeq + keyed.LastSeq;
            Assert.Equal(StoreStatus.PermanentError, PagedFile.Open(messages.Open("C"), writable, true, Order, out var misread));
            Assert.Null(misread);
            Assert.Equal(StoreStatus.PermanentError, PagedFile.Open(keyed.Open("C"), writable, true, Order, out misread, messageStore: true));
            Assert.Null(misread);
            Assert.Equal(beforeRefusals, messages.LastSeq + keyed.LastSeq);
        }
    }

    [Fact]
    public void The_core_lands_a_statement_of_a_client_with_no_transaction()
    {
        var (_, _, store) = NewMessageStore();
        Assert.Equal(StoreStatus.Ok, Statement(store, 1, "one"));
        Assert.Equal(StoreStatus.Ok, Statement(store, 2, "two"));
        Assert.Equal("1=one;2=two;", StoreKit.Show(store));
        Assert.True(store.Meta.Kind.IsMessage);
    }

    [Fact]
    public void The_core_refuses_and_discards_when_a_write_is_refused_and_the_next_statement_is_the_previous_ones()
    {
        // Count the writes of one landing statement, then refuse each in turn.
        var (probeMedium, probeHandle, probe) = NewMessageStore();
        Assert.Equal(StoreStatus.Ok, Statement(probe, 1, "one"));
        int before = probeHandle.WritesPerformed;
        Assert.Equal(StoreStatus.Ok, Statement(probe, 2, "two"));
        int writes = probeHandle.WritesPerformed - before;
        Assert.True(writes >= 2, "the statement writes tree pages, the ledger and page 2");
        _ = probeMedium;

        for (int refuseAt = 1; refuseAt <= writes; refuseAt++)
        {
            var medium = new MemoryPageMedium();
            var handle = medium.Open("A");
            int seen = 0;
            bool armed = false;
            var refusing = new RefusingPageMedium(handle, write: _ => armed && ++seen == refuseAt ? new IOException("refused") : null);
            Assert.Equal(StoreStatus.Ok, PagedFile.Create(refusing, StoreKind.Message, true, Order, out var store, out _));
            Assert.Equal(StoreStatus.Ok, Statement(store!, 1, "one"));
            armed = true;
            Assert.Equal(StoreStatus.PermanentError, Statement(store!, 2, "two"));
            armed = false;
            // The refused statement left no pending edit and no record: the committed trees are the previous statement's.
            Assert.Equal("1=one;", StoreKit.Show(store!));
            Assert.Equal(StoreStatus.Ok, Statement(store!, 3, "three"));
            Assert.Equal("1=one;3=three;", StoreKit.Show(store!));
        }
    }

    /// <summary>The write number <c>nth</c> (1-based, counted from <see cref="Arm"/>) is performed and THEN refused: the page
    /// is on the medium and the statement is told the host refused — the one refusal that lands.</summary>
    private sealed class ApplyThenRefuse(IPageMedium inner, int nth) : IPageMedium
    {
        private int _seen;
        public bool Armed { get; set; }
        public long Length => inner.Length;
        public void Read(uint page, Span<byte> destination) => inner.Read(page, destination);
        public void Sync() => inner.Sync();
        public void SetLength(long bytes) => inner.SetLength(bytes);
        public void Write(uint page, ReadOnlySpan<byte> source)
        {
            inner.Write(page, source);
            if (Armed && ++_seen == nth) throw new IOException("refused after the write");
        }
    }

    [Fact]
    public void A_reopen_over_the_same_medium_adopts_the_refused_statements_page_two_exactly_when_the_statement_landed()
    {
        // The generation the statement stamps when it lands.
        var (probeMedium, probeHandle, probe) = NewMessageStore();
        Assert.Equal(StoreStatus.Ok, Statement(probe, 1, "one"));
        ulong priorGeneration = probe.Meta.Generation;
        int before = probeHandle.WritesPerformed;
        Assert.Equal(StoreStatus.Ok, Statement(probe, 2, "two"));
        int writes = probeHandle.WritesPerformed - before;
        ulong stamped = probe.Meta.Generation;
        Assert.True(stamped > priorGeneration);
        _ = probeMedium;

        int landed = 0;
        for (int refuseAt = 1; refuseAt <= writes; refuseAt++)
        {
            // Refusal BEFORE the write, then AFTER it: page 2 is the statement's last write, so only an applied-then-refused
            // page-2 write leaves the statement landed.
            foreach (bool applyFirst in new[] { false, true })
            {
                var medium = new MemoryPageMedium();
                var handle = medium.Open("A");
                int seen = 0;
                bool armed = false;
                IPageMedium observed = applyFirst
                    ? new ApplyThenRefuse(handle, refuseAt)
                    : new RefusingPageMedium(handle, write: _ => armed && ++seen == refuseAt ? new IOException("refused") : null);
                Assert.Equal(StoreStatus.Ok, PagedFile.Create(observed, StoreKind.Message, true, Order, out var store, out _));
                Assert.Equal(StoreStatus.Ok, Statement(store!, 1, "one"));
                armed = true;
                if (observed is ApplyThenRefuse applying) applying.Armed = true;
                var status = Statement(store!, 2, "two");
                Assert.Equal(StoreStatus.PermanentError, status);
                // The in-hold reopen: a new PagedFile over the same handle's medium runs the adoption rule.
                // INPUT adopts in memory and writes nothing, so its generation is the adopted page's own (a writable reopen
                // would promote at a higher one).
                Assert.Equal(StoreStatus.Ok, PagedFile.Open(medium.Open("A"), writable: false, coherent: true, Order, out var reopened, messageStore: true));
                bool statementLanded = StoreKit.Show(reopened!) == "1=one;2=two;";
                bool generationSaysLanded = reopened!.Meta.Generation == stamped;
                Assert.Equal(statementLanded, generationSaysLanded);
                Assert.Equal(applyFirst && refuseAt == writes, statementLanded);
                if (statementLanded) landed++;
            }
        }
        Assert.Equal(1, landed);
    }
}
