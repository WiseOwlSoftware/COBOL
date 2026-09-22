// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.CodeGen;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A LINKAGE FORMAL CROSSES AS A CHARACTER IMAGE ONLY WHEN ITS OWN STORAGE *IS* A C# STRING (kb/Work PB663).
/// </summary>
/// <remarks>
/// <para>
/// <c>ProgramEmitter</c>'s formal loop used to carry a <c>bool isNum</c>: numeric on one side, "everything else
/// as a space-filled <c>ManagedPointer&lt;string&gt;</c>" on the other. The model has FOUR crossing forms, not
/// two — the native cell, the character image, the §8.5.1.12 variable-length carrier, and the MANAGED SLOT of a
/// class-pointer or class-object-reference item — and the fourth had no arm at all. The consequence was not a
/// wrong value: <c>01 L-P USAGE POINTER.</c> + <c>PROCEDURE DIVISION USING L-P.</c> emitted
/// <c>ManagedPointer&lt;string&gt; __lnkp0 = … new string(' ', 1)</c>, and the first reference to the formal was
/// a Roslyn <c>CS1503: cannot convert from 'string' to 'ManagedPointer?'</c> — the backend crashed on conforming
/// source.
/// </para>
/// <para>
/// The invariant below is DERIVED, not enumerated: it walks <see cref="PicCategory"/> itself and asserts that a
/// category classified onto the character image is one whose <c>DataItem.ElementType</c> is literally
/// <c>"string"</c>. A class the data model gives its own carrier and the crossing dispatch does not name is
/// therefore RED on the day the category is added, rather than silently space-filled.
/// </para>
/// <para>
/// PROVEN TO FAIL before being trusted: restoring the old classification (returning
/// <see cref="CallCrossing.Text"/> for a <see cref="PicCategory.Pointer"/> resident formal) makes the Pointer,
/// ProgramPointer, FunctionPointer and ObjectReference theory cases red on BOTH entry points, and the end-to-end
/// case red with the original CS1503.
/// </para>
/// </remarks>
public sealed class LinkageCarrierDriftTests : CobolNetTestBase
{
    /// <summary>Every <see cref="PicCategory"/>, so the population is the model's and not a copy of it.</summary>
    public static TheoryData<PicCategory> EveryCategory()
    {
        var data = new TheoryData<PicCategory>();
        foreach (var c in Enum.GetValues<PicCategory>()) data.Add(c);
        return data;
    }

    /// <summary>A representative elementary item of <paramref name="category"/> — a non-float usage throughout,
    /// so every category below is a LEGAL carrier-resident formal (residency demands a childless elementary
    /// item whose profile is not floating-point) and the two entry points see the same population.</summary>
    private static DataItem ItemOf(PicCategory category) => new()
    {
        Level = 1,
        CsName = "L_X",
        CobolName = "L-X",
        Uid = 7,
        Pic = category is PicCategory.Group ? null : new PicInfo(category, Usage.Display, 4, 4, 0, false),
    };

    private static LinkageFormal FormalOf(DataItem item, bool resident) =>
        new(item, 0, "__lnkp0", resident);

    /// <summary>THE INVARIANT, on the CARRIER-RESIDENT entry point — the arm the defect lived in (a pointer
    /// formal IS resident: elementary, childless, not redefined, not floating-point).</summary>
    [Theory]
    [MemberData(nameof(EveryCategory))]
    public void AResidentFormalCrossesAsAnImageOnlyWhenItsStorageIsAString(PicCategory category)
    {
        var item = ItemOf(category);
        if (item.Pic is null) return;   // a group is never carrier-resident (residency demands an elementary item)
        var formal = FormalOf(item, resident: true);

        var crossing = ProgramEmitter.FormalCrossing(formal, place: null);
        string carrier = ProgramEmitter.FormalCarrierType(formal, crossing);

        Assert.Equal(SlotWindow.CarriedBySlot(item), crossing is CallCrossing.Managed);
        if (crossing is CallCrossing.Text)
            Assert.Equal("string", item.ElementType);
        else
            Assert.Equal(item.ElementType, carrier);
    }

    /// <summary>THE SAME INVARIANT on the ROUND-TRIP entry point — the OTHER arm of the same dispatch (a group
    /// or redefined formal keeps a callee-local field and classifies from its <see cref="Place"/>). Flipping the
    /// axis the resident case holds fixed is the point: the defect's twin lived here, where a managed item
    /// classified NATIVE and asked <c>CobolArgAdapt.Num&lt;ManagedPointer&gt;</c> for a carrier whose generic
    /// constraint it cannot satisfy.</summary>
    [Theory]
    [MemberData(nameof(EveryCategory))]
    public void ARoundTripFormalCrossesAsAnImageOnlyWhenItsStorageIsAString(PicCategory category)
    {
        var item = ItemOf(category);
        if (item.Pic is null) return;   // a childless PIC-less item is neither group nor elementary — not a formal
        var formal = FormalOf(item, resident: false);
        var place = new MemberPlace(new AccessPath([new RootFieldSegment("L_X")]), item);

        var crossing = ProgramEmitter.FormalCrossing(formal, place);
        string carrier = ProgramEmitter.FormalCarrierType(formal, crossing);

        Assert.Equal(SlotWindow.CarriedBySlot(item), crossing is CallCrossing.Managed);
        if (crossing is CallCrossing.Text)
            Assert.Equal("string", item.ElementType);
        else if (crossing is CallCrossing.Native or CallCrossing.Managed)
            Assert.Equal(item.ElementType, carrier);
    }

    /// <summary>MEASURED, not deduced: the real compiler emits the formal's own carrier for each managed class,
    /// and the program runs. The CALL is <c>AS NESTED</c> because §14.9.4.3 SR10 bars a Format-1 CALL from
    /// passing a data item of class object or pointer BY REFERENCE at all — the callee side is reachable only
    /// through a Format-2 activation or a separately-compiled activator, which is precisely why the hole stayed
    /// open.</summary>
    [Theory]
    [InlineData("POINTER", "ManagedPointer")]
    [InlineData("PROGRAM-POINTER", "ProgramPointer")]
    [InlineData("OBJECT REFERENCE", "CobolObject?")]
    public void AManagedFormalIsDeclaredWithItsOwnCarrier(string usage, string carrier)
    {
        string src = $"""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. LCDMAIN.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 W-X USAGE {usage}.
                   PROCEDURE DIVISION.
                       CALL "LCDSUB" AS NESTED USING BY REFERENCE W-X
                       GOBACK.
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. LCDSUB.
                   DATA DIVISION.
                   LINKAGE SECTION.
                   01 L-X USAGE {usage}.
                   PROCEDURE DIVISION USING L-X.
                   SUBMAIN.
                       DISPLAY "IN SUB"
                       GOBACK.
                   END PROGRAM LCDSUB.
                   END PROGRAM LCDMAIN.

                   """;
        var (ok, stdout, detail) = CompileAndRun(src, dialectLevel: 2023);
        Assert.True(ok, detail);
        Assert.Equal("IN SUB", stdout);

        string generated = File.ReadAllText(Path.Combine(TempDir, "prog.g.cs"));
        Assert.Contains($"private ManagedPointer<{carrier}> __lnkp0", generated);
        Assert.DoesNotContain("ManagedPointer<string> __lnkp0", generated);
    }
}
