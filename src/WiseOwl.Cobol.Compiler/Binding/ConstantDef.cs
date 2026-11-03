// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Binding;

/// <summary>One folded compile-time constant (ISO §13.10). <paramref name="Text"/> is the substitution
/// value: for class numeric the LITERAL AS WRITTEN — literal-1 with its own sign and decimal separator
/// (§13.10.4 GR1, "as if literal-1 … were written where constant-name-1 is written"), or the GR3 integer
/// literal of an expression / LENGTH OF form; for the string classes the DECODED character value.
/// <para>⛔ A NUMERIC <paramref name="Text"/> IS SOURCE TEXT, NEVER A NORMALIZED VALUE (kb/Work PB1230). A
/// consumer that re-binds it takes it through its literal chokepoint in the active DECIMAL-POINT mode, exactly
/// as the written literal would go — which is why <c>AS 1,5</c> under DECIMAL-POINT IS COMMA substitutes
/// cleanly and <c>AS +5</c> displays '+5'. It used to hold the evaluator's normalized text ('1.5', '5'), and
/// every reference re-checked that as a literal: '1.5' was refused under the comma mode.</para>
/// <paramref name="IntegerText"/> is the constant's value as a canonical integer (no '+', no separator) when the
/// constant IS an integer (§13.10.3 SR2 — an integer literal-1, or GR3–GR6), else null: the form the integer
/// positions read (an OCCURS bound, a PICTURE repetition, a subscript). <paramref name="RawText"/> is the
/// equivalent literal as RAW source text (re-quoted per class) — the currency of the text-plumbed paths (the
/// DATA-division VALUE capture), mirroring <c>ConcatFolder.Folded.RawText</c>. <paramref name="Specification"/>
/// is the AS operand as written, which §13.10.3 SR9 compares when the name is duplicated.
/// <para><paramref name="WrittenLiteral"/> is literal-1 AS WRITTEN when it is ONE nonnumeric literal token (an
/// alphanumeric, hexadecimal, national or boolean literal) — written in the entry, or reached through a
/// constant-name the entry is written AS (it inherits that entry's), or the literal a FROM compilation variable
/// was defined as (§13.10.4 GR1, "as if literal-1 or the text represented by compilation-variable-name-1 were
/// written") — and null for every other operand (a concatenation, an arithmetic expression, a length phrase). It
/// is the subject of every rule that asks literal-1's FORM, which <paramref name="Text"/> (decoded) and
/// <paramref name="RawText"/> (re-quoted: X'2A' is "*") no longer carry: OPTIONS INITIALIZE's one-byte
/// hexadecimal-alphanumeric fill (§11.9.10.3 SR1) and CURRENCY SIGN's no-hexadecimal operands (§12.3.7.3 SR26).
/// ⛔ It is NOT <paramref name="Specification"/> (kb/Work PB780 review R1-1): <c>AS KX</c> SPECIFIES the word KX —
/// what SR9 compares — and WRITES KX's literal-1 — what the form rules ask. One field serving both made
/// <c>INITIALIZE … TO KY</c> (KY AS KX AS X'2A') ask the form of the word "KX" and refuse it, and
/// <c>PICTURE SYMBOL KY</c> (KY AS KX AS X'23') miss the hexadecimal literal.</para></summary>
public sealed record ConstantDef(
    string Name, PicCategory Category, string Text, string? IntegerText, bool IsGlobal, string RawText,
    string Specification, string? WrittenLiteral)
{
    /// <summary>What a diagnostic names as the literal the constant stands for: literal-1 as written when it is one
    /// literal token, else the entry's specification.</summary>
    public string StandsFor => WrittenLiteral ?? Specification;
}
