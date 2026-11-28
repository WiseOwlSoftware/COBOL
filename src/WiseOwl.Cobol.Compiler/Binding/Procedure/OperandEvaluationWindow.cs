// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Compiler.Binding.Bound;
using WiseOwl.Cobol.Compiler.Binding.Model;
using WiseOwl.Cobol.Compiler.Binding.Procedure.Verbs;
using WiseOwl.Cobol.Runtime.Exceptions;

namespace WiseOwl.Cobol.Compiler.Binding.Procedure;

/// <summary>
/// ⛔ THE OPERAND-EVALUATION WINDOW (kb/Work PB3223): the one protocol by which an operand that must run code before
/// its statement (a user-function activation, an inline method invocation, a §2023:15.4 function-bearing subscript's
/// temporary store, an object-property GET) is sequenced: <see cref="Mark"/> both statement-scoped pending lists on
/// entry, then drain only the suffix registered since, either HOISTED in front of the statement (<see cref="Hoist"/>,
/// evaluated exactly once per execution) or ATTACHED to a condition or operand the statement evaluates repeatedly or
/// conditionally (<see cref="AttachPerEvaluation(BoundCondition, PendingMark)"/>, §2023:8.8.4.13 GR2). Every drained
/// step is stamped as an operand evaluation of the statement now binding (<see cref="OperandSteps"/>).
/// <para>Four producers feed the lists (<see cref="UdfBinder"/>, the inline-invocation and property arms of
/// <see cref="OoBinder"/>, <see cref="ArgumentOrder"/>'s subscript temps) and the statement funnel plus the condition,
/// PERFORM, EVALUATE, SEARCH and report binders consume it, so it is owned by none of them: until PB3223 it lived in
/// <c>UdfBinder</c> and read as the function binder's private mechanism, which is how a property in
/// <c>PERFORM UNTIL P OF S &gt; 3</c> came to be fetched once for the whole loop (kb/Work PB987).</para>
/// </summary>
internal sealed class OperandEvaluationWindow(BinderContext ctx, StatementBinder host)
{
    /// <summary>THIS statement's not-yet-hoisted PRE-ops (statement-scoped: BindStatement marks the count on entry
    /// and drains only its own suffix — the property-op discipline).
    /// <para>⛔ IT IS THE SHARED <see cref="ProcedureBindState.PendingPreOps"/>, NOT A LIST OWNED HERE. A D18
    /// function-bearing subscript registers its §2023:15.4 temp store on the same list while a function's arguments
    /// bind (fix-queue PB17), and only ONE list in registration order sequences the store before the activation
    /// that consumes it. Every member below is therefore written against <see cref="BoundStatement"/>, not
    /// <see cref="BoundCallProgram"/>.</para></summary>
    private DrainQueue<BoundStatement> Pending => ctx.Data.Procedure.PendingPreOps;

    /// <summary>The mark of BOTH statement-scoped pending lists at one instant — the pre-op list
    /// (<see cref="ProcedureBindState.PendingPreOps"/>: function activations, inline method invocations, function-bearing
    /// subscript temps) and the object-property op list (<see cref="ProcedureBindState.PropertyOps"/>).</summary>
    internal readonly record struct PendingMark(int PreOps, int PropertyOps);

    /// <summary>The pending-list mark for the host chokepoints (the suffix-drain protocol). ⛔ IT MARKS BOTH LISTS
    /// (kb/Work PB987): a per-evaluation window drains everything that activates while it binds — a function, an
    /// inline invocation AND an object-property GET — so none of the three can be hoisted out of a condition that
    /// is evaluated repeatedly or conditionally. It marked only the pre-op list until PB987, and a property
    /// reference in <c>PERFORM UNTIL P OF S &gt; 3</c> was fetched ONCE for the whole loop.</summary>
    internal PendingMark Mark => new(Pending.Mark(), ctx.Data.Procedure.PropertyOps.Mark());

