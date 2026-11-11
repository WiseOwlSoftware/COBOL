// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// CCVS-85's CM module against the communication facility (kb/Work PB4677). The programs read their symbolic names from the X-cards NistPreprocessor maps, and tests/nist/data/mcs.json
/// defines those names to the MCS. Every program compiles at COBOL-85. CM102M, CM103M, CM105M and CM201M run: CCVS's own checks are the oracle, so a program reporting a failure, or none of its
/// tests, is a failure here. CM102M waits 90 s on its own clock, which its budget states (kb/Work PB5154). CM101M, CM104M and CM202M measure wall-clock delays of 15 and 30 seconds and wait on an operator's terminal, so they are compiled only: the report's records of
/// CM101M's and CM104M's status legs are ones these goldens witness (conformance-suite/wiseowl/cases/_unassigned/pb4677_mcs_*). CM303M and CM401M test the federal flagging layer, which is outside
/// the ISO suite (PB4614 decision 11), and compile clean.
/// </summary>
public sealed class CommunicationCcvsTests
{
    private static string Run(string program, IReadOnlyDictionary<string, string?>? env = null, ObservationBudget? budget = null)
    {
        string dir = ScratchDirectory.Create("WiseOwlCobol_Cm_");
        File.Copy(Path.Combine(TestRepo.Root, "tests", "nist", "data", "mcs.json"), Path.Combine(dir, "mcs.json"));
        string dll = Path.Combine(dir, program + ".dll");
        var compiled = CompiledProgramCache.Compile(CorpusManifest.CompileOptions(program, dll, 85, permissive: false, checkOnly: false));
        Assert.True(compiled.Success, string.Join("\n", compiled.Errors));
        var (ok, _, detail) = CutRunner.Run(dll, dir, null, env, budget);
        Assert.True(ok, detail);
        return dir;
    }

    [Theory]
    [InlineData("CM101M")]
    [InlineData("CM102M")]
    [InlineData("CM103M")]
    [InlineData("CM104M")]
    [InlineData("CM105M")]
    [InlineData("CM201M")]
    [InlineData("CM202M")]
    [InlineData("CM303M")]
    [InlineData("CM401M")]
    public void CmProgram_CompilesAtCobol85(string program)
    {
        string dir = ScratchDirectory.Create("WiseOwlCobol_Cm_");
        try
        {
            var compiled = CompiledProgramCache.Compile(CorpusManifest.CompileOptions(program, Path.Combine(dir, program + ".dll"), 85, permissive: false, checkOnly: true));
            Assert.True(compiled.Success, string.Join("\n", compiled.Errors));
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>CM102M: the SEND, ENABLE, DISABLE and status-key tests of the output CD, each compared with the status CCVS expects (20/1, 30, 40, 50 among them).</summary>
    [Fact]
    public void CM102M_ReportsNoFailure()
    {
        // CM102M's own clock (DELAY-FOR-30-SECS, an ACCEPT FROM TIME loop): MSG-BEFORE-DELAY-AND-DISABLE falls into one
        // delay and DELAY-DISABLE-DELAY-AND-STOP performs it twice, so the run waits 90 s whatever the host does, and its
        // budget states that wait on top of the unit (kb/Work PB5154).
        string dir = Run("CM102M", budget: ObservationBudget.OneProcessWaiting(TimeSpan.FromSeconds(3 * 30),
            "CM102M, which waits three 30-second delays on its own clock"));
        try
        {
            string report = File.ReadAllText(Path.Combine(dir, "cm102m.txt"));
            Assert.Contains("PASS  SEND-STATUS-TEST-05", report);
            Assert.Contains("NO ERRORS ENCOUNTERED", report);
            Assert.DoesNotContain("FAIL", report);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>CM103M echoes each message of its queue to its destination until a KILL message, logging both statuses.</summary>
    [Fact]
    public void CM103M_EchoesTheQueueUntilKill()
    {
        string dir = Run("CM103M");
        try
        {
            Assert.Equal(
                ["THE FIRST MESSAGE IN THE QUEUE", "KILL THE LOGGING PROGRAM"],
                File.ReadAllLines(Path.Combine(dir, "CCVSD1.out")));
            string log = File.ReadAllText(Path.Combine(dir, "cm103m.txt"));
            Assert.Contains("00  00/0    30   THE FIRST MESSAGE IN THE QUEUE", log);
            Assert.Contains("00  00/0    24   KILL THE LOGGING PROGRAM", log);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>CM105M: the queue hierarchy, each specified level series reading its own messages and an unspecified level choosing as E.5.5.4 illustrates.</summary>
    [Fact]
    public void CM105M_ReportsNoFailure()
    {
        string dir = Run("CM105M");
        try
        {
            string report = File.ReadAllText(Path.Combine(dir, "cm105m.txt"));
            Assert.Contains("PASS  QUEUE-TEST-10", report);
            Assert.Contains("NO ERRORS ENCOUNTERED", report);
            Assert.DoesNotContain("FAIL", report);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>CM201M: the program the MCS schedules (its CD says FOR INITIAL INPUT), which acknowledges each message of the queue that scheduled it and stops when the count is zero.</summary>
    [Fact]
    public void CM201M_ScheduledByTheMcs_AcknowledgesEachMessage()
    {
        string dir = Run("CM201M", new Dictionary<string, string?> { ["WISEOWL_COBOL_MCS_SCHEDULED"] = "CCVSQ1" });
        try
        {
            string[] device = File.ReadAllLines(Path.Combine(dir, "CCVSD1.out"));
            Assert.Equal(6, device.Length);
            Assert.Equal("RECEIPT OF MESSAGE FROM DEVICE \"CCVST1      \" IS ACKNOWLEDGED.", device[0]);
            Assert.StartsWith("QUEUE INVOLVED WAS \"CCVSQ1 ", device[1]);
            Assert.Equal("KILL THE LOGGING PROGRAM", device[5]);
        }
        finally { ScratchDirectory.Remove(dir); }
    }
}
