      *>--- conformance-case
      *> id: pb5480_indexed_read_record_length_conflict
      *> rules: [GR-9.1.13.2-3]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5480, PB6161. Status 04: "A READ statement is
      *> successfully executed but the physical record from the file is
      *> shorter than or longer than the minimum or maximum length of
      *> records allowed for the fixed file attributes for that file"
      *> (§2023:9.1.13.2 GR3); 2002: "the READ statement is successful
      *> and an I-O status is set indicating a record length conflict
      *> has occurred" (§2002:14.8.29.3 GR13). The reader's minimum and
      *> maximum record size is its RECORD clause, 5 TO 10
      *> (docs/conformance D-IO-STATUS-04). The reader opens INPUT on a
      *> zero-byte file, which holds no attribute to validate, and a
      *> second connector opens the same file I-O and writes records
      *> outside that size. Every SELECT is SHARING WITH ALL OTHER, so
      *> the sharing facility admits the second OPEN (§2023:9.1.15); at
      *> COBOL-85 the same OPEN is an OPEN of a file in the open mode,
      *> 41 (kb/Work PB6161), so this route exists from 2002 only.
      *> A 4-, a 20- and a 7-character record: the first two are
      *> outside 5 TO 10 (the long one truncated on the right to 10),
      *> status 04 each, by random READ and by READ NEXT; the third is
      *> inside, 00. The record area right of a short record is
      *> undefined, so only the characters read are displayed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5480I.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQ ASSIGN TO "PB5480I.DAT" FILE STATUS IS FSS
               LOCK MODE IS MANUAL
               SHARING WITH ALL OTHER.
           SELECT WF ASSIGN TO "PB5480I.DAT"
               ORGANIZATION IS INDEXED ACCESS MODE IS DYNAMIC
               RECORD KEY IS WK FILE STATUS IS FSW
               LOCK MODE IS MANUAL
               SHARING WITH ALL OTHER.
           SELECT RDF ASSIGN TO "PB5480I.DAT"
               ORGANIZATION IS INDEXED ACCESS MODE IS DYNAMIC
               RECORD KEY IS RK FILE STATUS IS FSR
               LOCK MODE IS MANUAL
               SHARING WITH ALL OTHER.
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
