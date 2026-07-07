// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The intrinsic-function catalog (ISO §15; COBOLNET_INTRINSICS_DESIGN spine 1; the Phase-1B brief). Two nets:
/// <list type="bullet">
///   <item><b>Differential</b> — pinned to the legacy oracle where it is sound (float math on values away from
///         quantization boundaries, string functions, statistics): the 42-program NIST IF suite is the bulk net;
///         these cover the channel matrix (COMPUTE / IF / EVALUATE / MOVE) compactly.</item>
///   <item><b>Spec-pinned</b> — where the legacy diverges from the standard or the value IS the spec: the §15.64.4
///         MOD sign table, §15.61.4 MEDIAN, the §15.5.2 integer-date epoch, NUMVAL/NUMVAL-C formats (§15.67/68),
///         CHAR/ORD ordinals (§15.15/§15.70), the FromDouble ROUNDING choice (hazard H2 — the legacy truncates the
///         LOG10(1000) double artifact to 2.999999; rounding is the better §15.4.1 approximation), D8 edition
///         gating, and the loud-failure doctrine for the named uncovered channels (hazard H3).</item>
/// </list>
/// </summary>
public sealed class IntrinsicFunctionDifferentialTests
{
    private static readonly ICompilerUnderTest Legacy = new LegacyCompiler();
    private static readonly ICompilerUnderTest CobolNet = new CobolNetCompiler();

    private static void AssertSameAsLegacy(string source)
    {
        var (lok, lout, ldetail) = Legacy.CompileAndRun(source);
        Assert.True(lok, $"legacy oracle failed: {ldetail}");
        var (cok, cout, cdetail) = CobolNet.CompileAndRun(source);
        Assert.True(cok, $"COBOL.NET failed: {cdetail}");
        Assert.Equal(lout, cout);
    }

    private static void AssertSpec(string source, string expected, int dialect = 85)
    {
        var (cok, cout, cdetail) = new CobolNetCompiler(dialect).CompileAndRun(source);
        Assert.True(cok, $"COBOL.NET failed: {cdetail}");
        Assert.Equal(CutRunner.Normalize(expected), cout);
    }

