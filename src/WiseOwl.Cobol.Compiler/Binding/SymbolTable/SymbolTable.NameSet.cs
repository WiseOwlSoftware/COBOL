// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Compiler.Binding.Model;

namespace WiseOwl.Cobol.Compiler.Binding;

/// <summary>The name classes of §1985:X.1.3.8.2 GR1's set as the one resolver <c>SymbolTable.NearestInNameSet</c> reads them, a set of
/// them being a question's classes. Each member is named after the <see cref="DeclarationKind"/> its declarations are answered as
/// (<c>GlobalNameClassDriftTests</c> holds the two lists and the resolver's arms to one another, kb/Work PB5975).</summary>
[Flags]
internal enum NameSetClass
{
    /// <summary>No class.</summary>
    None = 0,
    /// <summary>An ordinary data description entry's data-name (a record-name among them).</summary>
    DataItem = 1 << 0,
    /// <summary>The data-name of a report group description entry, a SUM entry's included.</summary>
    ReportEntry = 1 << 1,
    /// <summary>A sum counter, held off the data-names in the table's own counters.</summary>
    ReportSumCounter = 1 << 2,
    /// <summary>A level-88 condition-name.</summary>
    ConditionName = 1 << 3,
    /// <summary>A constant-name.</summary>
    ConstantName = 1 << 4,
    /// <summary>An index-name.</summary>
    IndexName = 1 << 5,
    /// <summary>A file-name.</summary>
    FileName = 1 << 6,
    /// <summary>A report-name.</summary>
    ReportName = 1 << 7,
    /// <summary>A cd-name (this element's own only).</summary>
    CdName = 1 << 8,
    /// <summary>A named CAPACITY register.</summary>
    CapacityRegister = 1 << 9,
    /// <summary>Every class.</summary>
    All = (1 << 10) - 1,
}

