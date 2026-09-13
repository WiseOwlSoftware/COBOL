// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;

namespace CobolNet.Binding.Procedure;

/// <summary>The KIND of source element whose PROCEDURE DIVISION is being bound. ISO §14.2.2 SR10 enumerates the
/// five source elements that may carry a Format-1/2 procedure division — "a function definition, a function
/// prototype definition, a method definition, a program definition, or a program prototype definition" — and the
/// EXIT / GOBACK placement rules are written against that enumeration, not against a program/not-program bit.
/// <para>⛔ THE ENUMERATION IS THE POINT (kb/Work PB403): <c>BindExit</c>'s SR7 arm used to test the single
/// predicate <c>host.InMethod</c>, so of the four non-program elements exactly one was refused and a FUNCTION
/// definition ending in EXIT PROGRAM was accepted AND given the program-activation machinery. A scalar where the
/// rule names a SET is the shape that rejects — or here admits — silently.</para></summary>
internal enum SourceElementKind
{
    /// <summary>A program definition (ISO §10.3) — the only element whose procedure division EXIT PROGRAM's
    /// §14.9.14.3 SR7 and GOBACK's program arm are written about.</summary>
    Program,

    /// <summary>A program prototype definition (ISO §10.6; PROGRAM-ID … IS PROTOTYPE). Its procedure division is
    /// still a PROGRAM's — the noun in §14.2.2 SR10's fifth item is "program" — so it is admitted wherever SR7
    /// says "a program procedure division"; the alternative reading would reject source the standard never
    /// forbids.
    /// <para>⚠ CURRENTLY UNREACHABLE, and measured so rather than assumed: the grammar's
    /// <c>programIdAttribute</c> admits only COMMON / INITIAL / RECURSIVE / GLOBAL, so ISO §11.10.2's Format-2
    /// <c>PROGRAM-ID. name IS PROTOTYPE.</c> does not parse (COBOLNET0901 on the reserved word PROTOTYPE), and
    /// <c>BinderDriver.MakeUnit</c> derives <c>IsPrototype</c> from the FUNCTION-ID paragraph alone. The member
    /// exists because it is one of §14.2.2 SR10's five and because the day that grammar arm lands, the
    /// classification is already right — it is NOT a claim that this compiler accepts program prototypes.
    /// <c>ExitPlacementContextDriftTests</c> pins the parse refusal, so landing the grammar turns that fact RED
    /// and forces a re-verification of this arm.</para></summary>
    ProgramPrototype,

    /// <summary>A function definition (ISO §10.4; FUNCTION-ID). NOT a program procedure division.</summary>
    FunctionDefinition,

    /// <summary>A function prototype definition (ISO §10.6; FUNCTION-ID … IS PROTOTYPE). NOT a program
    /// procedure division — the noun is "function".</summary>
    FunctionPrototype,

    /// <summary>A method definition (ISO §11.7). NOT a program procedure division — a method returns through
    /// GOBACK / EXIT METHOD.</summary>
    MethodDefinition,
}

/// <summary>One frame of the bind-time ENCLOSING-CONSTRUCT STACK — the lexical constructs a statement can be
/// written INSIDE that a placement syntax rule asks about. Pushed by the binder that descends into the
/// construct's statement block and popped when it leaves, so the innermost frame is the last.</summary>
internal enum EnclosingConstruct
{
    /// <summary>An inline PERFORM's imperative-statement-1 (ISO §14.9.28.2 Format 2).</summary>
    InlinePerform,

    /// <summary>An exception-checking PERFORM (ISO §14.9.28.2 Format 3), pushed over the WHOLE statement —
    /// imperative-statement-1, every handler body and the FINALLY phrase — because §14.9.14.4 GR4 gives an
    /// EXIT PERFORM written in any of them the same meaning: the implicit CONTINUE before END-PERFORM.</summary>
    ExceptionCheckingPerform,

    /// <summary>A WHEN / WHEN OTHER / WHEN COMMON phrase of an exception-checking PERFORM (imperative-statement
    /// 2/3/4) — the position §14.9.33.3 SR1 and §14.9.14.3 SR6 name beside "a declarative procedure".</summary>
    PerformWhen,

    /// <summary>A FINALLY phrase (imperative-statement-5). NOT a WHEN phrase: RESUME is not admitted there
    /// (§14.9.33.3 SR1), which is why the two handler kinds are separate frames.</summary>
    PerformFinally,
}

