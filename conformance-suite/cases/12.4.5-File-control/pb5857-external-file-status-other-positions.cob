      *>--- conformance-case
      *> id: pb5857-external-file-status-other-positions
      *> rules: [GR-12.4.5.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5857 (the FILE STATUS arm of the same identity).
      *> §2023:14.8.4.2: "For each external file connector, the file
      *> status, linage and relative key data items shall be external
      *> data items and shall refer to the same corresponding storage in
      *> each runtime element for which the file connector is used."
      *> EXT-FS is positions 1-2 of the external record EXT-STAT in
      *> PB5857E and positions 3-4 in PB5857F: the same name, other
      *> storage, so the two FILE STATUS items are not the same
      *> corresponding external data item and the program is refused with
      *> COBOL1573. A compiler that compares the names alone accepts it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5857E.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EXT-SEQ ASSIGN TO "pb5857fs.dat"
               FILE STATUS IS EXT-FS.
       DATA DIVISION.
       FILE SECTION.
       FD  EXT-SEQ IS EXTERNAL.
       01  EXT-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  EXT-STAT IS EXTERNAL.
           05  EXT-FS  PIC XX.
           05  FILLER  PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT EXT-SEQ.
           CLOSE EXT-SEQ.
           CALL "PB5857F".
           STOP RUN.
       END PROGRAM PB5857E.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5857F.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EXT-SEQ ASSIGN TO "pb5857fs.dat"
               FILE STATUS IS EXT-FS.
       DATA DIVISION.
       FILE SECTION.
       FD  EXT-SEQ IS EXTERNAL.
       01  EXT-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  EXT-STAT IS EXTERNAL.
           05  FILLER  PIC XX.
           05  EXT-FS  PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN INPUT EXT-SEQ.
           CLOSE EXT-SEQ.
           GOBACK.
       END PROGRAM PB5857F.
