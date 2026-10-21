// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;
using CobolNet.Tests.Shared;
using CobolNet.Tests.Unit.Store;
using Xunit;
using static CobolNet.Tests.Shared.KeyedFileHarness;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A KEYED WRITER WHOSE SHARING MODE ADMITS ANOTHER RUN UNIT'S CONNECTOR — <c>SHARING WITH READ ONLY</c> AS WELL AS
/// <c>ALL OTHER</c> — MAKES EVERY PAGE WRITE OF A STATEMENT INSIDE THE STORE MUTEX, AND GIVES THE MUTEX BACK WHEN THE
/// HOST REFUSES ONE (kb/Work PB3414; the behavioural witness of traceability row GR-14.9.51.4-12's keyed half).
///
/// <para><b>The rule.</b> §9.1.15 1): <i>"The sharing with no other mode specifies exclusive access to a physical
/// file"</i>; every other mode admits another run unit's connector, and §9.1.15 2) admits an INPUT one beside a
/// <c>SHARING WITH READ ONLY</c> writer. §14.9.51.4 GR12 releases a written record to the operating environment at the
/// WRITE. The keyed store engine writes the statement's tree pages and then page 2, which names the trees
/// (docs/rearchitecture/DESIGN-runtime-library.md 2.2.1.6, 2.2.1.7); a reader that followed the tree between those
/// writes could meet a page the ledger reuses, so the statement is ONE step to every other run unit, which is what the
/// store mutex (<c>KeyedConnector.TakeFileLock</c>'s <c>_coherent</c>, <c>FileLockPosture.AdmitsAnotherRunUnit</c>) is.
/// The predicate is the connector's, not the engine's alone: weakening it to <c>AdmitsAnotherWriter</c> (the streaming
/// writer's question, finding R2-7) leaves a READ ONLY writer outside the mutex while the INPUT reader the standard
/// admits is beside it, and the case that pins the choice by source text
/// (<c>StoreCoherenceTests.The_keyed_store_asks_about_any_run_unit_and_the_sequential_release_about_any_other_connector</c>)
/// was the only one that noticed.</para>
///
/// <para><b>The witness.</b> A medium (<see cref="RefusingPageMedium"/> through <c>KeyedStore.MediumObserver</c>) sees
/// every page write and sync of the writer's statement BEFORE it is made, and asks the host there what a reader of
/// another run unit would meet: a second handle on the file, opened as that reader's would be (read access), asks the
/// region host whether the store mutex byte is held by another handle. It is, at every write of every statement kind
/// (WRITE, REWRITE, DELETE) — including the one between the tree page and page 2 — so the reader's own statement, which
/// takes the mutex SHARED and waits, runs before the statement or after it and never between. A statement is observed
/// with no thread and no clock; the reader's reads at its two boundaries show the tree as it stood before and after.</para>
///
/// <para><b>The exception paths.</b> A host that refuses the <i>k</i>-th write of the statement (a full or failing
/// medium) answers '30' (§9.1.13.6 item 1: <i>"A permanent error exists and no further information is available
/// concerning the input-output operation"</i>) for every <i>k</i>; the statement does not release its record, and the
/// mutex is back on the host when it answers, so the other run unit's reader is not wedged and reads the tree as it stood.
/// The permanent error stays in effect until the CLOSE (§9.1.13.1), the later statements replay it without touching the
/// file, and the CLOSE answers '00'.</para>
/// </summary>
[Trait("Host", "ReFS")]
public sealed class ReadOnlyKeyedWriterStoreMutexTests
{
    /// <summary>The writer's medium seam: counts the page writes and syncs a watched statement makes, notes those made
    /// while the mutex was NOT held, and refuses the <see cref="RefuseAt"/>-th operation of the watch.</summary>
    private sealed class Watch(string host)
    {
        public bool Watching { get; set; }
        public int Operations { get; private set; }
        public int RefuseAt { get; set; } = -1;
        public List<string> Unheld { get; } = [];

        public void Begin(int refuseAt = -1)
        {
            Watching = true;
            Operations = 0;
            RefuseAt = refuseAt;
            Unheld.Clear();
        }

        public void End() => Watching = false;

        public Exception? At(string what, uint page)
        {
            if (!Watching) return null;
            int index = Operations++;
            if (!MutexHeld(host)) Unheld.Add($"{what} {page} (operation {index})");
            return index == RefuseAt ? new IOException("refused by the host") : null;
        }
    }

