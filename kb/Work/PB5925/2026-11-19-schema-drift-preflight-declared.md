## Closed: the schema drift test is a preflight class (lead 2026-11-18-schema-drift-preflight; branch listing-step-pb5951)

`tests/_shared/test-weights.json` now measures `WiseOwl.Cobol.Tests.Unit.Listing.ListingJsonSchemaDriftTests` (0.3 s), so kb/Work
PB5951's branch declares it in the `unit` target's `preflight` of `scripts/build_targets.json` with its reason (the generated
`listing-v1.schema.json`, red when the model changes and the schema was not regenerated). `python scripts/buildgraph.py check`:
"=== BUILD TARGETS: GREEN ... the preflight's 12 class(es) of 4 target(s) are declared, measured and within their budget". The
design's section 7.3 says so.
