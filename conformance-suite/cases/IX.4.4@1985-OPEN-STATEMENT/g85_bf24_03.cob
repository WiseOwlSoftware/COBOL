      *>--- conformance-case
      *> id: g85_bf24_03
      *> rules: [1985-GR-IX.4.4.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.4.4 GR2 - "If the file connector associated with
      *> file-name is an external file connector, there is only one
      *> record area associated with the file connector for the run
      *> unit." An indexed EXTERNAL file in random access, described
      *> identically in G85BF2403 and its contained G85BF2403S. The
      *> caller OPENs it and MOVEs K001MAIN01 into its record area;
      *> the contained program sees it (SUB-SEES=K001MAIN01), WRITEs
      *> K002SUBW02 and leaves K003SUBL03 in the area; the caller sees
      *> that (MAIN-SEES=K003SUBL03) and WRITEs its area, so the prime
      *> key written is K003, never K001. Read back by key: K002 and
      *> K003 are found, K001 is not (the invalid key condition,
      *> K001-ABSENT). A program-private record area fails SUB-SEES,
      *> MAIN-SEES and finds K001 instead of K003.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2403.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XF ASSIGN TO "g85bf2403.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS XF-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  XF IS EXTERNAL.
       01  XF-REC.
           05 XF-KEY PIC X(4).
           05 XF-VAL PIC X(6).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT XF.
           MOVE "K001MAIN01" TO XF-REC.
           CALL "G85BF2403S".
           DISPLAY "MAIN-SEES=" XF-REC.
           WRITE XF-REC INVALID KEY DISPLAY "W1-INVALID".
           CLOSE XF.
           OPEN INPUT XF.
           MOVE "K002" TO XF-KEY.
           READ XF INVALID KEY DISPLAY "K002-ABSENT".
           DISPLAY "R2=" XF-REC.
           MOVE "K003" TO XF-KEY.
           READ XF INVALID KEY DISPLAY "K003-ABSENT".
           DISPLAY "R3=" XF-REC.
           MOVE "K001" TO XF-KEY.
           READ XF INVALID KEY DISPLAY "K001-ABSENT".
           CLOSE XF.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2403S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XF ASSIGN TO "g85bf2403.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS XF-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  XF IS EXTERNAL.
       01  XF-REC.
           05 XF-KEY PIC X(4).
           05 XF-VAL PIC X(6).
       PROCEDURE DIVISION.
       SUB-PARA.
           DISPLAY "SUB-SEES=" XF-REC.
           MOVE "K002SUBW02" TO XF-REC.
           WRITE XF-REC INVALID KEY DISPLAY "W2-INVALID".
           MOVE "K003SUBL03" TO XF-REC.
           EXIT PROGRAM.
       END PROGRAM G85BF2403S.
       END PROGRAM G85BF2403.
