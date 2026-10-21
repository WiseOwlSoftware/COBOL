// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Runtime.CompilerServices;
using CobolNet.Runtime.IO;
using Xunit;

namespace CobolNet.Tests.Shared;

/// <summary>⛔ THE THREE FILE ORGANIZATIONS, ONE ENUM (kb/Work PB771, PB3459, PB3471). §9.1.6: <i>"There are three
/// organizations: sequential, relative, and indexed"</i>, and §9.1.15 names none of them — <i>"The successful opening of
/// a file establishes a file lock for the applicable sharing rules"</i> is owed by every organization there is. RELATIVE
/// and INDEXED once owed it and paid nothing (they held no long-lived host handle between OPEN and CLOSE), which is why a
/// measurement of the file lock, of a sharing refusal or of a record lock is a theory over THIS enum. A test of the keyed
/// store alone iterates <see cref="FileHarness.KeyedOrgs"/>.</summary>
public enum FileOrg { Sequential, Relative, Indexed }

/// <summary>
/// ⛔ THE ONE FILE TEST HARNESS (kb/Work PB3459, PB3471): open, seed, position, read and write a SEQUENTIAL, RELATIVE or
/// INDEXED file through a <see cref="FileRegistry"/> from a test, tear its pages, and (in the Unit project) ask the host
/// whether another run unit meets the store mutex. Nine test classes each wrote these helpers privately (and the tests
/// that followed them copied them again), so a change to the connector API or to the store-mutex shape took an edit per
/// copy and the copies could drift. A file test that needs one of them calls it here; a new helper of this job is written
/// here, never beside a test. A fourth organization is one new <see cref="FileOrg"/> arm in <see cref="Register"/>, and
/// <c>FileLockPostureDriftTests.EveryOrganisationsHandleAccessLiesInThatBand</c> is red until it is added.
///
/// <para><b>Shared by every test project (kb/Work PB3472).</b> This file lives in <c>tests/_shared</c>, which
/// <c>tests/Directory.Build.props</c> links into every test project, so a Conformance test that runs a compiled program
/// over a file calls the same helpers as a Unit test and registers no file of its own. It uses only the runtime's PUBLIC
/// surface, because the Conformance and Characterization assemblies are not granted the runtime's internals. The one
/// helper that needs them, <c>MutexHeld</c> (the region host's <c>HostRegionLocks</c> is internal), is this class's other
/// half, <c>FileHarness.StoreMutex.cs</c> in the Unit project.</para>
///
/// <para><b>The file.</b> An 8-character record: the key in positions 1-4 (also the relative record number, so one
/// <see cref="Rec"/> serves every organization; a sequential file simply carries it as data) and a 4-character
/// payload. Dynamic access, except for a connector that OPENs EXTEND (<see cref="FileHarness.AccessFor"/>, §14.9.27.3 SR2);
/// one prime key. A test with a different record layout (an alternate key, a wider record, a
/// LINE SEQUENTIAL file) registers its own file and may still use <see cref="Tmp"/>, <see cref="TryDelete"/> and
/// <see cref="Rec"/>.</para>
/// </summary>
public static partial class FileHarness
{
    /// <summary>The organizations that keep a store of keyed pages (RELATIVE and INDEXED): what a keyed-store test runs
    /// both of.</summary>
    public static readonly FileOrg[] KeyedOrgs = [FileOrg.Relative, FileOrg.Indexed];

    /// <summary>A fresh path in the temp directory, unique per call (the file is not created).</summary>
    public static string Tmp(string tag) => Path.Combine(Path.GetTempPath(), $"file-{tag}-{Guid.NewGuid():N}.dat");

