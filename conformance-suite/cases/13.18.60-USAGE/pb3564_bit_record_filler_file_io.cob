*>--- conformance-case
*> id: pb3564_bit_record_filler_file_io
*> rules: [GR-13.18.60.4-5, GR-9.1.13.4-1]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3564 - THE IMPLICIT FILLER BITS OF A RECORD GROUP TRAVEL WITH THE RECORD.
      *> ISO 8.5.1.6.3: after a bit item in an alphanumeric group the bits that complete the byte "are implicitly
      *> described as a filler elementary bit data item", so a record group that holds a one-bit item and a character
      *> holds seven filler bits between them.  ISO 13.18.60.4 GR5: the bit item occupies bits of the record area.  A
      *> READ makes the record "accessible" in the record area, and a WRITE sends the record area, so the bytes of
      *> the record are the bytes of the group - filler bits included - and a record read from a file and written
      *> back is the same bytes (14.9.25.4 GR4: the group is moved as an alphanumeric item).
      *>
      *> HOW EACH LEG CAN FAIL.  The records below are written and read through a SECOND connector on the same
      *> physical file whose record is plain PIC X(2), so the expected number is the ORD of a byte computed from the
      *> bits by hand.  A filler without storage reads back as zero bits: READ ... INTO a working-storage group and
      *> WRITE ... FROM it would then write 0x80 / 0x40 where the file held 0xC1 / 0x7E, and a REWRITE of a record
      *> would lose the filler of the record it replaced the same way.  The abnormal paths share the codec: AT END
      *> after the last record (status 10), INVALID KEY on an absent relative record (status 23), and the status 35
      *> of an OPEN INPUT of a file that does not exist (9.1.13.6 5: a permanent error condition).
      *>
      *> DERIVATIONS (ORD of a byte is its value + 1):
      *>   R1  X"C142": 0xC1 = 1100 0001 -> GB = 1, filler 1000001; "B".   Copied through the group: ORD 194, 67.
      *>   R2  X"7E41": 0x7E = 0111 1110 -> GB = 0, filler 1111110; "A".   Copied: ORD 127, 66.
      *>   REL record 1 X"C142" rewritten as X"7E41": the filler of the new record is 1111110: ORD 127, 66.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3564IO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FRAW ASSIGN TO "pb3564_in.dat" ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-A.
           SELECT FGRP ASSIGN TO "pb3564_in.dat" ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-B.
           SELECT FOUT ASSIGN TO "pb3564_out.dat" ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-C.
           SELECT FOUTX ASSIGN TO "pb3564_out.dat" ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-A.
           SELECT FMISS ASSIGN TO "pb3564_missing.dat" ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-D.
           SELECT FREL ASSIGN TO "pb3564_rel.dat" ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC RELATIVE KEY IS RK FILE STATUS IS ST-E.
       DATA DIVISION.
       FILE SECTION.
       FD FRAW.
       01 RAW-REC PIC X(2).
       FD FGRP.
       01 GRP-REC.
          05 GRP-B PIC 1 USAGE BIT.
          05 GRP-X PIC X.
       FD FOUT.
       01 OUT-REC.
          05 OUT-B PIC 1 USAGE BIT.
          05 OUT-X PIC X.
       FD FOUTX.
       01 OUTX-REC PIC X(2).
       FD FMISS.
       01 MISS-REC.
          05 MISS-B PIC 1 USAGE BIT.
          05 MISS-X PIC X.
       FD FREL.
       01 REL-REC.
          05 REL-B PIC 1 USAGE BIT.
          05 REL-X PIC X.
       WORKING-STORAGE SECTION.
       01 ST-A PIC XX.
       01 ST-B PIC XX.
       01 ST-C PIC XX.
       01 ST-D PIC XX.
       01 ST-E PIC XX.
       01 RK PIC 9(2).
       01 WS-G.
          05 WS-B PIC 1 USAGE BIT.
          05 WS-X PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
      *> ===== S1 - sequential: write two records as bytes, read them as the group, copy them through a WS group =====
           OPEN OUTPUT FRAW
           MOVE X"C142" TO RAW-REC
           WRITE RAW-REC
           MOVE X"7E41" TO RAW-REC
           WRITE RAW-REC
           CLOSE FRAW
           DISPLAY "S1-W=" ST-A
           OPEN INPUT FGRP
           OPEN OUTPUT FOUT
           READ FGRP INTO WS-G
           DISPLAY "S1-R1=" ST-B " B=[" WS-B "] X=[" WS-X "] ORD=" FUNCTION ORD(WS-G(1:1))
           WRITE OUT-REC FROM WS-G
           READ FGRP
           DISPLAY "S1-R2=" ST-B " B=[" GRP-B "] X=[" GRP-X "] ORD=" FUNCTION ORD(GRP-REC(1:1))
           MOVE GRP-REC TO OUT-REC
           WRITE OUT-REC
           READ FGRP AT END DISPLAY "S1-R3=" ST-B END-READ
           CLOSE FGRP
           CLOSE FOUT
           DISPLAY "S1-C=" ST-C
           OPEN INPUT FOUTX
           READ FOUTX
           DISPLAY "S1-O1=" ST-A " " FUNCTION ORD(OUTX-REC(1:1)) "," FUNCTION ORD(OUTX-REC(2:1))
           READ FOUTX
           DISPLAY "S1-O2=" ST-A " " FUNCTION ORD(OUTX-REC(1:1)) "," FUNCTION ORD(OUTX-REC(2:1))
           READ FOUTX AT END DISPLAY "S1-O3=" ST-A END-READ
           CLOSE FOUTX
      *> ===== S2 - the status of an OPEN INPUT of a file that does not exist =====
           OPEN INPUT FMISS
           DISPLAY "S2-OPEN=" ST-D
      *> ===== S3 - relative: write, read back, rewrite, read back, and an absent record =====
           OPEN OUTPUT FREL
           MOVE 1 TO RK
           MOVE X"C142" TO REL-REC
           WRITE REL-REC
           DISPLAY "S3-W=" ST-E
           CLOSE FREL
           OPEN I-O FREL
           MOVE 1 TO RK
           READ FREL
           DISPLAY "S3-R1=" ST-E " B=[" REL-B "] X=[" REL-X "] ORD=" FUNCTION ORD(REL-REC(1:1))
           MOVE X"7E41" TO REL-REC
           REWRITE REL-REC
           DISPLAY "S3-RW=" ST-E
           MOVE X"4040" TO REL-REC
           MOVE 1 TO RK
           READ FREL
           DISPLAY "S3-R2=" ST-E " B=[" REL-B "] X=[" REL-X "] ORD=" FUNCTION ORD(REL-REC(1:1))
           MOVE 2 TO RK
           READ FREL INVALID KEY DISPLAY "S3-R3=" ST-E END-READ
           CLOSE FREL
           STOP RUN.
