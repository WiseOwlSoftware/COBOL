// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Runtime;
using Microsoft.CodeAnalysis.CSharp;

namespace CobolNet.CodeGen.Emit;

/// <summary>
/// The shared spine the decomposed backend emitters/renderers cooperate over (COBOLNET_DESIGN §17 §2.1): the
/// output <see cref="Writer"/> and the bound DATA DIVISION model (<see cref="Data"/>), plus the derived
/// SPECIAL-NAMES render config. IMMUTABLE since P7 Step 3 (renamed from <c>EmissionContext</c>, ending the
/// legacy-tree name collision): the four mutable receiver fields (<c>TargetScale</c>/<c>TargetReal</c>/
/// <c>TargetRounding</c>/<c>InSizeErrorContext</c>) and their manual set/reset discipline are DELETED — the
/// receiver a numeric render is computed for now travels BY PARAMETER (<see cref="ReceiverContext"/>), killing
/// the H1 staleness class by construction.
/// </summary>
internal sealed class EmitContext(CodeWriter writer, DataBinder data, NameAllocator names)
{
    /// <summary>The C# output writer.</summary>
    public CodeWriter Writer { get; } = writer;

    /// <summary>The bound DATA DIVISION model.</summary>
    public DataBinder Data { get; } = data;

    /// <summary>The RUN-UNIT-scoped unique-name allocator (P7 Step 9a) — the SAME instance rides every per-unit
    /// context of one generated module, so minted temporaries never collide across units.</summary>
    public NameAllocator Names { get; } = names;

    /// <summary>The trailing weights argument for collated comparison renders — <c>", __COLLATE"</c> when a
    /// PROGRAM COLLATING SEQUENCE is active (ISO §12.3.6 GR11 — relation and condition-name comparisons), else
    /// empty (the native two-argument <c>CobolString.Compare</c> overload).</summary>
    public string CollateArg => Data.Collating is null ? "" : ", __COLLATE";

    /// <summary>The NATIONAL twin of <see cref="CollateArg"/> — <c>", __COLLATE_NAT"</c> when a NON-native
    /// NATIONAL program collating sequence is active (ISO §12.3.6 GR11 / §8.8.4.2.9 — an <c>ALPHABET … FOR
    /// NATIONAL</c> literal phrase; the identity sequences NATIVE/UCS-4 stay null, D-N3), else empty. National
    /// comparisons NEVER take the alphanumeric <c>__COLLATE</c> table (its 256-entry domain would alias national
    /// characters through <c>&amp; 0xFF</c>).</summary>
    public string NatCollateArg => Data.NationalCollating is null ? "" : ", __COLLATE_NAT";

    /// <summary>The SPECIAL-NAMES editing-config suffix for generated <c>CobolEdit</c> calls (named arguments,
    /// composing after any <c>blankWhenZero:</c>): the program's currency PICTURE SYMBOL when not <c>$</c> and
    /// DECIMAL-POINT IS COMMA when set (ISO §12.3.7 GR13/GR14). Empty under the default config, so the
    /// generated code of an ordinary program is unchanged. The ONE producer of these arguments — used by the
    /// orchestrator's MOVE/arithmetic edited stores and the renderer's DeEdit.</summary>
    public string EditCfgArgs =>
        (Data.CurrencyPicSymbol != '$'
            ? $", currency: {SymbolDisplay.FormatLiteral(Data.CurrencyPicSymbol, quote: true)}" : "")
        + (Data.DecimalPointIsComma ? ", commaMode: true" : "");

    // FigFill lives in FigurativeConstants since P7 Step 4 (the ONE figurative service).
}

