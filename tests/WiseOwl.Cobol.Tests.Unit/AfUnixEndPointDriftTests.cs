// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE RUNTIME FORMS AN AF_UNIX ENDPOINT IN ONE PLACE, <c>McsSocketDirectory.EndPointOf</c> (kb/Work PB4742; DESIGN-message-control.md §6 "The budget").
/// .NET's constructor answers a path over the OS limit with an <see cref="ArgumentOutOfRangeException"/>, which no caller's contract allows: a path comes from the host's
/// temp directory or <c>WISEOWL_COBOL_MCS_NOTIFY</c>, and a start that fails is EC-MCS-IMP with the reason (design 5.5). A direct construction anywhere else in <c>src/</c>
/// is one more place that exception can escape, which is how a notify path over the limit reached the program as a crash, and the communication facility's bell will need the same answer.
/// </summary>
[Trait("Sweep", "tree")]
public sealed class AfUnixEndPointDriftTests
{
    private const string Home = "src/WiseOwl.Cobol.Runtime/Mcs/McsSocketDirectory.cs";

    private static readonly TreeScope ProductSources = new(Under: ["src/"], Kinds: [".cs"]);

    [Fact]
    public void OnlyTheSocketDirectoryConstructsAnAfUnixEndPoint()
    {
        var constructing = TrackedTree.Files(ProductSources)
            .Where(file => File.ReadAllText(TestRepo.Within("src/", file)).Contains("new UnixDomainSocketEndPoint", StringComparison.Ordinal))
            .ToList();
        Assert.Equal([Home], constructing);       // equal, not "no others": a detector that stopped finding the one site would go red here
    }
}
