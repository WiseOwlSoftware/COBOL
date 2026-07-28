// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.Exceptions;

/// <summary>
/// The AMBIENT exception-checking state as ONE copyable value — every <c>…Checking</c> flag the generated
/// statement guards set and reset (ISO §14.6.13.1.1: "if checking for an exception that occurs is not enabled,
/// no exception condition is raised").
///
/// <para><b>Why a struct and not loose fields.</b> §14.9.28.4 GR14 requires an exception-checking PERFORM to
/// take "an implicit PUSH ALL followed by TURN OFF ALL … at the end of imperative-statement-1" and an
/// "implicit POP ALL" immediately before END-PERFORM — a WHOLESALE save/clear/restore of every flag at once.
/// Expressed over individual properties that is a hand-maintained list, and the failure mode is silent: an
/// exception condition added later that forgets to join the list simply stops obeying GR14, with no test
/// failing and no diagnostic. Holding the flags in one value makes PUSH ALL a struct copy and TURN OFF ALL a
/// <c>default</c>, so a new condition is covered by construction the moment its field exists here.</para>
///
/// <para><b>Adding a condition:</b> add a field here and a delegating property on <see cref="ExceptionEngine"/>
/// (plus the static shim forwarder). Nothing else has to know. <c>ExceptionCheckingFlagsDriftTests</c> fails if
/// an engine flag is ever declared outside this struct, so the invariant is enforced rather than remembered.</para>
/// </summary>
public struct CheckingFlags
{
    /// <summary>EC-ARGUMENT-FUNCTION — an intrinsic-function argument/domain error (§15.3).</summary>
    public bool ArgumentFunction;

    /// <summary>EC-DATA-CONVERSION — a conversion that loses information (§14.6.13.2).</summary>
    public bool DataConversion;

    /// <summary>EC-BOUND-OVERFLOW — a dynamic-capacity table receiving item grown past its expected bound.</summary>
    public bool BoundOverflow;

    /// <summary>EC-BOUND-REF-MOD — an out-of-range reference modification (§8.4.3.3.4).</summary>
    public bool BoundRefMod;

    /// <summary>EC-PERFORM-VARYING — a VARYING/AFTER control-variable violation (§14.9.28).</summary>
    public bool PerformVarying;

    /// <summary>EC-DATA-NOT-FINITE — a NaN/±Inf standard-float sending operand (§14.6.13.2 item 3).</summary>
    public bool FloatNotFinite;

    /// <summary>EC-SIZE-OVERFLOW on a floating-point target — an IEEE overflow to ±Inf.</summary>
    public bool FloatOverflow;

    /// <summary>EC-DATA-PTR-NULL — a reference through a NULL data-address pointer (§13.18.5.4 GR3), or a NULL
    /// operand on SET pointer UP/DOWN BY (§14.9.39 Format 10 GR18).</summary>
    public bool DataPtrNull;

    /// <summary>EC-BOUND-PTR — a reference through an address that is not NULL and not a valid address of
    /// storage (§13.18.5.4 GR4).</summary>
    public bool BoundPtr;

    /// <summary>EC-SIZE-ADDRESS — a non-integer SET pointer UP/DOWN BY amount (§14.9.39 Format 10 GR19).</summary>
    public bool SizeAddress;
}
