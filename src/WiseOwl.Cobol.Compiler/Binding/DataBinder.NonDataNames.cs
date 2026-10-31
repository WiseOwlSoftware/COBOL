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
/// member or condition-name the type-name whose declaration holds it, and whether the
/// declaration itself was REFUSED and reported at its entry (a constant entry that yielded no value), so a reference to
/// it is that one fault's consequence and its reader reports nothing more.</summary>
internal readonly record struct NonDataDeclaration(
    NonDataDeclarationKind Kind, string? TypeName = null, bool DeclarationRefused = false)
{
    /// <summary>⛔ THE ONE WORDING of what <paramref name="name"/> IS (kb/Work PB1906), completing a sentence whose subject
    /// is the reference as <paramref name="written"/>: "'T' is declared as a type-name …". Every site that refuses a
    /// reference for naming no data item says this rather than "not defined", which would send the user hunting a
    /// declaration that is right there.</summary>
    public string DeclaredAs(string written, string name) => Kind switch
    {
        NonDataDeclarationKind.ScreenEntry => "is declared as a SCREEN SECTION entry, not as a data item",
        NonDataDeclarationKind.AlphabetName => "is declared as an alphabet-name in the SPECIAL-NAMES paragraph, not as a data item",
        NonDataDeclarationKind.ConstantName => "is declared as a constant-name (a CONSTANT entry, ISO §13.10), not as a data item",
        NonDataDeclarationKind.IndexName => "is declared as an index-name (an INDEXED BY phrase, ISO §13.18.38), not as a data item",
        NonDataDeclarationKind.ConditionName => "is declared as a condition-name (a level-88 entry), not as a data item",
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
        NonDataDeclarationKind.FileName => "is declared as a file-name (a file control entry, ISO §12.4.5), not as a data item",
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
    /// uniqueness verdict stands. A data-name of the same spelling wins over a constant-name, a condition-name and
    /// the type declarations (§8.3.2.2 exception 3 lets a constant-name and a data-name share one word; the others are
    /// a §8.3.2.2 conflict reported at the declaration). A REPORT SECTION entry is
    /// <see cref="ReportEntryReferenceRefused"/>'s: its refusal is a section rule (§13.8.6.2.3), not this one.</summary>
    internal NonDataDeclaration? NonDataDeclarationOf(string name, IReadOnlyList<string> qualifiers, Model.Scope scope)
    {
        if (ScreenNames.Contains(name)) return new(NonDataDeclarationKind.ScreenEntry);
        if (IsAlphabetName(name)) return new(NonDataDeclarationKind.AlphabetName);
        bool dataName = Symbols.TryResolve(name, scope, out _);
        if (qualifiers.Count == 0 && !dataName && IsConstantName(name))
            return new(NonDataDeclarationKind.ConstantName, DeclarationRefused: IsRefusedConstantName(name));
        if (Symbols.IndexCandidates(name, qualifiers, scope) is not null) return new(NonDataDeclarationKind.IndexName);
        if (dataName) return null;
        // A file-name (kb/Work PB3557): this element's own SELECT, or a containing element's GLOBAL file (§13.18.27.4 GR1).
        if (qualifiers.Count == 0 && IsVisibleFileName(name)) return new(NonDataDeclarationKind.FileName);
        if (Symbols.TryResolveCondition(name, scope, out _)) return new(NonDataDeclarationKind.ConditionName);
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

    /// <summary>Whether <paramref name="name"/> is a file-name visible in this source element: one of its own files or a
    /// GLOBAL file of a containing element (<see cref="InheritedGlobalFiles"/>, known before the unit binds).</summary>
    internal bool IsVisibleFileName(string name) => FilesByName.ContainsKey(name) || InheritedGlobalFiles.ContainsKey(name);

    /// <summary>The type declarations visible in <paramref name="scope"/>, in <see cref="TryFindTypeDecl"/>'s order: a
    /// method's own, this element's, a container's GLOBAL one.</summary>
    private IEnumerable<DataItem> VisibleTypeDeclarations(Model.Scope scope)
    {
        IEnumerable<DataItem> visible = TypeDecls.Values.Concat(_inheritedTypeDecls.Values);
        return scope.Method is { } m ? m.TypeDecls.Values.Concat(visible) : visible;
    }
}
