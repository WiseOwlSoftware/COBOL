      *>--- conformance-case
      *> id: g85_x37
      *> rules: [1985-GR-IV.6.4.2.1.1-2, 1985-GR-IV.6.4.2.1.1.1-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.4.2.1.1 GR2 (1985-GR-IV.6.4.2.1.1-2) and
      *> §1985:IV.6.4.2.1.1.1 GR1 (1985-GR-IV.6.4.2.1.1.1-1). An output
      *> test, the accepted half: a READ with each of AT END, NOT AT
      *> END, INVALID KEY and NOT INVALID KEY, delimited by END-READ,
      *> is an imperative statement and stands as
      *> imperative-statement-1 of an inline PERFORM. Derived: SF holds
      *> one record R001 (NOT AT END, then AT END); XF holds K001/D001
      *> (K001 NOT INVALID KEY, K009 INVALID KEY). The declarative USE
      *> AFTER STANDARD ERROR procedure makes the phrase-less and
      *> NOT-only statements legal (§1985:VII.4.4.3 SR2 and its
      *> siblings require AT END or INVALID KEY otherwise); no
      *> exception occurs, so DECLARATIVE is never displayed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85X37.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "G85X37S".
           SELECT XF ASSIGN TO "G85X37X"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS XKEY.
       DATA DIVISION.
       FILE SECTION.
       FD  SF LABEL RECORDS ARE STANDARD.
       01  SREC PIC X(4).
       FD  XF LABEL RECORDS ARE STANDARD.
       01  XREC.
           05  XKEY PIC X(4).
           05  XDAT PIC X(4).
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON SF XF.
       D-PARA.
           DISPLAY "DECLARATIVE".
       END DECLARATIVES.
       MAIN-SEC SECTION.
       P0.
           OPEN OUTPUT SF XF.
           MOVE "R001" TO SREC.
           WRITE SREC.
           MOVE "K001" TO XKEY.
           MOVE "D001" TO XDAT.
           WRITE XREC.
           CLOSE SF XF.
           OPEN INPUT SF XF.
           PERFORM
               READ SF NOT AT END DISPLAY "NOT AT END " SREC
               END-READ
               READ SF AT END DISPLAY "AT END"
               END-READ
               MOVE "K001" TO XKEY
               READ XF
                   NOT INVALID KEY DISPLAY "NOT INVALID KEY " XDAT
               END-READ
               MOVE "K009" TO XKEY
               READ XF INVALID KEY DISPLAY "INVALID KEY"
               END-READ
           END-PERFORM.
           CLOSE SF XF.
           DISPLAY "END".
           STOP RUN.
