// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions;
using CobolNet.Frontend.Generated;
using CobolNet.Runtime;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>
/// User-defined function invocation (ISO §9.4 / §8.4.3.2; M2-UDF-1 — the PHASE4_RECONCILIATION
/// decision-complete design). A <c>FUNCTION user-name(args)</c> reference LOWERS at bind time onto the
/// program-activation machinery the group already has: a caller-side result temporary cloned from the
/// callee's RETURNING description (§8.4.3.2.4 GR1 :6963), a <see cref="BoundCallProgram"/> = CALL
/// "name" USING «args» RETURNING «temp» registered on a statement-scoped pending list, and the reading
/// expression/operand over the temp. <see cref="UdfWrapCalls"/> drains the list at the BindStatement
/// chokepoint into a <see cref="BoundSequence"/> that HOISTS each activation before the carrying
/// statement — always a PRE-op, because a function-identifier is never a receiving operand
/// (§8.4.3.2.3 SR1), so no store-polarity classification is needed (unlike property references).
/// Argument evaluation order (§8.4.3.2.4 GR2 — left to right, nested function-identifiers allowed) falls
/// out of registration order: a nested call registers while its consumer's arguments bind, so it
/// precedes the consumer in the sequence. A hoist is EXACT only where the reference is unconditionally
/// evaluated exactly once per statement execution — §8.8.4.13 r2 ties function evaluation to "if and
/// when the conditions containing them are evaluated", so conditionally-evaluated positions (short-
/// circuited combined-condition operands, EVALUATE selection, re-evaluated loop conditions) stage LOUD
/// (COBOLNET1509) rather than silently over/under-evaluating. Emission is 100% existing surface:
/// <c>CallEmitCall</c> → <c>ProgramRegistry.CallProgram</c>; FUNCTION-ID units already emit as callable
/// program classes with the RETURNING carrier.
/// P7 Step 10k: a real collaborator over <see cref="BinderContext"/>, landed TOGETHER with
/// <see cref="IntrinsicBinder"/> (the argument parse reaches back into its <c>ParseArgSegment</c>). The
/// host.UserFunctions/host.UdfSelfName injection surface STAYS on the StatementBinder host (BinderDriver's
/// object-initializer contract — re-homed at 10t); the statement-scoped <c>_udfPendingCalls</c> mark/drain
/// suffix protocol is exposed through <see cref="PendingCount"/> for the host's BindStatement /
/// BindFlatSequence chokepoints. The line-65 <c>ConstructRegistry.Check</c> stays THE documented bind-time
/// gate exception (fires on RECOGNITION, pre-hoist), moved VERBATIM.</summary>
internal sealed class UdfBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>THIS statement's not-yet-hoisted function activations (statement-scoped: BindStatement
    /// marks the count on entry and drains only its own suffix — the property-op discipline).</summary>
    private readonly List<BoundCallProgram> _udfPendingCalls = [];

    /// <summary>The pending-list mark for the host chokepoints (the suffix-drain protocol).</summary>
    internal int PendingCount => _udfPendingCalls.Count;

    /// <summary>Bind one user-function reference (the <see cref="BindIntrinsicCore"/> dispatch target for a
    /// REPOSITORY-declared name, which per §12.3.8.2 GR12 refers to the user function and never a same-named
    /// intrinsic): resolve the signature, bind the arguments in the §8.4.3.2.4 GR5 manner, synthesize the
    /// result temporary, register the hoisted activation, and return the temp-reading expression.</summary>
    internal BoundExpr UdfBindCall(string name, List<IToken> argTokens)
    {
        // INTRODUCTION gate: user-defined functions are COBOL-2002+ (§9.4 / §12.3.8; 0900 below 2002). It fires on
        // RECOGNITION — a below-2002 UDF reference is an edition violation independent of whether the function is
        // DEFINED (a locate miss is 1505 / EC-FUNCTION-NOT-FOUND). A bound-arm gate on the hoisted BoundCallProgram
        // loses it when the reference errors before the hoist (UdfInvocationTests.BinderGate_0900_At85), so it stays
        // BIND-TIME here until Step 14h moves ALL introduction gates to the presence-based post-bind parse-arm
        // (CI-red fix, 2026-07-09).
        ConstructRegistry.Check(ctx.Edition.Edition, ctx.Edition, Constructs.UserFunctionInvocation2002,
            $"FUNCTION {name.ToUpperInvariant()}");

        if (host.UserFunctions is null || !host.UserFunctions.TryGetValue(name, out var fn))
        {
            ctx.Edition.Error("COBOLNET1505",
                $"FUNCTION {name.ToUpperInvariant()} is declared in the REPOSITORY paragraph but the compilation "
                + "group contains neither a FUNCTION-ID definition nor a FUNCTION-ID … IS PROTOTYPE for it — "
                + "declare a function prototype (ISO §11.5 / §12.3.8 SR10) so its signature is available for a "
                + "separately-compiled target, or provide the definition in this group (function references from "
                + "class units remain a separate follow-up)");
            return new BoundExprError($"FUNCTION {name}");
        }
        if (fn.Returning is null)
            // Ill-formed function definition — COBOLNET1507 already reported once at the unit.
            return new BoundExprError($"FUNCTION {name} RETURNING");

        // Staged RETURNING categories (§1.4 — loud, never silently wrong): the result reads through
        // BoundNumRef, whose category classifiers and relation rendering are NUMERIC. A group RETURNING
        // would clone a Pic-less childless temp (an undeclarable field), and an alphanumeric/edited/boolean
        // result would COMPARE numerically in conditions — both fail loud by name until the
        // category-carrying result channel lands (M2-UDF follow-up).
        if (fn.Returning.IsGroup || fn.Returning.Pic is not { Category: PicCategory.Numeric, IsFloat: false })
        {
            ctx.Edition.Error("COBOLNET1510",
                $"FUNCTION {name.ToUpperInvariant()}: only an elementary fixed-point numeric RETURNING item "
                + "is implemented for user-defined function references — a group / alphanumeric / edited / "
                + "float result is a named follow-up (the result temporary's category channel, ISO "
                + "§8.4.3.2.4 GR1)");
            return new BoundExprError($"FUNCTION {name} RETURNING category");
        }

        // Arguments: split the flat token stream on depth-0 separators (the ONE splitter intrinsics and
        // subscripts share) and parse each segment. NO table(ALL) expansion here — §9.4 (:12529):
        // "arguments and returned values for user-defined functions may not use the word ALL as a
        // subscript" (an ALL token reaches ParseArgSegment and fails as a loud named operand).
        var operands = new List<BoundOperand>();
        foreach (var segment in ReferenceResolver.SplitSubscriptTokens(argTokens))
        {
            if (segment.All(t => t.Type == Core.SUB_WS)) continue;
            operands.Add(host.Intrinsic.ParseArgSegment(segment));
        }

        // Positional correspondence (§14.8.2): one argument per USING formal. OPTIONAL/OMITTED formals are
        // not modeled for functions — an exact-count mismatch is the honest loud stop.
        if (operands.Count != fn.Formals.Count)
        {
            ctx.Edition.Error("COBOLNET1506",
                $"FUNCTION {name.ToUpperInvariant()} takes {fn.Formals.Count} argument(s); {operands.Count} "
                + "given — arguments correspond positionally to the function's PROCEDURE DIVISION USING "
                + "formals (ISO §14.8.2)");
            return new BoundExprError($"FUNCTION {name} arity");
        }

        var callArgs = new List<BoundCallArg>(operands.Count);
        for (int i = 0; i < operands.Count; i++)
        {
            if (UdfArg(operands[i]) is not { } arg)
            {
                // Name the ACTUAL unsupported shape when the segment parser already classified it (a
                // reference-modified argument, a figurative, an unresolvable name) — never a message
                // claiming a legal form is illegal.
                string what = operands[i] is BoundOperandError err
                    ? err.Feature
                    : "this argument form (an identifier, a literal, or an arithmetic expression is "
                      + "supported — ISO §8.4.3.2.4 SR8/GR5)";
                ctx.Edition.Error("COBOLNET1506",
                    $"FUNCTION {name.ToUpperInvariant()} argument {i + 1}: {what} is not yet supported for "
                    + "user-defined function activation");
                return new BoundExprError($"FUNCTION {name} argument {i + 1}");
            }
            callArgs.Add(arg);
        }

        // The caller-side result temporary (§8.4.3.2.4 GR1 :6963 — "the description, class, and category of
        // the temporary data item is that specified by the description in the linkage section of the item
        // specified in the RETURNING phrase"), declared like any other item via the Roots pipeline.
        var temp = ctx.Data.CreateCompilerTemp(fn.Returning, "__FNRES-", "__fnres", name);
        if (ctx.Refs.ResolveItem(temp) is not { } tempPlace)
            return new BoundExprError($"FUNCTION {name} result temporary");

        _udfPendingCalls.Add(new BoundCallProgram(fn.Name, null, callArgs, tempPlace, null, null) { IsFunction = true });
        return new BoundNumRef(tempPlace);
    }

    /// <summary>One bound argument in its §8.4.3.2.4 GR5 manner: (a) an identifier permitted as a receiving
    /// operand, with the formal's BY REFERENCE implied ⇒ BY REFERENCE over the caller's storage; (b) a
    /// literal or arithmetic expression ⇒ a private-copy cell (the runtime <c>CobolArgAdapt</c> profile
    /// adaptation realizes the §14.2.3 GR9 copy-in conformance to the formal — same-scale cells alias, a
    /// scale difference gets the rescaling view). Header BY VALUE formals (GR5c) are not modeled for
    /// functions. Null = unsupported operand form (the caller reports).</summary>
    private static BoundCallArg? UdfArg(BoundOperand op) => op switch
    {
        BoundFieldOperand f => new BoundCallArg(CobolPassMode.Reference, f.Place, null),
        BoundNumericLiteral or BoundStringLiteral or BoundComputedOperand
            => new BoundCallArg(CobolPassMode.Content, null, op),
        _ => null,
    };

    /// <summary>Drain THIS statement's pending function activations (registered while the statement bound)
    /// into the hoisted <see cref="BoundSequence"/>: every activation is a PRE-op — a function-identifier
    /// is never a receiving operand (§8.4.3.2.3 SR1), so unlike property references there is no polarity
    /// classification and no post-ops. Runs INSIDE the property-op wrap at the BindStatement chokepoint, so
    /// a property-reference argument's GET (a pre-op of the OUTER wrap) still precedes the activation that
    /// consumes its temp.</summary>
    internal BoundStatement UdfWrapCalls(BoundStatement core, int mark)
    {
        var calls = _udfPendingCalls;
        if (calls.Count <= mark) return core;
        var taken = calls.GetRange(mark, calls.Count - mark);
        calls.RemoveRange(mark, calls.Count - mark);

        // Evaluation-cardinality guard (§1.4 — loud, never silently wrong). A once-hoisted activation is
        // exact only when the reference is evaluated exactly once per statement execution. Statements whose
        // condition/operand window re-evaluates or conditionally evaluates stage loud: a PERFORM
        // UNTIL/VARYING condition (or FROM/BY operand) and a SEARCH WHEN condition re-evaluate per
        // iteration/pass (§14.9.28 / §14.9.37); EVALUATE selection evaluates subjects once but objects only
        // until a WHEN satisfies (§14.9.13 — and this backend's chained-selection lowering re-renders
        // subject expressions per WHEN, so subjects guard too). Body statements are safe — they bind through
        // their own BindStatement and drain their own suffix; only the statement's OWN window reaches here.
        // (TIMES counts and a sole IF condition evaluate exactly once — the hoist is exact for those;
        // short-circuited combined-condition operands are guarded at BindFlatSequence, §8.8.4.13.)
        if (core is BoundInlinePerform { Control: not (PerformOnce or PerformTimes) }
            or BoundOutOfLinePerform { Control: not (PerformOnce or PerformTimes) }
            or BoundSearch or BoundEvaluate)
            ctx.Edition.Error("COBOLNET1509",
                "a user-defined function reference in a PERFORM UNTIL/VARYING phrase, a SEARCH WHEN "
                + "condition, or an EVALUATE selection requires per-evaluation activation (ISO §14.9.28 / "
                + "§14.9.37 / §14.9.13; §8.8.4.13 r2) — not yet implemented; move the reference to a "
                + "preceding COMPUTE (M2-UDF follow-up)");

        return new BoundSequence([.. taken, core]);
    }

    /// <summary>EXIT FUNCTION (pre-2023 editions — introduced 2002 with user functions, REMOVED by 2023,
    /// Annex E.2 :49036; the <c>exit-function-window</c> registry row flags 0900/0902 at the window edges
    /// via the version-conformance pass, mirroring EXIT METHOD): inside a function definition it is the
    /// function-return synonym — equivalent to GOBACK (the §14.9.18.4 GR5 semantics: the activation
    /// terminates and the RETURNING item's value becomes the function result); outside one it violates its
    /// placement rule (the 0827 EXIT-family placement band). The optional RAISING tail stages exactly like
    /// GOBACK RAISING (§14.9.18 GR — re-raised in the activator).</summary>
    internal BoundStatement UdfBindExitFunction(Core.ExitStatementContext e)
    {
        if (host.UdfSelfName is null)
        {
            ctx.Edition.Error("COBOLNET0827",
                "EXIT FUNCTION may be specified only in a function definition (the pre-2023 §14.9.14 "
                + "function form of the EXIT statement; this is not a function procedure division)");
            return new BoundNop();
        }
        if (e.raisingPhrase() is { } raising)
            return host.Ec.EcBindRaising(raising, e.Start.Line, "EXIT FUNCTION") is { } r
                ? new BoundGoback(null, r)
                : new BoundUnsupported("EXIT FUNCTION RAISING identifier (exception object — ISO §14.9.14)");
        return new BoundGoback(null);
    }

    /// <summary>The §8.8.4.13 short-circuit guard for combined conditions: rule 1 terminates a hierarchical
    /// level's evaluation as soon as its truth value is determined, and rule 2 evaluates functions "if and
    /// when the conditions containing them are evaluated" — so a user-function reference in any operand
    /// AFTER the first of an AND/OR chain is CONDITIONALLY evaluated and a once-hoisted activation would
    /// over-evaluate it. Called by BindFlatSequence with the pending count marked before each non-first
    /// operand binds (XOR is exempt — both operands are always required). Loud staging, per §1.4.</summary>
    internal void UdfGuardConditionalOperand(int mark, string op)
    {
        if (_udfPendingCalls.Count <= mark || op == "^") return;
        ctx.Edition.Error("COBOLNET1509",
            "a user-defined function reference in a non-first operand of an AND/OR combined condition is "
            + "evaluated only if the earlier operands do not determine the truth value (ISO §8.8.4.13 r1/r2 "
            + "short-circuit) — a hoisted activation cannot honor that; move the reference to a preceding "
            + "COMPUTE (M2-UDF follow-up)");
    }
}
