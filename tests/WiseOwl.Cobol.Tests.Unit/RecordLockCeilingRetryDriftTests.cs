// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE §12.4.5.9.4 GR7 RECORD-LOCK CEILING IS JUDGED IN ONE PLACE, UNDER THE STATEMENT'S RETRY PHRASE, AND NO
/// GOVERNED VERB DISCARDS THE PHRASE IT WAS HANDED (kb/Work PB707). '53' and '54' are record operation conflict
/// conditions by §9.1.13.8's own heading (<c>cite.py --check 9.1.13.8 "this file connector holds the maximum number of
/// locks"</c> → OK §9.1.13.8 4)), §14.7.9.3 GR4 engages on every such condition (<c>cite.py --check 14.7.9.3 "unsuccessful
/// on the first attempt because of a file sharing conflict condition or a record operation conflict condition"</c> → OK
/// §14.7.9.3 4)), and <c>FileRegistry.RetryLoop</c> is the one place that discipline is written. The ceiling used to be
/// asked straight from <c>PhysicalFileTable.PreflightNewLock</c> at three call sites, WRITE's with
/// <c>_ = retryKind; _ = retryAmount;</c> beside it, so the rule lived at the call sites rather than in the loop. The
/// answer was the same either way (a ceiling is this connector's and this run unit's own count, which no re-attempt
/// inside the executing statement lowers, and <c>ExhaustionStatus</c> returns a non-'51' conflict unchanged), which is
/// why only the placement is pinned: <c>FileRegistry.cs</c> calls <c>PreflightNewLock</c> exactly once, inside
/// <c>RecordLockCeiling</c>, which wraps it in <c>RetryLoop</c>, and discards no <c>retryKind</c> or
/// <c>retryAmount</c> parameter. The behavior half is <c>CobolFileLockTests</c>
/// (<c>LockCeiling_OnWriteRewriteAndRead_LandsTheCeilingsOwnStatus_UnderEveryRetryForm</c> and the '53'/'54' rows of
/// the <c>RetryLoop</c> class and attempt-count theories).
/// </summary>
public sealed class RecordLockCeilingRetryDriftTests
{
    private static string Code()
    {
        string path = TestRepo.Src("WiseOwl.Cobol.Runtime", "IO", "FileRegistry.cs");
        return string.Join("\n", File.ReadAllLines(path).Select(l => l.Split("//", 2)[0]));
    }

    [Fact]
    public void PreflightNewLock_IsAskedOnlyThroughRecordLockCeiling_WhichWrapsItInTheRetryLoop()
    {
        string code = Code();
        Assert.Single(Regex.Matches(code, @"\.PreflightNewLock\("));
        Assert.Matches(
            new Regex(@"private string RecordLockCeiling\([^)]*\)\s*=>\s*RetryLoop\(\(\) => RetryAttempt\.InRunUnit\(_physical\.PreflightNewLock\(st, name\)\), retryKind, retryAmount\);",
                RegexOptions.NonBacktracking),
            code);
    }

    [Fact]
    public void NoGovernedVerb_DiscardsItsRetryPhrase()
    {
        Assert.DoesNotMatch(new Regex(@"_\s*=\s*retry(Kind|Amount)\s*;", RegexOptions.NonBacktracking), Code());
    }
}