/// <summary>
/// ⛔ THE ONE "WHERE AM I BOUND?" PROBE — the answer every PLACEMENT syntax rule needs, computed once at the bind
/// cursor instead of re-derived by hand at each verb (kb/Work PB403, PB404).
///
/// <para><b>Why it exists.</b> ISO writes at least eight placement rules over four verbs — §14.9.14.3 SR2, SR6,
/// SR7, SR8; §14.9.18.3 SR1, SR5; §14.9.33.3 SR1, SR2 — and every one of them asks some part of the same
/// question: which source element is this, am I inside a declarative (and does its USE carry GLOBAL), am I inside
/// an inline or exception-checking PERFORM, am I inside a WHEN phrase. Before this type, RESUME answered it by
/// scanning <c>ctx.Table.Declaratives</c> in its own binder, the exception-checking PERFORM answered the
/// EXIT-PERFORM-CYCLE half by WALKING ITS OWN PARSE SUBTREE after the fact, EXIT PROGRAM answered a quarter of it
/// with one boolean, and EXIT PERFORM's placement half was not answered at all — which is how
/// <c>EXIT PERFORM</c> outside every PERFORM compiled clean and emitted a bare <c>break;</c> that spun the pc
/// dispatcher forever. One question written down four times is the defect; the extraction is the fix.</para>
///
/// <para><b>What it is.</b> A read-only view over <see cref="BinderContext"/>'s live bind position: the source
/// element kind, the enclosing-construct stack, and the declarative section containing the bind cursor. It
/// carries no state of its own and is recomputed per read, so it cannot go stale.</para>
/// </summary>
internal readonly struct EnclosingContext
{
    private readonly List<EnclosingConstruct>? _stack;

    internal EnclosingContext(SourceElementKind element, List<EnclosingConstruct> stack, BoundDeclarative? declarative)
    {
        SourceElement = element;
        _stack = stack;
        Declarative = declarative;
    }

    /// <summary>The kind of source element whose procedure division is being bound (ISO §14.2.2 SR10).</summary>
    public SourceElementKind SourceElement { get; }

    /// <summary>Is this a PROGRAM's procedure division — the predicate §14.9.14.3 SR7 states as "a program
    /// procedure division"? True for a program definition and for a program prototype definition (§10.6); false
    /// for the three FUNCTION / METHOD elements §14.2.2 SR10 names beside them.</summary>
    public bool InProgramProcedureDivision =>
        SourceElement is SourceElementKind.Program or SourceElementKind.ProgramPrototype;

    /// <summary>The declarative section containing the bind cursor, or null outside the declaratives portion —
    /// the ONE lookup behind §14.9.14.3 SR2, §14.9.18.3 SR1 and §14.9.33.3 SR1/SR2.</summary>
    public BoundDeclarative? Declarative { get; }

    /// <summary>Is this statement in a declarative procedure (ISO §14.3)?</summary>
    public bool InDeclarative => Declarative is not null;

    /// <summary>Is this statement in a declarative procedure "for which the GLOBAL phrase is specified in the
    /// associated USE statement" (ISO §14.9.14.3 SR2, §14.9.18.3 SR1, §14.9.33.3 SR2)?</summary>
    public bool InGlobalDeclarative => Declarative is { Global: true };

    /// <summary>The NEAREST enclosing PERFORM statement's format, or null when this statement is inside none.
    /// The WHEN / FINALLY frames are transparent here: a statement in a handler body is still inside its
    /// exception-checking PERFORM (§14.9.14.4 GR4). An OUT-OF-LINE PERFORM never appears — it does not
    /// lexically contain the statements it runs, which is exactly why §14.9.14.3 SR8 excludes it.</summary>
    public EnclosingConstruct? NearestPerform
    {
        get
        {
            if (_stack is null) return null;
            for (int i = _stack.Count - 1; i >= 0; i--)
                if (_stack[i] is EnclosingConstruct.InlinePerform or EnclosingConstruct.ExceptionCheckingPerform)
                    return _stack[i];
            return null;
        }
    }

    /// <summary>Is this statement inside "an inline or exception-checking PERFORM statement" — the one predicate
    /// ISO §14.9.14.3 SR8's first sentence states for EXIT PERFORM?</summary>
    public bool InPerform => NearestPerform is not null;

    /// <summary>Is the NEAREST enclosing PERFORM an exception-checking (Format-3) one? The CYCLE prohibition in
    /// §14.9.14.3 SR8's second sentence is about the PERFORM an EXIT PERFORM CYCLE would cycle, which is the
    /// nearest one — a CYCLE inside an ordinary inline PERFORM nested in a Format-3 PERFORM is legal.</summary>
    public bool InExceptionCheckingPerform => NearestPerform is EnclosingConstruct.ExceptionCheckingPerform;

    /// <summary>Is this statement in "a WHEN phrase in a PERFORM statement" (ISO §14.9.14.3 SR6, §14.9.18.3 SR5,
    /// §14.9.33.3 SR1)? ANY enclosing WHEN frame counts — a statement nested in an inline PERFORM written inside
    /// a WHEN phrase is still an imperative statement in that WHEN phrase.</summary>
    public bool InPerformWhen => _stack is not null && _stack.Contains(EnclosingConstruct.PerformWhen);
}
