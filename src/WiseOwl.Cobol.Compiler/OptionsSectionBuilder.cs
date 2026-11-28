// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Runtime.InteropServices;
using WiseOwl.Cobol.Listing;

namespace WiseOwl.Cobol.Compiler;

/// <summary>
/// ⛔ THE OPTIONS SECTION'S ONE TABLE (DESIGN-listing.md section 6.4): one row per public property of
/// <see cref="CompilerDriver.Options"/>, each with the command line's spelling of the option and of its value, and whether the
/// value is the option's default; the listing request expands into one row per property of <see cref="ListingRequest"/>; then
/// the compiler, the runtime, .NET and the operating system. It lives in the composition root because it alone may name the
/// compilation's options type (the builder is a layer below it, docs/rearchitecture/architecture-edges.json), and the driver
/// hands the rows to the listing as plain values.
/// <para>The Unit assembly's <c>OptionsSectionDriftTests</c> holds the table to the types: each public property of
/// <see cref="CompilerDriver.Options"/> and of <see cref="ListingRequest"/> is named by exactly one row, and setting that property
/// alone to a value other than its default makes exactly that row <see cref="ListingOptionOrigin.Specified"/>. A property added
/// to either type without a row, or a row that does not read its property, is red.</para>
/// </summary>
internal static class OptionsSectionBuilder
{
    /// <summary>One row of the table: the property it reads, the name it prints, and the spelling of a value of it. A row is
    /// <see cref="ListingOptionOrigin.Default"/> when its value is spelled as the default options' is, unless
    /// <paramref name="IsDefault"/> decides it: the source, which has no default, and a default derived from another value (the
    /// output path), which is the default when the option was not given, whatever it derives to.</summary>
    /// <param name="DerivedFrom">The property a derived default is derived from (the output path's from the source path), whose
    /// change changes this row's value too; null for every other row.</param>
    internal sealed record Row(string Property, string Name, Func<CompilerDriver.Options, string> Value,
        Func<CompilerDriver.Options, bool>? IsDefault = null, string? DerivedFrom = null);

    /// <summary>A row of the listing request: the property of <see cref="ListingRequest"/> it reads.</summary>
    internal sealed record RequestRow(string Property, string Name, Func<ListingRequest, string> Value);

    /// <summary>The options a command line that names only its source compiles under: the latest edition, strict (the CLI's
    /// <c>--std</c> default, <see cref="Editions.EditionInfo.Latest"/>), and every other option at its library default.</summary>
    private static readonly CompilerDriver.Options Defaults = new("", Editions.EditionInfo.Latest);

    private static readonly ListingRequest RequestDefaults = new();

    /// <summary>The two command-line options <see cref="CompilerDriver.Options.Edition"/> carries, each a row of its own: the
    /// year (<c>--std</c>) and the severity axis (<c>--permissive</c>), named as the property path that reads them.</summary>
    internal const string EditionYear = nameof(CompilerDriver.Options.Edition) + "." + nameof(Editions.EditionInfo.Year);

    /// <inheritdoc cref="EditionYear"/>
    internal const string EditionPermissive = nameof(CompilerDriver.Options.Edition) + "." + nameof(Editions.EditionInfo.Permissive);

