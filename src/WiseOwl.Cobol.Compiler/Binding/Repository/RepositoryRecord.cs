// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections;
using System.Collections.Immutable;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.Repository;

namespace WiseOwl.Cobol.Binding.Repository;

/// <summary>
/// ⛔ THE REPOSITORY RECORD OF ONE PUBLISHED UNIT (ISO §8.13; docs/rearchitecture/DESIGN-external-repository.md §6.1; kb/Work
/// PB2100): what the external repository holds about a program, a function or a prototype, in the PERSISTED vocabulary
/// (<c>WiseOwl.Cobol.Runtime.Repository</c>'s enums, §6.2 rule 1), so the attribute renderer and the reader are pure
/// transcriptions of it and the ONE translation between the compiler's own types and what is persisted is
/// <see cref="RepositoryCodec.Of"/> and its inverse <see cref="RepositoryCodec.Decode"/>.
/// <para>§8.13: "This information about a source unit, excluding the externalized name of the source unit, is called its
/// signature. Whether the information is taken from a prototype or a definition, the information stored in the external
/// repository about the signature of a program or a function is the same." So a prototype and its definition publish the
/// same shape from one <see cref="RepositoryCodec.Of"/>, and what decides "the same signature" reads the decoded
/// DESCRIPTIONS, never a data-name (§6.3; ISO §13.7.3 SR2).</para>
/// <para>The record is a value: two records describing one unit are equal (<see cref="RecordList{T}"/> compares its
/// elements), which is what §17.4's backend-independence fixture and §17.7's determinism test read.</para>
/// </summary>
/// <param name="Kind">What the unit is (§5.1).</param>
/// <param name="Name">The unit's user-defined word as declared (diagnostics, the host surface and the word rows of §7.4).</param>
/// <param name="ExternalizedName">The unit's externalized name as formed (DOC-A.1-68); the NAME part, which §8.13 keeps
/// out of the signature.</param>
/// <param name="EntryConvention">The entry convention (§8.13 "the entry convention of the source unit"): COBOL, the one
/// admissible value (<c>OptionsBinder.ScreenEntryConvention</c>).</param>
/// <param name="DecimalPointIsComma">Whether the DECIMAL-POINT IS COMMA clause is specified in the source unit (§8.13).</param>
/// <param name="Currencies">The currency symbols and their currency strings the source unit defines (§8.13), ordered by
/// symbol.</param>
/// <param name="Raising">The exceptions the runtime element may raise (§8.13): the PROCEDURE DIVISION header's RAISING
/// phrase, each an identity (§6.4).</param>
/// <param name="Formals">The formal parameters by position, each with its manner of receiving and OPTIONAL (§8.13).</param>
/// <param name="Returning">The returning item's description, if any (§8.13).</param>
/// <param name="TypeDeclarations">The type declarations the descriptions reference (§8.13 "type declarations required for
/// the description of parameters and returning items"), indexed by <see cref="DescriptionRecord.TypeDeclaration"/> and
/// <see cref="PicRecord.RestrictedTypeDeclaration"/>.</param>
/// <param name="DynamicLengthStructures">The dynamic-length structures the descriptions reference, by definition, indexed by
/// <see cref="DescriptionRecord.DynStructure"/> (§6.4).</param>
/// <param name="ReferencedPrototypes">The prototypes a restricted program- or function-pointer of the record names, each with
/// its FULL signature (§6.4), indexed by <see cref="PicRecord.RestrictedPrototype"/>.</param>
internal sealed record RepositoryRecord(
    RepositoryUnitKind Kind, string Name, string ExternalizedName,
    string EntryConvention, bool DecimalPointIsComma, RecordList<CurrencyRecord> Currencies,
    RecordList<RaisingRecord> Raising, RecordList<FormalRecord> Formals, DescriptionRecord? Returning,
    RecordList<DescriptionRecord> TypeDeclarations, RecordList<StructureRecord> DynamicLengthStructures,
    RecordList<PrototypeRecord> ReferencedPrototypes);

/// <summary>One currency symbol a source unit defines and its currency string (ISO §12.3.7.4 GR13; §8.13), with the class of
/// the literal that defined it (§12.3.7.3 SR28).</summary>
internal sealed record CurrencyRecord(char Symbol, string Text, RepositoryLiteralClass Class);

/// <summary>One element of a RAISING phrase (ISO §14.2.1; §8.13 "the exceptions that may be raised"): an exception-name, or
/// the externalized identity of an object class or an interface (§6.4), with the FACTORY phrase.</summary>
internal sealed record RaisingRecord(RepositoryRaisingKind Kind, string Identity, bool Factory);

