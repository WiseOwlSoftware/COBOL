      *>--- conformance-case
      *> id: g85_bf24_02
      *> rules: [1985-GR-VIII.4.4.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.4.4 GR2 - "If the file connector associated with
      *> file-name is an external file connector, there is only one
      *> record area associated with the file connector for the run
      *> unit." A relative EXTERNAL file in random access, described in
      *> G85BF2402 and its contained G85BF2402S; the RELATIVE KEY is
      *> one external data item in both (§1985:VIII.2.3.4 GR1f).
      *> The caller OPENs it and MOVEs MAINMOVE01 into its record
      *> area; the contained program sees it (SUB-SEES=MAINMOVE01),
      *> WRITEs SUBWRITE02 as record 2 and leaves SUBMOVE-03 in the
      *> area; the caller sees that (MAIN-SEES=SUBMOVE-03) and WRITEs
      *> it as record 1. Read back: R1=SUBMOVE-03, R2=SUBWRITE02. A
      *> program-private record area fails SUB-SEES, MAIN-SEES and R1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2402.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RLF ASSIGN TO "g85bf2402.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK.
       DATA DIVISION.
       FILE SECTION.
       FD  RLF IS EXTERNAL.
       01  RLF-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  RK EXTERNAL PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RLF.
           MOVE "MAINMOVE01" TO RLF-REC.
           CALL "G85BF2402S".
           DISPLAY "MAIN-SEES=" RLF-REC.
           MOVE 1 TO RK.
           WRITE RLF-REC INVALID KEY DISPLAY "W1-INVALID".
           CLOSE RLF.
           OPEN INPUT RLF.
           MOVE 1 TO RK.
           READ RLF INVALID KEY DISPLAY "R1-INVALID".
           DISPLAY "R1=" RLF-REC.
           MOVE 2 TO RK.
           READ RLF INVALID KEY DISPLAY "R2-INVALID".
           DISPLAY "R2=" RLF-REC.
           CLOSE RLF.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2402S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RLF ASSIGN TO "g85bf2402.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK.
       DATA DIVISION.
       FILE SECTION.
       FD  RLF IS EXTERNAL.
       01  RLF-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  RK EXTERNAL PIC 9(4).
       PROCEDURE DIVISION.
       SUB-PARA.
           DISPLAY "SUB-SEES=" RLF-REC.
           MOVE 2 TO RK.
           MOVE "SUBWRITE02" TO RLF-REC.
           WRITE RLF-REC INVALID KEY DISPLAY "W2-INVALID".
           MOVE "SUBMOVE-03" TO RLF-REC.
           EXIT PROGRAM.
       END PROGRAM G85BF2402S.
       END PROGRAM G85BF2402.
