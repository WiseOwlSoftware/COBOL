// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Binding.Passes;

/// <summary>
/// The CheckCommunicationRecords pass: the rules of ISO/IEC 1989:2002 A.1.2.2 that read a CD record's size and VALUE clauses, which the record's own bind
/// cannot answer (<see cref="CommunicationSectionBinder.CheckRecords"/>). It schedules the rule beside the other clause checks over the complete forest.
/// </summary>
internal sealed class CheckCommunicationRecordsPass : IBindPass
{
    /// <inheritdoc/>
    public string Name => "CheckCommunicationRecords";

    /// <inheritdoc/>
    public PassPhase Requires => PassPhase.OccursResolved;

    /// <inheritdoc/>
    public PassPhase Produces => PassPhase.OccursResolved;

    /// <inheritdoc/>
    public PassViews Views => PassViews.None;

    /// <inheritdoc/>
    public void Run(DataBinder binder) => binder.Communication.CheckRecords();
}
