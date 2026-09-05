// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ THE DRIFT PIN FOR THE ONE BYTE-WINDOW CARRIAGE GATE (<c>DataBinder.ByteWindowResidueOf</c>; kb/Work
/// PB231). Four surfaces ask the SAME question — "may this leaf ride a shared byte-window storage area?" —
/// and each has its own statement of the law:
/// <list type="bullet">
/// <item>REDEFINES — ISO §13.18.44.4 GR1, "Storage association for the subject of the entry starts at the
/// first bit of the data item referenced by data-name-2 and continues over an area sufficient to contain the
/// number of bits required" (<c>ComputeTier</c>);</item>
/// <item>EXTERNAL — §13.18.22, the run-unit cell (<c>CallMakeExternal</c>);</item>
/// <item>BASED + ALLOCATE — §13.18.5.3 SR1/SR2 restrict the subject to "shall not be of class object" and
/// "shall not be a dynamic-length elementary item or a variable-length group", §14.9.3.3 SR1 asks only for
/// the BASED clause, and §14.9.3.4 GR3 makes the area "the number of bytes required to hold an item as
/// described by data-name-1" (<c>PtrBindBasedAndAddressables</c>);</item>
/// <item>ADDRESS OF — §8.4.3.11, the per-instance cell (same forcer).</item>
/// </list>
/// <para>They were TWO hand-written lists — a deny-list in <c>ComputeTier</c>, an allow-list in
/// <c>ForceStringCanonical</c> — and they drifted. kb/Work PB203 opened the REDEFINES one to USAGE BIT leaves
/// and the cell one was never touched, so <c>01 R BASED. 05 B PIC 1(8) USAGE BIT.</c> drew a bind-time
/// COBOLNET1695 while its byte-identical REDEFINES twin compiled and ran — rejects-legal-source, since no
/// syntax rule anywhere conditions BASED, EXTERNAL or ADDRESS OF on a subordinate's USAGE. This is the ninth
/// recorded instance of this repo's two-arm-dispatch shape, and this file is what makes the tenth impossible:
/// the four surfaces must return the SAME verdict for the same leaf, whatever that verdict is.</para>
/// <para>The AGREEMENT half is deliberately stated without hard-coding the verdict, so the landing that
/// discharges a residue does not have to edit it. The PINNED half below states the verdicts that are settled
/// today, so "they all agree" can never be satisfied by all four regressing together.</para>
/// <para>⛔ IT WORKED AS DESIGNED, TWICE. kb/Work PB231's NATIONAL third — RESIDUE-11, the D-N1
/// two-bytes-per-position byte-window layout — was discharged by ONE edit to the gate plus the geometry behind
/// it, and the only change needed in this file was flipping the expectation column. The POINTER third then
/// emptied the gate entirely: a pointer-class leaf's value rides the area's MANAGED SLOT at the byte offset its
/// reserved bytes occupy, so <b>no leaf kind is refused any more</b> and the refusal fact below had to be
/// replaced rather than flipped.</para>
/// </summary>
public sealed class ByteWindowResidueDriftTests
{
    /// <param name="tag">A unique PROGRAM-ID stem (kb/Work — .NET serves a stale same-named assembly).</param>
    /// <param name="leaf">The leaf's description, written after the data-name.</param>
    /// <param name="bytes">The leaf's storage extent in bytes — the alias width the REDEFINES spelling needs
    /// and the byte quantity §14.9.3.4 GR3 allocates.</param>
    /// <param name="carried">The settled verdict: true when a shared byte-window area carries the leaf.</param>
    public static TheoryData<string, string, int, bool> Leaves() => new()
    {
        // Character categories — one byte per position (the documented item-209 serialization).
        { "BWR01", "PIC X(4)", 4, true },
        { "BWR02", "PIC ZZZ9", 4, true },
        // Every NUMERIC usage carries a pinned byte form since the Step D dissolution (kb/Work PB164):
        // §13.18.60.4 GR4 radix-2, GR11 BCD, the wave-2 IEEE pin.
        { "BWR03", "PIC S9(4) COMP", 2, true },
        { "BWR04", "PIC S9(3) COMP-3", 2, true },
        { "BWR05", "USAGE COMP-1", 4, true },
        // BOOLEAN, both representations. A DISPLAY boolean is one '0'/'1' character per position (D-B1);
        // ⛔ a USAGE BIT run is the §13.18.60.4 GR5 sub-byte packing laid out by §8.5.1.6.3 and windowed by
        // CobolBits.ReadWindow/WriteWindow — THE kb/Work PB231 ARM, carried on every surface or none.
        { "BWR06", "PIC 1(8)", 8, true },
        { "BWR07", "PIC 1(8) USAGE BIT", 1, true },
        { "BWR08", "PIC 1(4) USAGE BIT", 1, true },
        // NATIONAL, in BOTH its spellings — the category (PIC N / USAGE NATIONAL) and the national-form
        // NUMERIC (PIC 9 USAGE NATIONAL, §13.18.60.3 SR12). ⛔ CARRIED since kb/Work PB231 discharged
        // RESIDUE-11: §13.18.60.4 GR8 leaves the size to the implementor ("characters of a uniform size equal
        // to or a multiple of the size of characters in the computer's alphanumeric character set") and D-N1
        // pins TWO bytes, so the leaf's storage extent is 2n and the window transcodes the UTF-16BE pair
        // (Place.NationalWindow / CobolBits.NatReadWindow). The BYTES column is what proves it: an alias of
        // 2n is what the REDEFINES spelling needs and what §14.9.3.4 GR3 allocates.
        { "BWR09", "PIC N(4)", 8, true },
        // ⚠ NOT A ROW, AND THE REASON IS MEASURED, NOT DEDUCED: the gate's OTHER national spelling, a
        // national-form NUMERIC (`PIC 9(3) USAGE NATIONAL`, §13.18.60.3 SR12), never reaches any of these four
        // surfaces — `CheckDataAttributes` stages it loud at COBOLNET0899 ("national-form numeric data … is
        // recognized but not yet implemented", the Phase 4a national-DIGITS residue) before storage
        // classification runs. Its arm in ByteWindowResidueOf is the derived storage answer for the day that
        // stage lifts, and it is carried there rather than here so the two national spellings cannot drift.
    };

