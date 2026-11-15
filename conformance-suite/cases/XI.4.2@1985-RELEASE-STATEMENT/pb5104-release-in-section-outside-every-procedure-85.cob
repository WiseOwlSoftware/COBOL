      *>--- conformance-case
      *> id: pb5104-release-in-section-outside-every-procedure-85
      *> rules: [1985-SR-XI.4.2.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5104. §1985:XI.4.2.3 SR2: "A RELEASE statement may be used only within the
      *> range of an input procedure associated with a SORT statement for the file-name whose
      *> sort-merge file description entry contains record-name-1." SORT-A is named by a SORT
      *> (USING / OUTPUT PROCEDURE, no input procedure), and STRAY, which no procedure's range
      *> reaches, releases its record. Expected: COBOL7081 at 85 at that RELEASE. Fails if a
      *> RELEASE is accepted wherever its file is named by some SORT.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5104ST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SORT-A ASSIGN TO P5104SA.
           SELECT IN-F ASSIGN TO P5104SI.
       DATA DIVISION.
       FILE SECTION.
       SD  SORT-A.
       01  REC-A.
           05  KEY-A PIC X(3).
       FD  IN-F.
       01  IN-R PIC X(3).
       PROCEDURE DIVISION.
       MAIN-CTL SECTION.
       M1.
           SORT SORT-A ON ASCENDING KEY KEY-A
               USING IN-F
               OUTPUT PROCEDURE IS OUT-PROC.
           STOP RUN.
       STRAY SECTION.
       S1.
           RELEASE REC-A.
       OUT-PROC SECTION.
       O1.
           RETURN SORT-A AT END GO TO O9.
           DISPLAY KEY-A.
           GO TO O1.
       O9.
           EXIT.
