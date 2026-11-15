      *>--- conformance-case
      *> id: pb5572-release-in-declarative-no-input-procedure-85
      *> rules: [1985-SR-XI.4.2.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5572. §1985:XI.4.2.3 SR2: "A RELEASE statement may be used only within the
      *> range of an input procedure associated with a SORT statement for the file-name whose
      *> sort-merge file description entry contains record-name-1." §1985:XI.4.4.4 GR7 puts in
      *> the range "all statements in declarative procedures that are executed as a result of"
      *> the range's statements, but no SORT names SORT-A with an input procedure, so the range
      *> is empty and the RELEASE in the declaratives is outside every range. Expected:
      *> COBOL7081 at 85 at the RELEASE REC-A. Fails if a declarative statement is not asked.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5572RD.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SORT-A ASSIGN TO P5572SA.
           SELECT IN-F ASSIGN TO P5572MS.
       DATA DIVISION.
       FILE SECTION.
       SD  SORT-A.
       01  REC-A.
           05  KEY-A PIC X(3).
       FD  IN-F.
       01  IN-R PIC X(3).
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON IN-F.
       D2.
           MOVE "DDD" TO REC-A.
           RELEASE REC-A.
       END DECLARATIVES.
       M SECTION.
       M1.
           OPEN INPUT IN-F.
           STOP RUN.
