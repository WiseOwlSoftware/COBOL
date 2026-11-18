// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Binding;

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
        /// (<see cref="ResolveClauseOperand"/>) and an OCCURS DEPENDING ON object. Each used to screen its own subset, so a
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
        /// rank (APPLY COMMIT's GLOBAL file, <see cref="ScreenEnclosingFileOperand"/>) compares like with like.</para></summary>
        public DeclarationFact? OtherThanData(string name, IReadOnlyList<string> qualifiers, Scope scope)
        {
            if (_data.State.Forest.ScreenNames.Contains(name)) return new(DeclarationKind.ScreenEntry);
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
            if (qualifiers.Count == 0 && _data.TryFindTypeDecl(name, scope, out _)) return new(DeclarationKind.TypeName);
            // §2023:13.18.58.4 GR1: "Subordinate data description entries, condition-name entries, and RENAMES clauses are part of
            // the type declaration of that type" — a data-name or a condition-name such an entry declares.
            foreach (var declaration in VisibleTypeDeclarations(scope))
            {
                if (DataItem.DescendantsOf(declaration).Any(i => CobolNames.Same(i.CobolName, name)))
                    return new(DeclarationKind.TypeDeclarationMember, declaration.CobolName);
                if (DataItem.DescendantsOf(declaration).Prepend(declaration)
                        .Any(i => i.Own88s.Exists(c => CobolNames.Same(c.Name, name))))
                    return new(DeclarationKind.TypeDeclarationCondition, declaration.CobolName);
            }
            return null;
        }

        /// <summary>⛔ THE ONE LIST OF THE NAME CLASSES BESIDE A CONDITION-NAME, each with the depth of the element that declares
        /// it (kb/Work PB4432): the constant-name, the index-name and the file-name of the spelling (this element's own SELECT
        /// or a containing element's GLOBAL file, §2023:13.18.27.4 GR1) and this element's cd-name (never inherited, so depth 0; kb/Work
        /// PB4713), in the order a tie at one depth keeps.
        /// <see cref="DeclarationFactOf"/> ranks them with the condition-name for a reference that identifies no data
        /// item, and <see cref="NonConditionDeclarationNearerThan"/> asks the same list the other way round for the
        /// condition-name lookup, so a class added here ranks in both. (A named CAPACITY register is not in it: the register
        /// has ONE lookup, <c>ReferenceResolver.CapacityRegisterFor</c>, which judges the written reference
        /// (<c>CapacityRegisterReferenceDriftTests</c>), so the condition-name lookup asks that one,
        /// <c>ConditionBinder.ConditionOf</c>.)
        /// <paramref name="dataName"/> is whether the ranked data-name lookup left a data item standing: it answers for
        /// itself, so the constant and the file are not offered beside it (an index-name is, because
        /// <see cref="SymbolTable.IndexCandidates"/> has already weighed it against the data-name). The inherited
        /// GLOBAL constant is still in the table until the unit's data division has bound (<c>InheritStep.DropShadowed</c>), so
        /// the data division's own clause operands meet it here, where a contained program's own level-88 of the spelling
        /// must outrank it.</summary>
        private IEnumerable<DeclarationFact> NonConditionDeclarationsOf(
            string name, IReadOnlyList<string> qualifiers, Scope scope, bool dataName)
        {
            if (qualifiers.Count == 0 && !dataName && _data.Constants.IsConstantName(name))
                yield return new(DeclarationKind.ConstantName, DeclarationRefused: _data.Constants.IsRefusedConstantName(name),
                    Depth: _data.Constants.InheritedDepthOf(name));
            // An index-name that the written qualifiers reach NONE of is still the declaration a refusal words ("declared, but not
            // under the given qualifiers"), but the reference does not name it: it ranks behind every other class (int.MaxValue),
            // so a qualified `RELF OF X` that only a container's GLOBAL level-88 satisfies is not hidden by an own `INDEXED BY RELF`
            // (§2023:8.4.6.2.1 2): qualification is applied over the whole set before rule 3 ranks it; kb/Work PB4432 review R2-1).
            if (_table.IndexCandidates(name, qualifiers, scope) is { } indexes)
                yield return new(DeclarationKind.IndexName,
                    Depth: indexes.Count > 0 ? indexes.NearestDepth(i => _data.State.Forest.DeclaringDepth(i.Table)) : int.MaxValue);
            if (!dataName && qualifiers.Count == 0 && _table.FileNamed(Key(name), NameReach.Visible) is { } file)
                yield return new(DeclarationKind.FileName, Depth: _data.State.Inherited.FileDepth(file));
            // A cd-name is this element's own (2002 8.4.5), so it ranks at depth 0 (kb/Work PB4713).
            if (!dataName && qualifiers.Count == 0 && _table._cds.ContainsKey(Key(name)))
                yield return new(DeclarationKind.CdName);
        }

        /// <summary>⛔ ISO §2023:8.4.6.2.1 3) a) FOR A CONDITION-NAME REFERENCE AGAINST THE OTHER NAME CLASSES (kb/Work PB4432, the
        /// reverse arm of <see cref="DeclarationFactOf"/>'s ranking): the nearest constant-name, index-name or file-name of
        /// the spelling declared in a source element NEARER than <paramref name="depth"/>, the depth of the level-88 tier the
        /// reference reaches, or null when none is. "If the name is declared in source element B, the item in source element
        /// B is the referenced item", so a contained program's own `INDEXED BY RELF`, `RELF CONSTANT` or SELECT RELF hides its
        /// container's GLOBAL level-88 RELF exactly as its own `01 RELF` does. A tie is not "nearer": at one depth the
        /// condition-name keeps the reference, as it always has. Free for a unit that inherits nothing (every level-88 is
        /// then depth 0).</summary>
        private DeclarationFact? NonConditionDeclarationNearerThan(
            string name, IReadOnlyList<string> qualifiers, Scope scope, int depth)
        {
            if (depth == 0 || !_data.State.Forest.InheritsGlobalData) return null;
            DeclarationFact? nearest = null;
            foreach (var other in NonConditionDeclarationsOf(name, qualifiers, scope, dataName: false))
                if (other.Depth < depth && (nearest is not { } best || other.Depth < best.Depth)) nearest = other;
            return nearest;
        }

        /// <summary>Whether <see cref="NonConditionDeclarationNearerThan"/> finds a declaration — the question
        /// <c>SymbolTable.NearestDeclaringConditions</c> asks, which has no use for the declaration itself.</summary>
        public bool NonConditionDeclarationHides(string name, IReadOnlyList<string> qualifiers, Scope scope, int depth) =>
            NonConditionDeclarationNearerThan(name, qualifiers, scope, depth) is not null;

        /// <summary>⛔ ISO §2023:8.4.6.2.1 3) a) FOR A DATA-NAME AGAINST A NEARER FILE-NAME (kb/Work PB5786; §1985:X.1.3.8.2 GR3a): a
        /// reference is "to the object which that program describes" even when the name there is "a different type of
        /// user-defined word" (§1985:X.1.3.8), so a contained program's own file-name FX is what `FX` names there, never its
        /// container's GLOBAL data item FX. True when an unqualified spelling names a file-name (this element's own SELECT, or a
        /// containing element's GLOBAL file) declared in a source element NEARER than <paramref name="depth"/>; a file-name takes no
        /// qualifier, so a qualified reference is never hidden by one. The caller meets an unidentified reference, and
        /// <see cref="DeclarationFactOf"/> words it as "declared as a file-name". The reverse arm — an own data-name hiding
        /// a container's GLOBAL file — is <c>InheritedGlobals.DropShadowed</c>'s.</summary>
        /// <summary>Whether a SELECT of this element already names <paramref name="name"/> (§2023:12.4.5.2 SR2: one SELECT per
        /// file-name).</summary>
        public bool IsSelected(string name) => _table._selectedFiles.ContainsKey(Key(name));

        /// <summary>A container's GLOBAL file of the spelling, with its containment depth; hidden or not, since the question
        /// is how near the GLOBAL declaration is (<c>FileControlBinder.ScreenEnclosingFileOperand</c>).</summary>
        public DeclarationFact? InheritedFile(string name) =>
            _table._inheritedFiles.TryGetValue(Key(name), out var file)
                ? new DeclarationFact(DeclarationKind.FileName, Depth: _data.State.Inherited.FileDepth(file), Spelling: file.CobolName)
                : null;

        public bool NearerFileNameHides(string name, IReadOnlyList<string> qualifiers, int depth) =>
            depth != 0 && qualifiers.Count == 0 && _table.FileNamed(Key(name), NameReach.Visible) is { } file
            && _data.State.Inherited.FileDepth(file) < depth;

        /// <summary>⛔ ISO §2023:8.4.6.2.1 3) a) SEEN FROM A CONDITION-ONLY POSITION, FOR THE NAME CLASSES THAT ARE NOT DATA ITEMS
        /// (kb/Work PB4432 review R1-1, the class twin of <see cref="ConditionNameHiddenByNearerDataItem"/>): the written
        /// qualifiers reach a level-88 of the spelling, but a constant-name, index-name or file-name of it declared in a
        /// NEARER source element is the referenced item. The declaration that hides it, or null. Pure: it reports nothing.</summary>
        public DeclarationFact? ConditionNameHiddenByNearerNonDataName(
            string name, IReadOnlyList<string> qualifiers, Scope scope)
        {
            var tier = _data.ConditionTier(name, qualifiers, scope);
            return tier.Count == 0 ? null : NonConditionDeclarationNearerThan(name, qualifiers, scope, _data.State.Forest.DeclaringDepth(tier[0].Parent));
        }

        /// <summary>The type declarations visible in <paramref name="scope"/>, in <see cref="TryFindTypeDecl"/>'s order: a
        /// method's own, this element's, a container's GLOBAL one.</summary>
        private IEnumerable<DataItem> VisibleTypeDeclarations(Scope scope)
        {
            IEnumerable<DataItem> visible = _data.State.Forest.TypeDecls.Values.Concat(_data.State.Inherited.TypeDecls.Values);
            return scope.Method is { } m ? m.TypeDecls.Values.Concat(visible) : visible;
        }

        /// <summary>⛔ ISO §2023:8.4.6.2.1 3) ACROSS NAME CLASSES for a word that stands for a literal: whether this unit's set
        /// of names (its own and every container's global ones) declares <paramref name="name"/> as a data-name, a
        /// condition-name or an index-name in a source element NEARER than <paramref name="depth"/> (the containment
        /// distance of the element that declared the constant or symbolic-character; 1 = the direct container). "b) 1.
        /// The item in source element A if the name is declared in source element A. 2. The item in the containing
        /// source element if the name is not declared in source element A and is declared in the source element
        /// containing source element A." A condition-name is placed by its conditional variable.
        /// <para>⛔ EVERY NAME INDEX A DATA DESCRIPTION ENTRY DECLARES INTO, not only <see cref="ForestSlice.ByName"/> (kb/Work PB3154
        /// review R2-1): a named OCCURS DYNAMIC CAPACITY register is a data-name (§2023:13.18.38.3 SR30) kept off
        /// <see cref="ForestSlice.ByName"/> in <see cref="ForestSlice.CapacityRegistersSoFar"/>, placed by its table (a METHOD's own register is never
        /// in that index: it lives in <see cref="OoMethodDataScope.CapacityRegisters"/>, the method tier
        /// <see cref="OoDataBinder.ActiveMethodDeclares"/> asks, kb/Work PB3165); a REPORT SECTION sum
        /// counter is a data-name too (§2023:13.18.54.4 GR5) kept off it in <see cref="ReportScope.SumCounters"/>, placed by its report
        /// (<see cref="ReportScope.Depth"/>: 0 for this element's own, n for a container's GLOBAL report, §2023:13.18.27.4 GR1), so
        /// a FARTHER container's sum counter never hides a NEARER container's constant (kb/Work PB3154 review R3-1).
        /// <c>GlobalNameClassDriftTests</c> holds this predicate to the inherited name-index list, so a name index
        /// added there is asked here too.</para></summary>
        public bool DeclaredNearerThan(string name, int depth)
        {
            if (_data.State.Forest.ByName.TryGetValue(name, out var items) && items.Any(i => _data.State.Forest.DeclaringDepth(i) < depth)) return true;
            if (_data.State.Forest.Conditions.TryGetValue(name, out var conds) && conds.Any(c => _data.State.Forest.DeclaringDepth(c.Parent) < depth)) return true;
            if (_data.State.Forest.CapacityRegistersSoFar.TryGetValue(name, out var tables) && tables.Any(t => _data.State.Forest.DeclaringDepth(t) < depth)) return true;
            if (_data.State.Reports.SumCounters.TryGetValue(name, out var counters) && counters.Any(c => _data.State.Reports.Depth(c.Report) < depth)) return true;
            if (_data.State.Reports.NearerEntryHides(name, [], depth)) return true;   // every other named report entry (kb/Work PB4196)
            if (!_data.State.Forest.IndexNames.Declares(name)) return false;
            _data.State.Forest.IndexNames.Candidates(name, static _ => true, out int indexDepth);   // int.MaxValue when none survives
            return indexDepth < depth;
        }
    }
}
