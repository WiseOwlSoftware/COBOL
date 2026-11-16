      *>--- conformance-case
      *> id: g85_bf38_03
      *> rules: [1989-AR-A.2.35.3-1, 1989-RV-A.2.35.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.35.3 AR1 - argument-1 zero (literal) and a positive
      *> integer (literal 17, identifier 32767) are accepted and used as
      *> the seed: restarting with seed 17 gives the same three values
      *> (§1989:A.2.35.3 AR2, AR4, §1989:A.2.35.4 RV2), line 1; a compiler
      *> refusing a seed or ignoring it fails that line.
      *> §1989:A.2.35.4 RV1 - every value returned (seeded and 100 unseeded
      *> follow-on references) is >= 0 and < 1, line 2 counts violations;
      *> R keeps an integer digit so a value of 1 or more is seen.
      *> No value is printed: the sequence is the implementor's (RV3).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3803.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  SEED        PIC 9(5)     VALUE 32767.
       01  R           PIC S9V9(9).
       01  R1          PIC S9V9(9).
       01  R2          PIC S9V9(9).
       01  R3          PIC S9V9(9).
       01  S1          PIC S9V9(9).
       01  S2          PIC S9V9(9).
       01  S3          PIC S9V9(9).
       01  BAD         PIC 9(3)     VALUE ZERO.
       PROCEDURE DIVISION.
       P-MAIN.
           COMPUTE R = FUNCTION RANDOM (0).
           PERFORM P-CHECK.
           COMPUTE R = FUNCTION RANDOM (SEED).
           PERFORM P-CHECK.
           COMPUTE R1 = FUNCTION RANDOM (17).
           COMPUTE R2 = FUNCTION RANDOM.
           COMPUTE R3 = FUNCTION RANDOM.
           COMPUTE S1 = FUNCTION RANDOM (17).
           COMPUTE S2 = FUNCTION RANDOM.
           COMPUTE S3 = FUNCTION RANDOM.
           IF R1 = S1 AND R2 = S2 AND R3 = S3
               DISPLAY "SEED 17 SEQUENCE REPEATS"
           ELSE
               DISPLAY "SEED 17 SEQUENCE DIFFERS".
           MOVE R1 TO R. PERFORM P-CHECK.
           MOVE R2 TO R. PERFORM P-CHECK.
           MOVE R3 TO R. PERFORM P-CHECK.
           PERFORM P-NEXT 100 TIMES.
           DISPLAY "OUT OF RANGE " BAD.
           STOP RUN.
       P-NEXT.
           COMPUTE R = FUNCTION RANDOM.
           PERFORM P-CHECK.
       P-CHECK.
           IF R < 0 OR R NOT < 1
               ADD 1 TO BAD.
