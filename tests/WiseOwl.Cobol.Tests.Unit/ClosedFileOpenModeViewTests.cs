// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.IO;
using WiseOwl.Cobol.Runtime.IO.Store;
using WiseOwl.Cobol.Tests.Shared;
using WiseOwl.Cobol.Tests.Unit.Store;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE OPEN-MODE VIEW OF A FILE WHOSE CLOSE FAILED (kb/Work PB5261, the CLOSE-side arm of PB5200's stale-mode shape; the owner's
/// standing rule MANDATORY-PRACTICES I11: each abnormal path is tested as fully as its success path).
/// <para>§2023:14.9.49.4 GR6 b)–e) scopes a mode-specific USE procedure to a file "open in" that mode "or in the process of being
/// opened in" it. A CLOSE takes the connector out of its open mode whether it succeeds or fails (§2023:14.9.6.4 GR8 for the
/// success, docs/CONFORMANCE.md DOC-A.1-105 for the failure: the next OPEN answers no '41'), so once the CLOSE statement —
/// its own USE procedure included — has ended, the file is in no mode and <see cref="FileRegistry.OpenModeOf"/> answers −1. While the
/// statement executes the file is still the one the CLOSE was issued against, so a USE procedure for the CLOSE's own '30' finds the
/// mode it was open in (BeginClose). The '30' is the keyed store engine's refused promotion
/// (§2023:9.1.13.6 item 1), injected by the host-refusal medium of <see cref="KeyedClosePermanentErrorWitnessTests"/>.</para>
/// </summary>
public sealed class ClosedFileOpenModeViewTests : WiseOwlCobolTestBase
{
    /// <summary>The OPEN I-O reads committed slot A twice (its adoption and its fence's promotion); the third read is the CLOSE's
    /// promotion (the same measurement <see cref="KeyedClosePermanentErrorWitnessTests"/> makes).</summary>
    private const int RefusedSlotARead = 3;

    private static Func<IPageMedium, IPageMedium> RefusingTheNthSlotARead(int nth)
    {
        int seen = 0;
        return inner => new RefusingPageMedium(inner, read: page =>
            page == MetaPage.SlotA && ++seen == nth ? new IOException("refused by the host (test)") : null);
    }

    /// <summary>An I-O file whose CLOSE answers '30', run through <paramref name="statements"/> with the registry.</summary>
    private void WithFailingClose(FileOrg org, Action<FileRegistry> statements)
    {
        string host = Path.Combine(TempDir, $"pb5261-{org}.dat");
        FileHarness.Seed(host, org, ["AAAA", "BBBB"]);
        var reg = new FileRegistry();
        FileHarness.Register(reg, "W", host, org);
        PageMediumObserver.Value = RefusingTheNthSlotARead(RefusedSlotARead);
        try
        {
            reg.OpenStatic("W", FileOpenMode.IO);
            Assert.Equal("00", reg.Status("W"));
            Assert.Equal("00", FileHarness.Write(reg, "W", org, 3, "CCCC"));
            Assert.Equal((int)FileOpenMode.IO, reg.OpenModeOf("W"));   // the file is open in the I-O mode
            statements(reg);
        }
        finally { PageMediumObserver.Value = null; }
    }

    [Theory]
    [InlineData(FileOrg.Relative)]
    [InlineData(FileOrg.Indexed)]
    public void AfterAFailedClose_TheFileIsInNoOpenMode(FileOrg org) =>
        WithFailingClose(org, reg =>
        {
            reg.Close("W");
            Assert.Equal("30", reg.Status("W"));
            Assert.Equal(-1, reg.OpenModeOf("W"));
        });

    [Theory]
    [InlineData(FileOrg.Relative)]
    [InlineData(FileOrg.Indexed)]
    public void DuringTheCloseStatement_TheFileIsStillInTheModeItWasOpenIn_AndAfterItInNone(FileOrg org) =>
        WithFailingClose(org, reg =>
        {
            reg.BeginClose("W");
            reg.Close("W");
            Assert.Equal("30", reg.Status("W"));
            Assert.Equal((int)FileOpenMode.IO, reg.OpenModeOf("W"));   // the CLOSE's own USE procedure finds the I-O mode
            reg.EndModeScope("W");
            Assert.Equal(-1, reg.OpenModeOf("W"));
        });

    [Theory]
    [InlineData(FileOrg.Relative)]
    [InlineData(FileOrg.Indexed)]
    public void ACloseOfAFileThatIsNotOpen_HasNoModeToScopeTo(FileOrg org)
    {
        string host = Path.Combine(TempDir, $"pb5261-never-opened-{org}.dat");
        FileHarness.Seed(host, org, ["AAAA"]);
        var reg = new FileRegistry();
        FileHarness.Register(reg, "W", host, org);
        reg.BeginClose("W");
        reg.Close("W");
        Assert.Equal("42", reg.Status("W"));   // §2023:14.9.6.4 GR1
        Assert.Equal(-1, reg.OpenModeOf("W"));
        reg.EndModeScope("W");
        Assert.Equal(-1, reg.OpenModeOf("W"));
    }
}
