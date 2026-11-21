// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Symbols;

namespace WiseOwl.Cobol.Binding;

/// <summary>
/// THE §2023:8.4.2.2 CANDIDATE SETS OF A WRITTEN DATA-NAME, private to the table (docs/rearchitecture/DESIGN-listing.md section 6.7.1,
/// "Step 6b, remainder slice 3"; kb/Work PB5932). The qualification walk (§2023:8.4.2.2.3 SR4), the one candidate set of a spelling in a
/// scope, the set within one record or region, and the data description entry clause's implicit qualification (§2023:8.4.2.2.1 SR5)
/// were <c>DataBinder</c>'s, reached with the name as text; they are the table's now, and a caller hands the written words: the
/// procedure division a <see cref="WrittenQualifiedName"/>, a data division clause the <see cref="WrittenDataName"/> its model keeps.
/// The questions that answer no symbol (whether a spelling is declared, how many declarations the qualifiers reach, whether a
/// candidate's chain carries the written qualifiers) take the words as text: a <c>bool</c> or a count names nothing a caller could
/// use as an operand.
/// </summary>
public sealed partial class SymbolTable
{
    /// <summary>The written data-name's candidate set in <paramref name="scope"/> (the procedure division's
    /// <see cref="IdentifyDataName(WrittenQualifiedName, bool)"/> asks it in the active scope; the storage-claim scan in the scope of
    /// the method whose procedure division it reads).</summary>
    internal NameCandidates<DataItem> IdentifyDataName(WrittenQualifiedName written, Scope scope, bool report)
    {
        var candidates = DataNameCandidates(written.Name, written.QualifierWords, scope);
        if (report && candidates.Single is { } one) RecordData(one, written, report);
        return candidates;
    }

    /// <summary>
    /// The candidate set of a data-name referenced in a DATA DESCRIPTION ENTRY CLAUSE of <paramref name="subject"/> (OCCURS ...
    /// DEPENDING ON, SAME AS), with §2023:8.4.2.2.1 SR5's implicit qualification applied (from 2002,
    /// <see cref="ClauseOperandsTakeImplicitQualifiers"/>): "The name is a data-name referenced in a data description entry clause whose
    /// subject is subordinate to the same group item as that data-name. In this case, the names of any group items superordinate to
    /// both the data-name and the subject of the data description entry clause are used as implicit qualifiers for the reference, in
    /// addition to any explicit qualifiers needed to establish uniqueness within that group." So the set is the one under the
    /// INNERMOST group superordinate to the subject that holds any candidate (every candidate there shares exactly the same implicit
    /// qualifiers, so two of them are still ambiguous), and the scope-wide set only when no group of the subject's holds one. This is
    /// also the rule behind a TYPEDEF clone's own DEPENDING ON counter (a tree walk, so a clone's members off the name index are
    /// found). Counted like every candidate set (kb/Work PB978).
    /// </summary>
    internal NameCandidates<DataItem> IdentifyEntryClauseOperand(DataItem subject, WrittenDataName operand, Scope scope)
    {
        if (ClauseOperandsTakeImplicitQualifiers)
            for (DataItem? group = subject.Parent; group is not null; group = group.Parent)
                if (SubtreeCandidates(group, operand.Name, operand.Qualifiers) is { Count: > 0 } within) return Recorded(within, operand);
        return Recorded(DataNameCandidates(operand.Name, operand.Qualifiers, scope), operand);
    }

    /// <summary>The candidate set of a data-name an entry clause confines to ONE region (OCCURS KEY's data-name-2 to the table,
    /// §2023:13.18.38.3 SR3; RENAMES' data-name-2 and data-name-3 to the record, §2023:13.18.45.3 SR4), with the same edition step as
    /// <see cref="IdentifyEntryClauseOperand"/> (kb/Work PB5165). From 2002 the region's own subtree is the set, the superordinate
    /// groups acting as implicit qualifiers; below 2002 nothing narrows, the whole scope's candidates are counted, and the caller asks
    /// <see cref="DataBinder.IsWithinRegion"/> of the one that survives. One name declared in the region and in another record is then
    /// §2023:8.4.2.2.3 SR1's ambiguity (COBOL1639), as in the procedure division.</summary>
    internal NameCandidates<DataItem> IdentifyRegionOperand(DataItem region, WrittenDataName operand) =>
        Recorded(ClauseOperandsTakeImplicitQualifiers
            ? SubtreeCandidates(region, operand.Name, operand.Qualifiers)
            : DataNameCandidates(operand.Name, operand.Qualifiers, _data.ScopeOf(region.Root)), operand);

