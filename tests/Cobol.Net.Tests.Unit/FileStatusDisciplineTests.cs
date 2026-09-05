// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The ISO §9.1.13 I-O status DISCIPLINE seams kb/Work PB140 closed — pinned at the runtime registry over a
/// PRIVATE <see cref="FileRegistry"/> instance (no shared static state, so these parallelize freely):
/// the §9.1.13.7 item 3 '43' gate dropping on EVERY intervening statement (the Status-setter chokepoint),
/// CLOSE WITH LOCK only locking on a successful close, the REEL/UNIT surface honoring §14.9.6.4 GR6 for an
/// absent optional file, the loud unregistered-connector invariant that replaced the fail-open '00', and the
/// DELETE FILE GR17 medium-refusal classification. The end-to-end FILE STATUS observations ride the
/// pb140_* goldens; these facts reach the arms no golden can (the throw branches, the exception mapping).
/// </summary>
public sealed class FileStatusDisciplineTests
{
    private const int SeqAccess = 0;   // KeyedAccess.Sequential

    private static string Tmp(string tag) =>
        Path.Combine(Path.GetTempPath(), $"pb140-{tag}-{Guid.NewGuid():N}.dat");

    /// <summary>A relative ACCESS SEQUENTIAL connector loaded with <paramref name="records"/> and reopened I-O
    /// — the §9.1.13.7 3) DELETE-gate scenario's starting state.</summary>
    private static (FileRegistry reg, string host) SeqRelativeIo(params string[] records)
    {
        var reg = new FileRegistry();
        string host = Tmp("rel");
        reg.RegisterRelative("F", host, 8, false, SeqAccess, 4, -1, -1);
        reg.OpenStatic("F", FileOpenMode.Output);
        foreach (string r in records) reg.WriteKeyed("F", r.PadRight(8), -1);
        reg.Close("F");
        reg.OpenStatic("F", FileOpenMode.IO);
        return (reg, host);
    }

    // §9.1.13.7 3): UNLOCK is in §9.1.13.1's statement set, so a READ / UNLOCK / DELETE sequence must answer
    // '43' — the record survives and a fresh READ re-arms the gate. (The old hand-cleared flag survived
    // UNLOCK: the DELETE answered '00' and removed the record.)
    [Fact]
    public void ReadGate_UnlockDropsIt_DeleteIs43_RecordSurvives()
    {
        var (reg, host) = SeqRelativeIo("AAAAAAAA", "BBBBBBBB");
        try
        {
            Assert.Equal("00", reg.ReadKeyedNext("F", out string img));
            Assert.Equal("AAAAAAAA", img);
            reg.Unlock("F", records: false);
            Assert.Equal("00", reg.Status("F"));
            Assert.Equal(FileStatusCode.NoSuccessfulReadBeforeDeleteRewrite, reg.DeleteRecord("F", ""));
            Assert.Equal("00", reg.ReadKeyedNext("F", out img));   // record 2 — record 1 was NOT deleted
            Assert.Equal("BBBBBBBB", img);
            Assert.Equal("00", reg.DeleteRecord("F", ""));         // the fresh READ re-armed the gate
        }
        finally { try { File.Delete(host); } catch { } }
    }

    // §9.1.13.7 3) + item 1: the already-open OPEN ('41') is an executed input-output statement — the third
    // leak site (the '41' arm returned before the hand reset ran).
    [Fact]
    public void ReadGate_AlreadyOpen41DropsIt_DeleteIs43()
    {
        var (reg, host) = SeqRelativeIo("AAAAAAAA");
        try
        {
            Assert.Equal("00", reg.ReadKeyedNext("F", out _));
            reg.OpenStatic("F", FileOpenMode.IO);
            Assert.Equal(FileStatusCode.FileAlreadyOpen, reg.Status("F"));
            Assert.Equal(FileStatusCode.NoSuccessfulReadBeforeDeleteRewrite, reg.DeleteRecord("F", ""));
        }
        finally { try { File.Delete(host); } catch { } }
    }

    // §9.1.13.7 3): DELETE FILE (on the open connector — '41', §14.9.10.4 GR13) likewise drops the gate.
    [Fact]
    public void ReadGate_DeleteFile41DropsIt_DeleteIs43()
    {
        var (reg, host) = SeqRelativeIo("AAAAAAAA");
        try
        {
            Assert.Equal("00", reg.ReadKeyedNext("F", out _));
            Assert.Equal(FileStatusCode.FileAlreadyOpen, reg.DeleteFile("F"));
            Assert.Equal(FileStatusCode.NoSuccessfulReadBeforeDeleteRewrite, reg.DeleteRecord("F", ""));
        }
        finally { try { File.Delete(host); } catch { } }
    }

    // §14.9.6.4 GR1: an unsuccessful CLOSE WITH LOCK ('42' — never opened) performs NO closing action, so a
    // later OPEN must succeed. (The old unconditional _locked.Add answered '38' forever after.)
    [Fact]
    public void CloseWithLock_Unsuccessful42_DoesNotPoisonLaterOpen()
    {
        var reg = new FileRegistry();
        string host = Tmp("cwl");
        reg.Register("S", host, 8, lineSequential: false, optional: false, -1, -1);
        try
        {
            reg.CloseWithLock("S");
            Assert.Equal(FileStatusCode.FileNotOpen, reg.Status("S"));
            reg.OpenStatic("S", FileOpenMode.Output);
            Assert.Equal(FileStatusCode.Success, reg.Status("S"));   // NOT '38'
            reg.CloseWithLock("S");                                  // the successful close DOES lock
            Assert.Equal(FileStatusCode.Success, reg.Status("S"));
            reg.OpenStatic("S", FileOpenMode.Input);
            Assert.Equal(FileStatusCode.FileLocked, reg.Status("S"));
        }
        finally { try { File.Delete(host); } catch { } }
    }

