// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;
using Fault = WiseOwl.Cobol.Binding.Model.StrongTypeModel.DataPointerStoreFault;
using Restriction = WiseOwl.Cobol.Binding.Model.StrongTypeModel.TypeRestriction;

namespace WiseOwl.Cobol.Tests.Unit.Architecture;

/// <summary>
/// ⛔ §2023:14.9.39.3 SR19 OVER A DATA-POINTER PAIR IS DECIDED ONCE (kb/Work PB2625). SET Format 7 and a data-address-identifier
/// argument (§2023:14.8.2.3.2, "If either is a restricted pointer, both shall be restricted and of the same type") ask one
/// predicate, <c>StrongTypeModel.DataPointerStore</c>. The matrix pins it to both wordings of the rule at once, and the
/// structural test holds both binders to it, so a refinement of the relation cannot reach one route and not the other.
/// </summary>
[Trait("Sweep", "tree")]
public sealed class DataPointerStoreDriftTests
{
    private static readonly Restriction None = default;
    private static readonly Restriction A = new("TYPE-A", null);
    private static readonly Restriction B = new("TYPE-B", null);

    public static TheoryData<string, string, Fault> Matrix => new()
    {
        { "-", "-", Fault.None },
        { "A", "A", Fault.None },
        { "A", "-", Fault.ReceiverRestricted },
        { "A", "B", Fault.ReceiverRestricted },
        { "-", "A", Fault.SenderRestricted },
    };

    [Theory]
    [MemberData(nameof(Matrix))]
    public void TheVerdict_IsSr19s_AndAgreesWithTheArgumentRule(string receiver, string sender, Fault expected)
    {
        Restriction r = Of(receiver), s = Of(sender);
        var fault = StrongTypeModel.DataPointerStore(r, s);
        Assert.Equal(expected, fault);
        // §2023:14.8.2.3.2's wording of the same relation: either restricted, and not both restricted to the same type.
        bool argumentRule = (r.IsRestricted || s.IsRestricted) && !StrongTypeModel.SameRestriction(r, s);
        Assert.Equal(argumentRule, fault is not Fault.None);
    }

    [Fact]
    public void BothRoutes_AskTheOneVerdict()
    {
        string ptr = File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Compiler", "Binding", "Procedure", "Verbs", "PtrBinder.cs"));
        foreach (string member in new[] { "internal bool ScreenPointerReceiverRestriction(", "internal string? AddressConformanceReason(" })
        {
            int start = ptr.IndexOf(member, StringComparison.Ordinal);
            Assert.True(start >= 0, $"{member} moved: update this guard");
            int next = ptr.IndexOf("\n    internal ", start + member.Length, StringComparison.Ordinal);
            string body = next < 0 ? ptr[start..] : ptr[start..next];
            Assert.Contains("StrongTypeModel.DataPointerStore(", body, StringComparison.Ordinal);
        }
    }

    private static Restriction Of(string name) => name switch { "A" => A, "B" => B, _ => None };
}
