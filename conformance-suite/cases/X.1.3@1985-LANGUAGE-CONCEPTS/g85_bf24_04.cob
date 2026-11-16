      *>--- conformance-case
      *> id: g85_bf24_04
      *> rules: [1985-GR-X.1.3.7-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.1.3.7 GR1 - "An external file connector may be
      *> referenced from any program which describes that file
      *> connector." Three programs describe the EXTERNAL sequential
      *> file EF: G85BF2404 and its contained G85BF2404A and
      *> G85BF2404B. Each statement below works on the ONE connector,
      *> whichever program executes it:
      *>   G85BF2404A OPENs OUTPUT and WRITEs SUBOPEN-01;
      *>   G85BF2404 (which never opened EF itself) WRITEs MAINWRT-02
      *>   and CLOSEs the connector, then OPENs INPUT and READs
      *>   MAIN-R1=SUBOPEN-01;
      *>   G85BF2404B READs the NEXT record on that same connector,
      *>   SUB-R2=MAINWRT-02 (the CALL does not reposition it,
      *>   §1985:X.5.2.4 GR8), then its second READ hits the end of
      *>   file: SUB-EOF;
      *>   G85BF2404 CLOSEs it.
      *> A connector private to each program fails at once: the
      *> caller's WRITE would reference a file not in an open mode.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2404.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EF ASSIGN TO "g85bf2404.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  EF IS EXTERNAL.
       01  EF-REC PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "G85BF2404A".
           MOVE "MAINWRT-02" TO EF-REC.
           WRITE EF-REC.
           CLOSE EF.
           OPEN INPUT EF.
           READ EF AT END DISPLAY "MAIN-EOF".
           DISPLAY "MAIN-R1=" EF-REC.
           CALL "G85BF2404B".
           CLOSE EF.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2404A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EF ASSIGN TO "g85bf2404.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  EF IS EXTERNAL.
       01  EF-REC PIC X(10).
       PROCEDURE DIVISION.
       A-PARA.
           OPEN OUTPUT EF.
           MOVE "SUBOPEN-01" TO EF-REC.
           WRITE EF-REC.
           EXIT PROGRAM.
       END PROGRAM G85BF2404A.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2404B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EF ASSIGN TO "g85bf2404.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  EF IS EXTERNAL.
       01  EF-REC PIC X(10).
       PROCEDURE DIVISION.
       B-PARA.
           READ EF AT END DISPLAY "SUB-EOF-EARLY".
           DISPLAY "SUB-R2=" EF-REC.
           READ EF AT END DISPLAY "SUB-EOF".
           EXIT PROGRAM.
       END PROGRAM G85BF2404B.
       END PROGRAM G85BF2404.
