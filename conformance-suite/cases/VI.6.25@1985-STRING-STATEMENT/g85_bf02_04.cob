      *>--- conformance-case
      *> id: g85_bf02_04
      *> rules: [1985-GR-VI.6.25.4-10, 1985-GR-VI.6.25.4-11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.25.4 GR10 - with no overflow, NOT ON OVERFLOW's
      *> imperative-statement-2 runs and ON OVERFLOW is ignored (A, B:
      *> every statement of imperative-statement-2 runs; D: a GO TO in it
      *> transfers control and skips D-SKIPPED); a missing A-NOOVF, a
      *> present A-OVF or a D-SKIPPED line fails.
      *> §1985:VI.6.25.4 GR11 - END-STRING ends the statement: the DISPLAY
      *> after it runs whether an overflow phrase ran (A, B) or not (C,
      *> where the overflow skips imperative-statement-2), and inside an
      *> IF the statement after END-STRING belongs to the IF (E-IN-IF) and
      *> ELSE pairs with the IF; without END-STRING, C-AFTER would be part
      *> of imperative-statement-2 and would not be shown.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0204.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-R     PIC X(4).
       01  WS-FLAG  PIC X VALUE "Y".
       01  WS-CNT   PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE SPACES TO WS-R.
           STRING "AB" DELIMITED BY SIZE INTO WS-R
               ON OVERFLOW DISPLAY "A-OVF"
               NOT ON OVERFLOW DISPLAY "A-NOOVF"
           END-STRING
           DISPLAY "A-AFTER [" WS-R "]".
           MOVE SPACES TO WS-R.
           STRING "WXYZ" DELIMITED BY SIZE INTO WS-R
               NOT ON OVERFLOW
                   DISPLAY "B-NOOVF"
                   ADD 1 TO WS-CNT
           END-STRING
           DISPLAY "B-AFTER [" WS-R "] " WS-CNT.
           MOVE SPACES TO WS-R.
           STRING "ABCDE" DELIMITED BY SIZE INTO WS-R
               NOT ON OVERFLOW DISPLAY "C-NOOVF"
           END-STRING
           DISPLAY "C-AFTER [" WS-R "]".
           MOVE SPACES TO WS-R.
           STRING "Q" DELIMITED BY SIZE INTO WS-R
               NOT ON OVERFLOW GO TO D-PARA
           END-STRING
           DISPLAY "D-SKIPPED".
       D-PARA.
           DISPLAY "D-PARA [" WS-R "]".
           IF WS-FLAG = "Y"
               STRING "EF" DELIMITED BY SIZE INTO WS-R
                   ON OVERFLOW DISPLAY "E-OVF"
               END-STRING
               DISPLAY "E-IN-IF [" WS-R "]"
           ELSE
               DISPLAY "E-ELSE"
           END-IF
           DISPLAY "E-END".
           STOP RUN.
