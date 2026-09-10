// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;

namespace CobolNet.Binding.Procedure;

/// <summary>What a BARE operand — one <c>valueOperand</c> with no operator around it — turned out to BE once the
/// symbol was resolved. A bare COBOL word is equally a data reference, a level-88 condition-name and a
/// switch-status condition-name; only the symbol table separates them, so this is a BIND-time classification and
/// never a grammatical one.</summary>
public enum BareOperandForm
{
    /// <summary>A plain value operand — an identifier, a literal or an arithmetic expression.</summary>
    Value = 0,

    /// <summary>A level-88 condition-name, which IS a complete condition (ISO §8.8.4.2.7 rule 2).</summary>
    ConditionName = 1,

    /// <summary>A switch-status condition-name declared in SPECIAL-NAMES, which IS a complete condition
    /// (ISO §8.8.4.6).</summary>
    SwitchStatus = 2,

    /// <summary>A boolean expression (ISO §8.8.2) — a category-boolean item, a boolean literal, the figurative
    /// ZERO or <c>ALL B"…"</c>, or a boolean function reference. Whether it is ALSO a §8.8.4.3 simple boolean
    /// CONDITION is the consuming clause's question: everywhere but EVALUATE it always is, while §14.9.13.3 SR6
    /// decides it from the other side of the selection pair.</summary>
    Boolean = 3,
}

/// <summary>⛔ ONE resolution of a bare operand, carried so a caller that must CLASSIFY before it BINDS does not
/// resolve the symbol twice (kb/Work PB400). Produced by <c>ConditionBinder.AnalyzeBareOperand</c>; turned into a
/// condition — when the caller's clause says it is one — by <c>ConditionBinder.AsCondition</c>.</summary>
/// <param name="Form">Which of the four shapes the operand is.</param>
/// <param name="Condition">The bound condition for <see cref="BareOperandForm.ConditionName"/> and
/// <see cref="BareOperandForm.SwitchStatus"/>, which are conditions outright; null otherwise.</param>
/// <param name="Boolean">The bound boolean EXPRESSION for <see cref="BareOperandForm.Boolean"/>, deliberately
/// NOT yet wrapped as a §8.8.4.3 simple boolean condition — that wrapper carries the clause's SR1 length-1
/// screen, and firing it before the classification is settled reports a rule the operand is not subject to.</param>
/// <param name="BooleanLength">The §8.8.2 rules 9/10 RESULT length in boolean positions, or null when the
/// operand is positionless (figurative ZERO / <c>ALL B"…"</c>) or its length is a run-time value. This is the
/// length §14.9.13.3 SR6's "results in one boolean character" turns on.</param>
public readonly record struct BareOperandAnalysis(
    BareOperandForm Form,
    BoundCondition? Condition,
    BoundBoolExpr? Boolean,
    int? BooleanLength)
{
    /// <summary>An operand that IS a condition outright (a level-88 or switch-status condition-name).</summary>
    public static BareOperandAnalysis OfCondition(BareOperandForm form, BoundCondition cond) =>
        new(form, cond, null, null);

    /// <summary>True when the operand is a condition REGARDLESS of the consuming clause — the two
    /// condition-name forms. A boolean operand is deliberately excluded: §14.9.13.3 SR6 makes its classification
    /// depend on the other side of the pair, and answering "yes" here is what let EVALUATE's object classifier
    /// recognise a shape its subject classifier did not.</summary>
    public bool IsConditionName => Form is BareOperandForm.ConditionName or BareOperandForm.SwitchStatus;
}
