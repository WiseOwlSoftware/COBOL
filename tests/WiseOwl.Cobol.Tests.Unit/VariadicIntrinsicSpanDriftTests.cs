// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ NO VARIADIC INTRINSIC EVALUATION ALLOCATES ITS ARGUMENT LIST (kb/Work PB2636). Every variadic body of the intrinsic runtime
/// takes <c>params ReadOnlySpan&lt;T&gt;</c>, so a written argument list rides a stack inline array and a constant scale list static
/// data; a <c>params T[]</c> body made the compiler allocate an array per call, and the MAX / MIN / ORD-MAX / ORD-MIN arm emitted two
/// explicit arrays per evaluation, one of them the compile-time-constant scales. This class refuses a <c>params</c> array in the
/// intrinsic runtime and an array parameter on the selection entries.
/// </summary>
public sealed class VariadicIntrinsicSpanDriftTests
{
    private static readonly TreeScope IntrinsicSources = new(Under: ["src/WiseOwl.Cobol.Runtime/Intrinsics/"], Kinds: [".cs"]);

    private static readonly Regex ParamsArray = new(@"\bparams\s+[\w.<>]+\[\]\s+\w+\s*\)", RegexOptions.Compiled);

    [Fact]
    public void NoIntrinsicBody_TakesAParamsArray()
    {
        var offenders = new List<string>();
        int files = 0;
        foreach (string path in TrackedTree.Files(IntrinsicSources))
        {
            files++;
            string[] lines = File.ReadAllLines(TestRepo.Within("src/WiseOwl.Cobol.Runtime/Intrinsics/", path));
            for (int i = 0; i < lines.Length; i++)
                if (!lines[i].TrimStart().StartsWith("//", StringComparison.Ordinal) && ParamsArray.IsMatch(lines[i]))
                    offenders.Add($"{path}:{i + 1}: {lines[i].Trim()}");
        }
        Assert.True(files >= 10, $"only {files} intrinsic source file(s) were scanned: the scope stopped matching the runtime");
        Assert.True(offenders.Count == 0,
            "a variadic intrinsic body takes a params array, so every call allocates its argument list (take params ReadOnlySpan<T>; "
            + "kb/Work PB2636):\n" + string.Join("\n", offenders));
    }

    [Fact]
    public void EveryPublicIntrinsicEntry_TakesNoArrayArgumentList()
    {
        // The public surface the emitted code calls: an array parameter there is an allocation at every call site that writes the list.
        var arrays = typeof(CobolIntrinsics).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name is "MaxAt" or "MinAt" or "OrdMaxAt" or "OrdMinAt"
                        || m.GetParameters().Any(p => p.GetCustomAttribute<ParamArrayAttribute>() is not null))
            .SelectMany(m => m.GetParameters().Where(p => p.ParameterType.IsArray).Select(p => $"{m.Name}({p.Name})"))
            .ToList();
        Assert.True(arrays.Count == 0, "an intrinsic entry takes its argument list as an array (kb/Work PB2636): " + string.Join(", ", arrays));
        var spans = typeof(CobolIntrinsics).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Count(m => m.GetParameters().Any(p => p.ParameterType.IsGenericType
                                                   && p.ParameterType.GetGenericTypeDefinition() == typeof(ReadOnlySpan<>)));
        Assert.True(spans >= 60, $"only {spans} intrinsic entries take a span argument list: the reflection stopped finding the family");
    }
}
