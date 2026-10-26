      *>--- conformance-case
      *> id: pb1154-perform-f3-sr15-exception-name-repeat
      *> rules: [SR-14.9.28.3-15]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.28.3 SR15 — "All instances of an exception-name shall be
      *> specified only once in a format 3 PERFORM statement unless done so in
      *> conjunction with different file-names." EC-SIZE is written in two WHEN
      *> phrases with no file-name, so the second instance is the violation.
      *>   cite.py: OK  §14.9.28.3 15)  (Syntax rules)
      *> The statement is the only violation; the program is otherwise legal.
      *> Expected: COBOL1600, positioned at the offending statement (kb/Work PB1154).
      *> ONLY 2023 is named: the exception-checking PERFORM is new in
      *> COBOL-2023 (Annex E.3.3 item 36).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1154B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 0.
       01 M PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           PERFORM
               ADD 1 TO N
           WHEN EC-SIZE
               DISPLAY "A"
           WHEN EC-SIZE
               DISPLAY "B"
           END-PERFORM.
           STOP RUN.
