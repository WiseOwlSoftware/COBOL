## Entry 2578 — 2026-10-25 14:58 PDT — model_rules.json stops typing the implementer's turn cap (PB3741)

`scripts/orchestrator/model_rules.json` (`wave.max_notes_per_group_source`) ended its re-measure paragraph with "inside the
220-turn cap the dispatch spec states". The implementer cap is 400 (`.claude/agents/cobol-implementer.md` `maxTurns`, read
by `coord.agent_turn_cap`; kb/Work PB3701), so the sentence was false, and it was a second place a cap number lived.

Re-probe: the 220 was still there on main (204718793); PR #566 had removed the sibling "245 turns, past the 220-turn cap"
sentence but not this one. A sweep of `scripts/orchestrator/*.json`, `scripts/`, `docs/` and `.claude/` found no other
typed current cap; the remaining 220 and 245 hits are historical notes (PB1700, PB2852, PB3015, PB3611, PB3641, PB3704)
and frozen evidence, which describe what was true when written.

Fix: the sentence now says "inside the implementer's turn cap, which the dispatch spec states (coord.agent_turn_cap,
kb/Work PB3701)", so the number lives once. To keep it that way the PB3701 drift check
(`check_practices.turn_cap_copies`) gained `NUMBERLESS_CAP_FILES` (the rules file) with `TYPED_CAP`, which refuses
"N-turn cap", "turn cap (of) N" and "cap of N turns" while leaving measured figures such as "about 147 turns" alone.
`test_plan_wave.py` plants a "220-turn cap" in a copy of the rules file and expects exactly that refusal (4 refusals in
total with the existing three).

Rows: none claimed, none closed (process tooling). Self-review: the new regex is deliberately narrower than the briefs'
`LITERAL_CAP`, because the rules file's prose legitimately quotes measured turns per group size.

