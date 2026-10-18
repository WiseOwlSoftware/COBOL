      *>--- conformance-case
      *> id: pb1154-validate-multi-in-f3
      *> rules: [SR-14.9.50.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.50.3 SR6 — "A VALIDATE statement that specifies more than one
      *> identifier shall not be specified in an exception-checking PERFORM
      *> statement." VALIDATE N M names two identifiers.
      *>   cite.py: OK  §14.9.50.3 6)  (Syntax rules)
      *> The statement is the only violation; the program is otherwise legal.
      *> Expected: COBOLNET1607, positioned at the offending statement (kb/Work PB1154).
      *> ONLY 2023 is named: the exception-checking PERFORM is new in
      *> COBOL-2023 (Annex E.3.3 item 36).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1154D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 0.
       01 M PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           PERFORM
               VALIDATE N M
           WHEN EC-SIZE
               DISPLAY "A"
           END-PERFORM.
           STOP RUN.
