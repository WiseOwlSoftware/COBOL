// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Runtime.CompilerServices;
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Runtime;

/// <summary>The kind of the ACTIVATED runtime element whose formal parameter a reference names — the key of the
/// *-ARG-OMITTED rule, which the standard writes once per kind (kb/Work PB971):
/// <list type="bullet">
/// <item><see cref="Program"/> — ISO §14.9.4.4 GR12, "referenced in a called program … EC-PROGRAM-ARG-OMITTED".</item>
/// <item><see cref="Function"/> — ISO §8.4.3.2.4 GR8, "referenced in an activated function … EC-FUNCTION-ARG-OMITTED".</item>
/// <item><see cref="Method"/> — ISO §14.9.23.4 GR10, "referenced in an invoked method … EC-OO-ARG-OMITTED".</item>
/// </list></summary>
public enum ActivatedElementKind
{
    /// <summary>A called program (§14.9.4.4 GR12).</summary>
    Program,

    /// <summary>An activated user-defined function (§8.4.3.2.4 GR8).</summary>
    Function,

    /// <summary>An invoked method (§14.9.23.4 GR10).</summary>
    Method,
}

/// <summary>
/// ⛔ <b>THE ONE RAISE SITE OF THE *-ARG-OMITTED CONDITIONS</b> (kb/Work PB971). Each of the three rules reads
/// "If a parameter for which the omitted-argument condition is true is referenced in [a called program | an
/// activated function | an invoked method], except as an argument or in the omitted-argument condition, the
/// [EC-PROGRAM | EC-FUNCTION | EC-OO]-ARG-OMITTED exception condition is set to exist" — so the raise is keyed to
/// the REFERENCE and to the kind of the element that owns the formal, never to the activation that supplied the
/// argument and never to the carrier the argument happened to cross in.
/// <para>The compiler routes EVERY rendered reference whose access path is rooted at a formal parameter through
/// one of these guards (the structural <c>OmittedFormalGuard</c> on the path's root segment), which is what makes
/// the two exemptions fall out rather than be listed: the omitted-argument condition reads the presence fact
/// directly, and a formal forwarded as an argument is recognized as a WHOLE formal and its carrier forwarded
/// without rendering the path (§8.8.4.8.4 GR1c). The omitted CALL carrier itself (<c>CobolArgAdapt</c>) no longer
/// raises anything: before PB971 it raised EC-PROGRAM-ARG-OMITTED on read, which named the PROGRAM condition in
/// a function and could not reach a method's copy-in local or a group formal's boundary copy at all.</para>
/// <para>Checking OFF is lenient by construction: the guard returns the storage unchanged, and the storage holds
/// its initial state (a method local / a group formal's callee field) or the omitted carrier's benign empty value.
/// Each rule leaves the result undefined, so that is the documented implementor choice (kb/Work PB133).</para>
/// </summary>
public static class OmittedFormal
{
    /// <summary>The exception-name the reference raises for a formal of an element of <paramref name="kind"/> —
    /// the ONE (kind → name) table; the compiler's EC binder queries >>TURN for exactly this name, so the
    /// statement scope enables the flag the raise below tests.</summary>
    public static string ConditionName(ActivatedElementKind kind) => kind switch
    {
        ActivatedElementKind.Program => "EC-PROGRAM-ARG-OMITTED",    // §14.9.4.4 GR12
        ActivatedElementKind.Function => "EC-FUNCTION-ARG-OMITTED",  // §8.4.3.2.4 GR8
        ActivatedElementKind.Method => "EC-OO-ARG-OMITTED",          // §14.9.23.4 GR10
        _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
    };

    /// <summary>A reference to a formal whose storage is a FIELD or LOCAL of the activated element (a program's
    /// group / redefined formal, every method formal): raise when the argument was omitted, then hand back the
    /// storage BY REFERENCE, so the one guard serves a read, a store and a member access alike.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T Ref<T>(ref T storage, bool omitted, ActivatedElementKind kind, string formal)
    {
        if (omitted) Raise(kind, formal);
        return ref storage;
    }

    /// <summary>A reference to a CARRIER-RESIDENT formal (an elementary program / function formal whose every
    /// access is <c>carrier.Value</c>): the same guard over the carrier, which is returned for its
    /// <c>.Value</c>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TCarrier Carrier<TCarrier>(TCarrier carrier, bool omitted, ActivatedElementKind kind, string formal)
    {
        if (omitted) Raise(kind, formal);
        return carrier;
    }

    /// <summary>Raise the kind's condition through its checked helper — each helper reads its own flag
    /// (<c>ExceptionRaiseHelperDriftTests</c>), so a raise with checking off records nothing and returns.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Raise(ActivatedElementKind kind, string formal)
    {
        var exc = RunUnit.Current.Exceptions;
        string detail = $"reference to formal parameter {formal}, for which the omitted-argument condition is true";
        switch (kind)
        {
            case ActivatedElementKind.Program: exc.ProgramArgOmittedError(detail + " (ISO §14.9.4.4 GR12)"); break;
            case ActivatedElementKind.Function: exc.FunctionArgOmittedError(detail + " (ISO §8.4.3.2.4 GR8)"); break;
            case ActivatedElementKind.Method: exc.OoArgOmittedError(detail + " (ISO §14.9.23.4 GR10)"); break;
            default: throw new System.ArgumentOutOfRangeException(nameof(kind));
        }
    }
}
