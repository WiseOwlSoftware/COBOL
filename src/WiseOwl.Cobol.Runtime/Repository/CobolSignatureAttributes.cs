// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.ComponentModel;

namespace WiseOwl.Cobol.Runtime.Repository;

// ⛔ THE SIGNATURE HALF OF THE REPOSITORY SCHEMA (ISO §8.13; docs/rearchitecture/DESIGN-external-repository.md §6.2;
// kb/Work PB2100). A module compiled with --repository-update on carries, beside every unit's [CobolUnit], the
// description of its formals and returning item (on the host entry's Call parameters and return value) and the rows of
// the record's own tables (type declarations, dynamic-length structures, referenced prototypes) on the host-entry class.
// Every attribute applied here obeys §6's rules: sealed, not inherited, the identity in ONE constructor, every other fact a
// named property of a primitive, string, one-dimensional array or int-backed enum type, none a System.Type, so a reader
// decodes them from metadata without loading the assembly; a named argument is present only when the renderer wrote it
// (§6.2 rule 2: nullability is presence), and a structured value is flattened into prefixed named arguments (rule 3). The
// writer is the compiler's RepositoryAttributeRenderer and the reader its RepositoryReader; the compiler's codec
// (RepositoryRecord) is what both transcribe. The two abstract classes are never applied: CobolDescriptionFieldsAttribute
// states the description fields once for the three sealed attributes that carry a description, CobolFormalFieldsAttribute
// a formal's for the two that carry a formal.