    /// <summary>Delete the physical file. ONE artifact per physical file (kb/Work PB802): a keyed file's §9.1.6 fixed file
    /// attributes are the store header INSIDE it, so deleting the data file deletes them.</summary>
    public static void TryDelete(string host)
    {
        try { File.Delete(host); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }

    /// <summary>The LOCK MODE a declaration of <paramref name="sharing"/> needs and no more: §14.9.27.3 SR8 requires the
    /// LOCK MODE clause where the ALL phrase is specified in the SHARING clause (SHARING WITH ALL OTHER), and the other
    /// sharing modes need none.</summary>
    public static FileLockMode LockModeRequiredBy(FileSharing sharing) =>
        sharing == FileSharing.AllOther ? FileLockMode.Manual : FileLockMode.None;

    /// <summary>⛔ THE ACCESS MODE A CONNECTOR THAT OPENS IN <paramref name="mode"/> CAN DECLARE (kb/Work PB3479). §14.9.27.3 SR2:
    /// <i>"The EXTEND phrase shall be specified only if the access mode of the file connector referenced by file-name-1 is
    /// sequential"</i>, and the compiler refuses the other declarations (COBOLNET2372), so a RELATIVE or INDEXED connector
    /// opened EXTEND in a test is a state no program reaches. Every other open mode is legal under every access mode, and
    /// DYNAMIC is the one that admits both statement forms, so it is what the harness declares when the OPEN does not force
    /// SEQUENTIAL. A theory that crosses open modes registers each connector with <c>access: AccessFor(mode)</c> and opens it
    /// through <see cref="Open"/>, which fails the test that disagrees.</summary>
    public static KeyedAccess AccessFor(FileOpenMode mode) =>
        mode == FileOpenMode.Extend ? KeyedAccess.Sequential : KeyedAccess.Dynamic;

    /// <summary>The access mode <see cref="Register"/> declared for each connector it registered (the registry keeps the
    /// physical file a connector names, <see cref="FileRegistry.HostPathOf"/>, but not its access mode). Weak on the
    /// registry, so a test's registry takes its entries with it.</summary>
    private static readonly ConditionalWeakTable<FileRegistry, Dictionary<string, KeyedAccess>> Declared = new();

    /// <summary>Fail the test that would OPEN <paramref name="name"/> in <paramref name="mode"/> over a declaration no
    /// program makes: EXTEND over a connector <see cref="Register"/> declared with RANDOM or DYNAMIC access (§14.9.27.3 SR2,
    /// <see cref="AccessFor"/>). A connector no <see cref="Register"/> call declared is not checked, so a test that
    /// registers one with the registry's own methods keeps its own declaration. <see cref="Open"/> and the unit tests'
    /// <c>OpenStatic</c> shorthand both call it, so the check has no path around it.</summary>
    public static void AssertDeclarationAdmits(FileRegistry reg, string name, FileOpenMode mode)
    {
        if (mode != FileOpenMode.Extend || !Declared.TryGetValue(reg, out var declarations)
            || !declarations.TryGetValue(name, out var access) || access == KeyedAccess.Sequential) return;
        Assert.Fail($"OPEN EXTEND over '{name}' declared with {access} access: §14.9.27.3 SR2 allows EXTEND only "
            + "for sequential access, so this declaration is no program (FileHarness.AccessFor gives the declaration "
            + "an OPEN in this mode can make).");
    }

    /// <summary>OPEN <paramref name="name"/> in <paramref name="mode"/>, as <c>OPEN mode name</c> does for a connector
    /// <see cref="Register"/> declared, after <see cref="AssertDeclarationAdmits"/>. The connector must have been
    /// registered through <see cref="Register"/>.</summary>
    public static void Open(FileRegistry reg, string name, FileOpenMode mode)
    {
        Assert.True(Declared.TryGetValue(reg, out var declarations) && declarations.ContainsKey(name),
            $"'{name}' was not registered through FileHarness.Register, so its declaration cannot be checked");
        AssertDeclarationAdmits(reg, name, mode);
        reg.Open(name, mode, reg.HostPathOf(name), assignDynamic: false, page: null);
    }

    /// <summary>Register <paramref name="name"/> over <paramref name="host"/> as the program declares it: dynamic access
    /// (or <paramref name="access"/>, which a connector opened EXTEND sets to <see cref="AccessFor"/>'s answer; a SEQUENTIAL
    /// file has only sequential access, so it ignores it), the sharing
    /// <paramref name="sharing"/> under <paramref name="lockMode"/> (no sharing clause at all when
    /// <paramref name="sharing"/> is null: the NO OTHER default). With <paramref name="alternateKey"/> an INDEXED file also
    /// has an alternate key WITH DUPLICATES over the payload (positions 5-8), so a record is two trees' entries. Every
    /// connector on one physical file declares the SAME attributes, because §14.9.27.4 GR10 compares them at each
    /// OPEN.</summary>
    public static void Register(FileRegistry reg, string name, string host, FileOrg org,
        FileSharing? sharing = FileSharing.AllOther, FileLockMode lockMode = FileLockMode.Manual, bool optional = false,
        KeyedAccess access = KeyedAccess.Dynamic, bool alternateKey = false)
    {
        Assert.True(!alternateKey || org == FileOrg.Indexed, "only an INDEXED file has an alternate key");
        Declared.GetOrCreateValue(reg)[name] = org == FileOrg.Sequential ? KeyedAccess.Sequential : access;
        switch (org)
        {
            case FileOrg.Sequential:
                reg.Register(name, host, recordWidth: 8, lineSequential: false, optional: optional, varyMin: -1, varyMax: -1);
                break;
            case FileOrg.Relative:
                reg.RegisterRelative(name, host, recordWidth: 8, optional: optional, accessMode: (int)access,
                    relativeKeyDigits: 4, varyMin: -1, varyMax: -1);
                break;
            case FileOrg.Indexed:
                reg.RegisterIndexed(name, host, recordWidth: 8, optional: optional, accessMode: (int)access,
                    primeKey: [new(0, 4)], varyMin: -1, varyMax: -1);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(org), org, "a new organization owes its arm here");
        }
        if (alternateKey) reg.AddAlternateKey(name, [new(4, 4)], duplicates: true);
        if (sharing is { } s) reg.RegisterSharing(name, s, lockMode, multiple: false);
    }

