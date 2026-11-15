      *>--- conformance-case
      *> id: pb5572-return-in-declarative-no-output-procedure-85
      *> rules: [1985-SR-XI.4.3.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5572. §1985:XI.4.3.3 SR3: "A RETURN statement may only be used within the
      *> range of an output procedure associated with a SORT or MERGE statement for
      *> file-name-1." §1985:XI.4.4.4 GR10 puts in the range "all statements in declarative
      *> procedures that are executed as a result of" the range's statements, but the one SORT
      *> of SORT-A has an input procedure and GIVING, no output procedure, so the range is empty
      *> and the RETURN in the declaratives is outside every range. Expected: COBOL7081 at 85 at
      *> the RETURN SORT-A. Fails if a declarative statement is not asked, or an input procedure
      *> is taken for an output one.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5572TD.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SORT-A ASSIGN TO P5572SB.
           SELECT IN-F ASSIGN TO P5572MT.
           SELECT OUT-F ASSIGN TO P5572OT.
       DATA DIVISION.
       FILE SECTION.
       SD  SORT-A.
       01  REC-A.
           05  KEY-A PIC X(3).
       FD  IN-F.
       01  IN-R PIC X(3).
       FD  OUT-F.
       01  OUT-R PIC X(3).
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON IN-F.
       D2.
           RETURN SORT-A AT END DISPLAY "AT END".
       END DECLARATIVES.
       M SECTION.
       M1.
           SORT SORT-A ON ASCENDING KEY KEY-A
               INPUT PROCEDURE IS IN-PROC
               GIVING OUT-F.
           STOP RUN.
       IN-PROC SECTION.
       I1.
           OPEN INPUT IN-F.
           MOVE "AAA" TO REC-A.
           RELEASE REC-A.
