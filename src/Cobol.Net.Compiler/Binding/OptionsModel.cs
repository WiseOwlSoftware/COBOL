// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;

using CobolNet.Binding.Model;

namespace CobolNet.Binding;

/// <summary>
/// The fully-parsed content of a source unit's OPTIONS paragraph (ISO/IEC 1989:2023 §11.9), captured once at bind
/// time and exposed program-wide so every later pass can read it. CONSUMED today: ARITHMETIC +
/// INTERMEDIATE ROUNDING drive the numeric engine's standard-decimal SDIDI path (NumericRenderer.StandardDecimal
/// → CobolDec, §8.8.1.5), DEFAULT ROUNDED sets the bare-ROUNDED mode (§11.9.6). Still capture-only:
/// FLOAT-BINARY/DECIMAL (standard-float USAGE encodings) and INITIALIZE (the initial-state fill).
/// </summary>
/// <remarks>An absent OPTIONS paragraph (or an absent clause) yields the ISO-implied default — see
/// <see cref="Default"/>. The model carries the program's <i>intent</i>; whether a clause is yet <i>applied</i> is the
/// consuming feature's concern.</remarks>
public sealed record OptionsModel
{
    /// <summary>ARITHMETIC clause (§11.9.5). Default <see cref="ArithmeticMode.Native"/> when absent (§11.9.5.2 r4).</summary>
    public ArithmeticMode Arithmetic { get; init; } = ArithmeticMode.Native;

    /// <summary>DEFAULT ROUNDED clause (§11.9.6) — the rounding mode applied to a bare <c>ROUNDED</c> phrase that omits
    /// its own MODE. Default <see cref="CobolRounding.NearestAwayFromZero"/> when absent (§11.9.6 r2).</summary>
    public CobolRounding DefaultRounding { get; init; } = CobolRounding.NearestAwayFromZero;

    /// <summary>ENTRY-CONVENTION clause (§11.9.7): <c>COBOL</c> or an implementor entry-convention-name, as written;
    /// null when absent (the entry convention is then COBOL, §11.9.7.4).</summary>
    public string? EntryConvention { get; init; }

    /// <summary>FLOAT-BINARY clause endianness (§11.9.8). <see cref="FloatEndianness.Unspecified"/> when absent (the
    /// implementor default applies).</summary>
    public FloatEndianness FloatBinaryEndianness { get; init; } = FloatEndianness.Unspecified;

    /// <summary>FLOAT-DECIMAL clause encoding (§11.9.9). <see cref="FloatEncoding.Unspecified"/> when absent.</summary>
    public FloatEncoding FloatDecimalEncoding { get; init; } = FloatEncoding.Unspecified;

    /// <summary>FLOAT-DECIMAL clause endianness (§11.9.9). <see cref="FloatEndianness.Unspecified"/> when absent.</summary>
    public FloatEndianness FloatDecimalEndianness { get; init; } = FloatEndianness.Unspecified;

    /// <summary>INTERMEDIATE ROUNDING clause (§11.9.11) — rounding for intermediate results under standard
    /// arithmetic. Default <see cref="CobolRounding.NearestAwayFromZero"/> when absent (§11.9.11 r3).</summary>
    public CobolRounding IntermediateRounding { get; init; } = CobolRounding.NearestAwayFromZero;

    /// <summary>INITIALIZE clause (§11.9.10) — the section-background fill applied at allocation; null when absent.</summary>
    public OptionsInitialize? Initialize { get; init; }

    /// <summary>The all-defaults model used when a source unit has no OPTIONS paragraph (every ISO-implied default).</summary>
    public static readonly OptionsModel Default = new();
}

/// <summary>The ARITHMETIC clause mode (§11.9.5.1). <see cref="Standard"/> is the ISO/IEC 1989:2002 standardized
/// mode (NATIVE|STANDARD was the 2002 clause), designated obsolete by 2014 and removed by 2023 (Annex E.2
/// item 21) in favor of <see cref="StandardBinary"/>/<see cref="StandardDecimal"/>; the CCVS still writes it.</summary>
public enum ArithmeticMode { Native, Standard, StandardBinary, StandardDecimal }

