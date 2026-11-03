// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.CodeGen;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ <see cref="OutputFileProtocol.Replace"/> ON ITS ABNORMAL PATHS (DESIGN-external-repository §7.5; kb/Work PB2103, review
/// R1-3): an output directory is a repository directory another compilation or a running program may be reading, so a
/// produced file is written beside its target and renamed over it, and every way that can fail leaves the previous build in
/// place and no temporary behind. The success path replaces the target. A failed write returns false and touches nothing; a
/// write that throws propagates and leaves nothing; a target that cannot be replaced (here a directory stands where the file
/// goes, which fails the rename on every OS) is retried the bounded number of times and then fails LOUDLY with an
/// <see cref="IOException"/> that names the file, never silently with the old content.
/// </summary>
public sealed class OutputFileProtocolTests
{
    private static void WithDirectory(Action<string> body)
    {
        string dir = ScratchDirectory.Create("output-protocol-");
        try { body(dir); }
        finally { ScratchDirectory.Remove(dir); }
    }

    private static string[] Left(string dir) => Directory.GetFileSystemEntries(dir).Select(Path.GetFileName).Order().ToArray()!;

    [Fact]
    public void ASuccessfulWrite_ReplacesTheTarget_AndLeavesNoTemporary()
    {
        WithDirectory(dir =>
        {
            string target = Path.Combine(dir, "lib.dll");
            File.WriteAllText(target, "previous build");
            bool result = OutputFileProtocol.Replace(target, temporary => { File.WriteAllText(temporary, "new build"); return true; });
            Assert.True(result);
            Assert.Equal("new build", File.ReadAllText(target));
            Assert.Equal(["lib.dll"], Left(dir));
        });
    }

    [Fact]
    public void AWriteThatFails_LeavesThePreviousBuild_AndNoTemporary()
    {
        WithDirectory(dir =>
        {
            string target = Path.Combine(dir, "lib.dll");
            File.WriteAllText(target, "previous build");
            bool result = OutputFileProtocol.Replace(target, temporary => { File.WriteAllText(temporary, "half"); return false; });
            Assert.False(result);
            Assert.Equal("previous build", File.ReadAllText(target));
            Assert.Equal(["lib.dll"], Left(dir));
        });
    }

    [Fact]
    public void AWriteThatThrows_PropagatesAndLeavesThePreviousBuildAndNoTemporary()
    {
        WithDirectory(dir =>
        {
            string target = Path.Combine(dir, "lib.dll");
            File.WriteAllText(target, "previous build");
            var thrown = Assert.Throws<InvalidOperationException>(() => OutputFileProtocol.Replace(target, temporary =>
            {
                File.WriteAllText(temporary, "half");
                throw new InvalidOperationException("emit failed");
            }));
            Assert.Equal("emit failed", thrown.Message);
            Assert.Equal("previous build", File.ReadAllText(target));
            Assert.Equal(["lib.dll"], Left(dir));
        });
    }

    [Fact]
    public void ATargetThatCannotBeReplaced_FailsLoudlyNamingTheFile_AndLeavesNoTemporary()
    {
        WithDirectory(dir =>
        {
            string target = Path.Combine(dir, "lib.dll");
            Directory.CreateDirectory(target);   // a directory where the file goes: the rename fails however often it is retried
            var failure = Assert.Throws<IOException>(() =>
                OutputFileProtocol.Replace(target, temporary => { File.WriteAllText(temporary, "new build"); return true; }));
            Assert.Contains(target, failure.Message, StringComparison.Ordinal);
            Assert.Contains($"{OutputFileProtocol.Attempts} attempts", failure.Message, StringComparison.Ordinal);
            Assert.NotNull(failure.InnerException);
            Assert.Equal(["lib.dll"], Left(dir));   // the target directory only: the temporary is gone
        });
    }
}
