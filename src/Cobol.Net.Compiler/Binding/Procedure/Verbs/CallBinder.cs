// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;
using CobolNet.Runtime;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>
/// The CALL / inter-program slice of the binder (ISO §14.9.4 / §14.9.5 / §14.9.18; deep-dive design
/// COBOLNET_INTERPROGRAM_DESIGN). All semantic resolution happens here ONCE — target form, the GR5 transitive
/// pass-mode threading, the receiving-operand checks, and the per-edition gates (the G1 four-compilers rule:
/// the complete behavior where the edition HAS the construct, a targeted COBOLNET-08xx diagnostic where it
/// lacks it — see the deep-dive "Edition gating" section). P7 Step 10j: a real collaborator over
/// <see cref="BinderContext"/>; the two hooks that outlive the batch stay HOST edges — <c>InMethod</c>/
/// <c>OoBindMethodGoback</c> (the OO half converts LAST, 10s) and <c>EcBindRaising</c> (EcBinder lands 10r).
/// The 0884/0880 gates moved VERBATIM incl. the 0880 bare-GOBACK null-condition that encodes the
/// 0900-subsumption contract with the VersionConformancePass. The five bound types stayed in
/// <c>Binding/Bound/BoundCall.cs</c>.</summary>
internal sealed class CallBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>Bind <c>CALL</c> (ISO §14.9.4 Format 1; design D2 — the uniform opaque ABI call).</summary>
    public BoundStatement BindCall(Core.CallStatementContext call)
    {
        // ── §14.9.4.2 FORMAT 2: the AS phrase (fix-queue PB46, CALL half) ────────────────────────────────────
        // `CALL {identifier-1 | literal-1} AS {NESTED | program-prototype-name-1}`. The presence of AS is what
        // selects Format 2 — a SYNTACTIC discriminator, contrary to this item's own note, which had concluded the
        // formats were indistinguishable at parse time and the whole half therefore blocked on P13.
        // The brace has TWO arms with DIFFERENT dependencies, and only one of them needs the prototype registry.
        bool formatTwo = call.callAsPhrase() is not null;
        bool asNested = false;
        if (call.callAsPhrase() is { } asPhrase)
        {
            string asWord = asPhrase.cobolWord().GetText();
            asNested = string.Equals(asWord, "NESTED", StringComparison.OrdinalIgnoreCase);
            if (!asNested)
            {
                // §14.9.4.3 SR16: "Program-prototype-name-1 shall be specified in a program-specifier in the
                // REPOSITORY paragraph." The REPOSITORY grammar has no `PROGRAM program-prototype-name` entry
                // (§12.3.8.2's program-specifier), so no source can declare one — this arm is genuinely blocked
                // on the P13 prototype registry, and says so by name instead of failing as an unresolved call.
                ctx.Edition.Error(DiagnosticCatalog.CallAsPrototypeName,
                    $"CALL … AS {asWord}: a program-prototype-name shall be declared by a PROGRAM specifier in "
                    + "the REPOSITORY paragraph (ISO §14.9.4.3 SR16 / §12.3.8.2), which this compiler does not "
                    + "yet accept — the program-prototype registry is P13. `AS NESTED` is supported.");
                return new BoundNop();
            }
            // §14.9.4.3 SR13: the NESTED phrase may be specified only in a program definition.
            // §14.9.4.3 SR15: literal-1 shall be specified, and shall name a COMMON program or a program
            // directly contained in the calling program.
            if (call.callTarget().literal() is null)
            {
                ctx.Edition.Error(DiagnosticCatalog.CallAsNestedNeedsLiteral,
                    "CALL … AS NESTED: literal-1 shall be specified — the NESTED phrase names a contained or "
                    + "COMMON program by its PROGRAM-ID literal, not through an identifier (ISO §14.9.4.3 SR15)");
                return new BoundNop();
            }
        }
        _ = formatTwo;   // read below by the USING binder for the Format-2 BY CONTENT operand set

        // ── Target: literal (static) or identifier (dynamic, resolved at run time — GR3b) ──
        string? literalName = null;
        BoundOperand? dynamicName = null;
        bool isPointerTarget = false;   // identifier-1 is a PROGRAM-POINTER item (§14.9.4.3 SR1; P10 Step 7)
        var target = call.callTarget();
        if (target.literal() is { } lit)
        {
            // kb/Work PB130: through the ONE program-name-literal reader — SR2 admits alphanumeric AND
            // national literals (a hexadecimal literal IS §8.3.3.2 Format 2 of an alphanumeric one), and the
            // old STRINGLIT-only read sent N"P" / X".." to a run-time loud on legal source.
            if (ProgramNameLiteral(lit, "CALL", "§14.9.4.3 SR2") is not { } pn) return new BoundNop();
            literalName = pn;
        }
        else if (target.dataReference() is { } dref)
        {
            if (ctx.Refs.Resolve(dref) is not { } place)
                return new BoundUnsupported($"CALL target '{dref.GetText()}'");
            dynamicName = new BoundFieldOperand(place);
            // §14.9.4.3 SR1 (:26082): identifier-1 may be alphanumeric, national, OR a PROGRAM-POINTER item —
            // a pointer target activates the HELD program (GR :26177) through ProgramRegistry.CallPointer
            // instead of a name-string read (P10 Step 7).
            if (place.Item.Pic?.Category is PicCategory.ProgramPointer) isPointerTarget = true;
        }
        else
            return new BoundUnsupported("CALL target form");

        // ── USING arguments — the §14.9.4.4 GR5 TRANSITIVE pass mode: BY REFERENCE is assumed before the
        //    first phrase; each explicit BY REFERENCE / BY CONTENT / BY VALUE phrase applies to every following
        //    bare argument until the next phrase (the legacy CallBinder mode-threading, re-derived from GR5). ──
        var args = new List<BoundCallArg>();
        var mode = CobolPassMode.Reference;
        foreach (var a in call.callUsingPhrase()?.callArgument() ?? [])
        {
            if (a.callByReference() is { } byRef)
            {
                mode = CobolPassMode.Reference;
                // §14.9.4.2 Format 2: `[BY REFERENCE] {identifier-2 | OMITTED}` (kb/Work PB130). The omitted-
                // argument carrier joins the OPTIONAL-formal landing (PB133); until then the recognized form
                // draws the same staged diagnostic family instead of a parse error on legal source.
                if (byRef.OMITTED() is not null)
                {
                    ctx.Edition.Error(DiagnosticCatalog.OptionalFormal,
                        "CALL … USING BY REFERENCE OMITTED (ISO §14.9.4.2 Format 2 / §14.9.4.4 GR11): the "
                        + "omitted-argument carrier lands with OPTIONAL formal parameters (kb/Work PB133)");
                    return new BoundNop();
                }
                if (byRef.dataReference() is not { } byRefDref)
                    return new BoundUnsupported("CALL USING BY REFERENCE form");
                // kb/Work PB128: a BY REFERENCE argument is a RECEIVING operand and rides the ONE receiving
                // chokepoint — the direct Refs.Resolve bypass skipped the CONSTANT RECORD (§13.18.15.3 SR2),
                // CAPACITY-register (§13.18.38 SR30–32), constant-name and LINE-COUNTER screens, letting a
                // structured constant be silently overwritten by the callee (and a CAPACITY register reach
                // PlaceRenderer.Write's internal throw — an unhandled compiler exception).
                if (host.Expr.ResolveReceiving(byRefDref) is not { } p)
                    return new BoundUnsupported($"CALL USING argument '{byRefDref.GetText()}'");
                args.Add(new BoundCallArg(CobolPassMode.Reference, p, null));
            }
            else if (a.callByContent() is { } byContent)
            {
                mode = CobolPassMode.Content;
                // ── ONE OPERAND, THREE CHANNELS — normalized once, exactly as OoBindInvokeArg does ──
                // The grammar parses BY CONTENT wide (both formats share this rule). A B-operator-FREE
                // booleanExpression reduces to its bare valueOperand, so the predicate decides only which NODE
                // an operand lands in, never what it means.
                var cBool = byContent.booleanExpression();
                var cArith = byContent.arithmeticExpression();
                var cLit = byContent.literal();
                if (cBool is not null && ConditionBinder.UnwrapBareBool(cBool) is { } cBare)
                {
                    cBool = null;
                    cArith = cBare.arithmeticExpression();
                }
                // The grammar keeps a `dataReference` arm (legacy shares this rule), so a bare identifier lands
                // there directly; the sole-reference reduction still covers one that arrived inside an
                // expression node — the two paths must agree, which is why both are consulted here.
                var cDref = byContent.dataReference() ?? ConditionBinder.SoleDataReference(cArith);
                // §14.9.4.2 FORMAT 1's BY CONTENT IS `{ identifier-2 } …` AND NOTHING ELSE. An expression operand
                // is legal only under Format 2, which the AS phrase selects — so accepting one here without that
                // phrase would admit illegal source, the exact trade this item refused to make in the grammar.
                if (!formatTwo && (cBool is not null || cLit is not null || (cArith is not null && cDref is null)))
                {
                    ctx.Edition.Error(DiagnosticCatalog.CallContentOperandFormat,
                        $"CALL … USING BY CONTENT {byContent.GetText()}: an expression operand belongs to the "
                        + "program-prototype CALL (ISO §14.9.4.2 Format 2), which the AS phrase selects. "
                        + "Format 1's BY CONTENT admits `{ identifier-2 } …` only.");
                    return new BoundNop();
                }
                if (cDref is not null && ctx.Refs.Probe(cDref) is { } cp)   // Probe — the cArith arm below is the
                    args.Add(new BoundCallArg(CobolPassMode.Content, cp, null));   // legal alternative and its bind demands (R30)
                else if (cLit is { } clit)
                    args.Add(new BoundCallArg(CobolPassMode.Content, null, host.Expr.LiteralOperand(clit)));
                else if (cArith is { } cax)
                    // Format-2 arithmetic-expression-1: bind through the ONE expression path and pass its value.
                    args.Add(new BoundCallArg(CobolPassMode.Content, null,
                        IntrinsicBinder.OperandOf(host.Expr.BindExpr(cax))));
                else if (cBool is not null)
                {
                    ctx.Edition.Error(DiagnosticCatalog.CallContentOperandFormat,
                        "CALL … USING BY CONTENT <boolean-expression>: §14.9.4.2 Format 2 admits it and the "
                        + "boolean value channel does not yet cross a CALL boundary (the INVOKE side landed as "
                        + "PB46's BoundInvokeArg.ContentBool; the CALL argument model has no counterpart)");
                    return new BoundNop();
                }
                else
                    return new BoundUnsupported($"CALL USING BY CONTENT argument '{byContent.GetText()}'");
            }
            else if (a.callByValue() is { } byValue)
            {
                // §14.9.4.2 FORMAT 1 HAS NO BY VALUE ARM (kb/Work PB130): its USING brace prints BY REFERENCE
                // and BY CONTENT only (the repaired figure notes’ required-word list has no VALUE), and
                // SR21–SR23 sit under Format 2. Accepting it here passed a GR5-impossible mode.
                if (!formatTwo)
                {
                    ctx.Edition.Error(DiagnosticCatalog.CallContentOperandFormat,
                        "CALL … USING BY VALUE belongs to the program-prototype CALL (ISO §14.9.4.2 Format 2), "
                        + "which the AS phrase selects — Format 1’s USING admits BY REFERENCE and BY CONTENT only");
                    return new BoundNop();
                }
                // BY VALUE (§14.9.4) is a COBOL-2002 introduction; the edition gate moved to the post-bind
                // VersionConformancePass (Step 14c), firing on a BoundCallProgram whose args use value passing.
                mode = CobolPassMode.Value;
                // ⛔ BindByValueExpr, NOT BindExpr — §14.9.4.3 SR22 governs a BY VALUE operand, not §8.8.1.1.
                // The production is named arithmeticExpression and binding it as arithmetic put DA6's
                // §8.8.1.1 screen on it, so an alphanumeric operand was refused with a message about arithmetic
                // expressions and a "digit-decoding extension" — the right verdict quoting the wrong rule.
                var byValueOperand = new BoundComputedOperand(
                    host.Expr.BindByValueExpr(byValue.arithmeticExpression()));
                CheckByValueClass(byValue, byValueOperand);
                args.Add(new BoundCallArg(CobolPassMode.Value, null, byValueOperand));
            }
            else if (a.dataReference() is { } bare)
            {
                // A bare argument takes the prevailing transitive mode (GR5) — Reference by default, so it
                // is receiving-capable and rides the chokepoint too (kb/Work PB128).
                if (host.Expr.ResolveReceiving(bare) is not { } bp)
                    return new BoundUnsupported($"CALL USING argument '{bare.GetText()}'");
                args.Add(new BoundCallArg(mode, bp, null));
            }
            // §14.9.4.2 Format 2's keyword-less non-identifier arguments (kb/Work PB130): literal-2,
            // arithmetic-expression-1, boolean-expression-1 and OMITTED all print bare in Format 2 (its BY
            // phrases are plain brackets — GR9 a)2 exists precisely for the non-identifier bare argument);
            // Format 1's bare argument is identifier-2 only, so each arm narrows on formatTwo. A bare
            // non-identifier crosses BY CONTENT semantics (a value, never a writeback carrier).
            else if (a.OMITTED() is not null)
            {
                ctx.Edition.Error(DiagnosticCatalog.OptionalFormal,
                    "CALL … USING OMITTED (ISO §14.9.4.2 Format 2 / §14.9.4.4 GR11): the omitted-argument "
                    + "carrier lands with OPTIONAL formal parameters (kb/Work PB133)");
                return new BoundNop();
            }
            else if (a.literal() is { } bLit)
            {
                if (!formatTwo) { BareNeedsFormat2(bLit.GetText()); return new BoundNop(); }
                args.Add(new BoundCallArg(CobolPassMode.Content, null, host.Expr.LiteralOperand(bLit)));
            }
            else if (a.booleanExpression() is { } bBool)
            {
                if (!formatTwo) { BareNeedsFormat2(bBool.GetText()); return new BoundNop(); }
                ctx.Edition.Error(DiagnosticCatalog.CallContentOperandFormat,
                    "CALL … USING <boolean-expression> (§14.9.4.2 Format 2): the boolean value channel does "
                    + "not yet cross a CALL boundary (the INVOKE side landed as PB46's ContentBool; the CALL "
                    + "argument model has no counterpart — kb/Work PB131)");
                return new BoundNop();
            }
            else if (a.arithmeticExpression() is { } bArith)
            {
                // A parenthesized sole reference reduces to its identifier (the callByContent discipline).
                if (ConditionBinder.SoleDataReference(bArith) is { } sd)
                {
                    if (host.Expr.ResolveReceiving(sd) is not { } sp)
                        return new BoundUnsupported($"CALL USING argument '{sd.GetText()}'");
                    args.Add(new BoundCallArg(mode, sp, null));
                }
                else if (!formatTwo) { BareNeedsFormat2(bArith.GetText()); return new BoundNop(); }
                else
                    args.Add(new BoundCallArg(CobolPassMode.Content, null,
                        IntrinsicBinder.OperandOf(host.Expr.BindExpr(bArith))));
            }
        }

        void BareNeedsFormat2(string text) =>
            ctx.Edition.Error(DiagnosticCatalog.CallContentOperandFormat,
                $"CALL … USING {text}: a keyword-less literal or expression argument belongs to the "
                + "program-prototype CALL (ISO §14.9.4.2 Format 2), which the AS phrase selects — Format 1's "
                + "bare argument is identifier-2 only");

        // ── RETURNING (CALL side) — COBOL-2002+ (deep-dive "Edition gating"); maps to the activation result
        //    delivered through the opaque ABI's returning carrier (§14.2.3 GR7). ──
        Place? returning = null;
        if (call.callReturningPhrase() is { } rp)
        {
            // call-returning-2002: the VersionConformancePass owns the edition gate (Exec Step E).
            // kb/Work PB128: identifier-3 is a pure receiver — the chokepoint's screens apply.
            if (host.Expr.ResolveReceiving(rp.dataReference()) is not { } rpl)
                return new BoundUnsupported($"CALL RETURNING '{rp.dataReference().GetText()}'");
            returning = rpl;
        }

        // §14.9.4.3 SR11/SR18 — a CALL argument (BY REFERENCE or BY CONTENT) and the CALL RETURNING item shall
        // not be described with the ANY LENGTH clause: without a program-prototype the activated program's
        // formal cannot be proven ANY LENGTH (the §13.18.2.3 SR2 NOTE), so passing a runtime-length item onward
        // through CALL is banned outright (INVOKE permits it — §14.8.2.3.2 rule e pairs it with an ANY LENGTH
        // method formal).
        foreach (var a in args)
            if (a.Place is { Item.IsAnyLength: true } ap)
                ctx.Edition.Error("COBOLNET1542", $"CALL USING argument '{ap.Item.CobolName}' is described "
                    + "with the ANY LENGTH clause (ISO §14.9.4.3 SR11 — a CALL argument shall not be ANY LENGTH)");
        if (returning is { Item.IsAnyLength: true } anyRet)
            ctx.Edition.Error("COBOLNET1542", $"CALL RETURNING item '{anyRet.Item.CobolName}' is described "
                + "with the ANY LENGTH clause (ISO §14.9.4.3 SR18)");

        // ── Exception phrases — edition-gated spellings (deep-dive "Edition gating"; VERSION_CHANGE_REFERENCE
        //    row 3): [NOT] ON EXCEPTION is ANSI X3.23-1985 surface (CALL Format 2; CCVS-85 IC222A tests both
        //    phrases — "'ON OVERFLOW' CAN BE USED IN PLACE OF 'ON EXCEPTION'"); ON OVERFLOW is the 74-carried
        //    synonym, accepted 85–2014 and REMOVED at 2023 (Annex E.2 item 1c). ──
        List<BoundStatement>? onExc = null, notOnExc = null;
        bool usedOverflow = false;
        // The two phrases live under ONE container (callExceptionPhrases) so either order parses — ISO 5.2.6.4
        // choice indicators. Order of WRITING does not change binding: each phrase keeps its own role.
        var excPhrases = call.callExceptionPhrases();
        if (excPhrases?.callOnExceptionPhrase() is { } onp)
        {
            usedOverflow |= onp.OVERFLOW() is not null;
            onExc = host.BindBlocks([onp.statementBlock()]);
        }
        if (excPhrases?.callNotOnExceptionPhrase() is { } notp)
        {
            usedOverflow |= notp.OVERFLOW() is not null;
            notOnExc = host.BindBlocks([notp.statementBlock()]);
        }

        // ON OVERFLOW is the COBOL-74-carried synonym for ON EXCEPTION, REMOVED at ISO 2023; the edition gate
        // (CallOnOverflowRemoved2023) moved to the post-bind VersionConformancePass (Step 14d), reading the flag.
        return new BoundCallProgram(literalName, dynamicName, args, returning, onExc, notOnExc)
        {
            UsedOverflowSpelling = usedOverflow,
            IsPointerTarget = isPointerTarget,
        };
    }

    /// <summary>Bind <c>CANCEL {literal|identifier}…</c> (ISO §14.9.5 — targets resolved like CALL's, §8.4.6.3).</summary>
    /// <summary>The ONE program-name-literal reader (kb/Work PB130) — §14.9.4.3 SR2 / §14.9.5.3 SR2 admit
    /// an alphanumeric OR national literal (a hexadecimal literal is §8.3.3.2 Format 2 of the alphanumeric
    /// kind; a concatenation folds per §8.8.3.3 GR3; the D-N1 identity repertoire makes the national name
    /// the same string). Reports the SR violation itself and returns null — a boolean literal, a numeric
    /// literal, and a zero-length name each draw the cited diagnostic instead of a run-time loud.</summary>
    private string? ProgramNameLiteral(Core.LiteralContext lit, string verb, string clause)
    {
        if (host.Expr.NonNumericLiteralOperand(lit.nonNumericLiteral()) is BoundStringLiteral
            { Category: PicCategory.Alphanumeric or PicCategory.National } sl)
        {
            if (sl.Value.Length != 0) return sl.Value;
            ctx.Edition.Error(DiagnosticCatalog.IntrinsicArgumentClass,
                $"{verb} with a zero-length literal program name (ISO {clause})");
            return null;
        }
        ctx.Edition.Error(DiagnosticCatalog.IntrinsicArgumentClass,
            $"{verb} '{lit.GetText()}': the literal program name shall be an alphanumeric or national literal "
            + $"(ISO {clause})");
        return null;
    }

    public BoundStatement BindCancel(Core.CancelStatementContext cancel)
    {
        var targets = new List<(string?, BoundOperand?)>();
        foreach (var t in cancel.cancelTarget())
        {
            if (t.literal() is { } lit)
            {
                // kb/Work PB130: the ONE reader (national + hex admitted per §14.9.5.3 SR2 — the old
                // rejection also miscited §14.9.5.2 SR1); a bad target reports and the LOOP CONTINUES, so a
                // statement with one illegal target no longer discards its legal ones.
                if (ProgramNameLiteral(lit, "CANCEL", "§14.9.5.3 SR2") is { } pn)
                    targets.Add((pn, null));
            }
            else if (t.dataReference() is { } dref)
            {
                if (ctx.Refs.Resolve(dref) is { } p)
                    targets.Add((null, new BoundFieldOperand(p)));
                // an unresolved name was already diagnosed by the resolver; keep binding the rest
            }
        }
        return new BoundCancel(targets);
    }

    /// <summary>Bind <c>GOBACK [RETURNING x]</c> (ISO §14.9.18). GOBACK itself was introduced by ISO/IEC
    /// 1989:2002 — at <c>--std 85</c> it is rejected with a targeted diagnostic (the G1 lacks-it obligation;
    /// COBOL-85 programs use STOP RUN / EXIT PROGRAM). The 2023 WITH ERROR/NORMAL STATUS phrase (§14.9.18.2,
    /// VERSION_CHANGE_REFERENCE row 75) parses through the shared <c>statusPhrase</c> rule and is introduction-
    /// gated by the VersionConformancePass (GobackStatus2023); it binds presence-only here (the status VALUE →
    /// exit-code wiring is the shared STOP+GOBACK termination-status slice, matching the presence-only STOP sibling).</summary>
    public BoundStatement BindGoback(Core.GobackStatementContext g)
    {
        if (host.InMethod) return host.Oo.OoBindMethodGoback(g);   // §14.9.18.4 GR4 — a METHOD return, never an activation return (D8)
        // goback-bare-2002 / goback-returning-2002: the VersionConformancePass owns both edition gates
        // (Exec Step E folded the bare-GOBACK 0880; the RETURNING 0900 subsumption lives in its parse arm).
        Place? source = null;
        if (g.dataReference() is { } dref)
        {
            // GOBACK RETURNING x ≡ move x into the procedure-division RETURNING item, then return (§14.9.18 GR2
            // — the activation result; the grammar already 2002-gates the phrase).
            if (ctx.Refs.Resolve(dref) is not { } p)
                return new BoundUnsupported($"GOBACK RETURNING '{dref.GetText()}'");
            source = p;
        }
        if (g.raisingPhrase() is { } raising)
            return host.Ec.EcBindRaising(raising, g.Start.Line, "GOBACK") is { } r
                ? new BoundGoback(source, r)
                : new BoundUnsupported("GOBACK RAISING identifier (exception object — the OO wave; ISO §14.9.18.3 SR4)");
        // GOBACK … WITH {NORMAL|ERROR} STATUS [value] (§14.9.18.2, COBOL-2023, 2023-gated in the pass; mutually
        // exclusive with RAISING by the grammar). Decode the shared statusPhrase into the termination status; the
        // emit passes it to the OS only in a MAIN program (§14.9.18.4 GR3/GR10 — a called-program status is inert).
        return new BoundGoback(source, null, host.ControlFlow.BindTerminationStatus(g.statusPhrase()));
    }

    /// <summary>
    /// ISO §14.9.4.3 SR22 — a BY VALUE operand "shall be of class numeric, object, or pointer".
    /// </summary>
    /// <remarks>
    /// <para>
    /// Strict-reject with the leniency dialect-gated, the disposition DA6 established for the sibling §8.8.1.1
    /// question. GnuCOBOL accepts an alphanumeric operand as an extension and silently assumes BY CONTENT — a
    /// DIFFERENT passing mode from the one written, which is why accepting it quietly is the wrong kindness: the
    /// callee would receive an address where the source said value.
    /// </para>
    /// <para>
    /// ⚠ Fail-open on an undecidable class, exactly as the intrinsic screen does: a false reject turns legal
    /// COBOL away, a missed one leaves a rule unenforced, and only the first is forbidden outright.
    /// </para>
    /// </remarks>
    private void CheckByValueClass(Core.CallByValueContext byValue, BoundComputedOperand operand)
    {
        // ⛔ CLASSIFY THE UNDERLYING REFERENCE, NOT THE WRAPPER. IntrinsicArgumentRules.ClassOf maps any
        // BoundComputedOperand to NUMERIC — correct in its own context, where a computed operand really is an
        // arithmetic expression — so asking it about the wrapper made this check a silent no-op. The first
        // version did exactly that and turned a wrongly-worded REJECT into a clean ACCEPT, which looks like a fix
        // and is a regression: the rule stopped being enforced at all. A bare identifier binds to BoundNumRef, so
        // its Place is the thing SR22 is about.
        if (operand.Expr is not BoundNumRef { Place: { } place }) return;
        if (IntrinsicArgumentRules.ClassOf(new BoundFieldOperand(place)) is not { } actual) return;
        if (actual is CobolClass.Numeric or CobolClass.Object or CobolClass.Pointer) return;

        string what = byValue.arithmeticExpression().GetText();
        string rule = $"CALL … USING BY VALUE operand '{what}' is of class {actual.ToString().ToLowerInvariant()}; "
            + "ISO §14.9.4.3 SR22 admits only class numeric, object or pointer by value";
        if (ctx.Edition.Permissive)
        {
            ctx.Edition.Warning(DiagnosticCatalog.CallByValueOperandClass,
                $"{rule}; accepted under --permissive");
        }
        else
        {
            ctx.Edition.Error(DiagnosticCatalog.CallByValueOperandClass,
                $"{rule}. --permissive accepts it as an extension");
        }
    }
}