/// <summary>The fields of one data description entry (§6.1 <c>DescriptionRecord</c> and its <c>PicRecord</c>), stated
/// ONCE for every attribute that carries a description: <see cref="CobolDescriptionAttribute"/> (a formal's or the
/// returning item's tree), <see cref="CobolTypeDeclarationAttribute"/> and <see cref="CobolPrototypeDescriptionAttribute"/>.
/// A non-nullable field the renderer does not write reads as its default; a nullable field of the record is written exactly
/// when the record has a value (<see cref="Occurs"/>, <see cref="DynStructure"/>, <see cref="TypeDeclaration"/>,
/// <see cref="DigitPositions"/>, <see cref="CurrencyClass"/>, every nullable string); and a structured member exists exactly
/// when its PRESENCE MARKER is written: <see cref="Category"/> for the elementary entry's PICTURE / USAGE profile,
/// <see cref="OccursMax"/> for the OCCURS clause, <see cref="ClauseCharacterString"/> for the PICTURE clause identity,
/// <see cref="LocalePicture"/> for a format-2 picture, the five <c>EditRule…</c> arrays for the editing rules.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class CobolDescriptionFieldsAttribute(int ordinal, int parent) : Attribute
{
    /// <summary>The entry's preorder index within its tree (§6.2 "Anchoring").</summary>
    public int Ordinal { get; } = ordinal;

    /// <summary>The preorder index of the entry's parent, -1 at the root.</summary>
    public int Parent { get; } = parent;

    /// <summary>The level-number.</summary>
    public int Level { get; set; }
    /// <summary>The data-name as written (absent for FILLER).</summary>
    public string? Name { get; set; }
    /// <summary>The data-name a REDEFINES clause names.</summary>
    public string? RedefinesTarget { get; set; }
    /// <summary>The allocated occurrence count.</summary>
    public int Occurs { get; set; }
    /// <summary>The OCCURS clause's minimum (present with the structured clause).</summary>
    public int OccursMin { get; set; }
    /// <summary>The OCCURS clause's maximum (present exactly when the entry has the structured clause).</summary>
    public int OccursMax { get; set; }
    /// <summary>The DEPENDING ON object, a word of the record or <c>&lt;outside&gt;</c>.</summary>
    public string? OccursDepending { get; set; }
    /// <summary>A DYNAMIC-capacity table.</summary>
    public bool OccursDynamic { get; set; }
    /// <summary>A DYNAMIC table's CAPACITY data-name.</summary>
    public string? OccursCapacity { get; set; }
    /// <summary>A DYNAMIC table's initial capacity.</summary>
    public int OccursInitialCapacity { get; set; }
    /// <summary>A DYNAMIC table's expected maximum.</summary>
    public int OccursExpectedMax { get; set; }
    /// <summary>A DYNAMIC table's INITIALIZED phrase.</summary>
    public bool OccursInitialized { get; set; }
    /// <summary>JUSTIFIED.</summary>
    public bool Justified { get; set; }
    /// <summary>BLANK WHEN ZERO.</summary>
    public bool BlankWhenZero { get; set; }
    /// <summary>The SYNCHRONIZED clause's form.</summary>
    public RepositorySynchronized Synchronized { get; set; }
    /// <summary>ALIGNED.</summary>
    public bool Aligned { get; set; }
    /// <summary>ALIGNED placement as a level-1 item.</summary>
    public bool AlignedAsLevelOne { get; set; }
    /// <summary>ANY LENGTH.</summary>
    public bool AnyLength { get; set; }
    /// <summary>BASED.</summary>
    public bool Based { get; set; }
    /// <summary>DYNAMIC LENGTH.</summary>
    public bool DynamicLength { get; set; }
    /// <summary>The DYNAMIC LENGTH maximum size.</summary>
    public int DynMaxSize { get; set; }
    /// <summary>The dynamic-length structure, an index into the record's structures.</summary>
    public int DynStructure { get; set; }
    /// <summary>The carried type-name.</summary>
    public string? TypeName { get; set; }
    /// <summary>The carried STRONG phrase.</summary>
    public bool Strong { get; set; }
    /// <summary>The entry's own EXTERNAL clause.</summary>
    public bool External { get; set; }
    /// <summary>The entry is a type declaration.</summary>
    public bool IsTypedef { get; set; }
    /// <summary>The TYPEDEF clause's STRONG phrase.</summary>
    public bool TypedefStrong { get; set; }
    /// <summary>The type declaration is EXTERNAL.</summary>
    public bool IsExternalTypedef { get; set; }
    /// <summary>EXTERNAL carried from a type declaration.</summary>
    public bool ExternalFromType { get; set; }
    /// <summary>The type declaration the type-name names, an index into the record's type declarations.</summary>
    public int TypeDeclaration { get; set; }
    /// <summary>GROUP-USAGE.</summary>
    public RepositoryGroupUsage GroupUsage { get; set; }
    /// <summary>The category (present exactly when the entry is elementary).</summary>
    public RepositoryCategory Category { get; set; }
    /// <summary>The usage.</summary>
    public RepositoryUsage Usage { get; set; }
    /// <summary>The length in character positions.</summary>
    public int Length { get; set; }
    /// <summary>The digit count.</summary>
    public int Digits { get; set; }
    /// <summary>The scale.</summary>
    public int Scale { get; set; }
    /// <summary>Signed.</summary>
    public bool Signed { get; set; }
    /// <summary>The sign convention's name.</summary>
    public string? SignKind { get; set; }
    /// <summary>A recovery profile (an entry whose PICTURE was refused).</summary>
    public bool IsRecovery { get; set; }
    /// <summary>A restricted data-pointer's type-name as written.</summary>
    public string? RestrictedTypeName { get; set; }
    /// <summary>The type declaration it resolves to, an index into the record's type declarations.</summary>
    public int RestrictedTypeDeclaration { get; set; }
    /// <summary>A restricted program- or function-pointer's prototype-name as written.</summary>
    public string? RestrictedPrototypeName { get; set; }
    /// <summary>The prototype it names, an index into the record's referenced prototypes.</summary>
    public int RestrictedPrototype { get; set; }
    /// <summary>PACKED-DECIMAL WITH NO SIGN.</summary>
    public bool PackedNoSign { get; set; }
    /// <summary>A little-endian standard binary float.</summary>
    public bool FloatLittleEndian { get; set; }
    /// <summary>A floating-point numeric-edited picture.</summary>
    public bool IsFloatEdited { get; set; }
    /// <summary>The expanded edit mask.</summary>
    public string? EditMask { get; set; }
    /// <summary>The currency string the picture's currency symbol stands for.</summary>
    public string? CurrencyString { get; set; }
    /// <summary>The class of the currency literal.</summary>
    public RepositoryLiteralClass CurrencyClass { get; set; }
    /// <summary>Alphabetic.</summary>
    public bool IsAlphabetic { get; set; }
    /// <summary>The digit-position count as the analyzer wrote it.</summary>
    public int DigitPositions { get; set; }
    /// <summary>The PICTURE clause identity's character-string (present exactly when the entry has a clause).</summary>
    public string? ClauseCharacterString { get; set; }
    /// <summary>The PICTURE clause identity's currency string.</summary>
    public string? ClauseCurrencyString { get; set; }
    /// <summary>The PICTURE clause identity's DECIMAL-POINT IS COMMA state.</summary>
    public bool ClauseDecimalPointIsComma { get; set; }
    /// <summary>The PICTURE clause identity's EDITING text.</summary>
    public string? ClauseEditing { get; set; }
    /// <summary>The PICTURE EDITING rules' character-1, one per rule (present exactly when the entry has rules).</summary>
    public char[]? EditRuleChar1 { get; set; }
    /// <summary>The rules' negative text.</summary>
    public string[]? EditRuleNeg { get; set; }
    /// <summary>The rules' positive text.</summary>
    public string[]? EditRulePos { get; set; }
    /// <summary>The rules' simple-insertion flags.</summary>
    public bool[]? EditRuleSimpleInsertion { get; set; }
    /// <summary>The rules' floating flags.</summary>
    public bool[]? EditRuleFloating { get; set; }
    /// <summary>A format-2 picture's locale-name as written.</summary>
    public string? LocaleName { get; set; }
    /// <summary>The named locale's external identification (absent for the current locale).</summary>
    public string? LocaleExternal { get; set; }
    /// <summary>The external identification was a literal.</summary>
    public bool LocaleFromLiteral { get; set; }
    /// <summary>The SIZE phrase.</summary>
    public int LocaleSize { get; set; }
    /// <summary>The format-2 picture (present exactly when the entry has a locale edit).</summary>
    public string? LocalePicture { get; set; }
    /// <summary>The object-reference description's kind.</summary>
    public ObjectReferenceKind ObjectRefKind { get; set; }
    /// <summary>The written class or interface word.</summary>
    public string? ObjectRefName { get; set; }
    /// <summary>The externalized name the word resolved to in the publishing element.</summary>
    public string? ObjectRefIdentity { get; set; }
    /// <summary>FACTORY OF.</summary>
    public bool ObjectRefFactory { get; set; }
    /// <summary>ONLY.</summary>
    public bool ObjectRefOnly { get; set; }
    /// <summary>A formal parameter of a parameterized definition.</summary>
    public bool ObjectRefFormal { get; set; }
}

