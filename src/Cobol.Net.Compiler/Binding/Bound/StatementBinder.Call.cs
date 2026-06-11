// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using CobolSharp.Compiler.Generated;

namespace CobolNet.Binding.Bound;

using Core = CobolParserCore;

// ── Bound nodes — CALL / CANCEL / EXIT PROGRAM / GOBACK (ISO §14.9.4 / §14.9.5 / §14.9.14 / §14.9.18;
//    COBOLNET_INTERPROGRAM_DESIGN D1–D4) ────────────────────────────────────────────────────────────────────────

/// <summary>One CALL USING argument: its resolved pass mode (the §14.9.4.4 GR5 transitivity already applied at
/// bind time), and either a resolved <see cref="Place"/> (a data-reference argument) or a bound
/// <see cref="Value"/> operand (a literal — inherently BY CONTENT — or a BY VALUE expression, §14.9.4.3 SR4).</summary>
public sealed record BoundCallArg(CobolPassMode Mode, Place? Place, BoundOperand? Value);

/// <summary><c>CALL {literal|identifier} [USING …] [RETURNING …] [ON …][NOT ON …]</c> (ISO §14.9.4 Format 1).
/// <paramref name="LiteralName"/> is the static target (SR2 — a non-zero-length alphanumeric literal);
/// <paramref name="DynamicName"/> the runtime-resolved identifier target (GR3b). The exception phrases carry
/// the bound imperatives; the OVERFLOW-vs-EXCEPTION spelling is edition-gated at bind time and semantically
/// identical here (at 85 the only exception condition IS the resolution failure the OVERFLOW phrase catches).</summary>
public sealed record BoundCallProgram(
    string? LiteralName,
    BoundOperand? DynamicName,
    IReadOnlyList<BoundCallArg> Args,
    Place? Returning,
    IReadOnlyList<BoundStatement>? OnException,
    IReadOnlyList<BoundStatement>? NotOnException) : BoundStatement;

/// <summary><c>CANCEL {literal|identifier}…</c> (ISO §14.9.5): each target's next CALL finds its initial state
/// (GR3); contained programs cascade in reverse source order (GR4); open files close implicitly (GR9).</summary>
public sealed record BoundCancel(
    IReadOnlyList<(string? LiteralName, BoundOperand? DynamicName)> Targets) : BoundStatement;

/// <summary><c>EXIT PROGRAM</c> (ISO §14.9.14 Format 2): in a program NOT under the control of a calling runtime
/// element it is equivalent to CONTINUE (GR2); in a called program it returns to the activator per the GOBACK
/// rules (GR3). The distinction is a RUNTIME property of the activation, so the bound node is unconditional and
/// the emitted code tests the activation flag. (Archaic at 2023 — Annex F.1; flagged, not rejected.)</summary>
public sealed record BoundExitProgram : BoundStatement;

/// <summary><c>GOBACK [RETURNING x]</c> (ISO §14.9.18): terminates the executing program — return to the caller
/// in a called program (GR2), STOP-equivalent in a main program (GR3). <paramref name="ReturningSource"/> moves
/// into the procedure-division RETURNING item before return (the activation result, GR2). COBOL-2002+.</summary>
public sealed record BoundGoback(Place? ReturningSource) : BoundStatement;