/// <summary>
/// A rendered numeric expression: a C# <c>long</c>-valued expression holding the UNSCALED value, plus its fractional
/// <see cref="Scale"/> (the implied decimal position). The pair lets the backend align scales (ISO §8.8.1).
/// </summary>
/// <param name="Dec">True when <see cref="Expr"/> is a STANDARD-DECIMAL intermediate (<c>CobolDec</c>-typed,
/// §8.8.1.5) — <see cref="Scale"/> is then meaningless (the SDIDI carries its own exponent).</param>
/// <param name="Real">True when <see cref="Expr"/> is a C#-<c>double</c>-typed FLOATING-POINT intermediate (D16 —
/// any expression with a float operand evaluates in IEEE binary64); <see cref="Scale"/> is 0 and unused. <c>Real</c>
/// and <see cref="Dec"/> are mutually exclusive.</param>
/// <param name="U">True when <see cref="Expr"/> is a <c>UInt128</c>-typed UNSIGNED-WIDE intermediate (kb/Work
/// R10): a 16-byte unsigned COMP-5 item's field read, or a folded literal beyond <see cref="Int128.MaxValue"/>
/// (the HIGHEST-ALGEBRAIC container bound, §15.43.4 r2). The VALUE paths (store, display, relation) keep the
/// full [0, 2^128) range via the runtime's <c>UInt128</c> overloads; the ARITHMETIC paths funnel through
/// <c>CobolNum.Widen</c> (loud beyond the documented Int128 intermediate — CONFORMANCE.md §4.2.16), never a
/// silent wrap. Mutually exclusive with <see cref="Dec"/> and <see cref="Real"/>.</param>
internal readonly record struct NumX(string Expr, int Scale, bool Dec = false, bool Real = false, bool U = false);

/// <summary>Small text utilities shared by every backend emitter: loud-failure guards, literal escaping, and the
/// numeric-literal → unscaled-<c>long</c> conversions.</summary>
internal static class EmitText
{
    /// <summary>A C# statement that fails loud at run time for an unsupported construct (COBOLNET_DESIGN §1.4).</summary>
    public static string LoudStmt(string feature) => $"NotImplemented.Run({CsLiteral(feature)});";

    /// <summary>A C# expression (typed <paramref name="csType"/>) that fails loud at run time when reached.</summary>
    public static string LoudValue(string csType, string feature) =>
        $"NotImplemented.Value<{csType}>({CsLiteral(feature)})";

    /// <summary>Render a .NET string as a safely-escaped C# string literal.</summary>
    public static string CsLiteral(string value) => SymbolDisplay.FormatLiteral(value, quote: true);

    /// <summary>The C# expression evaluating to file <paramref name="f"/>'s runtime connector key — the ONE way a
    /// connector is addressed at OPEN/READ/WRITE/CLOSE/registration/STATUS/USE-selection (feedback_one_mechanism_per_job).
    /// For a program, factory, or EXTERNAL file it is the qualified-name literal; for a per-object instance file
    /// (M2-OO-1i) it is the object's minted-key field <c>this.__fkey_X</c> (ISO §9.1.4 — one connector per object
    /// instance). Until an instance file sets <see cref="FileModel.InstanceKeyField"/> this is byte-identical to
    /// <c>CsLiteral(f.CobolName)</c>, so routing every connector-key site through this helper is behaviour-neutral
    /// for all existing files (the refactor is proven by the full battery, not a new golden).</summary>
    public static string FileKeyExpr(FileModel f) =>
        f.InstanceKeyField is { } fld ? $"this.{fld}" : CsLiteral(f.CobolName);

    // FigurativeFill lives in FigurativeConstants.Fill(kind, null) since P7 Step 4.

    /// <summary>The character value of a figurative <c>ALL literal-1</c> in a WIDTH-SPECIFIED context (a VALUE clause /
    /// a fixed-length receiver / a compared-with operand; ISO §8.3.3.6.4 GR2): the literal is repeated character by
    /// character until its length is ≥ <paramref name="width"/>, then truncated from the right to <paramref
    /// name="width"/> (or 1, whichever is greater). An empty literal yields spaces. (A length-UNSPECIFIED context —
    /// DISPLAY / STOP / STRING — uses the literal once per GR3c, NOT this.)</summary>
    public static string RepeatToWidth(string literal, int width)
    {
        int w = Math.Max(width, 1);
        if (literal.Length == 0) return new string(' ', w);
        var sb = new System.Text.StringBuilder(w + literal.Length);
        while (sb.Length < w) sb.Append(literal);
        return sb.ToString()[..w];
    }

    /// <summary>Thin forward to the one codec — see <see cref="CobolNet.Common.CobolLiteral.AllLiteralText"/>
    /// (delimiter-agnostic, ISO §8.3.1.2).</summary>
    public static string? AllLiteralText(string raw) => CobolNet.Common.CobolLiteral.AllLiteralText(raw);

