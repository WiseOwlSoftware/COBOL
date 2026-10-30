      *>--- conformance-case
      *> id: pb911-perform-f3-empty-imperative-1
      *> rules: [FMT-14.9.28.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.28.2 Format 3 (the exception-checking PERFORM) prints
      *> imperative-statement-1 WITHOUT brackets, ahead of the WHEN group.
      *>   cite.py: OK  §14.9.28.2   (General formats)
      *> ISO §5.2.6.2 licenses an omission only for a BRACKETED portion and
      *> §5.2.6.3 requires one alternative of a brace group to be specified,
      *> so a Format-3 PERFORM whose WHEN follows PERFORM directly omits a
      *> required statement.
      *>   cite.py: OK  §5.2.6.2   (Brackets)
      *>   cite.py: OK  §5.2.6.3   (Braces)
      *> The statement is the only violation; the program is otherwise legal.
      *> Expected: COBOL2072 at the WHEN, the one diagnostic the Format-2
      *> spelling of the same omission draws (kb/Work PB911); the recovery
      *> draws no second one. ONLY 2023 is named: the exception-checking
      *> PERFORM is new in COBOL-2023 (Annex E.3.3 item 36).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB911A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           PERFORM
           WHEN EC-ALL
               ADD 1 TO N
           END-PERFORM.
           STOP RUN.
