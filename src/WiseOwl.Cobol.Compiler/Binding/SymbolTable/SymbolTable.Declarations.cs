// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Binding;

using Core = WiseOwl.Cobol.Frontend.Generated.CobolParserCore;

public sealed partial class SymbolTable
{
    /// <summary>The data division's DECLARATION questions (docs/rearchitecture/DESIGN-listing.md section 6.7.1, "Declarations
    /// cannot answer a reference"): is the spelling declared, as what class, how near, does a nearer declaration of another
    /// class hide it. Every answer is a <see cref="DeclarationFact"/> or a <c>bool</c>, never a symbol, so no caller can turn
    /// it into an operand: a reference needs a symbol, and only <see cref="SymbolTable"/>'s lookup hands one out
    /// (<c>NameTableOwnershipDriftTests</c> rule (4)).</summary>
    internal sealed class Declarations
    {
        private readonly SymbolTable _table;
        private readonly DataBinder _data;

        internal Declarations(SymbolTable table)
        {
            _table = table;
            _data = table._data;
        }
        /// <summary>⛔ THE ONE ANSWER TO "WHAT IS THIS NAME DECLARED AS, IF NOT A DATA ITEM?" (kb/Work PB1906), asked by every
        /// site that refuses a reference for identifying no data item (ISO §2023:8.4.2.1: "a statement shall contain a reference
        /// that uniquely identifies that resource"): the procedure division's unidentified reference
        /// (<see cref="NameResolution.ReportUnidentified"/>), a file clause's data-name operand
        /// (<see cref="DataBinder.ResolveClauseOperand"/>) and an OCCURS DEPENDING ON object. Each used to screen its own subset, so a
        /// type-name was "not defined" at all three and an alphabet-, constant-, index- or condition-name at the two clause
        /// sites. Null when the name is declared as nothing but data — or as nothing at all — so the caller's own
        /// uniqueness verdict stands. A data-name of the same spelling the ranked lookup leaves standing (a condition-name
        /// declared NEARER hides it, kb/Work PB4219) wins over a constant-name, a condition-name and the type declarations (§2023:8.3.2.2 exception 3 lets a constant-name and a data-name share one word; the others are
        /// a §2023:8.3.2.2 conflict reported at the declaration). A REPORT SECTION entry is
        /// <see cref="ReportSectionBinder.ReportEntryReferenceRefused"/>'s: its refusal is a section rule (§2023:13.8.6.2.3), not this one.
        /// <para>⛔ A CONSTANT-NAME, AN INDEX-NAME, A FILE-NAME AND A CONDITION-NAME ARE RANKED BY THE ELEMENT THAT DECLARES
        /// THEM (kb/Work PB4219, PB4432): ISO
        /// §2023:8.4.6.2.1 3) a) makes the declaration of the source element B the referenced item, and b) the nearest
        /// containing one otherwise, so a contained program's own level-88 RELF is what `RELF` names there, not its
        /// container's GLOBAL file, constant-name or index-name RELF; each used to be asked first whatever the nesting. Each answer carries its
        /// <see cref="DeclarationFact.Depth"/> so a caller that weighs it against a declaration this query does not
        /// rank (APPLY COMMIT's GLOBAL file, <see cref="FileControlBinder.ScreenEnclosingFileOperand"/>) compares like with like.</para></summary>
        public DeclarationFact? OtherThanData(string name, IReadOnlyList<string> qualifiers, Scope scope)
        {
            if (_table.DeclaresScreenName(name)) return new(DeclarationKind.ScreenEntry);
            if (_data.SpecialNames.IsAlphabetName(name)) return new(DeclarationKind.AlphabetName);
            // An unqualified reference's data-name answer is the RANKED one (§2023:8.4.6.2.1 3)): a container's GLOBAL data item that
            // a nearer condition-name hides is no answer here, so the condition-name below is (kb/Work PB4219). A qualified
            // one keeps the raw answer, which the callers word as "declared, but not under the given qualifiers".
            bool dataName = qualifiers.Count == 0
                ? _table.TryResolveUnqualified(name, scope, out _)
                : _table.TryResolve(name, scope, out _);
            // ⛔ THE NON-DATA DECLARATIONS OF THE SPELLING ARE RANKED TOGETHER, by the depth of the element that declares each
            // (§2023:8.4.6.2.1 3), kb/Work PB4219, PB4432): the classes NonConditionDeclarationsOf offers (constant-name, index-name,
            // file-name; the CAPACITY register has its own one lookup) and a condition-name. The nearest wins; at EQUAL depth one element declares both, a
            // §2023:8.3.2.2 conflict reported at the declaration, and the first offered keeps the answer as it always has.
            DeclarationFact? nearest = null;
            void Offer(DeclarationFact candidate)
            {
                if (nearest is not { } best || candidate.Depth < best.Depth) nearest = candidate;
            }
            foreach (var other in NonConditionDeclarationsOf(name, qualifiers, scope, dataName)) Offer(other);
            // A data-name the ranked lookup leaves standing answers for itself; the condition-name never takes it.
            if (!dataName && _table.TryResolveCondition(name, scope, out var conditions))
                Offer(new(DeclarationKind.ConditionName, Depth: conditions.Min(c => _data.State.Forest.DeclaringDepth(c.Parent))));
            if (nearest is { } answer) return answer;
            if (dataName) return null;
            if (qualifiers.Count == 0 && _table.TypeDeclarationNamed(Key(name), scope) is not null) return new(DeclarationKind.TypeName);
            // §2023:13.18.58.4 GR1: "Subordinate data description entries, condition-name entries, and RENAMES clauses are part of
            // the type declaration of that type" — a data-name or a condition-name such an entry declares.
            foreach (var declaration in _table.VisibleTypeDeclarations(scope))
            {
                if (DataItem.DescendantsOf(declaration).Any(i => CobolNames.Same(i.CobolName, name)))
                    return new(DeclarationKind.TypeDeclarationMember, declaration.CobolName);
                if (DataItem.DescendantsOf(declaration).Prepend(declaration)
                        .Any(i => i.Own88s.Exists(c => CobolNames.Same(c.Name, name))))
                    return new(DeclarationKind.TypeDeclarationCondition, declaration.CobolName);
            }
            return null;
        }

