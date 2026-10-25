## PB3389, PB3721, PB3654: the register derives `blocked`, reads a glob as a sweep, and refuses a note it cannot read

Three defects of one reader, `scripts/spec/work.py`, each a silent drop or a stale copy; each found by a landing or a plan
that went wrong without the register saying so.

**PB3654 — a note the reader could not parse left the register without a word.** Re-probe: a directory holding a
backtick-prefixed `PB3653.md` (PR #513 merged three such notes) and a good `PB3650.md`, `work.py --work <dir> check`,
printed the repository's own `✓ 3025 work items` and exit 0: `load` skipped every file whose frontmatter did not parse, and
`check` ignored `--work`. `work.read_note(name, text)` now answers a file not named like a note (`PB<n>.md`, `A<n>.md`,
`R<n>.md`) with a skip and one that is with a refusal naming the first bad line (the opening fence is not line 1, a
byte-order mark hides it, the block is never closed). `load` stops on a refusal, so `next`, the planner, the ledger and
every other reader fail closed; `check` reports each refusal and takes `--work` (the audits that compare the register with
the inventory, the R3 program and the Bases view run for the repository's own `kb/Work` only). The planner's own copy of
the skip, `plan_wave.load_notes`, refuses the same way. Four fixture cases (every line prefixed, no opening fence, never
closed, byte-order mark) and a `--self-test` arm that writes the fixture into a directory and runs `register_problems`,
`load` and `check` over it.

**PB3389 — a stored `blocked:` went stale when a blocker landed ahead in the queue.** The flag was a second spelling of
`blocked_by`, and `check` held it to the list, so its verdict depended on OTHER notes' status: PR 256, 268, 275, 444 and
493 passed every local check and then failed CI, three of them removed from the merge queue (PR 493 sat ten hours). Whether a note is blocked is
now derived and never stored: `work.waiting_on(note, by_id)` (every non-terminal `blocked_by` note, an unknown id counting
as open, plus `status: owner` or `blocked` for a hold on something that is no note) is read by `actionable`,
`cluster_order`, the ledger and the fix lane's population, and `check` refuses a `blocked:` key. Measured before the 3,025
`blocked:` lines were deleted: the stored value equalled the derived one for every non-terminal note on main, so no note
moved between ready and waiting. `PB670` and `PB296`, the two `blocked: true` notes with no blocker the note feared losing,
are landed, and the case needs no `blocked_on:` field: it is `status: blocked`. The public `fix_clusters.py` skips by a
stored flag and is not edited (a public change needs the owner's text first): `plan_wave.ready_notes_view` hands it a
directory of the ready notes, `.agent-fleet.json` `skip_flag` lost `blocked`, and `ledger_plan` lost its `STATE_FLAGS`
special case. `kb/Work.base` reads a `formula.blocked` that spells the same rule in the Bases language (NOT run in
Obsidian here: `check` holds that it names every terminal status, `owner` and `blocked`, and that no view reads a stored
`blocked`). Hazard for a branch in flight: a new note carrying `blocked:` is refused after main merges in; delete the line.

**PB3721 — the site reader dropped a path it could not claim.** Re-probe: PB3637, whose block names `scripts/**/*.py`,
declared `()` and `sites_problems` accepted it, so wave 20261107 planned it beside five groups on files it rewrites. A glob
is now a sweep: on a tree it expands with git's `:(glob)` pathspec over the committed files (`work.expand_globs`) and
claims each file whole (PB3637's claims 170); the arm chosen over refusing a glob, which forces a hand list that drifts
from what the sweep touches. `sites_problems` also refuses a held note whose block claims nothing (a path under no root, a
dead glob) unless it says `**Sites:** none (why)`. It flagged PB3702, PB3706 and PB3742, which name `.claude/` files, the
root gap of PB3720, so `campaign.src` now lists `.claude/skills`, `.claude/agents` and `.claude/workflows`; PB3720's
remaining drift test needs the owner's call on which tracked files outside every root are roots (28 open notes name some:
conformance goldens, `tools/`, `kb/`) and stays open with that measurement.

**Self-review.** Architecture: one function per answer (`read_note`, `waiting_on`, `expand_globs`), each read by every
caller; no wrapper left. Full code: `check` no longer reads other notes' status to judge a note, so a merge can change no
verdict; a stored `blocked:` refuses loudly because ignoring it would let `blocked: true` silently hold nothing.
Performance: `expand_globs` runs git only for a note that names a glob; the view copies about 700 notes per plan.
Duplication: audited the other stored fields of a note for a derivable copy: none depends on another note's status.
`gen_ledger.py` keeps counting an unparseable file in a historical tree as no note, on purpose (a comment says why).

**Review round 1 (R1-1, blocking).** The branch made `work.py check` refuse a stored `blocked:` key and deleted the line
from the notes it had, but a branch is gated at its own head and the landing re-runs no register audit on the head main
was merged into, so the nine notes main added since the base (PB3766 to PB3775) would have gone red for the first time in
the merge group's CI. Merging the reviewed head into a branch on current main reproduced it (9 problems); the nine lines
are deleted. The root is the landing, not those nine notes: any in-flight branch filing a note after this lands meets the
same rule only in CI. `enqueue.py checks` gained a third landing check, `work.py check`, run by the merged head's own
`scripts/spec/work.py` from the worktree (the copy CI runs), exit 6 with the DEVLOG refusal (a finisher's); the check
table became a `Check` named tuple (name, command, exit, range arguments, `in_tree`) in place of a five-field tuple, and
the self-test maps all three refusals and runs a worktree copy of work.py that refuses and one that passes.

Merging the reviewed head into current main also met `scripts/orchestrator/cancel_superseded.py`, a script main added after PB3637's sweep that carried its own `sys.stdout.reconfigure`: the standard-streams audit (COPY and ENTRY rules) was red on the merged tree, so it now calls `ensure_utf8()` first in its `__main__` block and its private copy is deleted.
