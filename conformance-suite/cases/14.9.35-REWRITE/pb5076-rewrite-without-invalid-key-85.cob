      *>--- conformance-case
      *> id: pb5076-rewrite-without-invalid-key-85
      *> rules: [GR-14.9.35.4-19]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5076 - 1985:IX.4.6.3 SR3: "The INVALID KEY phrase must be specified in the REWRITE statement for indexed files for which
      *>   an appropriate USE AFTER STANDARD EXCEPTION procedure is not specified." (cite.py: OK, as amended by X3.23a-1989).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5076N4.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XF ASSIGN TO "PB5076N4.DAT"
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
           MOVE "AAAA" TO XREC.
           REWRITE XREC.
           CLOSE XF.
           STOP RUN.
