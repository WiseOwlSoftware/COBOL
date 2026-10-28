// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Binding.Repository;
using WiseOwl.Cobol.Common;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.Repository;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;
using static WiseOwl.Cobol.Binding.Repository.RepositoryVocabulary;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE REPOSITORY'S PERSISTED VOCABULARY IS TOTAL AND FIXED (DESIGN-external-repository §6.2 "Encoding" rule 1, §17.2;
/// kb/Work PB2100). Every schema enum in <c>WiseOwl.Cobol.Runtime.Repository</c> is <c>int</c>-backed and gives every member an
/// explicit value (a reader decodes an enum argument without the defining assembly, and a value that moves silently reads
/// an old module as another fact), and every member of every compiler enum the codec persists round-trips through its
/// <see cref="RepositoryVocabulary"/> pair — so a member added to a compiler enum without a persisted value is red here
/// before it can reach a pair's discard arm.
/// </summary>
public sealed class RepositoryVocabularyDriftTests
{
    [Fact]
    public void EverySchemaEnum_IsIntBacked_AndEveryMemberHasAnExplicitValue()
    {
        var enums = typeof(CobolRepositoryAttribute).Assembly.GetTypes()
            .Where(t => t.IsEnum && t.Namespace == typeof(CobolRepositoryAttribute).Namespace).ToList();
        Assert.True(enums.Count >= 10, $"only {enums.Count} schema enums were found — the scan measured nothing");
        Assert.All(enums, e => Assert.True(Enum.GetUnderlyingType(e) == typeof(int), $"{e.Name} is not int-backed"));
        // An explicit value is a property of the source: every member line of every enum the namespace declares carries `= n`.
        string source = string.Concat(Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Runtime", "Repository"), "*.cs")
            .Order(StringComparer.Ordinal).Select(File.ReadAllText));
        foreach (var e in enums)
        {
            var body = Regex.Match(source, @"public enum " + e.Name + @"\s*\{(?<body>[^}]*)\}").Groups["body"].Value;
            Assert.False(string.IsNullOrEmpty(body), $"{e.Name} is not declared under src/WiseOwl.Cobol.Runtime/Repository");
            foreach (string member in Enum.GetNames(e))
                Assert.Matches(new Regex(@"^\s*" + member + @"\s*=\s*\d+\s*,", RegexOptions.Multiline), body);
        }
    }

    [Fact]
    public void EveryCompilerEnumMember_RoundTripsThroughItsPair()
    {
        foreach (var v in Enum.GetValues<PicCategory>()) Assert.Equal(v, FromPersisted(Persisted(v)));
        foreach (var v in Enum.GetValues<Usage>()) Assert.Equal(v, FromPersisted(Persisted(v)));
        foreach (var v in Enum.GetValues<GroupUsage>()) Assert.Equal(v, FromPersisted(Persisted(v)));
        foreach (var v in Enum.GetValues<DynamicLengthPrefix>()) Assert.Equal(v, FromPersisted(Persisted(v)));
        foreach (var v in Enum.GetValues<LiteralClass>()) Assert.Equal(v, FromPersisted(Persisted(v)));
        foreach (var v in Enum.GetValues<NumericDisplayForm>()) Assert.Equal(v, FromPersisted(Persisted(v)));
        foreach (var v in Enum.GetValues<RaisingTargetKind>()) Assert.Equal(v, FromPersisted(Persisted(v)));
        Assert.Null(FromPersisted(Persisted((SynchronizedPhrase?)null)));
        foreach (var v in Enum.GetValues<SynchronizedPhrase>()) Assert.Equal(v, FromPersisted(Persisted(v)));
        Assert.Null(FromPersisted(Persisted((ObjectRefDescriptor?)null)));
        foreach (var v in Enum.GetValues<ObjectRefKind>())
            Assert.Equal(v, FromPersisted(Persisted(new ObjectRefDescriptor(v, "C", false, false))));
    }

    /// <summary>The converse: every persisted member names a compiler value, so no value a record may carry is undecodable.</summary>
    [Fact]
    public void EveryPersistedMember_DecodesToACompilerValue()
    {
        foreach (var v in Enum.GetValues<RepositoryCategory>()) Assert.Equal(v, Persisted(FromPersisted(v)));
        foreach (var v in Enum.GetValues<RepositoryUsage>()) Assert.Equal(v, Persisted(FromPersisted(v)));
        foreach (var v in Enum.GetValues<RepositoryGroupUsage>()) Assert.Equal(v, Persisted(FromPersisted(v)));
        foreach (var v in Enum.GetValues<RepositoryPrefix>()) Assert.Equal(v, Persisted(FromPersisted(v)));
        foreach (var v in Enum.GetValues<RepositoryLiteralClass>()) Assert.Equal(v, Persisted(FromPersisted(v)));
        foreach (var v in Enum.GetValues<RepositoryNumericDisplay>()) Assert.Equal(v, Persisted(FromPersisted(v)));
        foreach (var v in Enum.GetValues<RepositoryRaisingKind>()) Assert.Equal(v, Persisted(FromPersisted(v)));
        foreach (var v in Enum.GetValues<RepositorySynchronized>()) Assert.Equal(v, Persisted(FromPersisted(v)));
        foreach (var v in Enum.GetValues<ObjectReferenceKind>())
            Assert.Equal(v, Persisted(FromPersisted(v) is { } kind ? new ObjectRefDescriptor(kind, "C", false, false) : null));
    }
}
