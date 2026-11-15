// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding;

/// <summary>
/// The state procedure binding keeps that is no fact at all (docs/rearchitecture/DESIGN-architecture-review.md §8.3 (1);
/// kb/Work PB2296), in the three shapes the data pipeline has no word for. It is not part of <see cref="DataBindState"/>:
/// a value slice is frozen when its phase ends and a fact set only grows, and each of these changes and changes back
/// while ONE phase, ProcedureBound, walks the statements in order.
/// <list type="bullet">
/// <item>A SCOPED OVERRIDE (<see cref="Scoped{T}"/>): <see cref="Options"/>, the unit's OPTIONS model, which a method's
/// own OPTIONS paragraph overrides for that method's sentences (ISO §2023:11.9.4 GR1; kb/Work PB135).</item>
/// <item>An ORDERED DRAIN QUEUE (<see cref="DrainQueue{T}"/>): <see cref="PendingPreOps"/> and
/// <see cref="PropertyOps"/>, statement-scoped — a statement marks the queue when it starts binding and takes only the
/// entries registered after its mark, so a reference in an IF condition belongs to the IF, not to an arm that binds
/// later.</item>
/// <item>A SCOPE CURSOR (<see cref="Cursor{T}"/>): <see cref="OperandEvaluations"/>, entered at zero per statement, and
/// <see cref="ActiveMethodScope"/>, entered per procedure-table entry.</item>
/// </list>
/// Each unit's <see cref="DataBinder"/> holds one, because the statement walk, the reference resolver and the binder's
/// own name lookup all reach it through the unit's binder (a class's methods bind through their half's binder).
/// </summary>
internal sealed class ProcedureBindState
{
    /// <summary>The OPTIONS model the statement now binding is governed by: the unit's own (<see cref="ConfigurationSlice.Options"/>,
    /// the root the reader passes to <see cref="Scoped{T}.Over"/>), or, while a method's sentences bind, the method's own
    /// OPTIONS model pushed over it
    /// (<c>StatementBinder.BindClassBody</c>; §2023:11.9.4 GR1). Read per statement: the default rounding
    /// (<c>ExpressionBinder</c>) and the arithmetic mode (<c>IntrinsicBinder</c>, <c>SetBinder</c>).</summary>
    public Scoped<OptionsModel> Options { get; } = new();

    /// <summary>⛔ THE ONE statement-scoped pending PRE-OP queue — every activation that must run BEFORE the carrying
    /// statement, in REGISTRATION order. Its clients register here:
    /// <list type="bullet">
    ///   <item>a user-defined function activation (<c>UdfBinder</c>, ISO §2023:8.4.3.2.4 GR1 — the caller-side
    ///         <c>BoundCallProgram</c> into the result temp), and an inline method invocation (§2023:8.4.3.4.4 GR1);</item>
    ///   <item>a function-bearing subscript or reference-modifier segment's §2023:15.4 temporary store
    ///         (<c>StatementBinder.BindPositionTemporary</c>);</item>
    ///   <item>a function argument's held value and its object-property GETs, placed at the argument's position by
    ///         <c>ArgumentOrder</c> (§2023:8.4.3.2.4 GR2; kb/Work PB1423, PB1932).</item>
    /// </list>
    /// <para>⛔ ONE QUEUE, NOT TWO, AND THAT IS A CORRECTNESS REQUIREMENT rather than tidiness: the two kinds INTERLEAVE. A
    /// function subscript inside a user-function argument — <c>FUNCTION F(W-E(FUNCTION INTEGER(3)))</c> — registers the
    /// subscript temp while F's arguments bind and F's activation after, so only a single queue in registration order
    /// puts the store before the activation that consumes it.</para>
    /// <para>Both kinds are UNCONDITIONAL pre-ops with identical hoist rules, because §2023:8.4.3.2.3 SR1 makes a
    /// function-identifier a non-receiving operand — so unlike <see cref="PropertyOps"/> there is no store-polarity
    /// classification and there are no post-ops. <c>UdfBinder.Mark</c> is the mark of the mark-on-entry /
    /// drain-own-suffix protocol at the <c>BindStatement</c> chokepoint, and it covers both queues.</para></summary>
    public DrainQueue<Bound.BoundStatement> PendingPreOps { get; } = new();

