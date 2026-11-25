// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.CodeGen.Emit;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE INTRINSIC CARRIER RULES ARE KEYED ON THE FUNCTION, AND EVERY KEY IS A CATALOG FUNCTION-NAME AS ITS ROW SPELLS IT
/// (kb/Work PB4370; DESIGN-backend-abstraction.md §1.2 bullet 2, §1.3 L9). <c>IntrinsicCarrierRules</c> lives in the bound
/// layer a second backend consumes, so it names a function by its identity (<c>IntrinsicSig.Name</c>, <c>"MOD"</c>) and
/// never by the runtime body one backend calls for it (<c>"ModScaled"</c>). A key is asked with <c>sig.Name</c> through an
/// ordinal set, so a key that is not exactly a row's name (a runtime spelling, a misspelling, a lower-case spelling the
/// catalog's own lookup would forgive) answers false in silence and drops its function from the SDIDI routing. This holds
/// every key to its row, and <see cref="KeyOf"/> is the one place a test asks it of a key.
/// <para>⛔ THE BACKEND'S TABLES KEY ON THE SAME NAME (kb/Work PB2627). The catalog row names no runtime member, so the bound layer's
/// native-body sets (<c>FunctionValueLane</c>) and the Roslyn renderer's function tables (<c>IntrinsicRenderer</c>) and switch arms are
/// keyed on the function too; <see cref="EveryBackendFunctionKey_IsACatalogFunctionName"/> holds them to the catalog, so a body's
/// spelling cannot creep back in as a key that answers false in silence.</para>
/// </summary>
public sealed class IntrinsicCarrierRuleKeyDriftTests
{
    /// <summary>Every function-keyed set <c>IntrinsicCarrierRules</c> declares, by its member name.</summary>
    private static readonly (string Set, IReadOnlySet<string> Functions)[] Sets =
    [
        (nameof(IntrinsicCarrierRules.CrossAligningFunctions), IntrinsicCarrierRules.CrossAligningFunctions),
        (nameof(IntrinsicCarrierRules.SdidiQuotientFunctions), IntrinsicCarrierRules.SdidiQuotientFunctions),
        (nameof(IntrinsicCarrierRules.ExactParseFunctions), IntrinsicCarrierRules.ExactParseFunctions),
        (nameof(IntrinsicCarrierRules.StandardArgumentKeepsAllDigits), IntrinsicCarrierRules.StandardArgumentKeepsAllDigits),
    ];

    /// <summary><paramref name="functionName"/> as a key: the catalog row's name, spelt exactly as the row spells it. Fails when no
    /// row is spelt exactly <paramref name="functionName"/>.</summary>
    internal static string KeyOf(string functionName)
    {
        Assert.True(IntrinsicCatalog.TryGet(functionName, out var sig), $"FUNCTION {functionName} has no IntrinsicCatalog row");
        Assert.True(string.Equals(sig.Name, functionName, StringComparison.Ordinal),
            $"the key {functionName} is not spelt as its catalog row spells it ({sig.Name}), and the carrier rules compare ordinally");
        return sig.Name;
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
                Assert.Equal(fn, KeyOf(fn));
        }
    }

    /// <summary>Every function key of the backend's tables, and every string label of a switch on <c>Sig.Name</c> in the renderer
    /// and the device-text builder, is a catalog function-name spelt as its row spells it (kb/Work PB2627).</summary>
    [Fact]
    public void EveryBackendFunctionKey_IsACatalogFunctionName()
    {
        var tables = new (string Table, IEnumerable<string> Keys)[]
        {
            (nameof(FunctionValueLane.NativeSdidiBodies), FunctionValueLane.NativeSdidiBodies),
            (nameof(FunctionValueLane.NativeSdidiBodiesWithExactResult), FunctionValueLane.NativeSdidiBodiesWithExactResult),
            (nameof(FunctionValueLane.NativeSdidiBodiesWithWholeResult), FunctionValueLane.NativeSdidiBodiesWithWholeResult),
            (nameof(IntrinsicRenderer.EquivalentExpressionUnaryArms), IntrinsicRenderer.EquivalentExpressionUnaryArms),
            (nameof(IntrinsicRenderer.Binary64Bodies), IntrinsicRenderer.Binary64Bodies.Keys),
        };
        foreach (var (table, keys) in tables)
        {
            Assert.NotEmpty(keys);
            foreach (string fn in keys)
                Assert.Equal(fn, KeyOf(fn));
        }

        int labels = 0;
        foreach (string file in new[] { "IntrinsicRenderer.cs", "FunctionDeviceText.cs" })
        {
            string src = File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Compiler", "CodeGen", "Emit", file));
            // A label of an arm keyed on the function: `case "MAX" or "MIN":`, `"SUM" => …`, `case (_, "ABS"):`, `Name is "MOD"`.
            foreach (Match m in Regex.Matches(src, @"(?:case\s+\(_,\s*|case\s+|^\s+|Name\s+(?:is|==|!=)\s+|\bor\s+)""(?<fn>[A-Z][A-Z0-9-]*)""(?=[\s:)=,;]|\s+or\b)",
                         RegexOptions.Multiline))
            {
                labels++;
                Assert.Equal(m.Groups["fn"].Value, KeyOf(m.Groups["fn"].Value));
            }
        }
        Assert.True(labels > 100, $"only {labels} function labels read from the renderer: the scan is blind");
    }
}
