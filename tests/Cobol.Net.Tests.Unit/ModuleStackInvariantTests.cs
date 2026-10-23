// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The §15.65.4 invariants of FUNCTION MODULE-NAME's returned value that no CLI-driven program can falsify (kb/Work
/// PB63): r1 — "an alphanumeric dynamic length elementary item with no trailing spaces, except that in a COBOL main
/// program with the ACTIVATING keyword a single space is returned"; r2 — the "does not fit" antecedent
/// (EC-BOUND-FUNC-RET-VALUE) is structurally unreachable because there is no fixed width between the module stack
/// and the expression (CONFORMANCE.md A.1 item 133 — RV-15.65.4-2); r10 — TOP-LEVEL names "the runtime element that
/// was activated by the operating environment", so a stack whose frame 0 was NOT pushed as the main (a .NET host
/// driving a COBOL element without RunMain) answers r3's documented single space (A.1 item 134 — RV-15.65.4-10). The
/// host boundary is exactly the configuration <c>cobol.exe --run</c> cannot reach, which is why it is pinned HERE.
/// </summary>
public sealed class ModuleStackInvariantTests
{
    private const int Current = 0, Activating = 1, Nested = 2, Stack = 3, TopLevel = 4;

    [Fact]
    public void NoTrailingSpaces_AndNoTruncation_AtEveryDepth()
    {
        var s = new ModuleStack();
        s.PushMain("MAINPROG", recursive: false);
        // A 300-character element name (r2 — nothing between the stack and the expression can truncate it).
        string longName = new string('Q', 300);
        s.Push(longName, longName, isNested: false, recursive: false);
        for (int i = 0; i < 60; i++) s.Push("RECURSE", "RECURSE", isNested: false, recursive: false);   // deep recursion (PB36's depth)
        foreach (int kind in new[] { Current, Activating, Nested, Stack, TopLevel })
        {
            string v = s.Name(kind);
            Assert.False(v.Length > 1 && v.EndsWith(' ') && kind != Stack, $"kind {kind}: '{v}' has trailing spaces (§15.65.4 r1)");
            Assert.NotEqual("", v);
        }
        Assert.Equal("RECURSE", s.Name(Current));
        Assert.Contains(longName, s.Name(Stack));                                        // r2 — the full name, untruncated
        Assert.Equal(60 + 1 + 1 + 1, s.Name(Stack).Split(';').Length);                    // CURRENT + 60 activators + main + " "
        Assert.EndsWith("; ", s.Name(Stack));                                             // r9 — the final single-space entry
        Assert.Equal("MAINPROG", s.Name(TopLevel));                                       // r10 — the run-unit main
    }

    [Fact]
    public void ActivatingInTheMain_IsASingleSpace_TheOneR1Exception()
    {
        var s = new ModuleStack();
        s.PushMain("MAINPROG", recursive: false);
        Assert.Equal(" ", s.Name(Activating));                                            // r5 — the stated exception
        Assert.Equal("MAINPROG; ", s.Name(Stack));                                        // r9 — CURRENT then the environment
    }

    [Fact]
    public void TopLevel_WithoutAMainFrame_IsTheDocumentedNonCobolValue()
    {
        // A .NET host that drives a COBOL element through the CALL path without ProgramTable.RunMain: frame 0 is
        // an ordinary CALLed frame (IsMain false), the operating environment activated the HOST, and r3's
        // documented value — a single space (A.1 item 134) — is what TOP-LEVEL returns (RV-15.65.4-10).
        var s = new ModuleStack();
        s.Push("HOSTED", "HOSTED", isNested: false, recursive: false);
        s.Push("INNER", "INNER", isNested: false, recursive: false);
        Assert.Equal(" ", s.Name(TopLevel));
        Assert.Equal("HOSTED", s.Name(Activating));                                       // r5 still names the activator
        Assert.Equal("INNER;HOSTED; ", s.Name(Stack));
        // And with no COBOL element running at all, every keyword is the r3 single space.
        var empty = new ModuleStack();
        foreach (int kind in new[] { Current, Activating, Nested, Stack, TopLevel }) Assert.Equal(" ", empty.Name(kind));
    }

    /// <summary>kb/Work PB3540 — §9.1.18.7's "under the control of a recursive runtime element" is a question to the module
    /// stack: a recursive frame (a RECURSIVE program, a function, a method; §8.6.6) is under way from its push to its
    /// pop, whatever kind of frame sits above it, and the count balances however frames interleave.</summary>
    [Fact]
    public void AnyRecursive_HoldsFromTheRecursiveFramesPushToItsPop()
    {
        var s = new ModuleStack();
        Assert.False(s.AnyRecursive);
        s.PushMain("MAINPROG", recursive: false);
        s.Push("PLAIN", "PLAIN", isNested: false, recursive: false);
        Assert.False(s.AnyRecursive);                    // a program with no RECURSIVE attribute is no recursive element
        s.Push("METH", "CLS", isNested: false, recursive: true);
        Assert.True(s.AnyRecursive);
        s.Push("CALLED", "CALLED", isNested: false, recursive: false);
        Assert.True(s.AnyRecursive);                     // a plain program called under the method is under its control
        s.Push("METH2", "CLS", isNested: false, recursive: true);
        s.Pop();
        Assert.True(s.AnyRecursive);                     // the outer recursive frame is still running
        s.Pop();
        Assert.True(s.AnyRecursive);
        s.Pop();
        Assert.False(s.AnyRecursive);                    // the last recursive frame returned
        var recursiveMain = new ModuleStack();
        recursiveMain.PushMain("RMAIN", recursive: true);
        Assert.True(recursiveMain.AnyRecursive);         // a main program carrying RECURSIVE is one too
        recursiveMain.Pop();
        recursiveMain.Pop();                             // an unbalanced pop is a no-op and never drives the count below zero
        Assert.False(recursiveMain.AnyRecursive);
    }
}
