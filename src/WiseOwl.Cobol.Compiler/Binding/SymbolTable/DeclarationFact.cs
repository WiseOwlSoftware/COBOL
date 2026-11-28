// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Compiler.Binding.Model;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Compiler.Binding;

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
    /// <summary>A report group description entry's data-name (ISO §2023:13.15.3 SR7) that names no sum counter: a report item
    /// §2023:13.8.6.2.3 lets no other section of the data division and no procedure division statement reference.</summary>
    ReportEntry,
    /// <summary>The data-name of a SUM entry, which names its sum counter (ISO §2023:13.18.54.4 GR5).</summary>
    ReportSumCounter,
    /// <summary>A report-name: a report description entry of this source element, or the GLOBAL report of a containing one
    /// (§2023:13.18.27.4 GR1; kb/Work PB5973).</summary>
    ReportName,
    /// <summary>An ordinary data item: a data description entry's data-name. Never an answer of
    /// <see cref="SymbolTable.Declarations.OtherThanData"/>; it is the class of a nearer declaration that hides another one
    /// (§1985:X.1.3.8.2 GR3a; <c>SymbolTable.NearestInNameSet</c>, kb/Work PB5975).</summary>
    DataItem,
    /// <summary>The named CAPACITY register of an OCCURS DYNAMIC table (ISO §2023:13.18.38.3 SR30). Like <see cref="DataItem"/>, the
    /// class of a hiding declaration only: the register has its own one lookup (<c>ReferenceResolver.CapacityRegisterFor</c>).</summary>
    CapacityRegister,
}

