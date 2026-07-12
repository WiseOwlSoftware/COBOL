// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;

namespace CobolNet.Binding.Bound;

// The SORT/MERGE/RELEASE/RETURN bound nodes (P7 Step 10i: the binder half moved to
// Binding/Procedure/Verbs/SortBinder.cs; these types STAY here — the emitter, BoundStores,
// UsageCollectionPass, and the source-generated visitor key on this namespace).

/// <summary>One sort/merge key (ISO §14.9.40 GR1/GR2 — significance is statement order, direction is the nearest
/// preceding ASCENDING/DESCENDING word): its character window within the SD record image (<paramref name="Offset"/>,
/// <paramref name="Length"/> — compile-time, §14.9.40.3 SR6a/SR6e: the same byte positions are the key in EVERY
/// record of the file) and its comparison kind — a NUMERIC key compares algebraically by decoded value (GR8 /
/// §8.8.4.2 — never through a collating sequence; <paramref name="Signed"/>/<paramref name="SignKind"/> are the
/// runtime <c>NumericSign</c> decode of the zoned/separate operational sign), an alphanumeric/group key compares as
/// characters under the statement's resolved collating sequence (GR5).</summary>
public sealed record BoundSortMergeKey(
    bool Descending, int Offset, int Length, bool Numeric, bool Signed, string SignKind);

/// <summary>The RECORD IS VARYING model of an SD/FD bound for the sort verbs (ISO §13.18.43): the resolved
/// DEPENDING ON place — RELEASE takes each record's length from it (GR13a), RETURN restores each returned record's
/// length into it (GR15) — and the min/max record sizes (the EC-SORT-MERGE-RELEASE bounds, §14.9.40 GR12b;
/// EC checking is OFF by default per COBOLNET_DESIGN §18.16, the bounds are carried for the seam).
/// <paramref name="Depending"/> is null for a variable-length SD/FD with no DEPENDING phrase (RECORD m TO n —
/// GR13b/c: each record then releases/writes at its own size; there is no length register to restore).</summary>
public sealed record SortVaryingInfo(Place? Depending, int Min, int Max);

/// <summary><c>SORT file-name-1 …</c> (ISO §14.9.40 Format 1): the three-phase file sort (GR9 — release, sequence,
/// return). <paramref name="Using"/>/<paramref name="InputProcedure"/> is the release phase (GR11/GR12),
/// <paramref name="Giving"/>/<paramref name="OutputProcedure"/> the return phase (GR14/GR15); a procedure is the
/// resolved inclusive pc range run as a bounded dispatch — the PC dispatcher's return IS the GR11/GR14
/// compiler-inserted return mechanism. <paramref name="Collating"/> is the GR5-resolved alphanumeric sequence
/// (statement alphabet first, else the program collating sequence, else null = native).
/// <paramref name="RecordWidth"/> is the SD record area's physical character-image width.</summary>
public sealed record BoundSort(
    FileModel File, int RecordWidth,
    IReadOnlyList<BoundSortMergeKey> Keys, bool DuplicatesInOrder, CollatingTable? Collating,
    IReadOnlyList<FileModel> Using, (int Start, int End)? InputProcedure,
    IReadOnlyList<FileModel> Giving, (int Start, int End)? OutputProcedure,
    SortVaryingInfo? Varying) : BoundStatement;

/// <summary><c>SORT data-name-2 …</c> (ISO §14.9.40 Format 2, COBOL-2002+): the in-place table sort over the typed
/// element array (COBOLNET_DESIGN §8.2 — the one sanctioned divergence from the image store: Format 2 operates on
/// the typed array directly with a typed comparer). <paramref name="Keys"/> are element-relative member paths; an
/// empty path is the table element itself (GR23). The whole fixed-OCCURS extent sorts (GR20/GR24).
/// <paramref name="Table"/> is carried (not its type name) because the element's storage type is finalized by the
/// POST-bind whole-group analysis (StoreAsImage) — the emitter reads <c>Table.ElementType</c> then.</summary>
public sealed record BoundTableSort(
    string ArrayPath, DataItem Table,
    IReadOnlyList<BoundTableSortKey> Keys, bool DuplicatesInOrder, CollatingTable? Collating) : BoundStatement;

/// <summary>One Format-2 table-sort key: the C# member path RELATIVE to an element variable (empty = the element
/// itself, ISO §14.9.40 GR23) and the key's <see cref="DataItem"/> (category/profile drive the typed compare).</summary>
public sealed record BoundTableSortKey(bool Descending, string MemberPath, DataItem Key);

/// <summary><c>MERGE file-name-1 …</c> (ISO §14.9.24): a k-way merge of the pre-sorted <paramref name="Using"/>
/// streams — equal keys keep USING-file order, all of one file's records before the next file's (GR4a/GR4b) —
/// written to every <paramref name="Giving"/> file (GR12 — each receives the FULL merged result) or pulled by
/// RETURN in the <paramref name="OutputProcedure"/> (GR8/GR9). Collating per GR5 (identical to SORT GR5).</summary>
public sealed record BoundMerge(
    FileModel File, int RecordWidth,
    IReadOnlyList<BoundSortMergeKey> Keys, CollatingTable? Collating,
    IReadOnlyList<FileModel> Using,
    IReadOnlyList<FileModel> Giving, (int Start, int End)? OutputProcedure,
    SortVaryingInfo? Varying) : BoundStatement;

/// <summary><c>RELEASE record-name-1 [FROM x]</c> (ISO §14.9.32): release the SD record's image to the initial
/// phase of the active sort (GR2). FROM ≡ <c>MOVE x TO record-name-1</c> then the same RELEASE (GR4). A varying SD
/// releases at the length the RECORD VARYING DEPENDING ON item holds (§13.18.43 GR13); a fixed SD at the record
/// area width (short images space-fill — §14.9.40 GR7c).</summary>
public sealed record BoundRelease(
    FileModel File, Place Record, int RecordWidth, BoundOperand? From, SortVaryingInfo? Varying) : BoundStatement;

/// <summary><c>RETURN file-name-1 RECORD [INTO x] AT END … [NOT AT END …]</c> (ISO §14.9.34): make the next record
/// (in key order) available in the SD record area (GR3); INTO ≡ RETURN then MOVE record-area → x (GR5, skipped at
/// end); at end → <paramref name="AtEnd"/>, else <paramref name="NotAtEnd"/> (GR3/GR4). A varying SD restores the
/// returned record's length into the DEPENDING item (§13.18.43 GR15).</summary>
public sealed record BoundReturn(
    FileModel File, Place RecordArea, Place? Into,
    IReadOnlyList<BoundStatement>? AtEnd, IReadOnlyList<BoundStatement>? NotAtEnd,
    SortVaryingInfo? Varying) : BoundStatement;
