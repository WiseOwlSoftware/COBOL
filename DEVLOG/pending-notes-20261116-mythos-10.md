## Eight notes and three finding files from the Mythos lane's batch-8 rulings of 2026-11-16

Filed in kb/Work (documents only, no compiler source, no case, no inventory record). Each defect note carries the rule text with its `cite.py --check` line, the repro program in full, expected and observed at `--std 85` and `--std 2023`, and a Where paragraph with its Sites block.

- PB5823 (defect, MAJOR): at `--std 85` the Report Writer sends a relative group of line sum 0 (a control footing with LINE PLUS 0) to the next page after a saved absolute NEXT GROUP, where the 1985 fit test keeps it on the page.
- PB5825 (defect, MAJOR): OPEN INPUT of a file already open OUTPUT or EXTEND fails with status 41 and runs no declarative, though USE ... ON OUTPUT or ON EXTEND is declared (every edition).
- PB5826 (defect, MINOR): at `--std 85` a relation or sign condition with no reference to a variable compiles clean, where the 1985 text requires one; the two sentences have no row in the 1985 catalog. The row `1985-SR-VI.6.22.3-3` itself is confirmed.
- PB5827 (defect, MINOR): at `--std 85` COBOL2973 refuses a data item of a sort-merge file's record as an operand of another file's statement, a restriction that first appears in 2002; its message cites a 2023 rule.
- PB5824 and PB5830 (adjudications, open): TERMINATE's control break level (FINAL or data-name-1), and the first line of a page heading after a report heading, which rests on the reconstructed folio XIII-30 (kb/Work PB4952).
- PB5828 (defect, process only): a tooling-lane gate cannot finish while the gate pool is full (a pipeline finding of 2026-11-15, not re-measured).
- PB5829 (analysis, process only): six 2023 rules whose recorded test, on one adjudicator's reading, cannot fail on the rule. Nobody has attacked these readings.

Three dated finding files beside existing notes: `kb/Work/PB5782/2026-11-16-a-second-row.md` (the row `1985-GR-XIII.3.7.4-2`), `kb/Work/PB5821/2026-11-16-the-report-module-row.md` (the row `1985-GR-XIII.4.8.4-4`), and the ruling copied unchanged as `kb/Work/PB5683/2026-11-16-rulings-code-reads-batch-8.md`.

**The rate so far at 1985:** 1,044 rows read; 76 contradicted, about 7.3 %, and one more defect found outside its row. Open readings: PB5789, PB5799, PB5800, PB5805, PB5812 (two rows), PB5817, PB5824, PB5830 and the EXTEND question beside PB5815.
