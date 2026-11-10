      *>--- conformance-case
      *> id: pb5076-delete-without-invalid-key-85
      *> rules: [GR-14.9.10.4-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5076 - 1985:IX.4.3.3 SR2: "The INVALID KEY phrase must be specified for a DELETE statement which references a file which
      *>   is not in sequential access mode and for which an applicable USE AFTER STANDARD EXCEPTION procedure is not specified." (cite.py: OK).
      *>   XF is in random access mode and the program has no declaratives.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5076N5.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XF ASSIGN TO "PB5076N5.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS XK.
       DATA DIVISION.
       FILE SECTION.
       FD  XF.
       01  XREC.
           05  XK  PIC X(2).
           05  XD  PIC X(2).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN I-O XF.
           MOVE "AA" TO XK.
           DELETE XF RECORD.
           CLOSE XF.
           STOP RUN.
