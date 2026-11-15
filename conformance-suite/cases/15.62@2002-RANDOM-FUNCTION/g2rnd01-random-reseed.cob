      *>--- conformance-case
      *> id: g2rnd01-random-reseed
      *> rules: [2002-AR-15.62.2-3, 2002-RV-15.62.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.62 RANDOM, judged on the 2002 text at --std 2002. No
      *> pseudo-random value is displayed (the values are the
      *> implementor's); every line is an equality the 2002 rules fix.
      *> §2002:15.62.2 AR3 - "If a subsequent reference specifies
      *>   argument-1, a new sequence of pseudo-random numbers is started."
      *>   RESEED-7: after RANDOM(7) and two further draws, RANDOM(12345)
      *>   and two draws, a later RANDOM(7) starts 7's sequence again, so
      *>   its three draws (AR5: the bare references return the next
      *>   number of the current sequence) equal the first three. Were
      *>   the seed ignored on the later reference, S1 would be the next
      *>   number of the 12345 sequence and the line would read DIFFERENT.
      *>   RESEED-12345: the same for seed 12345 after the second 7.
      *> §2002:15.62.3 RV2 - "For a given seed value on a given
      *>   implementation, the sequence of pseudo-random numbers will
      *>   always be the same." SEED-EXPR-7: the seed written as the
      *>   expression SEED + 1 (value 7) yields 7's sequence; the
      *>   0-500 loop seeds each value twice and compares two draws:
      *>   MISMATCH=0.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RND01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  SEED            PIC 9(5) VALUE 6.
       01  R1              PIC 9V9(9).
       01  R2              PIC 9V9(9).
       01  R3              PIC 9V9(9).
       01  S1              PIC 9V9(9).
       01  S2              PIC 9V9(9).
       01  S3              PIC 9V9(9).
       01  T1              PIC 9V9(9).
       01  T2              PIC 9V9(9).
       01  T3              PIC 9V9(9).
       01  U1              PIC 9V9(9).
       01  U2              PIC 9V9(9).
       01  U3              PIC 9V9(9).
       01  K               PIC 9(5).
       01  BAD             PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           COMPUTE R1 = FUNCTION RANDOM (7)
           COMPUTE R2 = FUNCTION RANDOM
           COMPUTE R3 = FUNCTION RANDOM
           COMPUTE U1 = FUNCTION RANDOM (12345)
           COMPUTE U2 = FUNCTION RANDOM
           COMPUTE U3 = FUNCTION RANDOM
           COMPUTE S1 = FUNCTION RANDOM (7)
           COMPUTE S2 = FUNCTION RANDOM ( )
           COMPUTE S3 = FUNCTION RANDOM
           IF R1 = S1 AND R2 = S2 AND R3 = S3
              DISPLAY "RESEED-7=SAME"
           ELSE
              DISPLAY "RESEED-7=DIFFERENT"
           END-IF
           COMPUTE T1 = FUNCTION RANDOM (SEED + 1)
           COMPUTE T2 = FUNCTION RANDOM
           COMPUTE T3 = FUNCTION RANDOM
           IF R1 = T1 AND R2 = T2 AND R3 = T3
              DISPLAY "SEED-EXPR-7=SAME"
           ELSE
              DISPLAY "SEED-EXPR-7=DIFFERENT"
           END-IF
           COMPUTE S1 = FUNCTION RANDOM (12345)
           COMPUTE S2 = FUNCTION RANDOM
           COMPUTE S3 = FUNCTION RANDOM
           IF U1 = S1 AND U2 = S2 AND U3 = S3
              DISPLAY "RESEED-12345=SAME"
           ELSE
              DISPLAY "RESEED-12345=DIFFERENT"
           END-IF
           PERFORM VARYING K FROM 0 BY 1 UNTIL K > 500
              COMPUTE R1 = FUNCTION RANDOM (K)
              COMPUTE R2 = FUNCTION RANDOM
              COMPUTE S1 = FUNCTION RANDOM (K)
              COMPUTE S2 = FUNCTION RANDOM
              IF R1 NOT = S1 OR R2 NOT = S2
                 MOVE 1 TO BAD
              END-IF
           END-PERFORM
           DISPLAY "RESEED-0-500-MISMATCH=" BAD
           STOP RUN.
