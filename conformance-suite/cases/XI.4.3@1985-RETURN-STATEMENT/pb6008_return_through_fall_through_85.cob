      *>--- conformance-case
      *> id: pb6008_return_through_fall_through_85
      *> rules: [1985-SR-XI.4.3.3-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6008 - the accepting control of
      *> pb6008-return-in-unreached-section-after-85. The range of an
      *> output procedure "includes all statements that are executed
      *> as the result of a transfer of control by CALL, EXIT, GO TO,
      *> and PERFORM statements in the range of the output procedure"
      *> (§1985:XI.4.4.4 GR10). O1 goes to H1, written after OUT-PROC;
      *> H1 falls through to H2, whose RETURN is therefore in the
      *> range, and H2 leaves by GO TO O9, the procedure's last
      *> paragraph. Expected output: H1, AAA, BBB.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6008FT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SORT-A ASSIGN TO P6008FT.
       DATA DIVISION.
       FILE SECTION.
       SD  SORT-A.
       01  REC-A.
           05  KEY-A PIC X(3).
       PROCEDURE DIVISION.
       MAIN-CTL SECTION.
       M1.
           SORT SORT-A ON ASCENDING KEY KEY-A
               INPUT PROCEDURE IS IN-PROC
               OUTPUT PROCEDURE IS OUT-PROC.
           STOP RUN.
       IN-PROC SECTION.
       I1.
           MOVE "BBB" TO REC-A.
           RELEASE REC-A.
           MOVE "AAA" TO REC-A.
           RELEASE REC-A.
       OUT-PROC SECTION.
       O1.
           GO TO H1.
       O9.
           EXIT.
       HELPERS SECTION.
       H1.
           DISPLAY "H1".
       H2.
           RETURN SORT-A AT END GO TO O9.
           DISPLAY KEY-A.
           GO TO H2.
