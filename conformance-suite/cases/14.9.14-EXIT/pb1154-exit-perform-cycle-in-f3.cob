      *>--- conformance-case
      *> id: pb1154-exit-perform-cycle-in-f3
      *> rules: [SR-14.9.14.3-8]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.14.3 SR8 — "The CYCLE phrase shall not be specified within an
      *> exception-checking PERFORM statement." An exception-checking PERFORM is
      *> not a loop, so there is no cycle to take.
      *>   cite.py: OK  §14.9.14.3 8)  (Syntax rules)
      *> The statement is the only violation; the program is otherwise legal.
      *> Expected: COBOLNET1604, positioned at the offending statement (kb/Work PB1154).
      *> ONLY 2023 is named: the exception-checking PERFORM is new in
      *> COBOL-2023 (Annex E.3.3 item 36).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1154C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 0.
       01 M PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           PERFORM
               ADD 1 TO N
               EXIT PERFORM CYCLE
           WHEN EC-SIZE
               DISPLAY "A"
           END-PERFORM.
           STOP RUN.
