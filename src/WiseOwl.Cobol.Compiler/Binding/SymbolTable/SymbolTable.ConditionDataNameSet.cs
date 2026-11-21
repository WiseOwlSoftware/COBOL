// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding;

/// <summary>
/// ⛔ COBOL-85'S FOURTH DISJOINT SET: CONDITION-NAMES, DATA-NAMES AND RECORD-NAMES ARE ONE SET OF NAMES (kb/Work PB5972).
/// §1985:IV.4.2.2.1.1 groups the user-defined words of a source program into disjoint sets, the fourth "condition-names, data-names,
/// and record-names", and "all user-defined words within a given disjoint set must be unique, except as specified in the rules for
/// uniqueness of reference"; §1985:IV.4.3.8.1 SRL2.1: "For each nonunique user-defined name that is explicitly referenced, uniqueness
/// must be established through a sequence of qualifiers which precludes any ambiguity of reference." So at COBOL-85 a level-88
/// <c>X</c> and a data item <c>X</c> of one source program may both be declared (<see cref="UserWordKinds.MayBeOneWord"/>), and a
/// reference to <c>X</c> is unique only when its qualifiers reach ONE of them, counted over both classes. From COBOL-2002 the two are
/// different types of user-defined word that one source element shall not share (§2023:8.3.2.2, COBOL2692), so no pair of one tier
/// exists to count and these questions answer nothing.
/// <para>The data-name lookup counts the condition-names (<see cref="ConditionNamesRivalingData"/>, in <see cref="DataNameCandidates"/>)
/// and the condition-name lookup the data items (<see cref="DataItemsRivalingCondition"/>, asked by <c>ConditionBinder.ConditionOf</c>),
/// each in the tier its own survivors stand in: across tiers the nearer declaration is the referenced one (§1985:X.1.3.8.2 GR3a), which
/// <see cref="NearestDeclaringData"/> and <see cref="NearestDeclaringConditions"/> already decide.</para>
/// </summary>
public sealed partial class SymbolTable
{
    /// <summary>Whether the edition puts condition-names in the data-names' disjoint set (COBOL-85 only).</summary>
    private bool ConditionNamesShareTheDataNameSet => _data.Edition.Edition.ConditionNamesShareTheDataNameSet;

    /// <summary>How many condition-names of <paramref name="name"/> the written <paramref name="quals"/> also reach in the source element
    /// <paramref name="depth"/> containment levels out, at COBOL-85: the level-88s (a type declaration's identify nothing alone,
    /// §2023:13.18.58.4 GR1) and, for an unqualified reference, a SPECIAL-NAMES switch's condition-name of that tier, which takes no
    /// qualifier. Zero at every later edition.</summary>
    private int ConditionNamesRivalingData(string name, IReadOnlyList<string> quals, Scope scope, int depth)
    {
        if (!ConditionNamesShareTheDataNameSet) return 0;
        int rivals = 0;
        foreach (var c in ConditionsUnderQualifiers(name, quals, scope))
            if (!DataBinder.IsTypeDeclarationCondition(c) && _data.State.Forest.DeclaringDepth(c.Parent) == depth) rivals++;
        if (quals.Count == 0 && SwitchConditionDepth(name) == depth) rivals++;
        return rivals;
    }

    /// <summary>How many data items of <paramref name="written"/>'s spelling its qualifiers also reach in the tier of the level-88s
    /// <paramref name="matches"/> it names (one tier: <see cref="IdentifyConditionName"/> narrowed them), at COBOL-85. Zero at every
    /// later edition. Pure: it reports nothing.</summary>
    internal int DataItemsRivalingCondition(WrittenQualifiedName written, IReadOnlyList<Condition88> matches)
    {
        if (!ConditionNamesShareTheDataNameSet || matches.Count == 0
            || !TryResolve(written.Name, _data.ActiveScope, out var items)) return 0;
        int depth = _data.State.Forest.DeclaringDepth(matches[0].Parent);
        int rivals = 0;
        foreach (var item in items)
            if (!DataBinder.InTypeDeclaration(item) && _data.State.Forest.DeclaringDepth(item) == depth
                && QualifierChainMatches(item, written.QualifierWords))
                rivals++;
        return rivals;
    }
}
