// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ THE BASED-ITEM SCREEN FOLLOWS THE DECLARATION'S SCOPE (kb/Work PB467). ISO §14.9.39.3 SR18
/// ("Data-name-1 shall be a based data item") and §14.9.3.3 SR1 ("The data item referenced by data-name-1 shall
/// be described with the BASED clause") are predicates over the item the reference IDENTIFIES, and inside a
/// method definition that is the method's own declaration: §11.7.4 GR5 — "If a given user-defined word is
/// defined in the data division of this method definition and in the data division of the containing object
/// definition, the use of that word in this method refers to the declaration in this method. The declaration in
/// the containing object definition is inaccessible to this method." §13.16.3 SR16 is what makes these programs
/// legal source at all: the BASED clause "may be specified only in data description entries in the linkage
/// section, in the working-storage section, and in the local-storage section" — all three of which a method
/// definition has.
///
/// <para><b>These tests MOVE ONE DECLARATION and assert the verdict follows it.</b> That is the shape the defect
/// had: <c>PtrResolveBased</c> resolved through the unit-wide <c>ByName</c> multimap, which cannot see a method,
/// so the verdict was wrong in BOTH directions — a legal method-local <c>01 MB BASED</c> refused, and a
/// method-local NON-based item that legally shadows an object-level BASED one accepted for rebasing. A lookup
/// that is merely INCOMPLETE fails one way; one that answers a DIFFERENT question fails both.</para>
///
/// <para>⚠ Every program here also draws <c>COBOLNET0899</c> — BASED data in a class definition's data divisions
/// is a named Phase-4b residue — so none of them compiles and the assertions are about the SR18 diagnostic
/// alone. That residue is exactly why the resolver fix is sequenced FIRST: when it lifts, these verdicts are the
/// visible behaviour of that landing.</para>
/// </summary>
public sealed class BasedItemScopeTests
{
    /// <summary>A class whose method GO1 runs <paramref name="body"/>, with <paramref name="objectData"/> in the
    /// object's WORKING-STORAGE and <paramref name="methodData"/> in the method's LOCAL-STORAGE.</summary>
    private static string Cls(string id, string objectData, string methodData, string body) => $$"""
        IDENTIFICATION DIVISION.
        CLASS-ID. {{id}}.
        IDENTIFICATION DIVISION.
        OBJECT.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 P1 USAGE POINTER.
        {{objectData}}
        PROCEDURE DIVISION.
        METHOD-ID. GO1.
        DATA DIVISION.
        LOCAL-STORAGE SECTION.
        {{methodData}}
        PROCEDURE DIVISION.
        MAIN.
        {{body}}
        END METHOD GO1.
        END OBJECT.
        END CLASS {{id}}.
        """;

    private const string Based = "01 MB BASED.\n           05 MBA PIC X(4).";
    private const string NotBased = "01 MB PIC X(4).";
    private const string Rebase = "    SET ADDRESS OF MB TO P1.";

    /// <summary>The control: the BASED item declared at OBJECT level, referenced from the method. SR18 is
    /// satisfied and no COBOLNET0869 is owed.</summary>
    [Fact]
    public void ObjectLevelBasedItem_SatisfiesSr18()
    {
        var (_, errors, _) = EditionHarness.CompileFull(Cls("PB467CA", Based, "01 FILLER-X PIC X.", Rebase), 2002);
        EditionHarness.AssertNoDiagnostic(errors, "COBOLNET0869");
    }

    /// <summary>THE SAME DECLARATION, MOVED into the method's LOCAL-STORAGE. §13.16.3 SR16 admits BASED there
    /// and §11.7.4 GR5 makes it the referenced item, so SR18 is satisfied exactly as at object level. This is
    /// the legal source the unit-wide lookup refused, because the method's own declaration was invisible to
    /// it.</summary>
    [Fact]
    public void MethodLocalBasedItem_SatisfiesSr18()
    {
        var (_, errors, _) = EditionHarness.CompileFull(Cls("PB467CB", "01 FILLER-Y PIC X.", Based, Rebase), 2002);
        EditionHarness.AssertNoDiagnostic(errors, "COBOLNET0869");
    }

    /// <summary>A method-local NON-based item legally shadowing an object-level BASED one of the same spelling.
    /// §11.7.4 GR5 makes the METHOD's declaration the referenced item and it is not BASED, so SR18 is violated.
    /// The unit-wide lookup saw the object-level BASED item — an item that is not the referenced item — and
    /// admitted the statement: illegal source accepted, the opposite direction of the same defect.</summary>
    [Fact]
    public void MethodLocalNonBasedItem_ShadowingABasedOne_ViolatesSr18()
    {
        var (_, errors, _) = EditionHarness.CompileFull(Cls("PB467CC", Based, NotBased, Rebase), 2002);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0869");
    }

    /// <summary>The negative control at object level: a non-BASED item with no shadowing anywhere still violates
    /// SR18. Without it, "the shadow case is refused" could be true for the wrong reason.</summary>
    [Fact]
    public void ObjectLevelNonBasedItem_ViolatesSr18()
    {
        var (_, errors, _) = EditionHarness.CompileFull(Cls("PB467CD", NotBased, "01 FILLER-Z PIC X.", Rebase), 2002);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0869");
    }

    /// <summary>ALLOCATE reaches the SAME helper, so §14.9.3.3 SR1 gets the same scoped verdict: the method's own
    /// BASED item is allocatable. A fix that repaired only SET ADDRESS OF would leave this arm wrong — the
    /// two-arm dispatch this repo produces most often.</summary>
    [Fact]
    public void Allocate_OfAMethodLocalBasedItem_SatisfiesSr1()
    {
        var (_, errors, _) = EditionHarness.CompileFull(
            Cls("PB467CE", "01 FILLER-W PIC X.", Based, "    ALLOCATE MB RETURNING P1."), 2002);
        EditionHarness.AssertNoDiagnostic(errors, "COBOLNET0869");
    }

    /// <summary>ALLOCATE of the method-local non-based shadow violates §14.9.3.3 SR1, the other arm.</summary>
    [Fact]
    public void Allocate_OfAMethodLocalNonBasedShadow_ViolatesSr1()
    {
        var (_, errors, _) = EditionHarness.CompileFull(
            Cls("PB467CF", Based, NotBased, "    ALLOCATE MB RETURNING P1."), 2002);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0869");
    }

    /// <summary>A name NO declaration in scope carries is an ISO §8.4.2.1 failure, not a category failure: the
    /// reference "identifies no resource", and saying "the operand shall be a BASED level-01/77 item" sends the
    /// reader hunting a BASED clause on a name that is not declared at all.</summary>
    [Fact]
    public void UndefinedOperand_IsAnUnidentifiedReference_NotACategoryError()
    {
        string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB467UND.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 P USAGE POINTER.
            PROCEDURE DIVISION.
            MAIN.
                SET ADDRESS OF NOSUCH TO P.
                STOP RUN.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1639");
        EditionHarness.AssertNoDiagnostic(errors, "COBOLNET0869");
    }
}
