// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Binding.Bound;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE INTRINSIC CARRIER RULES ARE KEYED ON THE FUNCTION, AND EVERY KEY IS A CATALOG FUNCTION-NAME AS ITS ROW SPELLS IT
/// (kb/Work PB4370; DESIGN-backend-abstraction.md §1.2 bullet 2, §1.3 L9). <c>IntrinsicCarrierRules</c> lives in the bound
/// layer a second backend consumes, so it names a function by its identity (<c>IntrinsicSig.Name</c>, <c>"MOD"</c>) and
/// never by the runtime body one backend calls for it (<c>"ModScaled"</c>). A key is asked with <c>sig.Name</c> through an
/// ordinal set, so a key that is not exactly a row's name (a runtime spelling, a misspelling, a lower-case spelling the
/// catalog's own lookup would forgive) answers false in silence and drops its function from the SDIDI routing. This holds
/// every key to its row, and <see cref="RuntimeMethodOf"/> is the one place a test maps a key to the Roslyn renderer's
/// spelling of it, the row's <c>RuntimeMethod</c>.
/// </summary>
public sealed class IntrinsicCarrierRuleKeyDriftTests
{
    /// <summary>Every function-keyed set <c>IntrinsicCarrierRules</c> declares, by its member name.</summary>
    private static readonly (string Set, IReadOnlySet<string> Functions)[] Sets =
    [
        (nameof(IntrinsicCarrierRules.CrossAligningFunctions), IntrinsicCarrierRules.CrossAligningFunctions),
        (nameof(IntrinsicCarrierRules.SdidiQuotientFunctions), IntrinsicCarrierRules.SdidiQuotientFunctions),
        (nameof(IntrinsicCarrierRules.ExactParseFunctions), IntrinsicCarrierRules.ExactParseFunctions),
    ];

    /// <summary>The runtime body the catalog row of <paramref name="functionName"/> names: the Roslyn renderer's spelling of
    /// the function its switch arms are labelled with. Fails when no row is spelt exactly <paramref name="functionName"/>.</summary>
    internal static string RuntimeMethodOf(string functionName)
    {
        Assert.True(IntrinsicCatalog.TryGet(functionName, out var sig), $"FUNCTION {functionName} has no IntrinsicCatalog row");
        Assert.True(string.Equals(sig.Name, functionName, StringComparison.Ordinal),
            $"the key {functionName} is not spelt as its catalog row spells it ({sig.Name}), and the carrier rules compare ordinally");
        return sig.RuntimeMethod;
    }

    [Fact]
    public void EveryKey_IsACatalogFunctionName_SpeltAsItsRowSpellsIt()
    {
        var reflected = typeof(IntrinsicCarrierRules)
            .GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            .Where(f => typeof(IReadOnlySet<string>).IsAssignableFrom(f.FieldType))
            .Select(f => f.Name)
            .Order(StringComparer.Ordinal);
        Assert.Equal(reflected, Sets.Select(s => s.Set).Order(StringComparer.Ordinal));
        foreach (var (set, functions) in Sets)
        {
            Assert.NotEmpty(functions);
            foreach (string fn in functions)
                Assert.False(string.IsNullOrEmpty(RuntimeMethodOf(fn)), $"{set}: FUNCTION {fn}'s catalog row names no runtime body");
        }
    }
}
