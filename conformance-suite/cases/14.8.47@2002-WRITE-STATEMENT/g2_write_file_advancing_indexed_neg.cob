      *>--- conformance-case
      *> id: g2_write_file_advancing_indexed_neg
      *> rules: [2002-SR-14.8.47.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.47.2 SR3 - "If the organization of the write file is
      *>   indexed or relative, format 2 shall be specified." The write
      *>   file here is the indexed IX named by FILE IX (SR1), and the
      *>   ADVANCING phrase exists only in format 1, so the statement
      *>   violates SR3 and is refused: COBOL1720 at the WRITE, as for
      *>   the record-name twin pb909.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2WRF3N.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX ASSIGN TO "g2wrf3n.idx" ORGANIZATION INDEXED
               ACCESS MODE IS DYNAMIC RECORD KEY IS K.
       DATA DIVISION.
       FILE SECTION.
       FD IX.
       01 R1.
          05 K PIC X(4).
       WORKING-STORAGE SECTION.
       01 WX PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IX
           WRITE FILE IX FROM WX AFTER ADVANCING 1 LINE
           CLOSE IX
           STOP RUN.
