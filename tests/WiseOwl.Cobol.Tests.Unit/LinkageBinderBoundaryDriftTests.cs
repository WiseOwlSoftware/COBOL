// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE LINKAGE FACTS HAVE ONE HOME AND ONE WRITER, AND THE BINDER HANDS THEM OUT AS READ-ONLY VIEWS (kb/Work PB2359;
/// docs/rearchitecture/DESIGN-architecture-review.md §8.3 (1)).
/// <para>Step 9 of the <c>DataBinder</c> extract moved the LINKAGE / EXTERNAL / GLOBAL partial into
/// <c>LinkageBinder</c>: the unit's linkage roots, its PROCEDURE DIVISION USING formals and RETURNING item, its EXTERNAL
/// records' backings, its GLOBAL roots and the fields the emitter must not declare, with every writer of them. Two
/// things keep that true without a list of member names that a rename would outlive:</para>
/// <list type="bullet">
/// <item>every view <c>LinkageBinder</c> exposes is read-only by its declared type (no <c>ICollection&lt;T&gt;</c>), so a
/// reader can append nothing and only the binder's own methods write;</item>
/// <item><c>DataBinder</c> declares no field, property or method whose type is one of the linkage fact types
/// (<c>LinkageFormal</c>, <c>CallExternalBacking</c>, <c>ExternalizedSubject</c>), so a second home for the facts, or a
/// forwarder that hands them out from the data binder again, cannot appear.</item>
/// </list>
/// </summary>
public sealed class LinkageBinderBoundaryDriftTests
{
    private const BindingFlags Everything =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    /// <summary>True when <paramref name="t"/> is, or is built from (a type argument or an element), <paramref name="target"/>.</summary>
    private static bool Mentions(Type t, Type target) =>
        t == target
        || (t.HasElementType && Mentions(t.GetElementType()!, target))
        || (t.IsGenericType && t.GetGenericArguments().Any(a => Mentions(a, target)));

    /// <summary>True when <paramref name="t"/> admits adding or removing an element: it is, or implements, <c>ICollection&lt;T&gt;</c>.</summary>
    private static bool IsMutableCollection(Type t) =>
        new[] { t }.Concat(t.GetInterfaces())
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>));

    [Fact]
    public void EveryViewTheLinkageBinderExposes_IsReadOnlyByItsDeclaredType()
    {
        var properties = typeof(LinkageBinder).GetProperties(Everything).Where(p => p.GetMethod is { IsPrivate: false }).ToList();
        Assert.True(properties.Count >= 6, $"the scan found only {properties.Count} linkage views — the reflection no longer reaches them");
        var mutable = properties.Where(p => IsMutableCollection(p.PropertyType)).Select(p => $"{p.Name}: {p.PropertyType}").ToList();
        Assert.True(mutable.Count == 0,
            "A linkage view admits adding or removing elements — expose IReadOnlyList / IReadOnlySet and give the binder a "
            + "writer method, so the facts have one writer:\n  " + string.Join("\n  ", mutable));
    }

    [Fact]
    public void TheDataBinder_DeclaresNoMemberOfALinkageFactType()
    {
        Type[] facts = [typeof(LinkageFormal), typeof(CallExternalBacking), typeof(ExternalizedSubject)];
        var offenders = new List<string>();
        foreach (var member in typeof(DataBinder).GetMembers(Everything))
        {
            var types = member switch
            {
                FieldInfo f => new[] { f.FieldType },
                PropertyInfo p => [p.PropertyType],
                MethodInfo m => m.GetParameters().Select(x => x.ParameterType).Append(m.ReturnType).ToArray(),
                _ => [],
            };
            foreach (var fact in facts)
                if (types.Any(t => Mentions(t, fact)))
                    offenders.Add($"{member.MemberType} {member.Name} mentions {fact.Name}");
        }
        Assert.True(offenders.Count == 0,
            "The data binder declares a member of a linkage fact type — those facts live in LinkageBinder "
            + "(DataBinder.Linkage), written only by it:\n  " + string.Join("\n  ", offenders));
    }

    [Fact]
    public void NoDataBinderPartial_IsNamedForTheLinkageConcern()
    {
        string binding = TestRepo.Src("WiseOwl.Cobol.Compiler", "Binding");
        Assert.True(File.Exists(Path.Combine(binding, "LinkageBinder.cs")), "LinkageBinder.cs is gone");
        Assert.False(File.Exists(Path.Combine(binding, "DataBinder.Linkage.cs")),
            "a DataBinder.Linkage.cs partial is back: the linkage concern is the LinkageBinder type, not a partial of the data binder");
    }
}
