## PB4466: an agent writes in its own scratch directory, never the session scratchpad

PB4466 recorded that two concurrent agents of wave 20261834 each wrote a `callers.py` into the one session scratchpad, and the
PB2359 implementer then ran the other group's copy against its own worktree, which re-applied that group's renames to three source
files it did not own. Re-probe: the implementer template already rendered `Scratch: <wave scratch>\<slug>\` per group, but no
sentence told an agent to write only there, and the harness's environment note ("always use the scratchpad") stayed the louder
instruction, so the per-agent directory was never the one used. The fix is a practice with its checks, not a reminder: MANDATORY-PRACTICES
P16 (YOUR SCRATCH IS YOUR OWN DIRECTORY) with its reason; the template's `Scratch:` line states it; `check_practices.py` requires the
line in the template and in every rendered spec as a slug directory (a bare wave scratch is refused), and
`bare_scratchpad_lines` refuses any `.claude/` or `scripts/hooks/` instruction surface that names the scratchpad other than as "the
session scratchpad". The agent definitions (adjudicator, refuter, locator), the golden-lane brief, the lane-1 workflow description,
the land-a-fix commit-message step, the two hook messages (`forbidden_commands.py`, `readonly_repo.py`) and the
`make_dispatch_specs.py` docstring example were reworded to send an agent to its own directory.

Tests: the 7c1 arms of `scripts/orchestrator/test_plan_wave.py` (the rendered line per group is `<scratch>\<slug>\` and distinct; the bare
scratch, a line without P16 and a missing line are refused; the real tree has no bare mention; each surface arm is refused on a plant).
Self-tests run: `test_plan_wave.py` 530/530, `test_dispatch_guard.py` 25/25, `test_readonly_repo.py` 5/5, `test_forbidden_commands.py`
232/232, `check_practices.py` GREEN. Rows re-verdicted: none (a process note).

Branch review round 1 found two blocking defects. R1-1: the reworded `meta.description` of `wf_lane1_draft.js` carried an unescaped
apostrophe, so the golden lane's stage-1 workflow no longer parsed; no audit parsed the workflow templates. Fixed (the word reworded),
and a new arm of `scripts/orchestrator/test_rolling_wave.py` runs `node --check` on every `wf_*.js` wrapped as the Workflow runtime loads
it (the body of an async function). That arm found two parse errors already on main, unescaped backticks around `refusals` inside the
prompt template literals of `wf_lane1_draft.js` and `wf_lane1_validate.js`, which are escaped in the same change. R1-2: the branch
reviewers of one wave shared the session scratchpad, since the reviewer prompt named no directory. `reviewBranch` now names
`<wave scratch>\review-w<wave>-<letter>\` (`reviewScratch`); the `cobol-reviewer` definition, the review skill's branch form and P16 say
so; `check_practices.py` pins the prompt text in `wf_rolling_wave.js`; and the rolling-wave self-test checks that each reviewer's prompt
names its own directory and not another group's. `test_rolling_wave.py` 52/52, `check_practices.py` GREEN.
