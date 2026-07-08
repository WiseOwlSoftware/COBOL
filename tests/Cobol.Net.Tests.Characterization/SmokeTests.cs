// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Characterization;

/// <summary>
/// A build-anchoring smoke fact so the Phase-0 characterization project is real and non-empty before the diagnostic
/// (step 5) and emitted-C# (step 6) snapshot tests land. The real gates arrive in <c>DiagnosticSnapshotTests</c> and
/// <c>EmittedCSharpSnapshotTests</c>.
/// </summary>
public sealed class SmokeTests
{
    [Fact]
    public void Harness_IsWired() => Assert.True(true);
}
