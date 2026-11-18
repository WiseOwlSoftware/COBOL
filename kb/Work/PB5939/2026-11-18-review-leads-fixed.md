# PB5939: the two low review leads, fixed at the root (2026-11-18)

Review leads R1-1 and R1-2 of the fast session start (PR #1876) each made the probe print a wrong line.

**R1-1, `scripts/session_probe.py` `merge_queue`.** GitHub omits `required_status_checks` from the classic branch
protection when no check is enabled. The probe read it by key, so the absent key became the UNREAD "did not parse" line,
where the pwsh probe printed the DRIFTED alarm with `contexts=[]`. An absent (or null) key is now no required checks, which
reaches the DRIFTED alarm; a response that is not JSON, not an object, or lacks `enforce_admins` still says UNREAD.
Case: `case_protection_without_required_checks_is_drift_not_unread` (red on the old code: it printed "did not parse").

**R1-2, `scripts/fuzz_history.py`.** `main_verdict` keyed its cache with `git rev-parse origin/main:<path>` and
`extract_verdict` then archived `origin/main` by name; a fetch between the two stored the newer main's verdict under the
older main's key, and `status` also read the ledger text by name a third time. Now `resolve_main` names origin/main as one
commit id, and the key (`main_state`), the archive (`extract_main`), the verdict compute and the ledger text (`main_ledger`)
all go by that id. Sweep: every other read of `MAIN_REF` by name is the id-less default of those functions, which only
`mark-published` (one read, no key) and the one-shot `extract_main` test now use. The `compute` seam takes `(repo, commit)`;
the new cases move the ref between resolve and compute and check the key, the archive and the content stay at the old id
(red on the old code: the seam did not exist).

**Review round 1 (blocking, fixed).** The R1-2 case stubbed the compute and so never ran `extract_verdict` or
`main_ledger(commit=)`; a mutation that archived origin/main by name kept it green. The case now builds two commits whose
`scripts/fuzz_history.py` and `FUZZ-LEDGER.md` name themselves A and B, moves origin/main to B after resolving A's id, and
runs the real `extract_verdict`, `main_ledger` and default-compute `main_verdict` at A's id. The reviewer's three mutations
(by-name archive, by-name ledger, compute handed None) each go RED; the code is GREEN.
