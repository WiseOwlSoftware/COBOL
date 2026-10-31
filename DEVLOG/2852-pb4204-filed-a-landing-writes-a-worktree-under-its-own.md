## Entry 2852 — 2026-10-31 09:08 PDT — PB4204 filed: a landing writes a worktree under its own running gate

At 08:58 PDT on 2026-10-31 the operator's landing lane landed `worktree-wf_34371f8b-c21-1` as PR #884. That was correct,
because gate run 000003 covers its tip. But the Mythos session's re-gate was running in that worktree at the time, and its
Linux half went RED with `repository-written` when the landing merged main and stamped DEVLOG 2851.

The cause is in `enqueue.py`. `tested_locally` consults the live gate lock only when no GREEN record covers the tip, so an
older GREEN record lets `land` merge and stamp under a running gate. The landing lane probes the gate lock only to retry
an attempt that already ended exit 12, never before the first attempt. kb/Work PB4204 (cluster PB1957) records the case
and the fix's shape: one busy check at the step that writes, exit 12 whatever the records say, with a self-test arm and
a sweep of the loop's other worktree writers. This landing changes only the register.

