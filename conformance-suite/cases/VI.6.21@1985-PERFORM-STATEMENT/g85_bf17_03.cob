      *>--- conformance-case
      *> id: g85_bf17_03
      *> rules: [1985-GR-VI.6.21.4-9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.21.4 GR9 - a set of statements reached other
      *> than by PERFORM passes through its last statement to the
      *> next executable statement.
      * P-A is the set of PERFORM P-A, and P-A THRU P-B the set of
      * a second PERFORM; both PERFORMs have completed when GO TO
      * enters P-A. GR9: control then passes through the end of
      * P-A into P-B, and through the end of P-B into P-C, as if no
      * PERFORM referenced either set. A return point left armed
      * at the end of P-A or P-B would print M1 or M2 again (or
      * lose C) instead of the lines A3, B, C.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1703.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           PERFORM P-A.
           DISPLAY "M1".
           PERFORM P-A THRU P-B.
           DISPLAY "M2".
           GO TO P-A.
       P-A.
           ADD 1 TO N.
           DISPLAY "A" N.
       P-B.
           DISPLAY "B".
       P-C.
           DISPLAY "C".
           STOP RUN.
