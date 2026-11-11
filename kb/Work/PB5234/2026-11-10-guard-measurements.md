**Addendum (2026-11-10): every guard, measured.** Step 1 of this note's work: one row per guard, then the heredoc
measurement (step 2). The decisions taken from these rows are step 3's commits, each naming its row.

**How it was measured.** The transcripts are the PB5207 friction survey's (`E:/COBOL-coord/scratch/process-friction/`,
its `common.py` reader, window from 2026-11-08 00:00 local, both accounts). Re-walked at 2026-11-10 ~23:00 the window held
1,161 transcripts (the survey's 891 at 15:14; the window grows as files are written), 42,411 Bash/PowerShell tool calls
(40,101 of them Bash). The scripts are in `E:/COBOL-coord/scratch/stage5/`: `sample.py` (every refusal with the command it
refused, `refusals.json`; ten sampled per rule, seed 5234), `heredoc_tp.py` and `shape_all.py` (the heredoc rule's true
positives), `chained_tp.py` (the chained-verdict rule's), `rmcount.py`, `timeit.py` (seconds per hook: median of 15 fresh
`python` runs on an ordinary input, as the harness starts them; `python -c pass` is 0.034 s on this host). A TRUE positive
is a refused command that would really have done the harm the rule names; a FALSE one would not.

**The PreToolUse and Stop hooks (`.claude/settings.json`, `.claude/agents/*.md`).**

| Guard | Harm it prevents (incident) | Runs | s/call (median) | Refusals in window (transcripts) | True / sampled | What else already proves the same | Decision |
|---|---|---|---|---|---|---|---|
| `forbidden_commands.py` rule 1, `git stash` / `--autostash` | the stash stack is shared by every worktree (PB713) | every Bash + PowerShell call | 0.070 (whole hook) | 1 (1) | 1/1 (`git stash push` of settings.local.json) | the settings.json deny rules cover four literal spellings only | KEEP, a name match on the git subcommand |
| rule 2, a push to THIS repository's main | main takes only queued pull requests (PB3023, PB3024) | same | - | 0 | - | the server ruleset refuses it (binds administrators) | KEEP, fast feedback for a refusal the server makes anyway |
| rule 2b, ANY `git push` inside a loop unit | a unit published the skills repository's main (PB2044) | same, when `COBOL_COORD_DIR` is set | - | 31 (31) | 0/31: every one pushed the unit's own work branch (`HEAD`, `HEAD:worktree-…`, `-u origin <branch>`); none named main, none another repository | rule 7 asks every checkpoint to be pushed | NARROW to a push to ANOTHER repository, judged by the remote's URL (fails closed when it cannot be read); rule 2 still refuses this repository's main |
| rule 3, a backslash escape in a heredoc body | heredoc bodies arrived altered (memory `feedback_heredoc_backslash_mangling`, 2026-08: PB60, PB64, PB66, PB78, PB82) | Bash + PowerShell | - | 576 (369) | 176/576 (31 %) carried the shape that is really altered (below); 400 held only single backslashes, which arrive intact | nothing: the cause is in the harness (below) | NARROW to the measured shape, Bash tool only |
| rule 4a, a bare `~X` test filter | `--filter "~X"` matches nothing and exits 0: a silent green | Bash + PowerShell | - | 0 | - | `dotnet test` prints "No test matches"; the gate never filters | KEEP: a match on the command's own `--filter` argument, zero false refusals |
| rule 4b, an unredirected filtered `dotnet test` | the output floods the context (no wrong result) | same | - | 0 | - | nothing needed: the cost is the agent's own context, not a wrong result | DELETE |
| rule 5, a command chained after a verdict command | the chain's status is its last command's, so a verdict is masked (`dotnet test && git commit` on a false green) | same | - | 127 (84) | 1/127: one chain ran `enqueue.py land` after an unread `build-local` verdict; 52 only read the verdict after it (tail/grep), 65 ran unrelated work after it (mostly a test after a build), 9 more cleaned up or read state (`gh pr list`, `Remove-Item Env:`, `cp` of a backup); no chain committed or pushed after a verdict command | `enqueue.py land` refuses a tip no GREEN gate record covers, so the one true chain could not have landed a red; a commit on a work branch is one commit from undone, and landing reads the gate record, never the chain's exit status | DELETE (measured: it catches nothing that is not already refused; PB5233's `build.py check` does not need to wait for it) |
| rule 6, WSL lifecycle commands | they end the owner's open session or burn disk (owner 2026-10-04) | same | - | 0 | - | nothing | KEEP, a name match on `wsl` and its flags |
| rule 7, `cd` in a backgrounded list with a command after the `&` | the later command runs in the caller's directory; a landing audit reported a false OK (PB3095) | same | - | 2 (2) | 1/2: `cd X && (self_tests) & (gate.py --self-test)` would have run the gate's self-test in the caller's tree; the other's second list had its own `cd` | nothing | KEEP (the true one is a wrong answer) |
| `status_guard.py` | a STATUS.md handoff that does not describe HEAD is trusted on resume (PB1698: 3 of 14 branches) | every Bash + PowerShell call, every Stop and SubagentStop | 0.080 (Bash), 0.163 (Stop) | 22 commit refusals (13) + 5 Stop | 22/22 a real stale stamp; none a harm: the stamp is read, not trusted | `status_delta.py` (agent-fleet skill), which every resume and finisher brief already runs, reports a STALE stamp and lists exactly the commits the summary does not cover | DELETE (PB5207 lists it) |
| `fleet_active_build.py` (1,204 lines) | a build while a fleet probes the same tree invalidated a 60-agent run (2026-08-04) | Bash + PowerShell `dotnet *` | 0.252 | 10 (4) | 0/10 proven: every refusal fell back to "a working tree that could not be determined, so the session-wide rule applies"; four were builds in a worktree created seconds before by the same command | every dispatched writer works in its own worktree (the harness's isolation, the workflow templates, the brief's `Worktree:` line), so a fleet's tree is not the caller's | DELETE |
| `worktree_rm_allow.py` | not a refusal: GRANTS `git rm -r` inside `.claude/worktrees/` past the auto-mode classifier (PB2142) | every Bash + PowerShell call | 0.457 (the slowest hook: every Bash call waits for it) | n/a | 2 of 42,411 shell calls named `git rm -r` | nothing | SCOPE its registration with `"if": "Bash(git *rm *)"` / `"PowerShell(git *rm *)"`: same grants, 0 s on the other 42,409 calls |
| `dispatch_guard.py` | a loop unit launches a fleet after STOP-UNIT (PB2597) | every Agent / Workflow call | 0.076 | 0 | - | nothing | KEEP (one rule, no shell parsing) |
| `own_scratch.py` | two dispatched agents shared the session scratchpad (PB4473, P16) | dispatched roles' Write/Edit/Bash/PowerShell | 0.058 | 9 (9) | 9/9 named the shared scratchpad | nothing | KEEP |
| `readonly_repo.py` | a read-only role wrote inside a git tree | read-only roles' Write/Edit | 0.123 | 0 | - | nothing | KEEP |
| `inbox_guard.py` (Stop) | an attended lane's watcher was not re-armed; nine messages waited 2.5 h (PB3387) | every Stop | 0.813 | 1 block + 11 warnings | 1/1 | nothing | KEEP (not a shell parser); its 0.8 s per Stop is a lead for stage 4 |
| `unpushed.py`, `golden_registered.py`, `session_start.py` | report, never refuse | Stop / PostToolUse / SessionStart | 0.120 / 0.065 / - | 0 | - | - | KEEP |
| the harness's worktree-isolation guard | not a repository file: Claude Code refuses a shell command of an `isolation: 'worktree'` agent that it cannot prove stays in the worktree | every shell call of a Workflow agent started with `isolation: 'worktree'` (`.claude/skills/workstream/templates/wf_rolling_wave.js`, three sites) | harness | 282 (107), every one in a Workflow subagent | 0/10 sampled: reads, probes in its own worktree, writes to the coordination scratch the brief names | the brief's `Worktree:` line and the permission rules | SPLIT (below) |

