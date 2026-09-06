// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using CobolNet.Runtime;
using CobolNet.Runtime.IO;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The persisted fixed-file-attribute catalog (ISO/IEC 1989:2023 §9.1.6) and the §14.9.27.4 GR10 comparison the
/// OPEN statement performs against it — kb/Work PB193. The end-to-end behaviour per organization rides the
/// goldens (<c>85/open_fixed_attribute_conflict</c>, <c>2023/open_fixed_attribute_conflict_ix</c>); these lock
/// the properties a single-process golden CANNOT see:
/// <list type="bullet">
/// <item>the catalog is on DISK, not in this process — an in-memory catalog would pass every golden while
/// leaving the real case, a file written by an EARLIER RUN, unprotected;</item>
/// <item>a file whose attributes are NOT recorded (an external tool's, or one from a build older than the
/// catalog) opens normally — GR10 compares against the file's attributes, and there are none to compare;</item>
/// <item>DELETE FILE takes the catalog with the file, so it can never be compared against a different file
/// later created at the same path.</item>
/// </list>
/// </summary>
public sealed class FixedFileAttributeCatalogTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "CobolNet_FFA_" + Guid.NewGuid().ToString("N")[..8]);

    public FixedFileAttributeCatalogTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* best-effort */ }
    }

    private string Host(string name) => Path.Combine(_dir, name);

    /// <summary>Create a physical file through a RELATIVE connector of 10-byte records — §14.9.27.4 GR18 makes
    /// the OPEN OUTPUT a creation, which is where §9.1.6 fixes a file's attributes.</summary>
    private void MakeRelativeTenByteFile(string host)
    {
        CobolFile.Init();
        CobolFile.RegisterRelative("MK", host, 10, false, 0, 0);
        CobolFile.OpenOutput("MK", host, assignDynamic: false, page: null);
        Assert.Equal(FileStatusCode.Success, CobolFile.Status("MK"));
        CobolFile.Close("MK");
    }

    /// <summary>Create a physical file through a record-SEQUENTIAL connector of fixed 20-byte records, holding
    /// one record — the shape a print, report or extract file has.</summary>
    private static void MakeSequentialTwentyByteFile(string host)
    {
        CobolFile.Init();
        CobolFile.Register("MK", host, 20, lineSequential: false, optional: false);
        CobolFile.OpenOutput("MK", host, assignDynamic: false, page: null);
        CobolFile.Write("MK", "ABCDEFGHIJKLMNOPQRST", -1, page: null);
        CobolFile.Close("MK");
        Assert.Equal(FileStatusCode.Success, CobolFile.Status("MK"));
    }

    /// <summary>A fresh run unit whose only connector opens the file INPUT under a record-sequential
    /// organization and a 40-byte record, and returns its I-O status.
    /// <para>⛔ Every caller of this helper builds its subject with <see cref="MakeRelativeTenByteFile"/>, and
    /// that is load-bearing: the contradiction it relies on is the ORGANIZATION (RELATIVE vs sequential), which
    /// §14.9.27.4 GR10 validates for every organization. The 40-vs-10 record size is a SECOND contradiction only
    /// because a relative store's sizes are validated too — over a SEQUENTIAL subject this connector contradicts
    /// nothing the sequential set compares and answers '00' (see
    /// <see cref="FixedFileAttributes.Conflicts"/>).</para></summary>
    private static string OpenContradictingConnector(string host)
    {
        CobolFile.Init();   // ⛔ a NEW registry: no connector, no catalog and no status survives from above
        CobolFile.Register("RD", host, 40, lineSequential: false, optional: false);
        CobolFile.OpenInput("RD", host, assignDynamic: false, page: null);
        return CobolFile.Status("RD");
    }

    [Fact]
    public void CatalogIsOnDisk_SoTheAttributesOutliveTheProcessThatWroteThem()
    {
        string host = Host("ffa-durable.dat");
        MakeRelativeTenByteFile(host);

        // The whole catalog is these bytes. Nothing in the process holds it, so a LATER RUN — which has only
        // the file system — sees exactly this and no less.
        string sidecar = FixedFileAttributes.SidecarPath(host);
        Assert.True(File.Exists(sidecar), $"the catalog sidecar {sidecar} must exist beside the data file");
        Assert.Equal(
            "COBOLNET-FIXED-FILE-ATTRIBUTES 1\norganization=RELATIVE\nrecord-type=FIXED\nrecord-min=10\nrecord-max=10\n",
            File.ReadAllText(sidecar).Replace("\r\n", "\n"));

        // §14.9.27.4 GR10 — organization and record size both differ: the OPEN is unsuccessful, '39'.
        Assert.Equal(FileStatusCode.FixedAttributeConflict, OpenContradictingConnector(host));
    }

    [Fact]
    public void MatchingConnectorOpensNormally()
    {
        string host = Host("ffa-match.dat");
        MakeRelativeTenByteFile(host);
        CobolFile.Init();
        CobolFile.RegisterRelative("RD", host, 10, false, 0, 0);
        CobolFile.OpenInput("RD", host, assignDynamic: false, page: null);
        Assert.Equal(FileStatusCode.Success, CobolFile.Status("RD"));   // the check rejects only real conflicts
        CobolFile.Close("RD");
    }

    [Fact]
    public void AFileWhoseAttributesAreNotRecorded_OpensNormally()
    {
        string host = Host("ffa-norecord.dat");
        MakeRelativeTenByteFile(host);
        File.Delete(FixedFileAttributes.SidecarPath(host));   // as a file written by an external tool arrives

        // GR10 compares the connector with "the fixed file attributes of the file". A file that does not state
        // them supplies nothing to compare, so nothing is validated — answering '39' here would reject legal
        // programs over a missing implementor artifact. The arithmetic fallback for this file is
        // RecordLayoutNotice, which reports on stderr and leaves the I-O status alone.
        Assert.Equal(FileStatusCode.Success, OpenContradictingConnector(host));
    }

    [Theory]
    [InlineData("not a catalog at all\n")]
    [InlineData("COBOLNET-FIXED-FILE-ATTRIBUTES 2\norganization=RELATIVE\n")]   // a LATER format version
    [InlineData("COBOLNET-FIXED-FILE-ATTRIBUTES 1\norganization=RELATIVE\n")]   // truncated: no record sizes
    [InlineData("COBOLNET-FIXED-FILE-ATTRIBUTES 1\norganization=RELATIVE\nrecord-min=x\nrecord-max=10\n")]
    [InlineData("COBOLNET-FIXED-FILE-ATTRIBUTES 1\norganization=RELATIVE\nrecord-type=SPANNED\nrecord-min=10\nrecord-max=10\n")]
    // §9.1.6 names exactly THREE organizations, so an "organization" outside them means the file's primary
    // attribute is unknown — "not recorded", never a value that compares unequal to every declared organization
    // and so MANUFACTURES a '39'. LINE-SEQUENTIAL is the concrete case: it was a recorded value while the
    // record delimiter was wrongly folded into the organization, and such a sidecar must read as unrecorded.
    [InlineData("COBOLNET-FIXED-FILE-ATTRIBUTES 1\norganization=PARTITIONED\nrecord-type=FIXED\nrecord-min=10\nrecord-max=10\n")]
    [InlineData("COBOLNET-FIXED-FILE-ATTRIBUTES 1\norganization=LINE-SEQUENTIAL\nrecord-type=FIXED\nrecord-min=10\nrecord-max=10\n")]
    [InlineData("COBOLNET-FIXED-FILE-ATTRIBUTES 1\norganization=RELATIVE\nrecord-type=FIXED\nrecord-min=10\nrecord-max=10\nkey=0,4,N\n")]
    public void AnUnreadableSidecarIsNotAConflict(string content)
    {
        string host = Host("ffa-bad.dat");
        MakeRelativeTenByteFile(host);
        File.WriteAllText(FixedFileAttributes.SidecarPath(host), content, new UTF8Encoding(false));

        // Every unreadable form degrades to "the attributes are not recorded", never to a manufactured '39'.
        Assert.Null(FixedFileAttributes.Load(host));
        Assert.Equal(FileStatusCode.Success, OpenContradictingConnector(host));
    }

    [Fact]
    public void DeleteFileTakesTheCatalogWithTheFile()
    {
        string host = Host("ffa-delete.dat");
        MakeRelativeTenByteFile(host);
        Assert.True(File.Exists(FixedFileAttributes.SidecarPath(host)));

        CobolFile.Init();
        CobolFile.RegisterRelative("DK", host, 10, false, 0, 0);
        Assert.Equal(FileStatusCode.Success, CobolFile.DeleteFile("DK"));
        Assert.False(File.Exists(host));
        Assert.False(File.Exists(FixedFileAttributes.SidecarPath(host)),
            "a catalog that outlived its file would judge a DIFFERENT file later created at the same path");
    }

    [Fact]
    public void DeleteFileTakesTheCatalogEvenWhenTheFileWasAlreadyGone()
    {
        // §14.9.10.4 GR14 makes a DELETE FILE of an ABSENT file a SUCCESSFUL completion, status '05' — and a
        // sidecar left beside an already-absent file is the same stale catalog as one left beside a deleted
        // one. The removal keys on the success family, not on '00'.
        string host = Host("ffa-delete05.dat");
        MakeRelativeTenByteFile(host);
        File.Delete(host);                                    // the data file goes; the catalog is left behind
        Assert.True(File.Exists(FixedFileAttributes.SidecarPath(host)));

        CobolFile.Init();
        CobolFile.RegisterRelative("DK", host, 10, false, 0, 0);
        Assert.Equal(FileStatusCode.OptionalFileNotFound, CobolFile.DeleteFile("DK"));   // '05' GR14
        Assert.False(File.Exists(FixedFileAttributes.SidecarPath(host)));
    }

    [Fact]
    public void AnUnsuccessfulDeleteFileLeavesTheCatalogWithTheFile()
    {
        // The mirror: '41' (the connector is open, GR13) does not delete the file, so it must not delete the
        // file's attributes either — a DELETE FILE that failed must leave GR10 exactly as strong as it was.
        string host = Host("ffa-delete41.dat");
        MakeRelativeTenByteFile(host);
        CobolFile.Init();
        CobolFile.RegisterRelative("DK", host, 10, false, 0, 0);
        CobolFile.OpenInput("DK", host, assignDynamic: false, page: null);
        Assert.Equal(FileStatusCode.FileAlreadyOpen, CobolFile.DeleteFile("DK"));        // '41' GR13
        Assert.True(File.Exists(host));
        Assert.True(File.Exists(FixedFileAttributes.SidecarPath(host)));
        CobolFile.Close("DK");
    }

    // ── The §14.9.27.4 GR10 validated set VARIES BY ORGANIZATION (GR10 sentence 3) ───────────────────────────
    //    "The implementor defines which of the fixed-file attributes are validated during the execution of the
    //    OPEN statement. The validation of fixed-file attributes may vary depending on the organization or
    //    storage medium of the file." These four lock BOTH arms of COBOL.NET's determination — a set that is
    //    too broad rejects legal source just as surely as one that is too narrow reads rubbish, and only the
    //    pair of directions can tell the two apart.

    [Theory]
    [InlineData(30, false)]   // a WIDER record description over the same record-sequential file
    [InlineData(9, false)]    // a NARROWER one — the report-file read-back shape
    [InlineData(30, true)]    // and a LINE SEQUENTIAL one: §9.1.6's record delimiter, also not validated
    public void ASequentialFilesRecordSizeAndDelimiterAreNotValidated(int width, bool lineSequential)
    {
        // §9.1.7.2 puts a sequential file's record lengths in the DATA and in the READING program, not in the
        // file: "In record sequential files the length of each record is determined by any information the
        // implementor may add to the record on the physical storage medium (such as record length headers)" —
        // COBOL.NET adds none to a fixed-length one — and "In line sequential files the length of each record
        // is determined by the number of characters between the preceding line delimiter and the following
        // line delimiter or the end of file if no line delimiter is present". The standard answers the
        // resulting disagreement with a SUCCESSFUL completion, §9.1.13.2 item 3's '04' and item 5's '06', so
        // GR10's '39' must NOT fire first — writing a print or report file and reading it back under a
        // different record description is exactly the idiom those statuses exist for.
        string host = Host($"ffa-seq-{width}-{lineSequential}.dat");
        MakeSequentialTwentyByteFile(host);

        CobolFile.Init();
        CobolFile.Register("RD", host, width, lineSequential, optional: false);
        CobolFile.OpenInput("RD", host, assignDynamic: false, page: null);
        Assert.Equal(FileStatusCode.Success, CobolFile.Status("RD"));
        CobolFile.Close("RD");
    }

    [Fact]
    public void ASequentialFilesORGANIZATION_IsStillValidated()
    {
        // The set for a sequential file is not EMPTY: §9.1.6's "primary attribute" is validated for every
        // organization, so a RELATIVE description over a sequential byte stream is still the '39' of GR10 —
        // that disagreement is the one a re-read cannot recover from.
        string host = Host("ffa-seq-as-relative.dat");
        MakeSequentialTwentyByteFile(host);

        CobolFile.Init();
        CobolFile.RegisterRelative("RD", host, 20, false, 0, 0);
        CobolFile.OpenInput("RD", host, assignDynamic: false, page: null);
        Assert.Equal(FileStatusCode.FixedAttributeConflict, CobolFile.Status("RD"));
    }

    [Fact]
    public void ARelativeStoresRecordSizeIsValidated()
    {
        // The complement, and the reason the determination is per organization rather than uniform: a relative
        // store is an implementor-defined structure whose slot layout IS the record size, so a description that
        // disagrees cannot interpret it — GR10's '39'.
        string host = Host("ffa-rel-size.dat");
        MakeRelativeTenByteFile(host);

        CobolFile.Init();
        CobolFile.RegisterRelative("RD", host, 40, false, 0, 0);
        CobolFile.OpenInput("RD", host, assignDynamic: false, page: null);
        Assert.Equal(FileStatusCode.FixedAttributeConflict, CobolFile.Status("RD"));
    }

    [Fact]
    public void TheMediumClassification_CoversEveryOrganizationTheConnectorsCanRecord()
    {
        // A drift guard on GR10 sentence 3's one exercise point. §9.1.6 names exactly three organizations, and
        // FileConnector.CatalogOrganization can produce exactly those three constants; every one of them must
        // be a DELIBERATE arm of the classification, not the conservative default. Adding a fourth organization
        // without classifying it would silently stop validating its record layout.
        Assert.False(FixedFileAttributes.MediumFixesRecordLayout(FixedFileAttributes.Sequential));
        Assert.True(FixedFileAttributes.MediumFixesRecordLayout(FixedFileAttributes.Relative));
        Assert.True(FixedFileAttributes.MediumFixesRecordLayout(FixedFileAttributes.Indexed));
        Assert.False(FixedFileAttributes.MediumFixesRecordLayout("SOMETHING-ELSE"));
    }

    [Fact]
    public void ASequentialFileStillRECORDSItsSizes_EvenThoughTheyAreNotValidated()
    {
        // §9.1.6 makes the record sizes fixed attributes of the file whatever the validated set is, so the
        // catalog records the truth for every organization and Conflicts decides what to compare. Recording
        // them is also what lets a later build widen the set without making today's files unreadable.
        string host = Host("ffa-seq-recorded.dat");
        MakeSequentialTwentyByteFile(host);
        Assert.Equal(
            "COBOLNET-FIXED-FILE-ATTRIBUTES 1\norganization=SEQUENTIAL\nrecord-type=FIXED\nrecord-min=20\nrecord-max=20\n",
            File.ReadAllText(FixedFileAttributes.SidecarPath(host)).Replace("\r\n", "\n"));
    }

    [Fact]
    public void OpenOutputReestablishesTheAttributes_GR18()
    {
        string host = Host("ffa-recreate.dat");
        MakeRelativeTenByteFile(host);

        // §14.9.27.4 GR18: "If the OUTPUT phrase is specified, the successful execution of the OPEN statement
        // creates the file" — and §9.1.6 fixes the attributes at creation. So OPEN OUTPUT is never judged
        // against the previous file's attributes; it replaces them.
        CobolFile.Init();
        CobolFile.Register("WR", host, 40, lineSequential: false, optional: false);
        CobolFile.OpenOutput("WR", host, assignDynamic: false, page: null);
        Assert.Equal(FileStatusCode.Success, CobolFile.Status("WR"));
        CobolFile.Close("WR");

        var recorded = FixedFileAttributes.Load(host);
        Assert.NotNull(recorded);
        Assert.Equal("SEQUENTIAL", recorded!.Organization);
        Assert.Equal(40, recorded.MaxRecordSize);

        // ... and the connector that matched before now conflicts.
        CobolFile.Init();
        CobolFile.RegisterRelative("RD", host, 10, false, 0, 0);
        CobolFile.OpenInput("RD", host, assignDynamic: false, page: null);
        Assert.Equal(FileStatusCode.FixedAttributeConflict, CobolFile.Status("RD"));
    }

    [Fact]
    public void AnAbsentOptionalFileCreatedByOpenExtend_RecordsItsAttributes_GR17()
    {
        // §14.9.27.4 GR17: an absent OPTIONAL file opened EXTEND (or I-O) is created "as if OPEN OUTPUT /
        // CLOSE" were executed — the other moment §9.1.6's "at the time it is created" names.
        string host = Host("ffa-optional.dat");
        CobolFile.Init();
        CobolFile.Register("OP", host, 12, lineSequential: false, optional: true);
        CobolFile.OpenExtend("OP", host, assignDynamic: false, page: null);
        Assert.Equal(FileStatusCode.OptionalFileNotFound, CobolFile.Status("OP"));   // '05' GR17
        CobolFile.Close("OP");

        var recorded = FixedFileAttributes.Load(host);
        Assert.NotNull(recorded);
        Assert.Equal(FixedFileAttributes.Sequential, recorded!.Organization);
        Assert.Equal(12, recorded.MaxRecordSize);

        // ⛔ The contradiction has to be one the SEQUENTIAL organization actually validates. This file was
        // created record-sequential, so its record sizes are recorded but not compared (§9.1.7.2 / §9.1.13.2
        // item 3 — see FixedFileAttributes.Conflicts): a wider sequential connector would open '00' and prove
        // nothing about GR17. The organization is validated for every organization, so that is the probe.
        CobolFile.Init();
        CobolFile.RegisterRelative("RD", host, 12, false, 0, 0);
        CobolFile.OpenInput("RD", host, assignDynamic: false, page: null);
        Assert.Equal(FileStatusCode.FixedAttributeConflict, CobolFile.Status("RD"));
    }

    [Fact]
    public void AnAbsentOptionalFileOpenedInput_IsNotCreated_AndRecordsNothing()
    {
        // Table 18: an OPTIONAL file opened INPUT while unavailable is a normal open whose first read is the at
        // end condition — the file is NOT created, so there is nothing whose attributes could be fixed.
        string host = Host("ffa-absent.dat");
        CobolFile.Init();
        CobolFile.Register("AB", host, 12, lineSequential: false, optional: true);
        CobolFile.OpenInput("AB", host, assignDynamic: false, page: null);
        Assert.Equal(FileStatusCode.OptionalFileNotFound, CobolFile.Status("AB"));
        CobolFile.Close("AB");
        Assert.False(File.Exists(FixedFileAttributes.SidecarPath(host)));
    }

    [Fact]
    public void IndexedKeyGeometryRoundTripsThroughTheCatalog()
    {
        string host = Host("ffa-ix.dat");
        CobolFile.Init();
        CobolFile.RegisterIndexed("IX", host, 20, false, 0, primeOffset: 0, primeLength: 4);
        CobolFile.AddAlternateKey("IX", 4, 5, duplicates: true, suppress: "ZZZZZ");
        CobolFile.OpenOutput("IX", host, assignDynamic: false, page: null);
        CobolFile.Close("IX");

        var recorded = FixedFileAttributes.Load(host);
        Assert.NotNull(recorded);
        Assert.Equal("INDEXED", recorded!.Organization);
        Assert.Equal(2, recorded.Keys.Count);
        Assert.Equal(new FixedFileAttributes.KeyDescriptor(0, 4, false, null, "NATIVE"), recorded.Keys[0]);
        Assert.Equal(new FixedFileAttributes.KeyDescriptor(4, 5, true, "ZZZZZ", "NATIVE"), recorded.Keys[1]);
    }

    [Fact]
    public void ASuppressValueCarryingTheFormatsOwnDelimitersRoundTrips()
    {
        // §12.4.5.6.4 GR6 admits any literal as the SUPPRESS WHEN value, including one holding a comma or a
        // line ending — which are the catalog's own field and record separators. Hex encoding is what keeps a
        // legal COBOL program from writing an unparseable catalog (and so from silently losing validation).
        string host = Host("ffa-suppress.dat");
        CobolFile.Init();
        CobolFile.RegisterIndexed("IX", host, 12, false, 0, primeOffset: 0, primeLength: 4);
        CobolFile.AddAlternateKey("IX", 4, 4, duplicates: false, suppress: "a,\r\nb");
        CobolFile.OpenOutput("IX", host, assignDynamic: false, page: null);
        CobolFile.Close("IX");

        var recorded = FixedFileAttributes.Load(host);
        Assert.NotNull(recorded);
        Assert.Equal("a,\r\nb", recorded!.Keys[1].Suppress);
        Assert.False(recorded.Conflicts(recorded));   // and so the file does not conflict with itself
    }

    [Fact]
    public void FingerprintIsNativeForNoSequence_AndIsStable()
    {
        Assert.Equal("NATIVE", FixedFileAttributes.Fingerprint(null));
        // A sequence's identity is its WEIGHTS, so the same sequence fingerprints identically every time and a
        // reordered one does not (§9.1.6 — the collating sequence of the keys is a fixed file attribute).
        var a = OrdinalSequence(swapAB: false);
        var b = OrdinalSequence(swapAB: false);
        var c = OrdinalSequence(swapAB: true);
        Assert.Equal(FixedFileAttributes.Fingerprint(a), FixedFileAttributes.Fingerprint(b));
        Assert.NotEqual(FixedFileAttributes.Fingerprint(a), FixedFileAttributes.Fingerprint(c));
        Assert.NotEqual("NATIVE", FixedFileAttributes.Fingerprint(c));
    }

    /// <summary>An ALPHABET-shaped sequence over the native ordinal positions (§12.3.7.4 GR7), optionally with
    /// the positions of 'A' and 'B' exchanged — two sequences that order key values differently.</summary>
    private static AlphanumericCollation OrdinalSequence(bool swapAB)
    {
        // The SPARSE §12.3.7.4 GR7 k table (kb/Work PB770): specify codes 0..255 at their own positions, so the
        // sequence IS the native order except for the optional 'A'/'B' exchange; everything above follows natively.
        var codes = Enumerable.Range(0, 256).Select(c => (ushort)c).ToArray();
        var positions = Enumerable.Range(0, 256).Select(c => (ushort)c).ToArray();
        var repByPos = Enumerable.Range(0, 256).Select(c => (ushort)c).ToArray();
        if (swapAB)
        {
            (positions['A'], positions['B']) = (positions['B'], positions['A']);
            (repByPos['A'], repByPos['B']) = (repByPos['B'], repByPos['A']);
        }
        return new AlphanumericCollation(codes, positions, repByPos, 256);
    }
}
