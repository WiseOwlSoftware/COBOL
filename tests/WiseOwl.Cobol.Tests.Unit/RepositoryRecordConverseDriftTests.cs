// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Binding.Repository;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE REPOSITORY RECORD MIRRORS THE DESCRIPTION MODEL ONE TO ONE (DESIGN-external-repository §6.1, §17.2's converse;
/// kb/Work PB2100). The round trip (<see cref="RepositoryCodecTests"/>, <see cref="RepositoryRoundTripCorpusTests"/>) proves the
/// members the comparators read TODAY survive the record; this proves the NEXT member does too: every constructor parameter
/// and settable property of <see cref="PicInfo"/> has a <see cref="PicRecord"/> field, and every <see cref="DataItem"/> member
/// the data-description copy carries (a <see cref="DescriptionCopyAttribute"/> whose kind is not None) has a
/// <see cref="DescriptionRecord"/> field or is in <see cref="NotRecorded"/> with the reason a record need not hold it. A member
/// added to either type without a decision fails here, before a record silently drops it.
/// </summary>
public sealed class RepositoryRecordConverseDriftTests
{
    /// <summary>The <see cref="PicInfo"/> members whose record field has another name, and that name.</summary>
    private static readonly Dictionary<string, string[]> PicRenamed = new(StringComparer.Ordinal)
    {
        ["RestrictedTypeDecl"] = ["RestrictedTypeDeclaration"],   // an index into the record's TypeDeclarations (§6.2 rule 4)
        ["ObjectRef"] = ["ObjectRefKind", "ObjectRefName", "ObjectRefIdentity", "ObjectRefFactory", "ObjectRefOnly", "ObjectRefFormal"],
    };

    /// <summary>The <see cref="DataItem"/> members whose record field has another name, and that name.</summary>
    private static readonly Dictionary<string, string> ItemRenamed = new(StringComparer.Ordinal)
    {
        ["CobolName"] = "Name", ["RedefinesTargetName"] = "RedefinesTarget", ["OccursSpec"] = "Table", ["StrongType"] = "Strong",
        ["IsAligned"] = "Aligned", ["IsAnyLength"] = "AnyLength", ["IsDynamicLength"] = "DynamicLength", ["IsBased"] = "Based",
        ["HasExternalClause"] = "External",
    };

    /// <summary>The carried <see cref="DataItem"/> members a record does not hold, each with its reason (§6.3 "Not recorded").</summary>
    private static readonly Dictionary<string, string> NotRecorded = new(StringComparer.Ordinal)
    {
        ["Pending"] = "binder-transient: the usage a PICTURE awaits from its group, resolved into Pic before binding completes",
        ["OwnSign"] = "the written SIGN clause, whose effect is the analyzed Pic (SignKind, Signed), which is recorded",
        ["OwnUsage"] = "the written USAGE clause, whose effect is the analyzed Pic.Usage, which is recorded",
        ["OwnNoSign"] = "the written WITH NO SIGN phrase, whose effect is the analyzed Pic (PackedNoSign), which is recorded",
        ["PicIsUsageSynthesized"] = "binder-transient: a group's synthesized usage PICTURE, shed by the usage walk",
        ["PicIsValueImplied"] = "the PICTURE a VALUE clause implied, whose effect is the analyzed Pic, which is recorded",
        ["PictureText"] = "the written character-string, whose analysis is Pic and whose identity is Pic.Clause, both recorded",
        ["RawValue"] = "a VALUE clause: initial content, not a description a comparator reads (§6.3)",
        ["TableValues"] = "a table's VALUE clauses: initial content (§6.3)",
        ["ValueCopiedFrom"] = "the entry that wrote a VALUE clause (§6.3: VALUEs are not recorded)",
        ["IndexNames"] = "INDEXED BY index-names (§6.3: index names are not recorded)",
        ["DeclaredAt"] = "a source position of the publishing element",
        ["Property"] = "the PROPERTY clause of an object's data (ISO §13.18.42), a class record's fact (slice 8), never a formal's",
    };

    [Fact]
    public void EveryPicInfoMember_HasAPicRecordField()
    {
        var fields = RecordFields(typeof(PicRecord));
        var members = typeof(PicInfo).GetConstructors().Single(c => c.GetParameters().Length == 6).GetParameters().Select(p => p.Name!)
            .Concat(typeof(PicInfo).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.SetMethod is { IsPublic: true }).Select(p => p.Name))
            .Distinct(StringComparer.Ordinal).ToList();
        Assert.True(members.Count > 20, $"only {members.Count} PicInfo members were found — the reflection measured nothing");
        var missing = members.Where(m => !(PicRenamed.TryGetValue(m, out var names) ? names : [m]).All(fields.Contains)).ToList();
        Assert.True(missing.Count == 0, "PicInfo members with no PicRecord field (DESIGN-external-repository §6.1 — the record is "
            + "PicInfo BY VALUE): " + string.Join(", ", missing));
    }

    [Fact]
    public void EveryCarriedDataItemMember_HasADescriptionRecordField_OrAReason()
    {
        var fields = RecordFields(typeof(DescriptionRecord));
        var carried = typeof(DataItem).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<DescriptionCopyAttribute>() is { Kind: not DescriptionCopyKind.None })
            .Select(p => p.Name).ToList();
        Assert.True(carried.Count > 20, $"only {carried.Count} carried DataItem members were found — the reflection measured nothing");
        var missing = carried.Where(m => !NotRecorded.ContainsKey(m) && !fields.Contains(ItemRenamed.GetValueOrDefault(m, m))).ToList();
        Assert.True(missing.Count == 0, "DataItem members the description copy carries with no DescriptionRecord field and no stated "
            + "reason (DESIGN-external-repository §6.1, §6.3): " + string.Join(", ", missing));
        var stale = NotRecorded.Keys.Where(m => !carried.Contains(m)).ToList();
        Assert.True(stale.Count == 0, "NotRecorded names members the description copy no longer carries: " + string.Join(", ", stale));
    }

    /// <summary>The members the restricted-pointer, typedef and structure facts need beyond the copy's classification.</summary>
    [Fact]
    public void TheTypeAndRestrictionFacts_AreRecorded()
    {
        var fields = RecordFields(typeof(DescriptionRecord));
        Assert.All(["IsTypedef", "TypedefStrong", "IsExternalTypedef", "External", "TypeDeclaration", "Children", "Level"],
            f => Assert.Contains(f, fields));
    }

    private static HashSet<string> RecordFields(Type record) =>
        [.. record.GetConstructors().Single(c => c.GetParameters().Length > 1).GetParameters().Select(p => p.Name!)];
}
