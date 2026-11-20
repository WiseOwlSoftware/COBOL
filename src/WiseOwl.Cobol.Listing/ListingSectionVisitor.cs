// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Listing;

/// <summary>
/// ⛔ ONE METHOD PER SECTION (DESIGN-listing.md section 7): every renderer that walks the document's sections implements this
/// interface, so a member added to <see cref="ListingSection"/> with its method here is a compile error in every renderer that has
/// not written it. A visitor is made for one rendering and holds the document and the request; each method returns that
/// renderer's form of one PRODUCED section (the text listing's rows, the HTML page's markup). A section asked for and not
/// produced is printed by the renderer from its <see cref="ListingSectionStatus.Reason"/> and visits nothing.
/// </summary>
/// <typeparam name="T">What the renderer makes of a section.</typeparam>
public interface IListingSectionVisitor<out T>
{
    /// <summary>The source section: every line, with its diagnostics and the DISPLAY directive's transfers.</summary>
    T Source();

    /// <summary>The program map.</summary>
    T Programs();

    /// <summary>The diagnostic summary.</summary>
    T Diagnostics();

    /// <summary>The options in effect.</summary>
    T Options();

    /// <summary>The statistics and the verbs.</summary>
    T Statistics();

    /// <summary>The data map and the file map.</summary>
    T DataMap();

    /// <summary>The cross-reference and the PERFORM and CALL graphs.</summary>
    T CrossReference();

    /// <summary>The object-code map.</summary>
    T ObjectMap();

    /// <summary>The object code.</summary>
    T ObjectCode();
}

/// <summary>The one dispatch from a <see cref="ListingSection"/> to its <see cref="IListingSectionVisitor{T}"/> method. It throws
/// on a member it does not know, and the Unit assembly's <c>ListingSectionsTests</c> calls it for every member, so a section added
/// to the enum without its method's arm here is red at test time and one without its method in a renderer does not compile.</summary>
public static class ListingSectionVisitor
{
    /// <summary><paramref name="visitor"/>'s form of <paramref name="section"/>.</summary>
    public static T Visit<T>(this IListingSectionVisitor<T> visitor, ListingSection section) => section switch
    {
        ListingSection.Source => visitor.Source(),
        ListingSection.Programs => visitor.Programs(),
        ListingSection.Diagnostics => visitor.Diagnostics(),
        ListingSection.Options => visitor.Options(),
        ListingSection.Statistics => visitor.Statistics(),
        ListingSection.DataMap => visitor.DataMap(),
        ListingSection.CrossReference => visitor.CrossReference(),
        ListingSection.ObjectMap => visitor.ObjectMap(),
        ListingSection.ObjectCode => visitor.ObjectCode(),
        _ => throw new ArgumentOutOfRangeException(nameof(section), section, "a listing section with no visitor method"),
    };
}