    /// <summary>A data division clause operand's candidate set, its occurrence recorded when the set has one member: a clause operand
    /// is the reference itself (the clause binders ask no probe of it), and the position it is written at is the operand's
    /// (<see cref="WrittenDataName.Line"/>), none for an operand no source wrote.</summary>
    private NameCandidates<DataItem> Recorded(NameCandidates<DataItem> candidates, WrittenDataName operand)
    {
        if (_entries.RecordsReferences && operand.Line > 0 && candidates.Single is { } one)
            _entries.Record(DataEntryOf(one), new SymbolPosition(operand.Line, operand.Column), operand.Length);
        return candidates;
    }

    /// <summary>The candidate set of a written data-name WITHIN the record or table <paramref name="root"/> (itself included): a SORT
    /// table key, §2023:14.9.40.3 SR14a ("The data item identified by a key data-name shall be the same as, or subordinate to, the data
    /// item referenced by data-name-2"), narrowed by the written qualifiers and counted (kb/Work PB1018).</summary>
    internal NameCandidates<DataItem> IdentifyWithin(DataItem root, WrittenQualifiedName written, bool report)
    {
        var candidates = SubtreeCandidates(root, written.Name, written.QualifierWords);
        if (report && candidates.Single is { } one) RecordData(one, written, report);
        return candidates;
    }

    /// <summary>Whether a data item of <paramref name="word"/> is visible in <paramref name="scope"/> at all, its qualifiers not
    /// applied: the procedure division's word-class probes (is the word a data-name here before it is read as an alphabet-name, a
    /// keyword, a type-name, a function or a file), and the diagnostic hint that a procedure-name is a data-name. A declaration
    /// question: no symbol answers, and a probe records no occurrence.</summary>
    internal bool DeclaresDataName(string word, Scope scope) => TryResolve(word, scope, out _);

    /// <summary>How many data items of <paramref name="name"/> are visible in <paramref name="scope"/> (<c>Declared</c>, 0 when the
    /// spelling names none) and how many of them the written <paramref name="qualifiers"/> reach (<c>Reached</c>), before the
    /// §2023:8.4.6.2.1 GR3 narrowing: the counts the unidentified-reference wording quotes (<c>NameResolution.ReportUnidentified</c>).</summary>
    internal (int Declared, int Reached) DataNameDeclarationCounts(string name, IReadOnlyList<string> qualifiers, Scope scope)
    {
        if (!TryResolve(name, scope, out var candidates)) return (0, 0);
        int reached = 0;
        foreach (var c in candidates) if (QualifierChainMatches(c, qualifiers)) reached++;
        return (candidates.Count, reached);
    }

    /// <summary>The entries a REDEFINES clause of <paramref name="redefiner"/> may name, read off the redefiner, which carries its
    /// data-name-2 as written (<see cref="DataItem.RedefinesTargetName"/>): every entry of <paramref name="scope"/> (the redefiner's
    /// siblings, or for a level-01 or level-77 entry the roots of its own section's records) that PRECEDES it and is so named, in
    /// declaration order. Data-name-2 is an entry of the subject's level-number (§2023:13.18.44.3 SR2) that the subject follows
    /// (SR10), and "Data-name-2 shall not be qualified" (SR6), whose NOTE 1 says that when the name is not unique "no ambiguity of
    /// reference exists because of the required placement", so the caller picks among these by placement
    /// (<see cref="DataItem.RedefinedEntry"/>), never by a qualifier. A name a symbol carries is resolved by the table from the symbol
    /// (section 6.7.1), so no caller compares it.</summary>
    internal List<DataItem> RedefinitionCandidatesOf(DataItem redefiner, IReadOnlyList<DataItem> scope)
    {
        List<DataItem> preceding = [];
        if (redefiner.RedefinesTargetName is not { } written) return preceding;
        foreach (var s in scope)
        {
            if (ReferenceEquals(s, redefiner)) break;
            if (CobolNames.Same(s.CobolName, written) && (redefiner.Parent is not null || s.Section == redefiner.Section))
                preceding.Add(s);
        }
        return preceding;
    }

