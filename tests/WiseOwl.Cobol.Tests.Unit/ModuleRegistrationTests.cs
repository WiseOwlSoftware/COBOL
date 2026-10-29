// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>kb/Work PB2097 — the run unit's ONE module-registration set (docs/rearchitecture/DESIGN-external-repository.md
/// §11.2, §17.3 "One registration set"): <see cref="ProgramTable.RegisterModule"/> is the only writer of a run unit's
/// programs, each module is registered once however it is reached (the emitted <c>Main</c>, the sibling-module probe, a
/// host), and a module carrying an already-registered outermost name, or compiled against another runtime major or call
/// ABI, registers NOTHING. ISO/IEC 1989:2023 §8.3.2.2 2) (cite.py OK): "Within a run unit, all instances of a given name
/// that is externalized to the operating environment shall identify the same kind of entity or item ... when two or more
/// source elements identify something with the same externalized name, they refer to the same instance." A refusal is
/// §14.9.4.4 GR3 b)'s EC-PROGRAM-NOT-FOUND ("If the program cannot be located", cite.py OK).</summary>
public sealed class ModuleRegistrationTests
{
    private sealed class Stub : ICobolProgram
    {
        public void Call(CobolArg[] args, CobolArg? returning) { }
        public void Activate() { }
        public void CloseFiles() { }
    }

    private static readonly string Current = RuntimeVersionSkew.Current;

    /// <summary>A module registrar body registering one outermost program per name, as the emitted <c>Register()</c>
    /// does; <paramref name="runs"/> counts how often it runs.</summary>
    private static Action Registrar(ProgramTable table, Action? runs, params (string Name, bool Function)[] units) => () =>
    {
        runs?.Invoke();
        foreach (var (name, function) in units)
            table.Register(name, name, null, initial: false, common: false, recursive: false, _ => new Stub(), isFunction: function);
    };

    private static bool Located(ProgramTable table, string name)
    {
        table.EntryOf(name, out bool notFound);
        return !notFound;
    }

    [Fact]
    public void AModule_IsRegisteredOnce_HoweverOftenItIsReached()
    {
        var table = new RunUnit().Programs;
        int runs = 0;
        var body = Registrar(table, () => runs++, ("P2097A", false));
        table.RegisterModule("Cobol.A.__CobolModule", Current, RuntimeAbi.CallAbi, body);
        table.RegisterModule("Cobol.A.__CobolModule", Current, RuntimeAbi.CallAbi, body);   // a host after the probe, say
        Assert.Equal(1, runs);
        Assert.True(Located(table, "P2097A"));
    }

    [Fact]
    public void AModuleOfAnotherRuntimeMajor_IsRefused_AndRegistersNothing()
    {
        var table = new RunUnit().Programs;
        int runs = 0;
        var other = RuntimeVersionSkew.OtherMajor;
        var e = Assert.Throws<CobolCallException>(() =>
            table.RegisterModule("Cobol.B.__CobolModule", other, RuntimeAbi.CallAbi, Registrar(table, () => runs++, ("P2097B", false))));
        Assert.Equal("EC-PROGRAM-NOT-FOUND", e.EcName);
        Assert.Contains(other, e.Message, StringComparison.Ordinal);
        Assert.Contains("major", e.Message, StringComparison.Ordinal);
        Assert.Equal(0, runs);
        Assert.False(Located(table, "P2097B"));
    }

    [Fact]
    public void AModuleOfAnotherCallAbi_IsRefused_AndRegistersNothing()
    {
        var table = new RunUnit().Programs;
        var e = Assert.Throws<CobolCallException>(() =>
            table.RegisterModule("Cobol.C.__CobolModule", Current, RuntimeAbi.CallAbi + 1, Registrar(table, null, ("P2097C", false))));
        Assert.Equal("EC-PROGRAM-NOT-FOUND", e.EcName);
        Assert.Contains($"call ABI {RuntimeAbi.CallAbi + 1}", e.Message, StringComparison.Ordinal);
        Assert.False(Located(table, "P2097C"));
    }

    [Fact]
    public void AModuleOfAnotherMinorVersion_IsCompatible()
    {
        var table = new RunUnit().Programs;
        var newerMinor = new Version(RuntimeAbi.Version.Major, RuntimeAbi.Version.Minor + 1, 0, 0).ToString();
        table.RegisterModule("Cobol.D.__CobolModule", newerMinor, RuntimeAbi.CallAbi, Registrar(table, null, ("P2097D", false)));
        Assert.True(Located(table, "P2097D"));
    }

