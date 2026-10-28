// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.CodeGen;
using WiseOwl.Cobol.Frontend.Diagnostics;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;
using CnFrontend = WiseOwl.Cobol.Frontend.Frontend;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A RESTRICTED POINTER IS TYPED BY ITS PROTOTYPE'S SIGNATURE, WHEREVER THE PROTOTYPE CAME FROM (ISO §13.18.60.4 GR25 and
/// GR26; DESIGN-external-repository §6.4 parts 1-3; kb/Work PB2100). <c>PrototypeSignatureClasses</c> keys its members by
/// ORIGIN AND NAME, admits decoded prototypes into the refinement (<c>Admit</c>: re-run from one block, group members
/// first, never an append) and spells a block a decoded member founds <c>DECODED k TO name</c>, so two blocks never share
/// an identity string. The fixture is five bound programs whose linkage items stand for five signatures (A and B are one
/// signature, C and D another, E a third), driven through the class directly, because no source yet reaches a decoded
/// prototype (the reader and the codec are the rest of the slice).
/// </summary>
public sealed class PrototypeSignatureClassesTests
{
    private const string Source = """
               IDENTIFICATION DIVISION.
               PROGRAM-ID. SIGA.
               DATA DIVISION.
               LINKAGE SECTION.
               01 X PIC 9(4).
               PROCEDURE DIVISION USING X.
                   GOBACK.
               END PROGRAM SIGA.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. SIGB.
               DATA DIVISION.
               LINKAGE SECTION.
               01 Y PIC 9(4).
               PROCEDURE DIVISION USING Y.
                   GOBACK.
               END PROGRAM SIGB.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. SIGC.
               DATA DIVISION.
               LINKAGE SECTION.
               01 X PIC X(2).
               PROCEDURE DIVISION USING X.
                   GOBACK.
               END PROGRAM SIGC.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. SIGD.
               DATA DIVISION.
               LINKAGE SECTION.
               01 Z PIC X(2).
               PROCEDURE DIVISION USING Z.
                   GOBACK.
               END PROGRAM SIGD.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. SIGE.
               DATA DIVISION.
               LINKAGE SECTION.
               01 X PIC S9(3).
               PROCEDURE DIVISION USING X.
                   GOBACK.
               END PROGRAM SIGE.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. HOLD.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01 P1 PIC X.
               01 P2 PIC X.
               01 P3 PIC X.
               01 P4 PIC X.
               01 P5 PIC X.
               01 P6 PIC X.
               PROCEDURE DIVISION.
                   GOBACK.
               END PROGRAM HOLD.
               """;

    private sealed record Fixture(CalleeSignature A, CalleeSignature B, CalleeSignature C, CalleeSignature D, CalleeSignature E,
        IReadOnlyList<DataItem> Holders);

    private static Fixture Bind()
    {
        string path = Path.Combine(Path.GetTempPath(), $"protoclasses_{Guid.NewGuid():N}.cob");
        File.WriteAllText(path, Source);
        try
        {
            var diags = new DiagnosticBag();
            var frontend = new CnFrontend { InitialFormat = InitialReferenceFormat.Auto, DialectLevel = 2023 };
            var tree = frontend.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            var emitter = new CSharpEmitter();
            var comp = emitter.Bind(tree!, new EditionContext(2023), frontend.Directives);
            CalleeSignature Of(string name)
            {
                var data = comp.Units.Single(u => u.Name == name).Data;
                return new CalleeSignature(data.LinkageFormals, data.LinkageReturning);
            }
            return new Fixture(Of("SIGA"), Of("SIGB"), Of("SIGC"), Of("SIGD"), Of("SIGE"),
                comp.Units.Single(u => u.Name == "HOLD").Data.WorkingStorageRoots);
        }
        finally { FileHarness.TryDelete(path); }
    }

    private static RestrictedPrototype Group(string name, CalleeSignature signature) => new(Function: false, name, signature);

    private static RestrictedPrototype Decoded(PrototypeOrigin origin, string name, CalleeSignature signature) =>
        new(Function: false, name, signature, origin);

    /// <summary>A pointer restricted to <paramref name="prototype"/>: a working-storage item standing in for it.</summary>
    private static DataItem Pointer(DataItem holder, RestrictedPrototype prototype)
    {
        holder.RestrictedPrototype = prototype;
        return holder;
    }

    [Fact]
    public void GroupPrototypes_AreClassifiedBySignature_AndAClassIsNamedByItsFirstMember()
    {
        var f = Bind();
        var classes = new PrototypeSignatureClasses();
        var pointers = new[]
        {
            Pointer(f.Holders[0], Group("P1", f.A)), Pointer(f.Holders[1], Group("P2", f.B)),
            Pointer(f.Holders[2], Group("Q1", f.C)), Pointer(f.Holders[3], Group("Q2", f.D)),
        };
        classes.Classify(pointers);
        Assert.Equal(["TO p1", "TO p1", "TO q1", "TO q1"], pointers.Select(p => p.PointerRestrictionIdentity!).ToArray());
    }

