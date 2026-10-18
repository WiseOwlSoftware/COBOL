      *>--- conformance-case
      *> id: pb1154-initialize-multi-in-f3
      *> rules: [SR-14.9.20.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.20.3 SR2 — "Identifier-1 shall be specified only once if the
      *> INITIALIZE statement is specified in imperative-statement-1 of an
      *> exception-checking PERFORM statement." The rule counts the metavariable
      *> identifier-1, which the general format repeats, so INITIALIZE N M names it
      *> twice (kb/Work PB417).
      *>   cite.py: OK  §14.9.20.3 2)  (Syntax rules)
      *> The statement is the only violation; the program is otherwise legal.
      *> Expected: COBOLNET1614, positioned at the offending statement (kb/Work PB1154).
      *> ONLY 2023 is named: the exception-checking PERFORM is new in
      *> COBOL-2023 (Annex E.3.3 item 36).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1154I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 0.
       01 M PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           PERFORM
               INITIALIZE N M
           WHEN EC-SIZE
               DISPLAY "A"
           END-PERFORM.
           STOP RUN.
