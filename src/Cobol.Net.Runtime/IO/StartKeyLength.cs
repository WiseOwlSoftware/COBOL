// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>
/// The GR17 b) length of an indexed START's temporary key area, carried to the connector AS THE SOURCE WROTE IT —
/// arithmetic-expression-1's exact value, not an emit-time narrowing of it (kb/Work PB357).
/// <para>⛔ §14.9.41.4 GR14 IS A PREDICATE ON arithmetic-expression-1 ITSELF: "If arithmetic-expression-1 does not
/// evaluate to a positive nonzero integer that is less than or equal to the length of the associated key, the I-O
/// status value in the file connector referenced by file-name-1 is set to '23', the invalid key condition exists,
/// and the execution of the START statement is unsuccessful." §14.9.41.3 has no syntax rule confining the
/// expression to an integer, so <c>WITH LENGTH 2.5</c> and a 12-digit count are LEGAL source whose defined result
/// is '23'. The emitter formerly rendered <c>(int)(Align(expr, 0))</c> — a TRUNCATION to scale 0 and an unchecked
/// 32-bit narrowing — so 2.5 arrived as 2, 4294967297 as 1, and the connector's bound test was applied to a value
/// already forced into range. The value now travels intact: the integrality question is answered by THE ONE
/// carrier-neutral integrality landing, <see cref="SetAmount"/>, and the range question by
/// <see cref="TryCompareLength"/>, which <see cref="IndexedConnector.Start"/> asks exactly once.</para>
/// <para>§14.9.41.4 GR13 makes the count a number of CHARACTER POSITIONS of data-name-1's class ("if data-name-1
/// or record-key-name-1 is of class national, arithmetic-expression-1 is the number of national character
/// positions"); <see cref="UnitWidth"/> is the storage width of one such position (kb/Work PB327), applied only
/// AFTER the GR14 test, on the exact integer, so the product can never wrap.</para>
/// </summary>
public readonly record struct StartKeyLength
{
    private readonly SetAmountLanding _landing;
    private readonly Int128 _whole;

    /// <summary>The storage width of one counted character position (1, or the national width); never below 1.</summary>
    public int UnitWidth { get; }

    private StartKeyLength(SetAmountLanding landing, Int128 whole, int unitWidth)
    {
        _landing = landing;
        _whole = whole;
        UnitWidth = Math.Max(1, unitWidth);
    }

    /// <summary>arithmetic-expression-1 on the exact fixed-point lane: <paramref name="scaled"/> is the value times
    /// 10^<paramref name="scale"/>, so any fraction is still present for the integrality test.</summary>
    public static StartKeyLength OfScaled(Int128 scaled, int scale, int unitWidth) =>
        new(SetAmount.Land(scaled, scale, out Int128 whole), whole, unitWidth);

    /// <summary>arithmetic-expression-1 on the native-float lane; the integrality test runs on the double itself
    /// (a NaN or an infinity is not an integer).</summary>
    public static StartKeyLength OfReal(double value, int unitWidth) =>
        new(SetAmount.Land(value, out Int128 whole), whole, unitWidth);

    /// <summary>No LENGTH phrase: GR17 b)'s "or else the length of data-name-1", already a storage width.</summary>
    public static StartKeyLength OfWidth(int storageWidth) => new(SetAmountLanding.Integer, storageWidth, 1);

    /// <summary>§14.9.41.4 GR14 — true, with the storage width of the temporary area, when the length is a positive
    /// nonzero integer no greater than the associated key's length (<paramref name="keyWidth"/>, a storage width);
    /// false is GR14's '23' leg.</summary>
    public bool TryCompareLength(int keyWidth, out int compareLength)
    {
        compareLength = 0;
        int unit = Math.Max(1, UnitWidth);   // a default(StartKeyLength) still answers, never divides by zero
        // whole × unit ≤ keyWidth ⇔ whole ≤ ⌊keyWidth / unit⌋ — tested by division so no product is ever formed
        // on a value that has not yet been bounded.
        if (_landing != SetAmountLanding.Integer || _whole < 1 || _whole > keyWidth / unit) return false;
        compareLength = (int)_whole * unit;
        return true;
    }
}