    /// <summary>Open <paramref name="name"/> of <paramref name="reg"/> in <paramref name="mode"/> with its store's medium
    /// observed by <paramref name="watch"/>.</summary>
    private static void OpenWatched(FileRegistry reg, string name, FileOpenMode mode, Watch watch)
    {
        KeyedStore.MediumObserver.Value = inner => new RefusingPageMedium(inner,
            write: page => watch.At("write", page), sync: () => watch.At("sync", 0));
        try { reg.OpenStatic(name, mode); }
        finally { KeyedStore.MediumObserver.Value = null; }
    }

    public static TheoryData<KeyedOrg, FileSharing, RegionHostKind> Shapes()
    {
        var data = new TheoryData<KeyedOrg, FileSharing, RegionHostKind>();
        foreach (var org in Enum.GetValues<KeyedOrg>())
            foreach (var sharing in new[] { FileSharing.ReadOnly, FileSharing.AllOther })
                foreach (var host in Enum.GetValues<RegionHostKind>())
                    data.Add(org, sharing, host);
        return data;
    }

    /// <summary>A WRITE, a REWRITE and a DELETE of a writer admitting another run unit — <c>READ ONLY</c> and
    /// <c>ALL OTHER</c> alike, on the machine's region host and on the lock-file host — make every page write and sync
    /// inside the store mutex: a WRITE of a new record writes at least its tree page and page 2, and the mutex is held at
    /// each. The INPUT reader of another run unit open beside the writer (§9.1.15 2)) reads the tree as it stood before
    /// each statement and as the statement left it, record by record.</summary>
    [Theory]
    [MemberData(nameof(Shapes))]
    public void Every_page_write_of_a_statement_is_made_under_the_store_mutex(KeyedOrg org, FileSharing sharing, RegionHostKind regionHost)
    {
        using var forced = RegionHostSeam.Use(regionHost);
        string host = Tmp($"held-{org}-{sharing}");
        try
        {
            SeedRecords(host, org, 2);
            var watch = new Watch(host);
            var a = new FileRegistry();
            var b = new FileRegistry();
            Register(a, "A", host, org, sharing, LockModeRequiredBy(sharing));
            Register(b, "B", host, org, FileSharing.AllOther);
            OpenWatched(a, "A", FileOpenMode.IO, watch);
            Assert.Equal("00", a.Status("A"));
            b.OpenStatic("B", FileOpenMode.Input);
            Assert.Equal("00", b.Status("B"));

            Assert.Equal("23", Read(b, "B", org, 3, out _));   // before the WRITE: the tree as it stood

            watch.Begin();
            Assert.Equal("00", Write(a, "A", org, 3, "CCCC"));
            watch.End();
            Assert.True(watch.Operations >= 2, $"a WRITE of a new record writes its tree page and page 2 ({watch.Operations})");
            Assert.True(watch.Unheld.Count == 0, "WRITE: operations made outside the store mutex: " + string.Join(", ", watch.Unheld));
            Assert.Equal("00", Read(b, "B", org, 3, out string written));
            Assert.Equal(Rec(3, "CCCC"), written);

            Assert.Equal("00", Read(a, "A", org, 1, out _));
            watch.Begin();
            Assert.Equal("00", Rewrite(a, "A", org, 1, "BBBB"));
            watch.End();
            Assert.True(watch.Operations >= 1, $"a REWRITE writes ({watch.Operations})");
            Assert.True(watch.Unheld.Count == 0, "REWRITE: operations made outside the store mutex: " + string.Join(", ", watch.Unheld));
            Assert.Equal("00", Read(b, "B", org, 1, out string rewritten));
            Assert.Equal(Rec(1, "BBBB"), rewritten);

            watch.Begin();
            Assert.Equal("00", Delete(a, "A", org, 2));
            watch.End();
            Assert.True(watch.Operations >= 1, $"a DELETE writes ({watch.Operations})");
            Assert.True(watch.Unheld.Count == 0, "DELETE: operations made outside the store mutex: " + string.Join(", ", watch.Unheld));
            Assert.Equal("23", Read(b, "B", org, 2, out _));

            Assert.False(MutexHeld(host), "between statements the mutex is back on the host");
            b.Close("B");
            a.Close("A");
            Assert.Equal("00", b.Status("B"));
            Assert.Equal("00", a.Status("A"));
        }
        finally { TryDelete(host); }
    }