        /// <summary>THE NAME CLASSES BESIDE A CONDITION-NAME THAT <see cref="OtherThanData"/> WORDS, each with the depth of the element
        /// that declares it (kb/Work PB4432): the constant-name, the index-name, the file-name (this element's own SELECT or a
        /// containing element's GLOBAL file, §2023:13.18.27.4 GR1), the report-name and this element's cd-name (never inherited, so
        /// depth 0; kb/Work PB4713), in the order a tie at one depth keeps, the order of <see cref="SymbolTable.NearestInNameSet"/>'s
        /// arms. It answers "what is this name declared as", so it offers only a declaration a reference can still name (a hidden
        /// GLOBAL file or report is no answer) and carries what the wording needs (a refused constant); whether a declaration of
        /// ANOTHER class hides a tier is the one resolver's question, never this list's (kb/Work PB5975). (A named CAPACITY register is
        /// not in it: the register has ONE lookup, <c>ReferenceResolver.CapacityRegisterFor</c>, which judges the written reference,
        /// <c>CapacityRegisterReferenceDriftTests</c>.)
        /// <paramref name="dataName"/> is whether the ranked data-name lookup left a data item standing: it answers for
        /// itself, so the constant and the file are not offered beside it (an index-name is, because
        /// <c>SymbolTable.IndexCandidates</c> has already weighed it against the data-name). The inherited
        /// GLOBAL constant is still in the table until the unit's data division has bound (<c>InheritStep.DropShadowed</c>), so
        /// the data division's own clause operands meet it here, where a contained program's own level-88 of the spelling
        /// must outrank it.</summary>
        private IEnumerable<DeclarationFact> NonConditionDeclarationsOf(
            string name, IReadOnlyList<string> qualifiers, Scope scope, bool dataName)
        {
            if (qualifiers.Count == 0 && !dataName && _table.IsConstantName(name))
                yield return new(DeclarationKind.ConstantName, DeclarationRefused: _table.IsRefusedConstantName(name),
                    Depth: _table.InheritedConstantDepthOf(name));
            // An index-name that the written qualifiers reach NONE of is still the declaration a refusal words ("declared, but not
            // under the given qualifiers"), but the reference does not name it: it ranks behind every other class (int.MaxValue),
            // so a qualified `RELF OF X` that only a container's GLOBAL level-88 satisfies is not hidden by an own `INDEXED BY RELF`
            // (§2023:8.4.6.2.1 2): qualification is applied over the whole set before rule 3 ranks it; kb/Work PB4432 review R2-1).
            if (_table.IndexCandidates(name, qualifiers, scope) is { } indexes)
                yield return new(DeclarationKind.IndexName,
                    Depth: indexes.Count > 0 ? indexes.NearestDepth(i => _data.State.Forest.DeclaringDepth(i.Table)) : int.MaxValue);
            if (!dataName && qualifiers.Count == 0 && _table.FileNamed(Key(name), NameReach.Visible) is { } file)
                yield return new(DeclarationKind.FileName, Depth: _data.State.Inherited.FileDepth(file));
            // A report-name: this element's own RD, or a containing element's GLOBAL one a nearer declaration does not hide
            // (§1985:X.1.3.8.2 GR3a; kb/Work PB5973). It takes no qualifier.
            if (!dataName && qualifiers.Count == 0 && _table.ReportNameDepth(Key(name)) is { } reportDepth)
                yield return new(DeclarationKind.ReportName, Depth: reportDepth);
            // A cd-name is this element's own (2002 8.4.5), so it ranks at depth 0 (kb/Work PB4713).
            if (!dataName && qualifiers.Count == 0 && _table._cds.ContainsKey(Key(name)))
                yield return new(DeclarationKind.CdName);
        }

