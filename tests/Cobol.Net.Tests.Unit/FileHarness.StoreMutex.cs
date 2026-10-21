// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;
using Xunit;

namespace CobolNet.Tests.Shared;

/// <summary>
/// The half of <see cref="FileHarness"/> that reaches the runtime's INTERNAL surface (kb/Work PB3472). The class
/// itself is <c>tests/_shared/FileHarness.cs</c>, linked into every test project; the region host
/// (<c>HostRegionLocks</c>) is visible to the Unit assembly only (the runtime's <c>InternalsVisibleTo</c>), so the
/// store-mutex probe is declared here, in the same partial class, and only the Unit project has it.
/// </summary>
public static partial class FileHarness
{
    /// <summary>What another run unit's connector meets at the store mutex: a second handle on the file, opened as
    /// that run unit's would be (<paramref name="writable"/>: write access, else read access), asks the region host
    /// whether ANOTHER handle holds it. The answer is the host's, so a failing host fails the test here.</summary>
    public static bool MutexHeld(string host, bool writable = false)
    {
        using var probe = new FileStream(host, FileMode.Open, writable ? FileAccess.ReadWrite : FileAccess.Read,
            FileShare.ReadWrite);
        using var regions = HostRegionLocks.Open(probe.SafeFileHandle, host, writable);
        var answer = regions.HeldByAnother(HostRegionLocks.StoreMutexByte);
        Assert.True(answer is RegionLockResult.Held or RegionLockResult.Ok, $"the region host answered {answer}");
        return answer == RegionLockResult.Held;
    }
}
