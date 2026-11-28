// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Compiler.Binding.Model;

/// <summary>⛔ THE CATALOG FACTS OF ONE FUNCTION CALL THAT DECIDE WHICH CARRIER ITS VALUE ARRIVES ON (kb/Work PB5891 review R5-2), as data the
/// bound layer reads: the bind-time carrier prediction (<c>Binding.Bound.FunctionValueLane</c>) and the lane of a numeric node
/// (<c>NumericLanes.LaneOf</c>) live in the bound layer, which may not read the binder's catalog (<c>IntrinsicSig</c>) or argument schemas
/// (<c>IntrinsicArgumentRules.Verified</c>). The call's facts are decided once, with its type, when the call is bound
/// (<c>BoundIntrinsicCall.CarrierFacts</c>, from <c>IntrinsicResultType.CarrierFacts</c>).</summary>
/// <param name="Name">The function's name (the key of the carrier rules, <c>IntrinsicCarrierRules</c>, and of the native SDIDI bodies,
/// <c>FunctionValueLane</c>; kb/Work PB2627).</param>
/// <param name="Float">Is the function of the §2023:15.4.1 floating-math family?</param>
/// <param name="IntegerResult">Is the call's resolved type Integer (§2023:15.2 GR5)?</param>
/// <param name="IntegerPositions">For each argument written, is its position a §2023:15.3 type 6 (integer) position?</param>
/// <param name="ArgumentRunAllNumeric">Are all the positions of the argument run, as written, numeric-only?</param>
public sealed record IntrinsicCarrierFacts(string Name, bool Float, bool IntegerResult,
    IReadOnlyList<bool> IntegerPositions, bool ArgumentRunAllNumeric)
{
    /// <summary>Is argument <paramref name="position"/> at a type 6 (integer) position?</summary>
    public bool IsIntegerPosition(int position) => position < IntegerPositions.Count && IntegerPositions[position];

    /// <summary>Two calls' facts are equal when every fact is, the positions compared element by element (a list compares by instance,
    /// and two binds of one call build two lists): <c>IntrinsicArgumentRules.SameTerm</c> compares two calls as records.</summary>
    public bool Equals(IntrinsicCarrierFacts? other) => other is not null && Name == other.Name
        && Float == other.Float && IntegerResult == other.IntegerResult && ArgumentRunAllNumeric == other.ArgumentRunAllNumeric
        && IntegerPositions.SequenceEqual(other.IntegerPositions);

    public override int GetHashCode() => HashCode.Combine(Name, Float, IntegerResult, ArgumentRunAllNumeric, IntegerPositions.Count);
}
