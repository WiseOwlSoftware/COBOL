// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Binding.Model;

/// <summary>
/// ⛔ THE IDENTITY OF A METHOD LOCAL, NEVER ITS NAME (kb/Work PB4321; DESIGN-backend-abstraction.md §1.2, §1.3 L8). A
/// statement-local value the generated method holds — a loop counter, a hoisted identification, an index vector — is
/// referenced from the model and the bound tree by WHO DECLARES IT, and each backend names it where it declares it:
/// the Roslyn backend spells a C# identifier (<c>CodeGen.LocalNames</c>), a CIL backend allocates a slot. A local's
/// name is no ABI (no other module binds to it), so it is no fact of the neutral tree.
/// <para>The binder's locals are <see cref="StatementLocal"/>s. A backend that builds model nodes over locals of its own
/// (a position over its own loop counter) derives its own kind, which carries what that backend needs and nothing the
/// binder can produce.</para>
/// </summary>
public abstract record MethodLocal;

/// <summary>The construct that declares a <see cref="StatementLocal"/>: with the ordinal, the local's whole identity.</summary>
public enum StatementLocalRole
{
    /// <summary>An INITIALIZE per-occurrence loop counter (ISO §14.9.20.4 5) b) 2.: "each occurrence of the elementary data
    /// item is a possible receiving-operand"); the ordinal is the unit's INITIALIZE loop serial.</summary>
    InitializeOccurrence,

    /// <summary>The element loop counter of a variable-length group MOVE's corresponding tables (ISO §14.6.9.2:
    /// "Correspondingly numbered elements are moved according to the rules of the MOVE statement"); the ordinal is the
    /// unit's element-loop serial.</summary>
    ElementMoveOccurrence,

    /// <summary>The sending group's anchor of a CORRESPONDING statement (ISO §14.7.6 6): the identification is done at the
    /// start of the statement); the ordinal is the statement's serial in the unit.</summary>
    CorrespondingSendingAnchor,

    /// <summary>The receiving group's anchor of a CORRESPONDING statement, the twin of
    /// <see cref="CorrespondingSendingAnchor"/>; the same ordinal.</summary>
    CorrespondingReceivingAnchor,

    /// <summary>A table(ALL) enumeration's index vector (ISO §15.3 14), one current occurrence per ALL level); the ordinal is
    /// the unit's table(ALL) serial.</summary>
    TableAllIndices,

    /// <summary>The index vector of a table(ALL) argument's held copy (kb/Work PB4162); the ordinal is the held table's
    /// <see cref="DataItem.Uid"/>.</summary>
    HeldTableAllIndices,

    /// <summary>A report VARYING counter (ISO §13.18.64.4 1): "an independent temporary integer data item"); the ordinal is
    /// the counter's compilation-unique <c>ReportVaryingModel.Uid</c>.</summary>
    ReportVaryingCounter,
}

/// <summary>A method local the BINDER declares: the construct that declares it (<paramref name="Role"/>) and its ordinal among
/// that role's locals of the unit (<paramref name="Ordinal"/>). Two references to the same local are equal values, so the
/// declaration and every read name the one local.</summary>
public sealed record StatementLocal(StatementLocalRole Role, int Ordinal) : MethodLocal;