    /// <summary>The rows of <see cref="CompilerDriver.Options"/>, in the order the command line's help lists the options.</summary>
    internal static readonly IReadOnlyList<Row> Table =
    [
        new(nameof(CompilerDriver.Options.SourcePath), "source", o => o.SourcePath, _ => false),
        new(nameof(CompilerDriver.Options.OutputPath), "--output", o => o.OutputDll(), o => o.OutputPath is null,
            nameof(CompilerDriver.Options.SourcePath)),
        new(nameof(CompilerDriver.Options.NistTestName), "--nist", o => o.NistTestName ?? "off"),
        new(EditionYear, "--std", o => string.Create(CultureInfo.InvariantCulture,
            $"{o.Edition.Year} ({o.Edition.StandardName})")),
        new(EditionPermissive, "--permissive", o => OnOff(o.Edition.Permissive)),
        new(nameof(CompilerDriver.Options.CopyPaths), "--copy", o => List(o.CopyPaths)),
        new(nameof(CompilerDriver.Options.SignEncoding), "--sign-encoding", o => o.SignEncoding.ToString().ToLowerInvariant()),
        new(nameof(CompilerDriver.Options.NumericDisplay), "--numeric-display",
            o => Binding.Model.NumericDisplayFormOption.OptionSpellings[(int)o.NumericDisplay]),
        new(nameof(CompilerDriver.Options.SourceFormat), "--source-format",
            o => Frontend.Preprocessor.InitialReferenceFormatOption.OptionSpellings[(int)o.SourceFormat]),
        new(nameof(CompilerDriver.Options.FlagExtensions), "--flag-extensions", o => OnOff(o.FlagExtensions)),
        new(nameof(CompilerDriver.Options.Suppress), "--suppress",
            o => o.Suppress is { Count: > 0 } names ? string.Join(",", names) : "none"),
        new(nameof(CompilerDriver.Options.RepositoryUpdate), "--repository-update", o => OnOff(o.RepositoryUpdate)),
        new(nameof(CompilerDriver.Options.RepositoryPaths), "--repository",
            o => o.RepositoryPaths is null ? "the output directory" : List(o.RepositoryPaths)),
        new(nameof(CompilerDriver.Options.ReferencePaths), "--reference", o => List(o.ReferencePaths)),
        new(nameof(CompilerDriver.Options.RepositoryCheck), "--repository-check", o => OnOff(o.RepositoryCheck)),
        new(nameof(CompilerDriver.Options.CheckOnly), "check-only", o => OnOff(o.CheckOnly)),
        new(nameof(CompilerDriver.Options.Listing), "--listing", o => OnOff(o.Listing is not null)),
    ];

    /// <summary>The rows of the listing request (<see cref="CompilerDriver.Options.Listing"/>), after the row that says a listing
    /// is produced.</summary>
    internal static readonly IReadOnlyList<RequestRow> RequestTable =
    [
        new(nameof(ListingRequest.OutputBase), "--listing-output", r => r.OutputBase ?? "the output assembly's path without its extension"),
        new(nameof(ListingRequest.Formats), "--listing-format", r => string.Join(",", ListingOutput.Each(r.Formats).Select(ListingOutput.Spelling))),
        new(nameof(ListingRequest.Sections), "--listing-sections", r => r.Sections.ToString()),
        new(nameof(ListingRequest.PageLength), "--listing-page-length", r => r.PageLength.ToString(CultureInfo.InvariantCulture)),
        new(nameof(ListingRequest.PageWidth), "--listing-page-width", r => r.PageWidth.ToString(CultureInfo.InvariantCulture)),
    ];

    /// <summary>The options section of a compilation of <paramref name="options"/>: every row of the table, each
    /// <see cref="ListingOptionOrigin.Default"/> when its value is spelled as the default's is; then the compiler and the host.</summary>
    public static IReadOnlyList<ListingOption> Build(CompilerDriver.Options options)
    {
        var rows = new List<ListingOption>(Table.Count + RequestTable.Count + 4);
        foreach (var row in Table)
        {
            string value = row.Value(options);
            bool isDefault = row.IsDefault?.Invoke(options) ?? value == row.Value(Defaults);
            rows.Add(new ListingOption(row.Name, value, isDefault ? ListingOptionOrigin.Default : ListingOptionOrigin.Specified));
        }
        if (options.Listing is { } request)
            foreach (var row in RequestTable)
            {
                string value = row.Value(request);
                rows.Add(new ListingOption(row.Name, value,
                    value == row.Value(RequestDefaults) ? ListingOptionOrigin.Default : ListingOptionOrigin.Specified));
            }
        rows.Add(new ListingOption("compiler", Listing.ListingBuilder.CompilerVersion, ListingOptionOrigin.Host));
        rows.Add(new ListingOption("runtime", Listing.ListingBuilder.VersionOf(typeof(Runtime.SignEncoding).Assembly), ListingOptionOrigin.Host));
        rows.Add(new ListingOption(".NET", RuntimeInformation.FrameworkDescription, ListingOptionOrigin.Host));
        rows.Add(new ListingOption("operating system",
            $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})", ListingOptionOrigin.Host));
        return rows;
    }

    private static string OnOff(bool on) => on ? "on" : "off";

    /// <summary>A list of paths, in order, joined by the platform's path-list separator (as PATH joins them).</summary>
    private static string List(IReadOnlyList<string>? items) =>
        items is { Count: > 0 } ? string.Join(Path.PathSeparator, items) : "none";
}
