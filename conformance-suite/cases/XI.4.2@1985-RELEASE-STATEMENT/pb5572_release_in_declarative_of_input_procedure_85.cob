      *>--- conformance-case
      *> id: pb5572_release_in_declarative_of_input_procedure_85
      *> rules: [1985-SR-XI.4.2.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5572. §1985:XI.4.4.4 GR7: the range of an input procedure "includes all
      *>   statements in declarative procedures that are executed as a result of" the
      *>   statements in it, so a RELEASE written in the declaratives is within the range of an
      *>   input procedure (§1985:XI.4.2.3 SR2) when that procedure's statements execute it.
      *>   IN-PROC opens IN-F, which does not exist; the unsuccessful OPEN executes D1, whose
      *>   RELEASE runs inside the range, then IN-PROC releases AAA. OUT-PROC returns both in
      *>   ascending KEY-A order. Expected output: AAA, then DDD. Fails if a declarative RELEASE
      *>   is refused where an input procedure for its file exists (COBOL7081).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5572RI.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SORT-A ASSIGN TO P5572SC.
           SELECT IN-F ASSIGN TO P5572MU.
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
           SORT SORT-A ON ASCENDING KEY KEY-A
               INPUT PROCEDURE IS IN-PROC
               OUTPUT PROCEDURE IS OUT-PROC.
           STOP RUN.
       IN-PROC SECTION.
       I1.
           OPEN INPUT IN-F.
           MOVE "AAA" TO REC-A.
           RELEASE REC-A.
       OUT-PROC SECTION.
       O1.
           RETURN SORT-A AT END GO TO O9.
           DISPLAY KEY-A.
           GO TO O1.
       O9.
           EXIT.
