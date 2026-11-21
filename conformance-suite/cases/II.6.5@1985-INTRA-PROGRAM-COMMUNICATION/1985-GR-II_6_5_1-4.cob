      *>--- conformance-case
      *> id: 1985-GR-II_6_5_1-4
      *> rules: [1985-GR-II.6.5.1-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:II.6.5.1 GR4 (INTRA-PROGRAM COMMUNICATION). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-99.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R02DECL.
      * 1985 II.6.5.1 (4): a declarative procedure is activated
      * when an I-O error condition occurs (USE AFTER ERROR).
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "R02NOFILE.DAT"
               FILE STATUS IS FS1.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 F1-REC PIC X(3).
       WORKING-STORAGE SECTION.
       01 FS1 PIC XX VALUE "00".
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F1.
       D1-P.
           DISPLAY "DECL-ACTIVATED FS=" FS1.
       END DECLARATIVES.
       M SECTION.
       M1.
           OPEN INPUT F1.
           DISPLAY "AFTER-OPEN FS=" FS1.
           STOP RUN.
