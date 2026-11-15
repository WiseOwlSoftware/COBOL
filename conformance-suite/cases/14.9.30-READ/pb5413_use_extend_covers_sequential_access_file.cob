      *>--- conformance-case
      *> id: pb5413_use_extend_covers_sequential_access_file
      *> rules: [GR-14.9.30.4-24]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5413 - the LEGAL side of 1985:IX.4.9.3 SR3 ("The INVALID KEY phrase must be specified if an applicable USE AFTER
      *>   STANDARD EXCEPTION procedure is not specified for the associated file-name", cite.py: OK). XF is indexed in SEQUENTIAL
      *>   access, so it can be open in the extend mode (1985:IX.4.4.3 SR1) and the USE ... ON EXTEND procedure is applicable to it
      *>   (1985:VIII.4.8.4 GR5): the WRITE names no INVALID KEY.
      *>   conformance:negative/pb5413-use-extend-dynamic-read-85 is the refused side (a dynamic-access file).
      *> DERIVATION. OPEN OUTPUT, WRITE key BB (status 00), CLOSE. OPEN EXTEND, WRITE key AA: a sequentially accessed indexed file
      *>   needs ascending record key values (the I-O status 21 sequence error, 1985:IX.1.3.4, cite.py: OK), so the WRITE fails with status 21;
      *>   with no INVALID KEY phrase the applicable USE ON EXTEND procedure is executed: HITS 1. Control returns after the WRITE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5413P1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XF ASSIGN TO "PB5413P1.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS XK
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  XF.
       01  XREC.
           05  XK  PIC X(2).
           05  XD  PIC X(2).
       WORKING-STORAGE SECTION.
       01  FS   PIC XX.
       01  HITS PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       U1 SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON EXTEND.
       U1-P.
           ADD 1 TO HITS.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN-PARA.
           OPEN OUTPUT XF.
           MOVE "BBxx" TO XREC.
           WRITE XREC.
           CLOSE XF.
           OPEN EXTEND XF.
           MOVE "AAyy" TO XREC.
           WRITE XREC.
           DISPLAY "EXTEND " FS " " HITS.
           CLOSE XF.
           STOP RUN.