    /// <summary>The unit's un-drained object-property reference ops (§2023:8.4.3.9): statement-scoped like
    /// <see cref="PendingPreOps"/>, and drained by <c>OoBinder.OoWrapPropertyOps</c> into the GET before and the SET after
    /// the statement that names the property.</summary>
    public DrainQueue<OoPendingPropertyOp> PropertyOps { get; } = new();

    /// <summary>The identifications of the RECEIVING data references the statement now binding identifies as it stores each one
    /// (kb/Work PB5386): what a receiver's subscripts and reference modifiers registered on <see cref="PendingPreOps"/> while it
    /// resolved, taken off that queue by <c>ExpressionBinder.ResolveReceivingAtStore</c> so it is not hoisted before the
    /// statement, each keyed on the receiver's place. Statement-scoped like the two queues above, and drained by
    /// <c>StatementBinder.BindStatement</c> into the statement's <see cref="Bound.BoundReceiverBrackets"/>, which the emitter
    /// opens at that receiver's store.</summary>
    public DrainQueue<Bound.ReceiverBracket> ReceiverIdentifications { get; } = new();

    /// <summary>How many OPERAND-EVALUATION steps (<see cref="Bound.BoundStatement.OperandEvaluation"/> — a function
    /// reference, an inline method invocation, an object-property accessor, a §2023:15.4 function-bearing subscript's temporary
    /// store; kb/Work PB892, PB1432) the statement now binding has drained so far — the test
    /// <c>StatementBinder.BindStatement</c> uses to wrap that statement in its <c>BoundActivationSite</c>. Entered at zero
    /// around each statement, so a nested statement's activations count toward the nested statement only (§2023:14.9.33.4 GR2
    /// a) 3.: "the lowest level statement, not the containing statement").</summary>
    public Cursor<int> OperandEvaluations { get; } = new(0);

    /// <summary>The name scope of the method whose statements are CURRENTLY being bound — entered per procedure-table entry
    /// by <c>BinderContext.EnterMethodScope</c>; null while binding program-, object- or factory-level code. A data-name
    /// lookup made while it is entered answers from the method first (§2023:11.7.4 GR5); the storage-claim resolution
    /// (<c>PtrBindBasedAndAddressablesPass.ResolveStorageClaimHead</c>) and the migration-flag pass (<c>FlagConformancePass</c>) enter it for
    /// the one method whose names they resolve.</summary>
    public Cursor<OoMethodDataScope?> ActiveMethodScope { get; } = new(null);

    /// <summary>⛔ THE ONE CLAIM A RECEIVER-PLACING BINDER MAKES (kb/Work PB2078): each of <paramref name="receivers"/> is a
    /// RECEIVING operand of a statement whose emitter places each receiver's accessors itself where the statement identifies
    /// and stores it (<c>ReceiverBracketEmitter.Identify / Receive / Settle</c>). When it is an object-property reference
    /// this marks the pending op, so <c>OoBinder.OoWrapPropertyOps</c> brackets it per receiver; any other place (and a
    /// null, an absent phrase) is none of this method's business.
    /// The claim and the emitter's <c>Receive</c> are one contract: a claimed property whose bracket the emitter never
    /// places fails the compilation (<c>ReceiverBracketEmitter.Emit</c>).</summary>
    public void ClaimInterleavedReceiver(params IEnumerable<Place?> receivers)
    {
        foreach (var receiver in receivers)
            if (receiver is not null)
                foreach (var op in PropertyOps.Since(0))
                    if (ReferenceEquals(op.Temp, receiver.Item)) { op.Interleaved = true; break; }
    }
}