public sealed partial class SymbolTable
{
    /// <summary>⛔ THE ONE RESOLVER OF §1985:X.1.3.8.2 ACROSS NAME CLASSES (kb/Work PB5975; §2023:8.4.6.2.1 is its successor). GR1:
    /// "The set of names to be used for determination of a referenced object consists of all names which are defined in program B
    /// and all global names which are defined in program A and in any programs which directly or indirectly contain program A. Using
    /// this set of names, the normal rules for qualification and any other rules for uniqueness of reference are applied until one or
    /// more objects is identified." GR3a: "If the name is declared in program B, the object in program B is the referenced object",
    /// whatever class it is declared as (§1985:X.1.3.8: a reference "even when it is a different type of user-defined word, is to the
    /// object which that program describes"). So the question every lookup asks of its own nearest tier, "is the spelling declared
    /// NEARER, in another class?", is ONE question over the whole set: the nearest declaration of <paramref name="name"/> of the
    /// <paramref name="classes"/> asked, that the written <paramref name="qualifiers"/> reach, as a fact (its kind, its depth, its
    /// entry's line), or null when the set holds none. A class that takes no qualifier (a file-name, report-name, constant-name,
    /// cd-name, sum counter or CAPACITY register held by spelling) is in the set only for an unqualified reference: qualification is
    /// applied first, and a qualified reference does not identify it. A type declaration's condition-name identifies nothing alone
    /// (§2023:13.18.58.4 GR1), so it is not in the set. A declaration that its own nearer declaration hides is still in the set at its
    /// own depth: whatever hides it is nearer still, so no "nearer than" answer changes.
    /// <para>Every narrowing asks it — the data-name and condition-name tiers (<see cref="NearestDeclaringData"/>,
    /// <see cref="NearestDeclaringConditions"/>), the classes resolved before them (<see cref="NearerNameOfAnotherClassHides"/>), the
    /// inherited names a nearer declaration hides (<see cref="Declarations.DeclaredNearerThan"/>), and the refusals that say what
    /// hides a name (<see cref="Declarations.ConditionNameHiddenByNearerNonDataName"/>, <see cref="Declarations.ReportNameHiddenBy"/>)
    /// — so a class added here is ranked against every other class at once, and no class pair is remembered by hand. At a tie the
    /// first arm keeps the answer, the order <see cref="Declarations.OtherThanData"/> words a tie in.</para></summary>
    private DeclarationFact? NearestInNameSet(string name, IReadOnlyList<string> qualifiers, NameSetClass classes)
    {
        DeclarationFact? nearest = null;
        var key = Key(name);
        bool unqualified = qualifiers.Count == 0;
        if ((classes & NameSetClass.DataItem) != 0 && _dataNames.TryGetValue(key, out var items))
            foreach (var item in items)
                if (QualifierChainMatches(item, qualifiers))
                    Offer(DeclarationKind.DataItem, _data.State.Forest.DeclaringDepth(item), item.ReferenceSite.DeclaredAt.Line);
        if ((classes & NameSetClass.ReportEntry) != 0 && _data.State.Reports.Visible.Count != 0
            && EntriesNamed(name, qualifiers) is [var (entryReport, entry), ..])
            Offer(entry.IsSumCounter ? DeclarationKind.ReportSumCounter : DeclarationKind.ReportEntry,
                _data.State.Reports.Depth(entryReport));
        if ((classes & NameSetClass.ReportSumCounter) != 0 && unqualified && _sumCounters.TryGetValue(key, out var counters))
            foreach (var (counterReport, _) in counters)
                Offer(DeclarationKind.ReportSumCounter, _data.State.Reports.Depth(counterReport));
        if ((classes & NameSetClass.ConditionName) != 0 && _conditionNames.TryGetValue(key, out var conditions))
            foreach (var c in conditions)
                if (!DataBinder.IsTypeDeclarationCondition(c) && ConditionQualifierChainMatches(c, qualifiers))
                    Offer(DeclarationKind.ConditionName, _data.State.Forest.DeclaringDepth(c.Parent), c.DeclaredAt.Line);
        if ((classes & NameSetClass.ConstantName) != 0 && unqualified && IsConstantName(name))
            Offer(DeclarationKind.ConstantName, InheritedConstantDepthOf(name));
        if ((classes & NameSetClass.IndexName) != 0 && _indexNames.Declares(key)
            && _indexNames.Candidates(key, d => IndexQualifierChainMatches(d, qualifiers), out int indexDepth) is { Count: > 0 } indexes)
            Offer(DeclarationKind.IndexName, indexDepth, indexes.Single is { } index ? index.Table.ReferenceSite.DeclaredAt.Line : 0);
        if ((classes & NameSetClass.FileName) != 0 && unqualified)
        {
            if (_ownFiles.TryGetValue(key, out var own)) Offer(DeclarationKind.FileName, 0, own.EntryAt.Line);
            else if (_inheritedFiles.TryGetValue(key, out var inherited))
                Offer(DeclarationKind.FileName, _data.State.Inherited.FileDepth(inherited), inherited.EntryAt.Line);
        }
        if ((classes & NameSetClass.ReportName) != 0 && unqualified && _visibleReports.TryGetValue(key, out var report))
            Offer(DeclarationKind.ReportName, _data.State.Reports.Depth(report), report.At.Line);
        if ((classes & NameSetClass.CdName) != 0 && unqualified && _cds.ContainsKey(key))
            Offer(DeclarationKind.CdName, 0);   // a cd-name is this element's own (§1985:X.1.3.8), never inherited
        if ((classes & NameSetClass.CapacityRegister) != 0 && unqualified && NearestCapacityRegisterTable(name) is { } table)
            Offer(DeclarationKind.CapacityRegister, _data.State.Forest.DeclaringDepth(table), table.ReferenceSite.DeclaredAt.Line);
        return nearest;

        void Offer(DeclarationKind kind, int depth, int line = 0)
        {
            if (nearest is not { } best || depth < best.Depth) nearest = new DeclarationFact(kind, Depth: depth, Line: line);
        }
    }

    /// <summary>Is <paramref name="name"/> declared, in one of <paramref name="classes"/> and reached by the written
    /// <paramref name="qualifiers"/>, in a source element NEARER than <paramref name="depth"/> (<see cref="NearestInNameSet"/>)? A tie
    /// is not nearer: at one depth one element declares both, a conflict of its own (§1985:IV.4.2.2.1.1, §2023:8.3.2.2). Free at depth
    /// 0, where nothing is nearer.</summary>
    private bool DeclaredNearerInNameSet(string name, IReadOnlyList<string> qualifiers, int depth, NameSetClass classes) =>
        depth > 0 && NearestInNameSet(name, qualifiers, classes) is { } nearer && nearer.Depth < depth;
}
