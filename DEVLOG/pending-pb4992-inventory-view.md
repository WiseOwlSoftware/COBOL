## PB4992: the traceability inventory is a view; the store holds adjudications only

The first slice of the rule-lineage design (kb/Work PB4991, DESIGN-spec-conformance-review.md section 4 and section
10.2), under the owner's standing requirement of 2026-11-08: "We must eliminate redundant code and data. The new
implementation should be as if we originally architected it."

**What the re-probe showed.** On the base (6508b35773) every one of the 4,214 store files carried the same 13 keys: the
adjudication, plus copies of the catalog's `section`, `kind`, `ordinal` and `subject`, plus the derived `state`.
`build_inventory.py` regenerated the copies and the state on every rebuild, and about 24 readers read them from the
files. No rule-digest had drifted. The predecessor branch `lineage-slices` carried a first cut whose design notes had
already landed; its one unlanded commit (the lineage module, the per-edition store directories, the year-led ids) was
cherry-picked and restructured here.

**The shape.**
- `scripts/spec/inventory_view.py` (new) is the ONE view per edition: `inventory(edition)` joins the edition's catalog
  with its store, computes each rule's state (the newest edition by `Schema.state_for`, an older one through its change
  record), in spec order. It refuses an adjudication of a rule the catalog dropped, a store file holding a catalog field
  or a state, and an adjudication decided against other text (kb/Work PB698). `inventory_at(rev)` joins a commit's store
  with that commit's catalog under that commit's schema, so the ratchet and the witness audit see a schema change that
  reopens rows; a commit before this one is read as its whole rows. `--check` is the gate's new INVENTORY VIEW audit
  (and a step of CI's audits job), `--summary` the session probe's per-edition line, `--states` the C# parity witness.
- `scripts/spec/inventory_store.py` is the store's files only: `read_store`, `write_store`, `record_of` (the rule id,
  the digest, and the adjudicated fields that say something; a rule with no adjudication has no file), `store_at`.
- `inventory-schema.json` gained `store`: the keys a store file carries and each edition's adjudicated fields, the one
  list `inventory_schema`, `lineage.Edition` and the C# view's row read.
- `lineage.Edition` carries what differs between editions as data (its catalog, the catalog key that locates a rule,
  its store fields); `status` left the store (a fact of the rule, for the catalog: PB4993).
- `build_inventory.py` is deleted: seeding has no job, the carry is the store, the digest check is the view's refusal.
- The 2023 store was migrated once through the store's writer: 4,214 files lost five keys each.
- Every reader moved onto the view (record_verdicts, the five audits, gen_ledger, ledger_plan, phase_b_batch,
  derive_verdict_batch, gen_conformance_notes, work_inventory, inventory_ratchet, carry_retired_registers, the doc audit,
  the conformance generator, the fleet templates, the session probe). `gen_ledger`'s trend joins each commit's catalog
  and schema inside the same two git processes, so its structural bound holds.
- C#: `TraceabilityInventory` is the typed view (catalog joined with the store); `SpecTraceabilityInventoryDriftTests`
  holds its `DerivedState` to the Python view's state of every rule (`EveryRuleState_AgreesWithThePythonView`) and the
  view's row to the schema's store fields; the two catalog-copy tests went with the copies.

**What stays open.** PB4993 (the 1985 catalog's `status`, `next` as schema data, recording the audit's kept batch) and
PB4994 (the ledger's per-edition counts, the C# chain state) are their own mechanisms and go to a fresh agent; their
notes record what is already in place.

**Self-review.** Looked for: a second reader of the catalog (one, `inventory_schema.load_catalog`), a second home of
the store's fields (the schema's `store`), a stored derived value (none: `state` is refused by the view), an edition
branch where data can carry it (the record validator branches on the edition's fields, not its year), the history
readers' cost (the trend stays two processes), and shadowing in `audit_annex_a1` (renamed). No inventory row was
re-verdicted; GAP 2023 stays 0.
