      *>--- conformance-case
      *> id: pb5627-file-status-occurs-85
      *> rules: [1985-SR-VII.2.5.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5627. FILE STATUS IS W-FS where W-FS is a table element
      *> (§1985:VII.2.5.3 SR2 "two-character data item of the category
      *> alphanumeric"): the operand is written without the subscript a
      *> table element reference must carry (§1985:IV.4.3.8.2.3 SR4), and
      *> the clause is no context that writes none. The refusal names the
      *> 1985 rule, not §2023:12.4.5.8.3 SR1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5627B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb5627b.txt"
               FILE STATUS IS W-FS.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  F-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  W-T.
           05  W-FS PIC XX OCCURS 3 TIMES.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
