      *>--- conformance-case
      *> id: pb4919-rerun-records-without-file
      *> rules: [FMT-12.4.6.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4919 - the X3.23-1985 RERUN clause's integer-1
      *> RECORDS phrase without file-name-2. The printed general format
      *> (§1985:VII.2.12.2, page VII-17) closes one brace pair over
      *> [END OF] {REEL | UNIT} and integer-1 RECORDS and follows it with
      *> OF file-name-2, outside brackets: the records counted are
      *> file-name-2's, so file-name-2 is required. The grammar had
      *> transcribed the arm as integer-1 RECORDS [OF file-name-2] and
      *> compiled this clause clean at 85.
      *> Expected: COBOL6496 at every edition (from 2002 the clause is
      *> also COBOL0902, removed).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P4919RN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "P4919RN".
       I-O-CONTROL.
           RERUN ON SYSRR EVERY 100 RECORDS.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "P4919RN".
           STOP RUN.