    /// <summary>⛔ THE ONE DRAIN OF THE PENDING PRE-OP SUFFIX, and the only place the list is mutated on the way
    /// out. Takes everything registered past <paramref name="mark"/> and REMOVES it, so exactly one carrier ends
    /// up owning each activation — the statement hoist, the per-evaluation condition wrapper, or the
    /// per-evaluation operand wrapper. Null when nothing was registered, which is what lets every caller return
    /// its subject UNCHANGED and keeps the generated source byte-identical on the function-free path.
    /// <para>It is one method because the three callers differ ONLY in the node they wrap the drained suffix in;
    /// three copies of the take-and-remove pair is the shape where one of them eventually forgets the remove and
    /// double-activates.</para>
    /// <para>⛔ IT IS ALSO WHERE AN OPERAND-EVALUATION STEP IS STAMPED (kb/Work PB892, PB1432). Everything it drains
    /// was WRITTEN INSIDE the statement now binding, so every step is marked
    /// <see cref="BoundStatement.OperandEvaluation"/> — an activation (§2023:14.9.33.4 GR2a.2): the applicable statement
    /// is "the statement in which the inline invocation or function invocation was specified") and a §2023:15.4
    /// subscript temporary store alike (GR2 a) 1.: "the one in which the exception condition was raised"; before
    /// PB1432 an unmarked store's size-error landing wrote a <c>goto</c> inside a short-circuited operand's lambda,
    /// CS0159, and fell back INTO the statement when hoisted). An <see cref="IProfiledStatement"/> is also given THAT statement's
    /// §2023:7.3.25 checking profile (§2023:14.9.18.4 GR1b) — "if checking for that exception condition is enabled in the
    /// activating runtime element"). The profile cannot wait for <c>EcBinder.EcWrap</c>'s stamp: a per-evaluation
    /// window's activations live inside a CONDITION or an OPERAND, which that statement-shaped walk never enters,
    /// so before PB892 they carried an empty profile and every condition they propagated was discarded.</para></summary>
    private List<BoundStatement>? DrainPending(int mark, List<BoundStatement>? leading = null)
    {
        var taken = leading ?? [];
        if (Pending.AnyAfter(mark)) taken.AddRange(Pending.DrainFrom(mark));
        return taken.Count == 0 ? null : OperandSteps(taken);
    }

    /// <summary>Stamp <paramref name="taken"/> as OPERAND-EVALUATION steps of the statement now binding (see
    /// <see cref="DrainPending"/>): each step is marked, an activation is given the statement's checking profile, and
    /// the statement's operand-evaluation count grows. The ONE place a step is stamped — also for steps a binder
    /// COPIES rather than drains (a SEARCH's prelude that fixes an enclosing occurrence, kb/Work PB3365).</summary>
    internal List<BoundStatement> OperandSteps(List<BoundStatement> taken)
    {
        EcCheckingProfile? profile = null;
        for (int i = 0; i < taken.Count; i++)
        {
            var step = taken[i] with { OperandEvaluation = true };
            if (step is IProfiledStatement a)
            {
                profile ??= ctx.EcState.Turn.ProfileAt(host.StatementLine);
                step = a.WithCheckingProfile(profile);
            }
            taken[i] = step;
            ctx.Data.Procedure.OperandEvaluations.Advance(ctx.Data.Procedure.OperandEvaluations.Current + 1);
        }
        return taken;
    }

    /// <summary>Drain one per-evaluation window's suffix of BOTH lists (<see cref="Mark"/>): the object-property
    /// GETs first — the statement-level order, where the property wrap is the OUTER sequence — then the pre-ops, in
    /// registration order. A property written in a FUNCTION ARGUMENT is not among those GETs: ArgumentOrder moved its
    /// GET onto the pre-op list at the argument's position (kb/Work PB1932), so it precedes the activation that
    /// consumes its temp and follows every argument to its left (§2023:8.4.3.2.4 GR2).
    /// Every property reference a window drains is a SENDING operand (a condition or an arithmetic expression
    /// has no receiving operand), so each one is a §2023:8.4.3.9.4 GR1 GET (<c>OoBinder.OoDrainPropertyGets</c>).
    /// <para>Called directly — not through a wrapper node — by a window whose carrier is no condition and no expression:
    /// a report-section SOURCE identifier and CODE identifier (<c>ReportWriterBinder.PerEvaluation</c>, kb/Work PB4517).</para></summary>
    internal List<BoundStatement>? DrainPerEvaluation(PendingMark mark) =>
        DrainPending(mark.PreOps, host.Oo.OoDrainPropertyGets(mark.PropertyOps));

