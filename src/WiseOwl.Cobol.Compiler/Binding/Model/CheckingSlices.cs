// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Binding.Model;

/// <summary>The checking facts of ONE program unit or class half (docs/rearchitecture/DESIGN-architecture-review.md §8.3
/// (1); kb/Work PB2296): a value slice of <c>DataBindState</c> whose one producer is <c>CheckingFold</c>,
/// written after the unit's data phases and before its procedure division binds. Code generation and the reference
/// resolver read it; codegen owns no <c>TurnState</c>, so every <c>&gt;&gt;TURN</c> and <c>&gt;&gt;PROPAGATE</c>
/// query a unit's emitted checks depend on is answered here once.</summary>
/// <param name="ExternalCheckMask">The unit's before-Environment-division EC-EXTERNAL enablement mask (ISO §2023:14.8.4.1 —
/// the ACTIVATED-element half of the both-elements rule; <c>ExternalChecks</c> bits): the group TurnState folded at the
/// unit's first post-Identification division header, emitted as the selfMask of the activation-entry
/// <c>ExternalStore.Describe</c> registrations.</param>
/// <param name="ArgMismatchChecking">§2023:14.9.4.4 GR3 d)'s ACTIVATED half (kb/Work PB133 wave C2b): the unit's
/// EC-PROGRAM-ARG-MISMATCH enablement at its PROCEDURE DIVISION header (§2023:7.3.25.4 GR6 / GR8;
/// <c>TurnState.EnabledAtHeader</c>, kb/Work PB1381), registered with the program table so a dynamic CALL's count check
/// can apply the enabled-in-both gate.</param>
/// <param name="ExternalDescribe">True when any enabling <c>&gt;&gt;TURN</c> event anywhere in the compilation group covers
/// an EC-EXTERNAL condition — the group-level gate for emitting the activation-entry <c>ExternalStore.Describe</c>
/// registrations at all (an element with a zero <paramref name="ExternalCheckMask"/> must still REGISTER its descriptions
/// so a later-enabled element can check against them, §2023:14.8.4; a group with no EC-EXTERNAL TURN emits nothing). A class
/// half carries it too: a method activation registers its factory's or object's external descriptions (§2023:14.9.23.4 GR7
/// d); kb/Work PB1138).</param>
/// <param name="OdoReferenceChecking">True when any enabling <c>&gt;&gt;TURN</c> event anywhere in the compilation group
/// covers EC-BOUND-ODO — the gate for attaching §2023:13.18.38.4 GR7's element-reference check
/// (<see cref="OdoReferenceCheck"/>) to a subscripted OCCURS DEPENDING level (kb/Work PB1268). The run-time
/// <c>BoundOdoChecking</c> flag still decides per statement; this only keeps a group that can never enable the condition
/// free of the data-name-1 read at every element reference.</param>
/// <param name="AutomaticPropagation">Is automatic propagation of exception conditions enabled for this program or function
/// unit (ISO §2023:7.3.21.4 GR1/GR3; kb/Work PB1119)? Folded at the unit's first line; read by <c>ProgramEmitter</c> into the
/// emitter's per-element propagation state, which <c>EcEmitter.EmitSelection</c> applies (§2023:14.6.13.1.3 6)).</param>
public sealed record UnitChecking(
    int ExternalCheckMask, bool ArgMismatchChecking, bool ExternalDescribe, bool OdoReferenceChecking, bool AutomaticPropagation)
{
    /// <summary>The slice before its producer has run, and for an element the fold does not reach (an interface, an
    /// unexpanded parameterized definition): nothing enabled.</summary>
    public static UnitChecking None { get; } = new(0, false, false, false, false);
}

/// <summary>The checking facts of ONE method (DESIGN-architecture-review §8.3 (1)): the method twin of <see cref="UnitChecking"/>, written by
/// <c>CheckingFold</c> once every method symbol exists (after <c>OoClassTable.ResolveOverrides</c>) and before any
/// method body binds, and read by <c>OoEmitter</c> through <c>OoMethodSymbol.Checking</c>.</summary>
/// <param name="OoUniversalChecking">§2023:14.9.23.4 GR7 c)'s METHOD-side half: EC-OO-UNIVERSAL enabled at the method's
/// PROCEDURE DIVISION HEADER, the query point of the <c>__CobolInvoke</c> prologue's conformance checks (§2023:7.3.25.4 GR6 /
/// GR8; kb/Work PB1381). A method with no procedure division answers at its METHOD-ID line.</param>
/// <param name="ExternalCheckMask">§2023:14.8.4.1's ACTIVATED-method half (kb/Work PB1138): the EC-EXTERNAL conditions enabled
/// "before the Environment division" of the method — its first division after METHOD-ID, the fold a program unit takes at
/// its own — baked as the self mask of the method activation's external-item registrations (§2023:14.9.23.4 GR7 d)).</param>
/// <param name="AutomaticPropagation">Automatic propagation of exception conditions for the METHOD (§2023:7.3.21.4 GR1/GR3;
/// kb/Work PB1119), folded at its METHOD-ID line; §2023:7.3.21.3 SR1 keeps the directive out of the class, so that is also the
/// CLASS-ID's state.</param>
public sealed record MethodChecking(bool OoUniversalChecking, int ExternalCheckMask, bool AutomaticPropagation)
{
    /// <summary>The slice before its producer has run: nothing enabled.</summary>
    public static MethodChecking None { get; } = new(false, 0, false);
}
