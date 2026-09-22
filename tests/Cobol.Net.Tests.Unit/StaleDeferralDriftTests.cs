// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE STALE-DEFERRAL INVENTORY. A <c>LoudStmt</c> whose message says a backend path is "deferred" is not a
/// statement about COBOL — it is a CLAIM ABOUT THIS BACKEND, and the backend moves under it. Nothing in the
/// build notices when such a claim stops being true, and the cost is not a missing feature but a WRONG SHAPE
/// OF FAILURE: the loud statement is emitted inline in the per-elementary run, so one refused leaf aborts the
/// whole statement — including for the items that have nothing to do with the refused one.
/// <para>
/// kb/Work PB420 is the measured case. <c>InitializeEmitter</c> carried an arm that intercepted every
/// <c>InitializeStore</c> into a COMP-1/COMP-2 receiver and emitted "float MOVE path deferred". The premise was
/// true when written; PB271 then hardened <c>MoveEmitter</c>'s float-receiver store, which presupposes that very
/// path. From that moment the identical explicit MOVE — the statement ISO §14.9.20.4 GR4 says the implicit one
/// IS ("Otherwise, the implicit statement is: MOVE sending-operand TO receiving-operand") — compiled and ran in
/// the same data division, while <c>INITIALIZE G REPLACING NUMERIC DATA BY 1</c> over a group with one float
/// leaf aborted the run unit. The same stale premise sat on <c>AcceptDisplayEmitter</c>'s Format-2 arm, where
/// §14.9.1.4 GR6 is even more explicit ("according to the rules for the MOVE statement").
/// </para>
/// <para>
/// ⛔ A SURVIVING ENTRY IS NOT AUTOMATICALLY A BUG — but the last one was not a survivor either. The Format-1
/// device arm refused a float receiver on the premise that §14.9.1.4 GR1's implementor-defined conversion had no
/// definition for an item with no digit positions. That was a missing DETERMINATION, not a missing path, and it
/// aborted legal source (§14.9.1.3 SR1 does not exclude a float receiver). kb/Work PB887 made it: one record, read
/// as the inverse of the DISPLAY image (CONFORMANCE.md §7 DOC-A.1-1). The inventory is now EMPTY, and the test
/// keeps it explicit so the next entry is a deliberate decision with its clause written beside it.
/// </para>
/// </summary>
public sealed class StaleDeferralDriftTests
{
    private static readonly string CodeGenDir = TestRepo.Src("Cobol.Net.Compiler", "CodeGen");

    /// <summary>Strip <c>///</c> doc comments, whole-line <c>//</c> comments and trailing <c>//</c> tails, so the
    /// scan sees CODE only. These arms are discussed at length in prose — that is the point of the prose — and a
    /// raw text scan would report every explanation as an emitted refusal.</summary>
    private static string CodeOnly(string src)
    {
        var code = new List<string>();
        foreach (string raw in src.Split('\n'))
        {
            string line = raw.TrimEnd('\r');
            string t = line.TrimStart();
            if (t.StartsWith("///") || t.StartsWith("//")) continue;
            int i = line.IndexOf("//", StringComparison.Ordinal);
            code.Add(i >= 0 ? line[..i] : line);
        }
        return string.Join("\n", code);
    }

    /// <summary>Every <c>LoudStmt(...)</c> ARGUMENT in <paramref name="code"/>, by paren balance — the message can
    /// span lines (an interpolated string concatenated across a wrap), so a per-line match would miss exactly the
    /// multi-line arms this inventory exists for.</summary>
    private static IEnumerable<string> LoudMessages(string code)
    {
        foreach (Match m in Regex.Matches(code, @"\bLoudStmt\("))
        {
            int depth = 1, i = m.Index + m.Length;
            int start = i;
            for (; i < code.Length && depth > 0; i++)
            {
                if (code[i] == '(') depth++;
                else if (code[i] == ')') depth--;
            }
            yield return code[start..(i - 1)];
        }
    }

    /// <summary>The pinned inventory: "&lt;file&gt;: &lt;a distinguishing fragment of the message&gt;". One entry
    /// per emitted refusal whose message calls a backend path deferred. Adding one is a DECISION — write down
    /// why the path cannot be reached through the rule's own seam — and removing one is the fix.</summary>
    private static readonly string[] PinnedDeferrals = [];

