      *> EC-DATA-NOT-FINITE (ISO §14.6.13.2 item 3, spec :24571; Table 13 Fatal): a standard-float SENDING operand
      *> whose content is NaN or ±Infinity is referenced. Raised at BOTH float read chokepoints — the numeric-value
      *> read (relation/arithmetic) and the string-image read (DISPLAY / STRING / MOVE-to-alphanumeric / a
      *> different-usage float MOVE source). The four exemptions do NOT raise: class condition, sign condition, a
      *> same-usage MOVE (COMP-2 -> COMP-2), and VALIDATE (not yet emitted). Under >>TURN … CHECKING ON the USE
      *> declarative reports the condition via FUNCTION EXCEPTION-STATUS (§15.33) and RESUME AT NEXT STATEMENT
      *> (§14.9.33) continues, so every leg is observable in one run. With checking OFF nothing raises.
      >>TURN EC-DATA-NOT-FINITE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EC-DNF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-INF USAGE COMP-2.
       01 WS-B   USAGE COMP-2.
       01 WS-S   USAGE COMP-1.
       01 WS-TXT PIC X(30).
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-DATA-NOT-FINITE.
       H-P.
           DISPLAY "CAUGHT=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           COMPUTE WS-INF = 1.0E300 * 1.0E300.
           IF WS-INF IS POSITIVE CONTINUE END-IF.
           DISPLAY "SIGN-EXEMPT-OK".
           IF WS-INF IS NUMERIC CONTINUE END-IF.
           DISPLAY "CLASS-EXEMPT-OK".
           MOVE WS-INF TO WS-B.
           DISPLAY "SAME-USAGE-EXEMPT-OK".
           DISPLAY "R1".
           DISPLAY WS-INF.
           DISPLAY "R2".
           IF WS-INF > 1.0 CONTINUE END-IF.
           DISPLAY "R3".
           MOVE WS-INF TO WS-S.
           DISPLAY "R4".
           STRING WS-INF DELIMITED BY SIZE INTO WS-TXT.
           DISPLAY "DONE".
           STOP RUN.
