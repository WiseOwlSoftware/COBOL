## PB4333 (step 10 of PB4323): the gate's growth trigger becomes an instrument, and its second criterion is measured

PB4323 section 3.4 builds the emitted-program differential when "the weekly median of the code lane's test phase
(recorded in `verdict.json`) exceeds 6 min, or Conformance exceeds 1.5 times today's", and only if the median share of
changed programs over 20 non-runtime landings is under 10 %. Nothing read either number.

**What the re-probe showed.** A gate's `verdict.json` is written by `Gate._finish` into its worktree's `TestResults/`,
and the worktree is removed when the branch lands: on 2026-11-01 63 records survived across 41 worktrees, two of them
with `timings.tests_s` (schema 5; the 14 whole-population records of the morning carry `legs_s`). A weekly median read
from `verdict.json` could never be computed, so the trigger was a hope. The note's site guess, `shard_weights.py`, reads
CI artifacts (the merge group's shard spread) and holds no local gate's test phase, so it is not a site.

**The fix, at the root.** `Gate._record_growth` writes one small file per GREEN code-lane gate that ran its hosts
(not a RED, not a REUSED verdict, not the inert lane) to `<git common dir>/cobol-gate-growth/`, beside the gate slots, by
atomic replace, one file per run. `scripts/gate_growth.py status` judges the trailing 7 days and prints one line that
`session-probe.ps1` shows every session: WITHIN, FIRED, PARTIAL or NOT MEASURED. The time arm fires only on a complete
window (7 days of samples, at least 10); the population arm (Conformance over 30,609 = 1.5 x 20,406) needs none. gate.py's
self-test gained two arms and three assertions (70 arms): a GREEN run leaves one sample, an unwritable store is said and
leaves the verdict, and the inert lane, a RED and a REUSED verdict leave none. gate_growth's own self-test pins the
boundaries (exactly 360 s is WITHIN, 360.1 s FIRES; exactly 30,609 does not fire), the partial window (one slow day is
PARTIAL, not FIRED), old samples that cover the window without entering its median, pruning, idempotence and a corrupt
file named.

**The first reading, 2026-11-01.** The 16 whole-population GREEN gates that exist (the shape began that day) have a
median test phase of 464 s = 7.7 min. That is over 6 minutes, but it is one day, run beside up to five fleets, and it is
the three-gate steady state PB4328 measured (503 s each); the trigger prints PARTIAL until a week exists (2026-11-08).
Conformance is 20,482 cases, 1.00 times the basis. The differential is NOT built.

**The second criterion, measured now (`scripts/arch/emit_fraction.py`, through the architecture oracle).** Over the 20 newest
non-runtime landings of `8fd4ad387` (each and its first parent captured: about 15,700 cases, about 4,700 programs): median
0.00 % of programs changed (0.03 % counting programs a landing adds), maximum 0.08 % (0.34 %); 2 of the 20 changed any C# (1
and 4 programs). The review's landing `a95e97138` gives 0, as the review measured, and the committed tool reproduced the
scratch measurement exactly on two landings (0 of 4,763 and 4 of 4,763). The 10 % criterion is met by two orders of magnitude.
What it does not measure, and the design doc (section 3.14.13) now says: of the 54 code-lane landings since 2026-10-31
only 28 (52 %) leave the runtime AND the Conformance harness alone; the differential is sound only for those, and the finer
rule for the rest is the classification that failed the impact map (PB2696, PB4228).

**Registers.** kb/Work PB4333 is `half`: the instrument and the measurement landed, the build waits on the trigger. It closes
no inventory row. No diagnostic code was used (COBOL5830 to COBOL5832 returned). No LSP diagnostics apply (Python and one
PowerShell line). Linux gate: not part of the per-change gate (PB4327).
