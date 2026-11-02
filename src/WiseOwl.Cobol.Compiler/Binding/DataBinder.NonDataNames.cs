// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Binding;

/// <summary>What a name written where a data item is required is declared as, when no data item answers it
/// (<see cref="DataBinder.NonDataDeclarationOf"/>).</summary>
internal enum NonDataDeclarationKind
{
    /// <summary>A SCREEN SECTION entry (the section is refused, COBOL1560).</summary>
    ScreenEntry,
    /// <summary>An alphabet-name of the SPECIAL-NAMES paragraph.</summary>
    AlphabetName,
    /// <summary>A constant-name (a CONSTANT entry, §13.10).</summary>
    ConstantName,
    /// <summary>An index-name (an INDEXED BY phrase, §13.18.38).</summary>
    IndexName,
    /// <summary>A level-88 condition-name.</summary>
    ConditionName,
    /// <summary>A type-name (a TYPEDEF entry, §13.18.58).</summary>
    TypeName,
    /// <summary>A data-name declared only by a subordinate entry of a type declaration (§13.18.58.4 GR1).</summary>
    TypeDeclarationMember,
    /// <summary>A condition-name declared only by a level-88 entry of a type declaration (§13.18.58.4 GR1, GR2).</summary>
    TypeDeclarationCondition,
    /// <summary>A file-name: a SELECT entry of this source element, or the GLOBAL file of a containing one (§13.18.27.4 GR1).</summary>
    FileName,
}

/// <summary>A declaration that gives a name although it declares no data item: its kind, for a type declaration's
/// member or condition-name the type-name whose declaration holds it, whether the
/// declaration itself was REFUSED and reported at its entry (a constant entry that yielded no value), so a reference to
/// it is that one fault's consequence and its reader reports nothing more, and the <paramref name="Depth"/> of the
/// source element that declares it (0 = this one, n = its n-th container, <see cref="DataBinder.DeclaringDepth"/>'s
/// measure), the tier ISO §8.4.6.2.1 3) ranks it in against the other declarations of the spelling (kb/Work PB4219).</summary>
internal readonly record struct NonDataDeclaration(
    NonDataDeclarationKind Kind, string? TypeName = null, bool DeclarationRefused = false, int Depth = 0)
{
    /// <summary>⛔ THE ONE WORDING of what <paramref name="name"/> IS (kb/Work PB1906), completing a sentence whose subject
    /// is the reference as <paramref name="written"/>: "'T' is declared as a type-name …". Every site that refuses a
    /// reference for naming no data item says this rather than "not defined", which would send the user hunting a
    /// declaration that is right there.</summary>
    public string DeclaredAs(string written, string name) => Kind switch
    {
        NonDataDeclarationKind.ScreenEntry => "is declared as a SCREEN SECTION entry, not as a data item",
        NonDataDeclarationKind.AlphabetName => "is declared as an alphabet-name in the SPECIAL-NAMES paragraph, not as a data item",
        NonDataDeclarationKind.ConstantName or NonDataDeclarationKind.IndexName or NonDataDeclarationKind.ConditionName
            or NonDataDeclarationKind.FileName => $"is declared as {ClassPhrase}, not as a data item",
        NonDataDeclarationKind.TypeName => "is declared as a type-name (a TYPEDEF entry), not as a data item: a type "
            + "declaration has no storage associated with it (ISO §13.18.58.4 GR2)",
        NonDataDeclarationKind.TypeDeclarationMember =>
            (CobolNames.Same(written, name) ? "is declared" : $"names '{name}', which is declared")
            + $" only by a subordinate entry of the type declaration '{TypeName}', not as a data item: such a data-name "
            + "may be referenced only as a subordinate item of a group defined using the type-name (ISO §13.18.58.4 "
            + "GR1), and no such group is visible here",
        NonDataDeclarationKind.TypeDeclarationCondition =>
            (CobolNames.Same(written, name) ? "is declared" : $"names '{name}', which is declared")
            + $" only by a level-88 entry of the type declaration '{TypeName}', not as a condition-name of a data item: a "
            + "type declaration has no storage associated with it (ISO §13.18.58.4 GR2), so the condition-name has no "
            + "conditional variable until a group is defined using the type-name (GR1), and no such group is visible here",
        _ => throw new System.Diagnostics.UnreachableException(Kind.ToString()),
    };

    /// <summary>What the three name classes that a position can meet BESIDE a level-88 are, as a noun phrase: the one
    /// wording of <see cref="DeclaredAs"/> ("is declared as …, not as a data item") and of the condition-only position's
    /// refusal of a condition-name a nearer declaration of the class hides (<c>ReferenceResolver.HiddenConditionText</c>,
    /// ISO §8.4.6.2.1 3) a)).</summary>
    public string ClassPhrase => Kind switch
    {
        NonDataDeclarationKind.ConstantName => "a constant-name (a CONSTANT entry, ISO §13.10)",
        NonDataDeclarationKind.IndexName => "an index-name (an INDEXED BY phrase, ISO §13.18.38)",
        NonDataDeclarationKind.ConditionName => "a condition-name (a level-88 entry)",
        NonDataDeclarationKind.FileName => "a file-name (a file control entry, ISO §12.4.5)",
        _ => throw new System.Diagnostics.UnreachableException(Kind.ToString()),
    };
}