    /// <summary>Render a numeric literal as a scaled integer: its digits as the unscaled value, its fractional
    /// digit count as the scale (e.g. <c>"3.5"</c> → <c>(35L, 1)</c>, <c>"-12"</c> → <c>(-12L, 0)</c>). A literal
    /// wider than 18 digits (legal to 31, ISO §8.3.1.2 — the 2002+ wide tier) emits an <c>Int128.Parse</c> since
    /// C# has no Int128 literal form.</summary>
    public static NumX UnscaledLit(string text)
    {
        string t = text.Trim().TrimStart('+');
        // A floating-point literal (ISO §8.3.3.3.3 — significand E exponent) is a floating-point operand: it
        // evaluates in IEEE binary64 (D16). The COBOL form (1.5E3, -2.5E-2) is itself a valid C# double literal,
        // so emit it directly as a Real NumX (never the scaled-integer parse below).
        if (t.IndexOf('E') >= 0 || t.IndexOf('e') >= 0)
            return new NumX(t, 0, Real: true);
        int dot = t.IndexOf('.');
        var (lit, u) = IntLiteralX(dot < 0 ? t : t.Remove(dot, 1));
        return new NumX(lit, dot < 0 ? 0 : t.Length - dot - 1, U: u);
    }

    /// <summary>The C# literal for an unscaled integer digit string: <c>…L</c> while it fits <see cref="long"/>
    /// (≤18 digits), else <c>Int128.Parse("…")</c>.</summary>
    public static string IntLiteral(string signedDigits) => IntLiteralX(signedDigits).Text;

    /// <summary>The carrier-aware form of <see cref="IntLiteral"/>. A SOURCE literal never exceeds 31 digits
    /// (ISO §8.3.1.2), but a COMPILER-SYNTHESIZED literal can: the HIGHEST-ALGEBRAIC fold of a 16-byte unsigned
    /// COMP-5 container is 2^128−1 (39 digits, §15.43.4 r2 — kb/Work R10 F73; the old unconditional
    /// <c>Int128.Parse</c> threw <c>OverflowException</c> at run time). A positive magnitude beyond
    /// <see cref="Int128.MaxValue"/> renders as <c>UInt128.Parse</c> and is flagged unsigned-wide (<c>U</c>) so
    /// the renderer routes it down the unsigned lane. (A NEGATIVE value never needs it — the most negative fold,
    /// −2^127, is exactly <see cref="Int128.MinValue"/>.)</summary>
    internal static (string Text, bool U) IntLiteralX(string signedDigits)
    {
        string mag = signedDigits.TrimStart('-').TrimStart('0');
        if (mag.Length <= 18) return ($"{signedDigits}L", false);
        if (signedDigits.StartsWith('-') || mag.Length < 39
            || System.Numerics.BigInteger.Parse(mag) <= (System.Numerics.BigInteger)Int128.MaxValue)
            return ($"Int128.Parse(\"{signedDigits}\")", false);
        return ($"UInt128.Parse(\"{signedDigits}\")", true);
    }

    /// <summary>Render a numeric literal as a C# <c>long</c> holding its UNSCALED value at <paramref name="scale"/>
    /// fractional digits — e.g. <c>"3.5"</c> at scale 2 → <c>350L</c>, <c>"12"</c> at scale 0 → <c>12L</c>. A NEGATIVE
    /// scale (a PICTURE-P trailing-scaled item, e.g. <c>99P(4) VALUE 990000</c> at scale −4 → <c>99L</c>) drops the
    /// low <c>|scale|</c> integer digits (the stored value is a multiple of 10^|scale|).</summary>
    public static string UnscaledAtScale(string raw, int scale)
    {
        string t = raw.Trim().TrimStart('+');
        bool neg = t.StartsWith('-');
        if (neg) t = t[1..];
        int dot = t.IndexOf('.');
        string intPart = dot < 0 ? t : t[..dot];
        string fracPart = dot < 0 ? "" : t[(dot + 1)..];
        string digits;
        if (scale >= 0)
        {
            if (fracPart.Length < scale) fracPart = fracPart.PadRight(scale, '0');
            else if (fracPart.Length > scale) fracPart = fracPart[..scale];
            digits = intPart + fracPart;
        }
        else
        {
            // Negative scale: the implied point sits |scale| positions right of the stored digits, so the low
            // |scale| integer digits are dropped (they are the assumed-zero P positions).
            string all = intPart + fracPart;
            int drop = -scale;
            digits = all.Length > drop ? all[..^drop] : "0";
        }
        digits = digits.TrimStart('0');
        return IntLiteral($"{(neg ? "-" : "")}{(digits.Length == 0 ? "0" : digits)}");
    }
}
