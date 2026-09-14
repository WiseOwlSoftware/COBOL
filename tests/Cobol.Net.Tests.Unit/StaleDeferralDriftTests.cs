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
/// ⛔ A SURVIVING ENTRY IS NOT AUTOMATICALLY A BUG. The Format-1 device arm below refuses a float receiver
/// because §14.9.1.4 GR1 leaves the device conversion to the implementor and COBOL.NET has not yet DEFINED one
/// for a float: <c>DataItem.DisplayTextWidth</c> is <c>pic.Digits</c> for a numeric receiver, and a float
/// PICTURE has no digit positions, so there is no device window to read. That needs a determination
/// (§4.2.16 documentation obligation), not a code move. This test does not prejudge it; it makes the inventory
/// EXPLICIT so no entry can rot unnoticed again, and forces a deliberate decision when one is added or removed.
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
    private static readonly string[] PinnedDeferrals =
    [
        // ISO §14.9.1.4 GR1 — Format 1's device conversion is implementor-defined, and COBOL.NET has not defined
        // one for a receiver with no digit positions (there is no device window width to read). Not a stale
        // premise: a genuinely undefined determination.
        "AcceptDisplayEmitter.cs: ACCEPT into floating-point receiver",
    ];

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
}
