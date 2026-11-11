## Entry 3465 — 2026-11-11 00:18 PST — PB5234: the guards measured, then cut to the ones that refuse a real harm (stage 5 of the build redesign)

Stage 5 of kb/Work PB5207. Every PreToolUse/Stop hook rule and every `scripts/guard-*.sh` was measured over the PB5207
friction window (1,161 transcripts, 42,411 shell calls, 2026-11-08..10): its refusals, how many of them were true on a
sample, its seconds per call, who runs it, and what else already proves the same. The table is
`kb/Work/PB5234/2026-11-10-guard-measurements.md`; the probes and replays are in `E:/COBOL-coord/scratch/stage5/`.

**The heredoc rule's mangling, measured.** The Write tool, PowerShell here-strings and heredocs that bash runs from a file
are byte-exact (checked against bytes built from `chr(92)`). The Bash TOOL is not: it halves every run of two or more
backslashes that does not end at a double quote, inside a heredoc or not, after the hooks read the command
(`/proc/$$/cmdline` shows bash receiving the halved text inside the harness's `eval '…'`). That is a harness defect,
kb/Work PB5237; reporting it upstream is the owner's. 176 of the rule's 576 refusals carried that shape; 400 held only
single backslashes, which arrive intact.

**What changed, one commit per guard.**
- `forbidden_commands.py` rule 5 (a command chained after a verdict command): deleted. 127 refusals, 1 true, and that
  one (`enqueue.py land` after an unread gate) the landing refuses itself.
- rule 4b (an unredirected filtered `dotnet test`): deleted. 0 refusals; its cost was context, never a wrong answer.
- rule 2b (any push inside a loop unit): narrowed to a push to ANOTHER repository, judged by the remote's URL. 31
  refusals, 0 true (each a unit pushing its own branch).
- rule 3 (heredoc escapes): narrowed to the shape the Bash tool alters, Bash tool only. 576 → 176 in the window.
- `status_guard.py`: deleted (22 commit and 5 Stop refusals, each a real stale stamp, none a harm: `status_delta.py`
  lists what a stale summary omits). `fleet_active_build.py` (1,204 lines): deleted (10 refusals, all on its
  "tree could not be determined" fallback, none proven). `worktree_rm_allow.py`: registered only for `git … rm`
  calls (`"if": "Bash(git *rm *)"`), instead of costing every call 0.457 s for 2 grants in 42,411 calls.
- Kept: `git stash`/`--autostash`, a push to this repository's main, the property-less `~X` filter, the WSL lifecycle
  commands, the backgrounded `cd` (1 of its 2 refusals true), and the role and Stop hooks.
- Every `scripts/guard-*.sh` is still called (the nightly NIST guard's chain, battery.sh, linux-gate.sh, the
  Conformance tests); none deleted; stage 6 (PB5235) owns them.

**Before and after.** Refusals by `forbidden_commands.py` over the same 43,400 replayed commands: 1,166 → 185 (2.68 →
0.43 per 100 shell calls; about 1.00 → 0.16 per transcript). Over the window as recorded, the repository's hooks refused
778 times (0.67 per transcript); the rules that remain would have refused 189 (0.16: heredoc 176, own_scratch 9, bg_cd 2,
stash 1, inbox_guard 1). Hook wall time per Bash call: 0.457 s → 0.070 s; a Stop keeps inbox_guard's 0.813 s (its hooks
run in parallel) and loses status_guard's 0.163 s of CPU. Guard lines: `scripts/hooks/` and `.claude/settings.json` lose 1,609 and gain
147.

**Not done here.** The harness's worktree-isolation guard (282 refusals, 0 of 10 sampled true) needs a dispatch change
in files another lane is editing; it is kb/Work PB5236. A measurement error of my own was caught by re-timing: the
narrowed rule 2b first read this repository's remote on every call (0.725 s median); it now asks git only for a unit's
push.