/// <summary>The fields of one formal parameter (ISO §8.13 "the manner of receiving parameters (by reference or by value) and
/// whether they are optional or not"), stated ONCE for the two attributes that carry a formal:
/// <see cref="CobolFormalAttribute"/> (a unit's own) and <see cref="CobolPrototypeFormalAttribute"/> (a referenced
/// prototype's). Never applied itself.</summary>
/// <param name="position">The formal's position in the USING phrase.</param>
/// <param name="name">The formal's data-name (diagnostics and the host surface; never compared).</param>
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class CobolFormalFieldsAttribute(int position, string name) : Attribute
{
    /// <summary>The formal's position.</summary>
    public int Position { get; } = position;
    /// <summary>The formal's data-name.</summary>
    public string Name { get; } = name;
    /// <summary>BY REFERENCE or BY VALUE.</summary>
    public PassingMode Mode { get; set; }
    /// <summary>OPTIONAL.</summary>
    public bool Optional { get; set; }
}

/// <summary>One formal parameter of the unit, on the host entry's <c>Call</c> parameter that carries it; its description tree
/// is the same parameter's <see cref="CobolDescriptionAttribute"/>s (§6.2 "Anchoring").</summary>
/// <param name="position">The formal's position in the USING phrase.</param>
/// <param name="name">The formal's data-name (diagnostics and the host surface; never compared).</param>
[EditorBrowsable(EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
public sealed class CobolFormalAttribute(int position, string name) : CobolFormalFieldsAttribute(position, name);

/// <summary>Marks the host entry's return value as the RETURNING item (ISO §8.13 "the description of the returning item");
/// its description tree is the return value's <see cref="CobolDescriptionAttribute"/>s.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.ReturnValue, Inherited = false, AllowMultiple = false)]
public sealed class CobolReturningAttribute : Attribute;

/// <summary>One entry of a formal's or the returning item's description tree, on the parameter or return value that
/// carries it (§6.2 "Anchoring").</summary>
/// <param name="ordinal">The entry's preorder index within its tree.</param>
/// <param name="parent">The preorder index of the entry's parent, -1 at the root.</param>
[EditorBrowsable(EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue, Inherited = false, AllowMultiple = true)]
public sealed class CobolDescriptionAttribute(int ordinal, int parent) : CobolDescriptionFieldsAttribute(ordinal, parent);

/// <summary>One entry of a type declaration the record's descriptions name (ISO §8.13 "type declarations required for the
/// description of parameters and returning items"), on the unit's host-entry class.</summary>
/// <param name="declaration">The row's index in the record's type declarations.</param>
/// <param name="ordinal">The entry's preorder index within the declaration's tree.</param>
/// <param name="parent">The preorder index of the entry's parent, -1 at the root.</param>
[EditorBrowsable(EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = true)]
public sealed class CobolTypeDeclarationAttribute(int declaration, int ordinal, int parent) : CobolDescriptionFieldsAttribute(ordinal, parent)
{
    /// <summary>The row's index in the record's type declarations.</summary>
    public int Declaration { get; } = declaration;
}

/// <summary>A dynamic-length structure the record's descriptions name, by definition (ISO §12.3.7.2; §6.4).</summary>
/// <param name="index">The row's index in the record's structures.</param>
/// <param name="name">The structure's name as the publisher wrote it.</param>
[EditorBrowsable(EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = true)]
public sealed class CobolStructureAttribute(int index, string name) : Attribute
{
    /// <summary>The row's index.</summary>
    public int Index { get; } = index;
    /// <summary>The structure's name.</summary>
    public string Name { get; } = name;
    /// <summary>The PREFIXED phrase's length field.</summary>
    public RepositoryPrefix Prefix { get; set; }
    /// <summary>DELIMITED.</summary>
    public bool Delimited { get; set; }
    /// <summary>A physical-structure-name.</summary>
    public string? PhysicalStructureName { get; set; }
}

/// <summary>A prototype a restricted program- or function-pointer of the record names (ISO §13.18.60.4 GR25 / GR26: the
/// pointer's type is that prototype's SIGNATURE; §6.4), its signature carried by the
/// <see cref="CobolPrototypeFormalAttribute"/>s and <see cref="CobolPrototypeDescriptionAttribute"/>s of the same index.</summary>
/// <param name="index">The row's index in the record's referenced prototypes.</param>
/// <param name="kind">A program or a function prototype.</param>
/// <param name="externalizedName">The prototype's externalized name.</param>
[EditorBrowsable(EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = true)]
public sealed class CobolPrototypeAttribute(int index, RepositoryUnitKind kind, string externalizedName) : Attribute
{
    /// <summary>The row's index.</summary>
    public int Index { get; } = index;
    /// <summary>A program or a function prototype.</summary>
    public RepositoryUnitKind Kind { get; } = kind;
    /// <summary>The prototype's externalized name.</summary>
    public string ExternalizedName { get; } = externalizedName;
    /// <summary>The prototype had a compile-time signature (false for ISO §12.3.8.4 GR10 c) / GR11 c)).</summary>
    public bool HasSignature { get; set; }
}

/// <summary>One formal of a referenced prototype's signature.</summary>
/// <param name="prototype">The prototype's row index.</param>
/// <param name="position">The formal's position.</param>
/// <param name="name">The formal's data-name.</param>
[EditorBrowsable(EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = true)]
public sealed class CobolPrototypeFormalAttribute(int prototype, int position, string name) : CobolFormalFieldsAttribute(position, name)
{
    /// <summary>The prototype's row index.</summary>
    public int Prototype { get; } = prototype;
}

/// <summary>One entry of a referenced prototype's formal or returning item.</summary>
/// <param name="prototype">The prototype's row index.</param>
/// <param name="formal">The POSITION of the formal whose tree holds the entry (its
/// <see cref="CobolPrototypeFormalAttribute.Position"/>), or <see cref="ReturningItem"/> for the returning item.</param>
/// <param name="ordinal">The entry's preorder index within its tree.</param>
/// <param name="parent">The preorder index of the entry's parent, -1 at the root.</param>
[EditorBrowsable(EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = true)]
public sealed class CobolPrototypeDescriptionAttribute(int prototype, int formal, int ordinal, int parent)
    : CobolDescriptionFieldsAttribute(ordinal, parent)
{
    /// <summary>The <see cref="Formal"/> of an entry of the prototype's returning item (a formal's position is never
    /// negative).</summary>
    public const int ReturningItem = -1;

    /// <summary>The prototype's row index.</summary>
    public int Prototype { get; } = prototype;
    /// <summary>The position of the formal, or <see cref="ReturningItem"/>.</summary>
    public int Formal { get; } = formal;
}
