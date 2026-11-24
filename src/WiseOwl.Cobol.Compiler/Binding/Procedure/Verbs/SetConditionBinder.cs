// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Frontend.Cst;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Frontend.Parsing;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions.Diagnostics;

namespace WiseOwl.Cobol.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>The SET Format 4, condition-names TO TRUE or FALSE (ISO §2023:14.9.39.2): one binder of the SET format families <see cref="SetBinder"/> dispatches to (kb/Work
/// PB2314; DESIGN-architecture-review §8.3, R0-0019). <see cref="SetFormatSelection"/> chose the format before this binder runs.</summary>
internal sealed class SetConditionBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary><c>SET condition-name+ TO TRUE | FALSE</c> (ISO §2023:14.9.39 Format 4).
    /// <para>⛔ ONE ARM, NOT TWO. §2023:14.9.39.4 GR6 and GR7 are the SAME sentence with one word changed — "<i>the
    /// literal in the VALUE clause</i>" against "<i>the literal in the FALSE phrase of the VALUE clause</i>",
    /// both "<i>placed in the conditional variable according to the rules for the VALUE clause</i>", both with
    /// the same group-length and zero-length provisos — so the binder resolves the operands once and carries
    /// WHICH literal as a flag; the emitter has one store path. The FALSE arm used to return
    /// <c>BoundUnsupported</c> because <c>Condition88</c> had no literal-4 to store (kb/Work PB555); now that it
    /// does, the only thing left to check is §2023:14.9.39.3 SR7 — the phrase has to be there.</para></summary>
    internal BoundStatement BindSetCondition(Core.SetBooleanStatementContext b)
    {
        var sets = new List<(Place, Condition88, bool)>();
        // ⛔ ONE LOOP OVER THE PRINTED GROUPS (kb/Work PB450). §2023:14.9.39.2 Format 4 wraps the whole
        // `{ condition-name-1 } … TO { TRUE | FALSE }` unit in an outer brace with a trailing `…`, so a
        // statement may write several groups and they need not agree on TRUE/FALSE; the grammar's
        // `setConditionPhrase` IS that unit, so the grouping is READ rather than re-derived from token
        // positions. §2023:14.9.39.4 GR8 makes the flattening exact: "If multiple condition-names are specified,
        // the results are the same as if a separate SET statement had been written for each condition-name-1."
        foreach (var phrase in b.setConditionPhrase())
        {
            bool toTrue = phrase.TRUE_() is not null;
            foreach (var dref in phrase.dataReference())
            {
                // ⛔ SR6 IS DECIDED HERE, SO IT IS REPORTED HERE (kb/Work PB390). "Condition-name-1 shall be
                // associated with a conditional variable" (ISO §2023:14.9.39.3 SR6) — and the operand that
                // DISCRIMINATES the rule is a SPECIAL-NAMES switch-status condition-name (§2023:8.4.4.1's second
                // kind), which the old message denied was a condition-name at all while staging the verdict to
                // a run-time abort.
                if (host.Cond.ConditionOf(dref) is not { } cond)
                {
                    // A DECLARED level-88 the qualifiers do not reach is misqualified, not "not a condition-name"
                    // (kb/Work PB567 — §2023:13.16.3 SR23's negative side, one wording for every site).
                    if (!host.Cond.ReportMisqualifiedCondition(dref))
                        ctx.Validation.RejectSetConditionName(WrittenSource.Of(dref), toTrue, host.Alter.SwitchNameOf(dref),
                            ReferenceResolver.WrittenWordOf(dref), ReferenceResolver.ReadWritten(dref).Qualifiers);
                    return BoundRejected.Reported(ctx.Edition);
                }
                // The reference's subscripts identify the CONDITIONAL VARIABLE's occurrence (§2023:8.4.2.3 Format 2).
                if (ctx.Refs.ResolveForItem(dref, cond.Parent) is var parentAnswer && parentAnswer.Place is not { } parent)
                    return parentAnswer.Refusal(ctx.Edition);   // the resolver's answer (kb/Work PB1030)
                // §2023:14.9.39.3 SR7 — "If the FALSE phrase is specified, the FALSE phrase shall be specified in the
                // VALUE clause of the data description entry for condition-name-1." The TRUE arm needs no twin
                // screen: §2023:13.18.63.3 SR24 already makes a VALUE clause mandatory on a level-88 entry, so
                // `cond.Values` is never empty where a condition-name exists.
                if (!toTrue && cond.FalseValue is null)
                {
                    return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.SetFalseWithoutFalsePhrase, $"SET '{cond.Name}' TO FALSE: "
                        + $"the VALUE clause of condition-name '{cond.Name}' writes no WHEN SET TO FALSE phrase, "
                        + "so there is no literal-4 to place in the conditional variable (ISO §2023:14.9.39.3 SR7)");
                }
                sets.Add((parent, cond, toTrue));
            }
        }
        return new BoundSetConditions(sets);
    }
}