**Hook seconds per tool call.** The hooks of one matcher group run in parallel, so a call waits for the slowest. Before: a
Bash or PowerShell call ran forbidden_commands (0.070), status_guard (0.080) and worktree_rm_allow (0.457), and a `dotnet`
call also fleet_active_build (0.252): 0.457 s of wall time and 0.607 s of CPU per call (0.859 s per `dotnet` call). A Stop
ran unpushed (0.120), status_guard (0.163) and inbox_guard (0.813). After: forbidden_commands alone, 0.070 s per call; a
Stop 0.813 s (inbox_guard). Over the window's 42,411 shell calls, 0.387 s saved per call is 4.6 h of waited hook time in
three days.

**The heredoc rule: the mangling still happens, and not where the rule looks (step 2).** The probes are in
`E:/COBOL-coord/scratch/stage5/`; `check.py` builds the bytes MEANT from `chr(92)` and holds no backslash itself, so no path
it measures could have altered it. The body: `print("a\nb\tc")`, a raw Windows path, a regex with `\d \s \( \w \) \\`, a
line `x = "\\n" + "\\\"" + "\0" + "\u0041" + "\x41"`, `E:\COBOL\scripts\hooks\test \a\b\f\v\r end`, `tab\there`.
- Byte-exact (`SAME`): the Write tool; PowerShell `@'…'@` and `@"…"@` here-strings through the PowerShell tool; and every
  heredoc run by bash from a file (`bash probe.sh`, written with the Write tool, so the hook did not see its heredocs; this
  measures the harness, the purpose the owner's design names, and is no route around the rule): `cat <<'EOF'`, `cat <<EOF`
  (exactly as POSIX specifies: `\\` becomes `\`), `python - <<'PYEOF'` with a raw string and with a non-raw `"\\n\\\""`.
  The 2026-08 incidents' "a heredoc decodes `\\n` to a real newline" did not reproduce.
- ALTERED: a Bash TOOL CALL, heredoc or not. `printf '%s' '<body>'` (single quotes, no heredoc) wrote `"\n"` for
  `"\\n"`. Isolated over two further calls (`t1.bin`, `t2.bin`): the Bash tool halves every run of two or more
  backslashes that does not end at a double quote (`A\\nZ`, `A\\Z`, `A\\/`, `A\\ Z`, `A\\0Z` → one backslash; `A\\\\Z` →
  two; six → three), and leaves a run before `"` (`A\\"Z`, `A\\\"Z`) and every single backslash (`\n`, `\d`, `C:\Users`)
  intact. The transcript (what the hook receives) records the doubled form; `/proc/$$/cmdline` shows bash received the
  halved one inside the harness's wrapper (`bash -c "source <snapshot> … && eval '<command>'"`). So the cause is in Claude
  Code's Bash tool on this Windows host, after the hook runs, and cannot be fixed in this repository; reporting it upstream
  is the owner's (CLAUDE.md, Ownership).
- What the rule refused: 576 heredocs in the window, all through the Bash tool. 176 of them (31 %) carried a run of 2+
  backslashes not ending at a quote in a body, the shape that is really altered (a body with a quoted delimiter is
  altered outright; with an unquoted delimiter a single pair is halved by bash anyway, so those are a residue of false
  refusals the narrowed rule keeps). 400 held only single backslashes: false refusals. The same shape appeared in 153 Bash
  commands OUTSIDE any heredoc, which no rule sees; sampled, most are a pair inside bash double quotes, where bash halves
  it anyway, and a few are real (`sed 's/\\./\\\\./g'` in single quotes).
- Decision: keep the rule, narrowed to that shape, for the Bash tool only (the PowerShell tool is exact): a heredoc body
  carrying two or more backslashes in a row that do not end at a double quote. Refusals over the window: 576 → 176.

**The `scripts/guard-*.sh` family (step 4): every script is still called; none is deleted.** All nine are the nightly
NIST guard, which stage 6 (PB5235) makes a slow target of the graph:
- `guard-fast.sh` (239 lines): the entry. Run by `.github/workflows/nightly.yml` (job `guard`), `scripts/battery.sh`,
  `scripts/linux-gate.sh`. Nightly job time 8.4-14.8 min (runs 42497371387, 42686567938, 42860414795); red in each.
- `guard-compiler.sh`, `guard-population.sh` (sourced by guard-fast; population also by guard-nist-audit and
  guard-run-group), `guard-compile.sh` and `guard-run-group.sh` (run by guard-fast through `xargs`), `guard-verdict.sh`
  (sourced by guard-run-group and guard-verify), `guard-nist-audit.sh` and `guard-baselines.sh` (run by guard-fast), and
  `guard-verify.sh` (guard-fast runs its `--self-test`; nightly names it). The Conformance tests read them too
  (`CorpusManifestTests.cs`, `NistRunner.cs`, `CompilerUnderTest.cs`, `NistWrittenFilesTests.cs`), and
  `scripts/build_targets.json` declares them as inputs.

**After, measured.** `replay.py` runs every Bash and PowerShell command of the window through a hook file in-process,
judged as an attended session, with the same inputs for both arms (`replay_old.log`, `replay_new.log`):

| forbidden_commands.py | Commands replayed | Refused | heredoc | chained | stash | bg_cd |
|---|---|---|---|---|---|---|
| origin/main (before) | 43,484 | 1,166 (2.68 per 100 calls) | 979 | 182 | 2 | 3 |
| this branch (after) | 43,414 | 185 (0.43 per 100 calls) | 180 | 0 | 2 | 3 |

The replay counts more heredoc refusals than the transcripts recorded (979 against 576), because it judges every command
by today's rule, including commands written where the hook did not run. The 31 unit pushes replayed as a unit: the old
rule refuses 30, the new one 0 of the 6 whose directory still exists; the other 25 ran in worktrees since pruned, whose
remote the rule cannot read, so it refuses them (it fails closed). Hook wall time per Bash call: 0.457 s → 0.070 s
(forbidden_commands alone; the `if`-scoped worktree_rm_allow runs on 2 calls in 42,411). Hook lines: `scripts/hooks/` and
`.claude/settings.json` lose 1,609 lines and gain 147.

**What this stage does not finish: the isolation guard (SPLIT, kb/Work PB5236).** Its 282 refusals are the second-largest family. Its
replacement is a dispatch shape: Workflow agents started without `isolation: 'worktree'`, each in a worktree the
dispatcher creates, bounded by the brief and permission rules. The harness-made worktree and its `worktree-wf_*` branch
names run through `plan_wave.py`, `prune_worktrees.py`, `landing_rules.py`, `review_lane.py`, `coord.py` and the
orchestrator tests, which another lane is editing now (`stages-in-parallel.md`). It is therefore its own change, made
after those land; it is not done here.
