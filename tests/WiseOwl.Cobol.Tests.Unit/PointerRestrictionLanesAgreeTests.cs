// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ ONE ANSWER TO "THE SAME RESTRICTION" FOR TWO CLASS POINTERS, ASKED BY EVERY LANE (kb/Work PB3162, PB3686). A
/// restricted pointer's TO phrase is its USAGE clause's restriction (ISO §14.8.3.3 "the same … USAGE clauses";
/// §14.8.2.3.2 "both shall be restricted and of the same type"; §14.9.39.3 SR19 / SR20 / SR22), and its type is its
/// prototype's SIGNATURE (§13.18.60.4 GR25 / GR26). A prototype with no compile-time signature is resolved by the run
/// unit (§12.3.8.4 GR10 c)), so nothing can show two such pointers differ. The three description lanes — a SET of the
/// pair (<see cref="ActivationRelations.ReturningMatches"/>), the BY REFERENCE argument
/// (<see cref="ActivationRelations.CallArgumentViolation"/>) and the CALL RETURNING item
/// (<see cref="ActivationRelations.CallReturningViolation"/>) — must give one pair one verdict.
/// </summary>
public sealed class PointerRestrictionLanesAgreeTests
{
    private static ActivationDescription Pointer(string category, string clauses) => new()
    {
        Shape = ActivationShape.Elementary, Category = category, Usage = category, Clauses = clauses,
        ClausePhrases = [clauses],
    };

    private static string External(string prototype) => ActivationDescription.ExternalRestrictionPrefix + prototype;

    /// <summary>(restriction of the receiving pointer, restriction of the sending pointer, do the lanes admit the pair).
    /// An unrestricted pointer never matches a restricted one in the argument and returning lanes ("both shall be
    /// restricted", "the same USAGE clauses"); the SET lane admits the unrestricted RECEIVER of a program-pointer
    /// (SR22's "restricted program-pointer" is the receiver's), so that pair is checked apart.</summary>
    public static TheoryData<string, string, bool> RestrictedPairs => new()
    {
        { "TO sig-1", "TO sig-1", true },                       // one signature class
        { "TO sig-1", "TO sig-2", false },                      // two resolved signatures that differ
        { External("X3686A"), External("X3686B"), true },        // two unresolved prototypes
        { External("X3686A"), External("X3686A"), true },
        { External("X3686A"), "TO sig-1", true },                // one unresolved, one resolved
        { "TO sig-1", External("X3686B"), true },
    };

    [Theory]
    [MemberData(nameof(RestrictedPairs))]
    public void ProgramPointerLanesGiveOnePairOneVerdict(string receivingClauses, string sendingClauses, bool admitted)
    {
        ActivationDescription receiving = Pointer(ActivationCategory.ProgramPointer, receivingClauses);
        ActivationDescription sending = Pointer(ActivationCategory.ProgramPointer, sendingClauses);

        bool set = ActivationRelations.ReturningMatches(receiving, sending);
        bool byReference = ActivationRelations.CallArgumentViolation(CobolPassMode.Reference, sending, receiving, true) is null;
        bool returning = ActivationRelations.CallReturningViolation(receiving, sending) is null;

        Assert.Equal(admitted, set);
        Assert.Equal(admitted, byReference);
        Assert.Equal(admitted, returning);
    }

    [Theory]
    [MemberData(nameof(RestrictedPairs))]
    public void FunctionPointerLanesGiveOnePairOneVerdict(string receivingClauses, string sendingClauses, bool admitted)
    {
        ActivationDescription receiving = Pointer(ActivationCategory.FunctionPointer, receivingClauses);
        ActivationDescription sending = Pointer(ActivationCategory.FunctionPointer, sendingClauses);

        Assert.Equal(admitted, ActivationRelations.ReturningMatches(receiving, sending));
        Assert.Equal(admitted, ActivationRelations.CallArgumentViolation(CobolPassMode.Reference, sending, receiving, true) is null);
        Assert.Equal(admitted, ActivationRelations.CallReturningViolation(receiving, sending) is null);
    }

    [Fact]
    public void AnUnrestrictedPointerNeverMatchesARestrictedOneInTheReferenceAndReturningLanes()
    {
        foreach (string restriction in new[] { "TO sig-1", External("X3686A") })
        {
            ActivationDescription r = Pointer(ActivationCategory.ProgramPointer, restriction);
            ActivationDescription u = Pointer(ActivationCategory.ProgramPointer, ActivationDescription.Unrestricted);
            foreach ((ActivationDescription receiving, ActivationDescription sending) in new[] { (r, u), (u, r) })
            {
                Assert.NotNull(ActivationRelations.CallArgumentViolation(CobolPassMode.Reference, sending, receiving, true));
                Assert.NotNull(ActivationRelations.CallReturningViolation(receiving, sending));
            }
        }
    }

    [Fact]
    public void ADataPointersRestrictionIsATypeIdentityAndIsNeverExternal()
    {
        ActivationDescription a = Pointer(ActivationCategory.DataPointer, "TO type-a");
        ActivationDescription b = Pointer(ActivationCategory.DataPointer, "TO type-b");
        Assert.NotNull(ActivationRelations.CallReturningViolation(a, b));
        Assert.Null(ActivationRelations.CallReturningViolation(a, a));
    }

    [Fact]
    public void TwoPointersOfDifferentCategoriesNeverConform()
    {
        ActivationDescription program = Pointer(ActivationCategory.ProgramPointer, External("X3686A"));
        ActivationDescription function = Pointer(ActivationCategory.FunctionPointer, External("X3686A"));
        Assert.NotNull(ActivationRelations.CallReturningViolation(program, function));
    }
}