    [Fact]
    public void ADecodedPrototype_OfTheGroupsSignature_JoinsTheGroupsBlock_AndNoGroupIdentityMoves()
    {
        var f = Bind();
        var classes = new PrototypeSignatureClasses();
        var group = new[] { Pointer(f.Holders[0], Group("B", f.A)), Pointer(f.Holders[1], Group("Q", f.C)) };
        classes.Classify(group);
        string[] before = group.Select(p => p.PointerRestrictionIdentity!).ToArray();

        // A decoded B whose signature is the group B's: ONE type, not two (GR25) — the same block, not a block of its own.
        var decoded = Pointer(f.Holders[2], Decoded(new PrototypeOrigin("record 1"), "B", f.B));
        classes.Admit([decoded]);
        Assert.Equal("TO b", decoded.PointerRestrictionIdentity);
        Assert.Equal(before, group.Select(p => p.PointerRestrictionIdentity!).ToArray());
        Assert.True(PrototypeSignatures.SameType(group[0], decoded));
    }

    [Fact]
    public void ADecodedPrototype_NamedLikeAGroupPrototype_OfAnotherSignature_IsNotTheGroupsType()
    {
        var f = Bind();
        var classes = new PrototypeSignatureClasses();
        // The group's B founds its block in the first round (A and B differ); the decoded B founds one of its own.
        var groupA = Pointer(f.Holders[0], Group("A", f.A));
        var groupB = Pointer(f.Holders[1], Group("B", f.C));
        classes.Classify([groupA, groupB]);
        Assert.Equal("TO b", groupB.PointerRestrictionIdentity);

        var decoded = Pointer(f.Holders[2], Decoded(new PrototypeOrigin("record 1"), "B", f.E));
        classes.Admit([decoded]);
        Assert.Equal("DECODED 1 TO b", decoded.PointerRestrictionIdentity);
        Assert.Equal("TO b", groupB.PointerRestrictionIdentity);
        Assert.Equal("TO a", groupA.PointerRestrictionIdentity);
        Assert.False(PrototypeSignatures.SameType(groupB, decoded));
    }

    [Fact]
    public void TwoDecodedPrototypes_OfOneName_AndDifferentSignatures_GetTwoIdentities_AndOneRecordsPrototypeIsOneMember()
    {
        var f = Bind();
        var classes = new PrototypeSignatureClasses();
        var one = new PrototypeOrigin("record 1");
        var two = new PrototypeOrigin("record 2");
        var first = Pointer(f.Holders[0], Decoded(one, "X", f.A));
        var second = Pointer(f.Holders[1], Decoded(two, "X", f.E));
        var again = Pointer(f.Holders[2], Decoded(one, "x", f.A));   // the same record's X, named in another case: the same member
        classes.Admit([first, second, again]);
        Assert.Equal("DECODED 1 TO x", first.PointerRestrictionIdentity);
        Assert.Equal("DECODED 2 TO x", second.PointerRestrictionIdentity);
        Assert.Equal("DECODED 1 TO x", again.PointerRestrictionIdentity);
    }

    [Fact]
    public void AGroupPrototype_NamedLikeADecodedSpelling_NeverSharesAnIdentityWithADecodedBlock()
    {
        var f = Bind();
        var classes = new PrototypeSignatureClasses();
        // Externalized names may hold spaces and '#' (AS literals), so a spelling that only appended a marker to "TO <name>"
        // could be forged by a group prototype named like the decoded one (review R4-1).
        var forgedDecoded = Pointer(f.Holders[0], Group("DECODED 1 TO B", f.A));
        var forgedMarker = Pointer(f.Holders[1], Group("B #1", f.C));
        classes.Classify([forgedDecoded, forgedMarker]);
        var decoded = Pointer(f.Holders[2], Decoded(new PrototypeOrigin("record 1"), "B", f.E));
        classes.Admit([decoded]);
        string[] identities = [forgedDecoded.PointerRestrictionIdentity!, forgedMarker.PointerRestrictionIdentity!, decoded.PointerRestrictionIdentity!];
        Assert.Equal(["TO decoded 1 to b", "TO b #1", "DECODED 1 TO b"], identities);
        Assert.Equal(3, identities.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void AdmittingBeforeAnyGroupPointer_StillStartsFromGroupMembersFirst()
    {
        var f = Bind();
        var classes = new PrototypeSignatureClasses();
        var decoded = Pointer(f.Holders[0], Decoded(new PrototypeOrigin("record 1"), "D", f.A));
        classes.Admit([decoded]);
        Assert.Equal("DECODED 1 TO d", decoded.PointerRestrictionIdentity);
        // A group pointer of the same signature, classified afterwards, re-runs the refinement with the group member first:
        // the group's class is founded by the group member, and the decoded prototype joins it.
        var group = Pointer(f.Holders[1], Group("G", f.B));
        classes.Classify([group]);
        Assert.Equal("TO g", group.PointerRestrictionIdentity);
        Assert.Equal("TO g", decoded.PointerRestrictionIdentity);
    }

    [Fact]
    public void ADecodedPrototype_AskedForBeforeItIsAdmitted_IsADefectOfTheCaller_AndAdmitTakesNoGroupPointer()
    {
        var f = Bind();
        var classes = new PrototypeSignatureClasses();
        var stray = Decoded(new PrototypeOrigin("record 7"), "S", f.A);
        var thrown = Assert.Throws<InvalidOperationException>(() => classes.Identity(stray));
        Assert.Contains("record 7", thrown.Message, StringComparison.Ordinal);
        Assert.Throws<InvalidOperationException>(() => classes.Admit([Pointer(f.Holders[0], Group("G", f.A))]));
    }
}