    /// <summary>
    /// ⭐ THE ONE ISO §2023:8.4.2.2 QUALIFICATION RESOLVER (kb/Work PB489). Every in-scope declaration of <paramref name="name"/> whose
    /// ancestor chain carries each written qualifier in order (inner -> outer, §2023:8.4.2.2.2 Format 1: qualifiers need not name
    /// consecutive levels), with the OUTERMOST qualifier optionally naming the owning FILE instead: Format 1 is <c>data-name-1 [
    /// data-qualifier ] ... [ file-report-qualifier ]</c>, so the FD/SD is the highest permissible qualifier (SQ207M's <c>WRITE
    /// PRINT-REC IN PRINT-FILE</c>).
    /// <para>⛔ IT RETURNS THE CANDIDATE SET, NOT A WINNER, because §2023:8.4.2.2.1 makes the number of survivors the whole answer:
    /// "uniqueness shall be established through qualification for each user-defined name explicitly referenced". One survivor
    /// resolves; zero or several is a resolution FAILURE the caller reports. A caller that took the first survivor would be
    /// substituting declaration order for the standard's uniqueness requirement, which is exactly what the file-clause lookups did (a
    /// bare <c>ByName[n][0]</c>) until PB489.</para>
    /// <para>⛔ ONE RULE FOR THE UNQUALIFIED AND THE QUALIFIED REFERENCE (kb/Work PB5932's dated file <c>2026-11-19-step-6b-r1</c>, its
    /// lead, decided here). An unqualified name used to be <c>TryResolveUnqualified</c>'s set and a qualified one this walk's, which
    /// also drops a set of lapsed type-declaration members alone and drops a repeated candidate. The two answers are one: with no
    /// qualifier the walk keeps every candidate, a candidate list is never a lapsed member alone (<see cref="TryResolve"/>'s list is the
    /// spelling's storage definitions, never empty, with the members that lapse beside them, <see cref="WithLapsedTypeMembers"/>), and
    /// the multimap holds each item once. So an unqualified reference reads the table's list as it stands (no copy: section 9's
    /// budget, every data reference is identified here) and both arms are this method.</para>
    /// </summary>
    private NameCandidates<DataItem> DataNameCandidates(string name, IReadOnlyList<string> quals, Scope scope)
    {
        if (!TryResolve(name, scope, out var candidates)) return new([]);
        List<DataItem> survivors;
        if (quals.Count == 0)
            survivors = candidates;
        else
        {
            survivors = [];
            foreach (var cand in candidates)
                if (QualifierChainMatches(cand, quals) && !survivors.Contains(cand))
                    survivors.Add(cand);
            // A lapsed type-declaration member (WithLapsedTypeMembers) only ever COUNTS against a storage item the qualifiers also
            // reach; alone it identifies nothing: a type declaration has no storage (§2023:13.18.58.4 GR2), and its members are
            // referenced "only as subordinate items of groups defined using the type-name" (GR1), so `X OF T` naming the type itself
            // stays an invalid reference.
            if (survivors.TrueForAll(DataBinder.InTypeDeclaration)) return new([]);
        }
        // §2023:8.4.6.2.1 GR3 AFTER qualification (§2023:8.4.6.2.1 GR1 applies "the normal rules for qualification" over the whole set first): the
        // survivors of the nearest declaring source element (kb/Work PB1047 / PB1243), and none when a report entry of a nearer source
        // element is the referenced item (kb/Work PB4196).
        var tier = NearestDeclaringData(survivors, name, quals);
        // ⛔ A REPORT SECTION ENTRY OF THE SAME SPELLING AND SOURCE ELEMENT IS ANOTHER DECLARATION OF THE NAME (kb/Work PB5775): its
        // data-name is a user-defined name, so "uniqueness must then be established through qualification for each user-defined name
        // explicitly referenced" (§1985:IV.4.3.8.1 SRL2.1; §2023:8.4.2.2.3 SR1) counts it, and `MOVE "Z" TO DA` over a Working-Storage
        // DA and a report group DA is nonunique, never the storage item (the report entry is named by DA OF the-report). It counts
        // WITH the items and resolves to none, and only beside one: a name the report section alone declares is the report-item
        // refusal's (ReportSectionBinder.ReportEntryReferenceRefused).
        int reportEntries = tier.Count == 0 ? 0 : _data.ReportSection.ReportEntriesInTierOf(new NameCandidates<DataItem>(tier), name, quals);
        // ⛔ AT COBOL-85 A CONDITION-NAME OF THE SAME SPELLING AND SOURCE ELEMENT IS ANOTHER NAME OF THE SAME DISJOINT SET (kb/Work
        // PB5972, §1985:IV.4.2.2.1.1): counted with the items as the report entries are, so `DISPLAY X` over a data item X and a
        // level-88 X is nonunique (SRL2.1); `X OF R2` that reaches the data item alone is not.
        int conditionNames = tier.Count == 0 ? 0 : ConditionNamesRivalingData(name, quals, scope, _data.State.Forest.DeclaringDepth(tier[0]));
        return new NameCandidates<DataItem>(tier, reportEntries, conditionNames);
    }

