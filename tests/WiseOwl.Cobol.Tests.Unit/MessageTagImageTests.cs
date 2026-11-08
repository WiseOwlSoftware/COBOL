// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// The message-tag storage-image codec (kb/Work PB4838; docs/CONFORMANCE.md §7 DOC-A.1-212). ISO §2023:13.18.60.4 GR9 leaves a
/// message-tag item's representation to the implementor (cite.py --check 13.18.60.4 OK, General rules 9), so the token is the
/// documented determination: NULL is eight X"00" positions, any other tag is its identifier in the run unit, big-endian. The
/// conformance golden can only build NULL tags (only the message control system makes another), so the non-null half of the
/// round trip is pinned here. The tag read back is the SAME tag, the message serial included, although §2023:8.8.4.2.14's
/// equality ignores the serial: a reply addressed through the tag read back must reach the message it was sent for.
/// </summary>
public sealed class MessageTagImageTests
{
    private const string Mcs = "0123456789abcdef";

    [Fact]
    public void Null_IsTheZeroAddress_LikeEveryPointerCategory()
    {
        Assert.Equal(PointerImage.NullImage, MessageTagImage.Of(MessageTag.Null));
        Assert.True(MessageTagImage.TagOf(PointerImage.NullImage).IsNull);
    }

    [Fact]
    public void TagOf_IsTheInverseOfOf_InTheSameRunUnit()
    {
        var server = new MessageTag(Mcs, MessageParty.Server, "PAYROLL", 7);
        var requestor = new MessageTag(Mcs, MessageParty.Requestor, "run-unit-3", 7);
        string serverImage = MessageTagImage.Of(server);
        string requestorImage = MessageTagImage.Of(requestor);
        Assert.NotEqual(serverImage, requestorImage);
        Assert.Equal(serverImage, MessageTagImage.Of(server));                       // stable
        var serverBack = MessageTagImage.TagOf(serverImage);
        Assert.True(MessageTag.SameParty(server, serverBack));
        Assert.Equal(7, serverBack.Message);
        Assert.Equal(MessageParty.Requestor, MessageTagImage.TagOf(requestorImage).Party);
    }

    [Fact]
    public void TwoTagsOfOnePartyAndDifferentSerials_AreEqualButHaveDifferentTokens()
    {
        var a = new MessageTag(Mcs, MessageParty.Server, "PAYROLL", 1);
        var b = new MessageTag(Mcs, MessageParty.Server, "PAYROLL", 2);
        Assert.True(MessageTag.SameParty(a, b));                                      // §2023:8.8.4.2.14
        Assert.NotEqual(MessageTagImage.Of(a), MessageTagImage.Of(b));
        Assert.Equal(2, MessageTagImage.TagOf(MessageTagImage.Of(b)).Message);
    }

    [Fact]
    public void AnUnassignedToken_IsAnUnresolvedTag_NotNull_ThatKeepsItsImage()
    {
        const string image = "\0\0\0\0\0\u0001\u0002\u0003";
        var tag = MessageTagImage.TagOf(image);
        Assert.False(tag.IsNull);
        Assert.Equal(MessageParty.Unresolved, tag.Party);
        Assert.Equal(image, MessageTagImage.Of(tag));
        Assert.True(MessageTag.SameParty(tag, MessageTagImage.TagOf(image)));
        Assert.False(MessageTag.SameParty(tag, MessageTag.Null));
        Assert.False(MessageTag.SameParty(tag, new MessageTag(Mcs, MessageParty.Server, image, 0)));
    }

    [Fact]
    public void ImagingNManyMessages_LeavesTheRunUnitTableBounded_AndEveryTokenReadsBackItsMessage()
    {
        // kb/Work PB4926: the serial rides in the token, so the table holds one entry per party and serial block, not one per message.
        var table = RunUnit.Current.SlotImages;
        int before = table.TagBlockCount;
        const int n = 20000;
        var tokens = new string[n];
        for (int i = 0; i < n; i++) tokens[i] = MessageTagImage.Of(new MessageTag(Mcs, MessageParty.Requestor, "run-unit-bounded", 1_000_000_000L + i));
        Assert.True(table.TagBlockCount - before <= n / 4096 + 2, $"grew by {table.TagBlockCount - before}");
        Assert.Equal(n, tokens.Distinct().Count());
        foreach (int i in new[] { 0, 1, 4095, 4096, 12345, n - 1 })
        {
            var back = MessageTagImage.TagOf(tokens[i]);
            Assert.Equal(MessageParty.Requestor, back.Party);
            Assert.Equal("run-unit-bounded", back.Name);
            Assert.Equal(1_000_000_000L + i, back.Message);
        }
    }

    [Fact]
    public void ASlotMembersImage_OfAMessageTag_IsItsToken()
    {
        var tag = new MessageTag(Mcs, MessageParty.Server, "PAYROLL", 5);
        Assert.Equal(MessageTagImage.Of(tag), PointerImage.OfSlot(tag));
        Assert.Equal(PointerImage.NullImage, PointerImage.OfSlot(MessageTag.Null));
    }
}
