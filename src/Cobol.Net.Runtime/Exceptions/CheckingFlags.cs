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
/// <para><b>Adding a condition</b> is five declarations and no prose: a field HERE, a delegating property on
/// <see cref="ExceptionEngine"/>, its static shim forwarder, the (exception-name → this flag) row in
/// <c>EcEmitter</c>'s gate table, and a ONE-LINE raise helper
/// (<c>… => FatalIfEnabled(&lt;flag&gt;, "&lt;EC&gt;", detail)</c> — kb/Work PB676). Nothing else has to know.
/// <c>ExceptionCheckingFlagsDriftTests</c> fails if an engine flag is ever declared outside this struct, so GR14's
/// PUSH ALL cannot be escaped; <c>ExceptionRaiseHelperDriftTests</c> fails if a helper reads a flag other than the
/// one the emitter pairs to the name it raises, or records a fatality Table 13 does not carry. Both invariants are
/// enforced rather than remembered.</para>
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

    /// <summary>EC-RANGE-PERFORM-VARYING — a PERFORM VARYING/AFTER that initializes an index-name from a data
    /// item whose value is not positive (§14.9.28.4 GR3). The field name is the emitter's flag stem, not the
    /// exception-name; the two are paired in <c>EcEmitter</c>'s gate table and the pairing is asserted by
    /// <c>ExceptionRaiseHelperDriftTests</c>.</summary>
    public bool PerformVarying;

    /// <summary>EC-DATA-NOT-FINITE — a NaN/±Inf standard-float sending operand (§14.6.13.2 item 3).</summary>
    public bool FloatNotFinite;

    /// <summary>EC-DATA-OVERFLOW — a MOVE whose finite sending algebraic value is farther from zero than a
    /// standard-float receiver's usage can represent — "the algebraic value of the sending operand is farther from
    /// zero than is permitted by the usage specifications of the receiving data item" (§14.9.25.4 GR6 d)4.a, which
    /// is MOVE-specific). NOT EC-SIZE-OVERFLOW, which this doc named until kb/Work PB676.</summary>
    public bool FloatOverflow;

    /// <summary>EC-DATA-INCOMPATIBLE — a de-editing MOVE whose numeric-edited sender holds content that is not a
    /// possible result of any editing operation in that item (§14.6.13.2 rule 4; kb/Work PB66).</summary>
    public bool DataIncompatible;

    /// <summary>EC-DATA-PTR-NULL — a reference through a NULL data-address pointer (§13.18.5.4 GR3), or a NULL
    /// operand on SET pointer UP/DOWN BY (§14.9.39 Format 10 GR18).</summary>
    public bool DataPtrNull;

    /// <summary>EC-BOUND-PTR — a reference through an address that is not NULL and not a valid address of
    /// storage (§13.18.5.4 GR4).</summary>
    public bool BoundPtr;

    /// <summary>EC-SIZE-ADDRESS — a non-integer SET pointer UP/DOWN BY amount (§14.9.39 Format 10 GR19).</summary>
    public bool SizeAddress;

    /// <summary>EC-PROGRAM-ARG-OMITTED — a reference to an omitted formal parameter outside the
    /// omitted-argument condition or an argument position (§14.9.4.4 GR12; kb/Work PB133 wave C).</summary>
    public bool ProgramArgOmitted;

    /// <summary>EC-BOUND-SUBSCRIPT — a subscript below 1 or above the highest permissible occurrence number
    /// (§8.4.2.3.4 GR2).</summary>
    public bool BoundSubscript;

    /// <summary>EC-BOUND-ODO — an OCCURS DEPENDING control value outside integer-1 through integer-2
    /// (§13.18.38.4 GR7).</summary>
    public bool BoundOdo;

    /// <summary>EC-OO-UNIVERSAL — the ACTIVATOR's half of §14.9.23.4 GR7c's "enabled in BOTH" gate. Set around
    /// an INVOKE by the emitted statement guard and read by the callee's <c>__CobolInvoke</c>, which is entered
    /// synchronously on the same run unit; the METHOD's half is a compile-time literal baked per method.</summary>
    public bool OoUniversal;

    /// <summary>EC-BOUND-TABLE-LIMIT — a dynamic-capacity table grown past the implementor maximum
    /// (§14.9.39.4 GR30).</summary>
    public bool BoundTableLimit;

    /// <summary>EC-FLOW-SEARCH — a capacity SET during a SEARCH of that same table (§14.9.39.4 GR31).</summary>
    public bool FlowSearch;

    /// <summary>EC-FLOW-REPORT — a GENERATE, INITIATE or TERMINATE executed within the range of a USE
    /// BEFORE REPORTING declarative procedure (§14.9.49.4 GR10).</summary>
    public bool FlowReport;

    /// <summary>EC-REPORT-ACTIVE — an INITIATE of a report that is already in the active state
    /// (§14.9.21.4 GR2).</summary>
    public bool ReportActive;

    /// <summary>EC-REPORT-INACTIVE — a GENERATE (§14.9.16.4 GR7) or TERMINATE (§14.9.46.4 GR1) of a report
    /// that is not in the active state.</summary>
    public bool ReportInactive;

    /// <summary>EC-REPORT-FILE-MODE — an INITIATE whose report file connector is not open in the output or
    /// the extend mode (§14.9.21.4 GR3).</summary>
    public bool ReportFileMode;

    /// <summary>EC-ORDER-NOT-SUPPORTED — FUNCTION STANDARD-COMPARE naming a cultural ordering table this
    /// processor does not provide, or an ordering level the table does not define (§15.85.4 r2).</summary>
    public bool OrderNotSupported;

    /// <summary>EC-LOCALE-MISSING — a locale that is not available in this operating environment: SET LOCALE's
    /// locale-name-1 (§14.9.39.4 GR24), a named IS LOCALE collating sequence at use (§8.2.1; DESIGN-locale-facility L1).</summary>
    public bool LocaleMissing;

    /// <summary>EC-LOCALE-INVALID-PTR — SET LOCALE through a pointer that does not reference saved locale
    /// information (§14.9.39.4 GR21).</summary>
    public bool LocaleInvalidPtr;

    /// <summary>EC-LOCALE-INCOMPATIBLE — a locale comparison over an operand the locale's collating sequence does
    /// not order (§8.8.4.2.11; DETERMINATION L6 — an ill-formed UTF-16 operand).</summary>
    public bool LocaleIncompatible;

    /// <summary>EC-LOCALE-INVALID — an operation using a locale whose content is invalid or incomplete in this
    /// environment (§8.2.1): a LOCALE-DATE / -TIME over a locale with no LC_TIME culture data (T4; T5/T6 add LC_CTYPE /
    /// LC_MONETARY).</summary>
    public bool LocaleInvalid;

    /// <summary>EC-LOCALE-SIZE — locale editing of a PICTURE format-2 item truncated a character that is neither a
    /// zero nor a space caused by a suppressed zero (§13.18.40.5 r14 b); Table 13 Fatal; PB64 T6).</summary>
    public bool LocaleSize;
}
