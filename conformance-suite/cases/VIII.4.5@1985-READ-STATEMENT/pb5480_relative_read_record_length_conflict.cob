      *>--- conformance-case
      *> id: pb5480_relative_read_record_length_conflict
      *> rules: [1985-GR-VIII.4.5.4-16]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5480. §1985:VIII.4.5.4 GR16: "In either of these
      *> cases, the READ statement is successful and an I-O status is
      *> set indicating a record length conflict has occurred", which
      *> §1985:VIII.1.3.4 GRL2.1 defines as status 04: "does not
      *> conform to the fixed file attributes for that file". The
      *> minimum and maximum logical record size is a fixed attribute
      *> (§1985:II.2.1) and the reader's is its RECORD clause, 5 TO 10.
      *> A reader is opened INPUT on a zero-byte file, which holds no
      *> attribute to validate, and a second connector then writes a
      *> 3-, a 20- and a 7-character record. The 3-character record is
      *> below the reader's minimum and the 20-character one above its
      *> maximum (truncated on the right to 10): status 04 each, by
      *> random READ and by READ NEXT. The 7-character record is inside
      *> it: status 00. Each leg fails if the READ reports 00 for a
      *> record outside 5 TO 10, or 04 for the one inside. The record
      *> area right of a short record is undefined, so only the
      *> characters read are displayed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5480R.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQ ASSIGN TO "PB5480R.REL" FILE STATUS IS FSS.
           SELECT WF ASSIGN TO "PB5480R.REL"
               ORGANIZATION IS RELATIVE ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK FILE STATUS IS FSW.
           SELECT RDF ASSIGN TO "PB5480R.REL"
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
       01  LW PIC 99 VALUE 3.
       01  LR PIC 99.
       PROCEDURE DIVISION.
       P.
           OPEN OUTPUT SQ. CLOSE SQ.
           OPEN INPUT RDF. DISPLAY "R OPEN " FSR.
           OPEN I-O WF.
           MOVE ALL "A" TO WR. MOVE 3 TO LW. MOVE 1 TO RK.
           WRITE WR INVALID KEY DISPLAY "IKW1".
           MOVE ALL "B" TO WR. MOVE 20 TO LW. MOVE 2 TO RK.
           WRITE WR INVALID KEY DISPLAY "IKW2".
           MOVE ALL "C" TO WR. MOVE 7 TO LW. MOVE 3 TO RK.
           WRITE WR INVALID KEY DISPLAY "IKW3".
           CLOSE WF.
           MOVE 1 TO RK2.
           READ RDF INVALID KEY DISPLAY "IKR".
           DISPLAY "RANDOM 1 " FSR " [" RR(1:3) "]".
           MOVE 2 TO RK2.
           READ RDF INVALID KEY DISPLAY "IKR".
           DISPLAY "RANDOM 2 " FSR " [" RR "]".
           MOVE 3 TO RK2.
           READ RDF INVALID KEY DISPLAY "IKR".
           DISPLAY "RANDOM 3 " FSR " [" RR(1:7) "]".
           MOVE 1 TO RK2.
           START RDF KEY IS EQUAL TO RK2 INVALID KEY DISPLAY "IKS".
           READ RDF NEXT AT END DISPLAY "END".
           DISPLAY "NEXT 1 " FSR " [" RR(1:3) "]".
           READ RDF NEXT AT END DISPLAY "END".
           DISPLAY "NEXT 2 " FSR " [" RR "]".
           READ RDF NEXT AT END DISPLAY "END".
           DISPLAY "NEXT 3 " FSR " [" RR(1:7) "]".
           CLOSE RDF.
           STOP RUN.