/// <summary>A declaration that gives a name although it declares no data item: its kind, for a type declaration's
/// member or condition-name the type-name whose declaration holds it, whether the
/// declaration itself was REFUSED and reported at its entry (a constant entry that yielded no value), so a reference to
/// it is that one fault's consequence and its reader reports nothing more, and the <paramref name="Depth"/> of the
/// source element that declares it (0 = this one, n = its n-th container, <see cref="ForestSlice.DeclaringDepth"/>'s
/// measure), the tier ISO §2023:8.4.6.2.1 GR3 ranks it in against the other declarations of the spelling (kb/Work PB4219).
/// <paramref name="Spelling"/> is the declaration's own spelling where a message quotes it (an inherited GLOBAL file-name, a report
/// entry's data-name), and <paramref name="Report"/> the report-name of the report description a report entry belongs to.
/// <paramref name="Line"/> is the source line of the declaring entry where a message names it (0 when none is known): a
/// declaration of §1985:X.1.3.8.2 GR1's set that HIDES another of its spelling (<see cref="DeclarationKind.DataItem"/> among them)
/// is a fact too, so a refusal can say what the reference names instead (kb/Work PB5975, PB5976). <paramref name="ReportFormat"/>
/// is a report entry's report group description format, whose own data-name rule a COBOL-85 refusal cites (kb/Work PB6026).
/// The fact names no symbol: it answers a declaration question (<see cref="SymbolTable.Declarations"/>), never a reference
/// (docs/rearchitecture/DESIGN-listing.md section 6.7.1).</summary>
internal readonly record struct DeclarationFact(
    DeclarationKind Kind, string? TypeName = null, bool DeclarationRefused = false, int Depth = 0, string? Spelling = null,
    string? Report = null, int Line = 0, ReportEntryFormat? ReportFormat = null)
{
    /// <summary>⛔ THE ONE WORDING of what <paramref name="name"/> IS (kb/Work PB1906), completing a sentence whose subject
    /// is the reference as <paramref name="written"/>: "'T' is declared as a type-name …". Every site that refuses a
    /// reference for naming no data item says this rather than "not defined", which would send the user hunting a
    /// declaration that is right there. Its citations are <paramref name="edition"/>'s own (<see cref="ClassPhrase"/>).</summary>
    public string DeclaredAs(string written, string name, EditionInfo edition) => Kind switch
    {
        DeclarationKind.ScreenEntry => "is declared as a SCREEN SECTION entry, not as a data item",
        DeclarationKind.AlphabetName => "is declared as an alphabet-name in the SPECIAL-NAMES paragraph, not as a data item",
        DeclarationKind.ConstantName or DeclarationKind.IndexName or DeclarationKind.ConditionName
            or DeclarationKind.FileName or DeclarationKind.CdName or DeclarationKind.ReportName => $"is declared as {ClassPhrase(edition)}, not as a data item",
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

    /// <summary>What a declaration of a class of §1985:X.1.3.8.2 GR1's name set is, as a noun phrase: the one wording of
    /// <see cref="DeclaredAs"/> ("is declared as …, not as a data item"), of the condition-only position's refusal of a
    /// condition-name a nearer declaration of the class hides (<c>NameResolution.HiddenConditionText</c>, ISO §2023:8.4.6.2.1 GR3a)
    /// and of a report verb's refusal of a report-name one hides (<see cref="Placed"/>, kb/Work PB5976). ⛔ The clause it names is the one of the edition the program is compiled at (<see cref="EditionInfo.Cite"/>, kb/Work
    /// PB5978): COBOL-85 has its own index-name, file control, communication description and report description clauses, and no
    /// constant entry at all (ISO/IEC 1989:2002 introduced it).</summary>
    public string ClassPhrase(EditionInfo edition) => Kind switch
    {
        DeclarationKind.ScreenEntry => "a SCREEN SECTION entry",
        DeclarationKind.AlphabetName => "an alphabet-name (an ALPHABET clause of the SPECIAL-NAMES paragraph)",
        DeclarationKind.ConstantName => edition.Cite("a constant-name (a CONSTANT entry, which COBOL-85 does not define)",
            "a constant-name (a CONSTANT entry, ISO §2023:13.10)"),
        DeclarationKind.IndexName => "an index-name (an INDEXED BY phrase, " + edition.CiteRule("§1985:VI.5.8", "§2023:13.18.38", "§2002:13.16.36", "§2014:13.18.38") + ")",
        DeclarationKind.ConditionName => "a condition-name (a level-88 entry)",
        DeclarationKind.FileName => "a file-name (a file control entry, " + edition.CiteRule("§1985:VII.2.3", "§2023:12.4.5", "§2002:12.3.4", "§2014:12.4.4") + ")",
        DeclarationKind.CdName => "a cd-name (a communication description entry, "
            + edition.Cite("§1985:XIV.2.2", "ISO/IEC 1989:2002 A.1.2") + ")",
        DeclarationKind.ReportName => "a report-name (a report description entry, " + edition.CiteRule("§1985:XIII.3.5", "§2023:13.14", "§2002:13.12", "§2014:13.14") + ")",
        DeclarationKind.ReportEntry => "the data-name of a report group description entry ("
            + edition.CiteRule("§1985:XIII.3.9", "§2023:13.15", "§2002:13.13", "§2014:13.15") + ")",
        DeclarationKind.ReportSumCounter => "a sum counter (the data-name of a SUM entry, "
            + edition.CiteRule("§1985:XIII.3.19", "§2023:13.18.54.4 GR5", "§2002:13.16.52.3 GR5", "§2014:13.18.54.3 GR5") + ")",
        DeclarationKind.DataItem => "a data item",
        DeclarationKind.CapacityRegister =>
            "the CAPACITY register of an OCCURS DYNAMIC table (a CAPACITY IN phrase, ISO §2023:13.18.38.3 SR30)",
        _ => throw new System.Diagnostics.UnreachableException(Kind.ToString()),
    };

    /// <summary>The declaration as the subject of a hiding (kb/Work PB5976): <see cref="ClassPhrase"/>, the line of its entry when
    /// known, and the program that declares it, "this program" (depth 0) or a containing one.</summary>
    public string Placed(EditionInfo edition) =>
        ClassPhrase(edition) + (Line > 0 ? $" declared at line {Line}" : " declared")
        + (Depth == 0 ? " in this program" : " in a containing program");
}