        /// <summary>Does the data description entry <paramref name="entry"/> declare <paramref name="word"/> (its own data-name, never a
        /// subordinate's)? Asked of the entry whose description is being bound (<c>DataBinder.IsBeingDescribed</c>, §2023:13.10.3 SR4).</summary>
        public bool EntryDeclares(Core.DataDescriptionEntryContext entry, string word) =>
            entry.dataName()?.GetText() is { } own && Key(own) == Key(word);

        /// <summary>Whether a SELECT of this element already names <paramref name="name"/> (§2023:12.4.5.2 SR2: one SELECT per
        /// file-name).</summary>
        public bool IsSelected(string name) => _table._selectedFiles.ContainsKey(Key(name));

        /// <summary>A container's GLOBAL file of the spelling, with its containment depth; hidden or not, since the question
        /// is how near the GLOBAL declaration is (<c>FileControlBinder.ScreenEnclosingFileOperand</c>).</summary>
        public DeclarationFact? InheritedFile(string name) =>
            _table._inheritedFiles.TryGetValue(Key(name), out var file)
                ? new DeclarationFact(DeclarationKind.FileName, Depth: _data.State.Inherited.FileDepth(file), Spelling: file.CobolName)
                : null;

        /// <summary>⛔ ISO §2023:8.4.6.2.1 3) a) SEEN FROM A CONDITION-ONLY POSITION, FOR THE NAME CLASSES THAT ARE NOT DATA ITEMS
        /// (kb/Work PB4432 review R1-1, the class twin of <see cref="SymbolTable.ConditionNameHiddenByNearerDataItem"/>): the written
        /// qualifiers reach a level-88 of the spelling, but a constant-name, index-name, file-name, report-name or cd-name of it
        /// declared in a NEARER source element is the referenced item (<see cref="SymbolTable.NearestInNameSet"/>, kb/Work PB5975).
        /// The declaration that hides it, or null. Pure: it reports nothing.</summary>
        public DeclarationFact? ConditionNameHiddenByNearerNonDataName(
            string name, IReadOnlyList<string> qualifiers, Scope scope)
        {
            var tier = _table.ConditionTier(name, qualifiers, scope);
            if (tier.Count == 0) return null;
            int depth = _data.State.Forest.DeclaringDepth(tier[0].Parent);
            return depth > 0 && _table.NearestInNameSet(name, qualifiers, NameSetClass.ConstantName | NameSetClass.IndexName
                       | NameSetClass.FileName | NameSetClass.ReportName | NameSetClass.CdName) is { } hider && hider.Depth < depth
                ? hider
                : null;
        }

