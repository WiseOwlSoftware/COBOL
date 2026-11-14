## 2026-11-13: step A as built, and what the re-probe changed in the design

The design (`docs/rearchitecture/DESIGN-orchestrator-loop.md` section 16.8) said `generate.py`'s completeness report reads
the register, so its lazy import of `conformance_source` would need a CODE_READS entry. The probe on this tree shows
otherwise: `generate.py` asks `Schema.register_items()`, which reads the registers' extract
(`tests/version-matrix/conformance-register.json`), never the Markdown. No change to `generate.py`, no CODE_READS entry
(`inert_lane.py check` GREEN with its 6 READ entries unchanged). `audit_derivations.py` likewise reads section 8 through the
extract and holds no parser.

The one place `inventory_schema.py` opened the document was `DerivedSelector.register` (the `determination-prefix` arm read
`register_determinations()` from disk, cached). It now reads `Schema.register_leads()` (the extract's `annexA1` leads, whose
length the DOC AUDITS audit holds over every schema `determination-prefix`), and `inventory_view.py`'s no-Markdown probe also
asks it, so the probe covers the arm it had not.

Outputs before and after on the same tree are byte-identical: `audit_annex_a1.py --json`, `audit_derivations.py --check`,
`gen_ledger.py --check` and `doc_audits.py --check` (the extract-fresh audit is the proof that `--write-register` writes the same
file), plus `gen_ledger.measure_conformance()` and `differential_fuzz.declined_modules()` compared old against new in one
process (equal).

Left for the later steps: `inventory-schema.json`'s `$comment` at the `anchor-exempt-verdicts` paragraph still says "one parser
per language (`section7_rows` / `ConformanceRegister`)". It is a code-lane file (the schema is a compiler test input), so it is
corrected with the first step that changes a compiler input anyway (PB5431 touches `src/`).
