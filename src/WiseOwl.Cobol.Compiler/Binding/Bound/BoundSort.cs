// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding.Bound;

// The SORT/MERGE/RELEASE/RETURN bound nodes (P7 Step 10i: the binder half moved to
// Binding/Procedure/Verbs/SortBinder.cs; these types STAY here — the emitter, BoundStores,
// UsageCollectionPass, and the source-generated visitor key on this namespace).

/// <summary>One sort/merge key (ISO §2023:14.9.40 GR1/GR2 — significance is statement order, direction is the nearest
/// preceding ASCENDING/DESCENDING word): its BYTE window within the SD record image (<paramref name="Offset"/>,
/// <paramref name="Length"/> — compile-time, §2023:14.9.40.3 SR6a/SR6e: the same byte positions are the key in EVERY
/// record of the file; a national position occupies two of those bytes, §2023:13.18.60.4 GR8 / D-N1) and its
/// <paramref name="Class"/>, which selects the comparator: a NUMERIC key compares algebraically by decoded value
/// (GR8 → §2023:8.8.4.2.4 — never through a collating sequence, and <paramref name="Item"/> carries the leaf whose
/// profile decodes the window), a NATIONAL key decodes its byte pairs and compares under the GR5 national sequence
/// (§2023:8.8.4.2.9), and an alphanumeric or ordinary group key compares as characters under the GR5 alphanumeric
/// sequence (§2023:8.8.4.2.7). (A BOOLEAN key is not a legal key — §2023:14.9.40.3 SR6 c), SR14 c); §2023:14.9.24.3 SR4 c).)
/// <para><paramref name="LayoutRecord"/> is the place of the key's record when a variable-length member precedes
/// the key (kb/Work PB1025; docs/CONFORMANCE.md §3 D-KWV): <paramref name="Offset"/> is then the key's offset in
/// that record's FIXED run, and the runtime locates it in each record through the record type's layout. Null for
/// every key whose window is the same in every record.</para></summary>
public sealed record BoundSortMergeKey(
    bool Descending, int Offset, int Length, CollatingClass Class, DataItem? Item, Place? LayoutRecord = null, Place? Written = null)
    : IBoundImplicitOperands
{
    /// <summary>The key data-name as the ASCENDING / DESCENDING KEY phrase wrote it (<see cref="Written"/>) is the statement's explicit
    /// reference (X3.23-1985 USE FOR DEBUGGING general rule 5 d); the record whose layout locates the key at run time is not one.</summary>
    public IEnumerable<object> ImplicitOperands => LayoutRecord is null ? [] : [LayoutRecord];
}

/// <summary>The RECORD IS VARYING model of an SD/FD bound for the sort verbs (ISO §2023:13.18.43): the resolved
/// DEPENDING ON place — RELEASE takes each record's length from it (GR13a), RETURN restores each returned record's
/// length into it (GR15) — and the min/max record sizes (the EC-SORT-MERGE-RELEASE bounds, §2023:14.9.40 GR12b;
/// §2023:13.18.43.4 GR14 b) / GR19 b) for the RELEASE statement — tested by CobolSort, kb/Work PB1036).
/// <paramref name="Depending"/> is null for a variable-length SD/FD with no DEPENDING phrase (RECORD m TO n —
/// GR13b/c: each record then releases/writes at its own size; there is no length register to restore).</summary>
public sealed record SortVaryingInfo(Place? Depending, int Min, int Max);

/// <summary>A sort verb that carries the RECORD VARYING model of its SD (SORT, MERGE, RELEASE): the DEPENDING ON item is the file description's,
/// read or restored on the statement's behalf, so the program did not write it (X3.23-1985 USE FOR DEBUGGING general rule 5: "explicitly
/// references"). RETURN states the same fact in its own larger list.</summary>
public interface IBoundSortVarying : IBoundImplicitOperands
{
    SortVaryingInfo? Varying { get; }

    IEnumerable<object> IBoundImplicitOperands.ImplicitOperands => Varying is null ? [] : [Varying];
}

/// <summary><c>SORT file-name-1 …</c> (ISO §2023:14.9.40 Format 1): the three-phase file sort (GR9 — release, sequence,
/// return). <paramref name="Using"/>/<paramref name="InputProcedure"/> is the release phase (GR11/GR12),
/// <paramref name="Giving"/>/<paramref name="OutputProcedure"/> the return phase (GR14/GR15); a procedure is the
/// resolved inclusive pc range run as a bounded dispatch — the PC dispatcher's return IS the GR11/GR14
/// compiler-inserted return mechanism. <paramref name="Collating"/> is the GR5-resolved sequence PAIR — the
/// alphanumeric one for keys of class alphabetic/alphanumeric and the national one for keys of class national,
/// each taken from the statement alphabet first, else the matching program collating sequence, else native.
/// The sort file's record length is <see cref="FileModel.RecordWidth"/> of <paramref name="File"/> — the ONE
/// record-area size (ISO §2023:13.18.43.4 GR2), which a Format 1 RECORD CONTAINS integer-1 on the SD sets (kb/Work
/// PB1276); it is not carried here a second time.</summary>
public sealed record BoundSort(
    FileModel File,
    IReadOnlyList<BoundSortMergeKey> Keys, bool DuplicatesInOrder, SortCollation Collating,
    IReadOnlyList<FileModel> Using, PcRange? InputProcedure,
    IReadOnlyList<FileModel> Giving, PcRange? OutputProcedure,
    SortVaryingInfo? Varying) : BoundStatement, IBoundSortVarying;

