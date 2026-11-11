## PB5325 filed: the audit-slot self-test arm asserts a 30-second wall-clock ceiling

The branch review of kb/Work PB5322 (PR #1516) raised a non-blocking lead. The new audit-slot arm of `gate.py
--self-test` ends its condition with `slot_wait_s < 30`, a stopwatch reading held to a fixed limit, which kb/Work
PB1590 forbids in a test. The clerk who filed this landing's other leads (PR #1518) read origin/main, where the arm
does not exist yet, and filed nothing. The arm lands with #1516, so the enqueue unit files it here as PB5325.
