// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using WiseOwl.Cobol.Runtime;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>kb/Work PB2101 review finding R1-1 — every fault of the sibling-module probe's load-and-register step is the CALL's
/// "not located" condition (ISO/IEC 1989:2023 §14.9.4.4 GR3 b), cite.py OK: "If the program cannot be located ... the
/// EC-PROGRAM-NOT-FOUND exception condition is set to exist"), including the <see cref="PlatformNotSupportedException"/> a
/// native-AOT application's <c>AssemblyLoadContext.LoadFromAssemblyPath</c> throws when it left the
/// <c>WiseOwl.Cobol.Runtime.DynamicProbe</c> switch on (DESIGN-external-repository §15.5). A native-AOT host cannot be run
/// here, so the classification the probe's catch applies is exercised directly.</summary>
public sealed class ProbeFaultClassificationTests
{
    [Fact]
    public void ANativeAotLoadRefusal_IsClassified_WithTheReasonToSetTheSwitch()
    {
        string? why = ProgramTable.ProbeFaultReason("/app/X.dll", new PlatformNotSupportedException("no loader"));
        Assert.NotNull(why);
        Assert.Contains("/app/X.dll", why, StringComparison.Ordinal);
        Assert.Contains("WiseOwl.Cobol.Runtime.DynamicProbe", why, StringComparison.Ordinal);
        Assert.Contains("native-AOT", why, StringComparison.Ordinal);
    }

    [Fact]
    public void ARegistrarFaultThatArrivesWrapped_IsClassified_WithTheRegistrarsOwnMessage()
    {
        var wrapped = new TargetInvocationException(new InvalidOperationException("registrar broke"));
        Assert.Equal("'/app/X.dll': registrar broke", ProgramTable.ProbeFaultReason("/app/X.dll", wrapped));
        Assert.NotNull(ProgramTable.ProbeFaultReason("/app/X.dll", new FileNotFoundException("gone")));
        Assert.NotNull(ProgramTable.ProbeFaultReason("/app/X.dll", new BadImageFormatException("not an assembly")));
    }

    [Fact]
    public void AFaultTheProbeDoesNotClassify_IsNotSwallowed()
    {
        Assert.Null(ProgramTable.ProbeFaultReason("/app/X.dll", new InvalidOperationException("a bug")));
        Assert.Null(ProgramTable.ProbeFaultReason("/app/X.dll", new OutOfMemoryException()));
    }
}
