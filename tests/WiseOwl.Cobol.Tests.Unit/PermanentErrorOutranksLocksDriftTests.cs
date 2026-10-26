// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A PERMANENT ERROR IN EFFECT ASKS NO §9.1.16 LOCK QUESTION, AND THAT RULE IS WRITTEN IN ONE PLACE,
/// <c>FileRegistry.PermanentErrorOutranksLocks</c>, WHICH THE READ PEEKS AND THE REWRITE / DELETE TARGET BOTH BEGIN WITH
/// (kb/Work PB3416). §9.1.13.1 keeps the condition <i>"in effect for all subsequent input-output operations on the file"</i>
/// and docs/CONFORMANCE.md DOC-A.1-105 makes it outrank the '51' a record another connector holds would answer. The rule
/// was first written for the READ peeks (<c>NoLockQuestion</c>) and then again, as a second guard beside
/// <c>FileConnector.MutationTargetRecordId</c> (which already answers "" under the condition), in the REWRITE body and in the
/// DELETE body: three writings of one rule, and the governance check still asked the host's presence byte for a statement
/// the condition had already settled. Pinned here, on <c>FileRegistry.cs</c> with its comments stripped: the one
/// <c>PermanentErrorInEffect is not null</c> test is the body of <c>PermanentErrorOutranksLocks</c>; <c>NoLockQuestion</c>
/// asks it; the one <c>MutationTargetRecordId(</c> call is inside <c>MutationTarget</c>, which asks it before
/// <c>RecordLocksGovern</c> and which the REWRITE and the DELETE body both call. The behavior half is
/// <c>PermanentErrorOutranksRecordLockTests</c>.
/// </summary>
public sealed class PermanentErrorOutranksLocksDriftTests
{
    private static string Code()
    {
        string path = TestRepo.Src("WiseOwl.Cobol.Runtime", "IO", "FileRegistry.cs");
        return string.Join("\n", File.ReadAllLines(path).Select(l => l.Split("//", 2)[0]));
    }

    [Fact]
    public void ThePermanentErrorTest_IsWrittenOnce_AsPermanentErrorOutranksLocks()
    {
        string code = Code();
        Assert.Single(Regex.Matches(code, @"PermanentErrorInEffect\s+is\s+not\s+null"));
        Assert.Matches(
            new Regex(@"private static bool PermanentErrorOutranksLocks\(FileConnector c\)\s*=>\s*c\.PermanentErrorInEffect is not null;",
                RegexOptions.NonBacktracking),
            code);
        Assert.DoesNotMatch(new Regex(@"PermanentErrorInEffect\s+is\s+null", RegexOptions.NonBacktracking), code);
    }

    [Fact]
    public void TheReadPeeks_AskItThroughNoLockQuestion()
    {
        Assert.Matches(
            new Regex(@"private static bool NoLockQuestion\([^)]*\)\s*=>\s*PermanentErrorOutranksLocks\(c\) \|\| NoRecordIsLocked\(st, c\);",
                RegexOptions.NonBacktracking),
            Code());
    }

    [Fact]
    public void TheMutationTarget_IsTakenInOnePlace_ThatAsksItBeforeTheLockGovernance_AndRewriteAndDeleteBothCallIt()
    {
        string code = Code();
        Assert.Single(Regex.Matches(code, @"\.MutationTargetRecordId\("));
        Assert.Matches(
            new Regex(@"private static string MutationTarget\([^)]*\)\s*=>\s*!PermanentErrorOutranksLocks\(c\) && RecordLocksGovern\(meta, st, name, c\) \? c\.MutationTargetRecordId\(image, extents\) : """";",
                RegexOptions.NonBacktracking),
            code);
        Assert.Equal(2, Regex.Matches(code, @"= MutationTarget\(meta, st, name, c,").Count);
    }
}
