      *>--- conformance-case
      *> id: g2_write_same_record_area
      *> rules: [2002-GR-14.8.47.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.47.3 GR2 - the record released by a WRITE stays
      *>   available in the record area of a file named in a SAME
      *>   RECORD AREA clause, as a record of that file AND of every
      *>   other file of the same clause.
      *>   cite.py --check "§2002:14.8.47.3 GR2" "is no longer available
      *>   in the record area unless the file-name associated with
      *>   record-name-1 is specified in a SAME RECORD AREA clause" ->
      *>     OK
      *>   cite.py --check "§2002:14.8.47.3 GR2" "The logical record is
      *>   also available as a record of other files referenced in the
      *>   same SAME RECORD AREA clause" -> OK
      *> Supporting rules, each cite.py --check OK:
      *>   §2002:14.8.47.3 GR3 "MOVE literal-1 TO record-name-1" - WRITE
      *>     record-name-1 FROM is the MOVE, then the WRITE.
      *>   §2002:14.8.47.3 GR5 "where implicit-record-1 refers to the
      *>     record area for file-name-1" - WRITE FILE writes through
      *>     the
      *>     record area of file-name-1, so GR2 reaches it too.
      *>   §2002:14.8.47.3 GR6 "the information in the area referenced
      *>     by identifier-1 is available".
      *> DERIVATION. F1, F2 and F3 share one record area (SAME RECORD
      *> AREA FOR F1 F2 F3), all open OUTPUT; R1 is X(8), R2 is 4|4,
      *> R3 is 3|3|2 over the same eight positions.
      *>   W1 MOVE "ABCDEFGH" TO R1, WRITE R1: GR2, still available as
      *>      R1, as R2 and as R3       -> R1 [ABCDEFGH] R2 ABCD|EFGH
      *>                                   R3 ABC|DEF|GH
      *>   W2 WRITE R2 FROM WS-SRC ("12345678"): GR3 moves it to R2,
      *>      GR2 keeps it visible as R1 and R3; GR6 keeps WS-SRC
      *>                                -> R1 [12345678] R3 123|456|78
      *>                                   SRC 12345678
      *>   W3 WRITE R3 FROM "XY" (FROM literal-1, a 2002 form): GR3 is
      *>      MOVE "XY" TO R3, a group receiver, so "XY" and six
      *>      spaces; GR2 shows it as R1 and R2
      *>                                -> R1 [XY      ] R2 XY  |
      *>   W4 WRITE FILE F2 FROM "QRSTUVWX": GR5 writes F2's record
      *>     area,
      *>      a file of the clause, so GR2 shows it as R1 and R3
      *>                                -> R1 [QRSTUVWX] R3 QRS|TUV|WX
      *> Each leg fails if the WRITE clears or reallocates the shared
      *> area, if the peers do not alias one storage area, or if the
      *> FROM / FILE paths move through a private buffer.
      *> The "no longer available" half of GR2 (a file not in the
      *> clause) leaves the area undefined and is not pinned.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2WSRA01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "g2wsra1.dat".
           SELECT F2 ASSIGN TO "g2wsra2.dat".
           SELECT F3 ASSIGN TO "g2wsra3.dat".
       I-O-CONTROL.
           SAME RECORD AREA FOR F1 F2 F3.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(8).
       FD F2.
       01 R2.
           05 R2-A PIC X(4).
           05 R2-B PIC X(4).
       FD F3.
       01 R3.
           05 R3-A PIC X(3).
           05 R3-B PIC X(3).
           05 R3-C PIC X(2).
       WORKING-STORAGE SECTION.
       01 WS-SRC PIC X(8) VALUE "12345678".
       PROCEDURE DIVISION.
       M1.
           OPEN OUTPUT F1 F2 F3
           MOVE "ABCDEFGH" TO R1
           WRITE R1
           DISPLAY "W1 R1 [" R1 "]"
           DISPLAY "W1 R2 " R2-A "|" R2-B
           DISPLAY "W1 R3 " R3-A "|" R3-B "|" R3-C
           WRITE R2 FROM WS-SRC
           DISPLAY "W2 R1 [" R1 "]"
           DISPLAY "W2 R3 " R3-A "|" R3-B "|" R3-C
           DISPLAY "W2 SRC " WS-SRC
           WRITE R3 FROM "XY"
           DISPLAY "W3 R1 [" R1 "]"
           DISPLAY "W3 R2 " R2-A "|" R2-B "|"
           WRITE FILE F2 FROM "QRSTUVWX"
           DISPLAY "W4 R1 [" R1 "]"
           DISPLAY "W4 R3 " R3-A "|" R3-B "|" R3-C
           CLOSE F1 F2 F3
           STOP RUN.
