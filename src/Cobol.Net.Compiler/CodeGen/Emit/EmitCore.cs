// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Runtime;
using Microsoft.CodeAnalysis.CSharp;

namespace CobolNet.CodeGen.Emit;

/// <summary>
/// The shared spine the decomposed backend emitters/renderers cooperate over (COBOLNET_DESIGN §17 §2.1): the
/// output <see cref="Writer"/>, the bound DATA DIVISION model (<see cref="Data"/>), and the mutable division working
/// scale (<see cref="TargetScale"/> — the receiver's scale, set before an arithmetic RHS is rendered so a quotient
/// is computed at the right precision). Emitters are stateless but for this context.
/// </summary>
internal sealed class EmissionContext(CodeWriter writer, DataBinder data)
{
    /// <summary>The C# output writer.</summary>
    public CodeWriter Writer { get; } = writer;

    /// <summary>The bound DATA DIVISION model.</summary>
    public DataBinder Data { get; } = data;

    /// <summary>The trailing weights argument for collated comparison renders — <c>", __COLLATE"</c> when a
    /// PROGRAM COLLATING SEQUENCE is active (ISO §12.3.6 GR11 — relation and condition-name comparisons), else
    /// empty (the native two-argument <c>CobolString.Compare</c> overload).</summary>
    public string CollateArg => Data.Collating is null ? "" : ", __COLLATE";

    /// <summary>The SPECIAL-NAMES editing-config suffix for generated <c>CobolEdit</c> calls (named arguments,
    /// composing after any <c>blankWhenZero:</c>): the program's currency PICTURE SYMBOL when not <c>$</c> and
    /// DECIMAL-POINT IS COMMA when set (ISO §12.3.7 GR13/GR14). Empty under the default config, so the
    /// generated code of an ordinary program is unchanged. The ONE producer of these arguments — used by the
    /// orchestrator's MOVE/arithmetic edited stores and the renderer's DeEdit.</summary>
    public string EditCfgArgs =>
        (Data.CurrencyPicSymbol != '$'
            ? $", currency: {SymbolDisplay.FormatLiteral(Data.CurrencyPicSymbol, quote: true)}" : "")
        + (Data.DecimalPointIsComma ? ", commaMode: true" : "");

    /// <summary>The C# char-literal a figurative constant fills with, PCS-AWARE for HIGH-/LOW-VALUE: under a
    /// program collating sequence they are the sequence's extremes (ISO §8.3.3.6 GR6/GR7 + §12.3.7 GR8/GR9 —
    /// character IDENTITY, not just comparison weight); otherwise the native U+00FF/U+0000 (COBOLNET_DESIGN
    /// §14.9). Other figuratives are sequence-independent.</summary>
    public string FigFill(char kind) => kind switch
    {
        'H' when Data.Collating is { } hc => SymbolDisplay.FormatLiteral(hc.HighValue, quote: true),
        'L' or 'N' when Data.Collating is { } lc => SymbolDisplay.FormatLiteral(lc.LowValue, quote: true),
        _ => EmitText.FigurativeFill(kind),
    };

    /// <summary>The figurative fill char for a receiver/anchor of the given data <paramref name="cat"/>: the
    /// ALPHANUMERIC program collating sequence governs HIGH-/LOW-VALUE ONLY for alphanumeric contexts. Category
    /// national and boolean use their OWN sequence (D-N3: HIGH/LOW-VALUE = U+00FF/U+0000 — the alphanumeric PCS
    /// never applies to national/boolean data, §8.3.3.6 GR6/GR7 over the NATIONAL sequence), so they take the
    /// PCS-independent <see cref="EmitText.FigurativeFill"/> pins.</summary>
    public string FigFill(char kind, Binding.Model.PicCategory? cat) =>
        cat is Binding.Model.PicCategory.National or Binding.Model.PicCategory.Boolean
            ? EmitText.FigurativeFill(kind)
            : FigFill(kind);

    /// <summary>The current division working scale (the receiving item's scale).</summary>
    public int TargetScale { get; set; }

