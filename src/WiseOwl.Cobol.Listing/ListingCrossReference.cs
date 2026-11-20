// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

namespace WiseOwl.Cobol.Listing;

/// <summary>The cross-reference (DESIGN-listing.md section 6.7): every declared name with its definition and every reference the
/// program wrote to it, each classified, and the PERFORM and CALL graphs the references make.</summary>
/// <remarks>A view of the symbol table's entries and their occurrences (DESIGN-listing.md section 6.7.1), built once by the compiler's
/// <c>CrossReferenceBuilder</c>; the copy members are a view of the inclusions.</remarks>
/// <param name="Symbols">Every symbol, sorted by name (the standard's case-insensitive fold of the name) and then by the line it is
/// defined on.</param>
/// <param name="Performs">The PERFORM graph: one edge per procedure a PERFORM statement names, from the procedure the statement is
/// written in, in listing order.</param>
/// <param name="Calls">The CALL graph: one edge per program, function or method prototype a statement activates by name, from the
/// source unit the statement is written in, in listing order.</param>
public sealed record ListingCrossReference(IReadOnlyList<ListingSymbol> Symbols, IReadOnlyList<ListingEdge> Performs,
    IReadOnlyList<ListingEdge> Calls);

/// <summary>One declared name and its uses.</summary>
/// <param name="Id">1-based, in the order of <see cref="ListingCrossReference.Symbols"/>.</param>
/// <param name="Name">The name as declared (<c>FILLER</c> is not listed; a copy member is its text-name).</param>
/// <param name="Kind">What the name names.</param>
/// <param name="Program">The <see cref="ListingProgram.Id"/> of the innermost program its definition stands in, or null.</param>
/// <param name="DataItem">The <see cref="ListingDataItem.Id"/> of its data map entry, when the data map holds it.</param>
/// <param name="References">Its definition (<see cref="ListingUse.Defined"/>, absent for a name the standard declares implicitly) and
/// every reference, by listing line and column.</param>
/// <param name="Unreferenced">The name is defined and the program never references it.</param>
public sealed record ListingSymbol(int Id, string Name, ListingSymbolKind Kind, int? Program, int? DataItem,
    IReadOnlyList<ListingReference> References, bool Unreferenced);

/// <summary>One use of a name.</summary>
/// <param name="Symbol">The <see cref="ListingSymbol.Id"/> it uses.</param>
/// <param name="Line">The listing line it is written on, or null when no listed line holds it.</param>
/// <param name="Column">The 1-based character position of its first character in that line's text, or null.</param>
/// <param name="Length">The characters it is written in: qualifiers, subscripts and reference modification included.</param>
/// <param name="Use">How it uses the name.</param>
/// <param name="Verb">The verb of the statement it is written in, upper case (the transfer of a procedure reference: PERFORM, GO,
/// ALTER, SORT, MERGE; the verb of a file or program reference), or null outside every statement.</param>
public sealed record ListingReference(int Symbol, int? Line, int? Column, int Length, ListingUse Use, string? Verb);

/// <summary>How a reference uses a name (DESIGN-listing.md section 6.7).</summary>
public enum ListingUse
{
    /// <summary>The declaration.</summary>
    Defined,
    /// <summary>A reference at no position its statement stores into.</summary>
    Read,
    /// <summary>A reference at a position its statement stores into, wholly or in part.</summary>
    Written,
    /// <summary>A reference at a position its statement reads and stores into (ADD TO, SUBTRACT FROM, INSPECT REPLACING, a STRING
    /// POINTER, ...).</summary>
    Modified,
}

/// <summary>What a cross-referenced name names.</summary>
public enum ListingSymbolKind
{
    /// <summary>A program-name or a user-function-name.</summary>
    Program,
    /// <summary>A data-name of a data description entry, a level-66 entry included.</summary>
    Data,
    /// <summary>A condition-name.</summary>
    Condition,
    /// <summary>An index-name.</summary>
    Index,
    /// <summary>A constant-name.</summary>
    Constant,
    /// <summary>A file-name.</summary>
    File,
    /// <summary>A report-name.</summary>
    Report,
    /// <summary>A cd-name.</summary>
    CommunicationDescription,
    /// <summary>A paragraph-name or a section-name.</summary>
    Procedure,
    /// <summary>A SPECIAL-NAMES switch-status condition-name.</summary>
    SwitchCondition,
    /// <summary>A class-name of a CLASS clause.</summary>
    ClassName,
    /// <summary>An alphabet-name of an alphanumeric ALPHABET clause.</summary>
    Alphabet,
    /// <summary>An alphabet-name of an ALPHABET … FOR NATIONAL clause.</summary>
    NationalAlphabet,
    /// <summary>A symbolic-character.</summary>
    SymbolicCharacter,
    /// <summary>An ordering-name.</summary>
    OrderingName,
    /// <summary>A dynamic-length-structure-name.</summary>
    DynamicLengthStructure,
    /// <summary>A locale-name.</summary>
    Locale,
    /// <summary>A mnemonic-name.</summary>
    Mnemonic,
    /// <summary>The data-name of a report group, as a reference that names a report group uses it.</summary>
    ReportGroup,
    /// <summary>A sum counter.</summary>
    SumCounter,
    /// <summary>A report VARYING counter.</summary>
    ReportVarying,
    /// <summary>A report entry named before the report section binds.</summary>
    ReportEntry,
    /// <summary>A SUM clause's data-name-1.</summary>
    ReportSumOperand,
    /// <summary>A program-prototype-name.</summary>
    ProgramPrototype,
    /// <summary>A function-prototype-name.</summary>
    FunctionPrototype,
    /// <summary>The DEBUG-ITEM special register or one of its members.</summary>
    DebugRegister,
    /// <summary>A type-name.</summary>
    TypeName,
    /// <summary>A named OCCURS DYNAMIC CAPACITY register.</summary>
    CapacityRegister,
    /// <summary>An object-class-name.</summary>
    ObjectClass,
    /// <summary>An interface-name.</summary>
    Interface,
    /// <summary>A parameter-name of a parameterized class or interface definition.</summary>
    ParameterName,
    /// <summary>A library text a COPY statement names: its definition is none, each COPY statement that names it a reference.</summary>
    CopyMember,
}

/// <summary>One edge of the PERFORM or CALL graph.</summary>
/// <param name="From">The <see cref="ListingSymbol.Id"/> of the procedure (PERFORM) or source unit (CALL) the statement is written in,
/// or null when none is defined before it.</param>
/// <param name="To">The <see cref="ListingSymbol.Id"/> the statement names.</param>
/// <param name="Line">The listing line of the reference.</param>
/// <param name="Verb">The verb of the statement, upper case.</param>
public sealed record ListingEdge(int? From, int To, int? Line, string Verb);

/// <summary>One name written on a source line (DESIGN-listing.md section 4.2): the join of the cross-reference onto the line, made
/// once, which the HTML listing links to the name's definition.</summary>
/// <param name="Column">The 1-based character position of its first character in the line's text.</param>
/// <param name="Length">The characters it is written in on this line.</param>
/// <param name="Symbol">The <see cref="ListingSymbol.Id"/> it uses.</param>
/// <param name="Use">How it uses the name.</param>
public sealed record ListingNameUse(int Column, int Length, int Symbol, ListingUse Use);
