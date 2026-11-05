## Entry 3124 — 2026-11-04 18:24 PST — PB4614: the conformance-suite design's revision 3 is approved, and its slices are filed

The owner approved revision 3 of `docs/rearchitecture/DESIGN-conformance-suite.md` on 2026-11-04: decisions 1-17, then
the ten sub-decisions of decision 17 ("approve 17a-17j"). It replaces revision 2. It decides what a test covers (every
rule row and every general-format alternative), how a diagnosis is judged (at the ISO minimum, at a marked line, against
a control), how a test is placed at each edition and with what evidence, how NIST-85 subsumption and completeness are
measured, and, per decision 17, that the unit of the tree is a case file of closely related tests run by one generated
driver, with a verdict per test and a solo rerun for any test whose section is not a clean pass.

Two reviews (feasibility and architecture, fidelity to the standard) raised 29 findings against the first draft; a
checker raised 16 against decision 17's revision. Each was re-measured or re-cited and folded in. Every ISO quote passed
`cite.py --check`.

Its 17 new slices are filed as PB4683-PB4699 in the PB3160 campaign, ordered by `blocked_by`: S5 PB4683, S8 PB4684, S5b PB4685, S6 PB4686, S7 PB4687, S3 PB3178, S9 PB4688, S10 PB4689, S11 PB4690, S12 PB4691, S13 PB4692, S14 PB4693, S15 PB4694, S16 PB4695, S17 PB4696, S18 PB4697, S19 PB4698, S20 PB4699. The pilot,
PB3178 (S3), now waits on S7, as decision 15 orders. PB4614 is landed.

