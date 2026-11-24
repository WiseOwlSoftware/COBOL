## Entry 4039 — 2026-11-24 07:24 PST — PB3205 retired: the duplicated impact-map format was deleted with the impact map (PB4328)

**PB3205** (an R2 duplication finding of the PB1754 architecture review, read at cd85e4901) said the impact-map file
format was written twice: `scripts/spec/record_impact_map.py` (the writer) and `scripts/spec/impacted_tests.py` (the
reader) each held SCHEMA, the bitset codec, `default_store`, the positional tests row and the partial-map name, with
`scripts/gate_plan.py:map_timings` indexing the row by position.

**Re-probe on 4133169b64.** None of the three files exists. The PB4328 landing (a8c569fc19, DEVLOG 2943) replaced the
planned gate with the whole population and deleted the plan, the impact map, its store, its lane and `tools/impact`
together; `git log -S "def bit_ids" -- scripts/` ends there, and a grep of `scripts/` and `tools/` for the format's
names finds nothing live. The only code still reading an impact map is the frozen PB1708 evidence under
`docs/rearchitecture/evidence/impact-map-pb1708/`.

**Outcome.** The note is retired unlanded with that evidence (`status: retired`, `closes_rows: []` with its reason). No
code changed, so no gate measured code; no inventory row was claimed or re-verdicted. Codes COBOL8116-COBOL8118 are
returned unused.