    /// <summary>⛔ THE AGREEMENT ASSERTION: all four byte-window surfaces return the same verdict for the
    /// same leaf. It is the whole point of the shared gate, and it is stated over the verdicts rather than
    /// over a remembered list so that discharging a residue needs no edit here.</summary>
    [Theory]
    [MemberData(nameof(Leaves))]
    public void EveryByteWindowSurface_AgreesOnTheSameLeaf(string tag, string leaf, int bytes, bool carried)
    {
        (string Surface, string Src)[] spellings =
        [
            ("REDEFINES",  Redefines(tag + "R", leaf, bytes)),
            ("EXTERNAL",   External(tag + "E", leaf)),
            ("BASED",      Based(tag + "B", leaf)),
            ("ADDRESS-OF", AddressOf(tag + "A", leaf)),
        ];
        var verdicts = spellings
            .Select(s => { var (ok, detail) = Run(s.Src); return (s.Surface, Ok: ok, Detail: detail); })
            .ToArray();

        // The AGREEMENT half FIRST, stated pairwise so the failure message names the two surfaces that
        // drifted. It comes first deliberately: it is the assertion that survives the landing which
        // discharges a residue, so it is the one whose failure branch was fired to prove it works —
        // re-introducing PB231's exact divergence (a bit deny in ForceStringCanonical only) makes it red.
        for (int i = 1; i < verdicts.Length; i++)
            Assert.True(verdicts[0].Ok == verdicts[i].Ok,
                $"BYTE-WINDOW GATE DRIFT on '{leaf}': {verdicts[0].Surface} "
                + $"{(verdicts[0].Ok ? "carries" : "refuses")} it but {verdicts[i].Surface} "
                + $"{(verdicts[i].Ok ? "carries" : "refuses")} it. Both must route through "
                + $"DataBinder.ByteWindowResidueOf (kb/Work PB231).\n"
                + $"  {verdicts[0].Surface}: {verdicts[0].Detail}\n  {verdicts[i].Surface}: {verdicts[i].Detail}");

        // The PINNED half — without it, "they all agree" would be satisfied by all four regressing together
        // (feedback_green_gates_arent_evidence: a check that never looked at what changed proves nothing).
        foreach (var (surface, ok, detail) in verdicts)
            Assert.True(ok == carried,
                $"{surface} over '{leaf}': expected {(carried ? "carried" : "refused")}, got "
                + $"{(ok ? "carried" : "refused")} — {detail}");
    }

