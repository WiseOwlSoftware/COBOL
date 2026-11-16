      *>--- conformance-case
      *> id: g85_f38_07
      *> rules: [1989-AR-A.2.35.3-2, 1989-RV-A.2.35.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.35.3 AR2 - a later reference with argument-1
      *> starts a new sequence; §1989:A.2.35.4 RV2 - one seed always
      *> gives the same sequence. Seed 5 starts sequence A (3 values,
      *> the 2nd and 3rd from references without argument-1,
      *> §1989:A.2.35.3 AR4); seed 17 then starts B (2 values); seed
      *> 5 again starts C and seed 17 again starts D. By AR2 each
      *> later seeded reference starts a fresh sequence rather than
      *> continuing the current one, and by RV2 that sequence is the
      *> one its seed gave before, so C = A and D = B (lines R1 and
      *> R2 pin both rules). No random value is printed: the
      *> sequence itself is the implementor's. A compiler that
      *> ignores a later seed, or seeds from a clock, prints
      *> DIFFERENT.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3807.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-SEQS.
           02  W-A     PIC 9V9(17) OCCURS 3 TIMES.
           02  W-B     PIC 9V9(17) OCCURS 2 TIMES.
           02  W-C     PIC 9V9(17) OCCURS 3 TIMES.
           02  W-D     PIC 9V9(17) OCCURS 2 TIMES.
       01  W-SEED      PIC 99 VALUE 17.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE W-A (1) = FUNCTION RANDOM (5).
           COMPUTE W-A (2) = FUNCTION RANDOM.
           COMPUTE W-A (3) = FUNCTION RANDOM.
           COMPUTE W-B (1) = FUNCTION RANDOM (W-SEED).
           COMPUTE W-B (2) = FUNCTION RANDOM.
           COMPUTE W-C (1) = FUNCTION RANDOM (5).
           COMPUTE W-C (2) = FUNCTION RANDOM.
           COMPUTE W-C (3) = FUNCTION RANDOM.
           COMPUTE W-D (1) = FUNCTION RANDOM (W-SEED).
           COMPUTE W-D (2) = FUNCTION RANDOM.
           IF W-C (1) = W-A (1) AND W-C (2) = W-A (2)
               AND W-C (3) = W-A (3)
               DISPLAY "R1 SEED 5 RESTARTED SAME"
           ELSE
               DISPLAY "R1 SEED 5 RESTARTED DIFFERENT".
           IF W-D (1) = W-B (1) AND W-D (2) = W-B (2)
               DISPLAY "R2 SEED 17 RESTARTED SAME"
           ELSE
               DISPLAY "R2 SEED 17 RESTARTED DIFFERENT".
           STOP RUN.
