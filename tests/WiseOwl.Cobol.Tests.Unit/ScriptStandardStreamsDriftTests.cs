// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ EVERY LIVE PYTHON TOOL PUTS ITS STANDARD STREAMS IN UTF-8 AT STARTUP, THROUGH ONE FUNCTION, AND PRINTS ITS --help
/// AND A USAGE ERROR UNDER A cp1252 STREAM (kb/Work PB3637, PB1546): <c>scripts/standard_streams.py --check</c> holds
/// every tracked <c>.py</c> file under <c>scripts/live_tools.py</c>'s <c>ROOTS</c> to its three rules — no file but
/// <c>scripts/standard_streams.py</c> reconfigures a stream's encoding (COPY), every entry point calls
/// <c>ensure_utf8()</c> first (ENTRY), and every argparse entry point's <c>--help</c> and usage error, RUN with
/// <c>PYTHONIOENCODING=cp1252</c>, exit 0 and 2 with no traceback and no backslash-escaped character (HELP).
/// </summary>
/// <remarks>
/// <para>
/// Why: on Windows a standard stream that is not a console (a pipe, a redirect: every agent's shell, every gate)
/// encodes in cp1252, and the scripts' help texts carry ⛔, → and §. 126 scripts each carried their own
/// <c>sys.stdout.reconfigure(encoding="utf-8")</c>, most of them inside <c>main()</c> after <c>parse_args()</c>, which
/// prints <c>--help</c> and exits first: <c>python scripts/spec/work.py --help</c> died with UnicodeEncodeError, and 31
/// entry points did so when measured. The owner's direction (2026-10-10, kb/Work PB1546) puts the switch IN THE
/// PROGRAM, as the compiler's <c>StandardStreams.EnsureUtf8</c> does; an environment variable alone does not hold,
/// since <c>PYTHONIOENCODING</c> beats <c>PYTHONUTF8</c> and a hand run carries neither.
/// </para>
/// <para>
/// The HELP rule is RUN, not read: a scan cannot see a help text built another way (kb/Work PB2615). Its probes are
/// the reason this sweep costs seconds; the audit's <c>--self-test</c> (run by <c>scripts/self_tests.py</c> in every
/// gate and in CI) plants a parser whose help and usage carry ⛔ and requires the probe to fail without the call and
/// pass with it, so a probe environment that stopped being hostile would be red there.
/// </para>
/// </remarks>
[Trait("Sweep", "tree")]
public sealed class ScriptStandardStreamsDriftTests
{
    /// <summary>
    /// The files the audit reads, before <c>live_tools.ROOTS</c> narrows them: every tracked Python file (declared,
    /// kb/Work PB3531, so the drift-rule index names this class for each one). The roots are read by the audit itself,
    /// from the one list, so this scope cannot copy them and holds the whole tree's Python instead; it also bounds the
    /// audit's child processes for its budget.
    /// </summary>
    private static readonly TreeScope PythonFiles = new(Under: [""], Kinds: [".py"]);

    private static readonly Regex Verdict = new(
        @"^=== STANDARD STREAMS: (?<v>PASS|FAIL) — (?<files>\d+) live Python files scanned, (?<probed>\d+) entry points",
        RegexOptions.Multiline | RegexOptions.Compiled);

    [Fact]
    public void EveryLiveTool_SetsItsStreamsThroughTheOneFunction_AndPrintsItsHelpUnderCp1252()
    {
        string audit = TestRepo.Scripts("standard_streams.py");
        Assert.True(File.Exists(audit), $"the audit is missing: {audit}");
        int files = TrackedTree.Files(PythonFiles).Count;
        // Two probes (--help, a usage error) per entry point at most, as many at once as the audit's pool runs.
        var budget = ObservationBudget.ForChildren(2 * files, Math.Min(32, Environment.ProcessorCount),
            "standard_streams.py --check: two cp1252 probes per argparse entry point");

        ProcessObservation r = PythonInstrument.Run(budget, audit, "--check");

        Match m = Verdict.Match(r.Stdout);
        Assert.True(m.Success, $"standard_streams.py --check printed no verdict (exit {r.ExitCode}).\n{r.Stdout}\n{r.Stderr}");
        Assert.True(r.ExitCode == 0 && m.Groups["v"].Value == "PASS",
            "a live Python tool sets its own stream encoding, skips ensure_utf8(), or cannot print its --help or a usage "
            + "error under cp1252. Call `ensure_utf8()` (from scripts/standard_streams.py) as the first statement of "
            + "the `__main__` block, or at module level before a hook's first statement, and delete the copy "
            + $"(kb/Work PB3637):\n{r.Stdout}\n{r.Stderr}");
        int scanned = int.Parse(m.Groups["files"].Value, System.Globalization.CultureInfo.InvariantCulture);
        int probed = int.Parse(m.Groups["probed"].Value, System.Globalization.CultureInfo.InvariantCulture);
        // A net over nothing is no net: the audit must have read the live trees and run their entry points.
        Assert.InRange(scanned, 1, files);
        Assert.InRange(probed, 1, scanned);
    }
}
