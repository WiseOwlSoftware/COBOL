      *>--- conformance-case
      *> id: pb3569-read-next-key
      *> rules: [GR-14.9.30.4-19, FMT-14.9.30.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3569. ISO 14.9.30.2 prints the KEY phrase in Format 2
      *> (random) only, and Format 2 prints no NEXT phrase; ISO 14.9.30.4
      *> GR19: "An implicit or explicit NEXT phrase or a PREVIOUS phrase
      *> results in a sequential read" (each citation through cite.py
      *> --check). READ ... NEXT KEY IS therefore mixes the two formats
      *> whatever the access mode, and the refusal (COBOL0864 at every
      *> edition) names the one sequential-read phrase the statement
      *> wrote, NEXT, and no phrase it did not. The file is ACCESS MODE
      *> DYNAMIC, where KEY alone is a legal random read and NEXT alone a
      *> legal sequential one (GR4), so the refusal is the combination
      *> and nothing about the file.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3569C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "PB3569C.TMP"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  IXF.
       01  IX-REC.
           05 IX-KEY     PIC X(3).
           05 IX-DATA    PIC X(3).
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN INPUT IXF
           READ IXF NEXT KEY IS IX-KEY
           END-READ
           CLOSE IXF
           STOP RUN.
