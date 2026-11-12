**2026-11-12, the 1985 re-read 2 (records-85f): four more rows of this root, recorded PARTIAL naming this note.**

The re-read (`E:/COBOL-coord/scratch/coderead-85/out-reread2/rr-02.json`, `rr-03.json`, `rr-04.json`) found the same root in four rows: the Report Writer grammar is the 2023 format and `VersionConformancePass` has no 1985 arm for the addition.

- `1985-FMT-XIII.3.9.2` (read: PARTIAL): `TYPE CH ON data-name` and `OR PAGE`, already named in this note.
- `1985-GR-XIII.3.19.4-4` (read: PARTIAL): the ROUNDED half (a 9V9 entry prints 37 where the 1985 MOVE gives 35); the spaces half landed with kb/Work PB5248.
- `1985-FMT-XIII.3.5.2` (read: PARTIAL): the RD additions, with two the note did not list: `PAGE IS 60` and `PAGE ARE 60`.
- `1985-FMT-XIII.3.22.2` (read: PARTIAL): `VALUES ARE "HELLO"` and bare `VALUES "HELLO"` in a report entry. Here the site is different: `src/WiseOwl.Cobol.Compiler/Binding/ReportSectionBinder.cs#BindReportEntry` routes a report VALUE through `src/WiseOwl.Cobol.Compiler/Binding/DataBinder.cs#CheckValueConnective` with the 2023 paired connective and no edition arm. The 1985 function says "The VALUE clause defines the value of Report Section printable items" (`§1985:XIII.3.22.1`, checked with `scripts/spec/cite.py --check`), and the format prints only the VALUE IS form.

Rerun on the pinned compiler (origin/main 8147f008b3, 2026-11-12, `E:/COBOL-coord/scratch/coderead-85/reprobe-85f/log.txt`): `E:/COBOL-coord/scratch/coderead-85/out-reread2/work-rr-04/v1b.cob` (`VALUES ARE`) and `s4b.cob` (`SUM ... ROUNDED`) compile and run with exit 0 at --std 85.
