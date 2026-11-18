## Entry 3848 — 2026-11-18 15:49 PST — The session probe's two wrong lines: a protection with no required checks, and a fuzz verdict keyed by one main and built from another

The review of the fast session start (kb/Work PB5939) left two low leads, and each made the probe print a wrong line.
GitHub omits `required_status_checks` when no check is enabled; `merge_queue` read it by key and printed "did not parse"
(UNREAD) where the old probe printed the DRIFTED alarm with `contexts=[]`. An absent key now means no required checks, and
only a response that does not parse stays UNREAD. Separately, `fuzz_history status` read origin/main by name three times (the
cache key, the archive, the ledger text); the probe now fetches at the same time, so a fetch between two reads could store the
newer main's verdict under the older main's key. It resolves origin/main to one commit id and every read goes by that id.
Both have self-test cases that fail on the old code (scripts/selftests/session_probe/test_session_probe.py,
scripts/test_fuzz_history.py).

