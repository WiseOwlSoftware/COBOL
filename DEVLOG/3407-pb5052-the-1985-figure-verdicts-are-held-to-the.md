## Entry 3407 — 2026-11-09 20:46 PST — PB5052: the 1985 figure verdicts are held to the transcription at the gate

The 1985 figure sweep check was red on main: the verdict on figure 0 of page 200 (a running head, not-a-format) recorded drawn_lines without the underline markup the transcription heading now carries, so `decide` refused it as "the transcription moved under the verdict", and the message named only the first line, which was identical. No gate ran the check (8.5 minutes, needs the licensed PDF), so the drift was silent.

Fix: p200-0.json re-recorded. The identity test is one function, `moved_under_verdict`, whose message prints both blocks in full; `decide` uses it and so does a new PDF-free mode, `--identity`, which checks that every verdict names its block and that none names a missing figure. The gate runs it as the FIGURE VERDICTS 1985 audit and CI audits job runs the same command, so a transcription edit that drifts a verdict is refused before the merge group. Self-test 33/33, with a planted stale verdict refused. Closes no inventory row (process only).

