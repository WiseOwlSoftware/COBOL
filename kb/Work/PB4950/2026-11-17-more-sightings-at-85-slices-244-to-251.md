## 2026-11-17 - more diagnostics seen in the 1985 code-read slices that cite 2023 rules, or name later-edition text, at `--std 85`

Filed by notes batch 22 of the Mythos lane (a clerk; nothing re-run). Each sentence is copied from the source named on its line. The fix this note owes is the one it already states: every diagnostic that cites a rule cites the edition in force, and the sweep covers every diagnostic that cites a rule. Harm: misleads_diagnosis (the refusal is right in each, the wording is wrong), severity MINOR.

**Sightings of the 2023 citation at `--std 85`** (sources: `E:/COBOL-coord/scratch/coderead-1985/out/<slice>.json`, the readers' notes; and the Mythos lane's rulings in `E:/COBOL-coord/scratch/my-20261115/`).
1. Slice 1985-244 (reader; ruling batch 18, "Leads that are not yet anything"): "some refusals at `--std 85` in this slice cite `§2023` in their message (a diagnostic naming the wrong edition's rule; it does not change what is refused). Nobody has listed which." The reader's notes: "Absolute and A R refusals cite §2023 rules at --std 85 (wording only)"; "The absolute-line and A R/NP R refusals cite §2023 (SR6 c/d) at --std 85 and rf4's message mentions the page footing's LINE-COUNTER where none exists: wording only, the limit applied is the 1985 one"; "the COBOL2631 refusal text cites §2023:8.3.5 at --std 85 (wording only)."
2. Slice 1985-245 (reader; ruling batch 19): "COBOL0864 cites `§2023:14.9.30.3 SR11` at `--std 85` (a diagnostic naming the wrong edition's rule; it does not change what is refused)." The reader's notes: "Several refusals carry generic or §2023-cited diagnostics (COBOL0001, COBOL1970, COBOL3734), a wording matter only".
   - `§2023:14.9.30.3 SR11`: "shall be specified in the RECORD KEY clause or an ALTERNATE RECORD KEY clause associated with file-name-1"
     `OK  §2023:14.9.30.3 SR11  (Syntax rules)  the text is in SR11`
3. Slice 1985-248 (reader; ruling batch 21: "refusals at `--std 85` cite `§2023` clauses in their messages (the third slice to see it; nobody has listed them)"). The reader's notes: "Diagnostics cite §2023:15.15.3 r1 at --std 85"; "Diagnostics cite §2023:15.64.3 r1 at --std 85"; "The diagnostic text cites the 2023 clause (§2023:15.36.3 r1) at --std 85 rather than §1989:A.2.14.3 AR1, a wording point only."
4. The folios refuter (`rulings-pb5789-folios.md`): "COBOL2199 cites `§2023:13.18.35.3 SR6c` at `--std 85` (a citation sibling of the slice-244 to 248 leads)."
   - `§2023:13.18.35.3 SR6c`: "no line appears above the upper limit or below the lower limit"
     `OK  §2023:13.18.35.3 SR6c  (Syntax rules)  the text is in SR6c`
5. Batch 17 (`rulings-code-reads-batch-17.md`, "Leads that are not yet anything"): "Diagnostics that cite a 2023 clause at `--std 85` (COBOL0864, COBOL2645, the refusals of ADD CORRESPONDING and of SET, COBOL1627): they join kb/Work PB5875."
6. Slices 250 and 251 (batch 23, `rulings-code-reads-batch-23.md`, "Leads"): "Both slices: messages cite §2023 at 85; this is the misleading-diagnostic lead of batch 22."

**Two wording leads of the same slices** (`rulings-code-reads-batch-23.md`, "Leads that are not yet anything", unchanged):
- Slice 250: at `--std 85` the page-heading tail COBOL2199 message names PRESENT WHEN, a later-edition phrase.
- Slice 251: COBOL0899 is placed at the RD line, and one message shows the internal name 'PageHeading'.

Related, already notes: kb/Work PB5875 (COBOL1778) and PB4939 (COBOL2112), the same kind for one code each.
