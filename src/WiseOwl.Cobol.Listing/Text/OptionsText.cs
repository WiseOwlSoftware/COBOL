// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

namespace WiseOwl.Cobol.Listing.Text;

/// <summary>The options section of the text listing (DESIGN-listing.md section 6.4): one row per option, its name, where its
/// value comes from and its value.</summary>
public static class OptionsText
{
    /// <summary>The options section's rows (DESIGN-listing.md section 6.4): each option's name, where its value comes from
    /// (<c>default</c>, <c>specified</c>, or <c>host</c> for a fact of the compiler or the host) and its value, a long value
    /// wrapped under itself.</summary>
    public static IEnumerable<string> Rows(IReadOnlyList<ListingOption> options, int width)
    {
        yield return TextListingRenderer.Columns((Headings[0], OptionNameWidth), (Headings[1], OriginWidth)) + Headings[2];
        foreach (var option in options)
        {
            // The value starts on the option's row and goes on under itself.
            string lead = TextListingRenderer.Columns((option.Name, OptionNameWidth), (OriginWord(option.Origin), OriginWidth));
            bool first = true;
            foreach (string piece in TextListingRenderer.Indented(option.Value, 0, 0, width - lead.Length))
            {
                yield return (first ? lead : new string(' ', lead.Length)) + piece;
                first = false;
            }
        }
    }

    private const int OptionNameWidth = 24, OriginWidth = 11;

    /// <summary>The options section's headings: the option, where its value comes from, and its value.</summary>
    public static readonly IReadOnlyList<string> Headings = ["OPTION", "ORIGIN", "VALUE"];

    /// <summary>The word an option row's origin is printed as.</summary>
    public static string OriginWord(ListingOptionOrigin origin) => origin switch
    {
        ListingOptionOrigin.Default => "default",
        ListingOptionOrigin.Specified => "specified",
        ListingOptionOrigin.Host => "host",
        _ => throw new ArgumentOutOfRangeException(nameof(origin), origin, "an option origin with no word"),
    };
}
