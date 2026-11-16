      *>--- conformance-case
      *> id: g85_bf24_05
      *> rules: [1985-GR-X.4.5.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.4.5.4 GR4 - "The file connector associated with this
      *> description entry is an external file connector." G85BF2405
      *> and its contained G85BF2405S each give EF the EXTERNAL clause,
      *> so both entries name ONE connector; its open mode is that one
      *> connector's state. Each program has its own FILE STATUS item.
      *>   caller OPEN OUTPUT -> MAIN-OPEN=00, WRITE MAIN-OPN01;
      *>   contained program (no OPEN of its own) WRITEs SUB-WRT-02
      *>   -> SUB-WRITE=00, then CLOSEs -> SUB-CLOSE=00;
      *>   caller OPEN INPUT -> MAIN-OPEN2=00: the contained CLOSE
      *>   closed the caller's connector (an open connector would give
      *>   41, §1985:VII.1.3.5);
      *>   caller READs MAIN-R1=MAIN-OPN01 and the contained program
      *>   READs the next record, SUB-R2=SUB-WRT-02;
      *>   caller CLOSE -> MAIN-CLOSE=00.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2405.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EF ASSIGN TO "g85bf2405.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST-M.
       DATA DIVISION.
       FILE SECTION.
       FD  EF IS EXTERNAL.
       01  EF-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  ST-M PIC XX.
       01  STEP PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT EF.
           DISPLAY "MAIN-OPEN=" ST-M.
           MOVE "MAIN-OPN01" TO EF-REC.
           WRITE EF-REC.
           CALL "G85BF2405S" USING STEP.
           OPEN INPUT EF.
           DISPLAY "MAIN-OPEN2=" ST-M.
           READ EF AT END DISPLAY "MAIN-EOF".
           DISPLAY "MAIN-R1=" EF-REC.
           MOVE 2 TO STEP.
           CALL "G85BF2405S" USING STEP.
           CLOSE EF.
           DISPLAY "MAIN-CLOSE=" ST-M.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2405S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EF ASSIGN TO "g85bf2405.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST-S.
       DATA DIVISION.
       FILE SECTION.
       FD  EF IS EXTERNAL.
       01  EF-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  ST-S PIC XX.
       LINKAGE SECTION.
       01  STEP PIC 9.
       PROCEDURE DIVISION USING STEP.
       S-PARA.
           IF STEP = 1
               MOVE "SUB-WRT-02" TO EF-REC
               WRITE EF-REC
               DISPLAY "SUB-WRITE=" ST-S
               CLOSE EF
               DISPLAY "SUB-CLOSE=" ST-S
           ELSE
               READ EF AT END DISPLAY "SUB-EOF"
               END-READ
               DISPLAY "SUB-R2=" EF-REC.
           EXIT PROGRAM.
       END PROGRAM G85BF2405S.
       END PROGRAM G85BF2405.