/// <summary><c>SORT data-name-2 …</c> (ISO §2023:14.9.40 Format 2, COBOL-2002+): the in-place table sort over the typed
/// element array (DESIGN §8.2 — the one sanctioned divergence from the image store: Format 2 operates on
/// the typed array directly with a typed comparer). <paramref name="Keys"/> are element-relative member chains; an
/// empty chain is the table element itself (GR23). <paramref name="Count"/> is the table's CURRENT occurrence count
/// (GR20 — "determined by the rules in the OCCURS clause": the fixed integer, an OCCURS DEPENDING data-name-1, or a
/// dynamic-capacity table's current capacity), and only those occurrences are sorted and placed back (GR24; kb/Work
/// PB1174). <paramref name="Table"/> is carried (not its type name) because the element's storage type is finalized
/// by the POST-bind whole-group analysis (StoreAsImage) — the emitter reads <c>Table.ElementType</c> then.
/// <para><paramref name="Storage"/> is WHERE the table's elements live (<see cref="TableSortStorage"/>): the typed
/// element array a plain table is, or the byte area a table inside a REDEFINES class shares with the other views
/// (§2023:13.18.44.4 GR1). The statement's two halves — §2023:14.9.40.4 GR19's key comparison and GR24's placing back — are the same
/// for both; only how an element is reached differs (kb/Work PB1175, PB1055).</para>
/// <para><paramref name="Written"/> is data-name-2 as the statement wrote it: the WHOLE table under the enclosing occurrences the
/// statement names (<c>PlaceBuilder.WholeTable</c>), the operand the statement explicitly references and whose content it changes
/// (X3.23-1985 USE FOR DEBUGGING general rules 5 and 6 c; kb/Work PB4855). The sort itself reads <paramref name="Storage"/>, never
/// it.</para></summary>
public sealed record BoundTableSort(
    TableSortStorage Storage, DataItem Table, AllCount Count,
    IReadOnlyList<BoundTableSortKey> Keys, bool DuplicatesInOrder, SortCollation Collating, TableAllPlace Written) : BoundStatement;

/// <summary>Where a Format-2 table SORT's elements are stored — the closed answer the binder reads off the table's
/// data description (§2023:14.9.40.3 SR13, §2023:13.18.44.4 GR1). The data-name-2 reference's own subscripts are the ENCLOSING
/// tables' occurrences (§2023:8.4.2.3.3 SR3, SR5 e), SR6), read once at the statement and carried here as structure.</summary>
public abstract record TableSortStorage
{
    /// <summary>A table with its own typed element array, reached by <paramref name="Array"/> — the whole-table
    /// access path, with an index for each enclosing table (<c>PlaceBuilder.BuildTablePath</c>).
    /// <para><paramref name="KeyWindowOuter"/> is null when every key is a stored member of the element struct (the key
    /// is read through its <see cref="BoundTableSortKey.MemberChain"/> off the element). It is the enclosing tables' rendered index expressions when ANY key is not —
    /// a key that lies behind a REDEFINES view of the element has no stored field on the struct (kb/Work PB599) — and then
    /// EVERY key is read through its window at an occurrence number (<c>PlaceBuilder.ResolveItemAt</c>, the
    /// <see cref="SharedArea"/> law), the element order is sorted, and the elements are placed back by that
    /// permutation.</para></summary>
    public sealed record TypedArray(AccessPath Array, IReadOnlyList<Position>? KeyWindowOuter = null) : TableSortStorage;

    /// <summary>A table whose storage is a shared byte area — a REDEFINES class, a record area shared by several
    /// 01s, a BASED or EXTERNAL record. It has no element array: each element and each key is a window the class's
    /// own offset law places (<c>PlaceBuilder.ResolveItemAt</c>), one rendered index expression per OCCURS
    /// level — <paramref name="OuterIndexExprs"/> for the enclosing tables, then the element's own.</summary>
    public sealed record SharedArea(IReadOnlyList<Position> OuterIndexExprs) : TableSortStorage;
}

