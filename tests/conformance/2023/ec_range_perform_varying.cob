      *> EC-RANGE-PERFORM-VARYING (ISO §14.9.28.4 GR3, spec :29222; Table 13 Fatal): when a PERFORM VARYING/AFTER
      *> initializes an INDEX-NAME from a data-item FROM operand whose value is NOT POSITIVE (<= 0). NARROW — only an
      *> index-name target with a data-item (not literal, not index-name) FROM; a data-item induction variable and a
      *> literal FROM are out of GR3 scope. Under >>TURN … CHECKING ON the USE declarative catches the fatal EC and
      *> RESUME AT NEXT STATEMENT continues (§14.9.33). The FROM value is tested (GR3 — the data item, not the index).
      >>TURN EC-RANGE-PERFORM-VARYING CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EC-RNG-PV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-T.
          05 WS-E PIC 9 OCCURS 5 TIMES INDEXED BY IX.
       01 WS-U.
          05 WS-F PIC 9 OCCURS 5 TIMES INDEXED BY JX.
       01 WS-ZERO PIC S9 VALUE 0.
       01 WS-NEG  PIC S9 VALUE -1.
       01 WS-POS  PIC S9 VALUE 1.
       01 WS-DVAR PIC S9.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-RANGE-PERFORM-VARYING.
       H-P.
           DISPLAY "CAUGHT=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
      *> positive control: index-name FROM data item = 1 -> no raise.
           DISPLAY "P1-POS".
           PERFORM VARYING IX FROM WS-POS BY 1 UNTIL IX > 3 CONTINUE END-PERFORM.
           DISPLAY "P1-DONE".
      *> RAISE: index-name FROM data item = 0 -> fatal, caught, RESUME.
           DISPLAY "P2-ZERO".
           PERFORM VARYING IX FROM WS-ZERO BY 1 UNTIL IX > 3 CONTINUE END-PERFORM.
           DISPLAY "P2-AFTER".
      *> RAISE: index-name FROM data item = -1 -> fatal, caught, RESUME.
           DISPLAY "P3-NEG".
           PERFORM VARYING IX FROM WS-NEG BY 1 UNTIL IX > 3 CONTINUE END-PERFORM.
           DISPLAY "P3-AFTER".
      *> control: literal FROM 0 (BoundNumLiteral) -> out of GR3 scope, no raise.
           DISPLAY "P4-LIT".
           PERFORM VARYING IX FROM 0 BY 1 UNTIL IX > 3 CONTINUE END-PERFORM.
           DISPLAY "P4-DONE".
      *> control: data-item induction variable FROM 0 (not an index-name) -> out of GR3 scope, no raise.
           DISPLAY "P5-DVAR".
           PERFORM VARYING WS-DVAR FROM WS-ZERO BY 1 UNTIL WS-DVAR > 3 CONTINUE END-PERFORM.
           DISPLAY "P5-DONE".
      *> RAISE: AFTER-level index-name FROM data item = 0 -> fatal, caught, RESUME.
           DISPLAY "P6-AFTERLVL".
           PERFORM VARYING IX FROM WS-POS BY 1 UNTIL IX > 1
                   AFTER JX FROM WS-ZERO BY 1 UNTIL JX > 1
               CONTINUE
           END-PERFORM.
           DISPLAY "P6-AFTER".
           STOP RUN.
