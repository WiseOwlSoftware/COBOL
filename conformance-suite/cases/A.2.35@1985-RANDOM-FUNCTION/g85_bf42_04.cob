      *>--- conformance-case
      *> id: g85_bf42_04
      *> rules: [1989-AR-A.2.35.3-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.35.3 AR4 - subsequent references without argument-1
      *> return the next number in the CURRENT sequence. With AR2 (a
      *> reference with argument-1 starts a new sequence) and §1989:A.2.35.4
      *> RV2 (a given seed always yields the same sequence):
      *>   seed 7, then 2 references       -> X1 X2
      *>   seed 8, then 1 reference        -> Y1
      *>   seed 7 again, then 2 references -> Z1 Z2: the k-th next number of
      *>     seed 7's sequence again, so Z1 = X1 and Z2 = X2
      *>   seed 8 again, then 1 reference  -> W1: the next number of the
      *>     CURRENT (seed 8) sequence, not seed 7's, so W1 = Y1
      *> Only equalities are pinned; the numbers themselves are the
      *> implementor's. A leg fails if an unseeded reference does not
      *> continue the sequence the last seeded reference started.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF4204.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-S                    PIC 9V9(9).
       01  WS-X1                   PIC 9V9(9).
       01  WS-X2                   PIC 9V9(9).
       01  WS-Y1                   PIC 9V9(9).
       01  WS-Z1                   PIC 9V9(9).
       01  WS-Z2                   PIC 9V9(9).
       01  WS-W1                   PIC 9V9(9).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE WS-S = FUNCTION RANDOM (7).
           COMPUTE WS-X1 = FUNCTION RANDOM.
           COMPUTE WS-X2 = FUNCTION RANDOM.
           COMPUTE WS-S = FUNCTION RANDOM (8).
           COMPUTE WS-Y1 = FUNCTION RANDOM.
           COMPUTE WS-S = FUNCTION RANDOM (7).
           COMPUTE WS-Z1 = FUNCTION RANDOM.
           COMPUTE WS-Z2 = FUNCTION RANDOM.
           COMPUTE WS-S = FUNCTION RANDOM (8).
           COMPUTE WS-W1 = FUNCTION RANDOM.
           IF WS-Z1 = WS-X1
               DISPLAY "SEED 7 NEXT 1 SAME"
           ELSE
               DISPLAY "SEED 7 NEXT 1 DIFFERS".
           IF WS-Z2 = WS-X2
               DISPLAY "SEED 7 NEXT 2 SAME"
           ELSE
               DISPLAY "SEED 7 NEXT 2 DIFFERS".
           IF WS-W1 = WS-Y1
               DISPLAY "SEED 8 NEXT 1 SAME"
           ELSE
               DISPLAY "SEED 8 NEXT 1 DIFFERS".
           STOP RUN.
