// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;

namespace CobolNet.Binding.Bound;

// The STRING/UNSTRING bound nodes (P7 Step 10d: the binder half moved to
// Binding/Procedure/Verbs/StringUnstringBinder.cs; these records STAY here — the source-generated
// visitor and StatementChildren key on this namespace).

// The entire STRING/UNSTRING surface — including NOT ON OVERFLOW and END-STRING/END-UNSTRING — is
// COBOL-85 (both verbs' phrases were complete by 1985); no edition gate applies. The post-85 deltas
// (class national / boolean operands, zero-length-item rules, dynamic-length SIZE — §14.9.43.4 GR1)
// concern data shapes the current data model cannot describe, and the EC-OVERFLOW-STRING /
// EC-OVERFLOW-UNSTRING names (2002+, GR8b / GR16b) await the EC model; the ON/NOT ON OVERFLOW control
// flow itself is edition-invariant.

/// <summary>One STRING sending operand with its governing delimiter (ISO §14.9.43.2): the DELIMITED phrase
/// written after a run of senders governs every sender of that run, so the binder back-propagates it
/// (<c>StringUnstringBinder.BindString</c>); a run with no following phrase is DELIMITED BY SIZE (SR9).
/// <paramref name="BySize"/> and a non-null <paramref name="Delimiter"/> are mutually exclusive.</summary>
public sealed record BoundStringSending(BoundOperand Value, BoundOperand? Delimiter, bool BySize);

/// <summary><c>STRING sendings… INTO into [WITH POINTER ptr] [ON/NOT ON OVERFLOW …]</c> (ISO §14.9.43): each
/// sending transfers character by character at the pointer position into the receiver — whose untouched portions
/// are PRESERVED (GR7, never space-filled) — under the GR8 per-character range check.</summary>
public sealed record BoundStringStmt(
    IReadOnlyList<BoundStringSending> Sendings, Place Into, Place? Pointer,
    IReadOnlyList<BoundStatement>? OnOverflow, IReadOnlyList<BoundStatement>? NotOnOverflow) : BoundStatement;

/// <summary>One UNSTRING delimiter (ISO §14.9.48.2): its value (a literal, a figurative — a single character per
/// GR7 — or a field read at execution) and whether the ALL phrase collapses contiguous occurrences (GR7).</summary>
public sealed record BoundUnstringDelimiter(BoundOperand Value, bool All);

/// <summary>One UNSTRING receiving area with its optional DELIMITER IN / COUNT IN companions (§14.9.48.2).
/// <paramref name="NoDelimSize"/> is the GR11b examination size used when no DELIMITED phrase governs: the
/// receiver's size in character positions, one less when its sign occupies a separate character position
/// (−1 marks a reference-modified receiver whose size is not static).</summary>
public sealed record BoundUnstringReceiver(Place Target, Place? DelimiterIn, Place? CountIn, int NoDelimSize);

/// <summary><c>UNSTRING source [DELIMITED BY …] INTO receivers… [WITH POINTER ptr] [TALLYING IN tly]
/// [ON/NOT ON OVERFLOW …]</c> (ISO §14.9.48).</summary>
public sealed record BoundUnstringStmt(
    Place Source, IReadOnlyList<BoundUnstringDelimiter> Delimiters, IReadOnlyList<BoundUnstringReceiver> Receivers,
    Place? Pointer, Place? Tallying,
    IReadOnlyList<BoundStatement>? OnOverflow, IReadOnlyList<BoundStatement>? NotOnOverflow) : BoundStatement;
