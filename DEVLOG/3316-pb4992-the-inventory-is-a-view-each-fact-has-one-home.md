## Entry 3316 — 2026-11-08 03:24 PST — PB4992: the inventory is a view: each fact has one home

The owner set two standing requirements on the lineage campaign (2026-11-08): "We must eliminate redundant code and
data. The new implementation should be as if we originally architected it. No minimal hacks", and "Consolidate code
where it makes sense. No god scripts or functions though". The first cut of slice 1 did the opposite. It seeded 2,253
empty 1985 store files that each copied nine catalog fields, and the 2023 store already copies four catalog fields and
stores a derived state in every file.

DESIGN-spec-conformance-review.md section 4 now gives each fact one home. The catalog owns the rule. A store file owns
the adjudication only, and a rule nobody has adjudicated has no file. The state is computed by the view, never stored.
Whether a 2023 rule exists at 85 is the lineage's, and the `85` token leaves the stored band once the derived and the
stored answers agree. A change record stores the ids it links, its reason and its source; its citations are read from
the catalogs. The store's files and the view are two modules (`inventory_store.py`, a new `inventory_view.py`), one job
each. PB4992 is rescoped to that view and every reader of it, PB4993 puts each rule's obsolete status in the catalog,
and PB4991 carries the standing requirements for every slice.

