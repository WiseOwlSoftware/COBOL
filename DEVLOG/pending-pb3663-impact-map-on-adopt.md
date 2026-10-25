## Owner decision: the loop records an impact map for every main commit it adopts (PB3663)

The owner asked whether all of the day's loop performance improvements are in use. The operator found that no impact map
has ever been recorded, because `.git/cobol-impact` does not exist. Every gate on 2026-10-24 therefore put every case in
tier 1, and its leg 1 was the cheapest slice of each assembly (36,802 of 38,134 Unit cases and 10,207 of 18,394
Conformance cases in gate 000004-20261024T221346Z), not the cases the change reaches. The rule that maps are recorded
"on demand, never per commit" (PB1708, 2026-09-28; PB1709 retired) dates from when every local gate ran the whole
population. Batched gating (PB2515) made leg 1 the only local run, and nothing ever asked for a map.

The owner answered the operator's question at about 17:10 PDT: "Yes, record the impact map automatically". PB3663 files
the decision at tier 1 of the PB1957 campaign. After each adoption (PB2699), the supervisor records one map for the newest
adopted commit, detached, at BelowNormal and inside a gate slot. A newer adoption coalesces into it, a
documentation-only adoption records nothing, and a failure is reported. The note's bar is the measured share of gates
that find a usable map. It warns that `impacted_tests.py`'s exact-base staleness rule may defeat the recorder, and that a
re-design to an ancestor map for unchanged files is in scope if the measurement shows that.
