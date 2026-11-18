## Lead: declare ListingJsonSchemaDriftTests a preflight class once the merge group has measured it (found by listing step 2)

DESIGN-listing.md section 7.3 says the schema drift test is a PREFLIGHT class: its red means a derived file
(`src/WiseOwl.Cobol.Listing/Json/listing-v1.schema.json`) is stale, which is what the preflight is for (kb/Work PB5872).
Step 2 could not declare it: `scripts/buildgraph.py`'s `preflight_findings` admits a class only when it is MEASURED in
`tests/_shared/test-weights.json` (its median merge-group seconds), and a class that has never run in a merge group has no
measurement, so declaring it on the branch would turn the BUILD TARGETS audit red. Until then the class runs in the Unit
population, where a red still fails the gate, only later than a preflight red would.

To do, after step 2 lands and a merge group has run the class: confirm
`WiseOwl.Cobol.Tests.Unit.Listing.ListingJsonSchemaDriftTests` appears in the weights file (it takes well under a second
locally), add it to the `unit` target's `preflight` map in `scripts/build_targets.json` with its reason ("the generated JSON
listing schema, src/WiseOwl.Cobol.Listing/Json/listing-v1.schema.json: red when the model changes and the schema was not
regenerated"), and run `python scripts/buildgraph.py check`. Fits in any later listing step's branch.
