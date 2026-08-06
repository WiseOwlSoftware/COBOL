// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

using CobolNet.Binding.Model;

using CobolNet.Compiler.Oo;

namespace CobolNet.Binding.Bound;

using Core = CobolParserCore;

/// <summary>An INVOKE's resolved call form (OO deep-dive D1/D5 — the binder chooses; both backends render).</summary>
public enum InvokeForm
{
    /// <summary><c>INVOKE Class "NEW" RETURNING obj</c> → <c>obj = new Class()</c> (§16.2.1 — the predefined
    /// NEW factory; the generated public ctor chains base then VALUE-initializes, deep-dive D4).</summary>
    New,
    /// <summary><c>INVOKE obj "M" …</c> → <c>RequireNonNull(obj).M(…)</c> — virtual dispatch on the runtime
    /// class (§9.3.6) behind the §14.9.23.4 GR5 null guard.</summary>
    Instance,
    /// <summary><c>INVOKE SELF "M" …</c> → <c>this.M(…)</c> — VIRTUAL dispatch on the RUNTIME class
    /// (§8.4.3.8 GR2: a subclass override wins even when the SELF call sits in an inherited base method —
    /// oo_self_polymorphic). Never null — no guard (slice 3b).</summary>
    Self,
    /// <summary><c>INVOKE SUPER "M" …</c> → <c>base.M(…)</c> — NON-virtual, resolution STARTS at the base
    /// class (§8.4.3.8 GR3 restricted search), so an override calling its base cannot recurse (slice 3b).</summary>
    Super,
    /// <summary><c>INVOKE class-name "M" …</c> (non-NEW) → <c>CLS__FACTORY.__Instance.M(…)</c> — a FACTORY
    /// method through the class's factory singleton (§11.4/§9.3.6; brief D11 — never null, no guard;
    /// virtual, so an inherited factory override dispatches).</summary>
    Factory,
    /// <summary><c>INVOKE SELF|SUPER "NEW" RETURNING r</c> inside a FACTORY method → <c>r = this.__New()</c>
    /// (§16.2.1 GR1 ACTIVE-CLASS creation: the covariant per-class <c>__New</c> override makes an inherited
    /// factory MAKE create the RUNTIME factory's class — the canonical factory pattern).</summary>
    NewSelf,
}

/// <summary>A bound INVOKE (ISO §14.9.23; deep-dive D5): the RESOLVED call form plus everything the backend
/// needs to render it — no name lookup happens at emit time. <paramref name="ClassCsName"/> is the emitted C#
/// type (New form); <paramref name="Receiver"/>/<paramref name="MethodCsName"/> drive the Instance form (the
/// method name is the ROSTER's exact spelling — COBOL names compare case-insensitively, §8.3.2.2, and the
/// C# override chain must reuse one spelling — the legacy trap-#2 rule); <paramref name="Returning"/> receives
/// the invocation result (NEW's created object, or the method's <paramref name="ReturningSource"/> item per
/// §14.9.23.4 GR8); <paramref name="Args"/> carries the positionally-bound USING arguments (D6 — GR3).</summary>
public sealed record BoundInvoke(
    InvokeForm Form, string? ClassCsName, Place? Receiver, string? MethodCsName, Place? Returning,
    IReadOnlyList<BoundInvokeArg>? Args = null, DataItem? ReturningSource = null,
    string? OwnerCsName = null)
    : BoundStatement;

