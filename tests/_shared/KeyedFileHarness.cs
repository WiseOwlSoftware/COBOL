// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;
using Xunit;

namespace CobolNet.Tests.Shared;

/// <summary>The two keyed organizations a keyed-store test runs both of (kb/Work PB3459).</summary>
public enum KeyedOrg { Relative, Indexed }

/// <summary>
/// ⛔ THE ONE KEYED-FILE TEST HARNESS (kb/Work PB3459): open, seed, position, read and write a RELATIVE or INDEXED file
/// through a <see cref="FileRegistry"/> from a test, tear its pages, and (in the Unit project) ask the host whether
/// another run unit meets the store mutex. Five test classes each wrote these helpers privately (and the keyed-store tests
/// that followed them copied them again), so a change to the connector API or to the store-mutex shape took an edit per
/// copy and the copies could drift. A keyed-store test that needs one of them calls it here; a new helper of this job is
/// written here, never beside a test.
///
/// <para><b>Shared by every test project (kb/Work PB3472).</b> This file lives in <c>tests/_shared</c>, which
/// <c>tests/Directory.Build.props</c> links into every test project, so a Conformance test that runs a compiled program
/// over a keyed file calls the same helpers as a Unit test and registers no file of its own. It uses only the runtime's
/// PUBLIC surface, because the Conformance and Characterization assemblies are not granted the runtime's internals. The
/// one helper that needs them, <c>MutexHeld</c> (the region host's <c>HostRegionLocks</c> is internal), is this
/// class's other half, <c>KeyedFileHarness.StoreMutex.cs</c> in the Unit project.</para>
///
/// <para><b>The file.</b> An 8-character record: the key in positions 1-4 (also the relative record number, so one
/// <see cref="Rec"/> serves both organizations) and a 4-character payload. Dynamic access; one prime key. A test with a
/// different record layout (an alternate key, a wider record) registers its own file and may still use
/// <see cref="Tmp"/>, <see cref="TryDelete"/> and <see cref="Rec"/>.</para>
/// </summary>
public static partial class KeyedFileHarness
{
    /// <summary>A fresh path in the temp directory, unique per call (the file is not created).</summary>
    public static string Tmp(string tag) => Path.Combine(Path.GetTempPath(), $"keyed-{tag}-{Guid.NewGuid():N}.dat");

