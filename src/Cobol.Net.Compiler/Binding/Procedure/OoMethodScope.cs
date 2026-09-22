// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding.Procedure;

/// <summary>The paragraph scope of ONE method body (ISO §11.7 — the legacy per-method scope algorithm,
/// ported): paragraph and section names declare METHOD-LOCALLY; PERFORM/GO TO inside the method resolve
/// against THESE maps only, so sibling methods may reuse names and a cross-method transfer fails loud
/// (the legacy traps #4/#5/#10 are structural here, not checks).</summary>
internal sealed class OoMethodScope
{
    // ⛔ ProcedureNameMap, not Dictionary: a method's paragraph/section scope owes the SAME §8.4.2.2.1
    // uniqueness answer the program-level scope owes, one level down — the method scope had the identical
    // TryAdd hole (kb/Work PB466).
    public readonly ProcedureNameMap<int> Paras = new();
    public readonly ProcedureNameMap<SectionInfo> Sections = new();
    /// <summary>The method's DATA name scope (§11.7 GR5) — activated on <c>DataBinder.ActiveMethodScope</c>
    /// while this method's statements bind (slice 2).</summary>
    public OoMethodDataScope? Data;
    /// <summary>The METHOD-ID name — the §15.30.3 r2b1 element name of a statement inside this method ("the name of
    /// the runtime element as specified in the FUNCTION-ID, METHOD-ID, or PROGRAM-ID paragraph of the function,
    /// method, or program containing the statement"; kb/Work PB63 — EcLocation read the CLASS-ID before).</summary>
    public string? MethodName;
    /// <summary>The method's PROCEDURE DIVISION USING formals — what §8.8.4.8.3 SR1 ("a formal parameter defined
    /// in the source element in which this condition is specified") resolves against inside a method body
    /// (kb/Work PB757). Empty for a method with no USING phrase.</summary>
    public IReadOnlyList<CobolNet.Compiler.Oo.OoFormal> Formals = [];
}
