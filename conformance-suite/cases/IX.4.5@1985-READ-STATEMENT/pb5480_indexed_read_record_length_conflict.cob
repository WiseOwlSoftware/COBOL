      *>--- conformance-case
      *> id: pb5480_indexed_read_record_length_conflict
      *> rules: [1985-GR-IX.4.5.4-18]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5480. §1985:IX.4.5.4 GR18: "In either of these
      *> cases, the READ statement is successful and an I-O status is
      *> set indicating a record length conflict has occurred", which
      *> §1985:IX.1.3.4 GRL2.1 defines as status 04: "does not conform
      *> to the fixed file attributes for that file". The minimum and
      *> maximum logical record size is a fixed attribute
      *> (§1985:II.2.1) and the reader's is its RECORD clause, 5 TO 10.
      *> A reader is opened INPUT on a zero-byte file, which holds no
      *> attribute to validate, and a second connector then writes a
      *> 4-, a 20- and a 7-character record. The first is below the
      *> reader's minimum and the second above its maximum (truncated
      *> on the right to 10): status 04 each, by random READ and by
      *> READ NEXT. The 7-character record is inside it: status 00.
      *> Each leg fails if the READ reports 00 for a record outside
      *> 5 TO 10, or 04 for the one inside. The record area right of a
      *> short record is undefined, so only the characters read are
      *> displayed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5480I.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQ ASSIGN TO "PB5480I.DAT" FILE STATUS IS FSS.
           SELECT WF ASSIGN TO "PB5480I.DAT"
               ORGANIZATION IS INDEXED ACCESS MODE IS DYNAMIC
               RECORD KEY IS WK FILE STATUS IS FSW.
           SELECT RDF ASSIGN TO "PB5480I.DAT"
               ORGANIZATION IS INDEXED ACCESS MODE IS DYNAMIC
               RECORD KEY IS RK FILE STATUS IS FSR.
       DATA DIVISION.
       FILE SECTION.
       FD  SQ.
       01  SQR PIC X(4).
       FD  WF RECORD VARYING FROM 4 TO 20 DEPENDING ON LW.
       01  WR.
           02 WK PIC X(4).
           02 WD PIC X(16).
       FD  RDF RECORD VARYING FROM 5 TO 10 DEPENDING ON LR.
       01  RR.
           02 RK PIC X(4).
           02 RDT PIC X(6).
       WORKING-STORAGE SECTION.
       01  FSS PIC XX.
       01  FSW PIC XX.
       01  FSR PIC XX.
       01  LW PIC 99.
       01  LR PIC 99.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D SECTION. USE AFTER STANDARD EXCEPTION PROCEDURE ON WF RDF.
       D1. CONTINUE.
       END DECLARATIVES.
       M SECTION.
       P.
           OPEN OUTPUT SQ. CLOSE SQ.
           OPEN INPUT RDF. DISPLAY "R OPEN " FSR.
           OPEN I-O WF.
           MOVE ALL "A" TO WR. MOVE "K001" TO WK. MOVE 4 TO LW.
           WRITE WR.
           MOVE ALL "B" TO WR. MOVE "K002" TO WK. MOVE 20 TO LW.
           WRITE WR.
           MOVE ALL "C" TO WR. MOVE "K003" TO WK. MOVE 7 TO LW.
           WRITE WR.
           CLOSE WF.
           MOVE "K001" TO RK.
           READ RDF.
           DISPLAY "RANDOM 1 " FSR " [" RR(1:4) "]".
           MOVE "K002" TO RK.
           READ RDF.
           DISPLAY "RANDOM 2 " FSR " [" RR "]".
           MOVE "K003" TO RK.
           READ RDF.
           DISPLAY "RANDOM 3 " FSR " [" RR(1:7) "]".
           MOVE "K001" TO RK.
           START RDF KEY IS EQUAL TO RK.
           READ RDF NEXT.
           DISPLAY "NEXT 1 " FSR " [" RR(1:4) "]".
           READ RDF NEXT.
           DISPLAY "NEXT 2 " FSR " [" RR "]".
           READ RDF NEXT.
           DISPLAY "NEXT 3 " FSR " [" RR(1:7) "]".
           CLOSE RDF.
           STOP RUN.
