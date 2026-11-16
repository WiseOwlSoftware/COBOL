      *>--- conformance-case
      *> id: g85_bf24_01
      *> rules: [1985-GR-VII.4.3.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.3.4 GR2 - "If the file connector associated with
      *> file-name is an external file connector, there is only one
      *> record area associated with the file connector for the run
      *> unit." A sequential EXTERNAL file, described in G85BF2401 and
      *> in its contained G85BF2401S under the same record-name.
      *> G85BF2401 OPENs it and MOVEs MAINMOVE01 into its record area;
      *> the contained program DISPLAYs its own record-name: ONE area,
      *> so SUB-SEES=MAINMOVE01 (fails if each program had its own
      *> area: spaces or garbage). The contained program WRITEs
      *> SUBWRITE02 and leaves SUBMOVE-03 in the area; back in the
      *> caller MAIN-SEES=SUBMOVE-03 (fails the same way), and the
      *> caller WRITEs that area. Read back: R1=SUBWRITE02,
      *> R2=SUBMOVE-03, then the at end condition (EOF). The MOVE
      *> after the contained WRITE is needed: §1985:VII.4.7.4 GR2,
      *> the released record "is no longer available in the record
      *> area".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2401.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EF ASSIGN TO "g85bf2401.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  EF IS EXTERNAL.
       01  EF-REC PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT EF.
           MOVE "MAINMOVE01" TO EF-REC.
           CALL "G85BF2401S".
           DISPLAY "MAIN-SEES=" EF-REC.
           WRITE EF-REC.
           CLOSE EF.
           OPEN INPUT EF.
           READ EF AT END DISPLAY "EOF-EARLY".
           DISPLAY "R1=" EF-REC.
           READ EF AT END DISPLAY "EOF-EARLY".
           DISPLAY "R2=" EF-REC.
           READ EF AT END DISPLAY "EOF".
           CLOSE EF.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2401S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EF ASSIGN TO "g85bf2401.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  EF IS EXTERNAL.
       01  EF-REC PIC X(10).
       PROCEDURE DIVISION.
       SUB-PARA.
           DISPLAY "SUB-SEES=" EF-REC.
           MOVE "SUBWRITE02" TO EF-REC.
           WRITE EF-REC.
           MOVE "SUBMOVE-03" TO EF-REC.
           EXIT PROGRAM.
       END PROGRAM G85BF2401S.
       END PROGRAM G85BF2401.
