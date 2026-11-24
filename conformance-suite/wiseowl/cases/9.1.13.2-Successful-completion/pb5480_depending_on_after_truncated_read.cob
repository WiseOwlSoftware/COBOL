      *>--- conformance-case
      *> id: pb5480_depending_on_after_truncated_read
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
      *> A DETERMINATION (docs/conformance D-IO-STATUS-04): the
      *> RECORD VARYING DEPENDING ON item holds the length of the record
      *> as delivered, so for a 20-character record read by a reader
      *> whose maximum is 10 it holds 10, never 20. The leg fails if the
      *> READ answers 00 or the item holds 20.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5480D.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQ ASSIGN TO "PB5480D.REL" FILE STATUS IS FSS
               LOCK MODE IS MANUAL
               SHARING WITH ALL OTHER.
           SELECT WF ASSIGN TO "PB5480D.REL"
               ORGANIZATION IS RELATIVE ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK FILE STATUS IS FSW
               LOCK MODE IS MANUAL
               SHARING WITH ALL OTHER.
           SELECT RDF ASSIGN TO "PB5480D.REL"
               ORGANIZATION IS RELATIVE ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK2 FILE STATUS IS FSR
               LOCK MODE IS MANUAL
               SHARING WITH ALL OTHER.
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
