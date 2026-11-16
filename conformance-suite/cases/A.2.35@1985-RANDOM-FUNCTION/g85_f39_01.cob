      *>--- conformance-case
      *> id: g85_f39_01
      *> rules: [1989-RV-A.2.35.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.35.4 RV3 - the seeds 0 through 32767 are inside the
      *> subset of argument-1 values "that will yield distinct
      *> sequences of pseudo-random numbers". Four seeds of that floor
      *> (0, 1, 16384, 32767) each start a sequence (§1989:A.2.35.3
      *> AR1); its next two numbers come from references without
      *> argument-1 (§1989:A.2.35.3 AR4). Every pair of the four
      *> sequences must differ, so all six pairs print DISTINCT. No
      *> pseudo-random value is printed: RV3 leaves the sequences
      *> themselves to the implementor. A leg fails when two seeds of
      *> the floor alias one sequence (a seed ignored, or masked to
      *> fewer than 15 bits).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3901.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  SEED-TABLE.
           02  SEED-VAL    PIC 9(5) OCCURS 4 TIMES.
       01  SEQ-TABLE.
           02  SEQ         OCCURS 4 TIMES.
               03  SEQ-V   PIC 9V9(15) OCCURS 3 TIMES.
       01  I               PIC 9.
       01  J               PIC 9.
       01  K               PIC 9.
       01  DIFF-FLAG       PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 0 TO SEED-VAL (1).
           MOVE 1 TO SEED-VAL (2).
           MOVE 16384 TO SEED-VAL (3).
           MOVE 32767 TO SEED-VAL (4).
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 4
               COMPUTE SEQ-V (I 1) = FUNCTION RANDOM (SEED-VAL (I))
               COMPUTE SEQ-V (I 2) = FUNCTION RANDOM
               COMPUTE SEQ-V (I 3) = FUNCTION RANDOM
           END-PERFORM.
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 3
               COMPUTE J = I + 1
               PERFORM COMPARE-PAIR UNTIL J > 4
           END-PERFORM.
           STOP RUN.
       COMPARE-PAIR.
           MOVE "N" TO DIFF-FLAG.
           PERFORM VARYING K FROM 1 BY 1 UNTIL K > 3
               IF SEQ-V (I K) NOT = SEQ-V (J K)
                   MOVE "Y" TO DIFF-FLAG
               END-IF
           END-PERFORM.
           IF DIFF-FLAG = "Y"
               DISPLAY "SEEDS " SEED-VAL (I) " " SEED-VAL (J)
                   " DISTINCT"
           ELSE
               DISPLAY "SEEDS " SEED-VAL (I) " " SEED-VAL (J)
                   " SAME"
           END-IF.
           ADD 1 TO J.
