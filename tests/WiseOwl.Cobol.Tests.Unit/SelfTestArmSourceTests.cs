// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// The arm reader the compiler's own generator and extractor tests use (<see cref="SelfTestArmSource"/>: the census's
/// and the derived-verdict batch's declared arms, kb/Work PB4395 item 2) is seen to fire: a planted deleted arm is
/// reported by name, a label split across two literals (an f-string's among them, after a CRLF) is found whole, and a
/// present label is not reported. It reads no file. The pipeline scripts' arms are <c>scripts/self_test_arms.py</c>'s,
/// whose own self-test plants the same cases (kb/Work PB4395 item 1).
/// </summary>
public sealed class SelfTestArmSourceTests
{
    [Fact]
    public void TheArmReader_ReportsADeletedArm_AndFindsASplitLabelWhole()
    {
        string planted = string.Join("\n",
            "    check(\"arm: a failing self-test is RED\", got, want)",
            "    check(\"arm: a changed input re-runs the self-test that read it; the others are \"",
            "          \"REUSED in every gate\", got, want)",
            "    check(f\"arm: {_FLEET_GATES} gates at once run at most {_FLEET_CAP} \"\r",
            "          f\"at a time, and none hangs\", got, want)");
        string[] arms =
        [
            "arm: a failing self-test is RED",
            "arm: a changed input re-runs the self-test that read it; the others are REUSED in every gate",
            "arm: {_FLEET_GATES} gates at once run at most {_FLEET_CAP} at a time, and none hangs",
            "arm: one red makes the verdict RED",
        ];
        Assert.Equal(["arm: one red makes the verdict RED"], SelfTestArmSource.Missing(planted, arms));
        Assert.Empty(SelfTestArmSource.Missing(planted, arms[..3]));
    }
}
