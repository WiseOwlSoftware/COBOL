// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
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
}

/// <summary>
/// A rendered numeric expression: a C# <c>long</c>-valued expression holding the UNSCALED value, plus its fractional
/// <see cref="Scale"/> (the implied decimal position). The pair lets the backend align scales (ISO §8.8.1).
/// </summary>
internal readonly record struct NumX(string Expr, int Scale);

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

    /// <summary>Render a numeric literal as a scaled <c>long</c>: its digits as the unscaled value, its fractional
    /// digit count as the scale (e.g. <c>"3.5"</c> → <c>(35L, 1)</c>, <c>"-12"</c> → <c>(-12L, 0)</c>).</summary>
    public static NumX UnscaledLit(string text)
    {
        string t = text.Trim().TrimStart('+');
        int dot = t.IndexOf('.');
        if (dot < 0) return new NumX($"{t}L", 0);
        int scale = t.Length - dot - 1;
        return new NumX($"{t.Remove(dot, 1)}L", scale);
    }

    /// <summary>Render a numeric literal as a C# <c>long</c> holding its UNSCALED value at <paramref name="scale"/>
    /// fractional digits — e.g. <c>"3.5"</c> at scale 2 → <c>350L</c>, <c>"12"</c> at scale 0 → <c>12L</c>.</summary>
    public static string UnscaledAtScale(string raw, int scale)
    {
        string t = raw.Trim().TrimStart('+');
        bool neg = t.StartsWith('-');
        if (neg) t = t[1..];
        int dot = t.IndexOf('.');
        string intPart = dot < 0 ? t : t[..dot];
        string fracPart = dot < 0 ? "" : t[(dot + 1)..];
        if (fracPart.Length < scale) fracPart = fracPart.PadRight(scale, '0');
        else if (fracPart.Length > scale) fracPart = fracPart[..scale];
        string digits = (intPart + fracPart).TrimStart('0');
        return $"{(neg ? "-" : "")}{(digits.Length == 0 ? "0" : digits)}L";
    }
}
