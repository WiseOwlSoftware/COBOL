// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A FORMAL PASSED ON BY REFERENCE IS ONE VIEW DEEP HOWEVER MANY ACTIVATIONS PASS IT ON (kb/Work PB4768).
/// ISO §14.2.3 GR8 — "the activated runtime element operates as if the formal parameter occupies the same storage
/// area as the argument" — makes every formal of a chain of activations the SAME storage. A view built over the
/// previous activation's view is a chain of one closure per activation: each access costs the chain's depth and a
/// recursion deep enough exhausts the .NET stack outside the activation-stack check (§14.9.4.4 GR3 c) and h)).
/// <para>RED BEFORE THE FIX: <c>CobolArgAdapt.Text</c> returned a new <c>OverField</c> over the previous carrier, so
/// the view the loop below reaches at level N read through N delegates and 100 000 levels overflowed the stack.</para>
/// </summary>
public sealed class CallAbiViewDepthDriftTests
{
    private const int Levels = 100_000;

    private static CobolArg Pass(ManagedPointer carrier) => new(CobolPassMode.Reference, carrier, null);

    [Fact]
    public void CharacterFormalPassedOn_StaysOneWindowOverTheCallersStorage()
    {
        var root = ManagedPointer<string>.Cell("ABCD");
        var carrier = (ManagedPointer<string>)root;
        for (int level = 0; level < Levels; level++)
            carrier = CobolArgAdapt.Text([Pass(carrier)], 0, 4, null, () => "    ");
        var window = Assert.IsType<TextWindow>(carrier.ViewTag);
        Assert.Same(root, window.Base);
        Assert.Equal("ABCD", carrier.Value);
        carrier.Value = "WXYZ";
        Assert.Equal("WXYZ", root.Value);
    }

    [Fact]
    public void AnyLengthAndDynamicFormalsPassedOn_StayOneViewOverTheCallersStorage()
    {
        var root = ManagedPointer<string>.Cell("ABCD");
        var any = (ManagedPointer<string>)root;
        var dyn = (ManagedPointer<string>)root;
        for (int level = 0; level < Levels; level++)
        {
            any = CobolArgAdapt.Text([Pass(any)], 0, -1, null, () => "");
            dyn = CobolArgAdapt.DynText([Pass(dyn)], 0, 6);
        }
        Assert.Same(root, Assert.IsType<AnyLengthView>(any.ViewTag).Base);
        Assert.Same(root, Assert.IsType<DynamicView>(dyn.ViewTag).Base);
        dyn.Value = "ABCDEFGHIJ";
        Assert.Equal("ABCDEF", root.Value);
    }

    /// <summary>The composed window equals the stack of windows it replaces: for every pair of widths a window of a
    /// window reads and stores exactly what the two-deep chain did (the closed form in <see cref="TextViews"/>).</summary>
    [Theory]
    [InlineData("ABCDEF", 4, 6, "xy")]
    [InlineData("ABCDEF", 6, 4, "xyzuvw")]
    [InlineData("ABCD", 6, 8, "xy")]
    [InlineData("ABCD", 2, 3, "xyzuvw")]
    [InlineData("AB", 5, 3, "xyzuvw")]
    [InlineData("", 3, 5, "xy")]
    public void WindowOfWindow_AgreesWithTheChainItReplaces(string storage, int w1, int w2, string stored)
    {
        static ManagedPointer<string> Naive(ManagedPointer<string> sp, int width) => ManagedPointer<string>.OverField(
            () => CobolString.Store(sp.Value, width),
            v => sp.Value = CobolString.WindowInto(sp.Value, 1, Math.Min(width, sp.Value?.Length ?? width), v));

        var chainRoot = ManagedPointer<string>.Cell(storage);
        var chain = Naive(Naive(chainRoot, w1), w2);
        var composedRoot = ManagedPointer<string>.Cell(storage);
        var composed = TextViews.Window(TextViews.Window(composedRoot, w1), w2);

        Assert.Equal(chain.Value, composed.Value);
        chain.Value = stored;
        composed.Value = stored;
        Assert.Equal(chainRoot.Value, composedRoot.Value);
        Assert.Equal(chain.Value, composed.Value);
    }
}
