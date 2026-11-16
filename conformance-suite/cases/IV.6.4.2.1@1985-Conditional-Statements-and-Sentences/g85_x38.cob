      *>--- conformance-case
      *> id: g85_x38
      *> rules: [1985-GR-IV.6.4.2.1.1-3, 1985-GR-IV.6.4.2.1.1.1-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.4.2.1.1 GR3 (1985-GR-IV.6.4.2.1.1-3) and
      *> §1985:IV.6.4.2.1.1.1 GR3 (1985-GR-IV.6.4.2.1.1.1-3). An output
      *> test, the accepted half: a WRITE with each of INVALID KEY, NOT
      *> INVALID KEY, END-OF-PAGE and NOT END-OF-PAGE, delimited by
      *> END-WRITE, stands as imperative-statement-1 of an inline
      *> PERFORM. Derived: the second WRITE of key K001 is a duplicate
      *> (INVALID KEY). PRTF's LINAGE-COUNTER is 1 after OPEN OUTPUT
      *> and each WRITE without ADVANCING adds 1 (§1985:VII.3.7.4 GR9d,
      *> c3): 2 is before the footing at 3 (NOT END-OF-PAGE), 3 reaches
      *> it (END-OF-PAGE, §1985:VII.4.7.4 GR18). The declarative USE
      *> AFTER STANDARD ERROR procedure makes the phrase-less and
      *> NOT-only statements legal (§1985:VII.4.4.3 SR2 and its
      *> siblings require AT END or INVALID KEY otherwise); no
      *> exception occurs, so DECLARATIVE is never displayed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85X38.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XF ASSIGN TO "G85X38X"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS XKEY.
           SELECT PRTF ASSIGN TO "G85X38P".
       DATA DIVISION.
       FILE SECTION.
       FD  XF LABEL RECORDS ARE STANDARD.
       01  XREC.
           05  XKEY PIC X(4).
           05  XDAT PIC X(4).
       FD  PRTF LABEL RECORDS ARE OMITTED
           LINAGE IS 5 LINES WITH FOOTING AT 3.
       01  PREC PIC X(8).
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON XF PRTF.
       D-PARA.
           DISPLAY "DECLARATIVE".
       END DECLARATIVES.
       MAIN-SEC SECTION.
       P0.
           OPEN OUTPUT XF PRTF.
           MOVE "K001" TO XKEY.
           MOVE "D001" TO XDAT.
           PERFORM
               WRITE XREC NOT INVALID KEY DISPLAY "NOT INVALID KEY"
               END-WRITE
               WRITE XREC INVALID KEY DISPLAY "INVALID KEY"
               END-WRITE
               MOVE "LINE 1" TO PREC
               WRITE PREC
                   NOT AT END-OF-PAGE DISPLAY "NOT END-OF-PAGE"
               END-WRITE
               MOVE "LINE 2" TO PREC
               WRITE PREC AT END-OF-PAGE DISPLAY "END-OF-PAGE"
               END-WRITE
           END-PERFORM.
           CLOSE XF PRTF.
           DISPLAY "END".
           STOP RUN.
