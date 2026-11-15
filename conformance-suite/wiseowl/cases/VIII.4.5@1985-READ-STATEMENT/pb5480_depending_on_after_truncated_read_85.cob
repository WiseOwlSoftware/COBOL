      *>--- conformance-case
      *> id: pb5480_depending_on_after_truncated_read_85
      *> rules: [1985-GR-VIII.4.5.4-16]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5480 - a DETERMINATION (docs/CONFORMANCE.md, the
      *> '04' entry). A record longer than the reader's RECORD
      *> VARYING maximum is "truncated on the right to the maximum
      *> size" (§1985:VIII.4.5.4 GR16), and the DEPENDING ON item
      *> "will indicate the number of character positions in the
      *> record just read" (§1985:VII.3.8.4 GR11). WiseOwl COBOL takes
      *> the record just read to be the record as delivered, so the
      *> item holds the maximum, 10, for a 20-character record written
      *> by another connector, and never a length above the reader's
      *> own RECORD clause. The leg fails if the item holds 20.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5480D.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQ ASSIGN TO "PB5480D.REL" FILE STATUS IS FSS.
           SELECT WF ASSIGN TO "PB5480D.REL"
               ORGANIZATION IS RELATIVE ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK FILE STATUS IS FSW.
           SELECT RDF ASSIGN TO "PB5480D.REL"
               ORGANIZATION IS RELATIVE ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK2 FILE STATUS IS FSR.
       DATA DIVISION.
       FILE SECTION.
       FD  SQ.
       01  SQR PIC X(4).
       FD  WF RECORD VARYING FROM 1 TO 20 DEPENDING ON LW.
       01  WR PIC X(20).
       FD  RDF RECORD VARYING FROM 5 TO 10 DEPENDING ON LR.
       01  RR PIC X(10).
       WORKING-STORAGE SECTION.
       01  FSS PIC XX.
       01  FSW PIC XX.
       01  FSR PIC XX.
       01  RK PIC 9(4) VALUE 1.
       01  RK2 PIC 9(4) VALUE 1.
       01  LW PIC 99 VALUE 20.
       01  LR PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       P.
           OPEN OUTPUT SQ. CLOSE SQ.
           OPEN INPUT RDF.
           OPEN I-O WF.
           MOVE ALL "B" TO WR. MOVE 20 TO LW. MOVE 1 TO RK.
           WRITE WR INVALID KEY DISPLAY "IKW".
           CLOSE WF.
           MOVE 1 TO RK2.
           READ RDF INVALID KEY DISPLAY "IKR".
           DISPLAY "READ " FSR " LR " LR.
           CLOSE RDF.
           STOP RUN.