    // §14.9.6.4 GR6: for an absent OPTIONAL input file NO unit processing is performed — CLOSE REEL/UNIT
    // completes successfully ('00', not the '07' unit-processing warning), and the connector stays open for
    // the real CLOSE. §9.1.13.7 item 2: on a not-open file the surface answers '42'.
    [Fact]
    public void CloseReelUnit_AbsentOptional_NoUnitProcessing()
    {
        var reg = new FileRegistry();
        string host = Tmp("opt");
        reg.Register("O", host, 8, lineSequential: false, optional: true, -1, -1);
        reg.OpenStatic("O", FileOpenMode.Input);
        Assert.Equal(FileStatusCode.OptionalFileNotFound, reg.Status("O"));   // '05' — open, not present
        reg.CloseReelUnit("O");
        Assert.Equal(FileStatusCode.Success, reg.Status("O"));                // GR6 — no '07'
        reg.Close("O");
        Assert.Equal(FileStatusCode.Success, reg.Status("O"));
        reg.CloseReelUnit("O");
        Assert.Equal(FileStatusCode.FileNotOpen, reg.Status("O"));            // '42'
    }

    // The unregistered-connector fail-open ('00' to the FILE STATUS item while the statement's own local held
    // '30') is replaced by a LOUD invariant — prove the failure branch fires.
    [Fact]
    public void Status_OfAnUnregisteredName_IsLoud()
    {
        var reg = new FileRegistry();
        Assert.Throws<InvalidOperationException>(() => reg.Status("NEVER-REGISTERED"));
    }

    // §14.9.6.3 SR1 binds REEL/UNIT to sequential organization; a keyed connector reaching the surface is a
    // compiler defect and must be loud, never a silently-skipped status assignment.
    [Fact]
    public void CloseReelUnit_OnAKeyedConnector_IsLoud()
    {
        var reg = new FileRegistry();
        reg.RegisterRelative("R", Tmp("reel"), 8, false, SeqAccess, 4, -1, -1);
        Assert.Throws<InvalidOperationException>(() => reg.CloseReelUnit("R"));
    }

    // §14.9.10.4 GR17 vs §9.1.13.6 item 1: a medium that refuses deletion (Windows ERROR_WRITE_PROTECT,
    // Unix EROFS) is '37'; any other IOException stays the generic '30'.
    [Fact]
    public void DeleteFileFailure_MediumRefusalIs37_OtherIs30()
    {
        Assert.Equal(FileStatusCode.PermissionDenied, FileStatusCode.ForDeleteFileFailure(new IOException("wp", unchecked((int)0x80070013))));
        Assert.Equal(FileStatusCode.PermissionDenied, FileStatusCode.ForDeleteFileFailure(new IOException("erofs", 30)));
        Assert.Equal(FileStatusCode.PermanentError, FileStatusCode.ForDeleteFileFailure(new IOException("other", unchecked((int)0x80070020))));
    }

    // §14.9.10.4 GR14: DELETE FILE of a genuinely absent physical file is the SUCCESSFUL '05'.
    [Fact]
    public void DeleteFile_AbsentPhysicalFile_Is05()
    {
        var reg = new FileRegistry();
        reg.Register("A", Tmp("absent"), 8, lineSequential: false, optional: false, -1, -1);
        Assert.Equal(FileStatusCode.OptionalFileNotFound, reg.DeleteFile("A"));
    }

    // §9.1.13.7 item 9 ('49'): DELETE RECORD on a connector not open I-O.
    [Fact]
    public void Delete_OpenInput_Is49()
    {
        var (reg, host) = SeqRelativeIo("AAAAAAAA");
        try
        {
            reg.Close("F");
            reg.OpenStatic("F", FileOpenMode.Input);
            Assert.Equal(FileStatusCode.DeleteRewriteNotOpenForIO, reg.DeleteRecord("F", ""));
        }
        finally { try { File.Delete(host); } catch { } }
    }

    // §9.1.13.5 ('23'): a random-access DELETE whose RELATIVE KEY names no record.
    [Fact]
    public void Delete_RandomMissingKey_Is23()
    {
        var reg = new FileRegistry();
        string host = Tmp("rand");
        reg.RegisterRelative("F", host, 8, false, 1 /* Random */, 4, -1, -1);
        try
        {
            reg.OpenStatic("F", FileOpenMode.Output);
            reg.Close("F");
            reg.OpenStatic("F", FileOpenMode.IO);
            reg.SetRelativeKey("F", 5);
            Assert.Equal(FileStatusCode.RecordNotFound, reg.DeleteRecord("F", ""));
        }
        finally { try { File.Delete(host); } catch { } }
    }

    // §14.9.6.4 GR8: after a successful CLOSE the connector is out of its open mode and dissociated — the
    // OPEN / CLOSE / CLOSE / OPEN cycle answers 00 / 00 / 42 / 00, never '41'.
    [Fact]
    public void Close_ThenCloseAgain42_ThenReopen00()
    {
        var reg = new FileRegistry();
        string host = Tmp("cycle");
        reg.Register("S", host, 8, lineSequential: false, optional: false, -1, -1);
        try
        {
            reg.OpenStatic("S", FileOpenMode.Output);
            reg.Close("S");
            Assert.Equal(FileStatusCode.Success, reg.Status("S"));
            reg.Close("S");
            Assert.Equal(FileStatusCode.FileNotOpen, reg.Status("S"));
            reg.OpenStatic("S", FileOpenMode.Input);
            Assert.Equal(FileStatusCode.Success, reg.Status("S"));
        }
        finally { try { File.Delete(host); } catch { } }
    }
}
