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
/// The §2023:13.18.63.3 SR7 VALUE-literal CLASS screen (kb/Work PB646). Placed HERE for two reasons, and it needs both. SR7 reads §2023:8.5.2.1 Table 2's CLASS of a numeric-edited item, which IS its usage ("Numeric-edited (if usage is display)" / "(if usage is national)"), so UsageInheritancePass must have settled §2023:13.18.60.4 GR1 — an INHERITED usage has to give the same verdict as a written one, in both directions. And the rule reaches formats 2 and 5 as well, whose per-occurrence literals only exist once ResolveTableValues has built the plan. Nothing later is needed: it reads declared shape only.
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

    /// <summary>⛔ THE §2023:13.18.63.3 SR7 SCREEN — the ONE VALUE-literal rule that reads the subject's CLASS
    /// instead of its category, asked ONCE per literal, after §2023:13.18.60.4 GR1 usage inheritance has settled
    /// (kb/Work PB646). <i>"If the item is of category numeric-edited and the literal is of class alphanumeric or
    /// national, the class of the literal shall conform to that of the data item, if the class is otherwise
    /// undefined, then the class of the data item shall be that of the literal."</i>
    /// <para>§2023:8.5.2.1 Table 2 is what makes it a USAGE test and what makes it bite in BOTH directions: category
    /// numeric-edited is class ALPHANUMERIC when its usage is display and class NATIONAL when its usage is
    /// national. The national-usage half was unreachable while §2023:13.18.60.3 SR12's national-form numeric-edited
    /// item was staged loud (COBOL0899).</para>
    /// <para>A NUMERIC literal is SR2/SR6's business, not this rule's, and a figurative constant takes the
    /// receiver's class (§2023:8.3.3.6), so neither reaches the test. ⚠ §2023:13.18.63.3 SR7's third clause — "if the class is
    /// otherwise undefined, then the class of the data item shall be that of the literal" — has NO SITE in this
    /// model and cannot acquire one: §2023:13.18.60.3 SR13 implies a usage for every item given none (NATIONAL for an
    /// 'N'-bearing picture character-string, else DISPLAY), so a numeric-edited item's usage — and with it its
    /// Table 2 class — is always defined.</para>
    /// <para>The class screen's SIZE half lives beside it for the same reason (kb/Work PB4610): once the literal's
    /// class conforms to the usage, §2023:13.18.63.3 SR5 sentence 2, "National literals in the VALUE clause of an
    /// elementary item shall not exceed the size indicated by an explicit PICTURE clause", measures a national
    /// literal on a USAGE NATIONAL numeric-edited item against the edited picture's positions, the editing
    /// characters counted. Its alphanumeric twin is the SR4 arm of <c>ValidateValueCategory</c>, which needs no
    /// usage. <paramref name="subject"/> is the SAME <see cref="ValueSubject"/> the entry-bind screen builds, so a
    /// CONDITION-NAME subject, which indicates no size, stays dark for this arm exactly as for that one.</para></summary>
    private static void ScreenNumericEditedLiteralClass(DataBinder binder, PicInfo pic, string raw, string where,
        ValueSubject subject)
    {
        if (pic.Category is not PicCategory.NumericEdited) return;
        LiteralClass? lit = CobolLiteral.ClassOf(raw);
        LiteralClass? allLit = CobolLiteral.AllLiteralRaw(raw) is { } allRaw ? CobolLiteral.ClassOf(allRaw) : null;
        bool isNatLit = lit is LiteralClass.National || allLit is LiteralClass.National;
        bool isAlnumLit = lit is LiteralClass.Alphanumeric || allLit is LiteralClass.Alphanumeric;
        if (!isNatLit && !isAlnumLit) return;   // a numeric literal, a boolean literal or a figurative — not SR7's
        if (isNatLit == (pic.Usage is Usage.National))
        {
            if (lit is LiteralClass.National && subject.SizePositions is { } natLimit
                && CobolLiteral.Decode(raw).Length > natLimit)
                binder.Edition.Error(DiagnosticCatalog.ValueNationalLiteralRule, $"{where}: the VALUE national literal "
                    + $"exceeds the numeric-edited item's {natLimit} national positions (ISO §2023:13.18.63.3 SR5)");
            return;
        }
        binder.Edition.Error(DiagnosticCatalog.ValueLiteralClassNumericEdited, $"{where}: the VALUE literal {raw} is of class "
            + $"{(isNatLit ? "national" : "alphanumeric")}, but the numeric-edited item is of usage "
            + $"{(pic.Usage is Usage.National ? "NATIONAL and therefore of class national" : "DISPLAY and therefore of class alphanumeric")}"
            + " — the class of the literal shall conform to that of the data item (ISO §2023:13.18.63.3 SR7; "
            + "§2023:8.5.2.1 Table 2)");
    }

    /// <summary>⛔ THE LITERALS OF A CONDITION-NAME WHOSE CONDITIONAL VARIABLE IS AN ALPHANUMERIC GROUP (kb/Work PB5215).
    /// A group item has class and category alphanumeric (§2023:8.5.2.1), and a condition-name takes its conditional
    /// variable's characteristics (§2023:13.18.63.4 GR19; §1985:VI.5.15.5 GR1), so its literals are those of an
    /// alphanumeric item: §2023:13.18.63.3 SR4 sentence 1 "literals in the VALUE clause shall be alphanumeric literals"
    /// and, at COBOL-85, §1985:VI.5.15.3 SR2 "A signed numeric literal must have associated with it a signed numeric
    /// PICTURE character-string", which a group has none of. <c>BindCondition</c> screens a literal only through
    /// the conditional variable's operand PICTURE, and an ordinary group has none, so <c>88 G-N VALUE -1.</c> under
    /// <c>01 G.</c> compiled and stored the characters "-1" while the same literal in the group's own VALUE clause is
    /// COBOL1657. It is a pass-level check because a group is known to be one only once its subordinates are
    /// attached, after the 88 was read. It asks the SAME funnel the item VALUE does (<c>ValidateValueCategory</c>) with
    /// the SAME descriptor the group VALUE builds its picture from (<see cref="CheckGroupValueDeclarationsPass.GroupSubjectPic"/>);
    /// a national or bit group has an as-if PICTURE and was already screened at bind.</summary>
    private static void ScreenGroupConditionName(DataBinder binder, DataItem group, Condition88 cond)
    {
        using var _ = binder.Edition.At(group);
        var pic = CheckGroupValueDeclarationsPass.GroupSubjectPic(group);
        var subject = ValueSubject.ForConditionName(pic, binder.Edition.DialectLevel);
        string where = $"condition-name '{cond.Name}' (of the alphanumeric group item '{group.CobolName ?? group.ClrName}')";
        foreach (var (lo, hi) in cond.Values)
        {
            binder.ValidateValueCategory(pic, lo, where, subject);
            if (hi is not null) binder.ValidateValueCategory(pic, hi, where, subject);
        }
    }

    /// <summary>The §2023:13.18.63.3 SR7 pass — every VALUE literal of every format, over the RESOLVED usage
    /// (kb/Work PB646). Format 1's item literal and format 2's per-occurrence literals hang off the item; formats
    /// 3 and 5's hang off the condition-names, whose "item" SR7 speaks of is the conditional variable (the same
    /// reading <c>ValidateValueCategory</c>'s condition-name call site already uses).
    /// <para>⛔ It is a PASS and not an entry-bind check because the rule reads §2023:8.5.2.1 Table 2's CLASS, which
    /// for a numeric-edited item is its usage — and §2023:13.18.60.4 GR1 lets a group's USAGE clause supply that
    /// usage after the entry has bound. Asking at entry bind gave the GR1 spelling the opposite verdict from its
    /// written-clause twin in BOTH directions, which is exactly what the inheritance pass's invariant forbids
    /// ("same syntax-rule verdicts, same diagnostics").</para>
    /// <para>⛔ ONE VERDICT PER WRITTEN LITERAL, AT THE ENTRY THAT WROTE IT (kb/Work PB4678). It walks
    /// <see cref="DataBinder.DeclaredForest"/>, the roots AND the TYPEDEF templates — ISO §2023:13.18.58.4 GR1 makes the
    /// entries subordinate to a type declaration and its condition-names part of the declaration, and a declaration
    /// no item references still has to be a conforming one — and skips what a copy carried: an item whose VALUE was
    /// copied from a template or a SAME AS target (<see cref="DataItem.ValueCopiedFrom"/>) and a condition-name a
    /// clone reproduced (<see cref="Condition88.ClonedFrom"/>). A VALUE the TYPE reference site writes itself is no
    /// copy, so it is asked at the reference site against the composed description.</para></summary>
    public void Run(DataBinder binder)
    {
        foreach (var item in binder.DeclaredForest())
        {
            // The subject of both screens is the entry that WROTE the VALUE (kb/Work PB4678): a TYPE / SAME AS copy
            // carries the template's or target's literal (ValueCopiedFrom) and a cloned condition-name its source's
            // (ClonedFrom), and each was answered where it is declared, once, however many reference sites copy it.
            // The forest holds the TYPEDEF templates (ISO §2023:13.18.58.4 GR1: their subordinate entries and
            // condition-names are part of the declaration), so an UNUSED declaration answers too.
            if (item.Pic is { } pic && !pic.IsRecovery && item.ValueCopiedFrom is null)
            {
                using var _ = binder.Edition.At(item);
                string where = $"data item '{item.CobolName ?? "FILLER"}'";
                var subject = ValueSubject.ForElementary(pic, item.IsDynamicLength, item.IsAnyLength, item.PicIsValueImplied);
                if (item.RawValue is { } raw) ScreenNumericEditedLiteralClass(binder, pic, raw, where, subject);
                // Each literal the clause WRITES, once — not once per table element it initializes (kb/Work PB1722),
                // and read off the clause rather than the resolved plan, which a template never has (its OCCURS
                // chain is where it ends up, ResolveTableValuesPass) and which omits a mis-shaped phrase.
                if (item.TableValues is { } specs)
                    foreach (var lit in specs.SelectMany(s => s.Literals).Distinct(StringComparer.Ordinal))
                        ScreenNumericEditedLiteralClass(binder, pic, lit, where, subject);
            }
            foreach (var cond in item.Own88s)
            {
                if (cond.ClonedFrom is not null) continue;
                if (cond.Parent.OperandPic is not { } cp)
                {
                    if (!cond.Parent.IsElementary) ScreenGroupConditionName(binder, item, cond);
                    continue;
                }
                using var _ = binder.Edition.At(item);
                foreach (var (lo, hi) in cond.Values)
                {
                    var condSubject = ValueSubject.ForConditionName(cp, binder.Edition.DialectLevel);
                    ScreenNumericEditedLiteralClass(binder, cp, lo, $"condition-name '{cond.Name}'", condSubject);
                    if (hi is not null)
                        ScreenNumericEditedLiteralClass(binder, cp, hi, $"condition-name '{cond.Name}'", condSubject);
                }
            }
        }
    }
}
