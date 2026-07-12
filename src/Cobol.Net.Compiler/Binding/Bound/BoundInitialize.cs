// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;

namespace CobolNet.Binding.Bound;

// The INITIALIZE bound nodes (P7 Step 10e: the binder half moved to
// Binding/Procedure/Verbs/InitializeBinder.cs; these records/enum STAY here — the source-generated
// visitor, the emitter, BoundStores, and UsageCollectionPass key on this namespace).

/// <summary><c>INITIALIZE</c> (ISO §14.9.20), expanded at BIND time into the spec's series of implicit elementary
/// MOVEs (§14.9.20 GR4) — there is no runtime INITIALIZE: each action is one per-elementary store (the full MOVE
/// conversion/editing/padding/truncation rules apply at emit, through the ONE MOVE path) or a per-occurrence loop
/// over a table dimension (GR5b2 — every occurrence of a table element is a possible receiving operand). Multiple
/// identifier-1 expand in source order as separate statements (GR3); elementary receivers within a group appear in
/// definition order (GR8).</summary>
public sealed record BoundInitialize(IReadOnlyList<InitializeAction> Actions) : BoundStatement;

/// <summary>One step of an expanded INITIALIZE.</summary>
public abstract record InitializeAction;

/// <summary>One implicit elementary MOVE (ISO §14.9.20 GR4): <paramref name="Source"/> stores into
/// <paramref name="Target"/> under the MOVE rules (§14.9.25 — conversion, editing, JUSTIFIED/padding, truncation).</summary>
public sealed record InitializeStore(Place Target, BoundOperand Source) : InitializeAction;

/// <summary>The per-occurrence expansion of ONE OCCURS dimension (ISO §14.9.20 GR5b2): the body repeats for
/// <paramref name="Var"/> = 1‥<paramref name="Count"/>; nested dimensions nest loops, outermost first (the loop
/// variable is spliced into each body place's subscript position).</summary>
public sealed record InitializeLoop(string Var, int Count, IReadOnlyList<InitializeAction> Body) : InitializeAction;

/// <summary>A receiver the binder could not materialize as a typed place — the backend emits a loud runtime
/// guard (COBOLNET_DESIGN §1.4), never a silent skip.</summary>
public sealed record InitializeErrorAction(string Feature) : InitializeAction;

/// <summary>The per-occurrence expansion of an OCCURS DYNAMIC dimension (ISO §14.9.20 GR10 / §8.5.1.9.1; data-model
/// D9): the body repeats for <paramref name="Var"/> = 1‥<paramref name="CapacityExpr"/> — the table's CURRENT
/// capacity, a RUN-TIME value (unlike the fixed-count <see cref="InitializeLoop"/>). The elements are initialized by
/// the INITIALIZE statement's own stores (the category defaults / REPLACING / VALUE-phrase senders — NOT the OCCURS
/// grow-seed), the capacity left unchanged (GR10: "all the elements of the table up to current capacity … are
/// initialized … the current capacity is left unchanged").</summary>
public sealed record InitializeDynLoop(string Var, string CapacityExpr, IReadOnlyList<InitializeAction> Body) : InitializeAction;

/// <summary>The INITIALIZE data categories (ISO §14.9.20.2 category-name, per §8.5.2 class/category) — the
/// COBOL-85 five plus the Phase-4a BOOLEAN and NATIONAL members (binder-side classification + GR6c default
/// fills; the REPLACING/VALUE <em>category words</em> BOOLEAN/NATIONAL — like NATIONAL-EDITED, the pointer
/// categories, and OBJECT-REFERENCE — are still absent from the initializeCategory grammar rule and arrive
/// with their lexer tokens in the edition-gated grammar fragments, a parse error today = loud).</summary>
public enum InitializeCategory { Alphabetic, Alphanumeric, AlphanumericEdited, Numeric, NumericEdited, Boolean, National }
