// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ TWO RELATIONS OVER RESTRICTED CLASS POINTERS, EACH WHERE THE STANDARD ASKS IT (kb/Work PB4107, overturning the
/// one-verdict reading of PB3162 / PB3686).
/// <list type="bullet">
/// <item>The SET relation — §14.9.39.3 SR19 ("restricted to the same type"), SR20 / SR22 ("the same signature") — over
/// what each pointer may CONTAIN (<see cref="ActivationDescription.ContentRestriction"/>: a prototype pointer's signature
/// class), with the unknown-signature stance for a prototype only the run unit resolves (§12.3.8.4 GR10 c)): a SET of the
/// pair (<see cref="ActivationRelations.ReturningMatches"/>, §9.3.6 rule 6) and a BY CONTENT argument (§14.8.2.3.3).</item>
/// <item>The identity rules — §14.8.2.3.2's "both shall be restricted and of the same type" (§8.5.3.1: the TYPE clause's
/// declaration, <see cref="ActivationDescription.PointerType"/>, and its USAGE clause) for a BY REFERENCE argument, and
/// §14.8.3.3's "the same ... USAGE clauses" for a returning item — over the TO phrase as it NAMES its prototype
/// (<see cref="ActivationDescription.Clauses"/>), never its signature.</item>
/// </list>
/// So two pointers restricted to two prototypes of one signature, or alike under two type-names, are admitted by the
/// first and refused by the second — the standard's split, as an object reference of a subclass is.
/// </summary>
public sealed class PointerRestrictionRelationsTests
{
    private static ActivationDescription Pointer(string category, string named, string content, string? type = null) => new()
    {
        Shape = ActivationShape.Elementary, Category = category, Usage = category, Clauses = named,
        ContentRestriction = content, PointerType = type, ClausePhrases = [named],
    };

    private static string External(string prototype) => ActivationDescription.ExternalRestrictionPrefix + prototype;

    private static bool Set(ActivationDescription receiving, ActivationDescription sending) =>
        ActivationRelations.ReturningMatches(receiving, sending);

    private static bool ByReference(ActivationDescription formal, ActivationDescription argument) =>
        ActivationRelations.CallArgumentViolation(CobolPassMode.Reference, argument, formal, specifiedProgram: true) is null;

    private static bool ByContent(ActivationDescription formal, ActivationDescription argument) =>
        ActivationRelations.CallArgumentViolation(CobolPassMode.Content, argument, formal, specifiedProgram: true) is null;

    private static bool Returning(ActivationDescription receiving, ActivationDescription sending) =>
        ActivationRelations.CallReturningViolation(receiving, sending) is null;

    /// <summary>(receiving / formal pointer, sending / argument pointer, does the SET admit, do the identity rules admit).
    /// Each pointer is (TO phrase, signature class or external identity, type).</summary>
    public static TheoryData<string, string, string?, string, string, string?, bool, bool> Pairs => new()
    {
        // One prototype, one type: every relation admits.
        { "TO p1", "SIG s", "ta", "TO p1", "SIG s", "ta", true, true },
        // Two prototypes of ONE signature: the SET admits, the identity rules refuse (§9.3.6 3 e), §14.8.3.3).
        { "TO p2", "SIG s", "tb", "TO p1", "SIG s", "ta", true, false },
        // Two prototypes of two signatures: neither admits.
        { "TO p2", "SIG t", "tb", "TO p1", "SIG s", "ta", false, false },
        // Two prototypes only the run unit resolves: the SET's stance admits; the names still differ.
        { "TO x2", External("X2"), "tb", "TO x1", External("X1"), "ta", true, false },
        { "TO x1", External("X1"), "ta", "TO x1", "SIG s", "ta", true, true },
    };

    [Theory]
    [MemberData(nameof(Pairs))]
    public void ProgramPointers_SetAsksTheSignature_TheIdentityRulesAskTheName(string rNamed, string rContent, string? rType,
        string sNamed, string sContent, string? sType, bool setAdmits, bool identityAdmits)
    {
        foreach (string category in new[] { ActivationCategory.ProgramPointer, ActivationCategory.FunctionPointer })
        {
            var receiving = Pointer(category, rNamed, rContent, rType);
            var sending = Pointer(category, sNamed, sContent, sType);
            Assert.Equal(setAdmits, Set(receiving, sending));
            Assert.Equal(setAdmits, ByContent(receiving, sending));
            Assert.Equal(identityAdmits, Returning(receiving, sending));
            Assert.Equal(identityAdmits, ByReference(receiving, sending));
        }
    }

