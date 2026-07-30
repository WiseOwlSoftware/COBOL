// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ EVERY PUBLIC ENTRY OF <c>NumericRenderer</c> THAT SETS THE AMBIENT RECEIVER MUST RESTORE IT.
/// <para>
/// <c>NumericRenderer._rcv</c> is per-unit MUTABLE state read by the intrinsic renderer to pick a working scale
/// (<c>max(Receiver.Scale, 9)</c>) and to decide the float/fixed shape. <c>Render</c> and <c>AsNum</c> always
/// saved and restored it in a <c>finally</c>; <c>Fold</c> and <c>Combine</c> did NOT, and simply assigned. An
/// ADD/SUBTRACT therefore left its receiver LATCHED on the renderer, and the next receiver-less render inherited
/// it — so <c>DISPLAY FUNCTION SQRT(2)</c> printed <c>1.414213562</c>, and then <c>1.414213562373</c> after an
/// unrelated <c>ADD 1 TO R</c>. A function's printed text depended on a preceding statement.
/// </para>
/// <para>
/// This is a CLASS test, not an instance test. Nothing else in the battery notices a leaked receiver: the leak
/// only becomes visible when a LATER render happens to have no receiver of its own, which is rare and
/// order-dependent. Pinning the save/restore discipline at the source is what makes the class impossible — a new
/// public entry that assigns <c>_rcv</c> without a restoring <c>finally</c> fails here.
/// </para>
/// </summary>
public sealed class ReceiverContextRestoreDriftTests
{
    private static string Source() =>
        File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Emit", "NumericRenderer.cs"));

    /// <summary>Every method that assigns <c>_rcv</c> must also restore it. The restore is recognised by the
    /// <c>finally { _rcv = saved…</c> idiom the four entries share.</summary>
    [Fact]
    public void EveryEntryThatSetsTheReceiver_AlsoRestoresIt()
    {
        string src = Source();

        // Split into method bodies at brace depth 1 (the class body), then check each that assigns _rcv.
        var offenders = new List<string>();
        foreach (Match m in Regex.Matches(src, @"^\s*(?:public|private|internal)[^\n=;]*\b(?<name>\w+)\s*\([^\)]*\)\s*$",
                     RegexOptions.Multiline))
        {
            int start = src.IndexOf('{', m.Index + m.Length);
            if (start < 0) continue;
            int depth = 0, i = start;
            for (; i < src.Length; i++)
            {
                if (src[i] == '{') depth++;
                else if (src[i] == '}' && --depth == 0) break;
            }
            string body = src[start..Math.Min(i + 1, src.Length)];
            if (!body.Contains("_rcv =")) continue;                       // does not touch the ambient receiver
            if (body.Contains("finally") && body.Contains("_rcv = saved")) continue;   // restores it
            offenders.Add(m.Groups["name"].Value);
        }

        Assert.True(offenders.Count == 0,
            "These NumericRenderer entries assign the ambient receiver (_rcv) without restoring it in a finally. "
            + "A leaked receiver makes a later receiver-less render (a numeric FUNCTION in DISPLAY / a text MOVE) "
            + "inherit an unrelated statement's scale — the DA2 SQRT defect. Offenders: "
            + string.Join(", ", offenders));
    }

    /// <summary>The guard above is only meaningful if it can actually see the assignments — a regex that matched
    /// nothing would pass vacuously forever. Assert the file really does contain the entries under test.</summary>
    [Fact]
    public void TheGuardActuallyInspectsTheReceiverAssignments()
    {
        string src = Source();
        Assert.True(Regex.Matches(src, @"_rcv = rcv").Count >= 4,
            "Expected at least the four public entries (Render, AsNum, Fold, Combine) to assign _rcv; "
            + "if this fails the drift guard above may be passing vacuously.");
        Assert.Contains("_rcv = saved", src);
    }
}
