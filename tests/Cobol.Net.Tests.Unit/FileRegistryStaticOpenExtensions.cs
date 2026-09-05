// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;

namespace CobolNet.Tests.Unit;

/// <summary>
/// OPEN shorthands for the runtime file tests whose subject file is described by ONE notional runtime element
/// with a STATIC ASSIGN specification and NO LINAGE clause.
/// <para>The real entry points (<see cref="FileRegistry.Open"/> / <see cref="FileRegistry.OpenNoRewind"/>) require
/// the executing element's own operands — its ASSIGN specification and its LINAGE operand values — with no
/// defaults, because ISO §12.4.5.3 GR3 a)/b) and §13.18.34 GR6 b) name the element that EXECUTES the statement and
/// a file connector may be shared by several of them (kb/Work PB673). A test that describes the file once and
/// writes no USING phrase and no LINAGE clause supplies: the connector's own registered association (re-associating
/// with the same specification is a no-op — §12.4.5.3 GR3 a's value is a source-text constant), <c>false</c> for
/// the USING phrase, and <c>null</c> for the page model.</para>
/// <para>⛔ Deliberately NOT named <c>Open</c>: an overload that differs only by arity would let a REAL emitter or
/// runtime path silently lose the operands, which is the defect this signature exists to make impossible. A test
/// that exercises ASSIGN … USING or LINAGE calls the full entry point and states its operands.</para>
/// </summary>
internal static class FileRegistryStaticOpenExtensions
{
    /// <summary>OPEN with the connector's own static association and no LINAGE page.</summary>
    public static void OpenStatic(this FileRegistry reg, string name, FileOpenMode mode) =>
        reg.Open(name, mode, reg.HostPathOf(name), assignDynamic: false, page: null);

    /// <summary>OPEN … WITH NO REWIND with the connector's own static association and no LINAGE page.</summary>
    public static void OpenNoRewindStatic(this FileRegistry reg, string name, FileOpenMode mode) =>
        reg.OpenNoRewind(name, mode, reg.HostPathOf(name), assignDynamic: false, page: null);
}
