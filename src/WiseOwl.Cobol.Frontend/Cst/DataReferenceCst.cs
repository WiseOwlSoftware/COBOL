// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Generated;

namespace WiseOwl.Cobol.Frontend.Cst;

using Core = CobolParserCore;

/// <summary>The special-register kind a <c>dataReference</c> names, or <see cref="None"/> for an ordinary
/// (optionally qualified/subscripted) data-name reference. These registers are runtime-sourced, never a storage
/// place (ISO §2023:8.4.3.14/§2023:8.4.3.15).</summary>
public enum SpecialRegister { None, LinageCounter, LineCounter, PageCounter }

/// <summary>
/// The rule-bearing read of a <see cref="Core.DataReferenceContext"/>, as an EXTENSION MEMBER of the generated context:
/// which special register the reference names. It computes no semantic state (that belongs to the binder), and a
/// grammar-rule rename breaks THIS file (a compile error). No wrapper: the binder reads the generated contexts through
/// <c>frontend.parse</c> (DESIGN-architecture-review §8.1), so the struct this once was, its implicit conversion and its
/// unread <c>BaseName</c> are deleted (kb/Work PB2731).
/// <para>The suffixes (qualification, the subscript list <see cref="Core.SubscriptPartContext"/> and the reference
/// modifier <see cref="Core.RefModPartContext"/>) are parse nodes since D10 removed the SUBSCRIPT lexer mode (kb/Work
/// PB2113), and the binder's <c>ReferenceResolver.ReadWritten</c> reads them off the context as the reference AS WRITTEN.
/// The former "has no suffix" test was deleted (kb/Work PB2193): <c>dataReferenceSuffix</c> carries qualification as well
/// as subscripts, so that test could not tell a legal qualified reference from a subscripted one (kb/Work PB457).</para>
/// </summary>
public static class DataReferenceCst
{
    extension(Core.DataReferenceContext ctx)
    {
        /// <summary>The special-register kind this reference names, else <see cref="SpecialRegister.None"/>.</summary>
        public SpecialRegister Register =>
              ctx.LINAGE_COUNTER() is not null ? SpecialRegister.LinageCounter
            : ctx.LINE_COUNTER()   is not null ? SpecialRegister.LineCounter
            : ctx.PAGE_COUNTER()   is not null ? SpecialRegister.PageCounter
            : SpecialRegister.None;
    }
}
