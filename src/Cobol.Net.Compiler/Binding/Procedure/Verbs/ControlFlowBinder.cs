// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime.Tree;
using CobolNet.Common;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>The control-flow verb binder (P7 Step 10l — the audit-extension class mirroring the emitter-side
/// <c>CodeGen/Verbs/ControlFlowEmitter</c>; a RECORDED deviation from the plan sketch's three-class split
/// [Perform/If/ControlFlow] — one class matches the emitter topology and the singular-pattern rule):
/// STOP RUN/literal, GO TO (plain / DEPENDING / the ALTER-subsystem delegations — host edges until 10n),
/// EXIT (paragraph/perform/program/section/method/function — the Oo/Udf/Ec dispatches ride host hooks until
/// 10r/10s), IF, and the PERFORM family (inline/out-of-line/TIMES/UNTIL/VARYING — the one control resolver
/// covering both grammar shapes, the NC106A/NC176A lesson). The condition/expression/blocks spine stays on
/// the host until 10o/10q/10t.</summary>
internal sealed class ControlFlowBinder(BinderContext ctx, StatementBinder host)
{
    public BoundStatement BindStop(Core.StopStatementContext stop)
    {
        // STOP RUN … WITH STATUS (§14.9.42) is a COBOL-2002 introduction; the edition gate (StopRunStatus2002)
        // lives in the post-bind VersionConformancePass (Step 14d), reading the PARSE tree (ctx.statusPhrase()).
        // The status VALUE → process-exit-code wiring is decoded here into BoundStop.Status (§14.9.42.4 GR5).
        // §8.8.3.3 GR3: a concatenation expression stands anywhere a literal of its class may — fold a
        // STOP literal-1 concat to the equivalent single literal before decoding (GetText on the whole
        // literal context would glue the operands and mis-decode).
        return stop.literal() is { } slit
            ? new BoundStopLiteral(slit.nonNumericLiteral()?.concatenationExpression() is { } ce
                ? ConcatFolder.Fold(ce, ctx.Edition, ctx.Data.Collating).Value
                : CobolLiteral.Decode(slit.GetText()))
            : new BoundStop(BindTerminationStatus(stop.statusPhrase()));
    }

    /// <summary>Decode a shared <c>statusPhrase</c> (<c>WITH? (ERROR|NORMAL) (STATUS (dataReference|literal)?)?</c>,
    /// ISO §14.9.42.2 / §14.9.18.2) into a <see cref="TerminationStatus"/>, or null when the phrase is absent. The
    /// ERROR/NORMAL keyword is mandatory when the phrase is present; the STATUS value operand is optional.
    /// Shared by STOP RUN and GOBACK — ONE method, so the two verbs cannot acquire different rules (verified, not
    /// assumed: CallBinder's GOBACK arm is this method's only other caller).
    ///
    /// <para>⛔ THE POSITION IS NOT AN ARITHMETIC EXPRESSION, and binding it as one rejected legal COBOL
    /// (kb/Work PB169). The general format §14.9.42.2 writes the operand <c>{identifier-1 | literal-1}</c>, not
    /// <c>arithmetic-expression-1</c>, so §8.8.1.1 never governed it — and both arms went through
    /// <c>host.Expr.BindExpr</c>. Measured on 9a89fbd1: <c>STOP RUN WITH ERROR STATUS "ABEND"</c> AND
    /// <c>STOP RUN WITH ERROR STATUS WS-CODE</c> with <c>WS-CODE PIC X(3)</c> each drew COBOLNET0844 citing
    /// §8.8.1.1, while THIS METHOD'S OWN DOC COMMENT already said the position takes "an integer literal or a
    /// display/national/integer data item" — the code rejected two of the three shapes its documentation
    /// promised. This is the <c>BindByValueExpr</c> / COBOLNET1628 shape exactly: a rule enforced at a site that
    /// could not know its own context, quoting the wrong clause at the programmer.</para>
    ///
    /// <para><b>The position's OWN rules, now enforced</b> (§14.9.42.3 for STOP, the identical §14.9.18.3 SR6–SR8
    /// for GOBACK), each cite.py-checked:
    /// <list type="bullet">
    ///   <item>SR2/SR6 — "Identifier-1 shall reference an integer data item or a data item with usage display or
    ///         usage national" (§14.9.18.3 SR6 is the same rule word for word, over <b>identifier-2</b> — GOBACK's
    ///         identifier-1 is the RAISING object; cite.py --check fails on the identifier-1 spelling there, so do
    ///         not propagate it). Excluded: an ALPHANUMERIC group (no elementary description of its own), a BIT
    ///         group and a usage-BIT item, an index data item, a pointer, an object reference and a non-integer
    ///         COMP/BINARY item. ADMITTED: a <c>PIC X(3)</c> DISPLAY item, a GROUP-USAGE NATIONAL group
    ///         (§13.18.29.4 GR2 b), and a REFERENCE-MODIFIED display/national operand (§8.4.3.3.4 GR6 preserves
    ///         usage) — the last two were the residual over-rejections PB217 closed.</item>
    ///   <item>SR3/SR7 — "If literal-1 is numeric, it shall be an integer". The CONDITIONAL is what makes the
    ///         non-numeric form conforming: a rule that has to say "if it is numeric" presupposes that it may
    ///         not be.</item>
    ///   <item>SR4/SR8 — "Literal-1 shall not be a zero-length literal" (previously unenforced at both verbs).</item>
    /// </list></para>
    ///
    /// <para>GR5 leaves the VALUE constraint implementor-defined and <c>docs/CONFORMANCE.md</c> item 192 already
    /// published this compiler's determination — "the integer value of literal-1 / identifier-1 (truncated toward
    /// zero) becomes the exit code; a non-integer display/national operand is interpreted numerically" — so the
    /// mapping here is not a new decision, it is the published one finally implemented. The renderer already had
    /// it: <c>NumericRenderer.FieldNumCore</c> decodes an alphanumeric/national place through
    /// <c>CobolNum.FromAlphanumeric</c>, and <c>Visit(BoundStringLiteral)</c> does the same for a literal.</para></summary>
    internal TerminationStatus? BindTerminationStatus(Core.StatusPhraseContext? sp)
    {
        if (sp is null) return null;
        return new TerminationStatus(sp.ERROR() is not null, StatusOperand(sp));
    }

    /// <summary>The STATUS phrase's <c>{identifier-1 | literal-1}</c> operand, bound as the position's own operand
    /// and screened by the position's own syntax rules — the <c>CountOperand</c> pattern (PERFORM … TIMES), which
    /// is the model this copies rather than adding a fifth <c>OperandContext</c> member: the slot does not admit
    /// an arithmetic EXPRESSION at all, so routing it through the expression spine would repeat the very category
    /// error PB169 is.</summary>
    private BoundOperand? StatusOperand(Core.StatusPhraseContext sp)
    {
        if (sp.literal() is { } l)
            return ScreenStatusOperand(host.Expr.LiteralOperand(l), l.GetText());   // THE one literal→operand mapping (§8.3.3)
        if (sp.dataReference() is { } d)
        {
            var op = host.Expr.FieldOperand(d);
            // §13.18.38.3 r7 — the STATUS phrase is NOT one of the five contexts that may reference an
            // index-name (a subscript, PERFORM/SEARCH VARYING, SET, a relation condition). The R16 screen for
            // exactly this already existed and simply was not applied at this site.
            if (host.Expr.ScreenIndexNameOperand(op, d.GetText(), "the STOP RUN / GOBACK status operand"))
                return new BoundOperandError($"index-name '{d.GetText()}' in a termination-status phrase");
            return ScreenStatusOperand(op, d.GetText());
        }
        return null;   // WITH ERROR / NORMAL, no STATUS value — GR2/GR3's implementor indication
    }

    /// <summary>⛔ THE POSITION'S SCREEN, KEYED ON THE BOUND SHAPE — never on which parse arm produced it
    /// (kb/Work PB216). The first cut asked SR3/SR4 inside the <c>sp.literal()</c> arm and SR2 inside the
    /// <c>sp.dataReference()</c> arm, which is wrong on BOTH sides of the general format: §13.10.4 GR1 makes a
    /// constant-name's effect "as if literal-1 … were written where constant-name-1 is written", so
    /// <c>78 K VALUE 1.5.</c> + <c>STATUS K</c> arrives on the dataReference arm AS a numeric literal and skipped
    /// SR3 entirely (measured: it compiled clean and exited 1); and §12.3.7.4 GR11 makes a symbolic-character
    /// reference a figurative constant, likewise on the dataReference arm. One screen over the shapes the ONE
    /// §8.3.3 literal→operand mapping and <c>FieldOperand</c> can produce is what keeps the next literal form
    /// from finding a hole — the identifier arm had a completeness structure (<see cref="AdmittedStatusItem"/>)
    /// while the literal arm was two ad-hoc <c>is</c> patterns over a six-shape mapping, and that asymmetry WAS
    /// the defect generator.
    /// <para>⛔ WHAT IS *NOT* SCREENED IS THE POINT. §8.3.3.6.3 SR1 admits a figurative constant "whenever
    /// 'literal' appears in a format", narrowed only (a) where "the literal is restricted to a numeric literal"
    /// — §14.9.42.3 SR3's CONDITIONAL "if literal-1 is numeric" is the proof that it is not — and (b) where a
    /// syntax rule prohibits it; SR2/SR3/SR4 prohibit nothing about class. §8.3.3.6.4 GR3 NOTE 2 names the STOP
    /// statement BY NAME and gives the figurative a length of one character there. So <c>STATUS SPACE</c>,
    /// <c>STATUS ALL "5"</c> and <c>STATUS B"01"</c> are CONFORMING source: the remedy is to RENDER them under
    /// the GR5 mapping docs/CONFORMANCE.md item 192 already publishes, not to add a sixth rejection arm — which
    /// would mint the rejects-legal-source defect PB169 exists to close, one literal form over. Measured before
    /// the fix: each compiled clean and died at run time with NotImplementedCobolFeatureException.</para>
    /// <para>The one word the grammar carries here that is NOT a literal is <c>NULL</c>: §8.3.3.6.2 lists no NULL
    /// format (it is a predefined address / object reference, §8.4.3.10.1) and §8.4.3.10.3 SR1 confines it to an
    /// INITIALIZE/SET sending operand, a prototype argument, and a pointer-or-object-reference relation
    /// condition — the status slot is none of those.</para></summary>
    private BoundOperand ScreenStatusOperand(BoundOperand op, string text)
    {
        switch (op)
        {
            // SR3/SR7 — a NUMERIC literal-1 shall be an integer. (A non-numeric literal-1 is unconstrained here;
            // SR4/SR8 bars only the zero-length one.)
            case BoundNumericLiteral num when !IsIntegerLiteralText(num.Text):
                StatusError($"the numeric status literal '{text}' shall be an integer", "SR3", "SR7");
                break;
            // SR4/SR8 — literal-1 shall not be a zero-length literal (alphanumeric, national or boolean alike).
            case BoundStringLiteral { Value.Length: 0 }:
                StatusError("the status literal shall not be a zero-length literal", "SR4", "SR8");
                break;
            // NULL — neither identifier-1 nor literal-1 (§8.4.3.10.1/.3 SR1). Loud-named rather than rendered:
            // the predefined address has no character value the GR5 mapping could interpret.
            case BoundFigurative { Kind: 'N' }:
                ctx.Edition.Error(DiagnosticCatalog.TerminationStatusOperand,
                    "NULL is a predefined address / object reference, not a literal or an identifier, and ISO "
                    + "§8.4.3.10.3 SR1 admits it only as an INITIALIZE/SET sending operand, a prototype argument, "
                    + "or in a pointer-or-object-reference relation condition — not in a termination-status phrase");
                return new BoundOperandError("NULL in a termination-status phrase");
            // SR2/SR6 — an integer data item, OR a data item with usage display or usage national.
            case BoundFieldOperand { Place: var place } when !AdmittedStatusItem(place):
                StatusError($"the status operand '{text}' shall reference an integer data item or a data "
                    + "item with usage display or usage national", "SR2", "SR6");
                break;
        }
        return op;
    }

    /// <summary>§14.9.42.3 SR2 / §14.9.18.3 SR6, read as written: an INTEGER data item, or a data item whose
    /// USAGE is DISPLAY or NATIONAL. An index data item, a pointer and an object reference have their own usages
    /// and are excluded by name; an ALPHANUMERIC group is excluded because §8.5.2.1 gives it class alphanumeric
    /// with no elementary description of its own.
    /// <para>⛔ THROUGH <see cref="DataItem.OperandPic"/>, THE ONE OPERAND-CATEGORY READER (D20) — never
    /// <c>Pic</c> guarded by <c>IsGroup</c>, which is the spelling that reader's own doc comment forbids by name
    /// and which this screen used to have (kb/Work PB217). §13.18.29.3 SR3 implies USAGE NATIONAL for the subject
    /// of a GROUP-USAGE NATIONAL entry and §13.18.29.4 GR2 b makes such a group "treated as though it were an
    /// elementary data item of usage national … described with PICTURE N(m)" — so it IS "a data item with usage
    /// national" and SR2 admits it. Reading <c>OperandPic</c> settles all four group kinds with no hand-list: a
    /// national group is admitted, a BIT group (usage bit) is not, an alphanumeric group has no operand PICTURE
    /// and is not, and every elementary item behaves exactly as before.</para>
    /// <para>⛔ A REFERENCE-MODIFIED OPERAND IS ADMITTED ON ITS SUBJECT'S USAGE (kb/Work PB217). §8.4.3.3.3 SR5
    /// permits reference modification "anywhere an identifier referencing a data item of class alphanumeric,
    /// boolean, or national is permitted", and §8.4.3.3.4 GR6 gives the unique data item "the same class,
    /// category, and usage as that defined for identifier-1" — the three lettered exceptions rewrite class and
    /// category only, never USAGE. So SR2's SECOND alternative decides a slice, and its FIRST cannot: GR6 c has
    /// already removed category numeric (<see cref="RefModPlace.Category"/>, THE ONE GR6 reader, records that no
    /// ref-mod result is ever category NUMERIC), so a slice of <c>PIC 9(5)</c> DISPLAY is admitted as a
    /// display item and not as an integer one. A slice of a usage-BIT item or of an alphanumeric group stays
    /// rejected. The former blanket <c>p is RefModPlace</c> rejected <c>STATUS WS-CODE(1:2)</c> with a diagnostic
    /// quoting the very rule that admits it.</para></summary>
    private static bool AdmittedStatusItem(Place p)
    {
        if (p.Item.OperandPic is not { } pic) return false;
        if (pic.Usage is Usage.Index) return false;   // §8.5.2.1 Table 2 — class index, not a display/national item
        if (IntrinsicArgumentRules.ClassOfPlace(p) is CobolClass.Pointer or CobolClass.Object) return false;
        // §8.4.3.3.4 GR6 — a slice keeps identifier-1's USAGE and loses category numeric (GR6 c), so only SR2's
        // usage alternative can admit it.
        if (p is RefModPlace) return pic.Usage is Usage.Display or Usage.National;
        // An INTEGER data item (any usage — BINARY, PACKED, COMP-5 …) is the first alternative.
        if (pic is { Category: PicCategory.Numeric, Scale: 0, IsFloat: false }) return true;
        // …or a data item WITH USAGE DISPLAY OR NATIONAL, whatever its category (this is the alternative that
        // makes `WS-CODE PIC X(3)` legal, and the one the arithmetic funnel could not express).
        return pic.Usage is Usage.Display or Usage.National;
    }

    /// <summary>A numeric literal is an INTEGER when it carries no decimal separator and no exponent
    /// (§8.3.3.3.2 / §8.3.3.3 — the literal's written form decides; DECIMAL-POINT IS COMMA is normalized by
    /// <c>ExpressionBinder.CheckLiteral</c> before this, so only the dot form reaches here).</summary>
    private static bool IsIntegerLiteralText(string text) =>
        !text.Contains('.') && !text.Contains('E') && !text.Contains('e');

    private void StatusError(string what, string stopRule, string gobackRule) =>
        ctx.Edition.Error(DiagnosticCatalog.TerminationStatusOperand,
            $"{what} (ISO §14.9.42.3 {stopRule} for STOP RUN; §14.9.18.3 {gobackRule} for GOBACK)");

    /// <summary>CONTINUE [AFTER arithmetic-expression-1 SECONDS] (ISO §14.9.9). Plain CONTINUE is a 1985-continuous
    /// no-op (<see cref="BoundNop"/>). The AFTER … SECONDS timed-pause phrase (COBOL-2023, introduction-gated on the
    /// phrase by the VersionConformancePass) binds to a <see cref="BoundContinueAfter"/>. Whether
    /// EC-CONTINUE-LESS-THAN-ZERO checking is enabled at this statement is captured from the TurnState NOW (a bound
    /// node carries no parse line), so the runtime raises the nonfatal exception (GR1b) only under CHECKING ON.</summary>
    public BoundStatement BindContinue(Core.ContinueStatementContext cont)
    {
        if (cont.arithmeticExpression() is not { } secs) return new BoundNop();   // plain CONTINUE — a §14.9.9 no-op
        bool checkLtz = ctx.EcState.Turn.Enabled("EC-CONTINUE-LESS-THAN-ZERO", null, cont.Start.Line);
        return new BoundContinueAfter(host.Expr.BindExpr(secs), checkLtz);
    }

    public BoundStatement BindGoTo(Core.GoToStatementContext g)
    {
        var names = g.procedureName();
        if (g.dataReference() is { } sel && names.Length >= 1)   // GO TO p1 p2 … DEPENDING ON sel
        {
            var targets = new List<int>();
            foreach (var n in names)
            {
                // A section target transfers to its first paragraph (ISO §14.9.17 GR1).
                if (ctx.Table.ResolveProcedure(n) is not { } range) return new BoundUnsupported($"GO TO unknown procedure '{n.GetText()}'{host.OoScopeHint}");
                targets.Add(range.Start);
            }
            return new BoundGoToDepending(host.Expr.FieldOperand(sel), targets, ctx.SourceLine(g));
        }
        if (names.Length == 0) return host.Alter.AlterBindBareGoTo(g);   // the 85-only target-less GO TO (ALTER subsystem)
        if (ctx.Table.ResolveProcedure(names[0]) is not { } target)
            return new BoundUnsupported($"GO TO unknown procedure '{names[0].GetText()}'{host.OoScopeHint}");
        return host.Alter.AlterGoTo(g, target.Start);   // alterable when the owning paragraph is an ALTER target, else plain GO TO
    }

    public BoundStatement BindExit(Core.ExitStatementContext e)
    {
        if (e.PARAGRAPH() is not null) return new BoundExitParagraph(ctx.SourceLine(e));
        if (e.PERFORM() is not null) return new BoundExitPerform(e.CYCLE() is not null);
        if (e.PROGRAM() is not null)   // §14.9.14 GR2/GR3 — CONTINUE in a non-called program, return-to-caller in a called one (runtime-contextual)
        {
            if (host.InMethod)   // §14.9.14.3 SR7: EXIT PROGRAM only in a PROGRAM procedure division
            {
                ctx.Edition.Error("COBOLNET0827",
                    "EXIT PROGRAM may be specified only in a program procedure division, not in a method "
                    + "(ISO §14.9.14.3 SR7 — a method returns via GOBACK)");
                return new BoundNop();
            }
            if (e.raisingPhrase() is { } raising)   // Format 2's RAISING tail (§14.9.14.2) — re-raise in the activator
                return host.Ec.EcBindRaising(raising, e.Start.Line, "EXIT PROGRAM") is { } r
                    ? new BoundExitProgram(r)
                    : new BoundUnsupported("EXIT PROGRAM RAISING identifier (exception object — the OO wave; ISO §14.9.14.3)");
            return new BoundExitProgram();
        }
        if (e.SECTION() is not null)   // §14.9.14 Format 4, GR7 — transfer to the section's end (its return mechanism)
        {
            if (ctx.CurrentSection is not { } sec)   // §14.9.14.3 SR9 — EXIT SECTION may be specified only in a section
            {
                ctx.Edition.Error("COBOLNET0827",
                    "EXIT SECTION may be specified only in a section (ISO §14.9.14.3 SR9)");
                return new BoundNop();
            }
            return new BoundExitSection(sec.EndPc, ctx.SourceLine(e));
        }
        if (e.METHOD() is not null) return host.Oo.OoBindExitMethod(e);   // method-return synonym ≤2014; 0902 at 2023 (validator)
        if (e.FUNCTION() is not null) return host.Udf.UdfBindExitFunction(e);   // function-return synonym ≤2014; 0900/0902 window (validator)
        return new BoundNop();   // bare EXIT
    }


    public BoundStatement BindIf(Core.IfStatementContext iff)
    {
        var thenBlocks = new List<Core.StatementBlockContext>();
        var elseBlocks = new List<Core.StatementBlockContext>();
        bool seenElse = false;
        foreach (var child in StatementBinder.Children(iff))
        {
            if (child is ITerminalNode t && t.Symbol.Type == CobolLexer.ELSE) seenElse = true;
            else if (child is Core.StatementBlockContext sb) (seenElse ? elseBlocks : thenBlocks).Add(sb);
        }
        return new BoundIf(host.Cond.BindCondition(iff.condition()), host.BindBlocks(thenBlocks), host.BindBlocks(elseBlocks));
    }


    public BoundStatement BindPerform(Core.PerformStatementContext p)
    {
        var names = p.procedureName();
        if (names.Length == 0)
        {
            // Format 3 (exception-checking, §14.9.28.2 Format 3) — any WHEN phrase, or a [WITH] LOCATION head,
            // marks the inline PERFORM as exception-checking. Everything else is a Format-2 inline PERFORM.
            if (IsFormat3(p))
                return BindExceptionPerform(p);
            return new BoundInlinePerform(BindPerformControl(p), host.BindBlocks(p.statementBlock()));
        }

        // Out-of-line: the resolved pc range [start, end] — a paragraph (start==end), a SECTION (its whole
        // paragraph range, ISO §14.9.28 — first statement of its first paragraph through last of its last), or
        // the THRU composition (first procedure's start through the last procedure's end).
        if (ctx.Table.ResolveProcedure(names[0]) is not { } first)
            return new BoundUnsupported($"PERFORM unknown procedure '{names[0].GetText()}'{host.OoScopeHint}");
        (int start, int end) = first;
        if ((p.THRU() is not null || p.THROUGH() is not null) && names.Length >= 2)
        {
            if (ctx.Table.ResolveProcedure(names[1]) is not { } thru) return new BoundUnsupported($"PERFORM THRU unknown procedure '{names[1].GetText()}'{host.OoScopeHint}");
            // An INVERTED range (the THRU procedure physically precedes the first, reached by GO TO — NC102A
            // PFM-TEST-F1-10) is legal: the dispatcher returns when the exit procedure completes, wherever it is.
            end = thru.End;
        }
        else if (start > end)
            return new BoundNop();   // PERFORM of an EMPTY section runs nothing (no first statement, ISO §14.9.28)

        return new BoundOutOfLinePerform(start, end, BindPerformControl(p), ctx.SourceLine(p));
    }

    /// <summary>An inline PERFORM is Format 3 (exception-checking) iff it carries any WHEN phrase (ordinary /
    /// OTHER / COMMON), a FINALLY phrase, or a [WITH] LOCATION head (§14.9.28.2 Format 3). The ONE discriminator —
    /// the binder (here) and the COBOLNET0900 introduction gate (<c>VersionConformancePass.VisitPerformStatement</c>)
    /// share it, so the 0899↔0900 hand-off cannot drift (DEVLOG-724-class hazard).</summary>
    internal static bool IsFormat3(Core.PerformStatementContext p) =>
        p.performWhenPhrase().Length > 0 || p.performWhenOther() is not null
        || p.performWhenCommon() is not null || p.performFinally() is not null
        || p.performInlineHead()?.performLocationPhrase() is not null;

    /// <summary>Bind a Format-3 (exception-checking) PERFORM (ISO §14.9.28 Format 3) — delegated to the EC binder,
    /// which owns the WHEN-operand resolution, the GR14 TurnState overlay, and the §14.9.28.3 syntax rules /
    /// cross-statement bans.</summary>
    private BoundStatement BindExceptionPerform(Core.PerformStatementContext p) => host.Ec.EcBindExceptionPerform(p);

    /// <summary>Bind the OPTIONAL control phrase (TIMES / UNTIL / VARYING) of a PERFORM. Per ISO §14.9.28 the phrase
    /// is independent of the THRU range (general format: <c>PERFORM proc-1 [THRU proc-2] [times|until|varying]</c>),
    /// but the grammar exposes it in two shapes: a direct child (<c>PERFORM proc TIMES</c>, alternatives without
    /// THRU) or wrapped in <c>performOptions</c> (the <c>PERFORM proc THRU proc [performOptions]</c> alternative and
    /// the inline <c>performOptions+</c> form). Resolving only the direct child dropped the count/condition on a THRU
    /// range, silently running the range once instead of N times (§14.9.28 GR9) — the NC106A/NC176A defect
    /// (DEVLOG 514). This one resolver handles every shape for both inline and out-of-line PERFORM.</summary>
    private BoundPerformControl BindPerformControl(Core.PerformStatementContext p)
    {
        // The optional control phrase appears in three tree shapes: a direct child (the out-of-line
        // `PERFORM proc TIMES` alternatives), the THRU form's `performOptions?`, or the inline head's
        // `performInlineHead performOptions+` (the Formats-2/3 merge moved the inline options under the head).
        var opt = p.performOptions() ?? p.performInlineHead()?.performOptions().FirstOrDefault();
        if ((p.performTimes() ?? opt?.performTimes()) is { } t) return new PerformTimes(CountOperand(t));
        if ((p.performUntil() ?? opt?.performUntil()) is { } u)
        {
            // UNTIL EXIT (§14.9.28.4 GR11, 2023): an infinite loop (a condition that never becomes true). The
            // grammar gives EXIT its own alternative, so SR8's "no TEST with EXIT" is structural; escape is the
            // programmer's job (inline: EXIT PERFORM; out-of-line: GOBACK/STOP). Introduction-gated in the pass.
            if (u.EXIT() is not null) return new PerformForever();
            // The UNTIL condition is evaluated per iteration (§14.9.28 GR6/GR13), so a user-function
            // reference inside it activates per evaluation — the drained-suffix wrapper, never the
            // once-per-statement hoist (§8.4.3.2.4 GR1/GR6a; §8.8.4.13 r2).
            int udfMark = host.Udf.PendingCount;
            var cond = host.Cond.BindCondition(u.condition());
            return new PerformUntil(host.Udf.UdfAttachPerEvaluation(cond, udfMark), u.AFTER() is not null);
        }
        if ((p.performVarying() ?? opt?.performVarying()) is { } v) return BindVarying(v);
        return new PerformOnce();
    }

    /// <summary>Bind a VARYING phrase (ISO §14.9.28 Format 4) into its ordered induction levels — the VARYING
    /// level first, then each AFTER level left-to-right. TEST AFTER is the phrase's own <c>TEST AFTER</c> (the
    /// AFTER tokens of the after-levels live in their sub-contexts, not here).</summary>
    private BoundPerformControl BindVarying(Core.PerformVaryingContext v)
    {
        var levels = new List<VaryingLevel>();
        if (BindVaryingLevel(v.dataReference(), v.arithmeticExpression(), v.condition(), firstLevel: true) is not { } head)
            return Unsupported($"PERFORM VARYING induction variable '{v.dataReference().GetText()}'");
        levels.Add(head);
        foreach (var a in v.performVaryingAfter())
        {
            if (BindVaryingLevel(a.dataReference(), a.arithmeticExpression(), a.condition(), firstLevel: false) is not { } level)
                return Unsupported($"PERFORM VARYING AFTER induction variable '{a.dataReference().GetText()}'");
            levels.Add(level);
        }
        // §14.9.28.4 GR3: an index-name varied/AFTER from a data-item FROM whose value is non-positive raises the
        // fatal EC-RANGE-PERFORM-VARYING. Capture the enable flag NOW (F10/V3 template) so the emitter keeps the
        // directive-free output byte-identical (no check emitted when off).
        bool checkIndexRange = ctx.EcState.Turn.Enabled("EC-RANGE-PERFORM-VARYING", null, v.Start.Line);
        return new PerformVarying(levels, v.TEST() is not null && v.AFTER() is not null, checkIndexRange);
    }

    /// <summary>One induction level: the variable is a SET-style target (index-name or data item); the expression
    /// array is [FROM] or [FROM, BY] (BY omitted ⇒ augment 1, GR12). User-function evaluation cardinality per
    /// window (§8.4.3.2.4 GR1/GR6a): the UNTIL condition re-evaluates per iteration — its activations attach
    /// per-evaluation; a FIRST-level FROM evaluates exactly once at loop start (GR13a/GR13b init) — its
    /// activations stay statement-hoisted (exact); an AFTER-level FROM (re-evaluated on each outer augment,
    /// GR13e.2) and any BY (evaluated per augment, GR12) stage LOUD — the narrowed 1509 residue.</summary>
    private VaryingLevel? BindVaryingLevel(
        Core.DataReferenceContext dref, Core.ArithmeticExpressionContext[] exprs, Core.ConditionContext cond,
        bool firstLevel)
    {
        if (host.Set.SetTargetOf(dref) is not { } var) return null;
        int fromMark = host.Udf.PendingCount;
        BoundExpr from = host.Expr.BindIndexWindowExpr(exprs[0]);   // PERFORM VARYING is an r7 window (kb/Work R29)
        if (!firstLevel)
            host.Udf.UdfStagePerEvaluationResidue(fromMark,
                "a PERFORM VARYING AFTER level's FROM operand (re-evaluated per outer augment, §14.9.28 GR13e.2)");
        int byMark = host.Udf.PendingCount;
        BoundExpr by = exprs.Length > 1 ? host.Expr.BindIndexWindowExpr(exprs[1]) : new BoundNumLiteral("1");
        host.Udf.UdfStagePerEvaluationResidue(byMark,
            "a PERFORM VARYING BY operand (evaluated per augment, §14.9.28 GR12)");
        int untilMark = host.Udf.PendingCount;
        return new VaryingLevel(var, from, by,
            host.Udf.UdfAttachPerEvaluation(host.Cond.BindCondition(cond), untilMark));
    }

    private static BoundPerformControl Unsupported(string feature) => new PerformTimes(new BoundOperandError(feature));

    /// <summary>The TIMES count (§14.9.28.2 Format 2 — <c>{identifier-1 | integer-1}</c>): an integer literal, a
    /// function-identifier (§8.4.3.2.4 GR1 — the FUNCTION spelling, or the keyword-omitted form under FUNCTION ALL
    /// INTRINSIC, which <see cref="ExpressionBinder.FieldOperand"/> already resolves), or a data reference. §14.9.28.3
    /// SR2 "Identifier-1 shall be an integer" is enforced HERE for every shape through the ONE integer classifier
    /// (<see cref="IntrinsicResultType.IsIntegerOperand"/>): a scale-0 numeric elementary item that is not USAGE
    /// INDEX, or a function whose resolved type is integer (§15.2 type 5). kb/Work PB86: a non-integer item was
    /// accepted and its unscaled digits iterated; a function count ran once.</summary>
    private BoundOperand CountOperand(Core.PerformTimesContext t)
    {
        BoundOperand op =
            t.integerLiteral() is { } lit ? new BoundNumericLiteral(lit.GetText())
            : t.functionCall() is { } fc ? host.Intrinsic.IntrinsicOperand(fc)
            : t.dataReference() is { } d ? host.Expr.FieldOperand(d)
            : new BoundOperandError("PERFORM … TIMES count shape (ISO §14.9.28.2 Format 2)");
        if (op is not BoundOperandError && !IntrinsicResultType.IsIntegerOperand(op))
            ctx.Edition.Error(DiagnosticCatalog.PerformTimesCountNotInteger,
                $"PERFORM ... TIMES count '{(t.functionCall() ?? (Antlr4.Runtime.ParserRuleContext?)t.dataReference())?.GetText()}' "
                + "shall be an integer (ISO §14.9.28.3 SR2) — an integer data item, an integer literal, or an "
                + "integer-type function-identifier (§15.2 type 5)");
        return op;
    }
}
