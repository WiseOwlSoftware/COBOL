      *>--- conformance-case
      *> id: pb5413-use-extend-dynamic-read-85
      *> rules: [GR-14.9.30.4-24]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5413 - 1985:IX.4.5.3 SR7: "The INVALID KEY phrase or the AT END phrase must be specified if no applicable USE AFTER
      *>   STANDARD EXCEPTION procedure is specified for file-name-1." (cite.py: OK). XF is indexed in DYNAMIC access; its only
      *>   declarative is USE ... ON EXTEND, which runs "for any file open in the extend mode" (1985:VIII.4.8.4 GR5, cite.py: OK), and
      *>   "The EXTEND phrase must only be used for files in the sequential access mode" (1985:IX.4.4.3 SR1, cite.py: OK), so it is
      *>   never applicable to XF and the READ with no phrase is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5413N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XF ASSIGN TO "PB5413N1.DAT"
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
       DECLARATIVES.
       U1 SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON EXTEND.
       U1-P.
           EXIT.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN-PARA.
           OPEN I-O XF.
           READ XF.
           CLOSE XF.
           STOP RUN.
