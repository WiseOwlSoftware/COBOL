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
/// <see cref="IntrinsicBinder"/> (the argument bind reaches back into its <c>BindArgOperand</c>). The
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
    internal BoundExpr UdfBindCall(string name, IReadOnlyList<Core.FunctionArgumentContext> argCtxs)
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

        // The category-carrying result channel (§8.4.3.2.4 GR1 — the temp's "description, class, and
        // category" ARE the RETURNING item's; §14.2.2 SR5 places NO category restriction on a function's
        // RETURNING item): elementary fixed-point numeric, alphanumeric/alphabetic, numeric-edited, and
        // national results — and a character-form GROUP (its full subtree cloned into the temp, the image
        // crossing the boundary via AsImage/FromImage) — are carried end-to-end: the temp clones the full
        // description, every operand chokepoint maps the read to a BoundFieldOperand (category drives
        // MOVE Table-16 legality, relation class dispatch, DISPLAY rendering, and the LENGTH fold), and
        // the CALL-ABI RETURNING delivery ships strings through CobolArgAdapt.StoreReturn(string). The
        // remaining shapes stay STAGED by name (§1.4 — loud, never silently wrong): see UdfReturningResidue.
        if (UdfReturningResidue(fn.Returning) is { } residue)
        {
            ctx.Edition.Error("COBOLNET1510",
                $"FUNCTION {name.ToUpperInvariant()}: {residue} (the result temporary's category channel, "
                + "ISO §8.4.3.2.4 GR1 / §14.8.3)");
            return new BoundExprError($"FUNCTION {name} RETURNING category");
        }

        // Arguments: one typed operand per argument parse tree, through the SAME BindArgOperand the intrinsic
        // path uses (the ONE argument pipeline). NO table(ALL) expansion here — §9.4 (:12529): "arguments and
        // returned values for user-defined functions may not use the word ALL as a subscript" (an ALL subscript
        // fails resolution and stays a loud named operand). OMITTED arguments (§14.8.2 OPTIONAL formals) are
        // not modeled for functions — a staged loud stop, never a silent skip (§1.4).
        var operands = new List<BoundOperand>();
        foreach (var a in argCtxs)
        {
            if (a.OMITTED() is not null)
            {
                ctx.Edition.Error("COBOLNET1506",
                    $"FUNCTION {name.ToUpperInvariant()}: an OMITTED argument requires an OPTIONAL formal "
                    + "parameter (ISO §14.8.2) — OPTIONAL/OMITTED formals are not modeled for user-defined "
                    + "function activation (M2-UDF follow-up)");
                return new BoundExprError($"FUNCTION {name} OMITTED argument");
            }
            operands.Add(host.Intrinsic.BindArgOperand(a));
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
        // specified in the RETURNING phrase"), declared like any other item via the Roots pipeline. A GROUP
        // model deep-clones its subtree (CreateCompilerTemp's group leg) so the FieldEmitter declares the
        // struct with its AsImage/FromImage codec — the CALL boundary's group crossing form.
        var temp = ctx.Data.CreateCompilerTemp(fn.Returning, "__FNRES-", "__fnres", name);
        if (ctx.Refs.ResolveItem(temp) is not { } tempPlace)
            return new BoundExprError($"FUNCTION {name} result temporary");

        _udfPendingCalls.Add(new BoundCallProgram(fn.Name, null, callArgs, tempPlace, null, null) { IsFunction = true });
        // The reading expression: a BoundNumRef over the temp's Place. Every general-operand chokepoint
        // (MOVE source, DISPLAY, relation operands, function arguments — IntrinsicBinder.OperandOf) maps it
        // to a BoundFieldOperand, whose Place.Item carries the cloned category into Table-16 legality, the
        // relation class dispatch, and the LENGTH fold. In a genuinely NUMERIC context (COMPUTE/arithmetic)
        // the temp behaves exactly like a same-category data item — the §14.9.25.4 GR6 unsigned-integer
        // decode for alphanumeric/national, the GR5 de-edit for numeric-edited (NumericRenderer.FieldNum).
        return new BoundNumRef(tempPlace);
    }

    /// <summary>The staged-loud RETURNING shapes (COBOLNET1510) — null when the category-carrying result
    /// channel handles the description end-to-end. ISO §14.2.2 SR5 places NO category restriction on a
    /// function's RETURNING item (any level-01/77 LINKAGE entry without BASED/REDEFINES), and §8.4.3.2.4 GR1
    /// clones its description into the caller temp — so every shape named here is an IMPLEMENTATION residue
    /// staged loud (§1.4), never a conformance rule. Supported: elementary fixed-point numeric,
    /// alphanumeric/alphabetic, numeric-edited, national; character-form groups (every leaf character-stored
    /// or fixed-point numeric DISPLAY — the leaves the whole-group image promotion covers). Staged: FLOAT
    /// (the CALL-boundary string carrier has no float write half — CallEmitter.CallStringWrite renders a raw
    /// string store into the double field), BOOLEAN (the §8.8.2 boolean-expression channel has no
    /// function-result arm — admitting it would half-wire: MOVE/relations would work while IF f(x) and
    /// COMPUTE Format 2 would not), pointer/object/index classes (§8.8.4-restricted reference sets), and the
    /// group residues (strongly-typed identity, internal REDEFINES, variable-length, non-character leaves).</summary>
    private static string? UdfReturningResidue(DataItem ret)
    {
        if (ret.IsGroup)
        {
            for (var stack = new Stack<DataItem>([ret]); stack.Count > 0;)
            {
                var d = stack.Pop();
                if (d.StrongType)
                    return "a strongly-typed group RETURNING item is not yet carried — the §8.5.3.3 same-type "
                        + "identity does not survive the caller-side temp clone (ISO §14.8.3.2; a named residue)";
                if (!ReferenceEquals(d, ret) && d.RedefinesTargetName is not null)
                    return "a group RETURNING item containing an internal REDEFINES is not yet carried — the "
                        + "temp clone precedes no REDEFINES re-classification (ISO §13.18.44; a named residue)";
                if (d.OccursSpec is { } os && (os.DependingName is not null || os.IsDynamic))
                    return "a variable-length (OCCURS DEPENDING/DYNAMIC CAPACITY) group RETURNING item is not "
                        + "yet carried (ISO §8.5.1.12 / §14.8.3.2 variable-length-group conformance; a named residue)";
                if (d.IsElementary && d.Pic is not (
                        { Category: PicCategory.Alphanumeric or PicCategory.NumericEdited
                            or PicCategory.National or PicCategory.Boolean }
                        or { Category: PicCategory.Numeric, IsFloat: false, Usage: Usage.Display }))
                    return $"a group RETURNING item with a non-character leaf ('{d.CobolName ?? "FILLER"}') is "
                        + "not yet carried — binary/packed/COMP-5/float/index/pointer/object leaves have no "
                        + "shared character image across the activation boundary (the Tier-C island, "
                        + "COBOLNET_DESIGN §4.2; a named residue)";
                foreach (var c in d.Children) stack.Push(c);
            }
            return null;   // a character-form group — the implemented group leg
        }
        return ret.Pic switch
        {
            null => "an undescribed RETURNING item",   // childless, PICTURE-less — already diagnosed at the entry
            { Category: PicCategory.Numeric, IsFloat: true } =>
                "a FLOAT RETURNING item (COMP-1/COMP-2/FLOAT-SHORT/-LONG/-EXTENDED) is not yet carried — the "
                + "CALL-boundary string carrier has no float write half (ISO §14.8.3.3 usage-identical "
                + "conformance; the float-RETURNING descriptor, a named residue)",
            { Category: PicCategory.Numeric, Usage: Usage.Index } =>
                "an index RETURNING item is not a carried result category (ISO §13.18.60 GR10 — only SET, "
                + "SEARCH, and relation conditions reference class index; a named residue)",
            { Category: PicCategory.Numeric } => null,                 // the original fixed-point leg
            { Category: PicCategory.Alphanumeric } => null,           // alphanumeric + alphabetic (§8.4.2 class alphanumeric)
            { Category: PicCategory.NumericEdited } => null,          // the edited image carries as its mask'd string
            { Category: PicCategory.National } => null,               // rides the Step-5 national category channel
            { Category: PicCategory.Boolean } =>
                "a BOOLEAN RETURNING item is not yet carried — the §8.8.2 boolean-expression channel "
                + "(IF simple-boolean-condition, COMPUTE Format 2) has no function-result arm; carrying only "
                + "the MOVE/relation legs would half-wire the category (a named residue)",
            { Category: PicCategory.Pointer } =>
                "a data-pointer RETURNING item is not yet carried across the function-activation boundary "
                + "(ISO §8.5.2.6 / §13.18.60 GR23 reference restrictions; a named residue)",
            { Category: PicCategory.ObjectReference } =>
                "an object-reference RETURNING item for a FUNCTION-ID is not yet carried (the §14.8.3.3 "
                + "object-reference conformance legs; a named residue)",
            _ => "this RETURNING item's category is not yet carried (a named residue)",
        };
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