/// <summary>One Format-2 table-sort key: the chain of members from an element of a
/// <see cref="TableSortStorage.TypedArray"/> table down to the key, outermost first (empty = the element itself, ISO
/// §2023:14.9.40.4 GR23) — the items, never their joined access text, so each backend spells the access (kb/Work PB4320) —
/// and the key's <see cref="DataItem"/> (category/profile drive the typed compare). For a
/// <see cref="TableSortStorage.SharedArea"/> table the chain is unused — the key item is addressed through the class's
/// window law. A null chain is a key with no stored field on the element struct (a REDEFINES-view member, kb/Work
/// PB599): the statement reads every key of such a table through its window
/// (<see cref="TableSortStorage.TypedArray.KeyWindowOuter"/>).</summary>
public sealed record BoundTableSortKey(bool Descending, IReadOnlyList<DataItem>? MemberChain, DataItem Key)
{
    /// <summary>The key data-name as the statement WROTE it — the key item in every current occurrence of the table
    /// (<c>PlaceBuilder.WholeTable</c>): a key the statement names is an operand it explicitly references, and the sort moves its content
    /// (X3.23-1985 USE FOR DEBUGGING general rules 5 and 6 c; kb/Work PB4855). Null for a key the statement did not write: the table
    /// element itself when data-name-1 is omitted (ISO/IEC 1989:2023 §2023:14.9.40.4 GR23, which is data-name-2, already
    /// <see cref="BoundTableSort.Written"/>) and the keys of the table's own KEY phrase (GR21).</summary>
    public TableAllPlace? Written { get; init; }
}

/// <summary><c>MERGE file-name-1 …</c> (ISO §2023:14.9.24): a k-way merge of the pre-sorted <paramref name="Using"/>
/// streams — equal keys keep USING-file order, all of one file's records before the next file's (GR4a/GR4b) —
/// written to every <paramref name="Giving"/> file (GR12 — each receives the FULL merged result) or pulled by
/// RETURN in the <paramref name="OutputProcedure"/> (GR8/GR9). Collating per GR5 (identical to SORT GR5). The
/// record length is <paramref name="File"/>'s <see cref="FileModel.RecordWidth"/>, as for <see cref="BoundSort"/>.</summary>
public sealed record BoundMerge(
    FileModel File,
    IReadOnlyList<BoundSortMergeKey> Keys, SortCollation Collating,
    IReadOnlyList<FileModel> Using,
    IReadOnlyList<FileModel> Giving, PcRange? OutputProcedure,
    SortVaryingInfo? Varying) : BoundStatement, IBoundSortVarying;

/// <summary><c>RELEASE record-name-1 [FROM x]</c> (ISO §2023:14.9.32): release the SD record's image to the initial
/// phase of the active sort (GR2). FROM ≡ <c>MOVE x TO record-name-1</c> then the same RELEASE (GR4). A varying SD
/// releases at the length the RECORD VARYING DEPENDING ON item holds (§2023:13.18.43 GR13); a fixed SD at its ONE record
/// size, the largest record description's (§2023:13.18.43.4 GR5 a) — a shorter record description sends the record area's
/// image, kb/Work PB322 F). A short USING-file record is filled by §2023:14.9.40.4 GR7, the USING transfer's own rule
/// (kb/Work PB1140), and a short RETURNed record for a GIVING file by GR16.</summary>
public sealed record BoundRelease(
    FileModel File, Place Record, int RecordWidth, BoundMove? FromMove, SortVaryingInfo? Varying) : BoundStatement, IBoundSortVarying;

/// <summary><c>RETURN file-name-1 RECORD [INTO x] AT END … [NOT AT END …]</c> (ISO §2023:14.9.34): make the next record
/// (in key order) available in the SD record area (GR3); INTO ≡ RETURN then MOVE record-area → x (GR5, skipped at
/// end); at end → <paramref name="AtEnd"/>, else <paramref name="NotAtEnd"/> (GR3/GR4). A varying SD restores the
/// returned record's length into the DEPENDING item (§2023:13.18.43 GR15).</summary>
public sealed record BoundReturn(
    FileModel File, Place RecordArea, BoundMove? IntoMove,
    IReadOnlyList<BoundStatement>? AtEnd, IReadOnlyList<BoundStatement>? NotAtEnd,
    SortVaryingInfo? Varying) : BoundStatement, IBoundImplicitOperands, IBoundIntoPhrase
{
    /// <summary>RETURN names the file, never its record: the record area, the DEPENDING item a varying SD restores and the record the INTO move
    /// sends are the verb's own operands (X3.23-1985 USE FOR DEBUGGING general rule 5: "explicitly references"); the INTO identifier is written.</summary>
    public IEnumerable<object> ImplicitOperands
    {
        get
        {
            yield return RecordArea;
            if (IntoMove is not null) yield return IntoMove.Source;
            if (Varying is not null) yield return Varying;
        }
    }
}
