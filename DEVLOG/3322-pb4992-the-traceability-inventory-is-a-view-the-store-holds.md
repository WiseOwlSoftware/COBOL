## Entry 3322 — 2026-11-08 06:36 PST — PB4992: the traceability inventory is a view; the store holds adjudications only

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
- `inventory-schema.json`'s `editions.of` is the one home of the editions: each edition's year, its band spelling and,
  for a held edition, its catalog, locator, store directory, the single file that held it before PB4148 and the
  adjudicated fields its store holds (`store.keys` beside). `inventory_schema.Edition` is built from it (the order,
  the next held edition and the bridge computed); no code names a year, so adding an edition is one entry there.
  `status` left the store (a fact of the rule, for the catalog: PB4993).
- A view reads ONE tree, `inventory_schema.Tree`: a working tree at a root, or a commit. Every file the state rule
  reads (the schema, the catalog, the store, docs/CONFORMANCE.md's registers, the Annex A.2 list) comes from it, so a
  commit's view is that commit's whole state and `doc_audits.py --root` reads that root's inventory.
- `build_inventory.py` is deleted: seeding has no job, the carry is the store, the digest check is the view's refusal.
- The 2023 store was migrated once through the store's writer: 4,214 files lost five keys each.
- Every reader moved onto the view (record_verdicts, the five audits, gen_ledger, ledger_plan, phase_b_batch,
  derive_verdict_batch, gen_conformance_notes, work_inventory, inventory_ratchet, carry_retired_registers, the doc audit,
  the conformance generator, the fleet templates, the session probe). `gen_ledger`'s trend joins each commit's catalog
  and schema inside the same two git processes, so its structural bound holds.
- C#: `TraceabilityInventory` is the typed view (the newest held edition's catalog joined with its store, both named by
  the schema); `SpecTraceabilityInventoryDriftTests` holds its `DerivedState` to the Python view's state of every rule
  (`EveryRuleState_AgreesWithThePythonView`) and the view's one row type, read through its own reader, to the schema's
  adjudicated fields. The two catalog-copy tests went with the copies, and the C# copy of the store's layout refusal
  went too: the store refuses a stray, misnamed or case-colliding file once, in Python, in every lane.

**Branch review round 1.** Six findings, each fixed at its root. R1-1: the editions, their spellings and store
directories lived in four hand tables (lineage's `EDITIONS` and `BAND_NAME`, the store's `INVENTORY_DIRS_REL`) and
two functions branched on 2023; the schema's `editions.of` now holds them, and `lineage.py --self-test` adds a 2027
held and unheld and watches the order, the bridges, the newest edition, the band and the store follow. R1-2: the C#
store-layout test was a second validator of the Python store's refusal, with a vacuous guard; deleted. R1-3: a
commit's view read the working tree's registers and A.2 list; the Tree reads the commit's (a self-test commits its
own registers and sees them), and `gen_ledger`'s trend reads them in its same two processes. R1-4: the doc check's
inventory ignored its root; a self-test plants a root and finds that root's one broken anchor. R1-5: two stale
names in the design docs. R1-6: one C# row type, with the field map on the row and the schema test going through the
view's own reader. The merge of main took PB4973's adjudication of GR-14.9.43.4-10 into the store's shape.

**Branch review round 2.** R2-1: the merge queue keeps a branch's commits, so after landing this branch's own WIP
checkpoint 47beeeca5e (adjudication-only records under a schema with no `editions.of`) would sit in main's history,
where `backfill_closes_rows.py` crashed on it with a bare KeyError and `gen_ledger`'s trend silently dropped it. The
root is the walk, not the reader: a history reader asks for LANDED states only. Both walks now go `--first-parent`
from `inventory_store.landed_head` (HEAD's merge base with origin/main), so every commit they read is a landing;
backfill attributes a landing by the messages of every commit it brought in. `load_schema` refuses a tree whose
schema lacks a key it needs, naming the commit and the key, and no reader of the old shape was added. Self-tests plant
the refusal (`inventory_view`) and a merged branch whose WIP checkpoint the walk must not read (`backfill_closes_rows`).
No first-parent commit of main reads a schema at all today: every one before this landing stores whole rows, which the
view returns as committed. R2-2: `audit_annex_a1`, `audit_code_citations`, `extract_annex_a2`, `measure_sublist_levels`
and the fleet's `build_lane1_inputs` template each held their own copy of the 2023 catalog path; each reads it from
its edition now.

**What stays open.** PB4993 (the 1985 catalog's `status`, `next` as schema data, recording the audit's kept batch) and
PB4994 (the ledger's per-edition counts, the C# chain state) are their own mechanisms and go to a fresh agent; their
notes record what is already in place.

**Self-review.** Looked for: a second reader of the catalog (one, `Edition.rules`; `load_catalog` is deleted and
`extract_rule_catalog`'s output paths are the schema's), a second home of the editions (the schema's `editions.of`), a stored derived value (none: `state` is refused by the view), an edition
branch where data can carry it (the record validator branches on the edition's fields, not its year), the history
readers' cost (the trend stays two processes), and shadowing in `audit_annex_a1` (renamed). No inventory row was
re-verdicted; GAP 2023 stays 0.

