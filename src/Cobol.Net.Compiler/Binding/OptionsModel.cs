// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;

namespace CobolNet.Binding;

/// <summary>
/// The fully-parsed content of a source unit's OPTIONS paragraph (ISO/IEC 1989:2023 §11.9), captured once at bind
/// time and exposed program-wide so every later pass can read it (the binder applies DEFAULT ROUNDED today; the
/// remaining clauses are captured for the features that will consume them — ARITHMETIC feeds the numeric engine,
/// FLOAT-BINARY/DECIMAL feed standard-float USAGE, INITIALIZE feeds the initial-state fill).
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

/// <summary>The ARITHMETIC clause mode (§11.9.5.1). <see cref="Standard"/> is the no-suffix spelling many compilers and
/// the CCVS accept alongside the standardized <see cref="StandardBinary"/>/<see cref="StandardDecimal"/> phrases.</summary>
public enum ArithmeticMode { Native, Standard, StandardBinary, StandardDecimal }

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
