// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using WiseOwl.Cobol.Runtime.IO;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ AN OPEN THAT DECIDED ON A PRESENT FILE TAKES A HANDLE THAT REFUSES TO CREATE ONE, AND IS MADE AGAIN WHEN THE FILE WAS REMOVED
/// MEANWHILE (kb/Work PB3946; the opposite direction of PB3635's <c>FileMode.CreateNew</c>; <c>FileConnector.Open</c>). Another run unit
/// can remove the file between the one presence probe and the connector's handle. A handle opened <see cref="FileMode.OpenOrCreate"/>
/// then created the file empty and adopted it, so an OPEN INPUT / I-O / EXTEND answered success over a file that was never there, and
/// a covered OPEN journaled a file that did not exist as present. The OPEN is unsuccessful instead (§14.9.27.4 GR25: the file is not
/// affected) and is made again from its entry step, so its probe finds the file gone and the order is the one in which the other run
/// unit's removal ran first: §9.1.13.6 item 5's '35' for a non-optional file, GR17's '05' creation for an OPTIONAL one on I-O or EXTEND,
/// GR13's '05' for an OPTIONAL INPUT, GR18's creation for OUTPUT.
/// <para>The removal is made at the exact point between the probe and the handle, with no timing: the Table 19 column request
/// (<see cref="ColumnRequestLog.Before"/>) of a present file's OPEN runs after the probe and before the organization's body in every
/// organization, covered and uncovered.</para>
/// </summary>
public sealed class FileRemovedMeanwhileTests
{
    public static TheoryData<FileOrg, bool, bool, FileOpenMode> Cases()
    {
        var data = new TheoryData<FileOrg, bool, bool, FileOpenMode>();
        foreach (var org in new[] { FileOrg.Sequential, FileOrg.Relative, FileOrg.Indexed })
            foreach (bool covered in new[] { false, true })
                foreach (bool optional in new[] { false, true })
                    foreach (var mode in new[] { FileOpenMode.Input, FileOpenMode.IO, FileOpenMode.Extend, FileOpenMode.Output })
                        data.Add(org, covered, optional, mode);
        return data;
    }

    private static void Declare(FileRegistry reg, string host, FileOrg org, bool covered, bool optional)
    {
        switch (org)
        {
            case FileOrg.Sequential:
                reg.Register("F", host, recordWidth: 8, lineSequential: false, optional: optional, varyMin: -1, varyMax: -1);
                break;
            case FileOrg.Relative:
                reg.RegisterRelative("F", host, recordWidth: 8, optional: optional, accessMode: (int)KeyedAccess.Dynamic,
                    relativeKeyDigits: 4, varyMin: -1, varyMax: -1);
                break;
            default:
                reg.RegisterIndexed("F", host, recordWidth: 8, optional: optional, accessMode: (int)KeyedAccess.Dynamic,
                    primeKey: [new(0, 4)], varyMin: -1, varyMax: -1);
                break;
        }
        if (covered) reg.RegisterCommitControl("F");
    }

    /// <summary>A file of one record, made by an earlier run unit and closed.</summary>
    private static void Seed(string host, FileOrg org)
    {
        var seed = new FileRegistry();
        Declare(seed, host, org, covered: false, optional: false);
        seed.OpenStatic("F", FileOpenMode.Output);
        Assert.Equal("00", seed.Status("F"));
        if (org == FileOrg.Relative) seed.SetRelativeKey("F", 1);
        Assert.Equal("00", seed.WriteShared("F", "0001AAAA", -1, FileRecordLock.None, FileRetryKind.None, 0, page: null));
        seed.Close("F");
        Assert.Equal("00", seed.Status("F"));
        Assert.True(File.Exists(host));
    }

    /// <summary>⛔ THE EXPECTED STATUS IS THE ONE THE OTHER RUN UNIT'S REMOVAL, RUN FIRST, WOULD HAVE GIVEN (§9.1.13.6 item 5, §14.9.27.4
    /// GR13, GR17, GR18), and the file is as that order leaves it: absent for a non-optional INPUT / I-O / EXTEND and for an OPTIONAL
    /// INPUT, an empty file for the creations. No creating handle is spelled by an OPEN of an existing file.</summary>
    [Theory]
    [MemberData(nameof(Cases))]
    public void An_open_of_a_file_removed_between_the_probe_and_the_handle_answers_what_the_removal_would_have(
        FileOrg org, bool covered, bool optional, FileOpenMode mode)
    {
        string dir = ScratchDirectory.Create("frm-");
        string f = Path.Combine(dir, "f.dat");
        try
        {
            Seed(f, org);
            var log = new ColumnRequestLog();
            log.Before = at => { if (at == 0) File.Delete(f); };
            using var scope = log.Install();
            var reg = new FileRegistry();
            Declare(reg, f, org, covered, optional);
            reg.OpenStatic("F", mode);
            string status = reg.Status("F");
            bool creates = mode is FileOpenMode.Output || optional && mode is FileOpenMode.IO or FileOpenMode.Extend;
            string expected = mode switch
            {
                FileOpenMode.Output => "00",
                _ when optional => "05",
                _ => "35",
            };
            Assert.Equal(expected, status);
            Assert.Equal(creates, File.Exists(f));
            if (creates && org == FileOrg.Sequential)
                Assert.Equal(0, new FileInfo(f).Length);   // the file the OPEN created is empty, not the removed file's bytes
            if (reg.Status("F")[0] == (char)48) reg.Close("F");   // the COMMIT or ROLLBACK follows the CLOSE: CloseAll would end the run unit
            if (covered && creates)
            {
                // A creation under commit control is journaled with no file at its start (2.2.2.14), so the ROLLBACK removes it.
                Assert.Null(reg.Rollback());
                Assert.False(File.Exists(f));
            }
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>The OPEN is made again a bounded number of times: a file that is present at every attempt's probe and removed before
    /// every attempt's handle ends in §9.1.13.6 item 1's '30' after 1 + <c>FileConnector.MaxPathChangedRetries</c> attempts (PB3635's bound
    /// for a path made at every attempt), and nothing is created.</summary>
    [Fact]
    public void An_open_whose_file_is_removed_at_every_attempt_answers_30_after_the_bound_and_creates_nothing()
    {
        string dir = ScratchDirectory.Create("frm-");
        string f = Path.Combine(dir, "f.dat");
        try
        {
            int attempts = 0;
            // Each attempt's entry step finds the file made again (the other run unit), so the probe answers Present; the column
            // request after the probe removes it before the handle.
            var medium = new FaultHostMedium { OnEntryMutex = () => File.WriteAllText(f, "AAAAAAAA", Encoding.Latin1) };
            var log = new ColumnRequestLog { Before = _ => { attempts++; File.Delete(f); } };
            using var scope = log.Install();
            var reg = new FileRegistry { Medium = medium };
            Declare(reg, f, FileOrg.Sequential, covered: true, optional: false);
            reg.OpenStatic("F", FileOpenMode.IO);
            Assert.Equal("30", reg.Status("F"));
            Assert.Equal(3, attempts);
            Assert.False(File.Exists(f));
            reg.CloseAll();
        }
        finally { ScratchDirectory.Remove(dir); }
    }
}
