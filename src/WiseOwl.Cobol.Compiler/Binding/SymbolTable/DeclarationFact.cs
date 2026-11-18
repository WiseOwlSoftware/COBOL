// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Binding;

/// <summary>What a name written where a data item is required is declared as, when no data item answers it
/// (<see cref="SymbolTable.Declarations.OtherThanData"/>).</summary>
internal enum DeclarationKind
{
    /// <summary>A SCREEN SECTION entry (the section is refused, COBOL1560).</summary>
    ScreenEntry,
    /// <summary>An alphabet-name of the SPECIAL-NAMES paragraph.</summary>
    AlphabetName,
    /// <summary>A constant-name (a CONSTANT entry, §2023:13.10).</summary>
    ConstantName,
    /// <summary>An index-name (an INDEXED BY phrase, §2023:13.18.38).</summary>
    IndexName,
    /// <summary>A level-88 condition-name.</summary>
    ConditionName,
    /// <summary>A type-name (a TYPEDEF entry, §2023:13.18.58).</summary>
    TypeName,
    /// <summary>A data-name declared only by a subordinate entry of a type declaration (§2023:13.18.58.4 GR1).</summary>
    TypeDeclarationMember,
    /// <summary>A condition-name declared only by a level-88 entry of a type declaration (§2023:13.18.58.4 GR1, GR2).</summary>
    TypeDeclarationCondition,
    /// <summary>A file-name: a SELECT entry of this source element, or the GLOBAL file of a containing one (§2023:13.18.27.4 GR1).</summary>
    FileName,
    /// <summary>A cd-name: a communication description entry of this source element (ISO/IEC 1989:2002 A.1.2). It is this element's alone,
    /// since a cd-name "may be referenced only by statements and entries in the source element that contains the communication section
    /// in which it is declared" (2002 8.4.5), so it is never inherited.</summary>
    CdName,
}

/// <summary>A declaration that gives a name although it declares no data item: its kind, for a type declaration's
/// member or condition-name the type-name whose declaration holds it, whether the
/// declaration itself was REFUSED and reported at its entry (a constant entry that yielded no value), so a reference to
/// it is that one fault's consequence and its reader reports nothing more, and the <paramref name="Depth"/> of the
/// source element that declares it (0 = this one, n = its n-th container, <see cref="ForestSlice.DeclaringDepth"/>'s
/// measure), the tier ISO §2023:8.4.6.2.1 3) ranks it in against the other declarations of the spelling (kb/Work PB4219).
/// <paramref name="Spelling"/> is the declaration's own spelling where a message quotes it (an inherited GLOBAL file-name).
/// The fact names no symbol: it answers a declaration question (<see cref="SymbolTable.Declarations"/>), never a reference
/// (docs/rearchitecture/DESIGN-listing.md section 6.7.1).</summary>
internal readonly record struct DeclarationFact(
    DeclarationKind Kind, string? TypeName = null, bool DeclarationRefused = false, int Depth = 0, string? Spelling = null)
{
    /// <summary>⛔ THE ONE WORDING of what <paramref name="name"/> IS (kb/Work PB1906), completing a sentence whose subject
    /// is the reference as <paramref name="written"/>: "'T' is declared as a type-name …". Every site that refuses a
    /// reference for naming no data item says this rather than "not defined", which would send the user hunting a
    /// declaration that is right there.</summary>
    public string DeclaredAs(string written, string name) => Kind switch
    {
        DeclarationKind.ScreenEntry => "is declared as a SCREEN SECTION entry, not as a data item",
        DeclarationKind.AlphabetName => "is declared as an alphabet-name in the SPECIAL-NAMES paragraph, not as a data item",
        DeclarationKind.ConstantName or DeclarationKind.IndexName or DeclarationKind.ConditionName
            or DeclarationKind.FileName or DeclarationKind.CdName => $"is declared as {ClassPhrase}, not as a data item",
        DeclarationKind.TypeName => "is declared as a type-name (a TYPEDEF entry), not as a data item: a type "
            + "declaration has no storage associated with it (ISO §2023:13.18.58.4 GR2)",
        DeclarationKind.TypeDeclarationMember =>
            (CobolNames.Same(written, name) ? "is declared" : $"names '{name}', which is declared")
            + $" only by a subordinate entry of the type declaration '{TypeName}', not as a data item: such a data-name "
            + "may be referenced only as a subordinate item of a group defined using the type-name (ISO §2023:13.18.58.4 "
            + "GR1), and no such group is visible here",
        DeclarationKind.TypeDeclarationCondition =>
            (CobolNames.Same(written, name) ? "is declared" : $"names '{name}', which is declared")
            + $" only by a level-88 entry of the type declaration '{TypeName}', not as a condition-name of a data item: a "
            + "type declaration has no storage associated with it (ISO §2023:13.18.58.4 GR2), so the condition-name has no "
            + "conditional variable until a group is defined using the type-name (GR1), and no such group is visible here",
        _ => throw new System.Diagnostics.UnreachableException(Kind.ToString()),
    };

    /// <summary>What the three name classes that a position can meet BESIDE a level-88 are, as a noun phrase: the one
    /// wording of <see cref="DeclaredAs"/> ("is declared as …, not as a data item") and of the condition-only position's
    /// refusal of a condition-name a nearer declaration of the class hides (<c>NameResolution.HiddenConditionText</c>,
    /// ISO §2023:8.4.6.2.1 3) a)).</summary>
    public string ClassPhrase => Kind switch
    {
        DeclarationKind.ConstantName => "a constant-name (a CONSTANT entry, ISO §2023:13.10)",
        DeclarationKind.IndexName => "an index-name (an INDEXED BY phrase, ISO §2023:13.18.38)",
        DeclarationKind.ConditionName => "a condition-name (a level-88 entry)",
        DeclarationKind.FileName => "a file-name (a file control entry, ISO §2023:12.4.5)",
        DeclarationKind.CdName => "a cd-name (a communication description entry, ISO/IEC 1989:2002 A.1.2)",
        _ => throw new System.Diagnostics.UnreachableException(Kind.ToString()),
    };
}
