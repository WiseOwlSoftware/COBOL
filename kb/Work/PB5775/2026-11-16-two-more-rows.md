**2026-11-16, two more rows join this mechanism (the Mythos lane's batch-9 code reads, mechanism 4; source `kb/Work/PB5683/2026-11-16-rulings-code-reads-batch-9.md`).**

**Ruling: UPHELD (b), harm under_rejects.** The rows `1985-SR-XIII.3.7.3-1` and `1985-SR-XIII.4.3.3-1` are the mechanism of this note: report names are resolved apart from data names, so an unqualified CONTROL name that a Report Section item also spells binds silently to WORKING-STORAGE, and `GENERATE DTL2` compiles when DTL2 names both a detail group and a data item. It is this note's mechanism verbatim for GENERATE (its title names GENERATE), and its `IsReportSectionOnlyName` "storage wins a homonym" arm for CONTROL. Unqualified, each is a nonunique explicit reference, which `§1985:IV.4.3.8.1 SRL2.1` forbids; qualified (`W-KEY OF WS-A`, `DTL2 OF RPT`) each is legal. The checker's confidence: high.

**Rows added.** `1985-SR-XIII.3.7.3-1` and `1985-SR-XIII.4.3.3-1`. `work.py check` refuses a 1985 rule id in `inventory_rows` (kb/Work PB5094), so the rows are named here.

**2023 rows whose stored verdict is wrong.** `SR-13.18.16.3-2` and `SR-14.9.16.3-1` (the cause is (b); a registrar re-records them non-conforming). They are not in this note's `inventory_rows` today.

**Where** (the checker's reading): `src/WiseOwl.Cobol.Compiler/Binding/ReportGroupResolution.cs#Resolve` (report groups only); `src/WiseOwl.Cobol.Compiler/Binding/ReportSectionBinder.cs#IsReportSectionOnlyName` (storage wins a homonym).

**Rules (each citation checked again with `python scripts/spec/cite.py --check`, verdict OK).**
- `§1985:IV.4.3.8.1 SRL2.1`: "For each nonunique user-defined name that is explicitly referenced, uniqueness must be established through a sequence of qualifiers"
  `OK  §1985:IV.4.3.8.1 SRL2.1  (4.3.8.1 Qualification)  the text is in SRL2.1`
- `§1985:XIII.3.7.3 SR1`: "Data-name-1 must not be defined in the Report Section"
  `OK  §1985:XIII.3.7.3 SR1  (3.7.3 Syntax Rules)  the text is in SR1`
- `§1985:XIII.4.3.3 SR1`: "Data-name-1 must name a type detail report group and may be qualified by a report-name"
  `OK  §1985:XIII.4.3.3 SR1  (4.3.3 Syntax Rules)  the text is in SR1`

**Repro** (`E:/COBOL-coord/scratch/my-20261115/contradictions-b9/src/`). Both compile and run at `--std 85` and at `--std 2023` (exit 0), printing `COMPILED AND RAN`.

`ctlhom.cob` (the unqualified CONTROL name `W-KEY` is spelled by a Working-Storage item and by a Report Section sum counter of `RD RPT`; compiles):

```cobol
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CTLHOM.
      * B9 MECHANISM 3: UNQUALIFIED CONTROL NAME W-KEY IS SPELLED BY
      * A WORKING-STORAGE ITEM AND BY A REPORT SECTION SUM COUNTER.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-F ASSIGN TO "CTLHOM.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-F REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-A.
           02  W-KEY       PIC X  VALUE "P".
           02  W-AMT       PIC 99 VALUE 0.
       REPORT SECTION.
       RD  RPT CONTROL IS W-KEY.
       01  DE-G TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC 99 SOURCE W-AMT.
       01  CF-W TYPE CONTROL FOOTING W-KEY LINE PLUS 1.
           02  W-KEY COLUMN 5 PIC 99 SUM W-AMT.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT RPT-F.
           INITIATE RPT.
           MOVE 1 TO W-AMT.
           GENERATE DE-G.
           TERMINATE RPT.
           CLOSE RPT-F.
           DISPLAY "COMPILED AND RAN".
           STOP RUN.
```

`genhom.cob` (`GENERATE DTL2`, where `DTL2` is a detail group and a Working-Storage item, not unique without qualifiers; compiles):

```cobol
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GENHOM.
      * B9 MECHANISM 3: GENERATE DTL2, WHERE DTL2 IS A DETAIL GROUP
      * AND A WORKING-STORAGE ITEM (NOT UNIQUE WITHOUT QUALIFIERS).
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-F ASSIGN TO "GENHOM.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-F REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-G.
           05  DTL2        PIC X  VALUE "W".
       REPORT SECTION.
       RD  RPT.
       01  DTL2 TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "DTL".
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT RPT-F.
           INITIATE RPT.
           GENERATE DTL2.
           TERMINATE RPT.
           CLOSE RPT-F.
           DISPLAY "COMPILED AND RAN".
           STOP RUN.
```

**Expected:** both refused as nonunique references (`§1985:IV.4.3.8.1 SRL2.1`). **Observed:** both compile and run at `--std 85` and at `--std 2023`, printing `COMPILED AND RAN`.
