      *>--- conformance-case
      *> id: g85_y91
      *> rules: [1985-SR-VI.6.9.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.9.3 SR1 (cite.py --check OK): "The CONTINUE statement
      *> may be used anywhere a conditional statement or an
      *> imperative-statement may be used." One leg per position that
      *> takes an imperative-statement in the 1985 Nucleus and Table
      *> Handling modules; each must translate, and CONTINUE does
      *> nothing (§1985:VI.6.9.4 GR1), so control reaches the next
      *> DISPLAY with no data changed.
      *> DERIVATION:
      *>   L1  IF true, CONTINUE in the THEN branch          -> L1
      *>   L2  IF false, CONTINUE in the ELSE branch         -> L2
      *>   L3  ADD 1 TO S (PIC 9 = 9): size error, S kept,
      *>       CONTINUE in ON SIZE ERROR                     -> L3 9
      *>   L4  EVALUATE WHEN 1 CONTINUE                      -> L4
      *>   L5  inline PERFORM 2 TIMES, CONTINUE after ADD    -> L5 2
      *>   L6  SEARCH finds no "Z": CONTINUE in AT END       -> L6
      *>   L7  SEARCH finds "B" at 2: CONTINUE in WHEN       -> L7 2
      *>   L8  STRING overflows O: CONTINUE in ON OVERFLOW   -> L8 AB
      *>   L9  CONTINUE as a sentence of its own             -> L9
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85Y91.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N   PIC 9 VALUE 1.
       01  S   PIC 9 VALUE 9.
       01  K   PIC 9 VALUE 0.
       01  T.
           02  E PIC X OCCURS 3 TIMES INDEXED BY IX.
       01  O   PIC X(2).
       PROCEDURE DIVISION.
       P1.
           IF N = 1 CONTINUE ELSE DISPLAY "L1-ELSE".
           DISPLAY "L1".
           IF N = 2 DISPLAY "L2-THEN" ELSE CONTINUE.
           DISPLAY "L2".
           ADD 1 TO S ON SIZE ERROR CONTINUE
               NOT ON SIZE ERROR DISPLAY "L3-NOT" END-ADD.
           DISPLAY "L3 " S.
           EVALUATE N WHEN 1 CONTINUE
               WHEN OTHER DISPLAY "L4-OTHER" END-EVALUATE.
           DISPLAY "L4".
           PERFORM 2 TIMES ADD 1 TO K CONTINUE END-PERFORM.
           DISPLAY "L5 " K.
           MOVE "ABC" TO T.
           SET IX TO 1.
           SEARCH E AT END CONTINUE
               WHEN E (IX) = "Z" DISPLAY "L6-Z".
           DISPLAY "L6".
           SET IX TO 1.
           SEARCH E AT END DISPLAY "L7-END"
               WHEN E (IX) = "B" CONTINUE.
           SET K TO IX.
           DISPLAY "L7 " K.
           MOVE SPACES TO O.
           STRING "AB" "CD" DELIMITED BY SIZE INTO O
               ON OVERFLOW CONTINUE END-STRING.
           DISPLAY "L8 " O.
           CONTINUE.
           DISPLAY "L9".
           STOP RUN.