    /// <summary>The §2023:8.4.2.2 candidate set of <paramref name="name"/> WITHIN one record: every entry of <paramref name="root"/>'s
    /// subtree (itself included) so named whose ancestor chain carries the written qualifiers (<see cref="QualifierChainMatches"/>).
    /// For the operands confined to one record: RENAMES data-name-2 / data-name-3 (§2023:13.18.45.3 SR4, "in the same record"), a
    /// TYPEDEF clone's own OCCURS DEPENDING ON counter and a SORT table key. The tree walk, not the name index, because a TYPEDEF
    /// template's members are off the name index (§2023:13.18.58.4 GR1). Counted like every other candidate set (kb/Work PB978).</summary>
    private NameCandidates<DataItem> SubtreeCandidates(DataItem root, string name, IReadOnlyList<string> quals)
    {
        List<DataItem> hits = [];
        void Walk(DataItem n)
        {
            if (CobolNames.Same(n.CobolName, name) && QualifierChainMatches(n, quals))
                hits.Add(n);
            foreach (var c in n.Children) Walk(c);
        }
        Walk(root);
        return new NameCandidates<DataItem>(hits);
    }

    /// <summary>⛔ WHETHER A NAME REFERENCED IN A DATA DESCRIPTION ENTRY CLAUSE TAKES THE GROUPS ABOVE ITS SUBJECT AS IMPLICIT
    /// QUALIFIERS (kb/Work PB5165): the ONE edition test of that rule, asked by <see cref="IdentifyEntryClauseOperand"/> (OCCURS ...
    /// DEPENDING ON, SAME AS) and <see cref="IdentifyRegionOperand"/> (OCCURS KEY, RENAMES). It is ISO/IEC 1989:2002's change
    /// (§2002:F.2, item 67: "Data-names referenced in a data description entry need not be qualified when they are defined in the same
    /// group as the subject of the entry"), carried on by §2023:8.4.2.2.1 SR5. §1985:IV.4.3.8.1 SR3 has only the exception "in the case
    /// of redefinition": every other explicitly referenced name must be made unique by qualification, so below 2002 the whole scope's
    /// candidates are counted (§1985:VI.5.8.3 SR2 and §1985:VI.5.11.3 SR5 give these clauses the qualifier to say it with).</summary>
    private bool ClauseOperandsTakeImplicitQualifiers => _data.Edition.DialectLevel >= 2002;

    /// <summary>True when every qualifier names strictly-superordinate context of <paramref name="cand"/>, consumed inner -> outer with
    /// gaps allowed (ISO §2023:8.4.2.2.3 SR4: "Qualifiers shall be specified in the order of successively more inclusive levels in the
    /// hierarchy"); when the data ancestors are exhausted, the OUTERMOST remaining qualifier may instead name the file whose FD/SD owns
    /// the candidate's record (§2023:8.4.2.2.2 Format 1's <i>file-report-qualifier</i>). A question over a candidate already in hand: it
    /// answers no symbol.</summary>
    internal bool QualifierChainMatches(DataItem cand, IReadOnlyList<string> qualifiers) =>
        QualifierChainMatchesFrom(cand.Parent, cand, qualifiers);

