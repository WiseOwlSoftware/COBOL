// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;

namespace CobolNet.Tests.Shared;

/// <summary>
/// ⛔ THE ONE PLACE A TEST ASKS THE HOST WHAT IT CAN ACTUALLY DO — and the rule is:
/// <b>a host capability a test depends on is MEASURED here, never asserted from a claim about the platform.</b>
/// </summary>
/// <remarks>
/// <para>kb/Work PB795 is what the absence cost. Two suites depended on a host capability and each stated it as
/// a fact instead of measuring it, and `main` was red for two days on both:</para>
/// <list type="number">
/// <item><description><c>FileAuthorityPresenceTests</c> established §14.9.27.4 GR3's <i>"insufficient
/// authority"</i> precondition and then classified a failure to establish it with a hard-coded sentence — <i>"A
/// merely ELEVATED Windows token does not bypass an explicit deny ACE for its own SID, so Windows has no bypass
/// case here"</i>. On the GitHub Windows runner the precondition was not established, and the guard reported
/// <i>"the helper is broken"</i> without ever looking at the host. It could not have known: nothing in the class
/// measured a host fact.</description></item>
/// <item><description><c>FileLockPostureDriftTests</c> asserted that a <see cref="FileShare.Read"/> file lock
/// refuses an outside WRITER. .NET on Unix implements <see cref="FileShare"/> with advisory <c>flock</c>, which
/// has two states — <see cref="FileShare.None"/> takes <c>LOCK_EX</c> and every other value takes
/// <c>LOCK_SH</c> — so that refusal cannot exist there at all.</description></item>
/// </list>
/// <para>Both are the same defect: a PREDICTION about the operating environment standing where a MEASUREMENT
/// belongs (feedback: <c>reachability_is_measured_not_deduced</c>). Every probe below runs against a real,
/// throwaway object on the host that is executing the test, caches its answer for the process, and carries the
/// evidence for its answer in a string a CI log can be read for.</para>
/// </remarks>
internal static class HostCapability
{
    /// <summary>A scratch directory for the probes, unique per process so two concurrent runs never share a
    /// witness.</summary>
    private static string Scratch()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"hostcap-{Environment.ProcessId}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    // ── Who this process is, as the host describes it ───────────────────────────────────────────────────────

