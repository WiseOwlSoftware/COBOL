      *>--- conformance-case
      *> id: pb5076-start-without-invalid-key-85
      *> rules: [GR-14.9.41.4-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5076 - 1985:IX.4.7.3 SR3: "The INVALID KEY phrase must be specified if no applicable USE AFTER STANDARD EXCEPTION
      *>   procedure is specified for file-name-1." (cite.py: OK).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5076N6.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XF ASSIGN TO "PB5076N6.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS XK.
       DATA DIVISION.
       FILE SECTION.
       FD  XF.
       01  XREC.
           05  XK  PIC X(2).
           05  XD  PIC X(2).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN INPUT XF.
           MOVE "AA" TO XK.
           START XF KEY IS EQUAL TO XK.
           CLOSE XF.
           STOP RUN.
