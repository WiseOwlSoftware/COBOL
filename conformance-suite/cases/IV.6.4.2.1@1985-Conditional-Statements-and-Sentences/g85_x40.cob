      *>--- conformance-case
      *> id: g85_x40
      *> rules: [1985-GR-IV.6.4.2.1.1-5, 1985-GR-IV.6.4.2.1.1.1-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.4.2.1.1 GR5 (1985-GR-IV.6.4.2.1.1-5) and
      *> §1985:IV.6.4.2.1.1.1 GR4 (1985-GR-IV.6.4.2.1.1.1-4). An output
      *> test, the accepted half: ADD, COMPUTE, DIVIDE, MULTIPLY and
      *> SUBTRACT, each with ON SIZE ERROR and with NOT ON SIZE ERROR
      *> and delimited by END-<verb>, stand as imperative-statement-1
      *> of an inline PERFORM. Derived from §1985:VI.6.4.2 (N PIC 9):
      *> 9+1, 2*5, a divisor of zero, 1*10 and 3-20 (absolute value 17)
      *> are size errors that leave N unchanged; 1+1=2, 2+1=3, 3/3=1,
      *> 1*4=4, 4-1=3 are not.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85X40.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N PIC 9 VALUE 9.
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       P0.
           PERFORM
               ADD 1 TO N ON SIZE ERROR DISPLAY "ADD SIZE ERROR " N
               END-ADD
               MOVE 1 TO N
               ADD 1 TO N
                   NOT ON SIZE ERROR
                       DISPLAY "ADD NOT SIZE ERROR " N
               END-ADD
               COMPUTE N = N * 5
                   ON SIZE ERROR
                       DISPLAY "COMPUTE SIZE ERROR " N
               END-COMPUTE
               COMPUTE N = N + 1
                   NOT ON SIZE ERROR
                       DISPLAY "COMPUTE NOT SIZE ERROR " N
               END-COMPUTE
               DIVIDE 0 INTO N
                   ON SIZE ERROR
                       DISPLAY "DIVIDE SIZE ERROR " N
               END-DIVIDE
               DIVIDE 3 INTO N
                   NOT ON SIZE ERROR
                       DISPLAY "DIVIDE NOT SIZE ERROR " N
               END-DIVIDE
               MULTIPLY 10 BY N
                   ON SIZE ERROR
                       DISPLAY "MULTIPLY SIZE ERROR " N
               END-MULTIPLY
               MULTIPLY 4 BY N
                   NOT ON SIZE ERROR
                       DISPLAY "MULTIPLY NOT SIZE ERROR " N
               END-MULTIPLY
               SUBTRACT 1 FROM N
                   NOT ON SIZE ERROR
                       DISPLAY "SUBTRACT NOT SIZE ERROR " N
               END-SUBTRACT
               SUBTRACT 20 FROM N
                   ON SIZE ERROR
                       DISPLAY "SUBTRACT SIZE ERROR " N
               END-SUBTRACT
           END-PERFORM.
           DISPLAY "END".
           STOP RUN.