    /// <summary>The §2023:8.4.2.2.2 Format 3 (qualified-index-name) twin of <see cref="QualifierChainMatches"/>: <c>index-name-1 [
    /// data-qualifier ] ... [ file-report-qualifier ]</c>, whose hierarchy §2023:8.4.2.2.3 SR6 gives: "The qualification of an
    /// index-name may include the name of the table with which the index-name is associated, as well as any name by which that table
    /// may be qualified." So the chain starts AT the table, not above it; the matching itself is the same one walk (kb/Work
    /// PB919).</summary>
    private bool IndexQualifierChainMatches(IndexDeclaration decl, IReadOnlyList<string> qualifiers) =>
        QualifierChainMatchesFrom(decl.Table, decl.Table, qualifiers);

    /// <summary>The §2023:8.4.2.2.2 Format 2 (qualified-condition-name) twin of <see cref="QualifierChainMatches"/>: §2023:8.4.2.2.3
    /// SR4, "For purposes of item qualification, the hierarchy of an item declared at level 88 includes the conditional variable with
    /// which it is associated", and SR5, "The qualification of a condition-name may include the conditional variable ... as well as by
    /// any name by which that conditional variable may be qualified". So the chain starts AT the conditional variable (kb/Work PB567:
    /// a private fourth copy of this walk without the file-name arm could never resolve a condition-name qualified through its
    /// record's FILE).</summary>
    private bool ConditionQualifierChainMatches(Condition88 cond, IReadOnlyList<string> qualifiers) =>
        QualifierChainMatchesFrom(cond.Parent, cond.Parent, qualifiers);

    /// <summary>The one qualifier walk of a data item: <paramref name="firstQualifiable"/> is the innermost entry a qualifier may
    /// name; <paramref name="member"/> locates the record for the file-name qualifier.</summary>
    private bool QualifierChainMatchesFrom(DataItem? firstQualifiable, DataItem member, IReadOnlyList<string> qualifiers)
    {
        if (qualifiers.Count == 0) return true;
        return QualifierWalk(NamesFrom(firstQualifiable), qualifiers, q =>
        {
            // Data ancestors exhausted: the OUTERMOST remaining qualifier may be the file name: any file-name visible here, a
            // container's GLOBAL one included (VisibleFiles; kb/Work PB3557 review R2-1).
            DataItem root = member;
            while (root.Parent is { } p) root = p;
            // A record of a communication description entry is qualified by its cd-name (ISO/IEC 1989:2002 8.4.1.1.1's
            // file-cd-report-qualifier).
            if (CdQualifies(q, root) is { } describes) return describes;
            return FileQualifies(q, root);
        });

        static IEnumerable<string?> NamesFrom(DataItem? anc)
        {
            for (; anc is not null; anc = anc.Parent) yield return anc.CobolName;
        }
    }

    /// <summary>⛔ THE ONE §2023:8.4.2.2.3 SR4 QUALIFIER WALK: the written qualifiers are consumed INNER -> OUTER against
    /// <paramref name="ancestorNames"/> (the names of the successively more inclusive levels above the item, innermost first; a level
    /// with no name is a null), with gaps allowed, and when the ancestors are exhausted the OUTERMOST remaining qualifier may instead be
    /// the container <paramref name="isOutermostContainer"/> accepts (§2023:8.4.2.2.2 Format 1's <i>file-report-qualifier</i>: the
    /// owning FILE of a data record, the owning REPORT of a report-section sum counter or entry, kb/Work PB1454). The hierarchy differs
    /// by item kind; the rule does not, so it is written once and each kind supplies its own ancestors and its own outermost
    /// container.</summary>
    private static bool QualifierWalk(
        IEnumerable<string?> ancestorNames, IReadOnlyList<string> qualifiers, Func<string, bool> isOutermostContainer)
    {
        using var ancestors = ancestorNames.GetEnumerator();
        for (int qi = 0; qi < qualifiers.Count; qi++)
        {
            string q = qualifiers[qi];
            bool found = false;
            while (!found && ancestors.MoveNext())
                found = CobolNames.Same(ancestors.Current, q);
            if (found) continue;
            return qi == qualifiers.Count - 1 && isOutermostContainer(q);
        }
        return true;
    }
}
