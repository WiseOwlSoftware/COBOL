      *>--- conformance-case
      *> id: pb5768_release_in_debugging_declarative_85
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
      *> THE DEBUGGING ARM: under WITH DEBUGGING MODE the debugging
      *> section DBG runs "immediately before each execution of the
      *> named procedure" I1 (§1985:XV.3.2.4 GR3a), a paragraph of
      *> the input procedure, so its RELEASE is executed as a result
      *> of the range and is in it; the range holds no input-output
      *> statement and no CALL. Fails (COBOL7081 on the RELEASE in
      *> DBG) if a declarative other than a file or open-mode USE is
      *> held to the I-O-statement test.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5768DB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANY-COMPUTER WITH DEBUGGING MODE.
       OBJECT-COMPUTER. ANY-COMPUTER.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SA ASSIGN TO P5768SC.
       DATA DIVISION.
       FILE SECTION.
       SD  SA.
       01  RA.
           05 KA PIC X(3).
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG SECTION.
           USE FOR DEBUGGING ON I1.
       DB1. MOVE "DDD" TO RA. RELEASE RA.
       END DECLARATIVES.
       M SECTION.
       M1. SORT SA ON ASCENDING KEY KA INPUT PROCEDURE IN-P
           OUTPUT PROCEDURE OUT-P.
           STOP RUN.
       IN-P SECTION.
       I1. MOVE "ZZZ" TO RA. RELEASE RA.
       OUT-P SECTION.
       O1. RETURN SA AT END GO TO O9.
           DISPLAY "R " RA. GO TO O1.
       O9. EXIT.
