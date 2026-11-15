// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>kb/Work PB234 residue 2 — ISO §2023:14.9.5.4 GR4, RUN-verified rather than derivation-verified: "When a
/// CANCEL statement is executed, all programs contained within the program referenced by the CANCEL statement are
/// also canceled. The result is the same as if an explicit CANCEL statement were executed for each contained
/// program in the reverse order in which the programs appear in the outermost program."
/// <para>The ORDER among sibling containees has no COBOL-observable witness a golden can print — GR9's implicit
/// closes run without USE procedures, and the §2023:14.6.2.3.2 resets are assignments — so the order is observed at
/// the one runtime surface that performs it: <see cref="ProgramTable"/>'s cancel cascade, through each node's
/// GR9 close hook and its static-reset hook, on a real run unit whose programs were really CALLed (GR7 makes a
/// never-called program a no-op, so every node is activated first).</para></summary>
public sealed class CancelCascadeOrderTests
{
    private sealed class Probe(string name, List<string> log) : ICobolProgram
    {
        public void Call(CobolArg[] args, CobolArg? returning) { }
        public void Activate() { }
        public void CloseFiles() => log.Add("close " + name);
    }

    [Fact]
    public void Cancel_CascadesOverContainees_InReverseSourceOrder_BeforeTheContainer()
    {
        var log = new List<string>();
        var table = new RunUnit().Programs;
        // Source order: OUTER contains A, then B (which contains B1), then C — containers register first.
        void Reg(string path, string name, string? parent) =>
            table.Register(path, name, parent, initial: false, common: false, recursive: false,
                _ => new Probe(name, log), staticReset: () => log.Add("reset " + name));
        // One module's registration set, as its __CobolModule.EnsureRegistered() makes it (kb/Work PB2097).
        table.RegisterModule("Cobol.OUTER.__CobolModule", RuntimeAbi.Version.ToString(), RuntimeAbi.CallAbi, () =>
        {
            Reg("OUTER", "OUTER", null);
            Reg("OUTER/A", "A", "OUTER");
            Reg("OUTER/B", "B", "OUTER");
            Reg("OUTER/B/B1", "B1", "OUTER/B");
            Reg("OUTER/C", "C", "OUTER");
        });
        table.CallProgram("OUTER", "", [], null);
        foreach (var n in new[] { "A", "B", "C" }) table.CallProgram(n, "OUTER", [], null);
        table.CallProgram("B1", "OUTER/B", [], null);
        log.Clear();   // Register's §2023:14.6.2.3.2 case-1 resets are not part of the cascade

        table.Cancel("OUTER", "", refusesEveryActiveProgram: true);

        // Reverse source order C, B (itself cascading over B1 first — the same rule one level down), A; each
        // program's own GR9 close then its GR3 initial-state reset; the container last.
        Assert.Equal(new[]
        {
            "close C", "reset C",
            "close B1", "reset B1", "close B", "reset B",
            "close A", "reset A",
            "close OUTER", "reset OUTER",
        }, log);
    }

    /// <summary>A program whose activation runs <paramref name="body"/>: the CANCEL statement its procedure executes.</summary>
    private sealed class Running(Action body) : ICobolProgram
    {
        public void Call(CobolArg[] args, CobolArg? returning) => body();
        public void Activate() => body();
        public void CloseFiles() { }
    }

    private static ProgramTable OneProgram(RunUnit unit, Action<ProgramTable> body)
    {
        var table = unit.Programs;
        table.RegisterModule("Cobol.P5241.__CobolModule", RuntimeAbi.Version.ToString(), RuntimeAbi.CallAbi, () =>
            table.Register("P5241", "P5241", null, initial: false, common: false, recursive: false,
                _ => new Running(() => body(table)), staticReset: null));
        return table;
    }

    /// <summary>kb/Work PB5241 — GR5's two editions over the run unit's MAIN program, which is active and was never called.
    /// From 2002 (§2023:14.9.5.4 GR5, "shall not be in the active state") its self-CANCEL raises the fatal
    /// EC-PROGRAM-CANCEL-ACTIVE and the run unit ends abnormally (DOC-A.1-18); at 85 §1985:X.5.3.4 GR5 forbids only a
    /// program "that has been called and has not yet executed an EXIT PROGRAM statement", so §1985:X.5.3.4 GR8 governs —
    /// "No action is taken" — and the run unit ends normally. Fails if the 85 arm still asks the 2002 test (status 1) or
    /// the 2002 arm stopped refusing the main program (status 0).</summary>
    [Theory]
    [InlineData(true, 1L)]
    [InlineData(false, 0L)]
    public void CancelOfTheActiveMainProgram_FollowsTheEditionsGr5(bool refusesEveryActiveProgram, long exitStatus)
    {
        var unit = new RunUnit();
        var table = OneProgram(unit, t => t.Cancel("P5241", "P5241", refusesEveryActiveProgram));
        table.RunMain("P5241");
        Assert.Equal(exitStatus, unit.ExitStatus);
    }

    /// <summary>kb/Work PB5241 — a program that has been CALLed and has not returned is refused at EVERY edition: it is
    /// active (§2023:14.9.5.4 GR5) and, at 85, called and not exited (§1985:X.5.3.4 GR5). Fails if the 85 arm let the MAIN
    /// exception swallow a called activation.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CancelOfACalledProgramNotExited_RaisesCancelActive_AtEveryEdition(bool refusesEveryActiveProgram)
    {
        var table = OneProgram(new RunUnit(), t => t.Cancel("P5241", "P5241", refusesEveryActiveProgram));
        var x = Assert.Throws<CobolCallException>(() => table.CallProgram("P5241", "", [], null));
        Assert.Equal("EC-PROGRAM-CANCEL-ACTIVE", x.EcName);
    }
}
