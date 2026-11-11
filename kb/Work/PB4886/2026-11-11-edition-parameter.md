## PB5256 built the edition parameter for 2002

PB4886 listed `build_inventory.py` and the ledger "with the same edition parameter" as still to build. Since then the inventory
became a VIEW (PB4992): an edition's inventory is its catalog joined with its store, read by `inventory_view.py`,
`inventory_store.py` and `record_verdicts.py --edition`, each of which takes the edition, and the ledger and the session probe
iterate `inventory_schema.HELD` (`inventory_view.edition_views`). No `build_inventory.py` stands in their place, and none is
needed: adding an edition is a row of the schema's `editions.of` and a catalog.

PB5256 (kb/Work, landing with this branch) did that for 2002. `extract_rule_catalog.py --edition 2002` reads
`specs/ISO_COBOL_2002.md` with the ISO extraction 2023 uses (one `extract` over the `ISO` table of edition facts), the schema
holds 2002 (`tests/version-matrix/inventory-2002`, the 1985 store's fields, `linked: false` until the 1985 to 2002 records
exist), and the probe's `invent` line prints "2002 3290 rules · 3290 GAP" beside 1985's and 2023's. What remains of PB4886 is
unchanged: the 1985 audit's own adjudication (PB4995, PB4996) and the 1985 conformance statement (PB4885).
