      *>--- conformance-case
      *> id: g85_f04_05
      *> rules: [1985-GR-VI.6.11.4-9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.11.4 GR9 - "Additional rules and explanations relative
      *> to this statement are given under the appropriate paragraphs"
      *> (Scope of Statements, ROUNDED, ON SIZE ERROR, Multiple Results).
      *> Each referenced paragraph is exercised by a DIVIDE. M: format 1
      *> DIVIDE D INTO A B C ROUNDED, D = 0.3: A = 20 / 0.3 = 66.66..
      *> truncated to 66 (§1985:VI.6.4.1: truncation unless ROUNDED); B =
      *> 50 / 0.3 = 166.66.. exceeds PIC 99, a size error (§1985:VI.6.4.2
      *> GR3), so B keeps 50; C = 2 / 0.3 = 6.666.. ROUNDED into 999V9 is
      *> 6.7 ("increased by one in the low-order position whenever the most
      *> significant digit of the excess is greater than or equal to
      *> five"); the results for A and C are stored as if no size error
      *> occurred (§1985:VI.6.4.2 GR3, §1985:VI.6.4.6 GR2 left to right)
      *> and the ON SIZE ERROR branch runs once after all are done. S1/S2:
      *> END-DIVIDE ends the statement inside an IF (Scope of Statements),
      *> so the DISPLAY after it belongs to the IF: S1 takes NOT ON SIZE
      *> ERROR, S2 takes ON SIZE ERROR, and both print their AFTER line. A
      *> compiler that made the trailing DISPLAY part of a phrase would drop
      *> one AFTER line. C is shown through its group G-C.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0405.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  D   PIC 9V9 VALUE 0.3.
       01  ZR  PIC 9 VALUE 0.
       01  A   PIC 99 VALUE 20.
       01  B   PIC 99 VALUE 50.
       01  G-C.
           05  C   PIC 999V9 VALUE 2.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DIVIDE D INTO A B C ROUNDED
               ON SIZE ERROR DISPLAY "M SIZE ERROR"
               NOT ON SIZE ERROR DISPLAY "M NO SIZE ERROR"
           END-DIVIDE.
           DISPLAY "M " A " " B " " G-C.
           IF A > 0
               DIVIDE 2 INTO A
                   ON SIZE ERROR DISPLAY "S1 SIZE ERROR"
                   NOT ON SIZE ERROR DISPLAY "S1 NO SIZE ERROR"
               END-DIVIDE
               DISPLAY "S1 AFTER " A
           END-IF.
           IF A > 0
               DIVIDE ZR INTO A
                   ON SIZE ERROR DISPLAY "S2 SIZE ERROR"
                   NOT ON SIZE ERROR DISPLAY "S2 NO SIZE ERROR"
               END-DIVIDE
               DISPLAY "S2 AFTER " A
           END-IF.
           STOP RUN.