    [Theory]
    [InlineData(false)]   // the second module defines the name as a program again
    [InlineData(true)]    // ... or as a function: one externalized name identifies one kind of entity (§8.3.2.2 2)
    public void AModuleCarryingAnAlreadyRegisteredName_IsRefusedWhole_NamingBothModules(bool asFunction)
    {
        var table = new RunUnit().Programs;
        table.RegisterModule("Cobol.FIRST.__CobolModule", Current, RuntimeAbi.CallAbi,
            Registrar(table, null, ("P2097F", false), ("P2097X", false)));
        var e = Assert.Throws<CobolCallException>(() =>
            table.RegisterModule("Cobol.SECOND.__CobolModule", Current, RuntimeAbi.CallAbi,
                Registrar(table, null, ("P2097S", false), ("P2097X", asFunction))));
        Assert.Equal("EC-PROGRAM-NOT-FOUND", e.EcName);
        Assert.Contains("Cobol.FIRST.__CobolModule", e.Message, StringComparison.Ordinal);
        Assert.Contains("Cobol.SECOND.__CobolModule", e.Message, StringComparison.Ordinal);
        Assert.Contains("P2097X", e.Message, StringComparison.Ordinal);
        // Nothing of the refused module is registered: not its own program, and not a second P2097X.
        Assert.False(Located(table, "P2097S"));
        Assert.True(Located(table, "P2097F"));
    }

