      *>--- conformance-case
      *> id: 1985-GR-VI_6_3-p1
      *> rules: [1985-GR-VI.6.3-p1, 1985-GR-VI.6.3.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.3 p1; §1985:VI.6.3.1 p1 (PROCEDURE DIVISION IN THE
      *> NUCLEUS MODULE). An output test: the program must translate and
      *> run at 85 and DISPLAY exactly the lines its .out holds, the
      *> outcome the rule requires. Placed from written and run by the
      *> PB5453 reader of lineage-reads-1985/s-10.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S10CTX.
      * 1985 VI.6.3 p1 / VI.6.3.1 p1: conditions in EVALUATE, IF,
      * PERFORM and SEARCH; simple and complex conditions in any
      * number of paired parentheses keep their truth value.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SWITCH-1 IS SW1 ON STATUS IS SW1-ON OFF STATUS IS SW1-OFF
           CLASS VOWEL IS "A" "E" "I" "O" "U".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N        PIC S9(3) VALUE -5.
       01  A        PIC X(3)  VALUE "AEI".
       01  C        PIC 9     VALUE 2.
           88  C-TWO VALUE 2.
       01  K        PIC 9     VALUE 0.
       01  TB.
           05  TE   PIC 9 OCCURS 3 TIMES INDEXED BY IX.
       PROCEDURE DIVISION.
       P0.
           IF ((N < 0)) DISPLAY "R1 T" ELSE DISPLAY "R1 F".
           IF (((A IS ALPHABETIC))) DISPLAY "R2 T" ELSE DISPLAY "R2 F".
           IF ((C-TWO)) DISPLAY "R3 T" ELSE DISPLAY "R3 F".
           IF ((SW1-OFF)) OR (SW1-ON) DISPLAY "R4 T"
               ELSE DISPLAY "R4 F".
           IF ((N IS NEGATIVE)) DISPLAY "R5 T" ELSE DISPLAY "R5 F".
           IF (A IS VOWEL) DISPLAY "R6 T" ELSE DISPLAY "R6 F".
           IF ((((N < 0) AND (C = 2)))) DISPLAY "R7 T"
               ELSE DISPLAY "R7 F".
           IF ((N > 0) OR (C NOT = 2)) DISPLAY "R8 T"
               ELSE DISPLAY "R8 F".
           EVALUATE TRUE
               WHEN ((N < 0)) DISPLAY "E1 T"
               WHEN OTHER DISPLAY "E1 F"
           END-EVALUATE.
           PERFORM P1 UNTIL ((K = 3)).
           DISPLAY "U1 " K.
           MOVE 123 TO TB.
           SET IX TO 1.
           SEARCH TE AT END DISPLAY "S1 END"
               WHEN ((TE (IX) = 2)) DISPLAY "S1 HIT " TE (IX).
           STOP RUN.
       P1.
           ADD 1 TO K.
