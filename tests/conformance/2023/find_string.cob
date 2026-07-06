      *> ISO §15.37 FIND-STRING — the 1-based position of argument-2 within argument-1. Default: the FIRST
      *> occurrence; LAST (rule 1) the last; [START AFTER] argument-3 (rule 2) ignores that many matches first;
      *> ANYCASE (rule 4) folds case. No match / zero-length argument returns 0 (rules 3, 5). HAY = "ABCABCABC"
      *> has "ABC" at non-overlapping positions 1, 4, 7.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INTRFIND.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-HAY  PIC X(9)  VALUE "ABCABCABC".
       01 WS-NDL  PIC X(3)  VALUE "ABC".
       01 WS-TXT  PIC X(11) VALUE "Hello World".
       01 WS-P    PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION FIND-STRING(WS-HAY WS-NDL) TO WS-P.
           DISPLAY "FIRST=" WS-P.
           MOVE FUNCTION FIND-STRING(WS-HAY WS-NDL LAST) TO WS-P.
           DISPLAY "LAST=" WS-P.
           MOVE FUNCTION FIND-STRING(WS-HAY WS-NDL START AFTER 1) TO WS-P.
           DISPLAY "SKIP1=" WS-P.
           MOVE FUNCTION FIND-STRING(WS-HAY WS-NDL LAST START AFTER 1) TO WS-P.
           DISPLAY "LASTSKIP1=" WS-P.
           MOVE FUNCTION FIND-STRING(WS-TXT "WORLD" ANYCASE) TO WS-P.
           DISPLAY "ANYCASE=" WS-P.
           MOVE FUNCTION FIND-STRING(WS-TXT "WORLD") TO WS-P.
           DISPLAY "CASED=" WS-P.
           MOVE FUNCTION FIND-STRING(WS-HAY "ZZZ") TO WS-P.
           DISPLAY "NONE=" WS-P.
           STOP RUN.
       END PROGRAM INTRFIND.
