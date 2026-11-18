// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary><see cref="TestRepo.MsBuildPath"/> resolves a path written in an MSBuild file the way MSBuild does on every OS
/// (kb/Work PB1957: merge-group run 44268547019 failed on Linux, where a csproj's backslash Include was one file name).</summary>
public sealed class TestRepoMsBuildPathTests
{
    [Theory]
    [InlineData(@"..\WiseOwl.Cobol.Runtime\WiseOwl.Cobol.Runtime.csproj")]
    [InlineData("../WiseOwl.Cobol.Runtime/WiseOwl.Cobol.Runtime.csproj")]
    [InlineData(@"..\WiseOwl.Cobol.Runtime/WiseOwl.Cobol.Runtime.csproj")]
    public void EitherSeparator_ResolvesAgainstTheWritingFile_ToTheSameFullPath(string written)
    {
        string compiler = TestRepo.Src("WiseOwl.Cobol.Compiler");
        string resolved = TestRepo.MsBuildPath(compiler, written);
        Assert.Equal(Path.GetFullPath(TestRepo.Src("WiseOwl.Cobol.Runtime", "WiseOwl.Cobol.Runtime.csproj")), resolved);
        // On an OS whose separator is '/', a backslash left in the path would be part of a file name.
        Assert.DoesNotContain(Path.DirectorySeparatorChar == '/' ? '\\' : '/', resolved);
        Assert.Equal("WiseOwl.Cobol.Runtime.csproj", Path.GetFileName(resolved));
    }
}