/// <summary>One formal parameter (ISO §8.13 "the description of the parameters … and the manner of receiving parameters (by
/// reference or by value) and whether they are optional or not"). <paramref name="Name"/> is recorded for diagnostics and the
/// host surface and is never compared (§6.3).</summary>
internal sealed record FormalRecord(int Position, string Name, PassingMode Mode, bool Optional, DescriptionRecord Description);

/// <summary>A dynamic-length structure by DEFINITION (ISO §12.3.7.2; §6.4): its layout, which is what a description compares,
/// and its name, which is the publisher's word.</summary>
internal sealed record StructureRecord(int Index, string Name, RepositoryPrefix Prefix, bool Delimited, string? PhysicalStructureName);

/// <summary>A prototype a restricted program- or function-pointer of the record names, with its FULL signature, so the
/// importer types the pointer by signature (ISO §13.18.60.4 GR25 / GR26; §6.4). Its own restricted-pointer formals index other
/// rows, so a recursive prototype records as a finite graph. <paramref name="HasSignature"/> is false for a prototype whose
/// details ISO §12.3.8.4 GR10 c) / GR11 c) leave to the external repository (no compile-time signature; the state slice 7
/// deletes, §6.4).</summary>
internal sealed record PrototypeRecord(int Index, RepositoryUnitKind Kind, string ExternalizedName, bool HasSignature,
    RecordList<FormalRecord> Formals, DescriptionRecord? Returning);

/// <summary>
/// One data description entry of a formal, a returning item or a type declaration, with its subordinate entries in
/// declaration order (§6.1, §6.3). Every member of <c>DataItem</c> that a comparator or a derived quantity reads, and that
/// the data-description copy carries (<c>DescriptionCopyKind</c> Clause, MemberOnly, Alignment, CopyWritten and the
/// description half of EntryOnly), has a field here; the converse drift test holds that true.
/// </summary>
/// <param name="Level">The level-number.</param>
/// <param name="Name">The data-name as written (null for FILLER): diagnostics, the host surface and the REDEFINES / OCCURS
/// DEPENDING targets inside one record; never compared (ISO §13.7.3 SR2).</param>
/// <param name="RedefinesTarget">The data-name a REDEFINES clause names (a sibling in the record).</param>
/// <param name="Occurs">The allocated occurrence count (<c>DataItem.Occurs</c>).</param>
/// <param name="Table">The structured OCCURS clause (<c>OccursSpec</c>), when the entry has one.</param>
/// <param name="Justified">JUSTIFIED (ISO §13.18.32).</param>
/// <param name="BlankWhenZero">BLANK WHEN ZERO (ISO §13.18.8).</param>
/// <param name="Synchronized">The SYNCHRONIZED clause's form (ISO §13.18.55; kb/Work PB538).</param>
/// <param name="Aligned">ALIGNED (ISO §13.18.1).</param>
/// <param name="AlignedAsLevelOne">ALIGNED placement as a level-1 item (<c>DataItem.AlignedAsLevelOne</c>).</param>
/// <param name="Based">The BASED clause (ISO §13.18.5).</param>
/// <param name="AnyLength">ANY LENGTH (ISO §13.18.2).</param>
/// <param name="DynamicLength">DYNAMIC LENGTH (ISO §13.18.19).</param>
/// <param name="DynMaxSize">The DYNAMIC LENGTH maximum size (the LIMIT phrase, else the default).</param>
/// <param name="DynStructure">The dynamic-length structure, an index into <see cref="RepositoryRecord.DynamicLengthStructures"/>.</param>
/// <param name="TypeName">The carried type-name (§8.5.3.1's first conjunct; compared, it is not a data-name).</param>
/// <param name="Strong">The carried STRONG phrase.</param>
/// <param name="External">The entry's own EXTERNAL clause.</param>
/// <param name="IsTypedef">The entry is a type declaration (TYPEDEF).</param>
/// <param name="TypedefStrong">The TYPEDEF clause's STRONG phrase.</param>
/// <param name="IsExternalTypedef">The type declaration is EXTERNAL.</param>
/// <param name="ExternalFromType">EXTERNAL carried from a type declaration (ISO §13.18.22.4 GR3).</param>
/// <param name="TypeDeclaration">The type declaration <see cref="TypeName"/> names, an index into
/// <see cref="RepositoryRecord.TypeDeclarations"/> (§8.13 "type declarations required for the description").</param>
/// <param name="GroupUsage">GROUP-USAGE (ISO §13.18.29).</param>
/// <param name="Pic">The analyzed PICTURE / USAGE / SIGN profile of an elementary entry; null for a group.</param>
/// <param name="Children">The subordinate entries, in declaration order.</param>
internal sealed record DescriptionRecord(
    int Level, string? Name, string? RedefinesTarget, int? Occurs, OccursRecord? Table,
    bool Justified, bool BlankWhenZero, RepositorySynchronized Synchronized, bool Aligned, bool AlignedAsLevelOne, bool AnyLength, bool Based,
    bool DynamicLength, int DynMaxSize, int? DynStructure,
    string? TypeName, bool Strong, bool External, bool IsTypedef, bool TypedefStrong, bool IsExternalTypedef, bool ExternalFromType,
    int? TypeDeclaration, RepositoryGroupUsage GroupUsage, PicRecord? Pic, RecordList<DescriptionRecord> Children);

