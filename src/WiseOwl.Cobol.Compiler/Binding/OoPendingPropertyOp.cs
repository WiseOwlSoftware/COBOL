// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding;

/// <summary>One RESOLVED object-property reference awaiting its statement-level desugar (deep-dive
/// D-P2; ISO §2023:8.4.3.9.4): the synthesized temp the statement bound over, the accessors' invocation
/// <paramref name="Form"/> and receiver (an object-reference Place for the instance form; null for the
/// <c>prop OF Class-name</c> factory form and for SELF / SUPER, which invoke on the current object — §2023:8.4.3.8.4), the
/// accessor symbols found on the pinned-name roster (either may be null — SR3/SR4 checked against the CLASSIFIED
/// polarity, not eagerly), and the source names for diagnostics. Registered by ReferenceResolver at resolution time,
/// drained by StatementBinder.OoWrapPropertyOps after the carrying statement binds. <paramref name="InterfaceClrName"/>
/// is the emitted C# interface of an INTERFACE-typed receiver (kb/Work PB1449) — the accessors are then the
/// interface's prototypes, which have no owning class, and the interface carries their formals' statics exactly as
/// it does for an INVOKE through the same reference; null for a class receiver (each accessor's own class).
/// <para><paramref name="Prelude"/> is what EVALUATES identifier-3 when it is a computed identifier (kb/Work PB1425 —
/// an object-view, a function-identifier, a subscript that activates a function): the statement pre-ops its
/// binding registered, taken off the statement's pending list so they run immediately before THIS property's
/// accessor — §2023:8.4.3.1.4 GR1 applies identifier-3 (a)–c)) before the property (d)), and the property's GET is
/// hoisted ahead of every pre-op of the statement (<c>OoWrapPropertyOps</c>), so left on that list they would run
/// after the accessor that reads their result.</para></summary>
internal sealed record OoPendingPropertyOp(
    DataItem Temp, Bound.InvokeForm Form, Place? Receiver, string ClassClrName,
    OoMethodSymbol? Get, OoMethodSymbol? Set, string PropName, string ReceiverName,
    IReadOnlyList<Bound.BoundStatement> Prelude, string? InterfaceClrName = null)
{
    /// <summary>The emitted type that qualifies <paramref name="accessor"/>'s formal statics at the call site.</summary>
    public string? OwnerClrNameOf(OoMethodSymbol accessor) => InterfaceClrName ?? accessor.Owner?.ClrName;

    /// <summary>True once the binder that built the statement's receiver list CLAIMED this property as one of its
    /// receivers (<see cref="ProcedureBindState.ClaimInterleavedReceiver"/>): that statement's emitter accesses each
    /// receiver in turn, so the accessors go around THIS receiver (<c>BoundReceiverBrackets</c>, kb/Work PB2078)
    /// instead of around the whole statement.</summary>
    public bool Interleaved { get; set; }

    /// <summary>Identifier-3 when a RECEIVING use of this property evaluates it into the identified-object
    /// temporary, so that the GET and the SET reach the one object identified (§2023:14.6.4; §2023:8.4.3.9.4 GR3; kb/Work
    /// PB2078): identifier-3 written as an object-reference data item and invoked on as an instance. Null when the
    /// accessors invoke on identifier-3 as it stands: a FACTORY OF or class receiver, or a computed object, whose
    /// evaluation is the <see cref="Prelude"/>.</summary>
    public Place? IdentifiedReceiver =>
        Form == Bound.InvokeForm.Instance && Receiver is { Item: { IsCompilerTemp: false, Pic.ObjectRef: not null } }
            ? Receiver : null;

    /// <summary>Whether a use of the property whose store polarity is <paramref name="kind"/> invokes the GET before
    /// the statement stores it (§2023:8.4.3.9.4 GR1 for a sending use and GR3 for a read-modify-write one; for a part store,
    /// §2023:14.9.43.4 GR7 and §2023:8.4.3.3.4 GR5, the GET supplies the kept positions where the class has one, kb/Work PB2618).</summary>
    public bool GetsBefore(Bound.StoreKind kind) =>
        kind is Bound.StoreKind.None or Bound.StoreKind.ReadWrite || (kind == Bound.StoreKind.WriteInPart && Get is not null);
}