/// <summary>
/// The CALL / inter-program slice of the binder (ISO §14.9.4 / §14.9.5 / §14.9.18; deep-dive design
/// COBOLNET_INTERPROGRAM_DESIGN). All semantic resolution happens here ONCE — target form, the GR5 transitive
/// pass-mode threading, the receiving-operand checks, and the per-edition gates (the G1 four-compilers rule:
/// the complete behavior where the edition HAS the construct, a targeted COBOLNET-08xx diagnostic where it
/// lacks it — see the deep-dive "Edition gating" section).
/// </summary>
public sealed partial class StatementBinder
{
    /// <summary>Bind <c>CALL</c> (ISO §14.9.4 Format 1; design D2 — the uniform opaque ABI call).</summary>
    private BoundStatement CallBindCall(Core.CallStatementContext call)
    {
        // ── Target: literal (static) or identifier (dynamic, resolved at run time — GR3b) ──
        string? literalName = null;
        BoundOperand? dynamicName = null;
        var target = call.callTarget();
        if (target.literal() is { } lit)
        {
            if (lit.nonNumericLiteral()?.STRINGLIT() is { } s)
                literalName = DecodeCobolString(s.GetText());
            else
                return new BoundUnsupported(
                    $"CALL with a non-alphanumeric literal target '{lit.GetText()}' (ISO §14.9.4.3 SR2)");
            if (literalName.Length == 0)
                return new BoundUnsupported("CALL with a zero-length literal target (ISO §14.9.4.3 SR2)");
        }
        else if (target.dataReference() is { } dref)
        {
            if (refs.Resolve(dref) is not { } place)
                return new BoundUnsupported($"CALL target '{dref.GetText()}'");
            dynamicName = new BoundFieldOperand(place);
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
                if (refs.Resolve(byRef.dataReference()) is not { } p)
                    return new BoundUnsupported($"CALL USING argument '{byRef.dataReference().GetText()}'");
                args.Add(new BoundCallArg(CobolPassMode.Reference, p, null));
            }
            else if (a.callByContent() is { } byContent)
            {
                mode = CobolPassMode.Content;
                if (byContent.literal() is { } clit)
                    args.Add(new BoundCallArg(CobolPassMode.Content, null, LiteralOperand(clit)));
                else if (byContent.dataReference() is { } cdref && refs.Resolve(cdref) is { } cp)
                    args.Add(new BoundCallArg(CobolPassMode.Content, cp, null));
                else
                    return new BoundUnsupported($"CALL USING BY CONTENT argument '{byContent.GetText()}'");
            }
            else if (a.callByValue() is { } byValue)
            {
                // BY VALUE was introduced by ISO/IEC 1989:2002 (§14.9.4; deep-dive "Edition gating").
                if (data.Edition.DialectLevel < 2002)
                    data.Edition.Error("COBOLNET0883",
                        "CALL … BY VALUE was introduced by ISO/IEC 1989:2002 (§14.9.4) — requires --std 2002 or "
                        + $"later (targeting COBOL-{data.Edition.DialectLevel})");
                mode = CobolPassMode.Value;
                args.Add(new BoundCallArg(CobolPassMode.Value, null,
                    new BoundComputedOperand(BindExpr(byValue.arithmeticExpression()))));
            }
            else if (a.dataReference() is { } bare)
            {
                // A bare argument takes the prevailing transitive mode (GR5).
                if (refs.Resolve(bare) is not { } bp)
                    return new BoundUnsupported($"CALL USING argument '{bare.GetText()}'");
                args.Add(new BoundCallArg(mode, bp, null));
            }
        }

        // ── RETURNING (CALL side) — COBOL-2002+ (deep-dive "Edition gating"); maps to the activation result
        //    delivered through the opaque ABI's returning carrier (§14.2.3 GR7). ──
        Place? returning = null;
        if (call.callReturningPhrase() is { } rp)
        {
            if (data.Edition.DialectLevel < 2002)
                data.Edition.Error("COBOLNET0884",
                    "CALL … RETURNING was introduced by ISO/IEC 1989:2002 (§14.9.4) — requires --std 2002 or "
                    + $"later (targeting COBOL-{data.Edition.DialectLevel})");
            if (refs.Resolve(rp.dataReference()) is not { } rpl)
                return new BoundUnsupported($"CALL RETURNING '{rp.dataReference().GetText()}'");
            returning = rpl;
        }

        // ── Exception phrases — edition-gated spellings (deep-dive "Edition gating"; VERSION_CHANGE_REFERENCE
        //    row 3): ON OVERFLOW is the 85 form, REMOVED at 2023; ON EXCEPTION / NOT ON EXCEPTION are 2002+;
        //    at 2002/2014 both spellings are accepted and equivalent. ──
        List<BoundStatement>? onExc = null, notOnExc = null;
        if (call.callOnExceptionPhrase() is { } onp)
        {
            CallGateExceptionSpelling(isOverflow: onp.OVERFLOW() is not null, negated: false);
            onExc = BindBlocks([onp.statementBlock()]);
        }
        if (call.callNotOnExceptionPhrase() is { } notp)
        {
            CallGateExceptionSpelling(isOverflow: notp.OVERFLOW() is not null, negated: true);
            notOnExc = BindBlocks([notp.statementBlock()]);
        }

