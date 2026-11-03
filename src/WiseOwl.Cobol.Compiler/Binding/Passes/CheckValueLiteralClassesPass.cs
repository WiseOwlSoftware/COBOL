// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Common;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions.Diagnostics;

namespace WiseOwl.Cobol.Binding.Passes;

/// <summary>
/// The CheckValueLiteralClasses pass: row 22 of the per-unit resolve pipeline (OccursResolved to OccursResolved).
/// </summary>
/// <remarks>
/// <para><b>Placement in the pipeline.</b>
/// The §13.18.63.3 SR7 VALUE-literal CLASS screen (kb/Work PB646). Placed HERE for two reasons, and it needs both. SR7 reads §8.5.2.1 Table 2's CLASS of a numeric-edited item, which IS its usage ("Numeric-edited (if usage is display)" / "(if usage is national)"), so UsageInheritancePass must have settled §13.18.60.4 GR1 — an INHERITED usage has to give the same verdict as a written one, in both directions. And the rule reaches formats 2 and 5 as well, whose per-occurrence literals only exist once ResolveTableValues has built the plan. Nothing later is needed: it reads declared shape only.
/// </para>
/// </remarks>
internal sealed class CheckValueLiteralClassesPass : IBindPass
{
    /// <inheritdoc/>
    public string Name => "CheckValueLiteralClasses";

    /// <inheritdoc/>
    public PassPhase Requires => PassPhase.OccursResolved;

    /// <inheritdoc/>
    public PassPhase Produces => PassPhase.OccursResolved;

    /// <inheritdoc/>
    public PassViews Views => PassViews.None;

    /// <summary>⛔ THE §13.18.63.3 SR7 SCREEN — the ONE VALUE-literal rule that reads the subject's CLASS
    /// instead of its category, asked ONCE per literal, after §13.18.60.4 GR1 usage inheritance has settled
    /// (kb/Work PB646). <i>"If the item is of category numeric-edited and the literal is of class alphanumeric or
    /// national, the class of the literal shall conform to that of the data item, if the class is otherwise
    /// undefined, then the class of the data item shall be that of the literal."</i>
    /// <para>§8.5.2.1 Table 2 is what makes it a USAGE test and what makes it bite in BOTH directions: category
    /// numeric-edited is class ALPHANUMERIC when its usage is display and class NATIONAL when its usage is
    /// national. The national-usage half was unreachable while §13.18.60.3 SR12's national-form numeric-edited
    /// item was staged loud (COBOL0899).</para>
    /// <para>A NUMERIC literal is SR2/SR6's business, not this rule's, and a figurative constant takes the
    /// receiver's class (§8.3.3.6), so neither reaches the test. ⚠ §13.18.63.3 SR7's third clause — "if the class is
    /// otherwise undefined, then the class of the data item shall be that of the literal" — has NO SITE in this
    /// model and cannot acquire one: §13.18.60.3 SR13 implies a usage for every item given none (NATIONAL for an
    /// 'N'-bearing picture character-string, else DISPLAY), so a numeric-edited item's usage — and with it its
    /// Table 2 class — is always defined.</para></summary>
    private static void ScreenNumericEditedLiteralClass(DataBinder binder, PicInfo pic, string raw, string where)
    {
        if (pic.Category is not PicCategory.NumericEdited) return;
        LiteralClass? lit = CobolLiteral.ClassOf(raw);
        LiteralClass? allLit = CobolLiteral.AllLiteralRaw(raw) is { } allRaw ? CobolLiteral.ClassOf(allRaw) : null;
        bool isNatLit = lit is LiteralClass.National || allLit is LiteralClass.National;
        bool isAlnumLit = lit is LiteralClass.Alphanumeric || allLit is LiteralClass.Alphanumeric;
        if (!isNatLit && !isAlnumLit) return;   // a numeric literal, a boolean literal or a figurative — not SR7's
        if (isNatLit == (pic.Usage is Usage.National)) return;
        binder.Edition.Error(DiagnosticCatalog.ValueLiteralClassNumericEdited, $"{where}: the VALUE literal {raw} is of class "
            + $"{(isNatLit ? "national" : "alphanumeric")}, but the numeric-edited item is of usage "
            + $"{(pic.Usage is Usage.National ? "NATIONAL and therefore of class national" : "DISPLAY and therefore of class alphanumeric")}"
            + " — the class of the literal shall conform to that of the data item (ISO §13.18.63.3 SR7; "
            + "§8.5.2.1 Table 2)");
    }

    /// <summary>The §13.18.63.3 SR7 pass — every VALUE literal of every format, over the RESOLVED usage
    /// (kb/Work PB646). Format 1's item literal and format 2's per-occurrence literals hang off the item; formats
    /// 3 and 5's hang off the condition-names, whose "item" SR7 speaks of is the conditional variable (the same
    /// reading <c>ValidateValueCategory</c>'s condition-name call site already uses).
    /// <para>⛔ It is a PASS and not an entry-bind check because the rule reads §8.5.2.1 Table 2's CLASS, which
    /// for a numeric-edited item is its usage — and §13.18.60.4 GR1 lets a group's USAGE clause supply that
    /// usage after the entry has bound. Asking at entry bind gave the GR1 spelling the opposite verdict from its
    /// written-clause twin in BOTH directions, which is exactly what the inheritance pass's invariant forbids
    /// ("same syntax-rule verdicts, same diagnostics").</para></summary>
    public void Run(DataBinder binder)
    {
        foreach (var root in binder.State.Forest.Roots) Walk(root);
        foreach (var list in binder.State.Forest.Conditions.Values)
            foreach (var cond in list)
            {
                if (cond.Parent.OperandPic is not { } cp) continue;
                using var _ = binder.Edition.At(cond.Parent);
                foreach (var (lo, hi) in cond.Values)
                {
                    ScreenNumericEditedLiteralClass(binder, cp, lo, $"condition-name '{cond.Name}'");
                    if (hi is not null) ScreenNumericEditedLiteralClass(binder, cp, hi, $"condition-name '{cond.Name}'");
                }
            }

        void Walk(DataItem item)
        {
            if (item.Pic is { } pic && !pic.IsRecovery)
            {
                using var _ = binder.Edition.At(item);
                string where = $"data item '{item.CobolName ?? "FILLER"}'";
                if (item.RawValue is { } raw) ScreenNumericEditedLiteralClass(binder, pic, raw, where);
                // Each literal the clause WRITES, once — not once per table element it initializes (kb/Work PB1722).
                if (item.TableValuePlan is { } plan)
                    foreach (var lit in plan.Phrases.SelectMany(p => p.Literals).Distinct(StringComparer.Ordinal))
                        ScreenNumericEditedLiteralClass(binder, pic, lit, where);
            }
            foreach (var c in item.Children) Walk(c);
        }
    }
}
