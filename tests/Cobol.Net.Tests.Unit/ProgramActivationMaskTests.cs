// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The §14.9.4.4 GR3e EC-EXTERNAL enablement handshake at the activation boundary (kb/Work PB133): the CALL
/// site's pending mask is consumed by THIS activation attempt — success or FAILURE. The failure half is the
/// half no CLI-driven golden can pin cleanly (the run terminates on the loud), and it is where the defect
/// lived: the old CallProgram zeroed the pending mask only after a successful resolution, so an
/// EC-PROGRAM-NOT-FOUND / EC-PROGRAM-RECURSIVE-CALL throw leaked the site's mask into the NEXT statement's
/// activator latch — checking "enabled in both" (§14.8.4.1) then read the WRONG activating element's half.
/// </summary>
public sealed class ProgramActivationMaskTests
{
    private sealed class Fake : ICobolProgram
    {
        public System.Action? OnCall;
        public void Call(CobolArg[] args, ManagedPointer? returning) => OnCall?.Invoke();
        public void Activate() { }
        public void CloseFiles() { }
    }

    [Fact]
    public void FailedActivation_ConsumesThePendingExternalMask()
    {
        var ru = new RunUnit();
        ru.Exceptions.ActivatorExternalMask = 0b0111;   // the CALLER's own activator half — must be untouched
        ru.Exceptions.ExternalCheckMask = 0b1010;       // the failing site's pending mask
        Assert.Throws<CobolCallException>(() => ru.Programs.CallProgram("NOWHERE", "MAIN", [], null));
        Assert.Equal(0, ru.Exceptions.ExternalCheckMask);        // consumed — never leaks into the next latch
        Assert.Equal(0b0111, ru.Exceptions.ActivatorExternalMask);
    }

    [Fact]
    public void SuccessfulActivation_LatchesThePendingMaskAsTheActivatorHalf()
    {
        var ru = new RunUnit();
        int seenDuringCall = -1, pendingDuringCall = -1;
        var fake = new Fake();
        fake.OnCall = () =>
        {
            seenDuringCall = ru.Exceptions.ActivatorExternalMask;    // the activated element reads the site's half
            pendingDuringCall = ru.Exceptions.ExternalCheckMask;     // re-zeroed so a site-emit-free nested CALL
        };                                                           // reads "checking not enabled"
        ru.Programs.Register("P1", "P1", null, initial: false, common: false, recursive: false, _ => fake);
        ru.Exceptions.ActivatorExternalMask = 0b0001;
        ru.Exceptions.ExternalCheckMask = 0b1100;
        ru.Programs.CallProgram("P1", "MAIN", [], null);
        Assert.Equal(0b1100, seenDuringCall);
        Assert.Equal(0, pendingDuringCall);
        Assert.Equal(0b0001, ru.Exceptions.ActivatorExternalMask);   // restored on return
        Assert.Equal(0, ru.Exceptions.ExternalCheckMask);
    }
}
