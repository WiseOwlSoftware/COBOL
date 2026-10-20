*>--- conformance-case
*> id: pb3441_dynamic_length_structure_national_member
*> rules: [GR-12.3.7.4-18, GR-12.3.7.4-19, DOC-A.1-63]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3441 (branch review R1-1) - a DYNAMIC-LENGTH NATIONAL item that is a MEMBER of a group record, and
      *> names a DYNAMIC LENGTH STRUCTURE, is framed in the record in the unit of its characters: two bytes a position
      *> (DOC-A.1-63; D-N1: UTF-16BE; docs/CONFORMANCE.md section 3 D-DL3).
      *> ISO 12.3.7.4 GR18: data described with a PREFIXED structure "is prefixed by a length field" (SHORT PREFIXED,
      *> two bytes, most significant first, DOC-A.1-205), counting character positions - so a two-position national
      *> value carries the field 0 2, and the data after it is the four bytes of its two positions.  GR19: a delimiter
      *> "shall directly follow the data", one character position of zero bits - for a national item two bytes, 0 0.
      *> The program never sees the frame (8.5.1.11.2), so the file is read back through a SECOND description of the
      *> same file (a dynamic-length record with no structure), and each byte's code (FUNCTION ORD less one) is shown.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM GR18, GR19 AND THE DETERMINATION - NOT FROM A RUN:
      *>   SHORT PREFIXED  SA N"AB" + SK "zz"   0 2 | 0 65 0 66 | 122 122            8 bytes
      *>   DELIMITED       DA N"A"  + DK "yy"   0 65 | 0 0 | 121 121                 6 bytes
      *> Read back through the typed descriptions each member returns at the length it was written with (8.5.1.10.4):
      *> SA = N"AB" (4 bytes), DA = N"A" (2 bytes), and the neighbours SK and DK are not displaced.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3441D.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DYNAMIC LENGTH STRUCTURE DS-SHORT IS SHORT PREFIXED
           DYNAMIC LENGTH STRUCTURE DS-DELIM IS DELIMITED.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-SHORT ASSIGN TO "pb3441d1.seq"
               ORGANIZATION IS SEQUENTIAL.
           SELECT F-DELIM ASSIGN TO "pb3441d2.seq"
               ORGANIZATION IS SEQUENTIAL.
           SELECT RAW-FILE ASSIGN USING RAW-NAME
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F-SHORT.
       01 R-SHORT.
          05 SA PIC N DYNAMIC LENGTH DS-SHORT LIMIT 5.
          05 SK PIC X(2).
       FD F-DELIM.
       01 R-DELIM.
          05 DA PIC N DYNAMIC LENGTH DS-DELIM LIMIT 5.
          05 DK PIC X(2).
       FD RAW-FILE.
       01 RAW PIC X DYNAMIC LENGTH.
       WORKING-STORAGE SECTION.
       01 RAW-NAME PIC X(20).
       01 I   PIC 9(4).
       01 N   PIC 999.
       01 CODE-VALUE PIC 9(4).
       01 LINE-LEN PIC 9(4).
       01 MORE PIC X VALUE "Y".
       01 L1  PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT F-SHORT F-DELIM
           MOVE N"AB" TO SA
           MOVE "zz" TO SK
           WRITE R-SHORT
           MOVE N"A" TO DA
           MOVE "yy" TO DK
           WRITE R-DELIM
           CLOSE F-SHORT F-DELIM
           MOVE "pb3441d1.seq" TO RAW-NAME
           PERFORM DUMP-RAW
           MOVE "pb3441d2.seq" TO RAW-NAME
           PERFORM DUMP-RAW
           MOVE N"QQQQ" TO SA DA
           MOVE "WW" TO SK DK
           OPEN INPUT F-SHORT F-DELIM
           READ F-SHORT
           MOVE FUNCTION BYTE-LENGTH (SA) TO L1
           DISPLAY "SHORT " L1 " " SK
           IF SA = N"AB" DISPLAY "SA-OK" ELSE DISPLAY "SA-BAD" END-IF
           READ F-DELIM
           MOVE FUNCTION BYTE-LENGTH (DA) TO L1
           DISPLAY "DELIM " L1 " " DK
           IF DA = N"A" DISPLAY "DA-OK" ELSE DISPLAY "DA-BAD" END-IF
           CLOSE F-SHORT F-DELIM
           STOP RUN.
       DUMP-RAW.
           OPEN INPUT RAW-FILE
           MOVE "Y" TO MORE
           PERFORM UNTIL MORE = "N"
               READ RAW-FILE
                   AT END MOVE "N" TO MORE
                   NOT AT END PERFORM SHOW-CODES
               END-READ
           END-PERFORM
           CLOSE RAW-FILE.
       SHOW-CODES.
           MOVE FUNCTION LENGTH(RAW) TO LINE-LEN
           DISPLAY "LEN " LINE-LEN " CODES" WITH NO ADVANCING
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > LINE-LEN
               COMPUTE CODE-VALUE = FUNCTION ORD(RAW(I:1)) - 1
               MOVE CODE-VALUE TO N
               DISPLAY " " N WITH NO ADVANCING
           END-PERFORM
           DISPLAY " ".
       END PROGRAM PB3441D.
