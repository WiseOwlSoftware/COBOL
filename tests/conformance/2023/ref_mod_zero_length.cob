      *> REF-MOD-ZERO-LENGTH directive (ISO/IEC 1989:2023 §7.3.23; §8.4.3.3.4 item 5c;
      *> Annex E.3.3 item 23). When the directive is ON a reference-modification result
      *> MAY be zero-length: WS-X(3:0) is allowed WITHOUT raising EC-BOUND-REF-MOD, even
      *> under >>TURN EC-BOUND-REF-MOD CHECKING ON (the zero-length source MOVEd to the
      *> 5-char receiver space-fills it; EXCEPTION-STATUS stays clear). The directive
      *> relaxes ONLY the zero-length aspect (§8.4.3.3.4 item 5c "the result may also be
      *> zero") — an out-of-range leftmost WS-X(7:2) (7 > the 5-position size) STILL
      *> raises the fatal EC-BOUND-REF-MOD, which the USE AFTER EXCEPTION declarative
      *> reports and RESUME AT NEXT STATEMENT continues past (WS-Z unchanged).
      >>REF-MOD-ZERO-LENGTH ON
      >>TURN EC-BOUND-REF-MOD CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. REFMOD-ZL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X(5) VALUE "HELLO".
       01 WS-Y PIC X(5) VALUE "-----".
       01 WS-Z PIC X(2) VALUE "??".
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-REF-MOD.
       H-P.
           DISPLAY "CAUGHT=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE WS-X (3:0) TO WS-Y.
           DISPLAY "Y=[" WS-Y "]".
           DISPLAY "S1=" FUNCTION EXCEPTION-STATUS.
           MOVE WS-X (7:2) TO WS-Z.
           DISPLAY "Z=[" WS-Z "]".
           STOP RUN.
