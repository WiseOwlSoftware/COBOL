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
/// evaluated exactly once per statement execution; every conditionally- or repeatedly-evaluated CONDITION
/// window (a PERFORM UNTIL/VARYING UNTIL condition, a SEARCH WHEN, an EVALUATE object term, a non-first
/// AND/OR operand) instead drains its suffix into a per-evaluation <see cref="BoundUdfEvaluated"/> wrapper
/// (<see cref="UdfAttachPerEvaluation"/> — §8.8.4.13 r2 "if and when the conditions containing them are
/// evaluated"); the two operand windows per-evaluation does not yet reach (VARYING BY / AFTER-level FROM)
/// and the per-WHEN-re-bound EVALUATE subject stage LOUD (the narrowed COBOLNET1509,
/// <see cref="UdfStagePerEvaluationResidue"/>) rather than silently over/under-evaluating. Emission is
/// 100% existing surface:
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
    /// <summary>THIS statement's not-yet-hoisted PRE-ops (statement-scoped: BindStatement marks the count on entry
    /// and drains only its own suffix — the property-op discipline).
    /// <para>⛔ IT IS THE SHARED <see cref="DataBinder.PendingPreOps"/>, NOT A LIST OWNED HERE. A D18
    /// function-bearing subscript registers its §15.4 temp store on the same list while this binder's arguments
    /// bind (fix-queue PB17), and only ONE list in registration order sequences the store before the activation
    /// that consumes it. Every member below is therefore written against <see cref="BoundStatement"/>, not
    /// <see cref="BoundCallProgram"/>.</para></summary>
    private List<BoundStatement> Pending => ctx.Data.PendingPreOps;

    /// <summary>The pending-list mark for the host chokepoints (the suffix-drain protocol) — now covering BOTH
    /// pre-op kinds, so the per-evaluation machinery below serves function subscripts with no extra wiring.</summary>
    internal int PendingCount => Pending.Count;

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
            // §8.4.3.2.3 SR10 (:6942): when the formal corresponding to argument-1 is specified with a BY
            // VALUE phrase, argument-1 shall be of class numeric, object, or pointer. Checked HERE (the one
            // place the formal↔argument pairing exists); the header side's SR2 already restricted the FORMAL.
            if (fn.Formals[i].ByValue && !UdfArgIsValueClass(operands[i]))
            {
                ctx.Edition.Error("COBOLNET1554",
                    $"FUNCTION {name.ToUpperInvariant()} argument {i + 1}: an argument passed to a BY VALUE "
                    + "formal parameter shall be of class numeric, object, or pointer (ISO §8.4.3.2.3 SR10)");
                return new BoundExprError($"FUNCTION {name} argument {i + 1} BY VALUE class");
            }
            if (UdfArg(operands[i], fn.Formals[i]) is not { } arg)
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

        Pending.Add(new BoundCallProgram(fn.Name, null, callArgs, tempPlace, null, null) { IsFunction = true });
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
    /// alphanumeric/alphabetic, numeric-edited, national; image-form groups (every leaf
    /// <see cref="DataItem.ElementImageCapable"/> — character-stored, or any pinned numeric byte form:
    /// zoned DISPLAY, binary, packed, COMP-5, IEEE float, INDEX). Staged: FLOAT
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
                // ⛔ THE DERIVED IMAGE PREDICATE, never a hand-rolled usage union. This screen asks exactly one
                // question — does the leaf have a character image the group codec can carry across the
                // activation boundary? — and DataItem.ElementImageCapable is where that question is answered
                // for every consumer (the group codec, the REDEFINES backing, the record window). It read
                // `{ Numeric, IsFloat: false, Usage: Display }` — the DISPLAY-only union from before V59 — so it
                // went on rejecting binary / packed / COMP-5 / float / INDEX group leaves years after those
                // forms got their pinned bytes (V59, kb/Work PB164 waves 1–2, R40). What is genuinely left is
                // the pointer/object class, which has no character image under EITHER axis.
                if (d.IsElementary && !d.ElementImageCapable)
                    return $"a group RETURNING item with a pointer- or object-class leaf ('{d.CobolName ?? "FILLER"}') "
                        + "is not yet carried — a data-pointer, program-pointer, function-pointer or "
                        + "object-reference leaf has no character image to cross the activation boundary "
                        + "(ISO §8.5.2.6 / §13.18.60 GR23 reference restrictions; a named residue)";
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
    /// operand, with the formal's BY REFERENCE stated or implied ⇒ BY REFERENCE over the caller's storage;
    /// (b) a literal or arithmetic expression ⇒ BY CONTENT, a private-copy cell (the runtime
    /// <c>CobolArgAdapt</c> profile adaptation realizes the §14.2.3 GR9 copy-in conformance to the formal —
    /// same-scale cells alias, a scale difference gets the rescaling view); (c) a formal specified BY VALUE ⇒
    /// BY VALUE for EVERY argument shape (GR5c :6991) — the caller snapshots the value and the callee adopts
    /// the §14.2.3 GR10 detached copy, so a store into the formal never reaches the argument. Null =
    /// unsupported operand form (the caller reports).</summary>
    private static BoundCallArg? UdfArg(BoundOperand op, LinkageFormal formal) => op switch
    {
        BoundFieldOperand f => new BoundCallArg(
            formal.ByValue ? CobolPassMode.Value : CobolPassMode.Reference, f.Place, null),
        BoundNumericLiteral or BoundStringLiteral or BoundComputedOperand
            => new BoundCallArg(formal.ByValue ? CobolPassMode.Value : CobolPassMode.Content, null, op),
        _ => null,
    };

    /// <summary>True when the argument is of class numeric, object, or pointer — the §8.4.3.2.3 SR10
    /// admissible classes for an argument whose corresponding formal is BY VALUE. A numeric literal and an
    /// arithmetic expression are class numeric by construction; an identifier tests its item's category.</summary>
    private static bool UdfArgIsValueClass(BoundOperand op) => op switch
    {
        BoundNumericLiteral or BoundComputedOperand => true,
        BoundFieldOperand f => f.Place.Item.Pic?.Category is PicCategory.Numeric or PicCategory.Pointer
            or PicCategory.ProgramPointer or PicCategory.ObjectReference,
        _ => false,
    };

    /// <summary>Drain THIS statement's pending function activations (registered while the statement bound)
    /// into the hoisted <see cref="BoundSequence"/>: every activation is a PRE-op — a function-identifier
    /// is never a receiving operand (§8.4.3.2.3 SR1), so unlike property references there is no polarity
    /// classification and no post-ops. Runs INSIDE the property-op wrap at the BindStatement chokepoint, so
    /// a property-reference argument's GET (a pre-op of the OUTER wrap) still precedes the activation that
    /// consumes its temp. The hoist is EXACT here: every conditionally- or repeatedly-evaluated window
    /// already drained its own suffix into a per-evaluation <see cref="BoundUdfEvaluated"/> wrapper (or the
    /// narrowed <see cref="UdfStagePerEvaluationResidue"/> 1509 stage) BEFORE the statement completed
    /// binding, so what remains pending is evaluated exactly once per statement execution (a plain operand,
    /// a sole IF condition, a TIMES count — §14.9.28 GR7, a first-level VARYING FROM — GR13a init, an
    /// EVALUATE subject occurrence).</summary>
    internal BoundStatement UdfWrapCalls(BoundStatement core, int mark)
    {
        var calls = Pending;
        if (calls.Count <= mark) return core;
        var taken = calls.GetRange(mark, calls.Count - mark);
        calls.RemoveRange(mark, calls.Count - mark);
        return new BoundSequence([.. taken, core]);
    }

    /// <summary>Attach the function activations registered while <paramref name="cond"/> bound (those past
    /// <paramref name="mark"/>) to the condition itself as a per-evaluation <see cref="BoundUdfEvaluated"/>
    /// wrapper — the §8.4.3.2.4 GR1/GR6a "value determined when the function is referenced" semantics for a
    /// window the statement evaluates conditionally or repeatedly (§8.8.4.13 r2). The drained suffix leaves
    /// the pending list, so the statement-level hoist never double-activates them. No pending growth returns
    /// the condition unchanged (zero cost for the UDF-free path).</summary>
    internal BoundCondition UdfAttachPerEvaluation(BoundCondition cond, int mark)
    {
        var calls = Pending;
        if (calls.Count <= mark) return cond;
        var taken = calls.GetRange(mark, calls.Count - mark);
        calls.RemoveRange(mark, calls.Count - mark);
        return new BoundUdfEvaluated(taken, cond);
    }

    /// <summary>The NARROWED evaluation-cardinality stage (§1.4 — loud, never silently wrong) for the
    /// operand windows per-evaluation activation does not yet reach: a PERFORM VARYING BY operand (evaluated
    /// per augment, §14.9.28 GR12) or a non-first (AFTER) level's FROM operand (re-evaluated per outer
    /// augment, GR13e.2), and an EVALUATE selection SUBJECT (this backend's chained-selection lowering
    /// re-binds subject expressions per WHEN, while §14.9.13 evaluates a subject once per statement — a
    /// hoist-per-WHEN would over-activate). Conditions are NOT staged — they ride
    /// <see cref="UdfAttachPerEvaluation"/>.</summary>
    internal void UdfStagePerEvaluationResidue(int mark, string where)
    {
        if (Pending.Count <= mark) return;
        ctx.Edition.Error("COBOLNET1509",
            $"a function reference in {where} requires an activation cardinality this "
            + "implementation does not yet realize for that operand position (ISO §8.4.3.2.4 GR1/GR6a; "
            + "§14.9.28 GR12/GR13 / §14.9.13) — move the reference to a preceding COMPUTE");
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

}