    [Fact]
    public void DeferredLoudArms_AreExactlyThePinnedInventory()
    {
        var found = new List<string>();
        foreach (string path in Directory.EnumerateFiles(CodeGenDir, "*.cs", SearchOption.AllDirectories))
        {
            string code = CodeOnly(File.ReadAllText(path));
            foreach (string msg in LoudMessages(code))
                if (msg.Contains("deferred", StringComparison.OrdinalIgnoreCase))
                    found.Add($"{Path.GetFileName(path)}: {msg}");
        }

        foreach (string f in found)
            Assert.True(PinnedDeferrals.Any(p => f.StartsWith(p.Split(": ")[0], StringComparison.Ordinal)
                                                 && f.Contains(p.Split(": ")[1], StringComparison.Ordinal)),
                $"A NEW 'deferred' loud arm is emitted that this inventory does not pin:\n  {f}\n"
                + "Re-derive the rule's own seam first (kb/Work PB420: the last one was stale for months and "
                + "aborted whole statements). If the deferral is genuinely undefined, pin it here WITH the "
                + "clause that leaves it undefined.");

        foreach (string p in PinnedDeferrals)
            Assert.True(found.Any(f => f.StartsWith(p.Split(": ")[0], StringComparison.Ordinal)
                                       && f.Contains(p.Split(": ")[1], StringComparison.Ordinal)),
                $"A pinned 'deferred' loud arm is gone — good news, but the inventory must say so:\n  {p}");
    }

    /// <summary>INITIALIZE HAS EXACTLY ONE STORE PATH. ISO §14.9.20.4 GR4 makes every non-pointer implicit
    /// statement a MOVE, so <c>InitializeEmitter</c> renders an <c>InitializeStore</c> through
    /// <c>MoveEmitter</c> and nothing else: no receiver-usage test, no per-category store, no second conversion
    /// seam. That is what makes conversion, editing, JUSTIFIED, truncation and the EC-DATA-OVERFLOW check
    /// identical to the explicit MOVE the rule names — and what made the PB420 arm a divergence rather than an
    /// extra. The only <c>LoudStmt</c> the emitter may render is <c>InitializeErrorAction</c>'s, whose message
    /// the BINDER composed.</summary>
    [Fact]
    public void InitializeEmitter_RoutesEveryStoreThroughTheOneMoveSeam()
    {
        string code = CodeOnly(File.ReadAllText(Path.Combine(CodeGenDir, "Verbs", "InitializeEmitter.cs")));

        Assert.Single(Regex.Matches(code, @"\bmove\.Emit\("));
        Assert.Single(Regex.Matches(code, @"\bcase InitializeStore\b"));
        Assert.DoesNotContain("IsFloat", code);
        Assert.DoesNotContain("Usage.", code);

        // The one permitted loud: the binder's own InitializeErrorAction message, rendered verbatim.
        string[] loud = [.. LoudMessages(code)];
        Assert.Single(loud);
        Assert.Equal("e.Feature", loud[0].Trim());
    }

    /// <summary>ACCEPT FORMAT 2 HAS EXACTLY ONE STORE PATH, AND IT IS THE MOVE EMITTER (kb/Work PB887). ISO
    /// §14.9.1.4 GR6 transfers the temporal value "to the data item specified by identifier-2 according to the rules
    /// for the MOVE statement", so the binder binds that transfer as an implicit MOVE from the GR7–GR12 conceptual
    /// item (<c>BoundAccept.Store</c>) and <c>AcceptDisplayEmitter</c> renders it through <c>MoveEmitter</c>. The
    /// emitter used to carry its own numeric / edited / alphanumeric / national / float / group arms — a second copy
    /// of <c>MoveEmitter.ConvertSource</c> — and that copy's missing float arm is how the PB420 fix had to be made
    /// twice. The primitives named below are the receiver-category MOVE stores of that copy; none may reappear in
    /// the ACCEPT emitter (the Format-1 device arm is explicitly NOT a MOVE, GR1–GR4, and needs none of them).</summary>
    [Fact]
    public void AcceptTemporal_RoutesThroughTheOneMoveSeam()
    {
        string code = CodeOnly(File.ReadAllText(Path.Combine(CodeGenDir, "Verbs", "AcceptDisplayEmitter.cs")));

        Assert.Single(Regex.Matches(code, @"\bmove\.Emit\("));
        foreach (string primitive in new[]
                 {
                     "EditFormatFor", "EditFormatSimpleInsertion", "StrStoreAligned", "StrStore(",
                     "FloatStoreSingleChecked", "FloatStoreDecChecked",
                 })
            Assert.False(code.Contains(primitive, StringComparison.Ordinal),
                $"AcceptDisplayEmitter calls RuntimeApi.{primitive.TrimEnd('(')} — a MOVE receiver-category store. "
                + "ACCEPT's temporal transfer IS a MOVE (ISO §14.9.1.4 GR6) and is rendered by MoveEmitter from "
                + "BoundAccept.Store; a second copy of the MOVE rules here is how the float arm went missing "
                + "(kb/Work PB420, PB887).");
        Assert.Empty(LoudMessages(code));
    }
}
