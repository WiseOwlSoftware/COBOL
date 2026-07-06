// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.Exceptions;

/// <summary>
/// The run-unit-wide LAST EXCEPTION STATUS register (ISO/IEC 1989:2023 §14.6.13.1.1: "a last exception status
/// exists for the entire run unit … set to indicate the last level-3 exception condition that was raised"), plus
/// the associated information the EXCEPTION-FILE / EXCEPTION-LOCATION / EXCEPTION-STATEMENT functions interrogate
/// (§15.28/§15.30/§15.32) and the propagation slot GOBACK/EXIT … RAISING uses to re-raise in the activator
/// (§14.9.18 / §14.9.14). It is written ONLY when checking for the condition is enabled (§14.6.13.1.1: the
/// indicator is set "when checking for the associated exception condition is enabled … and the associated
/// exception occurs") — the generated raise sites are themselves gated by the compile-time TurnState, so a
/// checking-off program never touches this class. <c>SET LAST EXCEPTION TO OFF</c> (SET Format 13, §14.9.39)
/// maps to <see cref="Clear"/>.
/// </summary>
public static class ExceptionState
{
    /// <summary>The last raised level-3 exception-name (uppercase), or null when no exception condition exists.</summary>
    public static string? LastName { get; private set; }

    /// <summary>Whether the last raised exception condition is fatal (Table 13 category; EC-*-IMP ⇒ fatal here).</summary>
    public static bool LastFatal { get; private set; }

    /// <summary>The file-connector key associated with the last EC-I-O exception (null for non-I-O conditions
    /// and for an EC-I-O raised by RAISE / EXIT RAISING / GOBACK RAISING — §15.28.4 r1b).</summary>
    public static string? LastFile { get; private set; }

    /// <summary>The two-character I-O status associated with the last EC-I-O exception (§15.28.4 r1c).</summary>
    public static string? LastIoStatus { get; private set; }

    /// <summary>The §15.30.3 r2 location string ("element; paragraph[ OF section]; line-id"), captured ONLY when
    /// the enabling TURN directive carried WITH LOCATION (§7.3.25.4 GR7; the implementor choice of §15.30.3 r1 is
    /// NO information without LOCATION — recorded in the deep-dive).</summary>
    public static string? LastLocation { get; private set; }

    /// <summary>The uppercase statement name that raised the last exception (§15.32.3 r2), captured only WITH
    /// LOCATION (r1 otherwise).</summary>
    public static string? LastStatement { get; private set; }

    /// <summary>EXCEPTION-OBJECT (§8.4.3.6) — the run unit's ONE predefined object reference (GR2):
    /// references the current exception object, null when none (GR1). Implicitly UNIVERSAL (SR2 :7249) —
    /// typed <see cref="CobolObject"/> per the D-U1 universal model. Set by RAISE identifier-1, by
    /// the GR1b2 activator-side pickup, and (GR15) it already holds the object when an F4 declarative
    /// enters; nulled by every NAMED raise (§14.9.29.4 GR1 / :24485).</summary>
    public static CobolObject? ExceptionObject { get; private set; }

    /// <summary>§15.33.3 rule 1's literal EXCEPTION-STATUS value when the last exception is an exception
    /// OBJECT — the LastName sentinel (NOT a catalog exception-name; ExceptionCatalog.TryGet fails on it
    /// by design, and the EXCEPTION-* functions degrade correctly: not an IO name → "00", no location →
    /// spaces).</summary>
    public const string ObjectSentinel = "EXCEPTION-OBJECT";

    /// <summary>Record a raised exception OBJECT (§14.6.13.1.5 (1)/(2): EXCEPTION-OBJECT references the
    /// object; the last exception status indicates an exception object — realized as the
    /// <see cref="ObjectSentinel"/> LastName). Exception objects are NOT TURN-gated (§7.3.25 takes
    /// exception-NAMES only) and a RAISE of an object is never fatal by itself (§14.9.29.4 GR2).</summary>
    public static void SetObject(CobolObject? obj)
    {
        LastName = ObjectSentinel;
        LastFatal = false;
        LastFile = null;
        LastIoStatus = null;
        LastStatement = null;
        LastLocation = null;
        ExceptionObject = obj;
    }

    /// <summary>Record a raised non-I-O exception condition (§14.6.13.1.1: sets the last exception status;
    /// EXCEPTION-OBJECT is set to null — §14.9.29.4 GR1).</summary>
    public static void Set(string name, bool fatal, string? statement = null, string? location = null)
    {
        LastName = name.ToUpperInvariant();
        LastFatal = fatal;
        LastFile = null;
        LastIoStatus = null;
        LastStatement = statement;
        LastLocation = location;
        ExceptionObject = null;
        _propagatedObject = default;   // a NAMED raise supersedes any staged object (the slots are exclusive)
    }

    /// <summary>Record a raised EC-I-O exception condition with its file connector and I-O status
    /// (the §15.28 EXCEPTION-FILE information).</summary>
    public static void SetIo(string name, bool fatal, string file, string ioStatus, string? statement = null, string? location = null)
    {
        Set(name, fatal, statement, location);
        LastFile = file;
        LastIoStatus = ioStatus;
    }

    /// <summary><c>SET LAST EXCEPTION TO OFF</c> (ISO §14.9.39 Format 13): the last exception status indicates
    /// that no exception condition exists.</summary>
    public static void Clear()
    {
        LastName = null;
        LastFatal = false;
        LastFile = null;
        LastIoStatus = null;
        LastStatement = null;
        LastLocation = null;
        ExceptionObject = null;
        _propagated = null;
        _propagatedObject = default;
    }

    // ── GOBACK / EXIT … RAISING propagation (§14.9.18 GR / §14.6.13.1.3 #6 shape) ────────────────────────────

