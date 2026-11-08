## Entry 3315 — 2026-11-08 03:12 PST — PB4991: rule lineage: one rule list per edition, linked by one change record between adjacent editions

The 1985 audit's results were ready to enter the inventory, and two designs disagreed on the shape: a second set of
1985 rows carrying 2023 verdicts by reference (the conformance-review design's §10.2), or one inventory with edition
bands (the conformance suite's design, which assumed the 1985 text would never be held). The owner asked for separate
rule lists per edition, with each later edition keeping, deprecating or removing each earlier rule and adding its own,
and for an efficient, maintainable mechanism. The design below was approved the same day (2026-11-08).

Each edition has its own rule list and inventory store. Each older rule carries one `next` change record against the
next held edition: KEPT, CHANGED or REMOVED, with its citation in both texts. A rule the next edition adds is NEW,
derived from what no record reaches. Obsolete and archaic are statuses of an edition's rule, so deprecation is KEPT into
an obsolete rule and then REMOVED. A verdict is held once per KEPT chain, on its newest row, so 2023's adjudicated work
serves every 1985 rule that carries into it. Each edition's GAP count, its conformance statement, the suite's bands, the
edition-gate drift test and each diagnostic's citation at each edition are derived from the chains. The owner noted the
last one: "a way for a compilation error to accurately describe the edition + rule violated" (PB4950, now fed by the
chains). Until 2002 and 2014 are transcribed, the 1985 records bridge to 2023.

DESIGN-spec-conformance-review.md §10.2 is rewritten. The conformance suite design's decision 10 now states that the
1985 text is held, and §3.5 derives bands from the chains. The slices are PB4992 to PB4997.

