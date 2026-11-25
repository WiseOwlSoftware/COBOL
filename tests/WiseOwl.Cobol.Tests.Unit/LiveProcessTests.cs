// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// The instrument that holds a child process open while a test acts (<see cref="LiveProcess"/>): every stdout line the program prints is kept in order for the step
/// that asks and for a failure message, so a non-observation in a two-process test can say what the other process had printed (kb/Work PB6134).
/// </summary>
public sealed class LiveProcessTests
{
    /// <summary>A child that prints one line per installed runtime and ends.</summary>
    private static LiveProcess StartRuntimeLister()
    {
        var psi = new ProcessStartInfo("dotnet");
        psi.ArgumentList.Add("--list-runtimes");
        return LiveProcess.Start(psi);
    }

    [Fact]
    public void EveryStdoutLineIsReadInOrderThenNullAtTheEnd_AndALineNoStepReadIsInTheReport()
    {
        using var read = StartRuntimeLister();
        using var unread = StartRuntimeLister();
        var lines = new List<string>();
        while (read.ReadLine() is { } line) lines.Add(line);
        Assert.NotEmpty(lines);
        Assert.Null(read.ReadLine());                                                 // stdout is over: null again, not a non-observation
        Assert.Equal(0, read.WaitForExit());
        Assert.Contains("exited with 0", read.Report());
        Assert.DoesNotContain(lines[0], read.Report());                               // every line was taken by a step, none is left to report

        Assert.Equal(0, unread.WaitForExit());
        Assert.True(SpinWait.SpinUntil(() => unread.Report().Contains(lines[0], StringComparison.Ordinal), ObservationBudget.UnitMs),   // the pump drains stdout on its own thread
            "the pump never held the program's first line");
        Assert.Equal(lines[0], unread.ExpectLine());                                  // and the line is still there for the step that asks
    }
}
