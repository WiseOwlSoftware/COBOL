// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance.Listing;

/// <summary>
/// The standard's listing hooks at <c>--std 2002</c>, judged by the 2002 text (DESIGN-listing.md section 10 item 2; step 4b of
/// section 11). §2002:7.2.15.2 GR1: "Whether the compiler produces a source listing is implementor defined"; this compiler
/// produces one on request (DOC-A.1-117), so GR2 to GR5 apply. These cases are the witnesses of the twelve 2002 listing rows of
/// section 2.2. 2002 has no PUSH, POP or DISPLAY directive and no exception-checking PERFORM, so those 2023 cases have no 2002
/// counterpart; DATE-COMPILED is removed (§2002:F.1), so the paragraph's replacement is a 1985 case.
/// </summary>
public sealed class Listing2002Tests : IDisposable
{
    private readonly string _dir = ScratchDirectory.Create("listing2002");
    private readonly ListingHookCases _cases;

    public Listing2002Tests() => _cases = new ListingHookCases(_dir, 2002);

    public void Dispose() => ScratchDirectory.Remove(_dir);

    /// <summary>
    /// §2002:7.2.15.2 GR2: "The default LISTING directive is '>>LISTING ON'"; GR3: "Each LISTING directive shall be listed, even if
    /// the listing is being suppressed by a LISTING directive"; GR4: "If OFF is specified, source lines shall not be listed until a
    /// LISTING directive specifying or implying the ON phrase is encountered, with the exception that another LISTING OFF directive
    /// shall be listed"; GR5: "If ON is specified or implied, source lines shall be listed until either a LISTING OFF directive is
    /// encountered or the end of the compilation group is reached". A bare <c>&gt;&gt;LISTING</c> implies ON (§2002:7.2.15.1: ON is
    /// not underlined).
    /// </summary>
    [Fact]
    public void ListingOff_StopsTheLinesAfterIt_UntilOn_AndEveryListingDirectiveIsListed() => _cases.ListingOffThenOn();

    /// <summary>
    /// §2002:7.2.16.3 GR2: "If a source listing is being produced, a PAGE directive shall cause page ejection followed by listing of
    /// the PAGE directive" (§2002:7.2.16.1: <c>&gt;&gt;PAGE [ comment-text-1 ]</c>). Under LISTING OFF the PAGE line is a source line
    /// §2002:7.2.15.2 GR4 stops and GR3 does not exempt.
    /// </summary>
    [Fact]
    public void APageDirective_EjectsAPage_ThenIsListed_ButNotUnderListingOff() => _cases.PageDirective();

    /// <summary>§2002:7.2.16.3 GR3: "If a source listing is not being produced, a PAGE directive shall have no effect".</summary>
    [Fact]
    public void APageDirective_WithNoListing_HasNoEffect() => _cases.PageDirective_WithNoListing_HasNoEffect();

    /// <summary>
    /// §2002:7.1.2.3 GR4: "If the SUPPRESS phrase is specified, library text incorporated as a result of COPY statement processing is
    /// not listed. If a listing is being produced, the COPY statement itself is listed."
    /// </summary>
    [Fact]
    public void CopySuppress_ListsTheCopyStatement_ButNotTheLibraryText() => _cases.CopySuppress();

    /// <summary>
    /// §2002:7.1.2.3 GR5: "At the completion of copying the library text into the compilation group, the LISTING directive that is in
    /// effect for the COPY statement itself is considered to be in effect, regardless of any LISTING directives in the library text."
    /// </summary>
    [Fact]
    public void TheListingStateAtTheCopy_IsInEffectAfterTheLibraryText() => _cases.ListingStateAtTheEndOfLibraryText();

    /// <summary>
    /// §2002:6.2.6.1: "If a source listing is being produced, a comment line identified by the fixed comment indicator slant (/)
    /// causes page ejection followed by printing of the comment line; comments identified by the fixed comment indicator asterisk
    /// (*) are printed at the next available line position of the listing".
    /// </summary>
    [Fact]
    public void ASlantLine_StartsAPage_AndAnAsteriskLineFollowsItsPredecessor() => _cases.SlantAndAsteriskLines();

    /// <summary>
    /// §2002:15.80.2 RV3: "The returned value shall denote the same time as the compilation date and time if provided in the
    /// listing and in the generated object code"; RV1 gives the 21 positions.
    /// </summary>
    [Fact]
    public void WhenCompiled_DenotesTheInstantTheListingCarries() => _cases.WhenCompiled_DenotesTheListingsInstant();
}
