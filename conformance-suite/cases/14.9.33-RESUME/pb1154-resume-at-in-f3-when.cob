      *>--- conformance-case
      *> id: pb1154-resume-at-in-f3-when
      *> rules: [SR-14.9.33.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.33.3 SR1 — a RESUME in a WHEN phrase of an exception-checking
      *> PERFORM shall specify the NEXT STATEMENT phrase; RESUME AT procedure-name is
      *> the declarative form.
      *>   cite.py: OK  §14.9.33.3 1)  (Syntax rules)
      *> The statement is the only violation; the program is otherwise legal.
      *> Expected: COBOLNET1610, positioned at the offending statement (kb/Work PB1154).
      *> ONLY 2023 is named: the exception-checking PERFORM is new in
      *> COBOL-2023 (Annex E.3.3 item 36).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1154F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 0.
       01 M PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           PERFORM
               ADD 1 TO N
           WHEN EC-SIZE
               RESUME AT DONE
           END-PERFORM.
       DONE.
           STOP RUN.
