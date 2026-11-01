## Entry 2887 — 2026-10-31 16:06 PDT — PB4269: a gate that cannot attribute its change runs tier 1 whole

The owner decided on 2026-10-31: "Run the whole tier". A gate whose change the impact map cannot attribute precisely
now puts every tier-1 case, and every genuinely new 0u case, in leg 1, exempt from `LEG_ONE_BUDGET`,
`LEG_ONE_UNKNOWN_BUDGET` and the collection cap. It no longer takes the cheapest 2 % of a superset chosen by duration.
PR #880's GREEN gate had run the cheapest 2 % of 19,609 tier-1 conformance cases and left out the 11 it broke.

Two causes count as unattributed. One is an `every_tier1` reason: no usable map, a grammar or build input, a file the
map never saw. The other is a changed file that differs between the map's commit and the base, which
`impacted_tests.drifted_change` takes at the file level and now names in `Reach.drifted`. `gate_plan.whole_tier_reason`
gives the one reason, `build_plan` carries it into the plan and every assembly, and the gate's verdict line ends
`tier 1 whole (kb/Work PB4269): <reason>`. A change attributed by method keeps the budget.

The cost, measured over the 41 gate plans of 2026-10-28 to 2026-10-31: 19 had an `every_tier1` reason, with a median
tier 1 of 89 % of the population, and those gates now run nearly everything. The reasons were grammar, build or
generator inputs (8), files new since the map (5), non-C# inputs under src/ (5) and test data (1). Closing those holes
(PB4270, PB4228, PB2696) makes the gates fast again. The self-tests of `gate_plan.py` (nine new arms),
`impacted_tests.py` (three) and `run_gate_legs.py` are green. The design doc, CLAUDE.md, plan §0, the `gate` skill,
MANDATORY-PRACTICES I1 and the implementer spec describe the rule.