    private static (string Name, bool Fatal)? _propagated;

    /// <summary>Stage an exception condition for re-raising in the ACTIVATOR after the current program returns
    /// (GOBACK/EXIT PROGRAM … RAISING, §14.9.18). The generated CALL site consumes it via
    /// <see cref="TakePropagated"/>; <see cref="ProgramRegistry"/> applies the fatal default when no CALL-site
    /// pickup exists (checking off in the caller — the §14.6.13.1.3 #8 implementor choice: terminate loudly).</summary>
    public static void SetPropagating(string name, bool fatal)
    {
        Set(name, fatal);   // the last exception status reflects the raise in the returning element
        _propagated = (name.ToUpperInvariant(), fatal);
    }

    // ── The exception-OBJECT propagation slot (§14.6.13.1.5; GOBACK/EXIT/EXIT METHOD … RAISING identifier).
    //    Mutually exclusive with _propagated — a GOBACK stages exactly ONE of a name or an object. ──

    private static (bool Has, CobolObject? Obj) _propagatedObject;

    /// <summary>Stage an exception OBJECT for the activator (GOBACK / EXIT PROGRAM / method-return RAISING
    /// identifier-1, §14.9.18.4 GR1b): the returning element's status reflects the raise (GR1b1 via
    /// <see cref="SetObject"/>); the ACTIVATING site consumes via <see cref="TakePropagatedObject"/> and
    /// applies the §14.6.13.1.5 activator rules (F4 → declarative; none → EC-OO-EXCEPTION).</summary>
    public static void SetPropagatingObject(CobolObject? obj)
    {
        SetObject(obj);
        _propagatedObject = (true, obj);
        _propagated = null;
    }

    /// <summary>Consume the staged exception object at the activating CALL/INVOKE site (mirrors
    /// <see cref="TakePropagated"/>; clears the slot).</summary>
    public static bool TakePropagatedObject(out CobolObject? obj)
    {
        (bool has, obj) = _propagatedObject;
        _propagatedObject = default;
        return has;
    }

    /// <summary>Stage the LAST EXCEPTION for re-raising (GOBACK RAISING LAST EXCEPTION, §14.9.18.2). A clear
    /// last-exception status stages nothing.</summary>
    public static void SetPropagatingLast()
    {
        // GOBACK RAISING LAST EXCEPTION with an OBJECT status re-propagates the OBJECT (§14.9.18.4 GR1b3a
        // :27724 → the §14.6.13.1.5 rules); a clear status stages nothing.
        if (LastName == ObjectSentinel) { _propagatedObject = (true, ExceptionObject); _propagated = null; }
        else if (LastName is { } n) _propagated = (n, LastFatal);
    }

    /// <summary>Consume the staged propagation (the generated CALL-site pickup). Returns false when none.</summary>
    public static bool TakePropagated(out string name, out bool fatal)
    {
        if (_propagated is { } p)
        {
            _propagated = null;
            (name, fatal) = p;
            return true;
        }
        name = "";
        fatal = false;
        return false;
    }

    // ── EC-ARGUMENT-FUNCTION ambient statement gate ───────────────────────────────────────────────────────────

    /// <summary>True while the currently-executing statement has EC-ARGUMENT-FUNCTION checking enabled (set and
    /// reset by the generated statement guard). Intrinsic-function domain-error sites consult it: when set, the
    /// §15.3 default-result-0 convention is replaced by raising EC-ARGUMENT-FUNCTION (fatal — Table 13); when
    /// clear, the default result stands (§14.6.13.1.4: an unraised condition's special rules apply). Ambient
    /// (not a per-call argument) because intrinsic calls render inline inside arbitrary expressions — threading
    /// a mask through every runtime signature would fork each intrinsic into checked/unchecked twins.</summary>
    public static bool ArgumentFunctionChecking { get; set; }

    /// <summary>Raise EC-ARGUMENT-FUNCTION for an intrinsic argument/domain error when checking is enabled
    /// (Table 13: Fatal — thrown as <see cref="CobolFatalException"/>, caught by the statement guard for USE F3
    /// dispatch, else terminating the run unit per §14.6.13.1.3 #7); otherwise return 0, the §15.3
    /// default-result convention.</summary>
    public static long ArgumentError(string detail)
    {
        if (ArgumentFunctionChecking)
        {
            Set("EC-ARGUMENT-FUNCTION", fatal: true);
            throw new CobolFatalException("EC-ARGUMENT-FUNCTION", detail);
        }
        return 0;
    }

    // ── EC-DATA-CONVERSION ambient statement gate (CONVERT / DISPLAY-OF / NATIONAL-OF) ────────────────────────

    /// <summary>True while the currently-executing statement has EC-DATA-CONVERSION checking enabled (set and
    /// reset by the generated statement guard — the nonfatal twin of <see cref="ArgumentFunctionChecking"/>).
    /// CONVERT's untranslatable-character sites consult it: the implementor-defined substitution character stands
    /// unconditionally, but the last exception status is recorded ONLY when checking is enabled (§14.6.13.1.1).</summary>
    public static bool DataConversionChecking { get; set; }

    /// <summary>Record EC-DATA-CONVERSION for an untranslatable CONVERT value (§15.19.4 r1/r3). Nonfatal
    /// (Table 13 — <see cref="ExceptionCatalog"/> EC-DATA-CONVERSION), so it never throws; it only sets the last
    /// exception status, and only while checking for the condition is enabled (§14.6.13.1.1). The substitution
    /// character is applied by the caller regardless.</summary>
    public static void DataConversionError(string detail)
    {
        if (DataConversionChecking) Set("EC-DATA-CONVERSION", fatal: false);
    }
}
