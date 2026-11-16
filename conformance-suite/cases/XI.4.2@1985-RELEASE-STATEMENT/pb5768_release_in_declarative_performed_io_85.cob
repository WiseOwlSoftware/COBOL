      *>--- conformance-case
      *> id: pb5768_release_in_declarative_performed_io_85
      *> rules: [1985-SR-XI.4.2.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5768. §1985:XI.4.4.4 GR7: the range of an input
      *> procedure includes "all statements in declarative procedures
      *> that are executed as a result of the execution of statements
      *> in the range of the input procedure", so a RELEASE in the
      *> declaratives is within the range (§1985:XI.4.2.3 SR2) when a
      *> statement of the range can execute its declarative
      *> (VersionConformancePass.DeclarativeExecutableFromRange).
      *> THE PERFORMED ARM: the range's OPEN INPUT F (an absent file,
      *> 35) is in P2, a paragraph outside the input procedure that
      *> it PERFORMs, so P2 is in the range (§1985:XI.4.4.4 GR7) and
      *> its OPEN executes D1 (USE ON F), whose RELEASE is then in the
      *> range. Fails (COBOL7081) if only the input procedure's own
      *> paragraphs, not the range they reach, are asked.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5768PI.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SA ASSIGN TO P5768SC.
           SELECT F ASSIGN TO "pb5768pi-absent.dat" FILE STATUS IS FS.
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
       D1. MOVE "DDD" TO RA. RELEASE RA.
       END DECLARATIVES.
       M SECTION.
       M1. SORT SA ON ASCENDING KEY KA INPUT PROCEDURE IN-P
           OUTPUT PROCEDURE OUT-P.
           STOP RUN.
       IN-P SECTION.
       I1. MOVE "ZZZ" TO RA. RELEASE RA.
           PERFORM P2.
       OUT-P SECTION.
       O1. RETURN SA AT END GO TO O9.
           DISPLAY "R " RA. GO TO O1.
       O9. EXIT.
       X SECTION.
       P2. OPEN INPUT F.
           DISPLAY "OPEN " FS.
