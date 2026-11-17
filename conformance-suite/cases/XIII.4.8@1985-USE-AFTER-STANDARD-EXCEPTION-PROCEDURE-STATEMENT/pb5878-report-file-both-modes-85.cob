      *>--- conformance-case
      *> id: pb5878-report-file-both-modes-85
      *> rules: [1985-GR-XIII.4.8.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5878. §1985:XIII.4.8.4 GR5b runs the OUTPUT procedure
      *>   "for any file open in the output mode or in the process of being
      *>   opened in the output mode", and GR5c says the same of the extend
      *>   mode. An OPEN EXTEND that fails 41 on a report file open in the
      *>   output mode meets both sentences, and the text orders neither:
      *>   WiseOwl runs the procedure of the mode the file is open in, as
      *>   GnuCOBOL does (CLAUDE.md rule 1; docs/conformance entry
      *>   D-USE-OPEN-MODE-BOTH). A compiler that runs the attempted mode's
      *>   procedure, or both, fails.
      *> DERIVATION. OPEN EXTEND of RPT-E open OUTPUT fails 41 and runs
      *>   USE-OUTPUT; after CLOSE, OPEN EXTEND succeeds and OPEN OUTPUT of
      *>   RPT-E open EXTEND fails 41 and runs USE-EXTEND. Each procedure
      *>   runs before the DISPLAY that follows its statement.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5878RW.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-E ASSIGN TO "pb5878re.dat"
               FILE STATUS IS FS-E.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-E
           REPORT IS REP-E.
       WORKING-STORAGE SECTION.
       01  FS-E PIC XX VALUE "??".
       REPORT SECTION.
       RD  REP-E.
       01  DTL-E TYPE IS DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "EEEEE".
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-OUT SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON OUTPUT.
       D-OUT-1.
           DISPLAY "USE-OUTPUT " FS-E.
       D-EXT SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON EXTEND.
       D-EXT-1.
           DISPLAY "USE-EXTEND " FS-E.
       END DECLARATIVES.
       MAIN-1 SECTION.
       P-1.
           OPEN OUTPUT RPT-E.
           DISPLAY "OPEN-E1 " FS-E.
           OPEN EXTEND RPT-E.
           DISPLAY "OPEN-E2 " FS-E.
           CLOSE RPT-E.
           OPEN EXTEND RPT-E.
           DISPLAY "OPEN-E3 " FS-E.
           OPEN OUTPUT RPT-E.
           DISPLAY "OPEN-E4 " FS-E.
           CLOSE RPT-E.
           STOP RUN.
