      *>--- conformance-case
      *> id: pb5076_keyed_statements_without_phrases_covered_by_use_io
      *> rules: [GR-14.9.30.4-24, GR-14.9.51.4-29, GR-14.9.35.4-19, GR-14.9.10.4-3, GR-14.9.41.4-5]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5076 - the LEGAL side of the keyed arms of the mandatory-phrase rule (1985:IX.4.5.3 SR7, IX.4.9.3 SR3, IX.4.6.3 SR3,
      *>   IX.4.3.3 SR2, IX.4.7.3 SR3; cite.py: OK). A USE AFTER STANDARD EXCEPTION PROCEDURE ON I-O is applicable to every verb of a file open
      *>   I-O (1985:VII.4.6.4 GR5 d), so no statement below names INVALID KEY or AT END.
      *> DERIVATION. Three records A1x1, B2y2, C3z3 are written. READ with key B2 returns B2y2. REWRITE stores B2QQ. DELETE with key A1
      *>   removes A1x1. START at key B2 positions on B2QQ; READ NEXT returns B2QQ, then C3z3. No operation raises an exception, so the
      *>   declarative never runs and the status is 00. conformance:negative/pb5076-write-without-invalid-key-85 and its five siblings are the
      *>   refused side.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5076P2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XF ASSIGN TO "PB5076P2.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
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
       PROCEDURE DIVISION.
       DECLARATIVES.
       XF-USE SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON I-O.
       XF-USE-P.
           DISPLAY "USE RAN " FS.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN-PARA.
           OPEN OUTPUT XF.
           CLOSE XF.
           OPEN I-O XF.
           MOVE "A1x1" TO XREC.
           WRITE XREC.
           MOVE "B2y2" TO XREC.
           WRITE XREC.
           MOVE "C3z3" TO XREC.
           WRITE XREC.
           MOVE "B2" TO XK.
           READ XF.
           DISPLAY "READ " XREC.
           MOVE "QQ" TO XD.
           REWRITE XREC.
           MOVE "A1" TO XK.
           DELETE XF RECORD.
           MOVE "B2" TO XK.
           START XF KEY IS EQUAL TO XK.
           READ XF NEXT.
           DISPLAY "NEXT " XREC.
           READ XF NEXT.
           DISPLAY "NEXT " XREC.
           DISPLAY "FS " FS.
           CLOSE XF.
           STOP RUN.
