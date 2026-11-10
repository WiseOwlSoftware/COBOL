      *>--- conformance-case
      *> id: pb5076-read-indexed-without-phrase-85
      *> rules: [GR-14.9.30.4-24]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5076 - 1985:IX.4.5.3 SR7: "The INVALID KEY phrase or the AT END phrase must be specified if no applicable USE AFTER
      *>   STANDARD EXCEPTION procedure is specified for file-name-1." (cite.py: OK). XF is indexed in sequential access, the program
      *>   has no declaratives, and the READ names neither phrase.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5076N2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XF ASSIGN TO "PB5076N2.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
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
           READ XF.
           CLOSE XF.
           STOP RUN.
