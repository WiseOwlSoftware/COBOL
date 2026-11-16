**2026-11-15, two more rows join this mechanism (the Mythos lane's batch-4 code reads, mechanism 9; source `kb/Work/PB5683/2026-11-15-rulings-code-reads-batch-4.md`).**

**Ruling: UPHELD (b), harm under_rejects.** The rows `1985-SR-XIII.4.9.3-2` and `1985-SR-XIII.3.18.3-1` are the mechanism of this note. `1985-SR-XIII.4.9.3-2` is this note's mechanism verbatim: USE BEFORE REPORTING `DET-A` with a Working-Storage `DET-A` compiles (`ubr_hom.cob`); it is not re-ruled. `1985-SR-XIII.3.18.3-1` is this note's other direction, the sibling this note records for MOVE: `SOURCE DFLD`, where `DFLD` is both a Working-Storage item and a printable report entry's data-name, compiles and moves the storage item (the report line is `RPV  WSV`; `src_hom.cob`). The program is illegal on either reading: unqualified, `DFLD` is a nonunique reference (`§1985:IV.4.3.8.1 SRL2.1`); as the report entry, SR10d forbids referencing a non-sum-counter report data-name and SOURCE SR1 forbids it as identifier-1. Whether the row is SR1's or the qualification rule's is bookkeeping; it joins this note. The checker's confidence: medium.

**Rows added.** `1985-SR-XIII.4.9.3-2` and `1985-SR-XIII.3.18.3-1`. `work.py check` refuses a 1985 rule id in `inventory_rows` (kb/Work PB5094), so the rows are named here.

**2023 rows whose stored verdict is wrong.** `SR-14.9.49.3-9` and `SR-13.18.53.3-4` (a registrar re-records them non-conforming). They are not in this note's `inventory_rows` today.

**Where** (the checker's reading): `src/WiseOwl.Cobol.Compiler/Binding/ReportGroupResolution.cs#Resolve`; `src/WiseOwl.Cobol.Compiler/Binding/ReportSectionBinder.cs#IsReportSectionOnlyName` (storage wins a homonym).

**Rules (each citation checked again with `python scripts/spec/cite.py --check`, verdict OK).**
- `§1985:IV.4.3.8.1 SRL2.1`: "For each nonunique user-defined name that is explicitly referenced, uniqueness must be established through a sequence of qualifiers"
  `OK  §1985:IV.4.3.8.1 SRL2.1  (4.3.8.1 Qualification)  the text is in SRL2.1`
- `§1985:XIII.3.9.3 SR10d`: "Data-name-1 may be referenced only if the entry defines a sum counter"
  `OK  §1985:XIII.3.9.3 SR10d  (3.9.3 Syntax Rules)  the text is in SR10d`
- `§1985:XIII.3.18.3 SR1`: "If identifier-1 is a Report Section item it must be a"
  `OK  §1985:XIII.3.18.3 SR1  (3.18.3 Syntax Rules)  the text is in SR1`
- `§1985:XIII.4.9.3 SR2`: "Identifier-1 must reference a report group"
  `OK  §1985:XIII.4.9.3 SR2  (4.9.3 Syntax Rules)  the text is in SR2`

**Repro** (`E:/COBOL-coord/scratch/my-20261115/contradictions-b4/src/`). Both compile at `--std 85` and at `--std 2023` (exit 0).

`ubr_hom.cob` (a Working-Storage `DET-A` beside the report group `DET-A`, named in USE BEFORE REPORTING; compiles):

```cobol
       IDENTIFICATION DIVISION.
       PROGRAM-ID. UBRHOM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPF ASSIGN TO "ubrhom.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPF REPORT IS R1.
       WORKING-STORAGE SECTION.
       01 WS-REC.
           02 WS-GRP PIC 9 VALUE 1.
           02 WS-OTH PIC 9 VALUE 0.
       01 DET-A PIC X.
       REPORT SECTION.
       RD R1 CONTROL IS WS-GRP.
       01 DET-A TYPE IS DETAIL.
           02 LINE PLUS 1.
               03 COLUMN 1 PIC 9 SOURCE WS-GRP.
       PROCEDURE DIVISION.
       DECLARATIVES.
       BR-SEC SECTION.
           USE BEFORE REPORTING DET-A.
       BR-P.
           CONTINUE.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       P1.
           OPEN OUTPUT RPF.
           INITIATE R1.
           GENERATE DET-A.
           TERMINATE R1.
           CLOSE RPF.
           DISPLAY "GRP=" WS-GRP.
           STOP RUN.
```

`src_hom.cob` (`DFLD` is a Working-Storage item and a report entry's data-name; `SOURCE DFLD` moves the storage item: the report line is `RPV  WSV`):

```cobol
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SRCHOM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPF ASSIGN TO "srchom.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPF REPORT IS R1.
       WORKING-STORAGE SECTION.
       01 WS-REC.
           05 DFLD PIC X(3) VALUE "WSV".
       REPORT SECTION.
       RD R1.
       01 DET TYPE IS DETAIL.
           02 LINE PLUS 1.
               03 DFLD COLUMN 1 PIC X(3) VALUE "RPV".
               03 COLUMN 6 PIC X(3) SOURCE DFLD.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT RPF.
           INITIATE R1.
           GENERATE DET.
           TERMINATE R1.
           CLOSE RPF.
           DISPLAY "DONE".
           STOP RUN.
```

**Expected:** both refused (the references are nonunique, or name a Report Section item the rule forbids). **Observed:** both compile at `--std 85` and at `--std 2023`; `src_hom.cob` prints `RPV  WSV`.
