## PB3701 — the implementer turn cap had two values; it now has one home, the agent's `maxTurns`, and a check that every copy matches it

**What was wrong.** The owner raised the implementer cap from 220 to 400 on 2026-09-30 (kb/Work R67). MANDATORY-PRACTICES P5 and `.claude/agents/cobol-implementer.md` (`maxTurns: 400`) said so, but `dispatch-spec-implementer.md` still rendered `Turn cap 220`, `check_practices.py` REQUIRED that text, the two implementer briefs said "HARD CAP of 220 / ~190", and O4 still said "220 turns". Implementers therefore SPLIT at 220 (PB3641 measured groups of three to five splitting about half the time) and each split cost a finisher dispatch. The re-probe on this build: `check_practices.py` was GREEN while the spec and the harness disagreed by 180 turns, so the check was the defect as much as the template.

**The fix, at the root.** The harness enforces exactly one number per role, the frontmatter `maxTurns` of its agent file, so that field is the one home (a second home in `model_rules.json` would have been a copy needing its own check).
- `coord.agent_turn_cap(role)` reads it (frontmatter only, LF or CRLF; a role without one raises).
- `make_dispatch_specs.render` fills `{turn_cap}` in the spec's `Turn cap` line and its sizing line from it.
- `check_practices.py` requires the placeholder in the template and `Turn cap <maxTurns> -` in a rendered spec (`RENDERED_TURN_CAP`), and `turn_cap_copies` (in the default run: the gate's FLEET PRACTICES audit and CI) checks every other statement of a cap against its role: P5 (implementer, read-only, finisher), O4 (implementer, finisher), the cost-law table, the workstream skill, the finisher brief. A statement whose text vanished fails too, since it would otherwise stop being checked.
- The two implementer briefs now state no number (they point at the spec's `Turn cap` line; "~190" became "about seven-eighths of that cap", and P5's "about 350 turns" the same), and `turn_cap_copies` refuses a typed number in either.
- Siblings found by the sweep and fixed: O4's `220 turns`, the cost-law table row, DESIGN-test-build-ci 3.14.9 ("each at most about 220 implementer turns"), the report instruction "stopped at the 220-turn cap" in both briefs. DESIGN-orchestrator-loop 4.1 records the rule.

**Tests.** `coord.py --self-test` covers `agent_turn_cap` (LF, CRLF, a `maxTurns` in the body only, no frontmatter). `test_plan_wave.py` block 7c2 pins the rendered spec (both places, no placeholder left), the refusal of a spec saying 220, and, on a scratch copy of the checked files, a differing copy, a vanished statement and a typed number in a brief, each refused. 430/430 plan_wave checks.

**Rows.** None: process tooling; no ISO rule governs it and the note claims no inventory row.

**Not done here.** Whether eight notes per group still fits at 400 is PB3704 (group cap 4).
