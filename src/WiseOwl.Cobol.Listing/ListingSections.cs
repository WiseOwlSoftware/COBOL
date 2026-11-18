// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Numerics;

namespace WiseOwl.Cobol.Listing;

/// <summary>
/// A set of listing sections (<see cref="ListingRequest.Sections"/>; DESIGN-listing.md section 8). It is a set of
/// <see cref="ListingSection"/>, the one enum of sections, so <see cref="All"/> holds a section the day it joins the enum and no
/// second list of sections exists. A value: two sets of the same sections are equal.
/// </summary>
public readonly record struct ListingSectionSet
{
    private readonly ulong _bits;

    private ListingSectionSet(ulong bits) => _bits = bits;

    /// <summary>The set of <paramref name="sections"/>.</summary>
    public static ListingSectionSet Of(params ReadOnlySpan<ListingSection> sections)
    {
        ulong bits = 0;
        foreach (var section in sections) bits |= Bit(section);
        return new ListingSectionSet(bits);
    }

    /// <summary>Every section (<c>all</c>).</summary>
    public static ListingSectionSet All { get; } = Of([.. Enum.GetValues<ListingSection>()]);

    /// <summary>The sections of a listing by default (<c>standard</c>; DESIGN-listing.md sections 8 and 13 item 1): the source,
    /// the diagnostics, the options, the program map and the statistics.</summary>
    public static ListingSectionSet Standard { get; } =
        Of(ListingSection.Source, ListingSection.Diagnostics, ListingSection.Options, ListingSection.Programs, ListingSection.Statistics);

    /// <summary>Whether the set holds <paramref name="section"/>.</summary>
    public bool Contains(ListingSection section) => (_bits & Bit(section)) != 0;

    /// <summary>The set and <paramref name="other"/>'s sections.</summary>
    public ListingSectionSet Union(ListingSectionSet other) => new(_bits | other._bits);

    /// <summary>Whether the set holds no section.</summary>
    public bool IsEmpty => _bits == 0;

    /// <summary>The sections of the set, in the order of <see cref="ListingSection"/>.</summary>
    public IReadOnlyList<ListingSection> Each()
    {
        var bits = _bits;
        return [.. Enum.GetValues<ListingSection>().Where(s => (bits & Bit(s)) != 0)];
    }

    /// <summary>The set as <c>--listing-sections</c> spells it: <c>standard</c> or <c>all</c> when it is one of them (in that
    /// order), else its sections' spellings joined by commas in the order of <see cref="ListingSection"/>.</summary>
    public override string ToString() =>
        this == Standard ? ListingSections.StandardSpelling
            : this == All ? ListingSections.AllSpelling
            : string.Join(",", Each().Select(ListingSections.Spelling));

    private static ulong Bit(ListingSection section) =>
        (int)section is >= 0 and < 64 ? 1UL << (int)section
            : throw new ArgumentOutOfRangeException(nameof(section), section, "not one listing section");

    /// <summary>The number of sections in the set.</summary>
    public int Count => BitOperations.PopCount(_bits);
}

/// <summary>
/// ⛔ THE ONE TABLE OF SECTION SPELLINGS (DESIGN-listing.md section 8): each section's <c>--listing-sections</c> word, and the
/// parser of a comma list of them. The command line's help, its validator and the options section's value of the request all
/// read it, so a section added to <see cref="ListingSection"/> is spelled here once and reaches each; <see cref="Spelling"/>
/// throws on a member it does not know, and the Unit assembly's <c>ListingSectionsTests</c> calls it for every member.
/// </summary>
public static class ListingSections
{
    /// <summary>The word for <see cref="ListingSectionSet.Standard"/>.</summary>
    public const string StandardSpelling = "standard";

    /// <summary>The word for <see cref="ListingSectionSet.All"/>.</summary>
    public const string AllSpelling = "all";

    /// <summary>The command-line word of one section.</summary>
    public static string Spelling(ListingSection section) => section switch
    {
        ListingSection.Source => "source",
        ListingSection.Programs => "programs",
        ListingSection.Diagnostics => "diagnostics",
        ListingSection.Options => "options",
        ListingSection.Statistics => "statistics",
        ListingSection.DataMap => "data-map",
        _ => throw new ArgumentOutOfRangeException(nameof(section), section, "not one listing section"),
    };

    /// <summary>Every word a list may hold, as a help text lists them: each section's, then <c>standard</c> and <c>all</c>.</summary>
    public static string Spellings =>
        string.Join(",", Enum.GetValues<ListingSection>().Select(Spelling).Append(StandardSpelling).Append(AllSpelling));

    /// <summary>The sections <paramref name="list"/> names: a comma-separated list of words (<see cref="Spellings"/>; ordinal,
    /// ignoring case and the spaces around a word), <c>standard</c> and <c>all</c> standing for their sets. False, with
    /// <paramref name="problem"/> saying why, when a word is empty or unknown.</summary>
    public static bool TryParse(string list, out ListingSectionSet sections, out string? problem)
    {
        sections = default;
        problem = null;
        var parsed = default(ListingSectionSet);
        foreach (string piece in list.Split(','))
        {
            string word = piece.Trim();
            if (word.Length == 0) { problem = $"a listing section list names a section between each two commas (got '{list}')"; return false; }
            if (WordIs(word, StandardSpelling)) parsed = parsed.Union(ListingSectionSet.Standard);
            else if (WordIs(word, AllSpelling)) parsed = parsed.Union(ListingSectionSet.All);
            else if (SectionSpelled(word) is { } section) parsed = parsed.Union(ListingSectionSet.Of(section));
            else { problem = $"a listing section is one of {Spellings} (got '{word}')"; return false; }
        }
        sections = parsed;
        return true;
    }

    private static ListingSection? SectionSpelled(string word)
    {
        foreach (var section in Enum.GetValues<ListingSection>())
            if (WordIs(word, Spelling(section))) return section;
        return null;
    }

    private static bool WordIs(string word, string spelling) =>
        string.Equals(word, spelling, StringComparison.OrdinalIgnoreCase);   // not a COBOL word: a command-line section name
}
