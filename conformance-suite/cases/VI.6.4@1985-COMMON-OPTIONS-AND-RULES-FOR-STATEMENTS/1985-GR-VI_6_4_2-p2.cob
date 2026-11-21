      *>--- conformance-case
      *> id: 1985-GR-VI_6_4_2-p2
      *> rules: [1985-GR-VI.6.4.2-p2, 1985-GR-VI.6.4.2-p3, 1985-GR-VI.6.4.2-p4, 1985-GR-VI.6.4.2-p5, 1985-GR-VI.6.4.4-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.4.2 p2; §1985:VI.6.4.2 p3; §1985:VI.6.4.2 p4;
      *> §1985:VI.6.4.2 p5; §1985:VI.6.4.4 p1 (COMMON OPTIONS AND RULES
      *> FOR STATEMENTS). An output test: the program must translate and
      *> run at 85 and DISPLAY exactly the lines its .out holds, the
      *> outcome the rule requires. Placed from written and run by the
      *> PB5453 reader of lineage-reads-1985/s-11.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * s-11 probe a2-size-error-control (PB5453), fixed, --std 85.
      * VI.6.4.2 p2: ON SIZE ERROR -> affected resultants unchanged,
      *   unaffected stored, handler runs after the operations, an
      *   explicit transfer (GO TO) in it is obeyed, otherwise control
      *   goes to the end and NOT ON SIZE ERROR is ignored.
      * VI.6.4.2 p3: no ON SIZE ERROR -> unaffected stored, control to
      *   the end, NOT ON SIZE ERROR ignored (affected value undefined,
      *   so never shown).
      * VI.6.4.2 p4: no size error -> ON SIZE ERROR ignored; NOT ON
      *   SIZE ERROR runs (a GO TO in it is obeyed), else control to
      *   the end of the statement.
      * VI.6.4.2 p5: ADD/SUBTRACT CORRESPONDING -> the handler runs
      *   only after every individual operation.
      *   (L7: 10 - (-95) = 105 exceeds X OF G2; Y gets 10 - 5.)
      * VI.6.4.4 p1: ADD, COMPUTE, DIVIDE, MULTIPLY and SUBTRACT are
      *   the arithmetic statements: each takes ON SIZE ERROR.
      * Expected:
      *   L1 SE A=98 B=15
      *   L2 GOTO-SE
      *   L3 END B=20
      *   L4 NOSE A=99
      *   L5 GOTO-NOSE
      *   L6 END A=00
      *   L7 SE X=10 Y=05
      *   L8 SE-ADD SE-SUB SE-MUL SE-DIV SE-CMP
       IDENTIFICATION DIVISION.
       PROGRAM-ID. A2SIZE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A       PIC 99 VALUE 98.
       01  B       PIC 99 VALUE 10.
       01  M       PIC X(40) VALUE SPACES.
       01  G1.
           05  X   PIC S99 VALUE -95.
           05  Y   PIC 99 VALUE 05.
       01  G2.
           05  X   PIC 99 VALUE 10.
           05  Y   PIC 99 VALUE 10.
       PROCEDURE DIVISION.
       L1-PARA.
           ADD 5 TO A B
               ON SIZE ERROR DISPLAY "L1 SE A=" A " B=" B
               NOT ON SIZE ERROR DISPLAY "L1 WRONG NOSE"
           END-ADD.
       L2-PARA.
           ADD 5 TO A
               ON SIZE ERROR GO TO L2-TARGET
           END-ADD.
           DISPLAY "L2 WRONG FELL THROUGH".
       L2-TARGET.
           DISPLAY "L2 GOTO-SE".
       L3-PARA.
           ADD 5 TO A B
               NOT ON SIZE ERROR DISPLAY "L3 WRONG NOSE"
           END-ADD.
           DISPLAY "L3 END B=" B.
       L4-PARA.
           MOVE 98 TO A.
           ADD 1 TO A
               ON SIZE ERROR DISPLAY "L4 WRONG SE"
               NOT ON SIZE ERROR DISPLAY "L4 NOSE A=" A
           END-ADD.
       L5-PARA.
           MOVE 0 TO A.
           ADD 1 TO A
               ON SIZE ERROR DISPLAY "L5 WRONG SE"
               NOT ON SIZE ERROR GO TO L5-TARGET
           END-ADD.
           DISPLAY "L5 WRONG FELL THROUGH".
       L5-TARGET.
           DISPLAY "L5 GOTO-NOSE".
       L6-PARA.
           MOVE 1 TO A.
           SUBTRACT 1 FROM A
               ON SIZE ERROR DISPLAY "L6 WRONG SE"
           END-SUBTRACT.
           DISPLAY "L6 END A=" A.
       L7-PARA.
           SUBTRACT CORRESPONDING G1 FROM G2
               ON SIZE ERROR DISPLAY "L7 SE X=" X OF G2
                   " Y=" Y OF G2
               NOT ON SIZE ERROR DISPLAY "L7 WRONG NOSE"
           END-SUBTRACT.
       L8-PARA.
           MOVE 99 TO A.
           ADD 1 TO A ON SIZE ERROR
               STRING M DELIMITED BY "  " " SE-ADD" DELIMITED BY SIZE
                   INTO M END-STRING
           END-ADD.
           MOVE 0 TO A.
           SUBTRACT 100 FROM A ON SIZE ERROR
               STRING M DELIMITED BY "  " " SE-SUB" DELIMITED BY SIZE
                   INTO M END-STRING
           END-SUBTRACT.
           MOVE 50 TO A.
           MULTIPLY 2 BY A ON SIZE ERROR
               STRING M DELIMITED BY "  " " SE-MUL" DELIMITED BY SIZE
                   INTO M END-STRING
           END-MULTIPLY.
           MOVE 0 TO B.
           DIVIDE B INTO A ON SIZE ERROR
               STRING M DELIMITED BY "  " " SE-DIV" DELIMITED BY SIZE
                   INTO M END-STRING
           END-DIVIDE.
           COMPUTE A = 99 + 1 ON SIZE ERROR
               STRING M DELIMITED BY "  " " SE-CMP" DELIMITED BY SIZE
                   INTO M END-STRING
           END-COMPUTE.
           DISPLAY "L8" M.
           STOP RUN.
