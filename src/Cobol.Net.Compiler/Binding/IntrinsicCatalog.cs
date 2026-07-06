// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding;

/// <summary>The ISO §15.2 function-type classification — THE return-type column of the catalog (deep-dive D1):
/// Integer → scale-0 native integer; Numeric → exact NumX (unscaled long + scale) or double for the §15.4.1
/// floating-math family; Alphanumeric/National → string; Boolean → bool; Index → occurrence number.</summary>
public enum IntrinsicType { Alphanumeric, Boolean, National, Numeric, Integer, Index }

/// <summary>The §15.3 arity model: a fixed argument count, optional trailing arguments (e.g. the RANDOM seed,
/// NUMVAL-C's currency), or a variable-length list (the statistical functions).</summary>
public enum IntrinsicArity { Fixed, OptionalTrailing, Variadic }

/// <summary>How a call binds (deep-dive D2/D7): <see cref="Runtime"/> = a <c>CobolIntrinsics</c>/<c>CobolDate</c>
/// call; <see cref="Fold"/> = resolved at compile time (LENGTH from PIC metadata §15.50, WHEN-COMPILED's
/// compilation timestamp §15.99.3 r2); <see cref="Deferred"/> = catalogued (so D8 edition gating and arity checks
/// apply) but not yet implemented — renders a LOUD not-implemented guard (COBOLNET_DESIGN §1.4), never a wrong
/// value.</summary>
public enum IntrinsicBind { Runtime, Fold, Deferred }

/// <summary>
/// One catalog row (deep-dive D2 — the SINGLE source of result-category truth). <paramref name="ArgKinds"/> is the
/// per-position argument-category code per §15.3 — <c>'n'</c> numeric, <c>'i'</c> integer, <c>'s'</c>
/// alphanumeric/string, <c>'p'</c> category-polymorphic (MAX/MIN families resolve by the bound arguments) — with
/// the LAST character repeating for arguments past the string's length (the variadic tail).
/// <paramref name="Float"/> marks the §15.4.1 floating-math family (computes in double; the emitter quantizes via
/// the one <c>FromDouble</c>). <paramref name="IntroducedIn"/>/<paramref name="RemovedIn"/> are the D8 edition
/// window (85 = the 1989 Intrinsic Function Module amendment, part of the CCVS-85 corpus), enforced by the binder
/// against <c>--std</c> with a per-edition diagnostic.
/// </summary>
public readonly record struct IntrinsicSig(
    string Name, IntrinsicType Type, IntrinsicArity Arity, int MinArgs, int MaxArgs,
    string ArgKinds, string RuntimeMethod, IntrinsicBind Bind, bool Float,
    int IntroducedIn, int? RemovedIn = null)
{
    /// <summary>The §15.3 kind code of argument position <paramref name="i"/> (0-based; the last code repeats).</summary>
    public char ArgKind(int i) =>
        ArgKinds.Length == 0 ? 'n' : ArgKinds[Math.Min(i, ArgKinds.Length - 1)];

    /// <summary>The data category of the function result (§15.2 → §8.4.2) — what MOVE/comparison/DISPLAY consult.</summary>
    public PicCategory ResultCategory =>
        Type is IntrinsicType.Alphanumeric or IntrinsicType.National ? PicCategory.Alphanumeric : PicCategory.Numeric;
}

/// <summary>
/// The ONE declarative intrinsic-function table (ISO §15.6 summary of functions; deep-dive D2 — replaces the
/// legacy's ad-hoc AlphanumericFunctions set + scattered special cases). Adding a function is one row. The 42
/// functions of the 1989 Intrinsic Function Module (the NIST IF suite) are fully implemented; later-edition
/// functions are catalogued with their D8 windows and bind <see cref="IntrinsicBind.Deferred"/> (loud) until their
/// subsystem wave lands. Edition windows for post-85 rows beyond the 2023 seven-function delta
/// (docs/VERSION_CHANGE_REFERENCE.md rows 65–73) are PROVISIONAL pending the version-test-matrix wave — D8 notes
/// the 85↔2002 gating derives from the 2002 standard, which the reference doc does not yet tabulate.
/// </summary>
public static class IntrinsicCatalog
{
    public static bool TryGet(string name, out IntrinsicSig sig) => Table.TryGetValue(name, out sig);

    private static readonly Dictionary<string, IntrinsicSig> Table = Build();