    /// <summary>True when the current arithmetic receiver is a floating-point item (COMP-1/2/FLOAT-*, D16): the whole
    /// RHS then evaluates in IEEE binary64 even when every OPERAND is fixed-point (so <c>COMPUTE f = 10 / 3</c> holds
    /// 3.333…, not the fixed pipeline's scale-0 truncation to 3). Set alongside <see cref="TargetScale"/> before an
    /// arithmetic RHS is rendered, and RESET to false at the condition-render entry (a receiver-less numeric render
    /// must never inherit a stale float-receiver flag — the H1 staleness discipline).</summary>
    public bool TargetReal { get; set; }

    /// <summary>The current receiver's ROUNDED mode (ISO §14.7.4). A division computed at the receiver scale
    /// (<see cref="TargetScale"/>) rounds with this mode in one exact step (<c>RoundDiv</c> uses the true integer
    /// remainder); a division forced to a higher intermediate scale truncates and the receiver store rounds. Set
    /// before an arithmetic RHS is rendered; defaults to TRUNCATION (the no-ROUNDED behavior).</summary>
    public CobolRounding TargetRounding { get; set; } = CobolRounding.Truncation;

    /// <summary>True while emitting the evaluation of an arithmetic statement that carries an ON SIZE ERROR phrase
    /// (ISO §14.7.5). When set, a division renders the checked <c>CobolNum.DivideOrThrow</c> (which raises a
    /// <c>CobolSizeError</c> on a zero divisor, caught by the statement's <c>try</c>) instead of <c>CobolNum.Divide</c>
    /// — so a statement WITHOUT the phrase is byte-for-byte unchanged.</summary>
    public bool InSizeErrorContext { get; set; }
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
internal readonly record struct NumX(string Expr, int Scale, bool Dec = false, bool Real = false);

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
    /// connector is addressed at OPEN/READ/WRITE/CLOSE/registration/STATUS/USE-selection (feedback_singular_pattern).
    /// For a program, factory, or EXTERNAL file it is the qualified-name literal; for a per-object instance file
    /// (M2-OO-1i) it is the object's minted-key field <c>this.__fkey_X</c> (ISO §9.1.4 — one connector per object
    /// instance). Until an instance file sets <see cref="FileModel.InstanceKeyField"/> this is byte-identical to
    /// <c>CsLiteral(f.CobolName)</c>, so routing every connector-key site through this helper is behaviour-neutral
    /// for all existing files (the refactor is proven by the full battery, not a new golden).</summary>
    public static string FileKeyExpr(FileModel f) =>
        f.InstanceKeyField is { } fld ? $"this.{fld}" : CsLiteral(f.CobolName);

    /// <summary>The C# <c>char</c>-literal a figurative constant fills with (ISO §8.3.1.2; HIGH/LOW = U+00FF/U+0000
    /// per COBOLNET_DESIGN §14.9): Z→<c>'0'</c>, S→space, H→U+00FF, L/N→U+0000, Q→quote.</summary>
    public static string FigurativeFill(char kind) => kind switch
    {
        'Z' => "'0'",
        'S' => "' '",
        'H' => "'\\u00ff'",
        'L' or 'N' => "'\\u0000'",
        'Q' => "'\\\"'",
        _ => "' '",
    };

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
        if (dot < 0) return new NumX(IntLiteral(t), 0);
        int scale = t.Length - dot - 1;
        return new NumX(IntLiteral(t.Remove(dot, 1)), scale);
    }

    /// <summary>The C# literal for an unscaled integer digit string: <c>…L</c> while it fits <see cref="long"/>
    /// (≤18 digits), else <c>Int128.Parse("…")</c>.</summary>
    public static string IntLiteral(string signedDigits)
    {
        string mag = signedDigits.TrimStart('-').TrimStart('0');
        return mag.Length <= 18 ? $"{signedDigits}L"
            : $"Int128.Parse(\"{signedDigits}\")";
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
