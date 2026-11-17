      *>--- conformance-case
      *> id: pb5877-read-key-redefines-own-record
      *> rules: [SR-14.9.30.3-11]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5877 - ISO 14.9.30.3 SR11 requires the KEY operand to be specified as a record key of file-name-1.
      *> IX-KR REDEFINES the record key IX-KEY inside the key's OWN record description entry. The implicit keys of
      *> 12.4.5.12.4 GR4 reach all OTHER record description entries of the file, so IX-KR is no key. The leg fails
      *> if it compiles.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5877R.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "pb5877r.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD IXF.
       01 IX-REC.
          05 IX-KEY  PIC X(4).
          05 IX-KR REDEFINES IX-KEY PIC X(4).
          05 IX-DATA PIC X(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN INPUT IXF
           READ IXF KEY IS IX-KR
               INVALID KEY DISPLAY "INVALID"
           END-READ
           CLOSE IXF
           STOP RUN.