    private static Dictionary<string, IntrinsicSig> Build()
    {
        var t = new Dictionary<string, IntrinsicSig>(StringComparer.OrdinalIgnoreCase);
        void Add(IntrinsicSig s) => t.Add(s.Name, s);
        const int inf = int.MaxValue;

        // ── The 1989 Intrinsic Function Module (IntroducedIn 85 — the NIST IF101A..IF142A surface) ──────────
        // §15.4.1 floating-math family (double + FromDouble quantization; Float: true).
        Add(new("ACOS", IntrinsicType.Numeric, IntrinsicArity.Fixed, 1, 1, "n", "Acos", IntrinsicBind.Runtime, true, 85));           // §15.8
        Add(new("ASIN", IntrinsicType.Numeric, IntrinsicArity.Fixed, 1, 1, "n", "Asin", IntrinsicBind.Runtime, true, 85));           // §15.10
        Add(new("ATAN", IntrinsicType.Numeric, IntrinsicArity.Fixed, 1, 1, "n", "Atan", IntrinsicBind.Runtime, true, 85));           // §15.11
        Add(new("COS", IntrinsicType.Numeric, IntrinsicArity.Fixed, 1, 1, "n", "Cos", IntrinsicBind.Runtime, true, 85));             // §15.20
        Add(new("SIN", IntrinsicType.Numeric, IntrinsicArity.Fixed, 1, 1, "n", "Sin", IntrinsicBind.Runtime, true, 85));             // §15.82
        Add(new("TAN", IntrinsicType.Numeric, IntrinsicArity.Fixed, 1, 1, "n", "Tan", IntrinsicBind.Runtime, true, 85));             // §15.89
        Add(new("SQRT", IntrinsicType.Numeric, IntrinsicArity.Fixed, 1, 1, "n", "Sqrt", IntrinsicBind.Runtime, true, 85));           // §15.84
        Add(new("LOG", IntrinsicType.Numeric, IntrinsicArity.Fixed, 1, 1, "n", "Log", IntrinsicBind.Runtime, true, 85));             // §15.55
        Add(new("LOG10", IntrinsicType.Numeric, IntrinsicArity.Fixed, 1, 1, "n", "Log10", IntrinsicBind.Runtime, true, 85));         // §15.56
        Add(new("ANNUITY", IntrinsicType.Numeric, IntrinsicArity.Fixed, 2, 2, "ni", "Annuity", IntrinsicBind.Runtime, true, 85));    // §15.9
        Add(new("PRESENT-VALUE", IntrinsicType.Numeric, IntrinsicArity.Variadic, 2, inf, "n", "PresentValue", IntrinsicBind.Runtime, true, 85)); // §15.74
        Add(new("RANDOM", IntrinsicType.Numeric, IntrinsicArity.OptionalTrailing, 0, 1, "i", "Random", IntrinsicBind.Runtime, true, 85));        // §15.75
        Add(new("STANDARD-DEVIATION", IntrinsicType.Numeric, IntrinsicArity.Variadic, 1, inf, "n", "StandardDeviation", IntrinsicBind.Runtime, true, 85)); // §15.86
        Add(new("VARIANCE", IntrinsicType.Numeric, IntrinsicArity.Variadic, 1, inf, "n", "Variance", IntrinsicBind.Runtime, true, 85));          // §15.98

        // Exact numeric / integer family (unscaled Int128 at a known scale — deep-dive D1).
        Add(new("FACTORIAL", IntrinsicType.Integer, IntrinsicArity.Fixed, 1, 1, "i", "Factorial", IntrinsicBind.Runtime, false, 85)); // §15.36
        Add(new("INTEGER", IntrinsicType.Integer, IntrinsicArity.Fixed, 1, 1, "n", "Floor", IntrinsicBind.Runtime, false, 85));       // §15.44
        Add(new("INTEGER-PART", IntrinsicType.Integer, IntrinsicArity.Fixed, 1, 1, "n", "Truncate", IntrinsicBind.Runtime, false, 85)); // §15.49
        Add(new("MOD", IntrinsicType.Integer, IntrinsicArity.Fixed, 2, 2, "ii", "ModScaled", IntrinsicBind.Runtime, false, 85));      // §15.64
        Add(new("REM", IntrinsicType.Numeric, IntrinsicArity.Fixed, 2, 2, "nn", "RemScaled", IntrinsicBind.Runtime, false, 85));      // §15.77
        Add(new("MAX", IntrinsicType.Numeric, IntrinsicArity.Variadic, 1, inf, "p", "MaxScaled", IntrinsicBind.Runtime, false, 85));  // §15.59 (category-polymorphic)
        Add(new("MIN", IntrinsicType.Numeric, IntrinsicArity.Variadic, 1, inf, "p", "MinScaled", IntrinsicBind.Runtime, false, 85));  // §15.63 (category-polymorphic)
        Add(new("ORD-MAX", IntrinsicType.Integer, IntrinsicArity.Variadic, 1, inf, "p", "OrdMax", IntrinsicBind.Runtime, false, 85)); // §15.71
        Add(new("ORD-MIN", IntrinsicType.Integer, IntrinsicArity.Variadic, 1, inf, "p", "OrdMin", IntrinsicBind.Runtime, false, 85)); // §15.72
        Add(new("SUM", IntrinsicType.Numeric, IntrinsicArity.Variadic, 1, inf, "n", "SumScaled", IntrinsicBind.Runtime, false, 85));  // §15.88
        Add(new("MEAN", IntrinsicType.Numeric, IntrinsicArity.Variadic, 1, inf, "n", "MeanScaled", IntrinsicBind.Runtime, false, 85)); // §15.60
        Add(new("MEDIAN", IntrinsicType.Numeric, IntrinsicArity.Variadic, 1, inf, "n", "MedianScaled", IntrinsicBind.Runtime, false, 85)); // §15.61
        Add(new("MIDRANGE", IntrinsicType.Numeric, IntrinsicArity.Variadic, 1, inf, "n", "MidrangeScaled", IntrinsicBind.Runtime, false, 85)); // §15.62
        Add(new("RANGE", IntrinsicType.Numeric, IntrinsicArity.Variadic, 1, inf, "n", "RangeScaled", IntrinsicBind.Runtime, false, 85)); // §15.76
        Add(new("NUMVAL", IntrinsicType.Numeric, IntrinsicArity.Fixed, 1, 1, "s", "Numval", IntrinsicBind.Runtime, false, 85));       // §15.67
        Add(new("NUMVAL-C", IntrinsicType.Numeric, IntrinsicArity.OptionalTrailing, 1, 2, "ss", "NumvalC", IntrinsicBind.Runtime, false, 85)); // §15.68

        // Character family + LENGTH/ORD.
        Add(new("CHAR", IntrinsicType.Alphanumeric, IntrinsicArity.Fixed, 1, 1, "i", "Char", IntrinsicBind.Runtime, false, 85));      // §15.15
        Add(new("ORD", IntrinsicType.Integer, IntrinsicArity.Fixed, 1, 1, "s", "Ord", IntrinsicBind.Runtime, false, 85));             // §15.70
        Add(new("LENGTH", IntrinsicType.Integer, IntrinsicArity.Fixed, 1, 1, "s", "Length", IntrinsicBind.Fold, false, 85));          // §15.50 (D7 compile-time fold)
        Add(new("LOWER-CASE", IntrinsicType.Alphanumeric, IntrinsicArity.Fixed, 1, 1, "s", "LowerCase", IntrinsicBind.Runtime, false, 85)); // §15.57
        Add(new("UPPER-CASE", IntrinsicType.Alphanumeric, IntrinsicArity.Fixed, 1, 1, "s", "UpperCase", IntrinsicBind.Runtime, false, 85)); // §15.97
        Add(new("REVERSE", IntrinsicType.Alphanumeric, IntrinsicArity.Fixed, 1, 1, "s", "Reverse", IntrinsicBind.Runtime, false, 85)); // §15.78

        // Date/time family (CobolDate; integer date form §15.5.2).
        Add(new("CURRENT-DATE", IntrinsicType.Alphanumeric, IntrinsicArity.Fixed, 0, 0, "", "CurrentDate", IntrinsicBind.Runtime, false, 85)); // §15.21
        Add(new("WHEN-COMPILED", IntrinsicType.Alphanumeric, IntrinsicArity.Fixed, 0, 0, "", "WhenCompiled", IntrinsicBind.Fold, false, 85));  // §15.99 (compile-time constant, r2)
        Add(new("DATE-OF-INTEGER", IntrinsicType.Integer, IntrinsicArity.Fixed, 1, 1, "i", "DateOfInteger", IntrinsicBind.Runtime, false, 85)); // §15.22
        Add(new("DAY-OF-INTEGER", IntrinsicType.Integer, IntrinsicArity.Fixed, 1, 1, "i", "DayOfInteger", IntrinsicBind.Runtime, false, 85));   // §15.24
        Add(new("INTEGER-OF-DATE", IntrinsicType.Integer, IntrinsicArity.Fixed, 1, 1, "i", "IntegerOfDate", IntrinsicBind.Runtime, false, 85)); // §15.46
        Add(new("INTEGER-OF-DAY", IntrinsicType.Integer, IntrinsicArity.Fixed, 1, 1, "i", "IntegerOfDay", IntrinsicBind.Runtime, false, 85));   // §15.47

        // ── COBOL-2002 additions (implemented where the runtime body is trivial; Deferred otherwise) ─────────
        Add(new("E", IntrinsicType.Numeric, IntrinsicArity.Fixed, 0, 0, "", "E", IntrinsicBind.Runtime, true, 2002));                 // §15.27
        Add(new("PI", IntrinsicType.Numeric, IntrinsicArity.Fixed, 0, 0, "", "Pi", IntrinsicBind.Runtime, true, 2002));               // §15.73
        Add(new("EXP", IntrinsicType.Numeric, IntrinsicArity.Fixed, 1, 1, "n", "Exp", IntrinsicBind.Runtime, true, 2002));            // §15.34
        Add(new("EXP10", IntrinsicType.Numeric, IntrinsicArity.Fixed, 1, 1, "n", "Exp10", IntrinsicBind.Runtime, true, 2002));        // §15.35
        Add(new("SIGN", IntrinsicType.Integer, IntrinsicArity.Fixed, 1, 1, "n", "SignOf", IntrinsicBind.Runtime, false, 2002));       // §15.81
        Add(new("FRACTION-PART", IntrinsicType.Numeric, IntrinsicArity.Fixed, 1, 1, "n", "FractionPart", IntrinsicBind.Runtime, false, 2002)); // §15.42
        Add(new("BOOLEAN-OF-INTEGER", IntrinsicType.Boolean, IntrinsicArity.Fixed, 2, 2, "ii", "", IntrinsicBind.Deferred, false, 2002)); // §15.13
        Add(new("BYTE-LENGTH", IntrinsicType.Integer, IntrinsicArity.Fixed, 1, 1, "s", "", IntrinsicBind.Deferred, false, 2002));     // §15.14 (byte size ≠ FUNCTION LENGTH, D7)
        Add(new("CHAR-NATIONAL", IntrinsicType.National, IntrinsicArity.Fixed, 1, 1, "i", "", IntrinsicBind.Deferred, false, 2002));  // §15.16
        Add(new("DATE-TO-YYYYMMDD", IntrinsicType.Integer, IntrinsicArity.OptionalTrailing, 1, 3, "iii", "", IntrinsicBind.Deferred, false, 2002)); // §15.23
        Add(new("DAY-TO-YYYYDDD", IntrinsicType.Integer, IntrinsicArity.OptionalTrailing, 1, 3, "iii", "", IntrinsicBind.Deferred, false, 2002));   // §15.25
        Add(new("YEAR-TO-YYYY", IntrinsicType.Integer, IntrinsicArity.OptionalTrailing, 1, 3, "iii", "", IntrinsicBind.Deferred, false, 2002));     // §15.100
        Add(new("DISPLAY-OF", IntrinsicType.Alphanumeric, IntrinsicArity.OptionalTrailing, 1, 2, "ss", "", IntrinsicBind.Deferred, false, 2002));   // §15.26
        Add(new("NATIONAL-OF", IntrinsicType.National, IntrinsicArity.OptionalTrailing, 1, 2, "ss", "", IntrinsicBind.Deferred, false, 2002));      // §15.66
        Add(new("EXCEPTION-FILE", IntrinsicType.Alphanumeric, IntrinsicArity.OptionalTrailing, 0, 1, "s", "EcFile", IntrinsicBind.Runtime, false, 2002)); // §15.28 (no-arg form r1; the 2023 file-connector-arg form renders loud — VCR row 68)
        Add(new("EXCEPTION-FILE-N", IntrinsicType.National, IntrinsicArity.OptionalTrailing, 0, 1, "s", "", IntrinsicBind.Deferred, false, 2002));   // §15.29
        // EXCEPTION-LOCATION/-STATEMENT/-STATUS render the runtime last-exception register (EcFunctions, the §11
        // EC model); the -N national twins stay Deferred-loud — no national runtime exists, and faking national
        // as UTF-16 alphanumeric would be the wrong data class (§15.29/§15.31; EC scout hazard H8).
        Add(new("EXCEPTION-LOCATION", IntrinsicType.Alphanumeric, IntrinsicArity.Fixed, 0, 0, "", "EcLocation", IntrinsicBind.Runtime, false, 2002));  // §15.30
        Add(new("EXCEPTION-LOCATION-N", IntrinsicType.National, IntrinsicArity.Fixed, 0, 0, "", "", IntrinsicBind.Deferred, false, 2002));           // §15.31
        Add(new("EXCEPTION-STATEMENT", IntrinsicType.Alphanumeric, IntrinsicArity.Fixed, 0, 0, "", "EcStatement", IntrinsicBind.Runtime, false, 2002)); // §15.32
        Add(new("EXCEPTION-STATUS", IntrinsicType.Alphanumeric, IntrinsicArity.Fixed, 0, 0, "", "EcStatus", IntrinsicBind.Runtime, false, 2002));    // §15.33
        Add(new("HIGHEST-ALGEBRAIC", IntrinsicType.Numeric, IntrinsicArity.Fixed, 1, 1, "s", "", IntrinsicBind.Deferred, false, 2002)); // §15.43
        Add(new("LOWEST-ALGEBRAIC", IntrinsicType.Numeric, IntrinsicArity.Fixed, 1, 1, "s", "", IntrinsicBind.Deferred, false, 2002));  // §15.58
        Add(new("INTEGER-OF-BOOLEAN", IntrinsicType.Integer, IntrinsicArity.Fixed, 1, 1, "s", "", IntrinsicBind.Deferred, false, 2002)); // §15.45
        Add(new("LOCALE-COMPARE", IntrinsicType.Alphanumeric, IntrinsicArity.OptionalTrailing, 2, 3, "sss", "", IntrinsicBind.Deferred, false, 2002)); // §15.51
        Add(new("LOCALE-DATE", IntrinsicType.Alphanumeric, IntrinsicArity.OptionalTrailing, 1, 2, "is", "", IntrinsicBind.Deferred, false, 2002));     // §15.52
        Add(new("LOCALE-TIME", IntrinsicType.Alphanumeric, IntrinsicArity.OptionalTrailing, 1, 2, "is", "", IntrinsicBind.Deferred, false, 2002));     // §15.53
        Add(new("LOCALE-TIME-FROM-SECONDS", IntrinsicType.Alphanumeric, IntrinsicArity.OptionalTrailing, 1, 2, "ns", "", IntrinsicBind.Deferred, false, 2002)); // §15.54
        Add(new("SECONDS-PAST-MIDNIGHT", IntrinsicType.Numeric, IntrinsicArity.Fixed, 0, 0, "", "", IntrinsicBind.Deferred, false, 2002)); // §15.80
        Add(new("STANDARD-COMPARE", IntrinsicType.Alphanumeric, IntrinsicArity.OptionalTrailing, 2, 4, "ssis", "", IntrinsicBind.Deferred, false, 2002)); // §15.85
        Add(new("TEST-DATE-YYYYMMDD", IntrinsicType.Integer, IntrinsicArity.Fixed, 1, 1, "i", "", IntrinsicBind.Deferred, false, 2002)); // §15.90
        Add(new("TEST-DAY-YYYYDDD", IntrinsicType.Integer, IntrinsicArity.Fixed, 1, 1, "i", "", IntrinsicBind.Deferred, false, 2002));   // §15.91
        Add(new("TEST-NUMVAL", IntrinsicType.Integer, IntrinsicArity.Fixed, 1, 1, "s", "", IntrinsicBind.Deferred, false, 2002));        // §15.93
        Add(new("TEST-NUMVAL-C", IntrinsicType.Integer, IntrinsicArity.OptionalTrailing, 1, 2, "ss", "", IntrinsicBind.Deferred, false, 2002)); // §15.94
        // CONCATENATE: 2002–2014 only — the 2023 standard's §15 has CONCAT (§15.18) and no CONCATENATE (window
        // provisional; the 2023 E.2 delta names only CONCAT as new).
        Add(new("CONCATENATE", IntrinsicType.Alphanumeric, IntrinsicArity.Variadic, 1, inf, "s", "", IntrinsicBind.Deferred, false, 2002, 2023));

        // ── COBOL-2014 additions (windows provisional pending the matrix wave) ────────────────────────────────
        Add(new("ABS", IntrinsicType.Numeric, IntrinsicArity.Fixed, 1, 1, "n", "AbsScaled", IntrinsicBind.Runtime, false, 2014));     // §15.7
        Add(new("COMBINED-DATETIME", IntrinsicType.Numeric, IntrinsicArity.Fixed, 2, 2, "in", "", IntrinsicBind.Deferred, false, 2014)); // §15.17
        Add(new("FORMATTED-CURRENT-DATE", IntrinsicType.Alphanumeric, IntrinsicArity.Fixed, 1, 1, "s", "", IntrinsicBind.Deferred, false, 2014)); // §15.38
        Add(new("FORMATTED-DATE", IntrinsicType.Alphanumeric, IntrinsicArity.Fixed, 2, 2, "si", "", IntrinsicBind.Deferred, false, 2014));        // §15.39
        Add(new("FORMATTED-DATETIME", IntrinsicType.Alphanumeric, IntrinsicArity.OptionalTrailing, 3, 5, "sinns", "", IntrinsicBind.Deferred, false, 2014)); // §15.40
        Add(new("FORMATTED-TIME", IntrinsicType.Alphanumeric, IntrinsicArity.OptionalTrailing, 2, 4, "snns", "", IntrinsicBind.Deferred, false, 2014));      // §15.41
        Add(new("INTEGER-OF-FORMATTED-DATE", IntrinsicType.Integer, IntrinsicArity.Fixed, 2, 2, "ss", "", IntrinsicBind.Deferred, false, 2014)); // §15.48
        Add(new("NUMVAL-F", IntrinsicType.Numeric, IntrinsicArity.Fixed, 1, 1, "s", "", IntrinsicBind.Deferred, false, 2014));        // §15.69
        Add(new("SECONDS-FROM-FORMATTED-TIME", IntrinsicType.Numeric, IntrinsicArity.Fixed, 2, 2, "ss", "", IntrinsicBind.Deferred, false, 2014)); // §15.79
        Add(new("TEST-FORMATTED-DATETIME", IntrinsicType.Integer, IntrinsicArity.Fixed, 2, 2, "ss", "", IntrinsicBind.Deferred, false, 2014)); // §15.92
        Add(new("TEST-NUMVAL-F", IntrinsicType.Integer, IntrinsicArity.Fixed, 1, 1, "s", "", IntrinsicBind.Deferred, false, 2014));   // §15.95
        Add(new("TRIM", IntrinsicType.Alphanumeric, IntrinsicArity.Variadic, 1, inf, "ss", "Trim", IntrinsicBind.Runtime, false, 2014)); // §15.96 — arg-1 + the LEADING/TRAILING phrase + one-or-more argument-2 trim chars (special bind path)

        // ── COBOL-2023 additions (docs/VERSION_CHANGE_REFERENCE.md rows 65–73) ────────────────────────────────
        Add(new("BASECONVERT", IntrinsicType.Alphanumeric, IntrinsicArity.Fixed, 3, 3, "sii", "BaseConvert", IntrinsicBind.Runtime, false, 2023)); // §15.12
        Add(new("CONCAT", IntrinsicType.Alphanumeric, IntrinsicArity.Variadic, 2, inf, "s", "Concat", IntrinsicBind.Runtime, false, 2023)); // §15.18
        Add(new("CONVERT", IntrinsicType.Alphanumeric, IntrinsicArity.Variadic, 2, 4, "ssss", "", IntrinsicBind.Deferred, false, 2023)); // §15.19
        Add(new("FIND-STRING", IntrinsicType.Integer, IntrinsicArity.Variadic, 2, 3, "sssii", "FindString", IntrinsicBind.Runtime, false, 2023)); // §15.37 — arg-1 arg-2 [LAST] [[START AFTER] arg-3] [ANYCASE] (special bind path)
        Add(new("MODULE-NAME", IntrinsicType.Alphanumeric, IntrinsicArity.OptionalTrailing, 0, 1, "s", "", IntrinsicBind.Deferred, false, 2023)); // §15.65
        Add(new("SMALLEST-ALGEBRAIC", IntrinsicType.Numeric, IntrinsicArity.Fixed, 1, 1, "s", "", IntrinsicBind.Deferred, false, 2023)); // §15.83
        Add(new("SUBSTITUTE", IntrinsicType.Alphanumeric, IntrinsicArity.Variadic, 3, inf, "s", "", IntrinsicBind.Deferred, false, 2023)); // §15.87

        return t;
    }
}
