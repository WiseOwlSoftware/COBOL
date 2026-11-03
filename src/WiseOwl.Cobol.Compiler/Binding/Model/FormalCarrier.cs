// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Binding.Model;

/// <summary>
/// ⛔ THE CARRIER OF A CARRIER-RESIDENT FORMAL PARAMETER, AS A FACT (<see cref="DataItem.ResidentCarrier"/>; kb/Work
/// PB4320, PB4380). ISO §14.2.3 GR8: "the activated runtime element operates as if the formal parameter occupies the same
/// storage area as the argument", so such a formal has no storage of its own and every access reaches the argument through
/// a <c>ManagedPointer&lt;T&gt;</c> carrier. Where that carrier lives is the one thing the two arms differ in: a PROGRAM's
/// carrier is a member of its class, which a GLOBAL bridge and the activation entry also name; a METHOD's is a local of
/// the method (DESIGN-backend-abstraction §1.2: a local is referenced by its identity, never its name). Each arm makes its
/// own access-path root (<see cref="Root"/>), so <c>PlaceBuilder.AddRoot</c> asks the carrier and never the arm.
/// </summary>
public abstract record FormalCarrier
{
    private FormalCarrier() { }

    /// <summary>The root of every access path through the carrier, carrying the formal's *-ARG-OMITTED
    /// <paramref name="guard"/> (kb/Work PB971); the next step is the storage the carrier designates,
    /// <see cref="CarrierValueSegment"/>.</summary>
    public abstract RootSegment Root(OmittedFormalGuard? guard);

    /// <summary>The PROGRAM arm: the program class's carrier member, <paramref name="MemberClrName"/>
    /// (<c>LinkageFormal.CarrierField</c>, set by <c>LinkageBinder.CallBindLinkage</c>).</summary>
    public sealed record Member(string MemberClrName) : FormalCarrier
    {
        /// <inheritdoc/>
        public override RootSegment Root(OmittedFormalGuard? guard) => new RootFieldSegment(MemberClrName, guard);
    }

    /// <summary>The METHOD arm: the method's carrier local, <paramref name="Carrier"/> (<c>OoFormal.Carrier</c>, a
    /// <see cref="StatementLocalRole.MethodFormalCarrier"/> local set by <c>DataBinder.OoBindMethodData</c>).</summary>
    public sealed record Local(MethodLocal Carrier) : FormalCarrier
    {
        /// <inheritdoc/>
        public override RootSegment Root(OmittedFormalGuard? guard) => new LocalRootSegment(Carrier, guard);
    }
}