/// <summary>The structured OCCURS clause (ISO §13.18.38; <c>OccursSpec</c>): the bounds, the DEPENDING ON object as a word
/// of the record (or <see cref="RepositoryCodec.OutsideTheRecord"/>), and a DYNAMIC table's capacity phrases. The KEY and
/// INDEXED BY phrases are not recorded: no comparator or host type reads them (§6.3).</summary>
internal sealed record OccursRecord(int Min, int Max, string? Depending, bool Dynamic, string? Capacity, int? InitialCapacity,
    int? ExpectedMax, bool Initialized);

/// <summary>
/// <c>PicInfo</c> BY VALUE — every member (§6.1). The two restriction NAMES are kept as written by the publishing element
/// (words) beside what they resolve to (a type declaration; a referenced prototype's full signature), each an index into
/// the record's own tables (§6.2 rule 4); <see cref="DigitPositions"/> is <c>PicInfo</c>'s nullable backing.
/// </summary>
internal sealed record PicRecord(
    RepositoryCategory Category, RepositoryUsage Usage, int Length, int Digits, int Scale, bool Signed, string SignKind,
    bool IsRecovery, string? RestrictedTypeName, int? RestrictedTypeDeclaration, string? RestrictedPrototypeName,
    int? RestrictedPrototype, bool PackedNoSign, bool FloatLittleEndian, bool IsFloatEdited, string? EditMask,
    string? CurrencyString, RepositoryLiteralClass? CurrencyClass, bool IsAlphabetic, int? DigitPositions,
    PictureClauseRecord? Clause, RecordList<EditRuleRecord>? EditingRules, LocaleEditRecord? LocaleEdit,
    ObjectReferenceKind ObjectRefKind, string? ObjectRefName, string? ObjectRefIdentity, bool ObjectRefFactory,
    bool ObjectRefOnly, bool ObjectRefFormal);

/// <summary><c>PictureClauseIdentity</c> by value (kb/Work PB1166).</summary>
internal sealed record PictureClauseRecord(string CharacterString, string? CurrencyString, bool? DecimalPointIsComma, string? Editing);

/// <summary><c>CobolEdit.EditRule</c> by value (a PICTURE EDITING phrase's resolved rule).</summary>
internal sealed record EditRuleRecord(char Char1, string Neg, string Pos, bool SimpleInsertion, bool Floating);

/// <summary><c>LocaleEditSpec</c> by value (ISO §13.18.40.2 format 2): <paramref name="External"/> is null for the locale
/// current at each use (<c>LocaleRef.Current</c>), else the named locale's external identification with the name as
/// written.</summary>
internal sealed record LocaleEditRecord(string? LocaleName, string? External, bool FromLiteral, int Size, string Picture);

/// <summary>
/// An immutable list that compares by its ELEMENTS, so a record holding one is a value (§17.4, §17.7). An
/// <see cref="ImmutableArray{T}"/> compares by reference, which would make two encodings of one unit unequal.
/// </summary>
internal readonly struct RecordList<T>(ImmutableArray<T> items) : IReadOnlyList<T>, IEquatable<RecordList<T>>
{
    private readonly ImmutableArray<T> _items = items.IsDefault ? [] : items;

    /// <summary>The empty list.</summary>
    public static RecordList<T> Empty { get; } = new([]);

    public T this[int index] => _items[index];

    public int Count => _items.IsDefault ? 0 : _items.Length;

    public bool Equals(RecordList<T> other) =>
        Count == other.Count && (Count == 0 || _items.SequenceEqual(other._items, EqualityComparer<T>.Default));

    public override bool Equals(object? obj) => obj is RecordList<T> other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var item in this) hash.Add(item);
        return hash.ToHashCode();
    }

    public static bool operator ==(RecordList<T> left, RecordList<T> right) => left.Equals(right);

    public static bool operator !=(RecordList<T> left, RecordList<T> right) => !left.Equals(right);

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)(_items.IsDefault ? [] : _items)).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
