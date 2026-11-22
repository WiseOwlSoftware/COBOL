// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding.Procedure;

/// <summary>The scope of ONE method body (ISO §2023:11.7 — the legacy per-method scope algorithm, ported). Its paragraph-names and
/// section-names declare METHOD-LOCALLY in the one store of names, keyed by this scope (<c>SymbolTable.DeclareParagraph</c>,
/// <c>DeclareSection</c>; kb/Work PB5932), and PERFORM/GO TO inside the method resolve against them only
/// (<c>SymbolTable.Identify</c> with the method as its <c>ProcedureSite</c>), so sibling methods may reuse names and a cross-method
/// transfer fails loud (the legacy traps #4/#5/#10 are structural, not checks).</summary>
internal sealed class OoMethodScope
{
    /// <summary>The method's DATA name scope (§2023:11.7.4 GR5) — activated on <c>ProcedureBindState.ActiveMethodScope</c>
    /// while this method's statements bind (slice 2).</summary>
    public OoMethodDataScope? Data;
    /// <summary>The METHOD-ID name — the §2023:15.30.3 RV2b.1 element name of a statement inside this method ("the name of
    /// the runtime element as specified in the FUNCTION-ID, METHOD-ID, or PROGRAM-ID paragraph of the function,
    /// method, or program containing the statement"; kb/Work PB63 — EcLocation read the CLASS-ID before).</summary>
    public string? MethodName;
    /// <summary>The method's PROCEDURE DIVISION USING formals — what §2023:8.8.4.8.3 SR1 ("a formal parameter defined
    /// in the source element in which this condition is specified") resolves against inside a method body
    /// (kb/Work PB757). Empty for a method with no USING phrase.</summary>
    public IReadOnlyList<OoFormal> Formals = [];
}