    /// <summary>The page writes and syncs one WRITE of a new record makes, measured on a run the host does not refuse.</summary>
    private static int OperationsOfAWrite(KeyedOrg org, FileSharing sharing)
    {
        string host = Tmp($"measure-{org}-{sharing}");
        try
        {
            SeedRecords(host, org, 2);
            var watch = new Watch(host);
            var a = new FileRegistry();
            Register(a, "A", host, org, sharing, LockModeRequiredBy(sharing));
            OpenWatched(a, "A", FileOpenMode.IO, watch);
            Assert.Equal("00", a.Status("A"));
            watch.Begin();
            Assert.Equal("00", Write(a, "A", org, 3, "CCCC"));
            watch.End();
            a.Close("A");
            return watch.Operations;
        }
        finally { TryDelete(host); }
    }

    /// <summary>A host refusal of EVERY page write and sync of a WRITE, one at a time, answers '30' and releases nothing:
    /// at the moment the statement answers, the mutex is back on the host (a reader of another run unit is not wedged
    /// behind a statement that died), that reader reads the tree as it stood — records 1 and 2, and no record 3 ('23') —
    /// and the writer's permanent error (§9.1.13.1) is replayed by its next statement, which touches nothing, until its
    /// CLOSE answers '00'. Records 1 and 2 are in the file for the next OPEN. It runs on the machine's region host and on
    /// the lock-file host, like the case above (<see cref="Shapes"/>): the store mutex is a different object on each, and
    /// "the refused statement gave it back" is a claim about that object.</summary>
    [Theory]
    [MemberData(nameof(Shapes))]
    public void A_refusal_at_every_page_write_of_a_WRITE_is_30_and_gives_the_store_mutex_back(KeyedOrg org, FileSharing sharing, RegionHostKind regionHost)
    {
        using var forced = RegionHostSeam.Use(regionHost);
        int operations = OperationsOfAWrite(org, sharing);
        Assert.True(operations >= 2, $"a WRITE of a new record writes its tree page and page 2 ({operations})");
        for (int k = 0; k < operations; k++)
        {
            string host = Tmp($"refuse-{org}-{sharing}-{k}");
            try
            {
                SeedRecords(host, org, 2);
                var watch = new Watch(host);
                var a = new FileRegistry();
                var b = new FileRegistry();
                Register(a, "A", host, org, sharing, LockModeRequiredBy(sharing));
                Register(b, "B", host, org, FileSharing.AllOther);
                OpenWatched(a, "A", FileOpenMode.IO, watch);
                b.OpenStatic("B", FileOpenMode.Input);
                Assert.Equal("00", a.Status("A"));
                Assert.Equal("00", b.Status("B"));

                watch.Begin(refuseAt: k);
                Assert.Equal("30", Write(a, "A", org, 3, "CCCC"));
                watch.End();
                Assert.True(watch.Operations == k + 1, $"refusal {k}: the statement stopped at the refused operation ({watch.Operations})");
                Assert.True(watch.Unheld.Count == 0, $"refusal {k}: operations made outside the store mutex: " + string.Join(", ", watch.Unheld));
                Assert.False(MutexHeld(host), $"refusal {k}: the refused statement gave the store mutex back");

                Assert.Equal("00", Read(b, "B", org, 2, out string two));
                Assert.Equal(Rec(2, "AAAA"), two);
                Assert.Equal("23", Read(b, "B", org, 3, out _));   // the refused WRITE released nothing

                watch.Begin();
                Assert.Equal("30", Write(a, "A", org, 4, "DDDD"));   // the permanent error in effect (§9.1.13.1) is replayed
                watch.End();
                Assert.Equal(0, watch.Operations);                   // and touches nothing
                Assert.Equal("23", Read(b, "B", org, 4, out _));

                a.Close("A");
                Assert.Equal("00", a.Status("A"));
                Assert.False(MutexHeld(host), $"refusal {k}: the CLOSE left the store mutex held");
                Assert.Equal("00", Read(b, "B", org, 1, out string one));
                Assert.Equal(Rec(1, "AAAA"), one);
                b.Close("B");
                Assert.Equal("00", b.Status("B"));
            }
            finally { TryDelete(host); }
        }
    }
}
