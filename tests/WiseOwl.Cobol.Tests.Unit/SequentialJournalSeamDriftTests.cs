// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE SEQUENTIAL CONNECTOR AND THE TRANSACTION JOURNAL REACH THE HOST ONLY THROUGH THE RUN UNIT'S <c>IHostMedium</c>
/// (docs/rearchitecture/DESIGN-runtime-library.md 2.2.2.12; kb/Work PB3340). The journal's write-ahead rules are promises about
/// what a death, a refused write or sync and a power loss leave at EVERY operation, and the fault-injecting test medium
/// (<see cref="FaultHostMedium"/>) can only hold them to it where every operation goes through it: one handle opened past the
/// medium, one removal or directory sync made directly, and that operation is never refused, never the point of a death and
/// never lost to a power loss in any witness, so a rule broken there passes every row. The production medium is
/// <c>HostMedium.Production</c>, which is <c>HostFile</c> and nothing else; this test keeps the two clients from going past it.
/// </summary>
public sealed class SequentialJournalSeamDriftTests
{
    /// <summary>A host operation that changes a file or a name, spelled past the medium.</summary>
    private static readonly Regex PastTheMedium = new(
        @"\bHostFile\.(?:OpenConnectorStream|OpenConnectorWriteStream|OpenAuxiliary|OpenTransient|SyncDirectory|OpenLockHolder|CreateLockDirectory)\s*\("
        + @"|\bFile\.(?:Delete|Move|Create|WriteAllBytes)\s*\(|\bDirectory\.(?:Delete|Move|CreateDirectory)\s*\("
        + @"|\bSidecarLockFiles\.(?:Publish|Retire|RetireDirectory)\s*\(|\bHostRegionLocks\.EntryMutex\s*\(",
        RegexOptions.Compiled);

    private static IEnumerable<string> Clients()
    {
        string io = TestRepo.Src("WiseOwl.Cobol.Runtime", "IO");
        foreach (string file in Directory.EnumerateFiles(io, "SequentialConnector*.cs")) yield return file;
        yield return Path.Combine(io, "Sharing", "SequentialJournal.cs");
        yield return Path.Combine(io, "Sharing", "SequentialTransactions.cs");
    }

    [Fact]
    public void TheSequentialConnectorAndTheJournalChangeTheHostOnlyThroughTheMedium()
    {
        var offenders = new List<string>();
        foreach (string file in Clients())
        {
            Assert.True(File.Exists(file), $"a client of the seam moved: {file}");
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                string code = lines[i].TrimStart();
                if (code.StartsWith("//", StringComparison.Ordinal)) continue;   // prose and documentation about the rule
                if (PastTheMedium.IsMatch(lines[i])) offenders.Add($"{Path.GetFileName(file)}:{i + 1}: {code}");
            }
        }
        Assert.True(offenders.Count == 0,
            "a host operation past the run unit's IHostMedium (DESIGN-runtime-library.md 2.2.2.12): the fault-injecting medium "
            + "cannot refuse it, die at it or lose it, so no witness holds the journal's rules there. Call the medium "
            + "(Medium.Open..., Medium.DeleteFile, Medium.SyncDirectory, Medium.PublishHolder, Medium.RetireName, Medium.EntryMutex):\n  "
            + string.Join("\n  ", offenders));
    }

    /// <summary>The positive complement: the detector still matches each spelling it exists to catch.</summary>
    [Theory]
    [InlineData("using var fs = HostFile.OpenAuxiliary(HostPath, FileMode.Open, FileAccess.Read);")]
    [InlineData("File.Delete(c.HostPath);")]
    [InlineData("HostFile.SyncDirectory(directory);")]
    [InlineData("var holder = SidecarLockFiles.Publish(directory, _ => name);")]
    [InlineData("var mutex = HostRegionLocks.EntryMutex(hostPath);")]
    public void TheDetectorStillMatchesEachSpellingItBans(string line) => Assert.Matches(PastTheMedium, line);
}
