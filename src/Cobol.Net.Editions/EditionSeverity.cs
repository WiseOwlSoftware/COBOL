// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Editions;

/// <summary>
/// The severity of an <see cref="EditionDiagnostic"/> — the two outcomes the ONE
/// <see cref="EditionSeverityPolicy"/> produces. <see cref="Error"/> fails the compile; <see cref="Warning"/>
/// is the non-failing channel (obsolete/archaic flags; removed constructs under <c>--permissive</c>). The
/// frontend's own <c>DiagnosticSeverity</c> (Info/Warning/Error) is a distinct, richer enum; the edition path
/// only ever produces these two.
/// </summary>
public enum EditionSeverity
{
    /// <summary>Fails the compilation (introduction gating on both axes; removed constructs under strict).</summary>
    Error,

    /// <summary>Never fails the compilation (obsolete/archaic 0903 flags; removed constructs under permissive).</summary>
    Warning,
}
