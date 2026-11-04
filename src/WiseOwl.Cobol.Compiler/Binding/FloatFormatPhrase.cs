// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding;

using Core = CobolParserCore;

/// <summary>The token → enum mapping for the two float FORMAT phrases of ISO §13.18.60.2 ("where encoding-phrase
/// is" / "where endianness-phrase is"). ONE definition for every clause that reads them — the OPTIONS FLOAT-BINARY
/// clause (§11.9.8, endianness) and the USAGE clause's per-item phrases (§13.18.60.2, both). The grammar shares one
/// <c>encodingPhrase</c>/<c>endiannessPhrase</c> rule pair across those and the OPTIONS FLOAT-DECIMAL clause
/// (§11.9.9), whose phrases nothing reads because the clause is declined (COBOL2424); this is the rule pair's
/// binder-side twin, so a further citing site inherits the mapping instead of copying it (kb/Work PB174 —
/// before this the mapping lived privately in <c>OptionsBinder</c> and the USAGE arm would have been copy #2). It reads the parse
/// tree, so it lives with the binders that read those clauses, never beside the <see cref="OptionsModel"/> it fills (kb/Work
/// PB2401: the model names no parse context).</summary>
internal static class FloatFormatPhrase
{
    /// <summary>§13.18.60.4 GR19a/b: HIGH-ORDER-LEFT is big-endian, HIGH-ORDER-RIGHT is little-endian.</summary>
    internal static FloatEndianness Endianness(Core.EndiannessPhraseContext e) =>
        e.HIGH_ORDER_LEFT() is not null ? FloatEndianness.HighOrderLeft : FloatEndianness.HighOrderRight;

    /// <summary>§13.18.60.4 GR20a/b: BINARY-ENCODING / DECIMAL-ENCODING select the ISO/IEC 60559:2020 3.5 / 3.5.2
    /// encoding of a standard decimal floating-point item.</summary>
    internal static FloatEncoding Encoding(Core.EncodingPhraseContext e) =>
        e.BINARY_ENCODING() is not null ? FloatEncoding.BinaryEncoding : FloatEncoding.DecimalEncoding;
}