    /// <summary>Record <paramref name="n"/>'s image: its key (also its relative record number) and a payload.</summary>
    public static string Rec(int n, string payload) => $"{n:0000}{payload}";

    /// <summary>Make record <paramref name="n"/> the one the next statement names: a RELATIVE file's relative key; an
    /// INDEXED file's key is in the record image, so nothing to do.</summary>
    public static void Position(FileRegistry reg, string name, FileOrg org, int n)
    {
        if (org == FileOrg.Relative) reg.SetRelativeKey(name, n);
    }

    public static string Write(FileRegistry reg, string name, FileOrg org, int n, string payload)
    {
        Position(reg, name, org, n);
        return reg.WriteShared(name, Rec(n, payload), -1, FileRecordLock.None, FileRetryKind.None, 0, page: null);
    }

    public static string Rewrite(FileRegistry reg, string name, FileOrg org, int n, string payload)
    {
        Position(reg, name, org, n);
        return reg.RewriteShared(name, Rec(n, payload), -1, FileRecordLock.None, FileRetryKind.None, 0);
    }

    /// <summary>A DELETE RECORD of record <paramref name="n"/>: not for a SEQUENTIAL file (§14.9.10.3 SR1).</summary>
    public static string Delete(FileRegistry reg, string name, FileOrg org, int n)
    {
        Assert.True(org != FileOrg.Sequential, "§14.9.10.3 SR1: DELETE RECORD is not for a file with sequential organization");
        Position(reg, name, org, n);
        return reg.DeleteShared(name, Rec(n, "    "), FileRetryKind.None, 0);
    }

    /// <summary>A random READ of record <paramref name="n"/> by its key: a keyed organization's statement (a SEQUENTIAL
    /// file is read with <see cref="ReadNext"/>).</summary>
    public static string Read(FileRegistry reg, string name, FileOrg org, int n, out string image,
        FileRecordLock phrase = FileRecordLock.None, bool ignoringLock = false, FileRetryKind retry = FileRetryKind.None,
        long amount = 0)
    {
        Assert.True(org != FileOrg.Sequential, "a keyed READ names a key, which a sequential file has none of");
        Position(reg, name, org, n);
        return reg.ReadKeyedShared(name, keyIndex: -1, Rec(n, "    "), phrase, ignoringLock, retry, amount, out image);
    }

    /// <summary>READ NEXT (or READ PREVIOUS when <paramref name="previous"/>) on any organization: the record after the
    /// one the connector is positioned at.</summary>
    public static string ReadNext(FileRegistry reg, string name, out string image, FileRecordLock phrase = FileRecordLock.None,
        bool ignoringLock = false, bool previous = false) =>
        reg.ReadShared(name, previous, phrase, advancingOnLock: false, ignoringLock, FileRetryKind.None, 0, out image);

    /// <summary>A file whose record <i>n</i> (1 to <paramref name="payloads"/>.Length) carries <c>payloads[n - 1]</c>,
    /// written by an OUTPUT connector with no sharing clause and CLOSEd (so committed). The file has the alternate key
    /// of <see cref="Register"/> when <paramref name="alternateKey"/>.</summary>
    public static void Seed(string host, FileOrg org, string[] payloads, bool alternateKey = false) =>
        Seed(host, org, [.. payloads.Select((payload, i) => (i + 1, payload))], alternateKey);

    /// <summary>A file whose records are exactly <paramref name="records"/> (key, payload), in that order: for a file whose
    /// keys are not 1 to n.</summary>
    public static void Seed(string host, FileOrg org, (int Key, string Payload)[] records, bool alternateKey = false)
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
    public static void SeedRecords(string host, FileOrg org, int records) =>
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
