// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// The runtime message-tag carrier (ISO §2023:13.18.60.4 GR9; docs/CONFORMANCE.md §7 DOC-A.1-212; kb/Work PB4740). The
/// conformance goldens can only build NULL tags, because only the message control system makes any other, so the
/// non-null half of §2023:8.8.4.2.14's equality is pinned here, on the one function the relation condition renders.
/// ISO §2023:8.8.4.2.14: "The operands are equal if they reference the same message requestor or message server of the
/// same name or one is NULL and the other is NULL." (cite.py --check 8.8.4.2.14 OK.)
/// </summary>
public sealed class MessageTagTests
{
    private const string Mcs = "0123456789abcdef";

    private static MessageTag Server(string name, long message = 1, string mcs = Mcs) =>
        new(mcs, MessageParty.Server, name, message);

    private static MessageTag Requestor(string runUnit, long message = 1) =>
        new(Mcs, MessageParty.Requestor, runUnit, message);

    [Fact]
    public void TheDefaultTag_IsTheNullContent_AndIsNullEqualsNull()
    {
        Assert.True(default(MessageTag).IsNull);
        Assert.True(MessageTag.Null.IsNull);
        Assert.True(MessageTag.SameParty(MessageTag.Null, default));
    }

    [Fact]
    public void ANonNullTag_IsNotEqualToNull_InEitherOrder()
    {
        Assert.False(Server("PAYROLL").IsNull);
        Assert.False(MessageTag.SameParty(Server("PAYROLL"), MessageTag.Null));
        Assert.False(MessageTag.SameParty(MessageTag.Null, Requestor("run-unit-7")));
    }

    [Fact]
    public void SameParty_IsEqualityOfTheParty()
    {
        // Two server tags of one MCS and one server name are one party, whatever the message number.
        Assert.True(MessageTag.SameParty(Server("PAYROLL", 1), Server("PAYROLL", 2)));
        Assert.False(MessageTag.SameParty(Server("PAYROLL"), Server("BILLING")));
        // Two requestor tags of one requestor run unit likewise.
        Assert.True(MessageTag.SameParty(Requestor("run-unit-7", 1), Requestor("run-unit-7", 5)));
        Assert.False(MessageTag.SameParty(Requestor("run-unit-7"), Requestor("run-unit-8")));
    }

    [Fact]
    public void ARoleDecides_AServerAndARequestorOfOneNameAreTwoParties()
    {
        Assert.False(MessageTag.SameParty(Server("X"), Requestor("X")));
        Assert.False(MessageTag.SameParty(Requestor("X"), Server("X")));
    }

    [Fact]
    public void ATagOfAnotherMcs_IsAnotherParty()
    {
        Assert.False(MessageTag.SameParty(Server("PAYROLL"), Server("PAYROLL", mcs: "fedcba9876543210")));
    }
}