public sealed partial class DataBinder
{
    /// <summary>⛔ THE ONE ANSWER TO "WHAT IS THIS NAME DECLARED AS, IF NOT A DATA ITEM?" (kb/Work PB1906), asked by every
    /// site that refuses a reference for identifying no data item (ISO §8.4.2.1: "a statement shall contain a reference
    /// that uniquely identifies that resource"): the procedure division's unidentified reference
    /// (<see cref="ReferenceResolver.ReportUnidentified"/>), a file clause's data-name operand
    /// (<see cref="ResolveClauseOperand"/>) and an OCCURS DEPENDING ON object. Each used to screen its own subset, so a
    /// type-name was "not defined" at all three and an alphabet-, constant-, index- or condition-name at the two clause
    /// sites. Null when the name is declared as nothing but data — or as nothing at all — so the caller's own
    /// uniqueness verdict stands. A data-name of the same spelling the ranked lookup leaves standing (a condition-name
    /// declared NEARER hides it, kb/Work PB4219) wins over a constant-name, a condition-name and the type declarations (§8.3.2.2 exception 3 lets a constant-name and a data-name share one word; the others are
    /// a §8.3.2.2 conflict reported at the declaration). A REPORT SECTION entry is
    /// <see cref="ReportEntryReferenceRefused"/>'s: its refusal is a section rule (§13.8.6.2.3), not this one.
    /// <para>⛔ A CONSTANT-NAME, AN INDEX-NAME, A FILE-NAME AND A CONDITION-NAME ARE RANKED BY THE ELEMENT THAT DECLARES
    /// THEM (kb/Work PB4219, PB4432): ISO
    /// §8.4.6.2.1 3) a) makes the declaration of the source element B the referenced item, and b) the nearest
    /// containing one otherwise, so a contained program's own level-88 RELF is what `RELF` names there, not its
    /// container's GLOBAL file, constant-name or index-name RELF; each used to be asked first whatever the nesting. Each answer carries its
    /// <see cref="NonDataDeclaration.Depth"/> so a caller that weighs it against a declaration this query does not
    /// rank (APPLY COMMIT's GLOBAL file, <see cref="ScreenEnclosingFileOperand"/>) compares like with like.</para></summary>
    internal NonDataDeclaration? NonDataDeclarationOf(string name, IReadOnlyList<string> qualifiers, Model.Scope scope)
    {
        if (ScreenNames.Contains(name)) return new(NonDataDeclarationKind.ScreenEntry);
        if (IsAlphabetName(name)) return new(NonDataDeclarationKind.AlphabetName);
        // An unqualified reference's data-name answer is the RANKED one (§8.4.6.2.1 3)): a container's GLOBAL data item that
        // a nearer condition-name hides is no answer here, so the condition-name below is (kb/Work PB4219). A qualified
        // one keeps the raw answer, which the callers word as "declared, but not under the given qualifiers".
        bool dataName = qualifiers.Count == 0
            ? Symbols.TryResolveUnqualified(name, scope, out _)
            : Symbols.TryResolve(name, scope, out _);
        // ⛔ THE NON-DATA DECLARATIONS OF THE SPELLING ARE RANKED TOGETHER, by the depth of the element that declares each
        // (§8.4.6.2.1 3), kb/Work PB4219, PB4432): the classes NonConditionDeclarationsOf offers (constant-name, index-name,
        // file-name; the CAPACITY register has its own one lookup) and a condition-name. The nearest wins; at EQUAL depth one element declares both, a
        // §8.3.2.2 conflict reported at the declaration, and the first offered keeps the answer as it always has.
        NonDataDeclaration? nearest = null;
        void Offer(NonDataDeclaration candidate)
        {
            if (nearest is not { } best || candidate.Depth < best.Depth) nearest = candidate;
        }
        foreach (var other in NonConditionDeclarationsOf(name, qualifiers, scope, dataName)) Offer(other);
        // A data-name the ranked lookup leaves standing answers for itself; the condition-name never takes it.
        if (!dataName && Symbols.TryResolveCondition(name, scope, out var conditions))
            Offer(new(NonDataDeclarationKind.ConditionName, Depth: conditions.Min(c => DeclaringDepth(c.Parent))));
        if (nearest is { } answer) return answer;
        if (dataName) return null;
        if (qualifiers.Count == 0 && TryFindTypeDecl(name, scope, out _)) return new(NonDataDeclarationKind.TypeName);
        // §13.18.58.4 GR1: "Subordinate data description entries, condition-name entries, and RENAMES clauses are part of
        // the type declaration of that type" — a data-name or a condition-name such an entry declares.
        foreach (var declaration in VisibleTypeDeclarations(scope))
        {
            if (DataItem.DescendantsOf(declaration).Any(i => CobolNames.Same(i.CobolName, name)))
                return new(NonDataDeclarationKind.TypeDeclarationMember, declaration.CobolName);
            if (DataItem.DescendantsOf(declaration).Prepend(declaration)
                    .Any(i => i.Own88s.Exists(c => CobolNames.Same(c.Name, name))))
                return new(NonDataDeclarationKind.TypeDeclarationCondition, declaration.CobolName);
        }
        return null;
    }

