// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Generated;

namespace WiseOwl.Cobol.Frontend.Cst;

using Core = CobolParserCore;

/// <summary>The one syntactic reading of a <c>setToStatement</c> (kb/Work PB4231): the grammar parses every
/// <c>SET receivers TO sender</c> of ISO §14.9.39.2 Formats 1, 5, 7, 8, 9, 14, 16 and 17 as ONE rule, so the readers
/// that used to tell Format 7 from the rest by WHICH rule matched (the binder's dispatch, the COBOL-2002 recognition gate,
/// the FLAG-02 e index-set flag) ask this instead.</summary>
public static class SetToStatementCst
{
    /// <summary>The statement spells Format 7's ADDRESS phrase: an <c>ADDRESS OF data-name-1</c> receiver (Format 7's
    /// printed brace <c>{ ADDRESS OF data-name-1 | identifier-5 }</c>) or a §8.4.3.11 data-address-identifier sender.
    /// No other format prints either, so such a statement IS Format 7 (data-pointer-assignment) by its spelling, before
    /// any operand is resolved.</summary>
    public static bool SpellsAddressPhrase(this Core.SetToStatementContext st) =>
        st.setToSender().dataAddressIdentifier() is not null
        || Array.Exists(st.setToReceiver(), r => r.ADDRESS() is not null);
}
