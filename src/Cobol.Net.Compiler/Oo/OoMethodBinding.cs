// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;

namespace CobolNet.Compiler.Oo;

/// <summary>
/// The AFTER-DATA-BIND half of a method's description (P9 R7 — phase-explicit): the resolved formal list,
/// RETURNING item, data roots, and (stamped later still, at class-BODY bind) the pc range. Attached to
/// <see cref="OoMethodSymbol.Binding"/> by <c>DataBinder.OoBindMethodData</c> once the method's data has
/// bound — reading it earlier is a null-deref (a TYPE-level ordering fact), never a silent <c>-1</c>
/// sentinel mis-read. (<c>OverrideOf</c> stays on the SYMBOL: it is a pass-1 identity fact —
/// <c>OoClassTable.Build</c> marks it and the CsName adoption depends on it before any data binds.)
/// </summary>
public sealed class OoMethodBinding
{
    /// <summary>The ordered PD USING formals (§14.9.23.4 GR3 — positional correspondence; every formal is
    /// BY REFERENCE, the header BY VALUE phrase being an unparsed grammar extension).</summary>
    public List<OoFormal> Formals { get; } = [];

    /// <summary>The PD RETURNING item (a LINKAGE 01/77 — §14.2.3 GR6: callee-allocated; the method's C# return
    /// value delivers it, §14.9.23.4 GR8), or null for a void method.</summary>
    public DataItem? Returning { get; set; }

    /// <summary>The method's LINKAGE roots (ALL of them — formals, the RETURNING item, and any unattached
    /// entry): each becomes a capturable C# LOCAL of the emitted method.</summary>
    public List<DataItem> LinkageRoots { get; } = [];

    /// <summary>LOCAL-STORAGE roots → C# locals, re-initialized on every activation (§14.5.3).</summary>
    public List<DataItem> LocalRoots { get; } = [];

    /// <summary>Method WORKING-STORAGE roots → STATIC fields (D3 — shared across instances, persistent across
    /// activations, §11.7; ILLEGAL at 2023, §13.5.3 SR 1 — the version-conformance pass window row).</summary>
    public List<DataItem> StaticRoots { get; } = [];

    /// <summary>The method's contiguous pc range in its class's one dispatch space — assigned by
    /// <c>StatementBinder.BindClassBody</c> (the exit-bounded range IS the fall-through guard: running past
    /// the last paragraph returns from the method, never into a sibling's paragraphs — the legacy trap #4).
    /// Entry &gt; End ⇔ an empty method body.</summary>
    public int EntryPc { get; set; } = -1;
    public int EndPc { get; set; } = -2;   // Entry > End ⇔ an empty method body
}
