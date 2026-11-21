      *>--- conformance-case
      *> id: pb6007-merge-output-reach-out-and-back-85
      *> rules: [1985-SR-XVI.3.3.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6007 - the MERGE twin of
      *> pb6007-perform-reach-out-and-back-85. OUTPUT PROCEDURE O30
      *> THRU O70 from segment 10 names O30 (segment 30) and O70
      *> (independent segment 70); O30-1 goes to O20-1, outside the
      *> named procedures, which goes back to O30-X, and O30-X falls
      *> into O70. The procedures execution reaches lie neither
      *> totally within non-independent segments nor wholly in one
      *> independent segment (§1985:XVI.3.3.3). Refused COBOL6877.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6007MR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IN1 ASSIGN TO "P6007MI1".
           SELECT IN2 ASSIGN TO "P6007MI2".
           SELECT SD-FILE ASSIGN TO "P6007MW".
       DATA DIVISION.
       FILE SECTION.
       FD  IN1.
       01  IN1-REC PIC X(4).
       FD  IN2.
       01  IN2-REC PIC X(4).
       SD  SD-FILE.
       01  SD-REC.
           05 SD-KEY PIC X(4).
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       M-1.
           GO TO MRG-1.
       MRG-SEC SECTION 10.
       MRG-1.
           MERGE SD-FILE ON ASCENDING KEY SD-KEY
               USING IN1 IN2
               OUTPUT PROCEDURE IS O30 THRU O70.
           STOP RUN.
       O20 SECTION 20.
       O20-1.
           GO TO O30-X.
       O30 SECTION 30.
       O30-1.
           GO TO O20-1.
       O30-X.
           DISPLAY "O30-X".
       O70 SECTION 70.
       OUT-1.
           RETURN SD-FILE AT END GO TO OUT-X.
           GO TO OUT-1.
       OUT-X.
           EXIT.
