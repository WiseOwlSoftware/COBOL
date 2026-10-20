*>--- conformance-case
*> id: pb3441_dynamic_national_member_record
*> rules: [DOC-A.1-63]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3441 - a RECORD holding a DYNAMIC-LENGTH NATIONAL item round-trips through a sequential file, with its
      *> member lengths and with characters past ISO 8859-1, in both forms of the record (determinations D-FRA (v) and
      *> (vi), docs/CONFORMANCE.md section 3).
      *>
      *> ISO 1989:2023 8.5.1.11.2: a WRITE sends a variable-length record "as though it were in fact contiguous with its
      *> neighbors", and DOC-A.1-63 makes the record image of a national member its bytes - two a character position,
      *> UTF-16BE (13.18.60.4 GR8; D-N1) - so a character whose code point does not fit one byte (U+4E2D) is sent whole.
      *> Each member comes back at the length it was written with (8.5.1.10.4: "the new value becomes the content of the
      *> item"). In a file of FIXED-LENGTH records (13.18.43.4 GR6: Format 1 "is used to specify fixed-length records") each member is padded with
      *> SPACES to its maximum size and read back without them (D-FRA (vi); 14.9.51.4 GR21 drops a LINE SEQUENTIAL
      *> record's trailing spaces by the same reading), and the space of a NATIONAL member is the NATIONAL space, the pair
      *> 00 20: U+2020 - the pair 20 20 - is a character of the data, and is not padding.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE DETERMINATION - NOT FROM A RUN (QA and QC are national, a position is two
      *> bytes; NX"4E2D" is one position, NX"2020" one position, NX"006100620063" three):
      *>   Q1  QA U+4E2D (2 bytes) + QK "KEY" + QC U+2020 (2 bytes)        2 2 KEY, both members equal what was written
      *>   Q2  QA "abc" (6 bytes) + QK "K2 " + QC empty                    6 0 K2
      *>   F1  FA U+2020 (2) + FK "XY" + FC "Z" (1)                         2 XY 1, FA equal U+2020: the data is not padding
      *>   F2  FA "a" (2) + FK "XY" + FC empty                              2 XY 0
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3441C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT QF ASSIGN TO "pb3441.seq"
               ORGANIZATION IS SEQUENTIAL.
           SELECT FF ASSIGN TO "pb3441.fix"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD QF.
       01 QR.
          05 QA PIC N DYNAMIC LENGTH LIMIT IS 5.
          05 QK PIC X(3).
          05 QC PIC N DYNAMIC LENGTH LIMIT IS 5.
       FD FF RECORD CONTAINS 20 CHARACTERS.
       01 FR.
          05 FA PIC N DYNAMIC LENGTH LIMIT IS 5.
          05 FK PIC X(2).
          05 FC PIC X DYNAMIC LENGTH LIMIT IS 4.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT QF
           MOVE NX"4E2D" TO QA
           MOVE "KEY" TO QK
           MOVE NX"2020" TO QC
           WRITE QR
           MOVE NX"006100620063" TO QA
           MOVE "K2 " TO QK
           MOVE N"" TO QC
           WRITE QR
           CLOSE QF
           OPEN OUTPUT FF
           MOVE NX"2020" TO FA
           MOVE "XY" TO FK
           MOVE "Z" TO FC
           WRITE FR
           MOVE NX"0061" TO FA
           MOVE "XY" TO FK
           MOVE "" TO FC
           WRITE FR
           CLOSE FF
           MOVE N"zzzzz" TO QA QC
           OPEN INPUT QF
           READ QF
           DISPLAY "Q1 " FUNCTION BYTE-LENGTH (QA) " " FUNCTION BYTE-LENGTH (QC) " " QK
           IF QA = NX"4E2D" DISPLAY "QA-OK" ELSE DISPLAY "QA-BAD" END-IF
           IF QC = NX"2020" DISPLAY "QC-OK" ELSE DISPLAY "QC-BAD" END-IF
           READ QF
           DISPLAY "Q2 " FUNCTION BYTE-LENGTH (QA) " " FUNCTION BYTE-LENGTH (QC) " " QK
           IF QA = NX"006100620063" DISPLAY "QA-OK" ELSE DISPLAY "QA-BAD" END-IF
           CLOSE QF
           MOVE N"zzzzz" TO FA
           MOVE "WWWW" TO FC
           OPEN INPUT FF
           READ FF
           DISPLAY "F1 " FUNCTION BYTE-LENGTH (FA) " " FK " " FUNCTION BYTE-LENGTH (FC)
           IF FA = NX"2020" DISPLAY "FA-OK" ELSE DISPLAY "FA-BAD" END-IF
           IF FC = "Z" DISPLAY "FC-OK" ELSE DISPLAY "FC-BAD" END-IF
           READ FF
           DISPLAY "F2 " FUNCTION BYTE-LENGTH (FA) " " FK " " FUNCTION BYTE-LENGTH (FC)
           IF FA = NX"0061" DISPLAY "FA-OK" ELSE DISPLAY "FA-BAD" END-IF
           CLOSE FF
           STOP RUN.
       END PROGRAM PB3441C.
