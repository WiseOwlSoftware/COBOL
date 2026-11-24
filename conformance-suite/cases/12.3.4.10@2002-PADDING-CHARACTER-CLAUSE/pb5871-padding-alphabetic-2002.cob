      *>--- conformance-case
      *> id: pb5871-padding-alphabetic-2002
      *> rules: [2002-SR-12.3.4.10.2-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5871 - §2002:12.3.4.10.2 SR3: "Data-name-1 shall be a
      *>   one-character data item of category alphanumeric, defined in
      *>   the working-storage, local-storage, or linkage section."
      *>   §2002:8.5.2.1: "Where an alphabetic data item is not
      *>   expressly permitted, the alphabetic data item is treated as
      *>   though it were an alphanumeric data item." PD is a PIC A item
      *>   of one character, so it is a legal data-name-1 at COBOL-2002
      *>   and the program compiles (with the obsolete-clause warning)
      *>   and runs: OK. The clause is removed from COBOL-2014 on, so no
      *>   later edition reaches the rule.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5871P.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb5871p.dat"
              ORGANIZATION IS SEQUENTIAL
              PADDING CHARACTER IS PD.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 PD PIC A VALUE "X".
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT F
           MOVE "ABCD" TO REC
           WRITE REC
           CLOSE F
           DISPLAY "OK"
           STOP RUN.
