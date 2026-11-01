// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// The PowerShell function reader <see cref="ReviewLaneDriftTests"/> still calls. The landing lane's rules this class
/// held (orchestrate.ps1 starts <c>land_lane.py tick</c> in the background from Step-Lane; enqueue.md step 3 lands
/// nothing; no current file says the enqueue unit lands; the verdict line's one home) are
/// <c>scripts/orchestrator/landing_rules.py --self-test</c>'s since kb/Work PB4395 item 1. This class holds no fact,
/// and is deleted with its last caller when ReviewLaneDriftTests leaves the compiler's test assembly (the same item).
/// </summary>
internal static class LandingLaneDriftTests
{
    /// <summary>The body of a PowerShell function: from its <c>function Name</c> line to the next top-level function.</summary>
    internal static string FunctionBody(string script, string name)
    {
        Match m = Regex.Match(script, @"^function " + Regex.Escape(name) + @"\b.*?(?=^function |\z)",
            RegexOptions.Multiline | RegexOptions.Singleline);
        return m.Success ? m.Value : "";
    }
}
