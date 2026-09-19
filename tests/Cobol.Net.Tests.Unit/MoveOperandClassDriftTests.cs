// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE TWO-ARM PIN FOR ISO §14.9.25.3 SR1 (kb/Work PB423).
///
/// <para>SR1 — <i>"The class of identifier-1 or identifier-2 shall not be index, message-tag, object, or
/// pointer"</i> — is ONE sentence about BOTH operand positions, and the compiler implemented it as two arms of
/// which only the sending one was ever widened. The receiving arm asked <c>t.Item.Pic is { Usage: Usage.Index }</c>
/// — one hand-written usage — under a comment asserting that the other three classes "cannot reach a bound
/// MOVE yet (their usages are compile-gated skeletons)". They had been live for several phases:
/// <c>MOVE P TO Y</c> over a <c>USAGE POINTER</c> sender printed <c>Y=[CobolNet]</c>, the CLR carrier's type
/// name, into an alphanumeric item, and <c>MOVE NULL TO P</c> reached Roslyn and surfaced
/// <c>CS0029</c> about generated C# in place of a COBOL diagnostic.</para>
///
/// <para><b>What this pins that a golden cannot.</b> The negatives prove the two shapes are refused today. This
/// asserts the PROPERTY that was lost: every usage §13.18.60.3 SR4 names — <i>"The INDEX, MESSAGE-TAG, OBJECT
/// REFERENCE, POINTER, FUNCTION-POINTER, and PROGRAM-POINTER phrases"</i>, exactly the six that produce SR1's
/// four classes (§8.5.2) — is refused in BOTH positions, and the two positions return the SAME text for the
/// same item. A usage going live can never again leave one arm stale, because
/// <c>ItemCategory.Sr4PhraseOf</c> drives the population instead of a hand-written list.</para>
/// </summary>
public sealed class MoveOperandClassDriftTests
{
    /// <summary>The six USAGE phrases ISO §13.18.60.3 SR4 names. Read from <c>ItemCategory.Sr4PhraseOf</c> — the
    /// compiler's ONE reader of that list — so this test cannot hold a seventh copy of it.</summary>
    public static TheoryData<Usage> Sr4Usages()
    {
        var data = new TheoryData<Usage>();
        foreach (Usage u in Enum.GetValues<Usage>())
            if (ItemCategory.Sr4PhraseOf(u) is not null) data.Add(u);
        return data;
    }

    private static DataItem ItemOf(Usage u)
    {
        var item = new DataItem { Level = 1, CobolName = "OP", CsName = "OP", OwnUsage = u };
        // The resolved PicInfo each usage carries once PictureAnalyzer has run. MESSAGE-TAG and, before
        // kb/Work PB452/PB817, FUNCTION-POINTER never gain one — the WRITTEN clause is all there is — so those
        // are modelled as OwnUsage alone, which is exactly the asymmetry ItemCategory.IsIndexMessageTagObjectOrPointer
        // exists to read.
        PicCategory? cat = u switch
        {
            Usage.Pointer => PicCategory.Pointer,
            Usage.ProgramPointer => PicCategory.ProgramPointer,
            Usage.FunctionPointer => PicCategory.FunctionPointer,
            Usage.ObjectReference => PicCategory.ObjectReference,
            Usage.Index => PicCategory.Numeric,
            _ => null,
        };
        if (cat is { } c)
            item.Pic = new PicInfo(c, u, Length: 8, Digits: 0, Scale: 0, Signed: false);
        return item;
    }

    /// <summary>⛔ BOTH POSITIONS, ONE ANSWER. A class SR1 excludes is refused whether the operand sends or
    /// receives, and with the same words — which the two entries can only manage by being one body. Fix one arm
    /// and not the other and this fails; it was verified to fail against the receiving arm as it stood, on the
    /// POINTER, PROGRAM-POINTER, FUNCTION-POINTER and OBJECT REFERENCE rows.</summary>
    [Theory]
    [MemberData(nameof(Sr4Usages))]
    public void EverySr4UsageIsRefusedInBothOperandPositions(Usage u)
    {
        DataItem item = ItemOf(u);
        if (!ItemCategory.IsIndexMessageTagObjectOrPointer(item))
            Assert.Fail($"USAGE {ItemCategory.Sr4PhraseOf(u)} is one of §13.18.60.3 SR4's six phrases, so the "
                + "§14.9.25.3 SR1 class population must contain it.");
        var place = new MemberPlace(new AccessPath([]), item);
        string? receiving = MoveTable16.ReceiverClassRefusal(place);
        string? sending = MoveTable16.SenderClassRefusal(new BoundFieldOperand(place));
        // A usage with no bound PicInfo (MESSAGE-TAG) cannot produce an operand at all — PictureAnalyzer refuses
        // the clause by name (COBOLNET1943) — so the class reader answers null and the screen fails OPEN, which
        // is the posture every class screen here takes. Say so rather than asserting a refusal that cannot exist.
        if (item.Pic is null)
        {
            Assert.Null(receiving);
            Assert.Null(sending);
            return;
        }
        Assert.NotNull(receiving);
        Assert.NotNull(sending);
        Assert.Equal(receiving, sending);
    }

    /// <summary>The complement (<c>feedback_measure_the_selectors_complement</c>): the screen must reject what
    /// the rule NAMES and nothing else. An ordinary alphanumeric, numeric, national or boolean operand passes in
    /// both positions, so a widening of the class list would show up here rather than in a corpus red.</summary>
    [Theory]
    [InlineData(PicCategory.Alphanumeric, Usage.Display)]
    [InlineData(PicCategory.Numeric, Usage.Display)]
    [InlineData(PicCategory.Numeric, Usage.Binary)]
    [InlineData(PicCategory.NumericEdited, Usage.Display)]
    [InlineData(PicCategory.National, Usage.National)]
    [InlineData(PicCategory.Boolean, Usage.Bit)]
    public void AnAdmittedClassPassesInBothOperandPositions(PicCategory cat, Usage usage)
    {
        var item = new DataItem { Level = 1, CobolName = "OK", CsName = "OK", OwnUsage = usage };
        item.Pic = new PicInfo(cat, usage, Length: 4, Digits: cat is PicCategory.Numeric ? 4 : 0,
            Scale: 0, Signed: false);
        var place = new MemberPlace(new AccessPath([]), item);
        Assert.Null(MoveTable16.ReceiverClassRefusal(place));
        Assert.Null(MoveTable16.SenderClassRefusal(new BoundFieldOperand(place)));
    }
}