        /// <summary>⛔ §1985:X.1.3.8.2 GR3a FOR A REPORT-NAME (kb/Work PB5976): the declaration that hides a containing program's GLOBAL
        /// report-name <paramref name="name"/> here, or null when no visible report of the spelling is hidden. "If the name is declared
        /// in program B, the object in program B is the referenced object", so `INITIATE R1` in a program that declares its own `01 R1`
        /// names that data item, and the refusal that it names no report says so (<see cref="DeclarationFact.Placed"/>) rather than
        /// reading as if the container's RD did not exist.</summary>
        public DeclarationFact? ReportNameHiddenBy(string name) =>
            _table.HiddenReportDepth(name) is { } reportDepth
            && _table.NearestInNameSet(name, [], NameSetClass.All & ~NameSetClass.ReportName) is { } hider && hider.Depth < reportDepth
                ? hider
                : null;

        /// <summary>⛔ §1985:X.1.3.8.2 GR3a FOR A FILE-NAME (kb/Work PB5980), the file twin of <see cref="ReportNameHiddenBy"/>: the declaration
        /// that hides a containing program's GLOBAL file-name <paramref name="name"/> here (<see cref="InheritedFile"/> is the GLOBAL file,
        /// hidden or not), or null when the spelling is no inherited file or nothing nearer declares it. The OPEN, CLOSE, READ, WRITE,
        /// REWRITE, DELETE and START of a name this way hidden name the data item, and their refusal says so.</summary>
        public DeclarationFact? FileNameHiddenBy(string name) =>
            InheritedFile(name) is { } global
            && _table.NearestInNameSet(name, [], NameSetClass.All & ~NameSetClass.FileName) is { } hider && hider.Depth < global.Depth
                ? hider
                : null;

        /// <summary>⛔ §1985:X.1.3.8.2 GR3a / ISO §2023:8.4.6.2.1 3) FOR AN INHERITED NAME A NEARER DECLARATION HIDES, of any class:
        /// whether this unit's set of names (its own and every container's global ones, <see cref="SymbolTable.NearestInNameSet"/>)
        /// declares <paramref name="name"/>, as anything, in a source element NEARER than <paramref name="depth"/> (the containment
        /// distance of the element that declared the inherited name; 1 = the direct container). "b) 1. The item in source element A if
        /// the name is declared in source element A. 2. The item in the containing source element if the name is not declared in source
        /// element A and is declared in the source element containing source element A." Asked unqualified, by the inherited constants
        /// and symbolic-characters, file-names and report-names (<c>InheritStep.DropShadowed</c>) and a container's sum counter
        /// (<c>CounterResolver</c>). A METHOD's own names are the one tier nearer still, asked at reference time
        /// (<see cref="OoDataBinder.ActiveMethodDeclares"/>, kb/Work PB3165). <c>GlobalNameClassDriftTests</c> holds the resolver's
        /// arms to every inherited name index, so a name index added there is asked here too.</summary>
        public bool DeclaredNearerThan(string name, int depth) =>
            _table.DeclaredNearerInNameSet(name, [], depth, NameSetClass.All);

        // ── The REPORT SECTION's declarations (docs/rearchitecture/DESIGN-listing.md section 6.7.1, "Step 6b, slice 5") ──────────