    [Fact]
    public void ByReference_TwoTypeNamesRestrictingAlike_AreNotOfTheSameType()
    {
        // §8.5.3.1: the same USAGE clause under two type-names is two types, so BY REFERENCE refuses, while the
        // returning item (the same USAGE clauses) and the SET (the same signature) admit.
        var formal = Pointer(ActivationCategory.ProgramPointer, "TO p1", "SIG s", "tb");
        var argument = Pointer(ActivationCategory.ProgramPointer, "TO p1", "SIG s", "tc");
        Assert.False(ByReference(formal, argument));
        Assert.Contains("not of the same type", ActivationRelations.CallArgumentViolation(
            CobolPassMode.Reference, argument, formal, specifiedProgram: true));
        Assert.True(Returning(formal, argument));
        Assert.True(Set(formal, argument));
        // A method §9.3.6 bound asks it too (3 e) proved only the USAGE clause).
        Assert.Null(ActivationRelations.MatchViolation(argument, formal));
        Assert.NotNull(ActivationRelations.ParameterViolation(argument, formal));
    }

    [Fact]
    public void ADataPointer_IsComparedByItsTypeAsWellAsItsRestriction()
    {
        var formal = Pointer(ActivationCategory.DataPointer, "TO g", "TO g", "tb");
        var sameType = Pointer(ActivationCategory.DataPointer, "TO g", "TO g", "tb");
        var otherType = Pointer(ActivationCategory.DataPointer, "TO g", "TO g", "ta");
        var otherTarget = Pointer(ActivationCategory.DataPointer, "TO h", "TO h", "tb");
        Assert.True(ByReference(formal, sameType));
        Assert.False(ByReference(formal, otherType));
        Assert.False(ByReference(formal, otherTarget));
        Assert.True(Set(formal, otherType));
        Assert.False(Set(formal, otherTarget));
    }

    [Fact]
    public void AnUnrestrictedPointerNeverMatchesARestrictedOneInTheIdentityRules()
    {
        var u = Pointer(ActivationCategory.ProgramPointer, ActivationDescription.Unrestricted, ActivationDescription.Unrestricted);
        foreach (var r in new[] { Pointer(ActivationCategory.ProgramPointer, "TO p1", "SIG s", "ta"),
                     Pointer(ActivationCategory.ProgramPointer, "TO x1", External("X1"), "ta") })
            foreach (var (receiving, sending) in new[] { (r, u), (u, r) })
            {
                Assert.False(ByReference(receiving, sending));
                Assert.False(Returning(receiving, sending));
            }
        // SR22's "restricted program-pointer" is the receiver's: an unrestricted receiver takes any program-pointer.
        Assert.True(Set(u, Pointer(ActivationCategory.ProgramPointer, "TO p1", "SIG s", "ta")));
    }

    [Fact]
    public void TwoUntypedFunctionPointers_AreOfTheSameTypeWhenTheirUsageClausesAre()
    {
        var a = Pointer(ActivationCategory.FunctionPointer, "TO f1", "SIG s");
        var b = Pointer(ActivationCategory.FunctionPointer, "TO f1", "SIG s");
        var typed = Pointer(ActivationCategory.FunctionPointer, "TO f1", "SIG s", "tf");
        Assert.True(ByReference(a, b));
        Assert.False(ByReference(a, typed));   // §8.5.3.1 defines "the same type" for TYPED items only
    }

    [Fact]
    public void TwoPointersOfDifferentCategoriesNeverConform()
    {
        var program = Pointer(ActivationCategory.ProgramPointer, "TO x1", External("X1"));
        var function = Pointer(ActivationCategory.FunctionPointer, "TO x1", External("X1"));
        Assert.False(Returning(program, function));
        Assert.False(ByReference(program, function));
        Assert.False(Set(program, function));
    }
}
