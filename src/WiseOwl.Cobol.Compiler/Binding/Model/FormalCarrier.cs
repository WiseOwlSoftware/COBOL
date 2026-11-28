// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Compiler.Binding.Model;

/// <summary>
/// ⛔ THE CARRIER OF A CARRIER-RESIDENT FORMAL PARAMETER, AS A FACT (<see cref="DataItem.ResidentCarrier"/>; kb/Work
/// PB4320, PB4380). ISO §2023:14.2.3 GR8: "the activated runtime element operates as if the formal parameter occupies the same
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

    /// <summary>⛔ THE ONE NAME OF A FORMAL'S CARRIER AND AREA POINTER (kb/Work PB3213): Uid-keyed, never positional (kb/Work
    /// PB1009), for the program arm's carrier member (<c>LinkageFormal.CarrierField</c>) and the method arm's area pointer
    /// (<c>OoFormal.AreaPointerClrName</c>) alike.</summary>
    public static string AreaPointerName(DataItem item) => $"__lnk{item.Uid}";

    /// <summary>⛔ THE ONE AREA-FORMAL RULE (ISO §2023:14.2.3 GR8; kb/Work PB2087, PB3213): a formal that is not carrier-resident
    /// is an AREA formal when its class was claimed onto a cell whose implicit data-address pointer IS
    /// <see cref="AreaPointerName"/> (<c>PtrBindBasedAndAddressablesPass</c>), so the binder and the emitter cannot disagree
    /// about which formals are areas, in either arm.</summary>
    public static bool IsAreaFormal(DataItem item, bool carrierResident) =>
        !carrierResident && item.Class?.BasedPointerField == AreaPointerName(item);

    /// <summary>The PROGRAM arm: the program class's carrier member, <paramref name="MemberClrName"/>
    /// (<c>LinkageFormal.CarrierField</c>, set by <c>LinkageBinder.CallBindLinkage</c>).</summary>
    public sealed record Member(string MemberClrName) : FormalCarrier
    {
        /// <inheritdoc/>
        public override RootSegment Root(OmittedFormalGuard? guard) => new RootFieldSegment(MemberClrName, guard);
    }

    /// <summary>The METHOD arm: the method's carrier local, <paramref name="Carrier"/> (<c>OoFormal.Carrier</c>, a
    /// <see cref="StatementLocalRole.MethodFormalCarrier"/> local set by <c>OoDataBinder.BindMethodData</c>).</summary>
    public sealed record Local(MethodLocal Carrier) : FormalCarrier
    {
        /// <inheritdoc/>
        public override RootSegment Root(OmittedFormalGuard? guard) => new LocalRootSegment(Carrier, guard);
    }
}
