      *>--- conformance-case
      *> id: pb1154-perform-f3-no-when
      *> rules: [FMT-14.9.28.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.28.2 Format 3 — the WHEN phrase is a required group of the
      *> exception-checking PERFORM general format; a PERFORM that carries only
      *> FINALLY is not a Format-3 PERFORM and has no meaning to compile.
      *>   cite.py: OK  §14.9.28.2   (General formats)
      *> The statement is the only violation; the program is otherwise legal.
      *> Expected: COBOL1597, positioned at the offending statement (kb/Work PB1154).
      *> ONLY 2023 is named: the exception-checking PERFORM is new in
      *> COBOL-2023 (Annex E.3.3 item 36).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1154A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 0.
       01 M PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           PERFORM
               ADD 1 TO N
           FINALLY
               DISPLAY "F"
           END-PERFORM.
           STOP RUN.