    /// <summary>⛔ THE GATE HAS NO RESIDUE LEFT, AND WHAT REPLACED ITS LAST REFUSALS ARE CONFORMANCE SCREENS
    /// (kb/Work PB231, the pointer third). This test is the successor of <c>ARefusal_NamesItsResidue</c>, and
    /// the replacement is forced rather than stylistic: that test asserted a refusal, every leaf kind the gate
    /// can see is now carried, and an assertion with no true instance left is not a test
    /// (feedback_measure_the_selectors_complement — a green that looked at nothing).
    /// <para>What must stay true is a THREE-PART fact, and each part fails independently:</para>
    /// <list type="number">
    /// <item>the level-1 <c>USAGE POINTER BASED</c> shape — §13.18.60.3 SR14's first arm, the one PB231's
    /// pointer third implements — COMPILES AND RUNS, and reads NULL after a plain ALLOCATE (§14.9.3.4 GR9);</item>
    /// <item><c>USAGE OBJECT REFERENCE BASED</c> is REFUSED, by §13.18.5.3 SR1 ("The subject of the entry shall
    /// not be of class object") — COBOLNET1797, not a residue;</item>
    /// <item><c>USAGE POINTER EXTERNAL</c> is REFUSED, by §13.18.22.3 SR4 ("The EXTERNAL clause shall not be
    /// specified for a data item of class object or pointer") — COBOLNET1796.</item>
    /// </list>
    /// <para>Parts 2 and 3 are the ones the landing could most easily have broken: both shapes used to be
    /// rejected by the residue diagnostic COBOLNET1695, so opening the gate without them would have turned two
    /// NONCONFORMING programs into clean compiles — an under-rejection created by a fix. The asymmetry between
    /// them (BASED bars only class object; EXTERNAL bars class object AND class pointer) is the standard's own,
    /// and asserting BOTH is what keeps a future "simplification" from unifying the two lists.</para></summary>
    [Fact]
    public void ThePointerClassLeafRides_AndTheTwoNonconformingShapesAreRefusedByTheirOwnRules()
    {
        var (carried, carriedOut) = RunOut("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. BWRPTR1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 P USAGE POINTER BASED.
            01 WS-P USAGE POINTER.
            PROCEDURE DIVISION.
            MAIN.
                ALLOCATE P RETURNING WS-P
                IF P = NULL DISPLAY "NULL" ELSE DISPLAY "SET" END-IF
                FREE WS-P
                STOP RUN.
            """);
        Assert.True(carried, "a level-1 USAGE POINTER BASED item is legal source — §13.18.5.3 SR1 bars only "
            + "class OBJECT from a BASED entry, and §14.9.3.3 SR1 asks only for the BASED clause. It rides the "
            + $"storage area's managed slot (kb/Work PB231): {carriedOut}");
        Assert.Contains("NULL", carriedOut, StringComparison.Ordinal);   // §14.9.3.4 GR9

        var (objOk, objDetail) = Run("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. BWRPTR2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 O USAGE OBJECT REFERENCE BASED.
            PROCEDURE DIVISION.
            MAIN.
                STOP RUN.
            """);
        Assert.False(objOk, "§13.18.5.3 SR1: the subject of a BASED entry shall not be of class object");
        Assert.Contains("COBOLNET1797", objDetail, StringComparison.Ordinal);

        var (extOk, extDetail) = Run("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. BWRPTR3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 EP USAGE POINTER EXTERNAL.
            PROCEDURE DIVISION.
            MAIN.
                STOP RUN.
            """);
        Assert.False(extOk, "§13.18.22.3 SR4: the EXTERNAL clause shall not be specified for a data item of "
            + "class object or pointer");
        Assert.Contains("COBOLNET1796", extDetail, StringComparison.Ordinal);
    }

    private static (bool Ok, string Detail) Run(string src)
    {
        var (ok, _, detail) = new CobolNetCompiler(2023).CompileAndRun(src);
        return (ok, detail);
    }

    /// <summary>The run's STDOUT — for the one assertion that needs the program's own output rather than the
    /// compiler's verdict text (§14.9.3.4 GR9's null seeding, observed).</summary>
    private static (bool Ok, string Out) RunOut(string src)
    {
        var (ok, stdout, _) = new CobolNetCompiler(2023).CompileAndRun(src);
        return (ok, stdout);
    }

    // ── The four spellings of one record ────────────────────────────────────────────────────────────────

    private static string Redefines(string pid, string leaf, int bytes) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {pid}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 A PIC X({bytes}).
        01 B REDEFINES A.
           05 L {leaf}.
        PROCEDURE DIVISION.
        MAIN.
            DISPLAY "L=[" L "]"
            STOP RUN.
        """;

    private static string External(string pid, string leaf) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {pid}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 G EXTERNAL.
           05 L {leaf}.
        PROCEDURE DIVISION.
        MAIN.
            DISPLAY "L=[" L "]"
            STOP RUN.
        """;

    private static string Based(string pid, string leaf) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {pid}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 R BASED.
           05 L {leaf}.
        01 WS-P USAGE POINTER.
        PROCEDURE DIVISION.
        MAIN.
            ALLOCATE R RETURNING WS-P
            DISPLAY "L=[" L "]"
            FREE WS-P
            STOP RUN.
        """;

    private static string AddressOf(string pid, string leaf) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {pid}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 REC.
           05 L {leaf}.
        01 WS-P USAGE POINTER.
        PROCEDURE DIVISION.
        MAIN.
            SET WS-P TO ADDRESS OF REC
            DISPLAY "L=[" L "]"
            STOP RUN.
        """;
}
