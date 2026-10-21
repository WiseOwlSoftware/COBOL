// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using CobolNet.Runtime.IO;
using CobolNet.Tests.Shared;
using Xunit;
using static CobolNet.Tests.Shared.FileHarness;

namespace CobolNet.Tests.Unit.Store;

/// <summary>PROBE.</summary>
[Trait("Host", "ReFS")]
public sealed class HoldingsKeyOrderTests
{
    /// <summary>ALPHABET: each letter ALSO its lower-case twin - one collating position per pair.</summary>
    internal static AlphanumericCollation CaseFold()
    {
        var codes = new List<ushort>(); var positions = new List<ushort>();
        for (int i = 0; i < 26; i++) { codes.Add((ushort)('A' + i)); positions.Add((ushort)i); }
        for (int i = 0; i < 26; i++) { codes.Add((ushort)('a' + i)); positions.Add((ushort)i); }
        var reps = Enumerable.Range(0, 26).Select(i => (ushort)('A' + i)).ToArray();
        return new AlphanumericCollation([.. codes], [.. positions], reps, 26);
    }

    private static FileRegistry Open(string host, bool commit, FileOpenMode mode = FileOpenMode.IO, string name = "F")
    {
        var reg = new FileRegistry();
        reg.RegisterIndexed(name, host, recordWidth: 8, optional: false, accessMode: (int)KeyedAccess.Dynamic,
            primeKey: [new(0, 4)], varyMin: -1, varyMax: -1, primeCollation: CaseFold());
        reg.RegisterSharing(name, FileSharing.AllOther, LockModeRequiredBy(FileSharing.AllOther), multiple: false);
        if (commit) reg.RegisterCommitControl(name);
        reg.OpenStatic(name, mode);
        Assert.Equal("00", reg.Status(name));
        return reg;
    }

    [Fact]
    public void Probe()
    {
        string host = Tmp("pb3485-probe");
        try
        {
            var seed = Open(host, false, FileOpenMode.Output);
            Assert.Equal("00", seed.WriteShared("F", "ABCDwxyz", -1, FileRecordLock.None, FileRetryKind.None, 0, page: null));
            seed.CloseAll();
            var a = Open(host, true);
            var b = Open(host, false);
            Assert.Equal("00", a.DeleteShared("F", "abcd    ", FileRetryKind.None, 0));
            Assert.Equal("51", b.WriteShared("F", "ABCDnew1", -1, FileRecordLock.None, FileRetryKind.None, 0, page: null));
            a.CloseAll(); b.CloseAll();
        }
        finally { TryDelete(host); }
    }
}
