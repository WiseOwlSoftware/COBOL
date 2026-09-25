# doc-rows-2 — LANDER brief (apply the §7 rows, record the verdicts, land)

⛔ FIRST read `E:\CobolSharp\.claude\skills\workstream\templates\MANDATORY-PRACTICES.md` WHOLE (all-roles + Lander + P10),
`E:\CobolSharp\CLAUDE.md`, and `kb/Work/PB1535.md` (the recording policy: a DOC row is recorded only WITH its §7 row;
wrong behaviour is never documented — rows document the INTENDED determination, verdict DIVERGES/PARTIAL against today's
code, claimed by the owning note). You are in a fresh worktree cut from main.

## Inputs — `{OUT}` (= E:\Temp\claude\E--CobolSharp\9db39f61-22be-4ee4-8ded-a0e19f9ebec3\scratchpad\doc2\out)
- `rows-d1.jsonl` … `rows-d7.jsonl` — writer rows + staged records (`row`, `record`, `owning_note`, `also_changes`).
- `refute-d1.jsonl` … `refute-d7.jsonl` — per item `upheld`, `kind`, `correction` (an overturned item's `correction`
  carries the corrected row / verdict / wording — APPLY IT; it supersedes the writer's).
- Superseded: items 31, 36, 38 in rows-d1 and 159, 167 in rows-d5 → take them from rows-d7 (+ refute-d7).
- Orchestrator decisions to apply: item 40 documents GnuCOBOL's COPY suffix AND directory order (rule 1) — the arm is
  added to PB1355; item 25 adds "a file with a byte-order mark whose content is malformed is a compile-time error naming
  the file and byte offset, no fallback" (and amends DOC-A.1-26 in the same change set); item 67 is trimmed to R44's decision
  like item 66 and recorded NOT-IMPLEMENTED under PB1086 (a DOC NOT-IMPLEMENTED still needs its anchor row — write the
  R44-only row); owner decisions kb/Work/R47 and R48 (R48 confirmed "keep R48") govern 31/36/38/146/159/167.

## Steps (checkpoint-commit after each; STATUS.md)
1. **Merge the FMT-5.2 branch first:** `git merge worktree-agent-a69372df1365cdda7` (w60b, PB1536 Q2). Then on the merged
   tree run `python scripts/spec/extract_rule_catalog.py`, `python scripts/spec/build_inventory.py`, and re-apply
   `{SCRATCH}\w60b\batch-1-fmt-15.78.2.json` (never merge catalog/inventory JSON as hunks). GAP −1.
2. **Build the final item set** (one script under `{SCRATCH}\doc2\land\`, reusing
   `adjudication/doc-rows-1/apply_rows.py`'s approach): for each of the 51 items take the writer row/record, apply the
   refuter's correction when overturned, and assert: one table line, exactly 4 cells, `code-location` starts with
   `docs/CONFORMANCE.md#DOC-A.1-N`, verdict in the schema's set. Print the table of item → verdict → owning note.
3. **Write the §7 rows** into docs/CONFORMANCE.md (replace an existing row for the item in place — e.g. 214; otherwise append in
   item order), then apply every `also_changes` edit the writers and refuters listed (rows 26, 56 duplicate, 62, 76, 106, 110,
   115, 143, 153, 160, 210, 216 and the §3 bullets they name; golden-text changes named there are NOT yours — list them as
   leads unless the row is false without them). Run `python scripts/spec/audit_annex_a1.py --check` (no findings) and
   `audit_doc_citations.py --check`.
4. **Record the verdicts:** a record_verdicts batch of the 51 records → `--dry-run`, then apply; read the GAP delta and the
   accepted-vs-submitted count (every record must be accepted).
5. **Register (same change set, CLAUDE.md rule 8):** every DIVERGES/PARTIAL/NOT-IMPLEMENTED row is claimed by its owning note's
   `inventory_rows`; drop rows from notes that no longer own them (PB1422 ← 14; PB1369 ← 23, 40; PB547 ← 36, 38; PB322 ← 52,
   and reword its determination E to documentation-only; PB1178 ← 192; PB1534 ← 105, 114, 124; PB1527 re-scoped to the STRING
   crash only — crashes true, wrong_answer false — and ← 217); extend notes with the refuters' new arms (PB1529 BIG*BIG,
   PB1355 suffix/directory order, PB1539 EXTERNAL AS sibling, PB1520 `long` property, PB833 lock-table flaw, PB1086 item-67
   design obligations, PB690 FileStatus.cs comment + '91'); a new mechanism with no owner gets a new note (next free id at
   landing time; ids PB1590+). `python scripts/spec/work.py check`; DefectiveRowCoverageDriftTests must be green.
6. **Gate (L2):** fetch the external corpus; build; the WHOLE Conformance assembly unfiltered + Unit + Characterization,
   Normal priority, blocking on verdict lines (P2). CI now runs the audits job too.
7. **Docs:** DEVLOG entry at the top (number after the final fetch; CRLF-safe); plan §0 (GAP; "0 rows never adjudicated");
   adjudication/doc-rows-2/LANDING.md (per item: verdict, owning note, what changed).
8. **Land:** commit (trailers `Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>` +
   `Claude-Session: https://claude.ai/code/session_011wQhKXa8jUvq5awAaoQTea`), fetch, rebase, `bash scripts/push-main.sh`
   from the Bash tool with PUSH-MAIN-EXIT blocking. Report ≤60 lines to `{SCRATCH}\reports\doc2-lander-report.md`.

GRACEFUL STOP: before each step check `{SCRATCH}\STOP`. ⛔ no git stash / --autostash.