/// <summary>One bound INVOKE argument (deep-dive D6; §14.9.23.4 GR6): the FORMAL it corresponds to
/// positionally (its description drives the marshaling — §14.8.2's strict conformance was validated at bind,
/// so the crossing is type-preserving), the identifier source place OR the literal (decoded string / raw
/// numeric text), and whether the argument writes back (BY REFERENCE identifier — changes visible to the
/// caller; BY CONTENT and the §14.9.23.3 SR 10 object-data auto-CONTENT case do not).</summary>
public sealed record BoundInvokeArg(
    DataItem Formal, Place? Source, string? NumericLiteral, string? StringLiteral, bool WriteBack,
    bool ByContent = false)
{
    /// <summary>A BY CONTENT <b>arithmetic-expression-1</b> argument (ISO §14.9.23.2; fix-queue PB46) — the
    /// operand shape the format admits that is neither an identifier nor a literal, so it has no
    /// <see cref="Source"/> and no literal text.</summary>
    /// <remarks>
    /// It is BY CONTENT by construction: §14.9.23.3 SR9 confines BY REFERENCE to an identifier, and an
    /// expression has no storage to write back to — <see cref="WriteBack"/> is always false for it.
    /// </remarks>
    public BoundExpr? ContentExpr { get; init; }
}

/// <summary>A bound UNIVERSAL-receiver INVOKE (deep-dive D10/D-U5): there is NO formal roster at compile
/// time, so the bound facts differ in KIND from <see cref="BoundInvoke"/> — the method selector is a
/// bind-normalized literal OR a data-item Place read at runtime (§14.9.23.3 SR7), and every argument
/// carries its caller-side CONFORMANCE DESCRIPTOR (OoClassTable.ConformanceDescriptor — checked by the
/// callee's generated switch at runtime per §14.9.23.4 GR7c, mismatch → EC-OO-UNIVERSAL). Every argument
/// is BY REFERENCE (SR6 — implicit), so every argument writes back through its box.</summary>
public sealed record BoundInvokeUniversal(
    Place Receiver, string? MethodLiteral, Place? MethodSource,
    IReadOnlyList<BoundUniversalArg> Args, Place? Returning, string? ReturningDescriptor) : BoundStatement;

/// <summary>One universal-dispatch argument: the storage and its conformance descriptor (D-U3).</summary>
public sealed record BoundUniversalArg(Place Source, string Descriptor);

/// <summary>SET Format 5 — object-reference assignment (ISO §14.9.39 :31162; D-U7): copy ONE sender
/// reference into each target in order (GR9/GR10). The sender is a Place, the NULL figurative, SELF
/// (legal only inside a method; renders <c>this</c>), a class-name (SR13 — renders the D11 factory
/// singleton <c>{SourceFactoryCs}.__Instance</c>), or the EXCEPTION-OBJECT register (§8.4.3.6 — the
/// EC-OO wave; implicitly UNIVERSAL, so a TYPED target takes the generated runtime narrow check,
/// §9.3.8.2 :12291 → EC-OO-UNIVERSAL).</summary>
public sealed record BoundSetObjectRef(IReadOnlyList<Place> Targets, Place? Source, bool SourceIsNull, bool SourceIsSelf) : BoundStatement
{
    public string? SourceFactoryCs { get; init; }
    public bool FromExceptionObject { get; init; }
}

/// <summary>A method-context <c>GOBACK</c> / (pre-2023) <c>EXIT METHOD</c> (ISO §14.9.18.4 GR4; deep-dive D8 —
/// the one decision that silently miscompiles if missed): terminates the executing METHOD only, returning
/// control to the INVOKE site — never the run unit (<see cref="BoundStop"/>) and never the program activation
/// (<see cref="BoundGoback"/>). Rendered as <c>throw new MethodReturn()</c>, caught at the method's public
/// entry (the ProgramReturn-pattern realization of D8: a plain <c>return</c> cannot unwind the nested bounded
/// <c>__Dispatch</c> frames an out-of-line PERFORM stacks).</summary>
public sealed record BoundMethodReturn(BoundRaising? Raising = null) : BoundStatement;
// Raising: GOBACK/EXIT METHOD … RAISING from a method (§14.9.18.4 GR1b; the EC-OO wave) — STAGED before
// the MethodReturn throw; the INVOKE site's pickup applies the §14.6.13.1.5 activator rules AFTER the
// RETURNING delivery + copy-outs (GR1b's result-before-exception ordering falls out of the throw/catch).
