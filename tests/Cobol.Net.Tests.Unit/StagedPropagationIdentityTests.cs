// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using CobolNet.Runtime.Exceptions;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB892 Arm B — a condition staged by <c>GOBACK / EXIT … RAISING</c> names the ACTIVATION it was staged
/// for, and only a pickup running in that activation takes it. ISO §14.9.18.4 GR1 b): "an exception condition is
/// raised in the activating runtime element if checking for that exception condition is enabled in the activating
/// runtime element" — one element, the one the returning element returns to. The staged slot used to be
/// anonymous, so the next pickup ANYWHERE took whatever an activator with no pickup (an EC-free group) had left.
/// These pins drive the engine and the activation record directly, in the shapes a run unit produces, so the rule
/// is checked for every activation mechanism at once (CALL, INVOKE, function reference all push a frame).
/// </summary>
public sealed class StagedPropagationIdentityTests
{
    private const string AllOn = "+EC-USER";   // the activator's §7.3.25 profile — checking enabled for EC-USER

    private static (ModuleStack Stack, ExceptionEngine Engine) NewRunUnitParts()
    {
        var s = new ModuleStack();
        return (s, new ExceptionEngine(s));
    }

    [Fact]
    public void TheActivatorTakesWhatItsCalleeStaged()
    {
        var (s, e) = NewRunUnitParts();
        s.PushMain("MAIN");
        s.Push("CALLEE", "CALLEE", false);
        e.SetPropagating("EC-USER-Q", fatal: false);   // the callee's GOBACK RAISING
        s.Pop();                                       // the activation returns
        Assert.True(e.TakeRaisedPropagation(AllOn, out var name, out _));
        Assert.Equal("EC-USER-Q", name);
    }

    [Fact]   // The w48s/sep probe: the activator had no pickup, a LATER sibling activation's pickup must not take it.
    public void AStagingLeftByAPickupFreeActivator_IsNotTakenByAnotherActivation()
    {
        var (s, e) = NewRunUnitParts();
        s.PushMain("MAIN");
        s.Push("RAISE1", "K", false);
        e.SetPropagating("EC-USER-BZ", fatal: false);
        s.Pop();                                       // MAIN emits no pickup (EC-free) ...
        s.Push("PROGB", "PROGB", false);               // ... and CALLs PROGB
        s.Push("QUIET", "K", false);
        s.Pop();                                       // QUIET raised nothing
        Assert.False(e.TakeRaisedPropagation(AllOn, out _, out _));
    }

    [Fact]   // X → E (no pickup) → R: the staging is E's; X, below E, must not take it after E returns.
    public void AStagingForAnIntermediateActivation_IsNotTakenByItsActivator()
    {
        var (s, e) = NewRunUnitParts();
        s.PushMain("X");
        s.Push("E", "E", false);
        s.Push("R", "K", false);
        e.SetPropagating("EC-USER-BZ", fatal: false);
        s.Pop();                                       // R returns to E, which has no pickup
        s.Pop();                                       // E returns to X
        Assert.False(e.TakeRaisedPropagation(AllOn, out _, out _));
    }

    [Fact]   // The OBJECT slot answers the same question (§14.6.13.1.5 — the same GR1 b) staging).
    public void TheObjectSlot_ObeysTheSameIdentity()
    {
        var (s, e) = NewRunUnitParts();
        s.PushMain("MAIN");
        s.Push("M", "K", false);
        e.SetPropagatingObject(null);
        s.Pop();
        s.Push("OTHER", "OTHER", false);
        Assert.False(e.TakePropagatedObject(out _));
        s.Pop();
        Assert.True(e.TakePropagatedObject(out _));    // MAIN, the activator, still can
    }

    [Fact]   // §14.9.18.4 GR3 — the main program's GOBACK RAISING has no activator: nothing can ever take it.
    public void TheMainProgramsStaging_HasNoActivator()
    {
        var (s, e) = NewRunUnitParts();
        s.PushMain("MAIN");
        e.SetPropagating("EC-USER-M", fatal: false);
        Assert.False(e.TakeRaisedPropagation(AllOn, out _, out _));
    }
}
