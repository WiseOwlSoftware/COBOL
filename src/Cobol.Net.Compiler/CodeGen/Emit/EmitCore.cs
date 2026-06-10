// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
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

    /// <summary>The current division working scale (the receiving item's scale).</summary>
    public int TargetScale { get; set; }

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
internal readonly record struct NumX(string Expr, int Scale, bool Dec = false);

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

    /// <summary>Decode a COBOL <c>STRINGLIT</c> (<c>"…"</c> with doubled <c>""</c>) to its character value.</summary>
    public static string DecodeCobolString(string raw) =>
        raw.Length >= 2 && raw[0] == '"' && raw[^1] == '"' ? raw[1..^1].Replace("\"\"", "\"") : raw;

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

    /// <summary>If <paramref name="raw"/> is the figurative <c>ALL "literal"</c> form (a VALUE / level-88 operand text),
    /// the decoded literal; otherwise <see langword="null"/>. Tolerant of whether the front-end preserved the space
    /// between <c>ALL</c> and the literal. Only the quoted-literal form matches — <c>ALL ZEROS</c> (a figurative word)
    /// returns <see langword="null"/> and is handled by the figurative-word path.</summary>
    public static string? AllLiteralText(string raw)
    {
        string t = raw.TrimStart();
        if (t.Length < 3 || !t.StartsWith("ALL", StringComparison.OrdinalIgnoreCase)) return null;
        string rest = t[3..].TrimStart();
        return rest.Length >= 2 && rest[0] == '"' && rest[^1] == '"' ? DecodeCobolString(rest) : null;
    }

    /// <summary>Render a numeric literal as a scaled integer: its digits as the unscaled value, its fractional
    /// digit count as the scale (e.g. <c>"3.5"</c> → <c>(35L, 1)</c>, <c>"-12"</c> → <c>(-12L, 0)</c>). A literal
    /// wider than 18 digits (legal to 31, ISO §8.3.1.2 — the 2002+ wide tier) emits an <c>Int128.Parse</c> since
    /// C# has no Int128 literal form.</summary>
    public static NumX UnscaledLit(string text)
    {
        string t = text.Trim().TrimStart('+');
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