        return new BoundCallProgram(literalName, dynamicName, args, returning, onExc, notOnExc);
    }

    /// <summary>Edition-gate one CALL exception-phrase spelling: ON OVERFLOW is valid 85–2014 and REMOVED at
    /// 2023 (VERSION_CHANGE_REFERENCE row 3 / E.2 item 1c); [NOT] ON EXCEPTION is 2002+; COBOL-85 has no NOT
    /// phrase at all (its §14.9.4 format is <c>[ON OVERFLOW imperative]</c> only).</summary>
    private void CallGateExceptionSpelling(bool isOverflow, bool negated)
    {
        int level = data.Edition.DialectLevel;
        if (isOverflow && level >= 2023)
            data.Edition.Error("COBOLNET0882",
                "CALL … ON OVERFLOW was removed by ISO/IEC 1989:2023 (Annex E.2 item 1c) — use ON EXCEPTION, or "
                + "target --std 85/2002/2014");
        if (!isOverflow && level < 2002)
            data.Edition.Error("COBOLNET0881",
                "CALL … ON EXCEPTION was introduced by ISO/IEC 1989:2002 (§14.9.4) — COBOL-85 has only "
                + $"ON OVERFLOW; requires --std 2002 or later (targeting COBOL-{level})");
        if (negated && level < 2002)
            data.Edition.Error("COBOLNET0881",
                "the NOT ON EXCEPTION/OVERFLOW phrase was introduced by ISO/IEC 1989:2002 (§14.9.4) — the "
                + $"COBOL-85 CALL has no NOT phrase; requires --std 2002 or later (targeting COBOL-{level})");
    }

    /// <summary>Bind <c>CANCEL {literal|identifier}…</c> (ISO §14.9.5 — targets resolved like CALL's, §8.4.6.3).</summary>
    private BoundStatement CallBindCancel(Core.CancelStatementContext cancel)
    {
        var targets = new List<(string?, BoundOperand?)>();
        foreach (var t in cancel.cancelTarget())
        {
            if (t.literal() is { } lit)
            {
                if (lit.nonNumericLiteral()?.STRINGLIT() is { } s)
                    targets.Add((DecodeCobolString(s.GetText()), null));
                else
                    return new BoundUnsupported($"CANCEL non-alphanumeric literal target '{lit.GetText()}' (ISO §14.9.5.2 SR1)");
            }
            else if (t.dataReference() is { } dref)
            {
                if (refs.Resolve(dref) is not { } p)
                    return new BoundUnsupported($"CANCEL target '{dref.GetText()}'");
                targets.Add((null, new BoundFieldOperand(p)));
            }
        }
        return new BoundCancel(targets);
    }

    /// <summary>Bind <c>GOBACK [RETURNING x]</c> (ISO §14.9.18). GOBACK itself was introduced by ISO/IEC
    /// 1989:2002 — at <c>--std 85</c> it is rejected with a targeted diagnostic (the G1 lacks-it obligation;
    /// COBOL-85 programs use STOP RUN / EXIT PROGRAM). The 2023-only WITH ERROR/NORMAL STATUS phrase is not in
    /// the grammar yet (VERSION_CHANGE_REFERENCE row 75 — a later slice with the §12 RETURN-CODE wiring).</summary>
    private BoundStatement CallBindGoback(Core.GobackStatementContext g)
    {
        if (data.Edition.DialectLevel < 2002)
            data.Edition.Error("COBOLNET0880",
                "GOBACK was introduced by ISO/IEC 1989:2002 (§14.9.16 there; §14.9.18 in 2023) — COBOL-85 uses "
                + $"STOP RUN / EXIT PROGRAM; requires --std 2002 or later (targeting COBOL-{data.Edition.DialectLevel})");
        Place? source = null;
        if (g.dataReference() is { } dref)
        {
            // GOBACK RETURNING x ≡ move x into the procedure-division RETURNING item, then return (§14.9.18 GR2
            // — the activation result; the grammar already 2002-gates the phrase).
            if (refs.Resolve(dref) is not { } p)
                return new BoundUnsupported($"GOBACK RETURNING '{dref.GetText()}'");
            source = p;
        }
        return new BoundGoback(source);
    }
}
