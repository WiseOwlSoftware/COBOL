      *>--- conformance-case
      *> id: pb5104-release-of-file-no-sort-names-85
      *> rules: [1985-SR-XI.4.2.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5104. §1985:XI.4.2.3 SR2: "A RELEASE statement may be used only within the
      *> range of an input procedure associated with a SORT statement for the file-name whose
      *> sort-merge file description entry contains record-name-1." IN-PROC releases REC-B, the
      *> record of SORT-B, a sort file no SORT statement names. Expected: COBOL7081 at 85 at the
      *> RELEASE REC-B. Fails if the file of the record is not asked.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5104RL.
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
           RELEASE REC-B.
           MOVE "BBB" TO REC-A.
           RELEASE REC-A.
       OUT-PROC SECTION.
       O1.
           RETURN SORT-A AT END GO TO O9.
           DISPLAY KEY-A.
           GO TO O1.
       O9.
           EXIT.
