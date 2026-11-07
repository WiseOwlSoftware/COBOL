// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Binding.Model;

/// <summary>⛔ THE CENSUS OF REFERENCE SHAPES THE RESOLVER DEFERS (kb/Work PB1030). Every
/// deferred answer of the reference resolver (<c>RefOutcome.Deferred</c>) names one member, so the list of legal reference shapes WiseOwl COBOL has
/// not built is THIS enum and nowhere else; <c>DeferredShapes.Describe</c> (beside the resolver's answer) gives each its text and its
/// owner. A new deferral is a new member — <c>RefResolutionDriftTests</c> fails a member with no description.</summary>
public enum DeferredShape
{
    /// <summary>LINAGE-COUNTER / LINE-COUNTER / PAGE-COUNTER (§2023:8.4.3.14 / §2023:8.4.3.15) reached the resolver at a
    /// site that does not route the special register to its runtime source.</summary>
    UnroutedSpecialRegister,
    /// <summary>A whole (unsubscripted) OCCURS DYNAMIC table (data-model D9) outside the contexts that take one.</summary>
    DynamicWholeTable,
    /// <summary>A string-canonical REDEFINES class whose backing is not reachable from the reference — the access
    /// path to the canonical's parent struct could not be built. A class within a FIXED, OCCURS DEPENDING or OCCURS
    /// DYNAMIC table is reached through the subscripted parent path (kb/Work PB1279, PB1933).</summary>
    NestedClassBacking,
    /// <summary>An item the access-path builder has no path for (a whole-table reference outside a
    /// table-taking context, or an OCCURS level the path cannot address).</summary>
    UnbuiltAccessPath,
    /// <summary>Reference modification of a numeric item whose substrate has no character image.</summary>
    NumericRefModSubstrate,
    /// <summary>An expression subscript or reference-modifier bound in a context with no statement to evaluate it
    /// in (a data-division resolver has no segment materializer).</summary>
    SegmentWithoutStatement,
    /// <summary>A subscript or reference-modifier expression whose own operand is a deferred shape.</summary>
    DeferredSegmentOperand,
}
