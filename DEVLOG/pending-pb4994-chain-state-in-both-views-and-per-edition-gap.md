## PB4994: an older row's chain state in the C# view, and the GAP count per edition in the ledger

Slice 3 of the rule-lineage campaign (kb/Work PB4991, DESIGN-spec-conformance-review.md section 10.2). PB4992 had put the
chain state (`lineage.state_of`) and the probe's per-edition `invent` line in Python; this slice does the rest.

Re-probe on the build: `inventory_view.py --summary` printed `2023 4214 rules · 0 GAP | 1985 2253 rules · 1087 GAP`, so the
probe half stood and needed no change. Two things were missing. The C# view (`TraceabilityInventory`) joined the newest
edition only, so no C# engine computed a 1985 state; and `gen_ledger.measure_inventory` read the newest edition alone.

Fix shape. `TraceabilityInventory` now holds every edition the schema's `editions.of` gives a store (`Editions`, `RowsOf`), and a
row carries its change record (`Row.Next`, kind and `to`). The 1985 catalog numbers some rules with a null ordinal, which the
join reads as 0. The drift test's `StateAt`/`StatesOf` mirror `lineage.state_of` (a KEPT row is OK when every rule it carries
into is OK at the next held edition and its band places the chain's test at the older edition), and the new
`EveryOlderEditionsRuleState_AgreesWithThePythonView` holds them to `inventory_view.py --states --edition` (1985: 2,253 rules,
1,087 GAP on both sides). `TheseChecks_ActuallyFail_OnAFabricatedInventory` plants each way a KEPT row stays a GAP (band lacks
the older edition, target not OK, no target, empty `to`) plus the CHANGED and newest-edition arms. `inventory_view.edition_views`
and `gaps` are the one per-edition read; the probe's `summary` and `gen_ledger.measure_inventory` both use it, and the ledger's
GAP tile names each older edition's GAP. The ledger's trend stays the newest edition's GAP, v1.0's metric.

Rows closed: none (a tooling slice).