/// <summary>Facts the standard states PER ARITHMETIC MODE. One table per fact, exhaustive over
/// <see cref="ArithmeticMode"/>, so adding a mode is a compile-or-drift-test failure rather than a silent
/// fall-through to whatever the old ternary's else-branch happened to be.</summary>
public static class ArithmeticModes
{
    /// <summary>The NUMVAL-family digit cap — the number of digits beyond which §15.93.4 r1b / §15.94.4 r1b
    /// report the next digit's position as the first character in error. The standard states it per mode, in
    /// three sub-notes of the same rule: <b>2)</b> "If native arithmetic is in effect, because the character in
    /// error for an argument that is greater than 31 digits is the 32nd digit…"; <b>3)</b> "If standard-binary
    /// arithmetic is in effect, and the argument has more than 35 digits…"; <b>4)</b> "If standard-decimal
    /// arithmetic is in effect, and the argument has more than 34 digits…".
    ///
    /// <para><see cref="ArithmeticMode.Standard"/> (the 2002 mode) is not named by those sub-notes; it takes 34
    /// because it routes to the SAME SDIDI decimal engine as STANDARD-DECIMAL for its reachable operands
    /// (<c>NumericRenderer.StandardDecimal</c>, <c>DataBinder.BindDeclarations</c>).</para>
    ///
    /// <para>⛔ <see cref="ArithmeticMode.StandardBinary"/>'s 35 is UNREACHABLE and is written down anyway. The
    /// mode is declined at bind (<c>OptionsBinder.ArithmeticOf</c> → COBOLNET0806), so no compilation can carry
    /// it this far — but a table with a hole is a table that answers a question it has never been asked, and the
    /// previous shape here was a two-state ternary whose else-branch silently gave standard-binary the NATIVE
    /// cap. Recording the spec's real value keeps the double defence honest: the mode is rejected, AND if it
    /// ever were not, this lane would not quietly emit the wrong number.
    /// <c>ArithmeticModeTableTests</c> asserts the table is exhaustive over the enum.</para></summary>
    public static int NumvalDigitCap(ArithmeticMode mode) => mode switch
    {
        ArithmeticMode.Native => 31,            // §15.93.4 r1b sub-note 2
        ArithmeticMode.Standard => 34,          // routes to the SDIDI decimal engine (see the remark above)
        ArithmeticMode.StandardBinary => 35,    // §15.93.4 r1b sub-note 3 — unreachable; declined at bind
        ArithmeticMode.StandardDecimal => 34,   // §15.93.4 r1b sub-note 4
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "no §15.93.4 r1b digit cap for this mode"),
    };

    /// <summary>The cap the runtime assumes when the emitted call omits the argument — the native one. Kept
    /// beside the table so "which value means omit" cannot drift away from "what the table says native is".</summary>
    public const int DefaultDigitCap = 31;
}

/// <summary>Endianness of a standard floating-point usage (FLOAT-BINARY / FLOAT-DECIMAL clauses, §11.9.8/§11.9.9).</summary>
public enum FloatEndianness { Unspecified, HighOrderLeft, HighOrderRight }

/// <summary>Encoding of a standard decimal floating-point usage (FLOAT-DECIMAL clause, §11.9.9).</summary>
public enum FloatEncoding { Unspecified, BinaryEncoding, DecimalEncoding }

/// <summary>The INITIALIZE clause (§11.9.10): which sections' background is set, and to what fill character.</summary>
/// <param name="Sections">The sections to initialize (ALL ⇒ all three).</param>
/// <param name="Fill">The fill kind.</param>
/// <param name="FillLiteral">The literal text when <paramref name="Fill"/> is <see cref="OptionsFill.Literal"/>.</param>
public sealed record OptionsInitialize(OptionsSections Sections, OptionsFill Fill, string? FillLiteral);

/// <summary>The sections an INITIALIZE OPTIONS clause targets (a flags set; ALL ⇒ all three).</summary>
[Flags]
public enum OptionsSections { None = 0, LocalStorage = 1, Screen = 2, WorkingStorage = 4, All = LocalStorage | Screen | WorkingStorage }

/// <summary>The fill character of an INITIALIZE OPTIONS clause (§11.9.10.4 r5).</summary>
public enum OptionsFill { BinaryZeroes, HighValues, LowValues, Spaces, Literal }