        /// <summary>The report entries a written reference, held as text by the data division's lookups, reaches (the table's one walk,
        /// <c>EntriesNamed</c>: every report this element can name, the written qualifiers through the §2023:8.4.2.2.3 SR4 walk, the
        /// nearest declaring source element, §2023:8.4.6.2.1 rule 3), each as a fact: <see cref="DeclarationKind.ReportSumCounter"/> or
        /// <see cref="DeclarationKind.ReportEntry"/>, its depth, its data-name and its report-name. Read by the report-item refusal
        /// (<see cref="ReportSectionBinder.ReportEntryReferenceRefused"/>, §2023:13.8.6.2.3), the ambiguity's wording
        /// (<see cref="ReportSectionBinder.ReportEntriesInTierOf"/>) and APPLY COMMIT's operand (kb/Work PB4196).</summary>
        public List<DeclarationFact> ReportEntriesNamed(string name, IReadOnlyList<string> qualifiers) =>
            [.. _table.EntriesNamed(name, qualifiers).Select(h => new DeclarationFact(
                h.Entry.IsSumCounter ? DeclarationKind.ReportSumCounter : DeclarationKind.ReportEntry,
                Depth: _data.State.Reports.Depth(h.Report), Spelling: h.Entry.Name, Report: h.Report.Name))];

        /// <summary>True when <paramref name="name"/> is defined in the report section AND NOWHERE ELSE (<see cref="SymbolTable.ReportSectionOnlyNames(ReportModel)"/>):
        /// the test §2023:13.18.16.3 SR2 needs of a CONTROL operand and §2023:13.15.3 SR16 of a PRESENT WHEN condition.</summary>
        public bool IsReportSectionOnlyName(string name)
        {
            foreach (var r in _data.State.Reports.Declared)
                foreach (var n in _table.ReportSectionOnlyNames(r))
                    if (CobolNames.Same(n, name)) return true;
            return false;
        }

        /// <summary>Is <paramref name="word"/> one of the identifiers ISO §2023:8.4.2.3.3 SR8 bars as a report section subscript: the
        /// LINE-COUNTER or PAGE-COUNTER register, or a sum counter: the name of a report section entry (and of no ordinary-storage
        /// item, §2023:8.4.2.1) that contains a SUM clause.</summary>
        public bool IsReportSubscriptCounter(string word) =>
            CobolNames.Same(word, "LINE-COUNTER")
            || CobolNames.Same(word, "PAGE-COUNTER")
            || (!_table.DeclaresOwnDataName(word) && _data.State.Reports.Declared.Any(r => r.NamedEntries.Any(e =>
                e.IsSumCounter && CobolNames.Same(e.Name, word))));

        /// <summary>Whether <paramref name="word"/> is the name of a sum counter of <paramref name="report"/>: §2023:13.18.53.3 SR4's
        /// other admitted report-section item of a SOURCE clause ("a sum counter defined in the current report").</summary>
        public bool IsSumCounterOf(ReportModel report, string word) =>
            report.SumFamilies.Any(f => f.Name is { } sn && CobolNames.Same(sn, word));

        /// <summary>Is a SUM addend written <paramref name="name"/> OF <paramref name="qualifiers"/> SR4's <c>data-name-1</c> (a report
        /// section item) rather than SR5's identifier-1 (ISO §2023:13.18.54.3)? A name declared in the report section AND NOWHERE ELSE
        /// is (<see cref="IsReportSectionOnlyName"/>: a name ALSO declared in ordinary storage resolves THERE, §2023:8.4.2.1); so is
        /// one a REPORT-name qualifier points at an entry of (SR4 g): the report-name says which report description's entry is meant).</summary>
        public bool IsRolledAddendName(string name, IReadOnlyList<string> qualifiers) =>
            IsReportSectionOnlyName(name)
            || (qualifiers.Any(q => _data.State.Reports.Declared.Any(r => CobolNames.Same(r.Name, q)))
                && _data.State.Reports.Declared.Any(r => r.EntryFamilies.Any(f => CobolNames.Same(f.Name, name))));

        /// <summary>⛔ THE FD'S REPORT CLAUSE AGAINST AN RD (kb/Work PB1285): whether <paramref name="clause"/>, one report-name-1 of an
        /// FD's REPORT clause, names <paramref name="report"/>, a report description entry of this element. A declaration agreement
        /// (§2023:13.14.3 SR1: "one and only one REPORT clause specifying report-name-1"; §2023:13.18.46.4 GR1), asked of every RD the
        /// clause may describe, so two RDs a refused duplicate report-name declares both keep their file.</summary>
        public bool NamesReport(ReportClauseName clause, ReportModel report) => CobolNames.Same(clause.Name, report.Name);
    }
}