    private static readonly Lazy<string> LazyIdentity =
        new(MeasureIdentity, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>The host's own description of this process's security identity — the token's user, its group
    /// SIDs and its privileges on Windows, the effective uid/gid on Unix.
    /// <para>⛔ MEASURED THROUGH THE HOST'S OWN TOOL, not deduced from <see cref="Environment.UserName"/>. The
    /// question a failed denial raises is <i>"which of this token's SIDs or privileges granted the access the
    /// DACL refused?"</i>, and only the token can answer it. It is read through
    /// <see cref="ProcessObserver"/> — the ONE child-process seam — so a contention timeout reports as a
    /// non-observation rather than as an empty identity, and it is read LAZILY, on the failure path only, so an
    /// ordinary green run never spawns it.</para></summary>
    public static string Identity => LazyIdentity.Value;

    private static string MeasureIdentity()
    {
        try
        {
            var psi = OperatingSystem.IsWindows()
                ? new ProcessStartInfo("whoami")
                : new ProcessStartInfo("id");
            if (OperatingSystem.IsWindows())
            {
                psi.ArgumentList.Add("/user");
                psi.ArgumentList.Add("/groups");
                psi.ArgumentList.Add("/priv");
                psi.ArgumentList.Add("/fo");
                psi.ArgumentList.Add("list");
            }
            var seen = ProcessObserver.ObserveOrThrow(psi);
            string text = (seen.Stdout + seen.Stderr).Trim();
            return text.Length == 0
                ? $"(the host's identity tool exited {seen.ExitCode} with no output)"
                : text;
        }
        catch (Exception e)   // a missing tool, a launch failure, a non-observation — all are "not measured"
        {
            return $"(this host's identity could not be measured: {e.GetType().Name}: {e.Message})";
        }
    }

    // ── Can this host refuse THIS process access to a file it owns? ─────────────────────────────────────────

    /// <summary>What a denial this process applied to its own file actually did.</summary>
    public enum DenialVerdict
    {
        /// <summary>The host refused the access. The precondition the test needs is really established.</summary>
        Enforced,

        /// <summary>The denial was applied — the host's own tool reported success — and the host granted the
        /// access anyway. This process's identity bypasses file permissions on this host, which is a statement
        /// ABOUT THE HOST and not about the helper.</summary>
        BypassedByThisIdentity,

        /// <summary>The denial could not be applied at all: the host's tool failed, or the mode we set did not
        /// read back. The helper is broken and the tests that depend on it prove nothing.</summary>
        ToolFailed,
    }

    /// <summary>One classified denial, with the evidence for its classification.</summary>
    /// <param name="Verdict">What the measurement showed.</param>
    /// <param name="Because">The measured evidence — the tool's exit codes and output, and, when the host
    /// granted an access it was told to refuse, the token identity that got it. This string is what a CI log
    /// carries, so it must name facts and never a conclusion drawn from the platform.</param>
    public readonly record struct Denial(DenialVerdict Verdict, string Because)
    {
        public bool Enforced => Verdict == DenialVerdict.Enforced;
    }

    /// <summary>Classify a denial this test just attempted. <paramref name="refused"/> is the RAW measurement
    /// (did the host actually refuse the access?) and <paramref name="applied"/> is whether the denial was
    /// successfully put on the object — the two facts that separate "this host cannot deny me" from "the helper
    /// did not deny anything".
    /// <para>⛔ THE IDENTITY IS ONLY MEASURED WHEN THE ANSWER NEEDS IT. A green run spawns no child process
    /// here; a red one carries the token that produced it.</para></summary>
    public static Denial Classify(bool refused, bool applied, string what, string toolDetail)
    {
        if (refused)
            return new Denial(DenialVerdict.Enforced, $"{what}: the host refused the access. {toolDetail}");
        if (!applied)
            return new Denial(DenialVerdict.ToolFailed,
                $"⛔ {what}: THE DENIAL WAS NEVER APPLIED — the host's own tool did not report success, so this "
                + "is the helper failing and not the host.\n  " + toolDetail);
        return new Denial(DenialVerdict.BypassedByThisIdentity,
            $"⛔ {what}: the denial WAS applied and this host granted the access anyway, so this process's "
            + "identity bypasses file permissions here.\n  " + toolDetail
            + "\n  --- the identity the host granted it to ---\n" + Indent(Identity));
    }

    private static string Indent(string text) =>
        string.Join("\n", text.Replace("\r\n", "\n").Split('\n').Select(l => "  " + l));

    // ── What do this host's SHARE MODES actually enforce? ───────────────────────────────────────────────────

    /// <summary>What the operating environment's share modes really do to a handle that is not this connector's
    /// — ISO §9.1.15's <i>file lock</i>, measured rather than assumed.
    /// <para>Each field is one real open against one real outstanding handle. The outsider always asks for
    /// <see cref="FileShare.ReadWrite"/>, exactly as <c>FileLockPostureDriftTests</c> does, so the probe never
    /// manufactures the refusal it is measuring.</para></summary>
    /// <param name="ExclusiveRefusesAnOutsideReader">A <see cref="FileShare.None"/> handle outstanding: may an
    /// outsider still READ? §9.1.15 1) says no.</param>
    /// <param name="ExclusiveRefusesAnOutsideWriter">The same handle: may an outsider still WRITE?</param>
    /// <param name="ReadShareAdmitsAnOutsideReader">A <see cref="FileShare.Read"/> handle outstanding: may an
    /// outsider READ? §9.1.15 2) says yes — it restricts others <i>"to input mode"</i>, it does not exclude
    /// them.</param>
    /// <param name="ReadShareRefusesAnOutsideWriter">⛔ THE DISCRIMINATING ONE. The same handle: is an outsider's
    /// WRITE refused? Only a host whose sharing is access-discriminating can answer yes; a host whose sharing is
    /// one advisory lock with two states cannot express the distinction at all.</param>
    /// <param name="ReadWriteShareAdmitsAnOutsideWriter">A <see cref="FileShare.ReadWrite"/> handle outstanding:
    /// may an outsider WRITE? §9.1.15 3) says yes.</param>
    /// <param name="Because">The measurement, as text, for the failure message that quotes it.</param>
    public readonly record struct SharingFacts(
        bool ExclusiveRefusesAnOutsideReader,
        bool ExclusiveRefusesAnOutsideWriter,
        bool ReadShareAdmitsAnOutsideReader,
        bool ReadShareRefusesAnOutsideWriter,
        bool ReadWriteShareAdmitsAnOutsideWriter,
        string Because)
    {
        /// <summary>Whether this host can express ISO §9.1.15 2) at all — a share mode that admits a reader and
        /// refuses a writer. Windows' share modes are mandatory and per-access, so it can; a host that maps
        /// every share mode but <see cref="FileShare.None"/> onto one shared advisory lock cannot.</summary>
        public bool SeparatesReadersFromWriters => ReadShareAdmitsAnOutsideReader && ReadShareRefusesAnOutsideWriter;

        /// <summary>Whether this host can express ISO §9.1.15 1)'s <i>exclusive access</i> against a handle that
        /// is not this run unit's.</summary>
        public bool EnforcesExclusiveAccess => ExclusiveRefusesAnOutsideReader && ExclusiveRefusesAnOutsideWriter;
    }

    private static readonly Lazy<SharingFacts> LazySharing =
        new(MeasureSharing, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>This host's share-mode semantics, measured once per process.</summary>
    public static SharingFacts Sharing => LazySharing.Value;

    private static SharingFacts MeasureSharing()
    {
        string dir = Scratch();
        string path = Path.Combine(dir, "sharing.probe");
        try
        {
            File.WriteAllText(path, "PROBE");

            bool exclRead, exclWrite, readRead, readWrite, rwWrite;
            using (var _ = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                exclRead = !OutsiderCan(path, FileAccess.Read);
                exclWrite = !OutsiderCan(path, FileAccess.Write);
            }
            using (var _ = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                readRead = OutsiderCan(path, FileAccess.Read);
                readWrite = !OutsiderCan(path, FileAccess.Write);
            }
            using (var _ = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                rwWrite = OutsiderCan(path, FileAccess.Write);
            }

            return new SharingFacts(exclRead, exclWrite, readRead, readWrite, rwWrite,
                $"measured on {path}: FileShare.None refuses an outside reader={exclRead}, an outside "
                + $"writer={exclWrite}; FileShare.Read admits an outside reader={readRead}, refuses an outside "
                + $"writer={readWrite}; FileShare.ReadWrite admits an outside writer={rwWrite}.");
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    /// <summary>⛔ THE ONE OUTSIDER PROBE. Can a handle that is not the outstanding one obtain
    /// <paramref name="access"/> on <paramref name="path"/> right now? It needs no second process: a host share
    /// mode names no requester, so a handle opened here is exactly as foreign to an outstanding one as another
    /// program's. <see cref="FileShare.ReadWrite"/> is what it asks for, so the probe never manufactures the
    /// refusal it is measuring.
    /// <para>It lives here rather than in the test that uses it because <see cref="MeasureSharing"/> and
    /// <c>FileLockPostureDriftTests</c> ask the same question of the same host, and two copies of "what may an
    /// outsider do" is one rule written down twice — the shape that lets a probe and the capability it is
    /// checked against drift apart (feedback: one_rule_one_place).</para></summary>
    public static bool OutsiderCan(string path, FileAccess access)
    {
        try
        {
            using var _ = new FileStream(path, FileMode.Open, access, FileShare.ReadWrite);
            return true;
        }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }
}
