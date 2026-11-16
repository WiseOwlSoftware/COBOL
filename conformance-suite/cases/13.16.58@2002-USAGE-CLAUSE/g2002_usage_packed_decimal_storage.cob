      *>--- conformance-case
      *> id: g2002_usage_packed_decimal_storage
      *> rules: [2002-GR-13.16.58.3-10]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.58.3 GR10 - USAGE PACKED-DECIMAL uses "a radix of
      *> 10", "each digit position shall occupy the minimum possible
      *> configuration in computer storage", the implementor specifies
      *> the representation "including the representation of any
      *> algebraic sign", and "Sufficient computer storage shall be
      *> allocated by the implementor to contain the maximum range of
      *> values implied by the associated decimal picture
      *> character-string". The implementor's specification
      *> (DOC-A.1-215): binary-coded decimal, one nibble per digit,
      *> most significant first, a trailing sign nibble C (+), D (-)
      *> or F (no operational sign), digits/2+1 bytes. The items stand
      *> in working storage (group WP, shown through WRAW REDEFINES
      *> WP) and in the record of an output CD (KR, shown through KRAW,
      *> a second record that implicitly redefines the same area,
      *> §2002:A.1.2.2 SR9).
      *>  LEN: 9 -> 1, 9(2) -> 2, 9(3) -> 2, S9(4) -> 3, 9(5) -> 3,
      *>   S9(18) -> 10, S9(31) -> 16 bytes (FUNCTION BYTE-LENGTH).
      *>  MAX: each item holds the largest magnitude its picture implies
      *>   (9, 99, 999, -9999, 99999, eighteen and thirty-one 9s).
      *>  IMG: radix 10 per nibble - C = 123 is X"123F" = 018 063 and
      *>   D = -1234 is X"01234D" = 001 035 077 (bytes 4-5 and 6-8,
      *>   shown as FUNCTION ORD - 1 in the native byte order).
      *> A line fails if an item takes more or less storage than one
      *> nibble per digit plus the sign, truncates its maximum, or is
      *> held in another radix.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2UA06.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  L   PIC 99.
       01  O1  PIC 999.
       01  O2  PIC 999.
       01  O3  PIC 999.
       01  O4  PIC 999.
       01  O5  PIC 999.
       01  WP.
           05 WA PIC 9 USAGE PACKED-DECIMAL.
           05 WB PIC 9(2) USAGE PACKED-DECIMAL.
           05 WC PIC 9(3) USAGE PACKED-DECIMAL.
           05 WD PIC S9(4) USAGE PACKED-DECIMAL.
           05 WE PIC 9(5) USAGE PACKED-DECIMAL.
           05 WF PIC S9(18) USAGE PACKED-DECIMAL.
           05 WG PIC S9(31) USAGE PACKED-DECIMAL.
       01  WRAW REDEFINES WP PIC X(37).
       COMMUNICATION SECTION.
       CD  CO FOR OUTPUT.
       01  KR.
           05 KA PIC 9 USAGE PACKED-DECIMAL.
           05 KB PIC 9(2) USAGE PACKED-DECIMAL.
           05 KC PIC 9(3) USAGE PACKED-DECIMAL.
           05 KD PIC S9(4) USAGE PACKED-DECIMAL.
           05 KE PIC 9(5) USAGE PACKED-DECIMAL.
           05 KF PIC S9(18) USAGE PACKED-DECIMAL.
           05 KG PIC S9(31) USAGE PACKED-DECIMAL.
       01  KRAW PIC X(37).
       PROCEDURE DIVISION.
       MAIN.
           MOVE 9 TO WA
           MOVE FUNCTION BYTE-LENGTH(WA) TO L
           IF WA = 9
               DISPLAY "WS A LEN=" L " MAX OK"
           ELSE
               DISPLAY "WS A LEN=" L " MAX BAD"
           END-IF
           MOVE 99 TO WB
           MOVE FUNCTION BYTE-LENGTH(WB) TO L
           IF WB = 99
               DISPLAY "WS B LEN=" L " MAX OK"
           ELSE
               DISPLAY "WS B LEN=" L " MAX BAD"
           END-IF
           MOVE 999 TO WC
           MOVE FUNCTION BYTE-LENGTH(WC) TO L
           IF WC = 999
               DISPLAY "WS C LEN=" L " MAX OK"
           ELSE
               DISPLAY "WS C LEN=" L " MAX BAD"
           END-IF
           MOVE -9999 TO WD
           MOVE FUNCTION BYTE-LENGTH(WD) TO L
           IF WD = -9999
               DISPLAY "WS D LEN=" L " MAX OK"
           ELSE
               DISPLAY "WS D LEN=" L " MAX BAD"
           END-IF
           MOVE 99999 TO WE
           MOVE FUNCTION BYTE-LENGTH(WE) TO L
           IF WE = 99999
               DISPLAY "WS E LEN=" L " MAX OK"
           ELSE
               DISPLAY "WS E LEN=" L " MAX BAD"
           END-IF
           MOVE -999999999999999999 TO WF
           MOVE FUNCTION BYTE-LENGTH(WF) TO L
           IF WF = -999999999999999999
               DISPLAY "WS F LEN=" L " MAX OK"
           ELSE
               DISPLAY "WS F LEN=" L " MAX BAD"
           END-IF
           MOVE -9999999999999999999999999999999 TO WG
           MOVE FUNCTION BYTE-LENGTH(WG) TO L
           IF WG = -9999999999999999999999999999999
               DISPLAY "WS G LEN=" L " MAX OK"
           ELSE
               DISPLAY "WS G LEN=" L " MAX BAD"
           END-IF
           MOVE 123 TO WC
           MOVE -1234 TO WD
           COMPUTE O1 = FUNCTION ORD(WRAW(4:1)) - 1
           COMPUTE O2 = FUNCTION ORD(WRAW(5:1)) - 1
           COMPUTE O3 = FUNCTION ORD(WRAW(6:1)) - 1
           COMPUTE O4 = FUNCTION ORD(WRAW(7:1)) - 1
           COMPUTE O5 = FUNCTION ORD(WRAW(8:1)) - 1
           DISPLAY "WS IMG C=" O1 " " O2 " D=" O3 " " O4 " " O5
           MOVE 9 TO KA
           MOVE FUNCTION BYTE-LENGTH(KA) TO L
           IF KA = 9
               DISPLAY "CD A LEN=" L " MAX OK"
           ELSE
               DISPLAY "CD A LEN=" L " MAX BAD"
           END-IF
           MOVE 99 TO KB
           MOVE FUNCTION BYTE-LENGTH(KB) TO L
           IF KB = 99
               DISPLAY "CD B LEN=" L " MAX OK"
           ELSE
               DISPLAY "CD B LEN=" L " MAX BAD"
           END-IF
           MOVE 999 TO KC
           MOVE FUNCTION BYTE-LENGTH(KC) TO L
           IF KC = 999
               DISPLAY "CD C LEN=" L " MAX OK"
           ELSE
               DISPLAY "CD C LEN=" L " MAX BAD"
           END-IF
           MOVE -9999 TO KD
           MOVE FUNCTION BYTE-LENGTH(KD) TO L
           IF KD = -9999
               DISPLAY "CD D LEN=" L " MAX OK"
           ELSE
               DISPLAY "CD D LEN=" L " MAX BAD"
           END-IF
           MOVE 99999 TO KE
           MOVE FUNCTION BYTE-LENGTH(KE) TO L
           IF KE = 99999
               DISPLAY "CD E LEN=" L " MAX OK"
           ELSE
               DISPLAY "CD E LEN=" L " MAX BAD"
           END-IF
           MOVE -999999999999999999 TO KF
           MOVE FUNCTION BYTE-LENGTH(KF) TO L
           IF KF = -999999999999999999
               DISPLAY "CD F LEN=" L " MAX OK"
           ELSE
               DISPLAY "CD F LEN=" L " MAX BAD"
           END-IF
           MOVE -9999999999999999999999999999999 TO KG
           MOVE FUNCTION BYTE-LENGTH(KG) TO L
           IF KG = -9999999999999999999999999999999
               DISPLAY "CD G LEN=" L " MAX OK"
           ELSE
               DISPLAY "CD G LEN=" L " MAX BAD"
           END-IF
           MOVE 123 TO KC
           MOVE -1234 TO KD
           COMPUTE O1 = FUNCTION ORD(KRAW(4:1)) - 1
           COMPUTE O2 = FUNCTION ORD(KRAW(5:1)) - 1
           COMPUTE O3 = FUNCTION ORD(KRAW(6:1)) - 1
           COMPUTE O4 = FUNCTION ORD(KRAW(7:1)) - 1
           COMPUTE O5 = FUNCTION ORD(KRAW(8:1)) - 1
           DISPLAY "CD IMG C=" O1 " " O2 " D=" O3 " " O4 " " O5
           STOP RUN.
