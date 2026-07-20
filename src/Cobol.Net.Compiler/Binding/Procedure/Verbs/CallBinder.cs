// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
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
        // ── Target: literal (static) or identifier (dynamic, resolved at run time — GR3b) ──
        string? literalName = null;
        BoundOperand? dynamicName = null;
        bool isPointerTarget = false;   // identifier-1 is a PROGRAM-POINTER item (§14.9.4.3 SR1; P10 Step 7)
        var target = call.callTarget();
        if (target.literal() is { } lit)
        {
            if (lit.nonNumericLiteral()?.STRINGLIT() is { } s)
                literalName = CobolLiteral.Decode(s.GetText());
            // §8.8.3.3 GR3: an alphanumeric concatenation expression may stand anywhere an alphanumeric
            // literal may — including the §14.9.4 literal-1 program name; fold it at compile time.
            else if (lit.nonNumericLiteral()?.concatenationExpression() is { } ce
                     && ConcatFolder.ClassOf(ce) is PicCategory.Alphanumeric)
                literalName = ConcatFolder.Fold(ce, ctx.Edition, ctx.Data.Collating).Value;
            else
                return new BoundUnsupported(
                    $"CALL with a non-alphanumeric literal target '{lit.GetText()}' (ISO §14.9.4.3 SR2)");
            if (literalName.Length == 0)
                return new BoundUnsupported("CALL with a zero-length literal target (ISO §14.9.4.3 SR2)");
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
                if (ctx.Refs.Resolve(byRef.dataReference()) is not { } p)
                    return new BoundUnsupported($"CALL USING argument '{byRef.dataReference().GetText()}'");
                args.Add(new BoundCallArg(CobolPassMode.Reference, p, null));
            }
            else if (a.callByContent() is { } byContent)
            {
                mode = CobolPassMode.Content;
                if (byContent.literal() is { } clit)
                    args.Add(new BoundCallArg(CobolPassMode.Content, null, host.Expr.LiteralOperand(clit)));
                else if (byContent.dataReference() is { } cdref && ctx.Refs.Resolve(cdref) is { } cp)
                    args.Add(new BoundCallArg(CobolPassMode.Content, cp, null));
                else
                    return new BoundUnsupported($"CALL USING BY CONTENT argument '{byContent.GetText()}'");
            }
            else if (a.callByValue() is { } byValue)
            {
                // BY VALUE (§14.9.4) is a COBOL-2002 introduction; the edition gate moved to the post-bind
                // VersionConformancePass (Step 14c), firing on a BoundCallProgram whose args use value passing.
                mode = CobolPassMode.Value;
                args.Add(new BoundCallArg(CobolPassMode.Value, null,
                    new BoundComputedOperand(host.Expr.BindExpr(byValue.arithmeticExpression()))));
            }
            else if (a.dataReference() is { } bare)
            {
                // A bare argument takes the prevailing transitive mode (GR5).
                if (ctx.Refs.Resolve(bare) is not { } bp)
                    return new BoundUnsupported($"CALL USING argument '{bare.GetText()}'");
                args.Add(new BoundCallArg(mode, bp, null));
            }
        }

        // ── RETURNING (CALL side) — COBOL-2002+ (deep-dive "Edition gating"); maps to the activation result
        //    delivered through the opaque ABI's returning carrier (§14.2.3 GR7). ──
        Place? returning = null;
        if (call.callReturningPhrase() is { } rp)
        {
            // call-returning-2002: the VersionConformancePass owns the edition gate (Exec Step E).
            if (ctx.Refs.Resolve(rp.dataReference()) is not { } rpl)
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
    public BoundStatement BindCancel(Core.CancelStatementContext cancel)
    {
        var targets = new List<(string?, BoundOperand?)>();
        foreach (var t in cancel.cancelTarget())
        {
            if (t.literal() is { } lit)
            {
                if (lit.nonNumericLiteral()?.STRINGLIT() is { } s)
                    targets.Add((CobolLiteral.Decode(s.GetText()), null));
                // §8.8.3.3 GR3: an alphanumeric concatenation expression stands anywhere an alphanumeric
                // literal may — including the §14.9.5 literal-1 program name.
                else if (lit.nonNumericLiteral()?.concatenationExpression() is { } ce
                         && ConcatFolder.ClassOf(ce) is PicCategory.Alphanumeric)
                    targets.Add((ConcatFolder.Fold(ce, ctx.Edition, ctx.Data.Collating).Value, null));
                else
                    return new BoundUnsupported($"CANCEL non-alphanumeric literal target '{lit.GetText()}' (ISO §14.9.5.2 SR1)");
            }
            else if (t.dataReference() is { } dref)
            {
                if (ctx.Refs.Resolve(dref) is not { } p)
                    return new BoundUnsupported($"CANCEL target '{dref.GetText()}'");
                targets.Add((null, new BoundFieldOperand(p)));
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
}
