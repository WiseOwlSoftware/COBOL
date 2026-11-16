      *>--- conformance-case
      *> id: g85_bf14_02
      *> rules: [1985-GR-VI.6.4.2-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.4.2 GR2 - division by zero terminates the arithmetic
      *> operation and causes a size error condition, in every DIVIDE
      *> format and in a COMPUTE expression; under ON SIZE ERROR the
      *> affected resultants keep their values. L1/L3/L5/L7 fail if no size
      *> error is raised; L2/L4/L6/L8 if any resultant is changed; L9/L10
      *> are the control (a non-zero divisor raises none and stores 06).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1402.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 99 VALUE 12.
       01 B PIC 99 VALUE 30.
       01 Z PIC 9 VALUE 0.
       01 C PIC 99 VALUE 77.
       01 D PIC 99 VALUE 88.
       01 Q PIC 99 VALUE 11.
       01 R PIC 99 VALUE 22.
       PROCEDURE DIVISION.
       P1.
           DIVIDE Z INTO A B
               ON SIZE ERROR DISPLAY "L1 SIZE ERROR"
               NOT ON SIZE ERROR DISPLAY "L1 NO SIZE ERROR"
           END-DIVIDE.
           DISPLAY "L2 " A " " B.
           DIVIDE 60 BY Z GIVING C D
               ON SIZE ERROR DISPLAY "L3 SIZE ERROR"
               NOT ON SIZE ERROR DISPLAY "L3 NO SIZE ERROR"
           END-DIVIDE.
           DISPLAY "L4 " C " " D.
           DIVIDE Z INTO 60 GIVING Q REMAINDER R
               ON SIZE ERROR DISPLAY "L5 SIZE ERROR"
               NOT ON SIZE ERROR DISPLAY "L5 NO SIZE ERROR"
           END-DIVIDE.
           DISPLAY "L6 " Q " " R.
           COMPUTE C = A + 60 / Z
               ON SIZE ERROR DISPLAY "L7 SIZE ERROR"
               NOT ON SIZE ERROR DISPLAY "L7 NO SIZE ERROR"
           END-COMPUTE.
           DISPLAY "L8 " C.
           DIVIDE 2 INTO A
               ON SIZE ERROR DISPLAY "L9 SIZE ERROR"
               NOT ON SIZE ERROR DISPLAY "L9 NO SIZE ERROR"
           END-DIVIDE.
           DISPLAY "L10 " A.
           STOP RUN.
