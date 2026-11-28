// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using WiseOwl.Cobol.Compiler.Binding;
using WiseOwl.Cobol.Compiler.Binding.Bound;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ WHAT A REFERENCE RESOLUTION MAY DO IS FIXED WHEN ITS RESOLVER IS CONSTRUCTED (kb/Work PB3230;
/// docs/rearchitecture/DESIGN-architecture-review.md §8.3 (4)). The probe mode, the statement site and the
/// one-report-per-reference sets used to be ambient mutable state every part of the subsystem read: a <c>Probing</c> flag
/// each speculative entry saved, set and restored, five delegate hooks the statement binder installed on the funnel and
/// three of its parts after construction, and report sets spread over the funnel, its qualification part and the
/// condition binder. These tests pin the shape that replaced them, by reflection over the built types, so a new
/// settable hook, a writable mode flag or a private report set fails here rather than in review.
/// </summary>
public sealed class ResolutionModeDriftTests
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    /// <summary>The funnel and every part it composes: each part is a class whose constructor takes the funnel.</summary>
    private static IReadOnlyList<Type> ResolverTypes { get; } =
        [typeof(ReferenceResolver), .. typeof(ReferenceResolver).Assembly.GetTypes()
            .Where(t => t.IsClass && t != typeof(ReferenceResolver)
                && t.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Any(c => c.GetParameters() is [{ ParameterType: var first }, ..] && first == typeof(ReferenceResolver)))];

    [Fact]
    public void TheSubsystemIsFound()
    {
        foreach (var part in new[] { "NameResolution", "SubscriptBinder", "RefModBinder", "ObjectPropertyResolver", "CounterResolver" })
            Assert.Contains(ResolverTypes, t => t.Name == part);
    }

    /// <summary>The statement site is a constructor argument (<see cref="IStatementSite"/>), so no part of the subsystem holds
    /// a delegate — settable or not — through which another layer could reach back into a resolution.</summary>
    [Fact]
    public void NoResolverPart_HoldsADelegate()
    {
        var offenders = ResolverTypes
            .SelectMany(t => t.GetFields(Members).Where(f => typeof(Delegate).IsAssignableFrom(f.FieldType)).Select(f => $"{t.Name}.{f.Name}")
                .Concat(t.GetProperties(Members).Where(p => typeof(Delegate).IsAssignableFrom(p.PropertyType)).Select(p => $"{t.Name}.{p.Name}")))
            .ToList();
        Assert.True(offenders.Count == 0,
            "a reference-resolution part holds a delegate; the statement site is ONE interface passed at construction "
            + "(IStatementSite, kb/Work PB3230): " + string.Join(", ", offenders));
    }

    /// <summary>A probe is a construction (<see cref="ReferenceResolver.Prober"/>), so the mode has no setter, and the site
    /// cannot be replaced under a resolver.</summary>
    [Fact]
    public void TheModeAndTheSite_AreConstantsOfTheConstruction()
    {
        foreach (var name in new[] { "Probing", "Site", "Prober", "Reports" })
        {
            var property = typeof(ReferenceResolver).GetProperty(name, Members);
            Assert.NotNull(property);
            Assert.False(property!.CanWrite, $"ReferenceResolver.{name} is writable; it is fixed when the resolver is constructed (kb/Work PB3230)");
        }
        Assert.Equal(typeof(IStatementSite), typeof(ReferenceResolver).GetProperty("Site", Members)!.PropertyType);
    }

    /// <summary>One report per written reference is ONE ledger (<see cref="ReferenceReports"/>): no part of the subsystem, and
    /// not the condition binder whose condition-name reports are the same discipline, keeps a set of its own.</summary>
    [Fact]
    public void ReportSets_LiveInTheOneLedger()
    {
        var offenders = ResolverTypes.Append(typeof(WiseOwl.Cobol.Compiler.Binding.Procedure.Verbs.ConditionBinder))
            .SelectMany(t => t.GetFields(Members)
                .Where(f => f.FieldType.IsGenericType && f.FieldType.GetGenericTypeDefinition() == typeof(HashSet<>))
                .Select(f => $"{t.Name}.{f.Name}"))
            .ToList();
        Assert.True(offenders.Count == 0,
            "a reference-resolution part keeps its own report set; one report per written reference is the ReferenceReports "
            + "ledger, asked through ReferenceResolver.ReportOnce (kb/Work PB3230): " + string.Join(", ", offenders));
    }

    /// <summary>The statement binder is the one statement site: the procedure resolver is constructed with it.</summary>
    [Fact]
    public void TheStatementBinder_IsTheOneStatementSite()
    {
        var sites = typeof(ReferenceResolver).Assembly.GetTypes()
            .Where(t => t.IsClass && typeof(IStatementSite).IsAssignableFrom(t)).ToList();
        Assert.Equal([typeof(StatementBinder)], sites);
    }
}
