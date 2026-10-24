## O11: nothing simple waits; the attended session lands what the lane cannot reach

Until now MANDATORY-PRACTICES O11 said, while the loop runs, "the attended session lands nothing itself": a finished
attended branch became a report for the enqueue unit, and everything stopped waited for that unit. Units run one at a
time, so a stop waited behind whatever campaign or wave unit was running.

On 2026-10-24 that rule held four branches whose remaining work was mechanical. PR #467 (PB3155) had been reviewed CLEAN
and re-armed, but sat 13 hours because the lane skips a NOT QUEUED row (PB3628). PR #493's notes bounced only because
their `blocked: true` went stale when PB3492 landed while the pull request waited. Two notes-only operator branches
were waiting for a reviewer. PB3112 was refused only because it carried a merged predecessor's stamped DEVLOG entry.
The operator landed or unblocked each within minutes, and the owner made that the rule: "We should always use such
approaches when available. Don't keep simple things blocked for long periods for stupid reasons".

O11, the workstream skill's section 3 and `references/landing.md` now say the attended session lands by hand, while the
loop runs, any branch the lane cannot reach or would leave idle for a mechanical reason, when no lane attempt runs for
it. It then steers the next unit so nothing is duplicated, re-arms what a predecessor's landing unblocks, and files each
case as a note in the process campaign so the loop does it itself next time. `check_practices.py` is green.
