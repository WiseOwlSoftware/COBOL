// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Model;

using Core = CobolParserCore;

/// <summary>One program unit of the compilation group: its identity, containment, PROGRAM-ID attributes
/// (ISO §11.10 / §8.6.6), bound model, and the GLOBAL bridges its class must emit (§13.18.27 GR2).
/// <para>Relocated from the emitter's private <c>CallUnit</c> (rearch PHASE-06 Step 2 — the BINDER owns the bound
/// model; the emitter consumes it read-only through <see cref="BoundCompilation"/>).</para></summary>
internal sealed class BoundUnit
{
    /// <summary>program-name-1 / user-function-name-1 as WRITTEN — the declared COBOL name. This is what a
    /// reference by a user-defined WORD resolves against (§10.7.3 SR2's END PROGRAM match, §14.9.4.3 SR15's
    /// AS NESTED literal, <c>FUNCTION user-function-name</c>) and what FUNCTION MODULE-NAME reports
    /// (CONFORMANCE.md DOC-A.1-135, the §15.65.4 r4 determination). NEVER the AS literal.</summary>
    public required string Name;
    /// <summary>The name externalized to the operating environment: the <c>AS</c> phrase's literal-1 when the
    /// identification division specifies one, else <see cref="Name"/> (ISO §11.10.4 GR1 "Literal-1, if
    /// specified, is the name of the program that is externalized to the operating environment"; §11.5.4 GR1/GR2
    /// for a function; §8.3.2.2 2) is the rule that makes the two distinct). This is what a reference by a
    /// LITERAL resolves against, and §8.3.2.2 makes that exhaustive: "Externalized names shall be referenced
    /// in a source element only: 1) in the AS phrase in a repository paragraph entry, 2) in the AS phrase in
    /// an EXTERNAL clause, 3) as program-name in a CALL statement, 4) as program-name in a CANCEL statement,
    /// 5) as program-name in a program-address-identifier, 6) as method-name in an INVOKE statement or inline
    /// method invocation.  All other references to names for which externalization is permitted shall be
    /// specified using the user-defined words, as opposed to the externalized names." It is also the key of
    /// §12.3.8.4 GR10 a)'s program-definition search. kb/Work PB303.</summary>
    public required string ExternalizedName;
    public required string ClassName;
    public required Core.ProgramUnitContext Ctx;
    public BoundUnit? Parent;
    public List<BoundUnit> Children = [];
    public bool Initial, Common, Recursive;
    /// <summary>True for a FUNCTION-ID unit (ISO §9.4 — a user-defined function; program-shaped except it
    /// RETURNs a value and always possesses the recursive attribute).</summary>
    public bool IsFunction;
    /// <summary>True for a FUNCTION-ID … IS PROTOTYPE unit (ISO §11.5 Format 2 / §10.6.2 SR4 — a
    /// signature-only unit: LINKAGE-only data + a header-only procedure division). It contributes its
    /// signature to the user-function table (M2-UDF-3) but emits NO body and does NOT register in the run
    /// unit — the separately-compiled definition (in-group per GR11a, else a sibling assembly) is the
    /// activation target. Always implies <see cref="IsFunction"/>.</summary>
    public bool IsPrototype;
    public DataBinder Data = null!;
    public ReferenceResolver Refs = null!;
    public BoundProgram Bound = null!;
    public List<CallBridge> Bridges = [];

    /// <summary>The run-unit-unique containment path id (registry key; §8.4.6.3 scoping).</summary>
    public string Path => Parent is null ? Name : Parent.Path + "/" + Name;

    /// <summary>The C# nested-type reference from the top-level scope (factory construction).</summary>
    public string ClassRef => Parent is null ? ClassName : Parent.ClassRef + "." + ClassName;
}

/// <summary>One inherited-GLOBAL bridge a nested class emits: a <c>ref</c>-returning property aliasing the
/// containing instance's field (ISO §13.18.27 GR2 — the name is visible in every contained program; the
/// STORAGE stays the container's). <paramref name="Kind"/>: "field" (a global root's typed field), "backing"
/// (a Tier-B class's string backing), or "index" (an INDEXED BY <c>long</c> field of a global table).</summary>
internal sealed record CallBridge(string Field, string Path, string Kind, DataItem? Item);
