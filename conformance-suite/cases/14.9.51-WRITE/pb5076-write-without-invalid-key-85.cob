      *>--- conformance-case
      *> id: pb5076-write-without-invalid-key-85
      *> rules: [GR-14.9.51.4-29]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5076 - 1985:IX.4.9.3 SR3: "The INVALID KEY phrase must be specified if an applicable USE AFTER STANDARD EXCEPTION
      *>   procedure is not specified for the associated file-name." (cite.py: OK). The WRITE to the indexed file XF names no INVALID KEY
      *>   and the program has no declaratives.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5076N3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XF ASSIGN TO "PB5076N3.DAT"
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
           OPEN OUTPUT XF.
           MOVE "AAAA" TO XREC.
           WRITE XREC.
           CLOSE XF.
           STOP RUN.
