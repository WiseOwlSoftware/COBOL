      *>--- conformance-case
      *> id: g85_x39
      *> rules: [1985-GR-IV.6.4.2.1.1-4, 1985-GR-IV.6.4.2.1.1.1-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.4.2.1.1 GR4 (1985-GR-IV.6.4.2.1.1-4) and
      *> §1985:IV.6.4.2.1.1.1 GR2 (1985-GR-IV.6.4.2.1.1.1-2). An output
      *> test, the accepted half: DELETE, READ, REWRITE, START and
      *> WRITE, each with INVALID KEY and with NOT INVALID KEY and
      *> delimited by its explicit scope terminator, stand as
      *> imperative-statement-1 of inline PERFORMs (dynamic access).
      *> Derived: K001 is written, its duplicate is INVALID KEY; K003
      *> is written without a phrase; K002 never exists (READ, START,
      *> REWRITE INVALID KEY); REWRITE K001 replaces D001 by NEW1,
      *> which the re-read shows; K003 is deleted once, the second
      *> DELETE is INVALID KEY. The declarative USE AFTER STANDARD
      *> ERROR procedure makes the phrase-less and NOT-only statements
      *> legal (§1985:VII.4.4.3 SR2 and its siblings require AT END or
      *> INVALID KEY otherwise); no exception occurs, so DECLARATIVE is
      *> never displayed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85X39.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XF ASSIGN TO "G85X39X"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS XKEY.
       DATA DIVISION.
       FILE SECTION.
       FD  XF LABEL RECORDS ARE STANDARD.
       01  XREC.
           05  XKEY PIC X(4).
           05  XDAT PIC X(4).
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON XF.
       D-PARA.
           DISPLAY "DECLARATIVE".
       END DECLARATIVES.
       MAIN-SEC SECTION.
       P0.
           OPEN OUTPUT XF.
           MOVE "K001" TO XKEY.
           MOVE "D001" TO XDAT.
           PERFORM
               WRITE XREC NOT INVALID KEY DISPLAY "WRITE NOT IK"
               END-WRITE
               WRITE XREC INVALID KEY DISPLAY "WRITE IK"
               END-WRITE
           END-PERFORM.
           MOVE "K003" TO XKEY.
           MOVE "D003" TO XDAT.
           WRITE XREC.
           CLOSE XF.
           OPEN I-O XF.
           PERFORM
               MOVE "K001" TO XKEY
               READ XF NOT INVALID KEY DISPLAY "READ NOT IK " XDAT
               END-READ
               MOVE "K002" TO XKEY
               READ XF INVALID KEY DISPLAY "READ IK"
               END-READ
               MOVE "K002" TO XKEY
               START XF INVALID KEY DISPLAY "START IK"
               END-START
               MOVE "K001" TO XKEY
               START XF NOT INVALID KEY DISPLAY "START NOT IK"
               END-START
               MOVE "NEW1" TO XDAT
               REWRITE XREC NOT INVALID KEY DISPLAY "REWRITE NOT IK"
               END-REWRITE
               MOVE "K002" TO XKEY
               REWRITE XREC INVALID KEY DISPLAY "REWRITE IK"
               END-REWRITE
               MOVE "K001" TO XKEY
               READ XF NOT INVALID KEY DISPLAY "REREAD " XDAT
               END-READ
               MOVE "K003" TO XKEY
               DELETE XF NOT INVALID KEY DISPLAY "DELETE NOT IK"
               END-DELETE
               DELETE XF INVALID KEY DISPLAY "DELETE IK"
               END-DELETE
           END-PERFORM.
           CLOSE XF.
           DISPLAY "END".
           STOP RUN.
