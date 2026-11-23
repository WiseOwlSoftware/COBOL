// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
#nullable enable
namespace WiseOwl.Cobol.Tests.Shared.Gate;

/// <summary>⛔ An orderer's output is CHECKED to be a permutation of its input. xunit trusts an orderer's result:
/// a case an orderer dropped would silently never run and one it repeated would run twice. The check throws instead,
/// and xunit then logs the orderer's exception and keeps its own (complete) order — ordering is best effort,
/// completeness is not.</summary>
internal static class GateOrder
{
    public static List<T> Permutation<T>(IEnumerable<T> input, Func<IReadOnlyList<T>, IEnumerable<T>> order)
    {
        var inList = input.ToList();
        var outList = order(inList).ToList();
        var count = new Dictionary<object, int>(ReferenceEqualityComparer.Instance);
        foreach (T item in inList)
        {
            object key = item!;
            count[key] = count.GetValueOrDefault(key) + 1;
        }

        foreach (T item in outList)
        {
            object key = item!;
            if (!count.TryGetValue(key, out int n) || n == 0)
            {
                throw new InvalidOperationException(
                    $"a gate orderer's output is not a permutation of its input ({inList.Count} in, {outList.Count} out; "
                    + $"an item it returned was not in the input or was returned twice)");
            }

            count[key] = n - 1;
        }

        if (outList.Count != inList.Count)
        {
            throw new InvalidOperationException(
                $"a gate orderer's output is not a permutation of its input ({inList.Count} in, {outList.Count} out)");
        }

        return outList;
    }
}
