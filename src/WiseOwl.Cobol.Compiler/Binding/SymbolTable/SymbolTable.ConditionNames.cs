// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding;

public sealed partial class SymbolTable
{
    /// <summary>
    /// ⭐ THE LOOKUP OF A WRITTEN CONDITION-NAME (§2023:8.4.2.2.2 Format 2; docs/rearchitecture/DESIGN-listing.md section 6.7.1,
    /// "Step 6b, remainder slice 1"): its §2023:8.4.2.2 candidate set, whose count is the caller's uniqueness verdict
    /// (<c>ConditionBinder.ConditionOf</c> reports it; a pass that only reads which condition a bare operand names,
    /// <c>FlagConformancePass</c>, ignores it, kb/Work PB3288). Every level-88 of the spelling visible in the active scope (the
    /// method's own first, §2023:11.7.4 GR5) whose conditional-variable chain carries each written qualifier (§2023:8.4.2.2.3 SR4
    /// and SR5), narrowed to the nearest declaring source element (§2023:8.4.6.2.1 3), kb/Work PB1047), and none when a data-name,
    /// report entry, constant-name, index-name or file-name of the spelling is declared nearer (kb/Work PB4196, PB4219, PB4432),
    /// or when every survivor is a lapsed type-declaration condition (§2023:8.4.2.2.1 SR4: alone it names nothing to test).
    /// <para>A qualifiable name's identification IS its candidate set, typed and unboxed, as the index-name's is
    /// (<see cref="IdentifyIndexName"/>): the data-names' twin is <see cref="IdentifyDataName"/>. <paramref name="report"/> is
    /// <see cref="Identify(Antlr4.Runtime.ParserRuleContext, NameClass, bool, NameReach, ProcedureSite?, ReportSite)"/>'s:
    /// listing step 7b records the occurrence here when it is true and the set has one member. Pure: it reports nothing.</para>
    /// </summary>
    internal List<Condition88> IdentifyConditionName(WrittenQualifiedName written, bool report)
    {
        var scope = _data.ActiveScope;
        var matches = NearestDeclaringConditions(ConditionsUnderQualifiers(written.Name, written.QualifierWords, scope), written.Name,
            written.QualifierWords, scope);   // kb/Work PB4196, PB4219, PB4432
        if (matches.TrueForAll(DataBinder.IsTypeDeclarationCondition)) return [];
        if (report && matches is [var one]) Record(DataEntryOf(one), written, report);
        return matches;
    }

    /// <summary>Whether a level-88 condition-name of <paramref name="written"/>'s spelling is visible in the active scope at all,
    /// whatever its qualifiers reach: the question that chooses between two MESSAGES (a misqualified condition-name, a word that
    /// is no condition-name), never which declaration a reference names. A declaration question: no symbol answers.</summary>
    internal bool DeclaresConditionName(WrittenQualifiedName written) =>
        TryResolveCondition(written.Name, _data.ActiveScope, out var conditions) && conditions.Count > 0;

    /// <summary>The nearest declaring source element's level-88s that <paramref name="written"/>'s qualifiers reach, BEFORE a nearer
    /// declaration of another name class hides them: the tier the §2023:8.4.6.2.1 3) a) question "is a nearer element's declaration
    /// the referenced item?" is asked of (<c>ConditionBinder</c>'s CAPACITY register arm).</summary>
    internal List<Condition88> ConditionTier(WrittenQualifiedName written) =>
        ConditionTier(written.Name, written.QualifierWords, _data.ActiveScope);

    /// <summary>⛔ ISO §2023:8.4.6.2.1 3) a) SEEN FROM A CONDITION-ONLY POSITION (kb/Work PB4219 review R1-1): the written qualifiers
    /// reach a level-88 of the spelling, but an ordinary data item of the spelling declared in a NEARER source element hides it, and
    /// that data item is what the reference names (it survives <see cref="DataNameCandidates"/>, so no report entry or
    /// condition-name nearer still takes the reference from it). "If the name is declared in source element B, the item in source
    /// element B is the referenced item." Pure: it reports nothing.</summary>
    internal bool ConditionNameHiddenByNearerDataItem(WrittenQualifiedName written)
    {
        var scope = _data.ActiveScope;
        return NearerDataItemHidesConditions(ConditionsUnderQualifiers(written.Name, written.QualifierWords, scope), written.Name,
                   written.QualifierWords)
               && DataNameCandidates(written.Name, written.QualifierWords, scope).Count != 0;
    }

    /// <summary>The tier of <see cref="ConditionTier(WrittenQualifiedName)"/>, for a name the table already holds as its words
    /// (<see cref="Declarations.ConditionNameHiddenByNearerNonDataName"/>).</summary>
    private List<Condition88> ConditionTier(string name, IReadOnlyList<string> qualifiers, Scope scope) =>
        NearestDeclaring(ConditionsUnderQualifiers(name, qualifiers, scope), static c => c.Parent);

    /// <summary>Every level-88 of <paramref name="name"/> visible in <paramref name="scope"/> whose conditional-variable chain carries
    /// each written qualifier, BEFORE the §2023:8.4.6.2.1 3) narrowing: the qualifier walk of <see cref="IdentifyConditionName"/>,
    /// <see cref="ConditionTier(string, IReadOnlyList{string}, Scope)"/> and <see cref="ConditionNameHiddenByNearerDataItem"/>.</summary>
    private List<Condition88> ConditionsUnderQualifiers(string name, IReadOnlyList<string> qualifiers, Scope scope)
    {
        if (!TryResolveCondition(name, scope, out var list)) return [];
        return qualifiers.Count == 0 ? list : list.Where(c => ConditionQualifierChainMatches(c, qualifiers)).ToList();
    }
}
