## PB3777, PB3780: an implementer reads one instruction file, a loop unit loads the overlay only; PB3788's public half is prepared and waits for the owner's push

The skills and brief token-optimization campaign (cluster PB3776), wave 20261709 group A. The owner, 2026-10-26 ~14:50,
on the Mythos measurement of 2026-10-25: "get it back from Mythos and act on it". All three notes are process tooling; none
claims an inventory row.

**PB3777: the spec and the brief that restated each other are one file.** Re-probed on the tree: the rendered spec (14.2 KB)
and `fix-lane-implementer-brief.md` (13.7 KB) both carried the checkpoint protocol, the gate paragraph, the DEVLOG entry, I9,
the citation rule and the STOP rule; the new `check_practices.repeated_sentences` run over the old pair finds 7 sentences
stated twice. `templates/dispatch-spec-implementer.md` is now the implementer's one instruction file, every paragraph once,
in the order the work runs. The fix-lane brief and the generic `implementer-brief.md` (nothing dispatched it) are deleted and
every caller changed with them (rule 4: no forwarder): `check_practices.BRIEFS` carries their patterns on the spec's entry
and eight more for what the fold moved, `NUMBERLESS_CAP_BRIEFS` names the template, `dispatch_guard.py` takes a rendered spec
only for an implementer, the agent definition drops its restatement of the gate, the checkpoint and the report, and SKILL.md
section 6, MANDATORY-PRACTICES P5 and O9 and DESIGN-orchestrator-loop.md say so. The old "read MANDATORY-PRACTICES WHOLE"
order is not carried: PB3778 measures that the harness cuts that file to a preview and renders each role's rows into the spec.
The guard is `repeated_sentences` (a sentence of 60 characters or more stated twice in the template or a rendered spec is
RED, seen RED on a planted repeat) and the dispatch guard's self-test, which now admits an implementer only with a spec the
real renderer wrote. Measured on this group's own render: 27,847 characters in two files before, 18,574 in one after (about
4.4K tokens, inside the acceptance's 4.5K). Not yet observed: the next wave's S2 table without the brief row.

**PB3780: the loop units load the overlay only.** The one thing that ordered the base load was the first block of
`workstream/SKILL.md`. It now orders none and names, with full paths, the reference each job reads once. `SKILL.md` went from
28,995 to 13,953 bytes (about 3.2K tokens, not the note's estimated 2.5K, because the loop block and the landing path stay
whole): section 2's rolling-wave narrative moved unchanged to `references/rolling-wave.md`, the attended session's duties
unchanged to `references/landing.md`, the rest cut to commands, args, the stop and steer rules, the landing path, the
allocation rules and the templates. `units/common.md` names which reference each unit kind reads. `check_practices.overlay_problems`
(in the FLEET PRACTICES audit) refuses a workstream overlay that orders its base's load, exceeds 15,000 bytes or names a
reference that does not exist; each arm seen refused on a plant. The practice "invokes its brent-tools base skill first" is
removed for workstream in the same change (O1); gate, review and spec-lookup keep it. Not yet observed: a unit's stream log with
no `agent-fleet` load.

**PB3788: prepared, not landed.** The split of the public plugin is built and linted but not pushed: a push to
`BrentRector/claude-skills` is the owner's yes after he has seen the diff (memory: review public posts first), and this
repository's pin cannot move to a commit the remote does not hold. In a clone of the public repository: `brent-tools` keeps nine
skills and the four agents at the repository root; `brent-tools-extras` (`extras/`: its own manifest, README and LICENSE, the six
skills, the nine eval cases and their README) holds `spec-compliance-audit`, `roslyn-analysis`, `claude-cloud-sessions`,
`automating-agent-guardrails`, `devlog` and `cross-platform`; both at 1.23.0; every mention of a moved skill from a core
skill is `brent-tools-extras:<skill>`; `scripts/lint_skills.py` lints every plugin the marketplace lists and refuses an eval
case in a plugin that does not ship its skill; `claude plugin validate` passes for both plugins. Nothing in this repository
changes for it yet (its `.claude/settings.json` already enables `brent-tools` alone). The note stays open with the commit, the
patch and the steps that remain.

**Self-review.** Architecture: one file per role's instruction, one checker function per rule (`repeated_sentences`,
`overlay_problems`), no wrapper left behind. Full code: the guard's implementer arm refuses a template brief; `overlay_problems` resolves
a base reference by its full path so a bare name is not misread. Performance: nothing on a hot path. Duplication: the gate paragraph, the
checkpoint protocol and the DEVLOG sentence now exist once in the spec; the agent definition no longer carries them.

**Gate.** (verdict line recorded below once the gate has run)
