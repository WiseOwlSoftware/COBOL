      *>--- conformance-case
      *> id: 1985-GR-IX_4_9_4-4
      *> rules: [1985-GR-IX.4.9.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.9.4 GR4 (THE WRITE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. WRTFROM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IFILE ASSIGN TO "IFILE"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IREC-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  IFILE
           LABEL RECORDS ARE STANDARD.
       01  IREC.
           05 IREC-KEY  PIC X(4).
           05 IREC-DATA PIC X(6).
       WORKING-STORAGE SECTION.
       01  WS-REC.
           05 WS-KEY  PIC X(4) VALUE "K001".
           05 WS-DATA PIC X(6) VALUE "ABCDEF".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IFILE.
           WRITE IREC FROM WS-REC
               INVALID KEY DISPLAY "INVALID KEY".
           CLOSE IFILE.
           DISPLAY WS-REC.
           STOP RUN.
