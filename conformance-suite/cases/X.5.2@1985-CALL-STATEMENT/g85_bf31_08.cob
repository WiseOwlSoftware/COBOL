      *>--- conformance-case
      *> id: g85_bf31_08
      *> rules: [1985-GR-X.5.2.4-11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.2.4 GR11 - "The values of the parameters referenced
      *> in the USING phrase of the CALL statement are made available
      *> to the called program at the time the CALL statement is
      *> executed." Before each of three CALLs the caller changes
      *> BF31-R (by reference, GR12 default) and BF31-C (BY CONTENT):
      *> the called program must see the values current at that CALL:
      *> 001/ONE1, 002/TWO2, 037/LAST. A callee that saw the first
      *> call's values, a stale BY CONTENT copy, or initial values
      *> fails a line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3108.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  BF31-R PIC 9(3).
       01  BF31-C PIC X(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 1 TO BF31-R.
           MOVE "ONE1" TO BF31-C.
           CALL "G85BF3108S" USING BF31-R BY CONTENT BF31-C.
           MOVE 2 TO BF31-R.
           MOVE "TWO2" TO BF31-C.
           CALL "G85BF3108S" USING BY REFERENCE BF31-R
                                    BY CONTENT BF31-C.
           MOVE 37 TO BF31-R.
           MOVE "LAST" TO BF31-C.
           CALL "G85BF3108S" USING BF31-R BY CONTENT BF31-C.
           DISPLAY "MAIN R=" BF31-R " C=" BF31-C.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3108S.
       DATA DIVISION.
       LINKAGE SECTION.
       01  L-R PIC 9(3).
       01  L-C PIC X(4).
       PROCEDURE DIVISION USING L-R L-C.
       S-PARA.
           DISPLAY "SUB R=" L-R " C=" L-C.
           EXIT PROGRAM.
       END PROGRAM G85BF3108S.
       END PROGRAM G85BF3108.
