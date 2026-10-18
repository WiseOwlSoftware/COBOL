      *>--- conformance-case
      *> id: pb1154-file-sort-in-f3
      *> rules: [SR-14.9.40.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.40.3 SR3 — "A SORT statement shall not appear in
      *> imperative-statement-1 of an exception-checking PERFORM statement, in an
      *> input or output procedure, or in a declarative procedure." Printed under
      *> the FORMAT 1 (file) SORT heading; this is the file format.
      *>   cite.py: OK  §14.9.40.3 3)  (Syntax rules)
      *> The statement is the only violation; the program is otherwise legal.
      *> Expected: COBOLNET1617, positioned at the offending statement (kb/Work PB1154).
      *> ONLY 2023 is named: the exception-checking PERFORM is new in
      *> COBOL-2023 (Annex E.3.3 item 36).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1154J.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb1154j1.dat"
               ORGANIZATION LINE SEQUENTIAL.
           SELECT F2 ASSIGN TO "pb1154j2.dat"
               ORGANIZATION LINE SEQUENTIAL.
           SELECT SF ASSIGN TO "pb1154js.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(4).
       FD F2.
       01 R2 PIC X(4).
       SD SF.
       01 SR.
          05 SK PIC X(4).
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           PERFORM
               SORT SF ASCENDING KEY SK USING F1 GIVING F2
           WHEN EC-I-O
               DISPLAY "A"
           END-PERFORM.
           STOP RUN.
