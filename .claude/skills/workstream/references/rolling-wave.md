# The fix lane as a rolling wave (workstream SKILL.md section 2, full text)

Read this once when you plan a chain, a successor, a campaign, a review round or a spec render. The text is the former
section 2 of `SKILL.md`, moved here unchanged (kb/Work PB3780): the scripts it describes (`wf_rolling_wave.js`,
`review_findings.py`, `prune_worktrees.py`, `plan_wave.py`, `make_dispatch_specs.py`) enforce it, and a loop unit that
runs a planned wave decides none of it by hand.

⭐ **THE FIX LANE IS A ROLLING WAVE — owner 2026-09-27 ("fix related work together … minimize repetitive context
gathering"; "use this new rolling functionality as it reduces context and token use").** Every fix-lane dispatch is
`Workflow({scriptPath: ".claude/skills/workstream/templates/wf_rolling_wave.js", args})`. There is no wave barrier:
- **Rolling pool.** `concurrency` workers pull groups from one queue, so a freed slot is refilled the moment an
  implementer returns, never after the slowest of the wave.
- **Review on the branch** (MANDATORY-PRACTICES I10, kb/Work PB3027, PB3381). A DONE implementer's branch is reviewed
  by ONE read-only `cobol-reviewer` (the `review` skill's branch form) inside its slot THE MOMENT IT RETURNS, a
  same-file predecessor's too; FINDINGS go to a review finisher and the branch is reviewed again (`review_fix_rounds`,
  2). Round 1 reviews the whole branch; round 2 and later review ONLY the finisher's diff from the head the last round
  reviewed, plus the report's answers. Every finding carries a severity, and `review_findings.py verdict --blocking`
  decides: critical, high and medium block while a finisher can still answer them, critical and high only at the
  round after the last finisher; low never blocks; every non-blocking finding is a LEAD (in the report's `## Review
  findings` and the wave's `leads`), filed by the enqueue unit's clerk. Owner 2026-10-18: "We should not be holding
  finished work this long, continually postponing it. Something in our process is flawed." (wave 1118: PB3331's three
  whole-branch rounds found 5, 2 and 1 findings over 2.3 h and the cap turned one LOW finding into a DECISION row).
  Only a branch stamped `Review: CLEAN` at its head is ready; the landing never reviews. `review_findings.py summary
  <scratch>` measures it.
- **No lander, no train (kb/Work PB3023).** The wave ends when its groups return; every branch reviewed CLEAN is the
  loop's landing lane's (`land_lane.py`, kb/Work PB3103), which lands each with `scripts/orchestrator/enqueue.py land`
  (main merged in, the gate on both hosts under the gate slot, the DEVLOG stamp, the landing checks, one pull request
  into the merge queue); the `enqueue` unit handles the reviews, the finishers, the re-arms and the leads.
- **Same-file successors.** A file with more open notes than the cluster cap (`model_rules.json` `wave.max_notes_per_group`, the cap `plan_wave.py` hands `fix_clusters.py`) becomes
  two or more groups, and each later one declares `after: '<predecessor letter>'`. A wave dispatches only the first
  `wave.links_per_chain` links of each chain (the owner's ONE, kb/Work PB3638: "no wave may take hours"), placed in
  tier order so the highest tier heads the chain; the rest wait for the next wave. Where a wave does run a successor, it waits until its
  predecessor is SETTLED (its review loop ended: CLEAN, FINDINGS n, STOPPED, NO-RESULT or STALE; or it returned
  unfinished), merges the predecessor's FINAL branch (the review finisher's, when one ran: the reviewed head), and
  orients from that report's "For the next implementer" section and STATUS.md instead of re-surveying the file.
  NOTHING IS HELD (kb/Work PB3381, owner 2026-10-18: "Make the changes now and land the clean links as they come."): a
  predecessor reviewed CLEAN is ready at once and the landing lane lands it, the successor's pull request merging on
  top. The train-era hold (the predecessor "lands THROUGH the successor") saved nothing once every branch became its
  own pull request (kb/Work PB3023, PB3024) and only postponed: wave 1118's six-link chain had six groups DONE and
  none landed in seven hours. Whether a branch carries a predecessor's UNANSWERED findings is read from ONE source,
  the predecessor's FINDINGS FILE (its last verdict: FINDINGS with blocking findings at a head the branch contains),
  through ONE reader, `review_findings.py last_verdict` (CLI `last-verdict`), which the wave's `predecessorEnd` and
  `prune_worktrees.py carrying_findings` both call; never from a report stamp, because a review finisher rewrites the
  group's one report for its own branch with no Review line whatever it returns (branch review R3-1). While that
  verdict stands, the survey holds the successor (IN FLIGHT, `waits_on`) until the predecessor's own finisher has
  answered them and landed it. A predecessor CAPPED at the round after its last finisher keeps them for that
  next-wave finisher, and its successor neither answers them nor records them again; a predecessor whose review
  finisher was sent to answer them and came back without doing so (SPLIT, BLOCKED, STALLED, killed, with or without a
  commit) is UNANSWERED: its report no longer shows them, so the successor's review covers the predecessor's content
  and records again each finding that still holds, and those findings are the SUCCESSOR's alone: the survey holds it
  only until its own review is CLEAN over a head containing the predecessor's (`prune_worktrees.review_answers`),
  one owner per case (kb/Work PB3404). The survey finds findings files by NAME only (`review-*.jsonl` in
  the coordination scratch and one level under it, and every file a stamp names) and never opens one to decide
  whether it is one. Every content problem is a TEAR, the reader's to classify: a file it cannot open or read (a
  directory in its place, a read denied), a byte-order mark, bytes that are not UTF-8, no final newline, no record, or
  a line that is not a record under the one record schema the writers share (`review_findings.SCHEMA`). A TORN file
  holds every branch it names, whatever its records say (one with no readable name, through its wave name or the stamp
  that names it): the reader reads no verdict from it (verdict TORN, with its reason, file and line), the CLI
  `last-verdict` and the survey read that same answer, the CLI exits 0, the survey holds each branch it names and each
  branch containing a head it names and names the tear in the held row, the wave reads it UNANSWERED, never capped, the
  reader never raises, and every writer refuses it BEFORE writing until the operator mends it by hand (`review_findings.py
  repair <file>`, which removes a byte-order mark, keeps a last record that only lost its newline, removes any other
  torn tail and deletes a file left with no record; a file it cannot read is made readable first) (branch review R4-1,
  R5-1, R6-1, R6-2). A successor may also follow a group in the same technological
  area (a runtime file, then its emitter).
- **A campaign is a named cluster, and it is planned, not hand-picked** (kb/Work PB2120, design section 9.1). Process
  work the fix lane cannot see (`process_only`, sites outside `src/WiseOwl.Cobol.*`: the legacy retirement `PB2108`, the
  architecture review `PB1754`) runs from its notes' `cluster:`, `blocked_by:` and the owner's `tier:` (`tier:
  ["<lead>:1"]` ranks a ready note first in that campaign, kb/Work PB3423):
  `python scripts/spec/work.py next --cluster <lead>` lists it in tier, then dependency order,
  `python scripts/orchestrator/plan_wave.py --cluster <lead> --budget-points P --dry-run` plans a wave of it (its
  dependent notes become successors, `after:`, of the groups holding their blockers), and
  `pwsh scripts/orchestrator/orchestrate.ps1 -Cluster <lead>` alternates its waves with fix-lane waves until every
  note naming it has landed. Never hand-write a `groups.json` for a campaign; a note that needs Fable or Mythos is
  held for the owner's per-dispatch approval and dispatched by the attended session. An owner instruction every group of
  the campaign must meet goes in the lead note's `## Standing requirements` section (a bullet each, kb/Work PB3359):
  the planner puts it in the spec, the args entry and the branch reviewer's prompt of every group whose notes name that
  cluster, in a fix-lane wave as in a campaign wave; never hand-edit it into `groups.json`. A bullet is a case only
  that campaign has: the owner's SORT and file I/O test rule lives in MANDATORY-PRACTICES I11 alone and is never copied
  into a lead (kb/Work PB3369; `test_plan_wave.py` refuses the copy).
- **Why.** Orientation was ~46 % of every implementer's tokens (waves 45–57): a successor pays the file's context
  once, not once per cluster. It is a FRESH agent, not a longer transcript, because of the quadratic cost law (`references/cost-law-and-budget.md`):
  the cap exists because the sixth defect in one transcript costs more than a new agent, and the successor keeps the
  cheap part (the learned context) without the expensive part (the long transcript).
- **Args:** `{ scratch, wave, stop_file, global_stop, concurrency (6), implementer_model ('opus'; 'sonnet' when the owner allows it; a group's own `model` overrides it, so one wave can mix Sonnet and Opus groups),
  groups: [{ letter, lead, notes, codes, after? }], review_fix_rounds (2), reviewer_ceiling_min (90) }`.
- **Return payloads are capped** (kb/Work PB1912, owner 2026-10-02): an implementer's `summary` is at most 900 characters,
  `leads` at most 6 of at most 500, and a finisher's final text at most 25 lines, because everything an agent returns is
  re-read by the orchestrator on every later turn. The detail lives in the report file. Experiment pending: compare the
  workflow output size with waves 1001 (31,864 bytes) and 1002 (27,210).
- **Specs.** Render them with `make_dispatch_specs.py <groups.json>`, which takes the same groups plus `slug`,
  `group`, `root`, `files`, `body`, an optional `pred` text (predecessor or resume instructions) and the optional
  `after` (ignored by the renderer; the workflow uses it), and `file_set`, the list of files the group changes
  (`plan_wave.py` writes it; without it the renderer computes the set from the group's notes). Spec files are keyed
  on the full letter: `msg-w<wave>-<letter lower-cased>.txt`. Once its `check_practices.py` run is GREEN the renderer
  also RECORDS every group in the dispatch ledger (`dispatch_ledger.record_dispatch`, an upsert on wave and letter that keeps a re-rendered group's first `at`, kb/Work PB3392; `--coord
  DIR`), the ledger's one writer, so the planners' file-set partition and the landing check see a hand-planned wave too
  (kb/Work PB3186); a RED render records nothing (kb/Work PB3114). It refuses, before writing, a `scratch` or
  `stop_file` that is not an absolute path (and a scratch inside the checkout): quote Windows paths in Git Bash.
  `check_practices.py <groups.json>` refuses two groups whose claims MEET on ANY file both claim, a primary file or a
  secondary one (`inflight.collision`, the planner's own rule: either has no `member_set` entry for the file, so claims
  it whole, or a member of one matches a member of the other) unless one declares `after:` the other: two groups on
  different members of one file whose claims meet nowhere run side by side (kb/Work PB3670, PB3696). The partition reads members the same way (DESIGN-orchestrator-loop.md section 9 item 1b):
  give a note's `**Sites:**` paragraph its members (`path#Member`, or `` `path` (`member`, ...) ``, a `:line` after
  the path optional: MANDATORY-PRACTICES R3, `work.SITES_RULE`), or the planner claims the whole file, as it does for
  a name the file does not declare; a note with no site block holds nothing and is planned UNPARTITIONED, never held.
