// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Compiler.Oo;
using WiseOwl.Cobol.Runtime;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ TWO RELATIONS OVER RESTRICTED CLASS POINTERS, EACH WHERE THE STANDARD ASKS IT (kb/Work PB4107, overturning the
/// one-verdict reading of PB3162 / PB3686).
/// <list type="bullet">
/// <item>The SET relation — §14.9.39.3 SR19 ("restricted to the same type"), SR20 / SR22 ("the same signature") — over
/// what each pointer may CONTAIN (<see cref="ActivationDescription.ContentRestriction"/>: a prototype pointer's signature
/// class; every prototype has one, the external repository entry's for §12.3.8.4 GR10 c), kb/Work PB2103): a SET of the
/// pair (<see cref="ActivationRelations.ReturningMatches"/>, §9.3.6 rule 6) and a BY CONTENT argument (§14.8.2.3.3).</item>
/// <item>The identity rules — "both shall be restricted and of the same type" (§14.8.2.3.2, read through §8.5.3.1: the
/// TYPE clause's declaration, <see cref="ActivationDescription.PointerType"/>, and its USAGE clause) for a BY REFERENCE argument, and
/// §14.8.3.3's "the same ... USAGE clauses" for a returning item — over the TO phrase as it NAMES its prototype
/// (<see cref="ActivationDescription.Clauses"/>), never its signature.</item>
/// </list>
/// So two pointers restricted to two prototypes of one signature, or alike under two type-names, are admitted by the
/// first and refused by the second — the standard's split, as an object reference of a subclass is. The pair-mode
/// identity of §9.3.8.2.3 rules 3 and 6 (<see cref="ActivationRelations.SignatureDifference"/>) is a second identity rule:
/// it asks the TO phrase's name too (kb/Work PB4442).
/// </summary>
public sealed class PointerRestrictionRelationsTests
{
    private static ActivationDescription Pointer(string category, string named, string content, string? type = null) => new()
    {
        Shape = ActivationShape.Elementary, Category = category, Usage = category, Clauses = named,
        ContentRestriction = content, PointerType = type, ClausePhrases = [named],
    };

    private static bool Set(ActivationDescription receiving, ActivationDescription sending) =>
        ActivationRelations.ReturningMatches(receiving, sending);

    private static bool ByReference(ActivationDescription formal, ActivationDescription argument) =>
        ActivationRelations.CallArgumentViolation(CobolPassMode.Reference, argument, formal, specifiedProgram: true) is null;

    private static bool ByContent(ActivationDescription formal, ActivationDescription argument) =>
        ActivationRelations.CallArgumentViolation(CobolPassMode.Content, argument, formal, specifiedProgram: true) is null;

    private static bool Returning(ActivationDescription receiving, ActivationDescription sending) =>
        ActivationRelations.CallReturningViolation(receiving, sending) is null;

    /// <summary>(receiving / formal pointer, sending / argument pointer, does the SET admit, do the identity rules admit).
    /// Each pointer is (TO phrase, signature class, type).</summary>
    public static TheoryData<string, string, string?, string, string, string?, bool, bool> Pairs => new()
    {
        // One prototype, one type: every relation admits.
        { "TO p1", "SIG s", "ta", "TO p1", "SIG s", "ta", true, true },
        // Two prototypes of ONE signature: the SET admits, the identity rules refuse (§9.3.6 3 e), §14.8.3.3).
        { "TO p2", "SIG s", "tb", "TO p1", "SIG s", "ta", true, false },
        // Two prototypes of two signatures: neither admits.
        { "TO p2", "SIG t", "tb", "TO p1", "SIG s", "ta", false, false },
        // One prototype NAME whose signature two compilations saw differently (a callee rebuilt between them): one type
        // declaration is one type in both (the real builder's PointerType is the same text on each side,
        // TheTypeOfATypedRestrictedPointer_IsTheSameText_WhateverItsPrototypesSignature), and the SET refuses the pair —
        // there is no "unknown signature" that conforms to every other (kb/Work PB2103 deleted it).
        { "TO x1", "SIG t", "ta", "TO x1", "SIG s", "ta", false, true },
    };

    private const string ResolvingGroup = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. P4107X IS PROTOTYPE.
        DATA DIVISION.
        LINKAGE SECTION.
        01 L-Y PIC 9(4).
        PROCEDURE DIVISION USING L-Y.
        END PROGRAM P4107X.

        """;

    private const string Holder = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. H4107X.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            PROGRAM P4107X.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 TB TYPEDEF USAGE PROGRAM-POINTER TO P4107X.
        01 TG TYPEDEF STRONG.
           05 M TYPE TB.
        01 PB TYPE TB.
        01 GB TYPE TG.
        PROCEDURE DIVISION.
            GOBACK.
        END PROGRAM H4107X.
        """;

    /// <summary>⛔ The run-time "of the same type" compares two compilations' §8.5.3.1 keys (kb/Work PB4107, branch review
    /// R1-1): a key built from a prototype's signature class differs between two compilations that saw P4107X with two
    /// signatures (a callee rebuilt between them), and would refuse one type declaration against itself. The key reads the
    /// TO phrase by the prototype it names, so both compilations spell the pointer's type and the strong group's type with
    /// one string, while what the pointer may CONTAIN differs as the SET relation needs it to.</summary>
    [Fact]
    public void TheTypeOfATypedRestrictedPointer_IsTheSameText_WhateverItsPrototypesSignature()
    {
        (ActivationDescription Pointer, ActivationDescription Group) Describe(string source)
        {
            var roots = RepositoryCodecTests.Bind(source, 2002).Units.Single(u => u.Name == "H4107X").Data.WorkingStorageRoots;
            return (ActivationDescriptions.Of(roots.Single(r => r.CobolName == "PB"), classes: null)!,
                ActivationDescriptions.Of(roots.Single(r => r.CobolName == "GB"), classes: null)!);
        }

        var resolved = Describe(ResolvingGroup + Holder);
        var external = Describe(ResolvingGroup.Replace("PIC 9(4)", "PIC X(2)", StringComparison.Ordinal) + Holder);
        Assert.NotNull(resolved.Pointer.PointerType);
        Assert.Equal(resolved.Pointer.PointerType, external.Pointer.PointerType);
        Assert.Equal(resolved.Pointer.Clauses, external.Pointer.Clauses);
        Assert.NotNull(resolved.Group.StrongType);
        Assert.Equal(resolved.Group.StrongType, external.Group.StrongType);
        Assert.StartsWith(PrototypeSignatures.CanonicalPrefix, resolved.Pointer.ContentRestriction, StringComparison.Ordinal);
        Assert.StartsWith(PrototypeSignatures.CanonicalPrefix, external.Pointer.ContentRestriction, StringComparison.Ordinal);
        Assert.NotEqual(resolved.Pointer.ContentRestriction, external.Pointer.ContentRestriction);
        Assert.False(Set(resolved.Pointer, external.Pointer));
        Assert.True(ByReference(resolved.Pointer, external.Pointer));
        Assert.True(ByReference(external.Pointer, resolved.Pointer));
    }

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
                     Pointer(ActivationCategory.ProgramPointer, "TO x1", "SIG t") })
            foreach (var (receiving, sending) in new[] { (r, u), (u, r) })
            {
                Assert.False(ByReference(receiving, sending));
                Assert.False(Returning(receiving, sending));
            }
        // SR22's "restricted program-pointer" is the receiver's: an unrestricted receiver takes any program-pointer.
        Assert.True(Set(u, Pointer(ActivationCategory.ProgramPointer, "TO p1", "SIG s", "ta")));
    }

    [Fact]
    public void AnUntypedRestrictedPointer_IsOfTheTypeItsUsageClauseStates()
    {
        // §8.5.3.1 defines "the same type" for TYPED items only: an untyped FUNCTION-POINTER TO entry, or the unique item an
        // address-identifier creates (which INVOKE passes BY REFERENCE, §14.9.23.3 SR9), is compared by its USAGE clause.
        var untyped = Pointer(ActivationCategory.FunctionPointer, "TO f1", "SIG s");
        var alsoUntyped = Pointer(ActivationCategory.FunctionPointer, "TO f1", "SIG s");
        var typed = Pointer(ActivationCategory.FunctionPointer, "TO f1", "SIG s", "tf");
        var otherName = Pointer(ActivationCategory.FunctionPointer, "TO f2", "SIG s", "tf");
        Assert.True(ByReference(untyped, alsoUntyped));
        Assert.True(ByReference(typed, untyped));
        Assert.False(ByReference(otherName, untyped));
    }

    [Fact]
    public void TwoPointersOfDifferentCategoriesNeverConform()
    {
        var program = Pointer(ActivationCategory.ProgramPointer, "TO x1", "SIG s");
        var function = Pointer(ActivationCategory.FunctionPointer, "TO x1", "SIG s");
        Assert.False(Returning(program, function));
        Assert.False(ByReference(program, function));
        Assert.False(Set(program, function));
        Assert.NotNull(ActivationRelations.SignatureDifference(program, function));
        Assert.NotNull(ActivationRelations.SignatureDifference(program, function, pointersBySignature: true));
    }

    /// <summary>⛔ THE PAIR-MODE IDENTITY ASKS THE TO PHRASE'S NAME (kb/Work PB4442). §9.3.8.2.3 rules 3 and 6 — an override,
    /// an IMPLEMENTS pair, an interface pair, a property accessor pair — ask "the same ... USAGE clauses", so two
    /// program-pointers restricted to two prototypes of ONE signature are two descriptions, as the same pair is as members of
    /// a strongly-typed group (rule 7). Only the signature graph (<c>pointersBySignature</c>, <c>PrototypeSignatures.Same</c>)
    /// asks what each may contain, and it merges them.</summary>
    [Theory]
    [InlineData(ActivationCategory.ProgramPointer)]
    [InlineData(ActivationCategory.FunctionPointer)]
    public void PairMode_TwoPrototypesOfOneSignature_AreTwoDescriptions_TheSignatureGraphMergesThem(string category)
    {
        var q1 = Pointer(category, "TO q1", "SIG s");
        var q2 = Pointer(category, "TO q2", "SIG s");
        var other = Pointer(category, "TO q3", "SIG t");
        Assert.NotNull(ActivationRelations.SignatureDifference(q1, q2));
        Assert.NotNull(ActivationRelations.SignatureDifference(q2, q1));
        Assert.Null(ActivationRelations.SignatureDifference(q1, q1 with { }));
        Assert.Null(ActivationRelations.SignatureDifference(q1, q2, pointersBySignature: true));
        Assert.NotNull(ActivationRelations.SignatureDifference(q1, other, pointersBySignature: true));
        // An unrestricted pointer is a third description in both.
        var unrestricted = Pointer(category, ActivationDescription.Unrestricted, ActivationDescription.Unrestricted);
        Assert.NotNull(ActivationRelations.SignatureDifference(q1, unrestricted));
        Assert.NotNull(ActivationRelations.SignatureDifference(q1, unrestricted, pointersBySignature: true));
    }
}
