      *> CONTINUE AFTER arithmetic-expression SECONDS (ISO §14.9.9, COBOL-2023): a timed pause. A negative interval
      *> is forced to 0 (GR1a) and, when EC-CONTINUE-LESS-THAN-ZERO checking is enabled, sets that nonfatal
      *> exception (GR1b) then continues. Observed via FUNCTION EXCEPTION-STATUS (a 31-char left-justified name).
      >>TURN EC-CONTINUE-LESS-THAN-ZERO CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CONT-AFTER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-N PIC S9 VALUE -3.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "A".
           CONTINUE AFTER 0 SECONDS.
           DISPLAY "B".
           CONTINUE AFTER WS-N SECONDS.
           DISPLAY "EC=" FUNCTION EXCEPTION-STATUS.
           STOP RUN.
