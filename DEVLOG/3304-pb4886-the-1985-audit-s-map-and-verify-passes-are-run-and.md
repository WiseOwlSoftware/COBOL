## Entry 3304 — 2026-11-07 22:48 PST — PB4886: the 1985 audit's map and verify passes are run, and four leads are filed

The 1985 audit's Message Batches passes ran on account 1, with state under `E:/COBOL-coord/scratch/audit-1985/`.
- The claims pass (63 claims, $0.48): 59 CONFIRMED, 7 NOT-ADDRESSED, 0 REFUTED. VERSION_CHANGE_REFERENCE.md's 1985
  claims hold against the 1985 text.
- The map pass, with 15 candidates per rule (2,253 rules, $20.72): SAME 1,251, DIFFERS 538, NONE 464. An earlier run
  with 5 candidates gave false NONEs and was abandoned.
- The verify pass, one request per SAME (1,251, $7.41): UPHELD 747, REFUTED 486, UNSURE 18.
- Results: 747 rules carry their 2023 verdict, 504 are disputed and 1,002 are rows of their own. The disputed rows and
  the own rows go to fleet adjudication against `--std=1985`. The drafts pass for the own rows is in flight.

Leads filed: PB4968 (Section I's conformance prose has no catalog rows), PB4969 (43 sub-list rows share their first
list's citation), PB4970 (semgrep verify names DiagnosticCatalog.cs as unreadable, though the file parses alone) and
PB4971 (about 217 of the NONEs are candidate-retrieval misses, not deleted features).

Fixed in place: "fopting" on PDF page 634 (TYPE clause, syntax rule 5) is the printed page's own misprint, read on the
rendered page, so it is kept as printed and recorded KEPT in LEDGER-1985. The comment above `extract_rule_catalog.KINDS`
named the deleted `rule_catalog_1985.BLOCK_KINDS`; it now names the 1985 manifest. PB4886's note now says the addenda's
25 pipe-less change rows are on four pages, not one.