    private static string Program(string ws, string proc, string id = "IFTEST") => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {id}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {ws}
        PROCEDURE DIVISION.
        MAIN-PARA.
        {proc}
            STOP RUN.
        """;

    // ── Differential: float math away from quantization boundaries (§15.4.1 approximation license) ──────────

    [Theory]
    [InlineData("COMPUTE R = FUNCTION SQRT(625).")]                     // 25 exact
    [InlineData("COMPUTE R = FUNCTION SQRT(2.25).")]                    // 1.5 exact
    [InlineData("COMPUTE R = FUNCTION SIN(0).")]                        // 0
    [InlineData("COMPUTE R = FUNCTION COS(0).")]                        // 1
    [InlineData("COMPUTE R = FUNCTION ATAN(0).")]                       // 0
    [InlineData("COMPUTE R = FUNCTION ACOS(1).")]                       // 0
    [InlineData("COMPUTE R = FUNCTION LOG10(100).")]                    // 2 (double-exact)
    [InlineData("COMPUTE R = FUNCTION ANNUITY(0, 4).")]                 // §15.9.4 rate 0 ⇒ 1/n = 0.25 exact
    [InlineData("COMPUTE R = FUNCTION SQRT(2) * 0.")]                   // intrinsic inside a larger expression
    public void FloatFamily_MatchesLegacy(string stmt) =>
        AssertSameAsLegacy(Program("01 R PIC S9(5)V9(4).", $"    {stmt}\n    DISPLAY R."));

    // ── Differential: exact statistics + the channel matrix ─────────────────────────────────────────────────

    [Theory]
    [InlineData("01 R PIC S9(5)V9(4).", "COMPUTE R = FUNCTION SUM(1.5, 2.25, 3).")]         // §15.88 — scale-aligned Σ
    [InlineData("01 R PIC S9(5)V9(4).", "COMPUTE R = FUNCTION MEAN(1, 2, 4).")]             // §15.60 — Σ/n
    [InlineData("01 R PIC S9(5)V9(4).", "COMPUTE R = FUNCTION MAX(-4.3, 10.2, -0.7, 3.9).")]
    [InlineData("01 R PIC S9(5)V9(4).", "COMPUTE R = FUNCTION MIN(7, -2, 19).")]
    [InlineData("01 R PIC S9(5)V9(4).", "COMPUTE R = FUNCTION RANGE(3, 11, 7).")]           // §15.76 max − min
    [InlineData("01 R PIC S9(5)V9(4).", "COMPUTE R = FUNCTION MIDRANGE(2, 10).")]           // §15.62 (min+max)/2
    [InlineData("01 R PIC S9(9).", "COMPUTE R = FUNCTION FACTORIAL(9).")]                   // §15.36 — 362880
    [InlineData("01 R PIC S9(9).", "COMPUTE R = FUNCTION INTEGER(-1.5).")]                  // §15.44 floor → −2
    [InlineData("01 R PIC S9(9).", "COMPUTE R = FUNCTION INTEGER-PART(-1.5).")]             // §15.49 truncate → −1
    public void ExactFamily_MatchesLegacy(string ws, string stmt) =>
        AssertSameAsLegacy(Program(ws, $"    {stmt}\n    DISPLAY R."));

    [Fact]
    public void ChannelMatrix_IfEvaluateMove_MatchesLegacy() =>
        // The three non-COMPUTE channels the NIST suite exercises: a FUNCTION in an IF relation, as an EVALUATE
        // subject, and as a MOVE source (§15.2 — usable wherever a sending item of its category is).
        AssertSameAsLegacy(Program("01 R PIC S9(5)V9(4).\n01 T PIC X(5).", """
                IF FUNCTION SQRT(625) = 25 DISPLAY "REL-OK" END-IF.
                EVALUATE FUNCTION MAX(3, 9, 4)
                    WHEN 9 DISPLAY "EVAL-OK"
                    WHEN OTHER DISPLAY "EVAL-BAD"
                END-EVALUATE.
                MOVE FUNCTION REVERSE("ABCDE") TO T.
                DISPLAY T.
            """));

    [Fact]
    public void TableAllExpansion_MatchesLegacy() =>
        // table(ALL): each occurrence becomes a separate argument, left to right (§15.3).
        AssertSameAsLegacy(Program("""
            01 P PIC S9(4) VALUE 2.
            01 ARR VALUE "40537".
                02 IND OCCURS 5 TIMES PIC 9.
            01 R PIC S9(5)V9(4).
            """, """
                COMPUTE R = FUNCTION SUM(IND(ALL)).
                DISPLAY R.
                COMPUTE R = FUNCTION MAX(IND(ALL)).
                DISPLAY R.
                COMPUTE R = FUNCTION ORD-MAX(IND(ALL)).
                DISPLAY R.
                COMPUTE R = FUNCTION MEDIAN(IND(ALL)).
                DISPLAY R.
                COMPUTE R = FUNCTION SUM(IND(ALL)) + IND(P).
                DISPLAY R.
            """));

    [Theory]
    // SPEC-PINNED, not differential: the bracketed DISPLAY exposes the legacy's known non-conformance (it trims
    // an alphanumeric operand's trailing spaces, contra ISO §14.9.11.4 GR6 — "the size … is the sum of the sizes
    // of the operands"); the X(8) receiver's full padded image is the correct output.
    [InlineData("MOVE FUNCTION UPPER-CASE(\"hello9z\") TO T.", "[HELLO9Z ]")]   // §15.97
    [InlineData("MOVE FUNCTION LOWER-CASE(\"HELLO9Z\") TO T.", "[hello9z ]")]   // §15.57
    [InlineData("MOVE FUNCTION REVERSE(\"abc 12\") TO T.", "[21 cba  ]")]       // §15.78
    public void StringFamily_PinnedToSpec(string stmt, string expected) =>
        AssertSpec(Program("01 T PIC X(8).", $"    {stmt}\n    DISPLAY \"[\" T \"]\"."), expected);

    // ── Spec-pinned: the §15.64.4 MOD / §15.77.4 REM sign tables ─────────────────────────────────────────────

    [Theory]
    [InlineData("11, 5", "+0001")]     // the §15.64.4 NOTE table, row by row
    [InlineData("-11, 5", "+0004")]
    [InlineData("11, -5", "-0004")]
    [InlineData("-11, -5", "-0001")]
    public void Mod_SignTable_PinnedToSpec(string args, string expected) =>
        AssertSpec(Program("01 R PIC S9(4) SIGN LEADING SEPARATE.",
            $"    COMPUTE R = FUNCTION MOD({args}).\n    DISPLAY R."), expected);

    [Fact]
    public void Rem_TruncatedRemainder_PinnedToSpec() =>
        // §15.77.4: a − b × INTEGER-PART(a/b) — sign follows the dividend; fractional operands are class numeric.
        AssertSpec(Program("01 R PIC S9(4)V9 SIGN LEADING SEPARATE.", """
                COMPUTE R = FUNCTION REM(-11, 5).
                DISPLAY R.
                COMPUTE R = FUNCTION REM(7.5, 2).
                DISPLAY R.
            """), "-00010\n+00015");

    [Fact]
    public void Median_EvenCount_MeanOfMiddles_PinnedToSpec() =>
        // §15.61.4 rule 2: an even argument count returns (b + c) / 2 of the two middle values — exact at one
        // extra fraction digit (the renderer's ×10/2 discipline).
        AssertSpec(Program("01 R PIC 9(3)V9(2).",
            "    COMPUTE R = FUNCTION MEDIAN(1, 2, 3, 4).\n    DISPLAY R."), "00250");

    [Fact]
    public void OrdMax_TieTakesFirst_PinnedToSpec() =>
        // §15.71.4: the ordinal of the GREATEST argument; the strictly-greater scan keeps the FIRST of equals.
        AssertSpec(Program("01 R PIC 9(3).",
            "    COMPUTE R = FUNCTION ORD-MAX(3, 7, 7, 2).\n    DISPLAY R."), "002");

    // ── Spec-pinned: hazard H2 — FromDouble ROUNDS at the quantization point ────────────────────────────────

    [Fact]
    public void Log10_1000_RoundsTheDoubleArtifact_PinnedToSpec() =>
        // Math.Log10(1000) = 2.9999999999999996 in IEEE double. §15.4.1 licenses an implementor-defined
        // APPROXIMATION; rounding inside the ONE FromDouble yields the true value 3 (the legacy's truncation
        // yields 2.999999 — conforming but strictly worse; hazard H2). The greenfield value is pinned.
        AssertSpec(Program("01 R PIC 9V9(6).",
            "    COMPUTE R = FUNCTION LOG10(1000).\n    DISPLAY R."), "3000000");

    [Fact]
    public void OutOfDomain_EcDefaultZero_PinnedToSpec() =>
        // §15.3: with EC-ARGUMENT-FUNCTION checking disabled "the implementor defines the result" — this
        // implementation returns 0 (NaN → 0 in FromDouble; the legacy-compatible default the goldens encode).
        AssertSpec(Program("01 R PIC S9(4)V9(2) SIGN LEADING SEPARATE.", """
                COMPUTE R = FUNCTION SQRT(0 - 4).
                DISPLAY R.
                COMPUTE R = FUNCTION ACOS(2).
                DISPLAY R.
                COMPUTE R = FUNCTION LOG(0).
                DISPLAY R.
                COMPUTE R = FUNCTION FACTORIAL(0 - 1).
                DISPLAY R.
            """), "+000000\n+000000\n+000000\n+000000");

    // ── Spec-pinned: NUMVAL / NUMVAL-C formats (§15.67.3 / §15.68.3) ─────────────────────────────────────────

    [Theory]
    [InlineData("\"  -123.45 \"", "-0012345")]      // leading sign, surrounding spaces (format 1)
    [InlineData("\"   -  929.03\"", "-0092903")]    // spaces between sign and digits — ignored before the first digit (r2)
    [InlineData("\"82.93+\"", "+0008293")]          // trailing sign (format 2)
    [InlineData("\"12cr\"", "-0001200")]            // CR suffix, case-insensitive (§15.67.3 r1) ⇒ negative (§15.67.4 r2)
    [InlineData("\".5\"", "+0000050")]              // the ". digit" alternative
    [InlineData("\"1O2\"", "+0000000")]             // malformed (letter O) → EC-ARGUMENT default 0 (§15.3)
    public void Numval_Formats_PinnedToSpec(string arg, string expected) =>
        AssertSpec(Program("01 R PIC S9(5)V9(2) SIGN LEADING SEPARATE.",
            $"    COMPUTE R = FUNCTION NUMVAL({arg}).\n    DISPLAY R."), expected);

    [Theory]
    [InlineData("\"$1,234.56\"", "+0123456")]       // default currency + grouping separators ignored (§15.68.4 r2)
    [InlineData("\"- $ 890.05\"", "-0089005")]      // sign before currency with space-strings (§15.68.3 r4a)
    [InlineData("\"Z93,021\", \"Z\"", "+9302100")]  // argument-2 names the currency string (§15.68.3 r2)
    public void NumvalC_Formats_PinnedToSpec(string args, string expected) =>
        AssertSpec(Program("01 R PIC S9(5)V9(2) SIGN LEADING SEPARATE.",
            $"    COMPUTE R = FUNCTION NUMVAL-C({args}).\n    DISPLAY R."), expected);

    [Fact]
    public void NumvalC_DefaultCurrencyFromSpecialNames_PinnedToSpec() =>
        // §15.68.3 r3: with argument-2 omitted, the compilation unit's ONE currency string applies — the
        // SPECIAL-NAMES CURRENCY SIGN literal, injected by the binder at bind time.
        AssertSpec("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. NVCCUR.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                CURRENCY SIGN IS "F".
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 R PIC S9(5)V9(2) SIGN LEADING SEPARATE.
            PROCEDURE DIVISION.
            MAIN-PARA.
                COMPUTE R = FUNCTION NUMVAL-C("F12.50").
                DISPLAY R.
                STOP RUN.
            """, "+0001250");

    // ── Spec-pinned: CHAR / ORD ordinals + LENGTH fold + nesting ─────────────────────────────────────────────

    [Fact]
    public void CharOrd_NativeOrdinals_PinnedToSpec() =>
        // §15.15.4: CHAR(n) is the character in ordinal position n of the collating sequence (native: code n−1,
        // so CHAR(66) = 'A'); §15.70.4: ORD is its inverse; nesting must round-trip (the §15.3 nested-FUNCTION
        // argument shape).
        AssertSpec(Program("01 T PIC X(1).\n01 R PIC 9(3).", """
                MOVE FUNCTION CHAR(66) TO T.
                DISPLAY T.
                COMPUTE R = FUNCTION ORD("A").
                DISPLAY R.
                COMPUTE R = FUNCTION ORD(FUNCTION CHAR(42)).
                DISPLAY R.
            """), "A\n066\n042");

    [Fact]
    public void LengthFold_CharacterPositions_PinnedToSpec() =>
        // §15.50.4: length in character positions — a literal's size; a numeric item's digit positions (an
        // over-punched sign occupies none, a SEPARATE sign one, §13.18.52); a group's leaf sum; and
        // LENGTH(REVERSE(x)) = LENGTH(x) via the fixed-width image (D7).
        AssertSpec(Program("""
            01 N9 PIC S9(5)V9(3).
            01 NSEP PIC S9(3) SIGN TRAILING SEPARATE.
            01 G.
                02 A1 PIC X(7).
                02 A2 PIC 9(4) OCCURS 3 TIMES.
            01 R PIC 9(3).
            """, """
                COMPUTE R = FUNCTION LENGTH("ABCD").
                DISPLAY R.
                COMPUTE R = FUNCTION LENGTH(N9).
                DISPLAY R.
                COMPUTE R = FUNCTION LENGTH(NSEP).
                DISPLAY R.
                COMPUTE R = FUNCTION LENGTH(G).
                DISPLAY R.
                COMPUTE R = FUNCTION LENGTH(FUNCTION REVERSE("ABCDEFG")).
                DISPLAY R.
            """), "004\n008\n004\n019\n007");

    // ── Spec-pinned: the date/time family ────────────────────────────────────────────────────────────────────

    [Fact]
    public void IntegerDateForm_Epoch_PinnedToSpec() =>
        // §15.5.2: integer date 1 = Monday 1601-01-01; INTEGER-OF-DATE(99991231) = 3,067,671. Invalid calendar
        // dates → the EC-ARGUMENT default 0 (§15.3).
        AssertSpec(Program("01 R PIC 9(8).", """
                COMPUTE R = FUNCTION INTEGER-OF-DATE(16010101).
                DISPLAY R.
                COMPUTE R = FUNCTION DATE-OF-INTEGER(1).
                DISPLAY R.
                COMPUTE R = FUNCTION DAY-OF-INTEGER(1).
                DISPLAY R.
                COMPUTE R = FUNCTION INTEGER-OF-DATE(99991231).
                DISPLAY R.
                COMPUTE R = FUNCTION INTEGER-OF-DAY(FUNCTION DAY-OF-INTEGER(150000)).
                DISPLAY R.
                COMPUTE R = FUNCTION INTEGER-OF-DATE(20230230).
                DISPLAY R.
            """), "00000001\n16010101\n01601001\n03067671\n00150000\n00000000");

    [Fact]
    public void WhenCompiled_CompileTimeConstant_PinnedToSpec() =>
        // §15.99.3 r2: WHEN-COMPILED is the COMPILATION timestamp (baked constant) — never after CURRENT-DATE
        // at run time, and in the §15.21.3 21-character layout (4-digit year sanity-checked via ref-mod).
        AssertSpec(Program("01 T PIC X(21).", """
                MOVE FUNCTION WHEN-COMPILED TO T.
                IF T(1:2) = "20" DISPLAY "CENTURY-OK" ELSE DISPLAY "CENTURY-BAD " T END-IF.
                IF FUNCTION CURRENT-DATE >= FUNCTION WHEN-COMPILED
                    DISPLAY "ORDER-OK"
                ELSE DISPLAY "ORDER-BAD" END-IF.
            """), "CENTURY-OK\nORDER-OK");

    [Fact]
    public void Random_SeededSequence_PinnedToSpec()
    {
        // §15.75.3 r3/r5 + §15.75.4 r1/r2: a seeded reference restarts the sequence (same seed ⇒ same first
        // value), argument-less references continue it, and every value is in [0, 1).
        var (ok, stdout, detail) = CobolNet.CompileAndRun(Program(
            "01 R1 PIC V9(9).\n01 R2 PIC V9(9).\n01 R3 PIC V9(9).", """
                COMPUTE R1 = FUNCTION RANDOM(7).
                COMPUTE R2 = FUNCTION RANDOM.
                COMPUTE R3 = FUNCTION RANDOM(7).
                IF R1 = R3 DISPLAY "SEED-REPEAT-OK" ELSE DISPLAY "SEED-REPEAT-BAD" END-IF.
                IF R2 NOT = R1 DISPLAY "NEXT-DIFFERS-OK" ELSE DISPLAY "NEXT-DIFFERS-BAD" END-IF.
            """));
        Assert.True(ok, detail);
        Assert.Equal("SEED-REPEAT-OK\nNEXT-DIFFERS-OK", stdout);
    }

    // ── D8 edition gating + the loud-failure doctrine (hazards H3/H6) ────────────────────────────────────────

    [Fact]
    public void EditionGate_2014FunctionAt85_RejectedByName()
    {
        // D8: outside its window a function is rejected with a diagnostic naming the function and editions
        // (TRIM was introduced by ISO/IEC 1989:2014 — catalog row; the 1989-module rows bind at 85).
        var (ok, _, detail) = new CobolNetCompiler(85).CompileAndRun(
            Program("01 T PIC X(5).", "    MOVE FUNCTION TRIM(\"  X  \") TO T.\n    DISPLAY T."));
        Assert.False(ok);
        Assert.Contains("TRIM", detail);
        Assert.Contains("COBOLNET1502", detail);
    }

    [Fact]
    public void DeferredFunction_InWindow_FailsLoud_NeverWrong()
    {
        // A catalogued-but-deferred function INSIDE its window compiles and fails LOUD at run time naming the
        // function (COBOLNET_DESIGN §1.4) — never a silent wrong value. (BYTE-LENGTH §15.14 is still Deferred —
        // the byte-size ≠ FUNCTION LENGTH; it awaits the USAGE-width model. Retargeted off FORMATTED-CURRENT-DATE,
        // now implemented in DEVLOG 635.)
        var (ok, _, detail) = new CobolNetCompiler(2023).CompileAndRun(
            Program("01 T PIC 9(4).", "    MOVE FUNCTION BYTE-LENGTH(\"ABC\") TO T.\n    DISPLAY T."));
        Assert.False(ok);
        Assert.Contains("BYTE-LENGTH", detail);
    }

    [Fact]
    public void NumericIntrinsicInStringContext_FailsLoud()
    {
        // Hazard H3: a NUMERIC-result intrinsic moved to an alphanumeric receiver is a named staged-out channel
        // (the §14.9.25 numeric→alphanumeric move of a function result) — loud, not wrong.
        var (ok, _, detail) = CobolNet.CompileAndRun(
            Program("01 T PIC X(8).", "    MOVE FUNCTION NUMVAL(\"1\") TO T.\n    DISPLAY T."));
        Assert.False(ok);
        Assert.Contains("not", detail, StringComparison.OrdinalIgnoreCase);
    }

    // ── 2023 string intrinsics (Phase 5, DEVLOG 628): CONCAT §15.18, BASECONVERT §15.12 ─────────────────────

    [Fact]
    public void Concat_ConcatenatesArgumentImages_2023()
    {
        // §15.18.4 rule 1 — all characters of each argument (a fixed-width image includes its trailing padding);
        // rule 4 — variadic, left to right. A = PIC X(3) VALUE "AB" ⇒ image "AB " ⇒ CONCAT(A, B) = "AB CD".
        AssertSpec(Program(
            "01 A PIC X(3) VALUE \"AB\".\n           01 B PIC X(2) VALUE \"CD\".\n           01 R PIC X(8).",
            "    MOVE FUNCTION CONCAT(A, B) TO R.\n    DISPLAY R.\n"
            + "    MOVE FUNCTION CONCAT(\"X\", \"Y\", \"Z\") TO R.\n    DISPLAY R.", "IFCAT"),
            "AB CD\nXYZ", 2023);
    }

    [Theory]
    [InlineData("\"FF\", 16, 10", "255")]     // §15.12.4 — base 16 → base 10
    [InlineData("\"255\", 10, 16", "FF")]     //           base 10 → base 16
    [InlineData("\"1010\", 2, 16", "A")]      //           base 2  → base 16 (10 = A)
    [InlineData("\"0\", 10, 2", "0")]         //           zero
    public void BaseConvert_ReExpressesInTargetBase_2023(string args, string expected)
        => AssertSpec(Program("01 R PIC X(10).",
            $"    MOVE FUNCTION BASECONVERT({args}) TO R.\n    DISPLAY R.", "IFBASE"), expected, 2023);

    [Fact]
    public void Concat_And_BaseConvert_GatedBelow2023_1502()
    {
        // Both are 2023 introductions (D8 edition window) — the binder names the edition (COBOLNET1502) at 2014.
        foreach (string call in new[] { "FUNCTION CONCAT(\"A\", \"B\")", "FUNCTION BASECONVERT(\"F\", 16, 10)" })
        {
            var (ok, _, detail) = new CobolNetCompiler(2014).CompileAndRun(
                Program("01 R PIC X(8).", $"    MOVE {call} TO R.\n    DISPLAY R."));
            Assert.False(ok, $"{call} is 2023+; 2014 must reject");
            Assert.Contains("COBOLNET1502", detail);
        }
    }

    // ── TRIM (§15.96, Phase 5, DEVLOG 629): LEADING/TRAILING/both + the 2023 argument-2 char set ─────────────

    [Theory]
    [InlineData("FUNCTION TRIM(S)", "HELLO")]            // §15.96.4 r3 — both, default space (the 2014 form)
    [InlineData("FUNCTION TRIM(S LEADING)", "HELLO")]    // r1 — leading (the trailing padding is Normalize-trimmed)
    [InlineData("FUNCTION TRIM(S TRAILING)", "  HELLO")] // r2 — trailing (leading spaces are preserved)
    public void Trim_SpaceForm_2014(string call, string expected)
        => AssertSpec(Program("01 S PIC X(10) VALUE \"  HELLO   \".\n           01 R PIC X(12).",
            $"    MOVE {call} TO R.\n    DISPLAY R.", "IFTRIM"), expected, 2014);

    [Fact]
    public void Trim_ArgumentTwoCharSet_2023()
        // §15.96 argument-2 (delete a specified character) — the 2023 enhancement. Z = "0042" ⇒ leading "0" ⇒ "42".
        => AssertSpec(Program("01 Z PIC X(8) VALUE \"0042\".\n           01 R PIC X(12).",
            "    MOVE FUNCTION TRIM(Z LEADING \"0\") TO R.\n    DISPLAY R.", "IFTRIM2"), "42", 2023);

    [Fact]
    public void Trim_ArgumentTwo_GatedBelow2023_1502_ButSpaceFormBinds()
    {
        // The argument-2 form is a 2023 enhancement (Annex E.3.3 item 31) — rejected at 2014 by name+edition…
        var (ok, _, detail) = new CobolNetCompiler(2014).CompileAndRun(
            Program("01 Z PIC X(8) VALUE \"0042\".\n           01 R PIC X(8).",
                "    MOVE FUNCTION TRIM(Z LEADING \"0\") TO R.\n    DISPLAY R."));
        Assert.False(ok, "TRIM argument-2 is 2023+; 2014 must reject");
        Assert.Contains("COBOLNET1502", detail);
        // …but TRIM itself (the space-trimming form) is 2014, so it binds+runs there.
        var (ok2, out2, d2) = new CobolNetCompiler(2014).CompileAndRun(
            Program("01 S PIC X(8) VALUE \"  HI  \".\n           01 R PIC X(8).",
                "    MOVE FUNCTION TRIM(S TRAILING) TO R.\n    DISPLAY R."));
        Assert.True(ok2, d2);
        Assert.Equal("  HI", out2);
    }

    // ── FIND-STRING (§15.37, Phase 5, DEVLOG 630): substring position; LAST / START AFTER argument-3 / ANYCASE ─

    [Theory]
    [InlineData("FUNCTION FIND-STRING(H N)", "1")]                     // §15.37.4 r1 — first occurrence
    [InlineData("FUNCTION FIND-STRING(H N LAST)", "7")]                // r1 — last occurrence (positions 1,4,7)
    [InlineData("FUNCTION FIND-STRING(H N START AFTER 1)", "4")]       // r2 — ignore 1 match from the first
    [InlineData("FUNCTION FIND-STRING(H N LAST START AFTER 1)", "4")]  // r1+r2 — 1 before the last
    [InlineData("FUNCTION FIND-STRING(H \"ZZ\")", "0")]                // r3 — no match
    public void FindString_Positions_2023(string call, string expected)
        => AssertSpec(Program("01 H PIC X(9) VALUE \"ABCABCABC\".\n           01 N PIC X(3) VALUE \"ABC\".\n           01 P PIC 9.",
            $"    MOVE {call} TO P.\n    DISPLAY P.", "IFFIND"), expected, 2023);

    [Fact]
    public void FindString_Anycase_FoldsCase_2023()
        // §15.37.4 r4 — ANYCASE folds case per LOWER-CASE; "WORLD" matches "World" at position 7.
        => AssertSpec(Program("01 T PIC X(11) VALUE \"Hello World\".\n           01 P PIC 9.",
            "    MOVE FUNCTION FIND-STRING(T \"WORLD\" ANYCASE) TO P.\n    DISPLAY P.", "IFFINDA"), "7", 2023);

    [Fact]
    public void FindString_CaseSensitiveByDefault_2023()
        // Without ANYCASE the match is ordinal — "WORLD" (upper) does not occur in "Hello World" ⇒ 0 (r3).
        => AssertSpec(Program("01 T PIC X(11) VALUE \"Hello World\".\n           01 P PIC 9.",
            "    MOVE FUNCTION FIND-STRING(T \"WORLD\") TO P.\n    DISPLAY P.", "IFFINDC"), "0", 2023);

    [Fact]
    public void FindString_GatedBelow2023_1502()
    {
        // FIND-STRING is a 2023 addition (§15.37) — rejected by name+edition below 2023.
        var (ok, _, detail) = new CobolNetCompiler(2014).CompileAndRun(
            Program("01 H PIC X(3) VALUE \"ABC\".\n           01 P PIC 9.",
                "    MOVE FUNCTION FIND-STRING(H \"B\") TO P.\n    DISPLAY P."));
        Assert.False(ok, "FIND-STRING is 2023+; 2014 must reject");
        Assert.Contains("COBOLNET1502", detail);
    }

    // ── SUBSTITUTE (§15.87, Phase 5, DEVLOG 631): per-pair replacement; ANYCASE / FIRST / LAST; multi-pair ────

    [Theory]
    [InlineData("FUNCTION SUBSTITUTE(S \"A\" \"X\")", "XBXBXB")]           // §15.87.4 r3 — all occurrences
    [InlineData("FUNCTION SUBSTITUTE(S FIRST \"A\" \"X\")", "XBABAB")]     // r3.a — first only
    [InlineData("FUNCTION SUBSTITUTE(S LAST \"A\" \"X\")", "ABABXB")]      // r3.b — last only
    [InlineData("FUNCTION SUBSTITUTE(S \"AB\" \"WXYZ\")", "WXYZWXYZWXYZ")] // growing replacement, all
    public void Substitute_SinglePair_2023(string call, string expected)
        => AssertSpec(Program("01 S PIC X(6) VALUE \"ABABAB\".\n           01 R PIC X(12).",
            $"    MOVE {call} TO R.\n    DISPLAY R.", "IFSUBS"), expected, 2023);

    [Fact]
    public void Substitute_Anycase_FoldsCase_2023()
        // §15.87.4 r5 — ANYCASE folds case; every a/A in "aAaA" becomes "-".
        => AssertSpec(Program("01 S PIC X(4) VALUE \"aAaA\".\n           01 R PIC X(8).",
            "    MOVE FUNCTION SUBSTITUTE(S ANYCASE \"a\" \"-\") TO R.\n    DISPLAY R.", "IFSUBA"), "----", 2023);

    [Fact]
    public void Substitute_MultiPair_OnePass_2023()
        // §15.87.4 r3/r4 — two pairs applied in one left-to-right pass: "CAT DOG" ⇒ "FISH BIRD".
        => AssertSpec(Program("01 S PIC X(7) VALUE \"CAT DOG\".\n           01 R PIC X(12).",
            "    MOVE FUNCTION SUBSTITUTE(S \"CAT\" \"FISH\" \"DOG\" \"BIRD\") TO R.\n    DISPLAY R.", "IFSUBM"),
            "FISH BIRD", 2023);

    [Fact]
    public void Substitute_GatedBelow2023_1502()
    {
        // SUBSTITUTE is a 2023 addition (§15.87) — rejected by name+edition below 2023.
        var (ok, _, detail) = new CobolNetCompiler(2014).CompileAndRun(
            Program("01 S PIC X(3) VALUE \"ABC\".\n           01 R PIC X(3).",
                "    MOVE FUNCTION SUBSTITUTE(S \"B\" \"X\") TO R.\n    DISPLAY R."));
        Assert.False(ok, "SUBSTITUTE is 2023+; 2014 must reject");
        Assert.Contains("COBOLNET1502", detail);
    }

    // ── CONVERT (§15.19, Phase 5, DEVLOG 632): data-representation conversion; source/destination formats ─────

    [Theory]
    [InlineData("01 A PIC X VALUE \"A\".\n           01 R PIC X(4).", "CONVERT(A ANUM ANUM HEX)", "41")]   // NOTE 3a
    [InlineData("01 H PIC XX VALUE \"41\".\n           01 R PIC X(4).", "CONVERT(H HEX ANUM)", "A")]         // NOTE 3b
    [InlineData("01 H PIC XX VALUE \"41\".\n           01 R PIC X(4).", "CONVERT(H HEX BYTE)", "A")]         // r5
    [InlineData("01 A PIC XXX VALUE \"AB\".\n           01 R PIC X(8).", "CONVERT(A ANUM ANUM HEX)", "414220")] // r2, image space 0x20
    [InlineData("01 N PIC N VALUE N\"A\".\n           01 R PIC X(8).", "CONVERT(N NAT ANUM HEX)", "0041")]   // r2 over UTF-16BE
    public void Convert_Formats_2023(string ws, string call, string expected)
        => AssertSpec(Program(ws, $"    MOVE FUNCTION {call} TO R.\n    DISPLAY R.", "IFCONV"), expected, 2023);

    [Fact]
    public void Convert_AnumNatAnum_RoundTrip_2023()
        // §15.19.4 r1/r3 — ANUM→NAT→ANUM repertoire round-trip returns the original character (Latin-1 ⊂ national).
        => AssertSpec(Program("01 A PIC X VALUE \"Z\".\n           01 NR PIC N.\n           01 R PIC X(4).",
            "    MOVE FUNCTION CONVERT(A ANUM NAT) TO NR.\n    MOVE FUNCTION CONVERT(NR NAT ANUM) TO R.\n    DISPLAY R.",
            "IFCONVR"), "Z", 2023);

    [Theory]
    [InlineData("CONVERT(A ANUM ANUM)", "COBOLNET1514")]   // SR3 — source == destination
    [InlineData("CONVERT(A ANUM BYTE)", "COBOLNET1514")]   // SR9 — BYTE needs a HEX source
    [InlineData("CONVERT(A ANY ANUM)", "COBOLNET1514")]    // SR8 — ANY needs an ANUM HEX / NAT HEX destination
    public void Convert_SyntaxRuleViolations_1514(string call, string code)
    {
        var (ok, _, detail) = new CobolNetCompiler(2023).CompileAndRun(
            Program("01 A PIC X VALUE \"A\".\n           01 R PIC X(4).",
                $"    MOVE FUNCTION {call} TO R.\n    DISPLAY R."));
        Assert.False(ok, $"{call} violates a §15.19.3 syntax rule");
        Assert.Contains(code, detail);
    }

    [Fact]
    public void Convert_GatedBelow2023_1502()
    {
        var (ok, _, detail) = new CobolNetCompiler(2014).CompileAndRun(
            Program("01 A PIC X VALUE \"A\".\n           01 R PIC X(4).",
                "    MOVE FUNCTION CONVERT(A ANUM ANUM HEX) TO R.\n    DISPLAY R."));
        Assert.False(ok, "CONVERT is 2023+; 2014 must reject");
        Assert.Contains("COBOLNET1502", detail);
    }

    [Fact]
    public void Convert_UntranslatableSetsDataConversion_WhenChecked_2023()
    {
        // §15.19.4 r1 — a national character with no alphanumeric correspondent (U+0100) yields the substitution
        // char AND sets EC-DATA-CONVERSION; under >>TURN … CHECKING ON, EXCEPTION-STATUS reports the condition.
        var src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. IFCONVEC.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-N PIC N(2) VALUE N"AŠ".
            01 WS-R PIC X(4).
            PROCEDURE DIVISION.
            MAIN.
            >>TURN EC-DATA-CONVERSION CHECKING ON
                MOVE FUNCTION CONVERT(WS-N NAT ANUM) TO WS-R.
            >>TURN EC-DATA-CONVERSION CHECKING OFF
                DISPLAY "S=" FUNCTION EXCEPTION-STATUS.
                STOP RUN.
            """;
        var (ok, output, detail) = new CobolNetCompiler(2023).CompileAndRun(src);
        Assert.True(ok, detail);
        // EXCEPTION-STATUS is a fixed-width register; its trailing spaces are trimmed by Normalize.
        Assert.Equal("S=EC-DATA-CONVERSION", output);
    }

    // ── MODULE-NAME (§15.65, Phase 5, DEVLOG 633): the runtime COBOL hierarchy; CURRENT/ACTIVATING/… keyword ──

    [Theory]
    [InlineData("CURRENT", "[MODNM   ]")]        // §15.65.4 r7 — the running compilation unit's outermost program
    [InlineData("TOP-LEVEL", "[MODNM   ]")]      // r10 — the run-unit main
    [InlineData("ACTIVATING", "[        ]")]     // r5 — a main program's activator is a single space
    public void ModuleName_MainProgram_2023(string kw, string expected)
        => AssertSpec(Program("01 N PIC X(8).",
            $"    MOVE FUNCTION MODULE-NAME({kw}) TO N.\n    DISPLAY \"[\" N \"]\".", "MODNM"), expected, 2023);

    [Fact]
    public void ModuleName_ActivatingAcrossCall_2023()
    {
        // §15.65.4 r5 — in a CALLed program ACTIVATING is the caller's name; STACK (r9) is CURRENT;…;TOP-LEVEL;space.
        var src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DRVMOD.
            PROCEDURE DIVISION.
            MAIN.
                CALL "HLPMOD".
                STOP RUN.
            END PROGRAM DRVMOD.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. HLPMOD.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 N PIC X(10).
            01 S PIC X(20).
            PROCEDURE DIVISION.
            HP.
                MOVE FUNCTION MODULE-NAME(ACTIVATING) TO N.
                DISPLAY "A=[" N "]".
                MOVE FUNCTION MODULE-NAME(STACK) TO S.
                DISPLAY "S=[" S "]".
                EXIT PROGRAM.
            END PROGRAM HLPMOD.
            """;
        var (ok, output, detail) = new CobolNetCompiler(2023).CompileAndRun(src);
        Assert.True(ok, detail);
        Assert.Equal("A=[DRVMOD    ]\nS=[HLPMOD;DRVMOD;      ]", output);
    }

    [Fact]
    public void ModuleName_GatedBelow2023_1502()
    {
        var (ok, _, detail) = new CobolNetCompiler(2014).CompileAndRun(
            Program("01 N PIC X(4).", "    MOVE FUNCTION MODULE-NAME(CURRENT) TO N.\n    DISPLAY N.", "MODNG"));
        Assert.False(ok, "MODULE-NAME is 2023+; 2014 must reject");
        Assert.Contains("COBOLNET1502", detail);
    }

    [Fact]
    public void ModuleName_NestedOutsideNestedProgram_1515()
    {
        // §15.65.3 argument rule 1 — NESTED shall be specified only within a contained program.
        var (ok, _, detail) = new CobolNetCompiler(2023).CompileAndRun(
            Program("01 N PIC X(8).", "    MOVE FUNCTION MODULE-NAME(NESTED) TO N.\n    DISPLAY N.", "MODNN"));
        Assert.False(ok, "MODULE-NAME NESTED requires a contained program");
        Assert.Contains("COBOLNET1515", detail);
    }

    // ── SMALLEST/HIGHEST/LOWEST-ALGEBRAIC (§15.83/§15.43/§15.58, Phase 5, DEVLOG 634): PICTURE-metadata folds ──

    [Theory]
    [InlineData("HIGHEST-ALGEBRAIC(X)", "01 X PIC S99.\n           01 R PIC +999.", "+099", 2002)]
    [InlineData("LOWEST-ALGEBRAIC(X)",  "01 X PIC S99.\n           01 R PIC +999.", "-099", 2002)]
    [InlineData("HIGHEST-ALGEBRAIC(X)", "01 X PIC S9(4).\n           01 R PIC +99999.", "+09999", 2002)]
    [InlineData("LOWEST-ALGEBRAIC(X)",  "01 X PIC 99V9(3).\n           01 R PIC +99.999.", "+00.000", 2002)] // unsigned ⇒ 0
    [InlineData("SMALLEST-ALGEBRAIC(X)","01 X PIC S9PP.\n           01 R PIC +9999.", "+0100", 2023)]        // scale −2
    [InlineData("SMALLEST-ALGEBRAIC(X)","01 X PIC 99V9(3).\n           01 R PIC +9.999.", "+0.001", 2023)]
    public void Algebraic_Fold(string call, string ws, string expected, int dialect)
        => AssertSpec(Program(ws, $"    MOVE FUNCTION {call} TO R.\n    DISPLAY R.", "ALGB"), expected, dialect);

    [Fact]
    public void Algebraic_LiteralArgument_1516()
    {
        // §15.83.3 r1 — argument-1 shall be a data item, not a literal.
        var (ok, _, detail) = new CobolNetCompiler(2023).CompileAndRun(
            Program("01 R PIC +999.", "    MOVE FUNCTION SMALLEST-ALGEBRAIC(5) TO R.\n    DISPLAY R."));
        Assert.False(ok, "a literal argument violates §15.83.3 r1");
        Assert.Contains("COBOLNET1516", detail);
    }

    [Fact]
    public void Algebraic_SmallestGatedBelow2023_1502()
    {
        var (ok, _, detail) = new CobolNetCompiler(2014).CompileAndRun(
            Program("01 X PIC S999.\n           01 R PIC +999.",
                "    MOVE FUNCTION SMALLEST-ALGEBRAIC(X) TO R.\n    DISPLAY R."));
        Assert.False(ok, "SMALLEST-ALGEBRAIC is 2023+; 2014 must reject");
        Assert.Contains("COBOLNET1502", detail);
    }

    // ── The COBOL-2014 date/time + number family (§15.17/38-41/48/69/79/92/95, Phase 5, DEVLOG 635) ───────────

    [Fact]
    public void FormattedTime_UtcRollAcrossMidnight_2014()
        // §15.41 r2 — a UTC format shows local − offset; 01:00 local at +2h ⇒ 23:00:00Z (the previous day).
        => AssertSpec(Program("01 R PIC X(12).",
            "    MOVE FUNCTION FORMATTED-TIME(\"hh:mm:ssZ\", 3600, 120) TO R.\n    DISPLAY R.", "FT1"), "23:00:00Z", 2014);

    [Fact]
    public void IntegerOfFormattedDate_Ordinal_2014()
        => AssertSpec(Program("01 N PIC +9(9).",
            "    MOVE FUNCTION INTEGER-OF-FORMATTED-DATE(\"YYYYDDD\", \"2021167\") TO N.\n    DISPLAY N.", "IOF"), "+000153569", 2014);

    [Fact]
    public void SecondsFromFormattedTime_Fractional_2014()
        // The result scale comes from the format's fractional-second count (2 here) — §15.79.4.
        => AssertSpec(Program("01 F PIC +9(5).99.",
            "    MOVE FUNCTION SECONDS-FROM-FORMATTED-TIME(\"hh:mm:ss.ss\", \"12:34:56.50\") TO F.\n    DISPLAY F.", "SFT"), "+45296.50", 2014);

    [Theory]
    [InlineData("YYYYMMDD", "20051314", "+000000006")]     // month 13 becomes provable at digit 6 (§15.92 NOTE)
    [InlineData("YYYYMMDD", "15990316", "+000000002")]     // year 15xx < 1601 provable at digit 2
    [InlineData("YYYY-MM-DD", "2021-06-16", "+000000000")] // valid ⇒ 0
    public void TestFormattedDatetime_ErrorPosition_2014(string fmt, string data, string expected)
        => AssertSpec(Program("01 N PIC +9(9).",
            $"    MOVE FUNCTION TEST-FORMATTED-DATETIME(\"{fmt}\", \"{data}\") TO N.\n    DISPLAY N.", "TFD"), expected, 2014);

    [Theory]
    [InlineData("1.5E+3", "+1500.0000")]
    [InlineData("-2.5E-2", "-0000.0250")]
    public void NumvalF_Values_2014(string arg, string expected)
        => AssertSpec(Program("01 F PIC +9(4).9(4).",
            $"    COMPUTE F = FUNCTION NUMVAL-F(\"{arg}\").\n    DISPLAY F.", "NVF"), expected, 2014);

    [Theory]
    [InlineData("0 1E+2", "+000000003")]   // an embedded space ⇒ the first non-space after it (§15.95 r b.1)
    [InlineData(" +.", "+000000004")]       // no significand digit ⇒ LENGTH+1 (r c)
    [InlineData("1.5E+3", "+000000000")]    // valid ⇒ 0
    public void TestNumvalF_Positions_2014(string data, string expected)
        => AssertSpec(Program("01 N PIC +9(9).",
            $"    MOVE FUNCTION TEST-NUMVAL-F(\"{data}\") TO N.\n    DISPLAY N.", "TNF"), expected, 2014);

    [Fact]
    public void DateFamily_GatedBelow2014_1502()
    {
        var (ok, _, detail) = new CobolNetCompiler(2002).CompileAndRun(
            Program("01 R PIC X(10).", "    MOVE FUNCTION FORMATTED-DATE(\"YYYYMMDD\", 153569) TO R.\n    DISPLAY R."));
        Assert.False(ok, "FORMATTED-DATE is 2014+; 2002 must reject");
        Assert.Contains("COBOLNET1502", detail);
    }

    [Fact]
    public void FormattedDate_NonLiteralFormat_1517()
    {
        // §15.39.3 r1 — the format shall be a literal (analyzed at compile time).
        var (ok, _, detail) = new CobolNetCompiler(2014).CompileAndRun(
            Program("01 FMT PIC X(8) VALUE \"YYYYMMDD\".\n           01 R PIC X(10).",
                "    MOVE FUNCTION FORMATTED-DATE(FMT, 153569) TO R.\n    DISPLAY R."));
        Assert.False(ok, "a non-literal format violates §15.39.3 r1");
        Assert.Contains("COBOLNET1517", detail);
    }
}