    /// <summary>⛔ THE ONE LIST OF THE NAME CLASSES BESIDE A CONDITION-NAME, each with the depth of the element that declares
    /// it (kb/Work PB4432): the constant-name, the index-name and the file-name of the spelling (this element's own SELECT
    /// or a containing element's GLOBAL file, §13.18.27.4 GR1), in the order a tie at one depth keeps.
    /// <see cref="NonDataDeclarationOf"/> ranks them with the condition-name for a reference that identifies no data
    /// item, and <see cref="NonConditionDeclarationNearerThan"/> asks the same list the other way round for the
    /// condition-name lookup, so a class added here ranks in both. (A named CAPACITY register is not in it: the register
    /// has ONE lookup, <c>ReferenceResolver.CapacityRegisterFor</c>, which judges the written reference
    /// (<c>CapacityRegisterReferenceDriftTests</c>), so the condition-name lookup asks that one,
    /// <c>ConditionBinder.ConditionOf</c>.)
    /// <paramref name="dataName"/> is whether the ranked data-name lookup left a data item standing: it answers for
    /// itself, so the constant and the file are not offered beside it (an index-name is, because
    /// <see cref="Model.SymbolTable.IndexCandidates"/> has already weighed it against the data-name). The inherited
    /// GLOBAL constant is still in the table until the unit's data division has bound (<c>DropShadowedConstants</c>), so
    /// the data division's own clause operands meet it here, where a contained program's own level-88 of the spelling
    /// must outrank it.</summary>
    private IEnumerable<NonDataDeclaration> NonConditionDeclarationsOf(
        string name, IReadOnlyList<string> qualifiers, Model.Scope scope, bool dataName)
    {
        if (qualifiers.Count == 0 && !dataName && IsConstantName(name))
            yield return new(NonDataDeclarationKind.ConstantName, DeclarationRefused: IsRefusedConstantName(name),
                Depth: _inheritedConstants.GetValueOrDefault(name));
        // An index-name that the written qualifiers reach NONE of is still the declaration a refusal words ("declared, but not
        // under the given qualifiers"), but the reference does not name it: it ranks behind every other class (int.MaxValue),
        // so a qualified `RELF OF X` that only a container's GLOBAL level-88 satisfies is not hidden by an own `INDEXED BY RELF`
        // (§8.4.6.2.1 2): qualification is applied over the whole set before rule 3 ranks it; kb/Work PB4432 review R2-1).
        if (Symbols.IndexCandidates(name, qualifiers, scope) is { } indexes)
            yield return new(NonDataDeclarationKind.IndexName,
                Depth: indexes.Count > 0 ? indexes.NearestDepth(i => DeclaringDepth(i.Table)) : int.MaxValue);
        if (!dataName && qualifiers.Count == 0 && TryGetVisibleFile(name, out var file))
            yield return new(NonDataDeclarationKind.FileName, Depth: FileDepth(file));
    }

