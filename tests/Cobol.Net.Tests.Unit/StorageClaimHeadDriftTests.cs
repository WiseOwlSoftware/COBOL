// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ EVERY STORAGE CLAIM RESOLVES ITS HEAD THROUGH THE ONE CONTAINED-AWARE RESOLVER (kb/Work PB3286). The data pass puts
/// the record named by an <c>ADDRESS OF</c> head or a BY REFERENCE operand on addressable cell storage, and a head written
/// in a CONTAINED program can only name the container's GLOBAL names (ISO §13.18.27.4 GR2; §8.4.6.2.1). The two arms each
/// resolved their head in the whole container scope — the ADDRESS OF arm took candidate 0, the BY REFERENCE arm required
/// exactly one — so a container-local record sharing a name with a GLOBAL one hid the global record from the claim and
/// the contained <c>SET P TO ADDRESS OF X</c> was refused. <c>DataBinder.Ptr.cs</c> carries the ONE resolver
/// (<c>ResolveStorageClaimHead</c>), both arms ask it with their <c>contained</c> flag, and the two private copies are gone.
/// </summary>
public sealed class StorageClaimHeadDriftTests
{
    [Fact]
    public void BothClaimArms_AskTheOneResolver_WithTheirContainedFlag()
    {
        string text = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "Binding", "DataBinder.Ptr.cs"));
        Assert.Contains("ResolveStorageClaimHead(name, quals, method, contained, firstOfSeveral: true)", text);
        Assert.Contains("ResolveStorageClaimHead(name, quals, method, contained, firstOfSeveral: false)", text);
        Assert.DoesNotContain("ResolveByReferenceOperand(", text);
        Assert.DoesNotContain("PtrResolveAddressOfTarget(", text);
    }

    [Fact]
    public void TheResolver_FiltersAContainedHeadToTheGlobalRoots()
    {
        string text = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "Binding", "DataBinder.Ptr.cs"));
        int at = text.IndexOf("private DataItem? ResolveStorageClaimHead(", StringComparison.Ordinal);
        Assert.True(at > 0, "ResolveStorageClaimHead is gone");
        Assert.Contains("CallGlobalRoots.Contains(c.Root)", text[at..Math.Min(text.Length, at + 1500)]);
    }
}
