// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using WiseOwl.Cobol.Listing;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Listing;

/// <summary>
/// ⛔ THE OPTIONS SECTION NAMES EVERY OPTION (DESIGN-listing.md sections 6.4 and 10 item 6): the one table
/// (<see cref="OptionsSectionBuilder"/>) is held to the types it reads. Each settable public property of
/// <see cref="CompilerDriver.Options"/> and of <see cref="ListingRequest"/> is named by exactly one row, and setting that property
/// alone to a value other than its default changes exactly that row, which becomes <see cref="ListingOptionOrigin.Specified"/>.
/// A property added to either type without a row, a row naming no property, and a row that reads another row's property are
/// each red. The expected origins are the types' own defaults (<c>new CompilerDriver.Options(source)</c>,
/// <c>new ListingRequest()</c>), never a captured listing.
/// </summary>
public sealed class OptionsSectionDriftTests
{
    private const string Source = "a.cob";

    /// <summary>The properties a caller chooses: each one with a public setter, and each one the record's primary constructor takes
    /// (<see cref="ListingRequest"/>'s destination, <c>OutputBase</c> with <c>Formats</c>, is set only as a pair, by the constructor or
    /// <see cref="ListingRequest.WithDestination"/>, so it has no setter of its own).</summary>
    private static IEnumerable<PropertyInfo> Settable(Type type)
    {
        var positional = type.GetConstructors().MaxBy(c => c.GetParameters().Length)!.GetParameters().Select(p => p.Name)
            .ToHashSet(StringComparer.Ordinal);
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.SetMethod is { IsPublic: true } || positional.Contains(p.Name));
    }

    [Fact]
    public void EveryOptionProperty_HasExactlyOneRow()
    {
        Assert.Equal(Settable(typeof(CompilerDriver.Options)).Select(p => p.Name).Order(StringComparer.Ordinal),
            OptionsSectionBuilder.Table.Select(r => r.Property).Order(StringComparer.Ordinal));
        Assert.Equal(Settable(typeof(ListingRequest)).Select(p => p.Name).Order(StringComparer.Ordinal),
            OptionsSectionBuilder.RequestTable.Select(r => r.Property).Order(StringComparer.Ordinal));
        string[] names = [.. OptionsSectionBuilder.Table.Select(r => r.Name), .. OptionsSectionBuilder.RequestTable.Select(r => r.Name)];
        Assert.Equal(names.Length, names.Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>The library's defaults: every row default but the source (it has no default) and the listing itself (a listing
    /// section exists only when one was asked for), then the compiler, the runtime, .NET and the operating system.</summary>
    [Fact]
    public void AtTheDefaults_EveryOptionRowIsDefault()
    {
        var rows = OptionsSectionBuilder.Build(new CompilerDriver.Options(Source) { Listing = new ListingRequest() });
        int options = OptionsSectionBuilder.Table.Count + OptionsSectionBuilder.RequestTable.Count;
        Assert.Equal(options + 4, rows.Count);
        Assert.Equal(["source", "--listing"], rows.Take(options).Where(r => r.Origin != ListingOptionOrigin.Default).Select(r => r.Name));
        Assert.Equal(Source, rows[0].Value);
        Assert.Equal([("compiler", ListingOptionOrigin.Host), ("runtime", ListingOptionOrigin.Host), (".NET", ListingOptionOrigin.Host),
            ("operating system", ListingOptionOrigin.Host)], rows.Skip(options).Select(r => (r.Name, r.Origin)));
        Assert.All(rows, r => Assert.False(string.IsNullOrWhiteSpace(r.Value), r.Name));
        Assert.Equal("standard", rows.Single(r => r.Name == "--listing-sections").Value);
        Assert.Equal("2023 (ISO/IEC 1989:2023)", rows.Single(r => r.Name == "--std").Value);
    }

    public static TheoryData<string> OptionProperties() => [.. OptionsSectionBuilder.Table.Select(r => r.Property)];

    public static TheoryData<string> RequestProperties() => [.. OptionsSectionBuilder.RequestTable.Select(r => r.Property)];

    /// <summary>Setting one property of the options alone changes its row and no other: it is Specified (the source, which has
    /// no default, is Specified at every value and shows the new one). The one exception is a row whose default is derived from
    /// the property (the output path's, from the source path), whose value follows it and whose origin does not change.</summary>
    [Theory]
    [MemberData(nameof(OptionProperties))]
    public void SettingOneOption_ChangesExactlyItsRow(string property)
    {
        var defaults = new CompilerDriver.Options(Source);
        var changed = With(defaults, property);
        int count = OptionsSectionBuilder.Table.Count;
        var before = OptionsSectionBuilder.Build(defaults).Take(count).ToList();
        var after = OptionsSectionBuilder.Build(changed).Take(count).ToList();
        var table = OptionsSectionBuilder.Table;
        int row = table.ToList().FindIndex(r => r.Property == property);
        var derived = Enumerable.Range(0, count).Where(i => table[i].DerivedFrom == property).ToList();
        Assert.Equal([.. derived.Append(row).Order()], Enumerable.Range(0, count).Where(i => before[i] != after[i]));
        Assert.Equal(ListingOptionOrigin.Specified, after[row].Origin);
        Assert.All(derived, i => Assert.Equal(before[i].Origin, after[i].Origin));
    }

    /// <summary>The same for each property of the listing request, in the rows after the table's.</summary>
    [Theory]
    [MemberData(nameof(RequestProperties))]
    public void SettingOneListingProperty_ChangesExactlyItsRow(string property)
    {
        var defaults = new CompilerDriver.Options(Source) { Listing = new ListingRequest() };
        var changed = defaults with { Listing = With(new ListingRequest(), property) };
        var before = OptionsSectionBuilder.Build(defaults);
        var after = OptionsSectionBuilder.Build(changed);
        int row = OptionsSectionBuilder.Table.Count + OptionsSectionBuilder.RequestTable.ToList().FindIndex(r => r.Property == property);
        Assert.Equal([row], Enumerable.Range(0, before.Count).Where(i => before[i] != after[i]));
        Assert.Equal(ListingOptionOrigin.Specified, after[row].Origin);
    }

    /// <summary>A copy of <paramref name="record"/> with <paramref name="property"/> alone set to a value other than its own: through its
    /// setter, or, for half of a listing request's destination, through <see cref="ListingRequest.WithDestination"/> with the other half
    /// kept.</summary>
    private static T With<T>(T record, string property) where T : class
    {
        var info = typeof(T).GetProperty(property)!;
        object? other = Other(info, info.GetValue(record));
        if (record is ListingRequest request && info.SetMethod is not { IsPublic: true })
            return (T)(object)(property switch
            {
                nameof(ListingRequest.OutputBase) => request.WithDestination((string?)other, request.Formats),
                nameof(ListingRequest.Formats) => request.WithDestination(request.OutputBase, (ListingFormats)other!),
                _ => throw new InvalidOperationException($"no way to set ListingRequest.{property} alone: teach this test one"),
            });
        var copy = (T)typeof(T).GetMethod("<Clone>$")!.Invoke(record, null)!;
        info.SetValue(copy, other);
        return copy;
    }

    /// <summary>A value of <paramref name="property"/>'s type other than <paramref name="current"/>, one the type admits.</summary>
    private static object? Other(PropertyInfo property, object? current)
    {
        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        if (type == typeof(string)) return current is "elsewhere" ? "other" : "elsewhere";
        if (type == typeof(bool)) return !(bool)current!;
        if (type == typeof(int))
            return property.Name == nameof(CompilerDriver.Options.DialectLevel) ? ((int)current! == 85 ? 2002 : 85) : (int)current! + 1;
        if (type.IsEnum) return Enum.GetValues(type).Cast<object>().First(v => !v.Equals(current));
        if (type == typeof(IReadOnlyList<string>)) return new[] { "dir" };
        if (type == typeof(ListingRequest)) return current is null ? new ListingRequest() : null;
        if (type == typeof(ListingSectionSet))
            return (ListingSectionSet)current! == ListingSectionSet.Of(ListingSection.Source) ? ListingSectionSet.All : ListingSectionSet.Of(ListingSection.Source);
        throw new InvalidOperationException($"no other value of {type} for {property.DeclaringType!.Name}.{property.Name}: teach this test one");
    }
}
