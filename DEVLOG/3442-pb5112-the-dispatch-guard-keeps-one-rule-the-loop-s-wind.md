## Entry 3442 — 2026-11-10 12:05 PST — PB5112: the dispatch guard keeps one rule, the loop's wind-down; no hook says how a session delegates

The structural half of the retired CLAUDE.md rule 9. `scripts/hooks/dispatch_guard.py` refused a Workflow call, and an
implementer or finisher Agent call, unless the session had loaded the `workstream` skill and the brief was a rendered spec
that `check_practices.py` passed. The owner removed every delegation rule on 2026-11-10 ("You decide when to delegate"), so
both checks and their self-test cases are deleted. The hook keeps its rule 0: a unit of the orchestrator loop launches no
fleet once its supervisor has written STOP-UNIT (PB2597), which is the loop's own wind-down. Its self-test now holds that
rule and that nothing else is refused (15 cases), and no longer walks the tree, so it declares no population.
MANDATORY-PRACTICES O9, DESIGN-orchestrator-loop.md, the wave unit's first step and the SessionStart hook's lane text say
the same: the `workstream` skill is the loop's procedure and a tool for a session that runs a fleet, not a gate.

