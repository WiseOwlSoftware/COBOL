// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Editions;

/// <summary>
/// The report channel <see cref="ConstructRegistry.Check"/> writes to — layer-neutral so BOTH
/// <c>Cobol.Net.Frontend</c> (the preprocessor / parse-layer error strategy) and <c>Cobol.Net.Compiler</c>
/// (the binder/validator, via the <c>EditionContext</c> adapter) can receive the SAME structured edition
/// diagnostic. This ends the duplicate strict/permissive severity logic each layer used to carry.
/// </summary>
public interface IDiagnosticSink
{
    /// <summary>Record one edition diagnostic. Passed <c>in</c> (a readonly-struct value; no copy).</summary>
    void Report(in EditionDiagnostic diagnostic);
}
