      *>--- conformance-case
      *> id: pb5104-return-of-file-no-sort-names-85
      *> rules: [1985-SR-XI.4.3.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5104. §1985:XI.4.3.3 SR3: "A RETURN statement may only be used within the
      *> range of an output procedure associated with a SORT or MERGE statement for file-name-1."
      *> OUT-PROC returns from SORT-B, a sort file no SORT or MERGE statement names. Expected:
      *> COBOL7081 at 85 at the RETURN SORT-B. Fails if the file of the RETURN is not asked.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5104RT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SORT-A ASSIGN TO P5104WA.
           SELECT SORT-B ASSIGN TO P5104WB.
       DATA DIVISION.
       FILE SECTION.
       SD  SORT-A.
       01  REC-A.
           05  KEY-A PIC X(3).
       SD  SORT-B.
       01  REC-B.
           05  KEY-B PIC X(3).
       PROCEDURE DIVISION.
       MAIN-CTL SECTION.
       M1.
           SORT SORT-A ON ASCENDING KEY KEY-A
               INPUT PROCEDURE IS IN-PROC
               OUTPUT PROCEDURE IS OUT-PROC.
           STOP RUN.
       IN-PROC SECTION.
       I1.
           MOVE "CCC" TO REC-A.
           RELEASE REC-A.
           MOVE "AAA" TO REC-A.
           RELEASE REC-A.
           MOVE "BBB" TO REC-A.
           RELEASE REC-A.
       OUT-PROC SECTION.
       O1.
           RETURN SORT-B AT END GO TO O9.
           DISPLAY KEY-A.
           GO TO O1.
       O9.
           EXIT.
