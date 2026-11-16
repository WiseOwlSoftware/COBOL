      *>--- conformance-case
      *> id: g2_write_sequential_lock_phrase_manual
      *> rules: [2002-SR-14.8.47.2-21]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.47.2 SR21 - "If automatic locking has been specified
      *> for the write file, neither the WITH LOCK phrase nor the WITH NO
      *> LOCK phrase shall be specified." The converse leg on Format 1
      *> (sequential), the phrases in the 2002 order (§2002:14.8.47.1:
      *> END-OF-PAGE phrases, then WITH LOCK; kb/Work PB5620): LOCK MODE
      *> IS MANUAL, so WITH LOCK is legal and the statement compiles.
      *> Line 2 of a 5-line page body fits, so no end-of-page condition
      *> occurs and §2002:14.8.47.3 GR24 "control is transferred to
      *> imperative-statement-2": N=2, status 00. Fails if the 2002 order
      *> does not parse or if SR21 is applied to a MANUAL file. The
      *> AUTOMATIC twin is g2_write_sequential_lock_phrase_automatic_neg.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2WR21S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LPF ASSIGN TO "g2wr21s.prt" ORGANIZATION SEQUENTIAL
               LOCK MODE IS MANUAL FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD LPF LINAGE IS 5 LINES.
       01 P-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       01 N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT LPF
           MOVE "AAAA" TO P-REC
           WRITE P-REC AFTER ADVANCING 1 LINE
               AT END-OF-PAGE ADD 1 TO N
               NOT AT END-OF-PAGE ADD 2 TO N
               WITH LOCK
           END-WRITE
           DISPLAY "W " FS " N=" N
           CLOSE LPF
           STOP RUN.