    /// <summary>⛔ ISO §8.4.6.2.1 3) a) FOR A CONDITION-NAME REFERENCE AGAINST THE OTHER NAME CLASSES (kb/Work PB4432, the
    /// reverse arm of <see cref="NonDataDeclarationOf"/>'s ranking): the nearest constant-name, index-name or file-name of
    /// the spelling declared in a source element NEARER than <paramref name="depth"/>, the depth of the level-88 tier the
    /// reference reaches, or null when none is. "If the name is declared in source element B, the item in source element
    /// B is the referenced item", so a contained program's own `INDEXED BY RELF`, `RELF CONSTANT` or SELECT RELF hides its
    /// container's GLOBAL level-88 RELF exactly as its own `01 RELF` does. A tie is not "nearer": at one depth the
    /// condition-name keeps the reference, as it always has. Free for a unit that inherits nothing (every level-88 is
    /// then depth 0).</summary>
    internal NonDataDeclaration? NonConditionDeclarationNearerThan(
        string name, IReadOnlyList<string> qualifiers, Model.Scope scope, int depth)
    {
        if (depth == 0 || !InheritsGlobalData) return null;
        NonDataDeclaration? nearest = null;
        foreach (var other in NonConditionDeclarationsOf(name, qualifiers, scope, dataName: false))
            if (other.Depth < depth && (nearest is not { } best || other.Depth < best.Depth)) nearest = other;
        return nearest;
    }

    /// <summary>Whether <see cref="NonConditionDeclarationNearerThan"/> finds a declaration — the question
    /// <c>SymbolTable.NearestDeclaringConditions</c> asks, which has no use for the declaration itself.</summary>
    internal bool NonConditionDeclarationHides(string name, IReadOnlyList<string> qualifiers, Model.Scope scope, int depth) =>
        NonConditionDeclarationNearerThan(name, qualifiers, scope, depth) is not null;

    /// <summary>⛔ ISO §8.4.6.2.1 3) a) SEEN FROM A CONDITION-ONLY POSITION, FOR THE NAME CLASSES THAT ARE NOT DATA ITEMS
    /// (kb/Work PB4432 review R1-1, the class twin of <see cref="ConditionNameHiddenByNearerDataItem"/>): the written
    /// qualifiers reach a level-88 of the spelling, but a constant-name, index-name or file-name of it declared in a
    /// NEARER source element is the referenced item. The declaration that hides it, or null. Pure: it reports nothing.</summary>
    internal NonDataDeclaration? ConditionNameHiddenByNearerNonDataName(
        string name, IReadOnlyList<string> qualifiers, Model.Scope scope)
    {
        var tier = ConditionTier(name, qualifiers, scope);
        return tier.Count == 0 ? null : NonConditionDeclarationNearerThan(name, qualifiers, scope, DeclaringDepth(tier[0].Parent));
    }

    /// <summary>Whether <paramref name="name"/> is a file-name visible in this source element: one of its own files or a
    /// GLOBAL file of a containing element (<see cref="InheritedGlobalFiles"/>, known before the unit binds).</summary>
    internal bool IsVisibleFileName(string name) => TryGetVisibleFile(name, out _);

    /// <summary>⛔ THE ONE LOOKUP OF A FILE-NAME VISIBLE IN THIS SOURCE ELEMENT, at any point of the bind: its own file,
    /// which hides a container's of the same spelling (§8.4.6.2.1 3) a)), else the nearest container's GLOBAL file
    /// (§13.18.27.4 GR1). <see cref="FilesByName"/> alone answers only after the unit binds, when
    /// <c>BinderDriver.BindUnitData</c> merges the inherited files in; the data division's own clauses resolve before
    /// that, and a container's GLOBAL record qualified by its file (§8.4.2.2.2 Format 1's file-report-qualifier) must
    /// resolve there too (kb/Work PB3557 review R2-1).</summary>
    internal bool TryGetVisibleFile(string name, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out FileModel? file) =>
        FilesByName.TryGetValue(name, out file) || InheritedGlobalFiles.TryGetValue(name, out file);

    /// <summary>The type declarations visible in <paramref name="scope"/>, in <see cref="TryFindTypeDecl"/>'s order: a
    /// method's own, this element's, a container's GLOBAL one.</summary>
    private IEnumerable<DataItem> VisibleTypeDeclarations(Model.Scope scope)
    {
        IEnumerable<DataItem> visible = TypeDecls.Values.Concat(_inheritedTypeDecls.Values);
        return scope.Method is { } m ? m.TypeDecls.Values.Concat(visible) : visible;
    }
}
