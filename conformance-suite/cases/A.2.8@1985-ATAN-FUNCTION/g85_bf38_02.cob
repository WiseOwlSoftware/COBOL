      *>--- conformance-case
      *> id: g85_bf38_02
      *> rules: [1989-FMT-A.2.8.2, 1989-RV-A.2.8.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.8.2 - FUNCTION ATAN ( argument-1 ) is accepted with an
      *> identifier, a literal and an arithmetic expression as argument-1;
      *> a compiler refusing the form fails.
      *> §1989:A.2.8.4 RV1 - the value approximates the arctangent and
      *> lies strictly between -pi/2 and +pi/2: ATAN(+/-1) = +/-pi/4,
      *> ATAN(0) = 0, and for +/-99999999 (arctangent pi/2 - 1E-8) the value
      *> compares below 1.570796326794896 (a value under pi/2) and above
      *> its negative. A wrong value or a value at pi/2 fails a line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3802.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A-ONE       PIC S9      VALUE +1.
       01  BIG         PIC S9(8)   VALUE +99999999.
       01  R           PIC S9V9(6).
       01  E           PIC -9.9(6).
       PROCEDURE DIVISION.
       P-MAIN.
           COMPUTE R ROUNDED = FUNCTION ATAN (A-ONE).
           MOVE R TO E.
           DISPLAY "ATAN(+1)   " E.
           COMPUTE R ROUNDED = FUNCTION ATAN (0 - A-ONE).
           MOVE R TO E.
           DISPLAY "ATAN(-1)   " E.
           COMPUTE R ROUNDED = FUNCTION ATAN (0).
           MOVE R TO E.
           DISPLAY "ATAN(0)    " E.
           COMPUTE R ROUNDED = FUNCTION ATAN (BIG).
           MOVE R TO E.
           DISPLAY "ATAN(BIG)  " E.
           IF FUNCTION ATAN (BIG) < 1.570796326794896
               DISPLAY "ATAN(BIG) BELOW +PI/2"
           ELSE
               DISPLAY "ATAN(BIG) NOT BELOW +PI/2".
           IF FUNCTION ATAN (0 - BIG) > -1.570796326794896
               DISPLAY "ATAN(-BIG) ABOVE -PI/2"
           ELSE
               DISPLAY "ATAN(-BIG) NOT ABOVE -PI/2".
           STOP RUN.