    /// <summary>kb/Work PB2843: an outermost unit's path is rooted at its EXTERNALIZED name, escaped so that an AS
    /// literal holding the separator never meets a containee's path: <c>AS "A/B"</c> and the containee <c>B</c> of an
    /// outermost <c>A</c> are two paths, and both register.</summary>
    [Fact]
    public void ThePath_IsInjective_EvenForAnExternalizedNameHoldingTheSeparator()
    {
        string a = ProgramTable.OutermostPath("P2843A");
        string contained = ProgramTable.ContainedPath(a, "B");
        string slashed = ProgramTable.OutermostPath("P2843A/B");
        Assert.NotEqual(contained, slashed, StringComparer.OrdinalIgnoreCase);
        Assert.NotEqual(ProgramTable.OutermostPath("P2843A\\/B"), slashed, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(ProgramTable.OutermostPath("P2843A"), ProgramTable.OutermostPath("  P2843A "));   // formed first

        var table = new RunUnit().Programs;
        table.RegisterModule("Cobol.PA.__CobolModule", Current, RuntimeAbi.CallAbi, () =>
        {
            table.Register(a, "P2843A", null, initial: false, common: false, recursive: false, _ => new Stub());
            table.Register(contained, "B", a, initial: false, common: false, recursive: false, _ => new Stub());
        });
        table.RegisterModule("Cobol.PZ.__CobolModule", Current, RuntimeAbi.CallAbi, () =>
            table.Register(slashed, "Z", null, initial: false, common: false, recursive: false, _ => new Stub(),
                externalizedName: "P2843A/B"));
        Assert.True(Located(table, "P2843A"));
        Assert.True(Located(table, "P2843A/B"));
    }

    /// <summary>kb/Work PB2920: a class's run-unit keys are rooted at its EXTERNALIZED name, in a namespace no program's
    /// path can enter (<c>OutermostPath</c> escapes every <c>/</c>, so none begins with one), and its two halves are two
    /// identities: <c>CLASS-ID. ACCT</c> and <c>CLASS-ID. ACCT AS "LEDGER-ACCT"</c> differ, a class and an outermost
    /// program of one name differ, and a class named <c>A/FACTORY</c> never meets the factory half of class <c>A</c>.</summary>
    [Fact]
    public void AClassPath_IsRootedAtTheExternalizedName_AndNeverMeetsAProgramOrAHalf()
    {
        Assert.Equal(ProgramTable.ClassPath("P2920A"), ProgramTable.ClassPath("  P2920A "));   // formed first
        Assert.NotEqual(ProgramTable.ClassPath("P2920A"), ProgramTable.ClassPath("P2920-LEDGER"));
        Assert.NotEqual(ProgramTable.ClassPath("P2920A"), ProgramTable.OutermostPath("P2920A"));
        Assert.NotEqual(ProgramTable.ClassHalfPath("P2920A", factory: false), ProgramTable.ClassHalfPath("P2920A", factory: true));
        Assert.NotEqual(ProgramTable.ClassHalfPath("P2920A", factory: true), ProgramTable.ClassHalfPath("P2920A/FACTORY", factory: false));
        Assert.NotEqual(ProgramTable.ClassHalfPath("P2920A", factory: true), ProgramTable.ClassPath("P2920A/FACTORY"));
        Assert.False(ProgramTable.OutermostPath("/P2920A").StartsWith('/'));   // the namespace is the one character no program path begins with
    }

    /// <summary>kb/Work PB2843: a registrar that repeats a path, inside its module or across modules, is a compiler
    /// defect, refused LOUDLY rather than replacing a node another unit registered. Like every registrar fault it is
    /// §14.9.4.4 GR3 b)'s EC-PROGRAM-NOT-FOUND naming the module and the fault (DESIGN-external-repository §11.2, kb/Work
    /// PB2101): the module registers nothing.</summary>
    [Fact]
    public void ARepeatedPath_IsRefusedLoudly_NeverReplacingANode()
    {
        var table = new RunUnit().Programs;
        string w = ProgramTable.OutermostPath("P2843W");
        var inModule = Assert.Throws<CobolCallException>(() =>
            table.RegisterModule("Cobol.PW.__CobolModule", Current, RuntimeAbi.CallAbi, () =>
            {
                table.Register(w, "P2843W", null, initial: false, common: false, recursive: false, _ => new Stub());
                table.Register(w, "P2843V", null, initial: false, common: false, recursive: false, _ => new Stub());
            }));
        Assert.Equal("EC-PROGRAM-NOT-FOUND", inModule.EcName);
        Assert.Contains("PB2843", inModule.Message, StringComparison.Ordinal);
        Assert.Contains("InvalidOperationException", inModule.Message, StringComparison.Ordinal);
        Assert.False(Located(table, "P2843W"));   // the module's first unit was staged, not registered

        table.RegisterModule("Cobol.PW2.__CobolModule", Current, RuntimeAbi.CallAbi, () =>
            table.Register(w, "P2843W", null, initial: false, common: false, recursive: false, _ => new Stub()));
        // A hand-made registrar that roots its program at the declared name P2843W while externalizing P2843X.
        var across = Assert.Throws<CobolCallException>(() =>
            table.RegisterModule("Cobol.PX.__CobolModule", Current, RuntimeAbi.CallAbi, () =>
                table.Register(w, "P2843W", null, initial: false, common: false, recursive: false, _ => new Stub(),
                    externalizedName: "P2843X")));
        Assert.Equal("EC-PROGRAM-NOT-FOUND", across.EcName);
        Assert.Contains("Cobol.PW2.__CobolModule", across.Message, StringComparison.Ordinal);
        Assert.False(Located(table, "P2843X"));
    }

    /// <summary>DESIGN-external-repository §11.2 (kb/Work PB2101): EVERY registrar fault is the CALL's own condition,
    /// EC-PROGRAM-NOT-FOUND with the fault named (a registrar that throws for any reason, the PB154
    /// containee-before-container guard, a static reset that throws as its unit is adopted), and the module registers
    /// NOTHING: not the units its registrar staged before the fault, and not the module itself, so the same module may be
    /// registered again once the cause is gone.</summary>
    [Fact]
    public void ARegistrarFault_IsEcProgramNotFound_AndRegistersNothing()
    {
        var table = new RunUnit().Programs;
        var bodyFault = Assert.Throws<CobolCallException>(() =>
            table.RegisterModule("Cobol.F1.__CobolModule", Current, RuntimeAbi.CallAbi, () =>
            {
                table.Register("P5398A", "P5398A", null, initial: false, common: false, recursive: false, _ => new Stub());
                throw new InvalidOperationException("the registrar's own fault");
            }));
        Assert.Equal("EC-PROGRAM-NOT-FOUND", bodyFault.EcName);
        Assert.Contains("Cobol.F1.__CobolModule", bodyFault.Message, StringComparison.Ordinal);
        Assert.Contains("the registrar's own fault", bodyFault.Message, StringComparison.Ordinal);
        Assert.False(Located(table, "P5398A"));

        var containee = Assert.Throws<CobolCallException>(() =>
            table.RegisterModule("Cobol.F2.__CobolModule", Current, RuntimeAbi.CallAbi, () =>
                table.Register("P5398B/INNER", "INNER", "P5398B", initial: false, common: false, recursive: false, _ => new Stub())));
        Assert.Contains("PB154", containee.Message, StringComparison.Ordinal);

        // A static reset that throws as its unit is adopted: the program registered BEFORE it in the same module is not left
        // behind, and the module is not in the registration set.
        int attempts = 0;
        Action body = () =>
        {
            table.Register("P5398C", "P5398C", null, initial: false, common: false, recursive: false, _ => new Stub());
            table.Register("P5398D", "P5398D", null, initial: false, common: false, recursive: true, _ => new Stub(),
                staticReset: () => { if (attempts++ == 0) throw new InvalidOperationException("the static reset fails"); });
        };
        var reset = Assert.Throws<CobolCallException>(() => table.RegisterModule("Cobol.F3.__CobolModule", Current, RuntimeAbi.CallAbi, body));
        Assert.Contains("the static reset fails", reset.Message, StringComparison.Ordinal);
        Assert.False(Located(table, "P5398C"));
        Assert.False(Located(table, "P5398D"));
        table.RegisterModule("Cobol.F3.__CobolModule", Current, RuntimeAbi.CallAbi, body);   // not remembered as registered
        Assert.True(Located(table, "P5398C"));
        Assert.True(Located(table, "P5398D"));
    }

    /// <summary>One module naming one externalized name twice is a registrar defect too (§8.3.2.2 2): refused whole, so the
    /// per-kind name dictionary never sees a duplicate.</summary>
    [Fact]
    public void AModuleCarryingOneOutermostNameTwice_RegistersNothing()
    {
        var table = new RunUnit().Programs;
        var twice = Assert.Throws<CobolCallException>(() =>
            table.RegisterModule("Cobol.T.__CobolModule", Current, RuntimeAbi.CallAbi, () =>
            {
                table.Register("P5398T", "P5398T", null, initial: false, common: false, recursive: false, _ => new Stub());
                table.Register("P5398T-2", "P5398T-2", null, initial: false, common: false, recursive: false, _ => new Stub(),
                    externalizedName: "p5398t");
            }));
        Assert.Contains("twice", twice.Message, StringComparison.Ordinal);
        Assert.False(Located(table, "P5398T"));
    }

    [Fact]
    public void AProgram_IsRegisteredOnlyThroughItsModule()
    {
        var table = new RunUnit().Programs;
        var e = Assert.Throws<InvalidOperationException>(() =>
            table.Register("P2097R", "P2097R", null, initial: false, common: false, recursive: false, _ => new Stub()));
        Assert.Contains("RegisterModule", e.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TheVersionSkewRule_IsTheMajorAndTheCallAbi()
    {
        Assert.Null(RuntimeAbi.Skew(Current, RuntimeAbi.CallAbi));
        // A different major is refused in EITHER direction, and refused as a major (not as an unparsable text).
        Assert.Contains("different major", RuntimeAbi.Skew($"{RuntimeAbi.Version.Major + 1}.0.0.0", RuntimeAbi.CallAbi));
        Assert.Contains("different major", RuntimeAbi.Skew($"{RuntimeAbi.Version.Major - 1}.0.0.0", RuntimeAbi.CallAbi));
        Assert.Contains("different major", RuntimeAbi.Skew(RuntimeVersionSkew.OtherMajor, RuntimeAbi.CallAbi));
        Assert.NotNull(RuntimeAbi.Skew(Current, RuntimeAbi.CallAbi + 1));
        Assert.NotNull(RuntimeAbi.Skew("not a version", RuntimeAbi.CallAbi));
    }

    /// <summary>The skew witnesses' version of another major (<see cref="RuntimeVersionSkew.OtherMajorOf"/>) is a
    /// different major of the same text length for every major shape, including the all-nines majors where the next
    /// major gains a digit: the conformance witness patches it over the text a compiled module recorded.</summary>
    [Theory]
    [InlineData("0.0.0.0", "1.0.0.0")]
    [InlineData("8.0.0.0", "9.0.0.0")]
    [InlineData("9.0.0.0", "8.0.0.0")]
    [InlineData("10.0.0.0", "11.0.0.0")]
    [InlineData("99.1.2.3", "98.1.2.3")]
    [InlineData("100.0.0.0", "101.0.0.0")]
    public void TheSkewWitnessesOtherMajor_KeepsTheTextLength(string recorded, string expected)
    {
        string other = RuntimeVersionSkew.OtherMajorOf(recorded);
        Assert.Equal(expected, other);
        Assert.Equal(recorded.Length, other.Length);
        Assert.NotEqual(Version.Parse(recorded).Major, Version.Parse(other).Major);
    }
}
