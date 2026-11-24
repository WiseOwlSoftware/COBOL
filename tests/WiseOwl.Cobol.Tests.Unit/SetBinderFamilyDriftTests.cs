// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using WiseOwl.Cobol.Binding.Procedure;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ SetBinder SELECTS THE SET FORMAT AND EACH FORMAT FAMILY'S BINDER BINDS IT (kb/Work PB2314; docs/rearchitecture/
/// DESIGN-architecture-review.md §8.3, R0-0019: "one binder per SET format family").
/// <para>SetBinder was 1,540 lines and 40 members, every format of ISO §2023:14.9.39.2 in one type. It now holds the format
/// dispatch alone (<c>BindSet</c>, the TO NULL and TO value arms that <see cref="SetFormatSelection"/> decides, and the guard
/// that refuses a non-receivable operand before any format is chosen), and the members of each family live in that family's
/// binder: content (Format 15), locale (11, 12), pointer (7, 8, 9, 17), address senders (ENTRY, ADDRESS OF FUNCTION, ADDRESS OF
/// PROGRAM), index (1, 2), capacity (14, 16) and condition (4). The C# types cannot say two things, which this test says:</para>
/// <list type="bullet">
/// <item>SetBinder declares no method beyond its dispatch, so a format's rule never grows back into the selector;</item>
/// <item>no member has two homes: no two of the family binders, and not SetBinder and a family binder, declare a member of
/// the same name and parameter types (a forwarder left by a move, CLAUDE.md rule 4).</item>
/// </list>
/// </summary>
public sealed class SetBinderFamilyDriftTests
{
    private const BindingFlags AllDeclared = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public
        | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    private static readonly Type[] Families =
    [
        typeof(SetContentBinder), typeof(SetLocaleBinder), typeof(SetPointerBinder), typeof(SetAddressBinder),
        typeof(SetIndexBinder), typeof(SetCapacityBinder), typeof(SetConditionBinder),
    ];

    /// <summary>The dispatch SetBinder keeps (its family accessors are properties, not methods).</summary>
    private static readonly HashSet<string> Dispatch = ["BindSet", "BindSetToNull", "BindSetTo", "RefuseNonReceivable"];

    private static IEnumerable<MethodInfo> Methods(Type type) =>
        type.GetMethods(AllDeclared).Where(m => !m.IsSpecialName && !m.Name.Contains('<'));

    private static IEnumerable<string> Signatures(Type type) =>
        Methods(type).Select(m => $"{m.Name}({string.Join(",", m.GetParameters().Select(p => p.ParameterType.FullName))})");

    [Fact]
    public void SetBinder_DeclaresOnlyTheFormatDispatch()
    {
        var methods = Methods(typeof(SetBinder)).Select(m => m.Name).Distinct().Order(StringComparer.Ordinal).ToList();
        Assert.True(methods.Count >= Dispatch.Count, $"SetBinder reflects only {methods.Count} methods — the scan lost them");
        var extra = methods.Where(m => !Dispatch.Contains(m)).ToList();
        Assert.True(extra.Count == 0,
            $"SetBinder declares {string.Join(", ", extra)}, which is not the format dispatch: a SET format's rule belongs to its "
            + "family's binder (SetContentBinder, SetLocaleBinder, SetPointerBinder, SetAddressBinder, SetIndexBinder, "
            + "SetCapacityBinder, SetConditionBinder; kb/Work PB2314)");
    }

    [Fact]
    public void NoMember_HasTwoHomes_AcrossTheSetBinders()
    {
        var homes = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var type in Families.Prepend(typeof(SetBinder)))
            foreach (string signature in Signatures(type).Distinct())
                (homes.TryGetValue(signature, out var list) ? list : homes[signature] = []).Add(type.Name);
        Assert.True(homes.Count >= 40, $"the SET binders reflect only {homes.Count} methods — the scan lost them");
        var twice = homes.Where(h => h.Value.Count > 1).Select(h => $"{h.Key} in {string.Join(" and ", h.Value)}").ToList();
        Assert.True(twice.Count == 0,
            "A member is declared by two SET binders: " + string.Join("; ", twice) + ". A moved member has ONE home; change its "
            + "callers and delete the other (CLAUDE.md rule 4).");
    }
}
