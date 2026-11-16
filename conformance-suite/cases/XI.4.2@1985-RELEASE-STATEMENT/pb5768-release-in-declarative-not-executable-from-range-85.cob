      *>--- conformance-case
      *> id: pb5768-release-in-declarative-not-executable-from-range-85
      *> rules: [1985-SR-XI.4.2.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5768. The range of the SORT procedures includes "all
      *> statements in declarative procedures that are executed as a result
      *> of the execution of statements in the range" (§1985:XI.4.4.4 GR7;
      *> GR10 for an output procedure). The USE procedure D applies to F,
      *> and no statement of either procedure is an input-output statement
      *> on F or a CALL, so no statement of the range can execute D: its
      *> RELEASE is outside every range and is refused (COBOL7081), as
      *> where no procedure exists at all (kb/Work PB5572). Only the OPEN
      *> of F after the SORT can execute D.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5768REL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SA ASSIGN TO "SA".
           SELECT F ASSIGN TO "nofile.dat" FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       SD  SA.
       01  RA.
           05 KA PIC X(3).
       FD  F.
       01  FR PIC X(3).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F.
       D1. MOVE "DDD" TO RA. RELEASE RA. DISPLAY "T14 DECL".
       END DECLARATIVES.
       M SECTION.
       M1. SORT SA ON ASCENDING KEY KA INPUT PROCEDURE IN-P
           OUTPUT PROCEDURE OUT-P.
           OPEN INPUT F. DISPLAY "T14 FS " FS.
           STOP RUN.
       IN-P SECTION.
       I1. MOVE "ZZZ" TO RA. RELEASE RA.
       OUT-P SECTION.
       O1. RETURN SA AT END GO TO O9.
           DISPLAY "T14 " RA. GO TO O1.
       O9. EXIT.