    /// <summary>Hoist THIS statement's pending function activations (registered while the statement bound)
    /// into the hoisted <see cref="BoundSequence"/>: every activation is a PRE-op — a function-identifier
    /// is never a receiving operand (§2023:8.4.3.2.3 SR1), so unlike property references there is no polarity
    /// classification and no post-ops. Runs INSIDE the property-op wrap at the BindStatement chokepoint; a
    /// property-reference ARGUMENT's GET is not in that wrap but on this list, at its argument's position
    /// (ArgumentOrder, kb/Work PB1932), so it precedes the activation that consumes its temp and follows the
    /// arguments to its left. The hoist is EXACT here: every conditionally- or repeatedly-evaluated window
    /// already drained its own suffix into a per-evaluation <see cref="BoundUdfEvaluated"/> wrapper BEFORE the
    /// statement completed
    /// binding, so what remains pending is evaluated exactly once per statement execution (a plain operand,
    /// a sole IF condition, a TIMES count — §2023:14.9.28.4 GR7, a first-level VARYING FROM — GR13a init, an
    /// EVALUATE subject occurrence).</summary>
    internal BoundStatement Hoist(BoundStatement core, int mark) =>
        DrainPending(mark) is { } taken ? new BoundSequence([.. taken, core]) : core;

    /// <summary>Attach the function activations registered while <paramref name="cond"/> bound (those past
    /// <paramref name="mark"/>) to the condition itself as a per-evaluation <see cref="BoundUdfEvaluated"/>
    /// wrapper — the §2023:8.4.3.2.4 GR1/GR6a "value is determined when the function is referenced at runtime" semantics for a
    /// window the statement evaluates conditionally or repeatedly (§2023:8.8.4.13 GR2). The drained suffix leaves
    /// the pending list, so the statement-level hoist never double-activates them. No pending growth returns
    /// the condition unchanged (zero cost for the UDF-free path).</summary>
    internal BoundCondition AttachPerEvaluation(BoundCondition cond, PendingMark mark) =>
        DrainPerEvaluation(mark) is { } taken ? new BoundUdfEvaluated(taken, cond) : cond;

    /// <summary>The EXPRESSION twin of <see cref="AttachPerEvaluation(BoundCondition, PendingMark)"/>: attach the activations registered
    /// while <paramref name="e"/> bound (those past <paramref name="mark"/>) to the OPERAND as a
    /// <see cref="BoundUdfEvaluatedExpr"/>, for a window the statement evaluates repeatedly — ISO §2023:14.9.28.4
    /// GR12's "item identification … is done each time the content … is used in a setting or augmenting
    /// operation" (§2023:8.4.3.2.4 GR1/GR6a). The drained suffix leaves the pending list, so the statement-level
    /// hoist never double-activates it; no pending growth returns the expression unchanged, so the UDF-free
    /// path costs nothing and the generated source is byte-identical.
    /// <para>⛔ IT REPLACED A REFUSAL, NOT A SILENCE (kb/Work PB437). These two operand positions used to call
    /// the (since deleted) narrowed residue stage and REJECT the program (COBOL1509) while the adjacent
    /// first-level FROM and the UNTIL condition accepted the same construct — a capability limit shipped as a
    /// diagnostic, pinned green by two tests. The carrier the condition already used is what the augment and
    /// re-initialization sites needed too.</para></summary>
    internal BoundExpr AttachPerEvaluation(BoundExpr e, PendingMark mark) =>
        DrainPerEvaluation(mark) is { } taken ? new BoundUdfEvaluatedExpr(taken, e) : e;
}
