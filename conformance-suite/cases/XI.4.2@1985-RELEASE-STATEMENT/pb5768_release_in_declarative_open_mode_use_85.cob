      *>--- conformance-case
      *> id: pb5768_release_in_declarative_open_mode_use_85
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
      *> THE OPEN-MODE ARM: USE AFTER STANDARD ERROR PROCEDURE ON INPUT
      *> names no file, so any file opened INPUT may execute it; the
      *> range's OPEN INPUT F of an absent file fails (35) and
      *> executes D1, whose RELEASE is then in the range. Fails
      *> (COBOL7081) if an open-mode USE is matched only against the
      *> files a USE names.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5768OM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SA ASSIGN TO P5768SC.
           SELECT F ASSIGN TO "pb5768om-absent.dat" FILE STATUS IS FS.
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
           USE AFTER STANDARD ERROR PROCEDURE ON INPUT.
       D1. MOVE "DDD" TO RA. RELEASE RA.
       END DECLARATIVES.
       M SECTION.
       M1. SORT SA ON ASCENDING KEY KA INPUT PROCEDURE IN-P
           OUTPUT PROCEDURE OUT-P.
           STOP RUN.
       IN-P SECTION.
       I1. MOVE "ZZZ" TO RA. RELEASE RA.
           OPEN INPUT F.
           DISPLAY "OPEN " FS.
       OUT-P SECTION.
       O1. RETURN SA AT END GO TO O9.
           DISPLAY "R " RA. GO TO O1.
       O9. EXIT.