    public static void TryDelete(string host)
    {
        try { File.Delete(host); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }

    /// <summary>The LOCK MODE a declaration of <paramref name="sharing"/> needs and no more: §14.9.27.3 SR8 requires the
    /// LOCK MODE clause where the ALL phrase is specified in the SHARING clause (SHARING WITH ALL OTHER), and the other
    /// sharing modes need none.</summary>
    public static FileLockMode LockModeRequiredBy(FileSharing sharing) =>
        sharing == FileSharing.AllOther ? FileLockMode.Manual : FileLockMode.None;

    /// <summary>Register <paramref name="name"/> over <paramref name="host"/> as the program declares it: dynamic access
    /// (or <paramref name="access"/>), the sharing <paramref name="sharing"/> under <paramref name="lockMode"/> (no
    /// sharing clause at all when <paramref name="sharing"/> is null: the NO OTHER default). With
    /// <paramref name="alternateKey"/> an INDEXED file also has an alternate key WITH DUPLICATES over the payload
    /// (positions 5-8), so a record is two trees' entries.</summary>
    public static void Register(FileRegistry reg, string name, string host, KeyedOrg org,
        FileSharing? sharing = FileSharing.AllOther, FileLockMode lockMode = FileLockMode.Manual, bool optional = false,
        KeyedAccess access = KeyedAccess.Dynamic, bool alternateKey = false)
    {
        Assert.True(!alternateKey || org == KeyedOrg.Indexed, "only an INDEXED file has an alternate key");
        if (org == KeyedOrg.Relative)
            reg.RegisterRelative(name, host, recordWidth: 8, optional: optional, accessMode: (int)access,
                relativeKeyDigits: 4, varyMin: -1, varyMax: -1);
        else
            reg.RegisterIndexed(name, host, recordWidth: 8, optional: optional, accessMode: (int)access,
                primeKey: [new(0, 4)], varyMin: -1, varyMax: -1);
        if (alternateKey) reg.AddAlternateKey(name, [new(4, 4)], duplicates: true);
        if (sharing is { } s) reg.RegisterSharing(name, s, lockMode, multiple: false);
    }

    /// <summary>Record <paramref name="n"/>'s image: its key (also its relative record number) and a payload.</summary>
    public static string Rec(int n, string payload) => $"{n:0000}{payload}";

    /// <summary>Make record <paramref name="n"/> the one the next statement names: a RELATIVE file's relative key; an
    /// INDEXED file's key is in the record image, so nothing to do.</summary>
    public static void Position(FileRegistry reg, string name, KeyedOrg org, int n)
    {
        if (org == KeyedOrg.Relative) reg.SetRelativeKey(name, n);
    }

    public static string Write(FileRegistry reg, string name, KeyedOrg org, int n, string payload)
    {
        Position(reg, name, org, n);
        return reg.WriteShared(name, Rec(n, payload), -1, FileRecordLock.None, FileRetryKind.None, 0, page: null);
    }

    public static string Rewrite(FileRegistry reg, string name, KeyedOrg org, int n, string payload)
    {
        Position(reg, name, org, n);
        return reg.RewriteShared(name, Rec(n, payload), -1, FileRecordLock.None, FileRetryKind.None, 0);
    }

    public static string Delete(FileRegistry reg, string name, KeyedOrg org, int n)
    {
        Position(reg, name, org, n);
        return reg.DeleteShared(name, Rec(n, "    "), FileRetryKind.None, 0);
    }

    /// <summary>A random READ of record <paramref name="n"/>.</summary>
    public static string Read(FileRegistry reg, string name, KeyedOrg org, int n, out string image,
        FileRecordLock phrase = FileRecordLock.None, bool ignoringLock = false, FileRetryKind retry = FileRetryKind.None,
        long amount = 0)
    {
        Position(reg, name, org, n);
        return reg.ReadKeyedShared(name, keyIndex: -1, Rec(n, "    "), phrase, ignoringLock, retry, amount, out image);
    }

    /// <summary>A file whose record <i>n</i> (1 to <paramref name="payloads"/>.Length) carries <c>payloads[n - 1]</c>,
    /// written by an OUTPUT connector with no sharing clause and CLOSEd (so committed). The file has the alternate key
    /// of <see cref="Register"/> when <paramref name="alternateKey"/>.</summary>
    public static void Seed(string host, KeyedOrg org, string[] payloads, bool alternateKey = false) =>
        Seed(host, org, [.. payloads.Select((payload, i) => (i + 1, payload))], alternateKey);

    /// <summary>A file whose records are exactly <paramref name="records"/> (key, payload), in that order: for a file whose
    /// keys are not 1 to n.</summary>
    public static void Seed(string host, KeyedOrg org, (int Key, string Payload)[] records, bool alternateKey = false)
    {
        var seed = new FileRegistry();
        Register(seed, "S", host, org, sharing: null, alternateKey: alternateKey);
        seed.Open("S", FileOpenMode.Output, host, assignDynamic: false, page: null);
        Assert.Equal("00", seed.Status("S"));
        foreach (var (key, payload) in records) Assert.Equal("00", Write(seed, "S", org, key, payload));
        seed.Close("S");
        Assert.Equal("00", seed.Status("S"));
    }

    /// <summary>A file of <paramref name="records"/> records, 1 to n, each payload "AAAA", CLOSEd.</summary>
    public static void SeedRecords(string host, KeyedOrg org, int records) =>
        Seed(host, org, [.. Enumerable.Repeat("AAAA", records)]);

    /// <summary>Tear <paramref name="host"/>'s store: every page that carries <paramref name="record"/>'s bytes gets one
    /// byte flipped, so its checksum fails at the next read and the engine answers the record statement with the
    /// permanent error '30' (§9.1.13.6 item 1; 2.2.1.6 R3). Scans every page from the first: a record image is never in
    /// a meta page. Returns the number of pages torn, and fails the test when it is none (the record was not found).</summary>
    public static int TearPagesCarrying(string host, ReadOnlySpan<byte> record)
    {
        const int PageSize = HostFile.InputOutputAreaBytes;
        byte[] file = File.ReadAllBytes(host);
        int torn = 0;
        for (int page = 0; page < file.Length / PageSize; page++)
        {
            if (file.AsSpan(page * PageSize, PageSize).IndexOf(record) < 0) continue;
            file[page * PageSize + 100] ^= 0xFF;
            torn++;
        }
        Assert.True(torn > 0, "the record's leaf was found");
        File.WriteAllBytes(host, file);
        return torn;
    }
}
