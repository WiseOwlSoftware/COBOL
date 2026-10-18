      *>--- conformance-case
      *> id: pb1154-close-multi-in-f3
      *> rules: [SR-14.9.6.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.6.3 SR3 — "A CLOSE statement that specifies more than one
      *> file-name shall not be specified in imperative-statement-1 in an
      *> exception-checking PERFORM statement."
      *>   cite.py: OK  §14.9.6.3 3)  (Syntax rules)
      *> The statement is the only violation; the program is otherwise legal.
      *> Expected: COBOLNET1612, positioned at the offending statement (kb/Work PB1154).
      *> ONLY 2023 is named: the exception-checking PERFORM is new in
      *> COBOL-2023 (Annex E.3.3 item 36).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1154G.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb1154g1.dat"
               ORGANIZATION LINE SEQUENTIAL.
           SELECT F2 ASSIGN TO "pb1154g2.dat"
               ORGANIZATION LINE SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(4).
       FD F2.
       01 R2 PIC X(4).
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT F1 F2.
           PERFORM
               CLOSE F1 F2
           WHEN EC-I-O
               DISPLAY "A"
           END-PERFORM.
           STOP RUN.
