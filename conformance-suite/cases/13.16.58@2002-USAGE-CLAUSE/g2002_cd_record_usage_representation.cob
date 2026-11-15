      *>--- conformance-case
      *> id: g2002_cd_record_usage_representation
      *> rules: [2002-SR-13.16.58.2-12, 2002-GR-13.16.58.3-2, 2002-GR-13.16.58.3-4, 2002-GR-13.16.58.3-6, 2002-GR-13.16.58.3-7, 2002-GR-13.16.58.3-8, 2002-GR-13.16.58.3-9]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> At 2002 the record description entries after a CD entry are
      *> ordinary data description entries (§2002:A.1.1.2), so the USAGE
      *> clause governs them as it governs working storage. Record R
      *> below sits under an OUTPUT CD; RAW is a second record under the
      *> same CD and so implicitly redefines the same area (§2002:A.1.2.2
      *> SR9), which lets the program show the bytes each usage stores.
      *> Each image byte is shown as FUNCTION ORD - 1 (§2002:15.57: the
      *> ordinal position in the program collating sequence, lowest 1;
      *> no PROGRAM COLLATING SEQUENCE is named, so it is the native
      *> byte order).
      *>
      *> §2002:13.16.58.3 GR4 - B PIC S9(4) BINARY: radix 2; the
      *>   implementor's representation (DOC-A.1-205) is two's complement,
      *>   most significant byte first, 2 bytes for 3-4 digits, so -1234
      *>   (65536-1234 = 64302 = X"FB2E") is 251 046.
      *> §2002:13.16.58.3 GR6 - C PIC S9(4) COMP: format specified by the
      *>   implementor, identical to BINARY (DOC-A.1-208): 251 046.
      *> §2002:13.16.58.2 SR12 b) and GR7 - D PIC X(3), Z PIC 9(3): no
      *>   USAGE anywhere above them, so DISPLAY is implied; one byte
      *>   per character (DOC-A.1-209): lengths 3, and the area holds the
      *>   characters XYZ and 123 themselves.
      *> §2002:13.16.58.2 SR12 a) and GR8 - N PIC N(2): no USAGE, the
      *>   picture has 'N', so NATIONAL is implied; a national character
      *>   is a multiple of the alphanumeric size, 2 bytes UTF-16BE in
      *>   this implementation (DOC-A.1-213): length 4, N"QR" is
      *>   000 081 000 082. An implied DISPLAY would give length 2.
      *> §2002:13.16.58.3 GR9 - IX USAGE INDEX holds the occurrence
      *>   number: SET IX TO X1 (4), SET X1 TO 1, SET X1 TO IX selects
      *>   TE(4) = "D"; DOC-A.1-211 images it as an 8-byte big-endian
      *>   integer, so its last byte is 004.
      *> §2002:13.16.58.3 GR2 - the usage changes only representation,
      *>   not use: B + C + PD computes -1234 - 1234 - 12345 = -14813,
      *>   and B = C compares the two numeric values (equal).
      *> Each leg fails if the item takes another usage, width, byte
      *> order or value in the CD record area.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2USG01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T.
           05 TE PIC X OCCURS 5 INDEXED BY X1.
       01  K   PIC S9(5) SIGN IS LEADING SEPARATE.
       01  L   PIC 99.
       01  O1  PIC 999.
       01  O2  PIC 999.
       01  O3  PIC 999.
       01  O4  PIC 999.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           TEXT LENGTH IS TL
           STATUS KEY IS SK.
       01  R.
           05 B   PIC S9(4) BINARY.
           05 C   PIC S9(4) COMP.
           05 PD  PIC S9(5) PACKED-DECIMAL.
           05 D   PIC X(3).
           05 N   PIC N(2).
           05 Z   PIC 9(3).
           05 IX  USAGE INDEX.
       01  RAW PIC X(25).
       PROCEDURE DIVISION.
       MAIN.
           MOVE "ABCDE" TO T
           MOVE -1234 TO B
           MOVE -1234 TO C
           MOVE -12345 TO PD
           MOVE "XYZ" TO D
           MOVE N"QR" TO N
           MOVE 123 TO Z
           SET X1 TO 4
           SET IX TO X1
           SET X1 TO 1
           SET X1 TO IX
           MOVE FUNCTION BYTE-LENGTH(B) TO L
           COMPUTE O1 = FUNCTION ORD(RAW(1:1)) - 1
           COMPUTE O2 = FUNCTION ORD(RAW(2:1)) - 1
           DISPLAY "B LEN=" L " IMG=" O1 " " O2
           MOVE FUNCTION BYTE-LENGTH(C) TO L
           COMPUTE O1 = FUNCTION ORD(RAW(3:1)) - 1
           COMPUTE O2 = FUNCTION ORD(RAW(4:1)) - 1
           DISPLAY "C LEN=" L " IMG=" O1 " " O2
           MOVE FUNCTION BYTE-LENGTH(D) TO L
           DISPLAY "D LEN=" L " RAW=" RAW(8:3)
           MOVE FUNCTION BYTE-LENGTH(N) TO L
           COMPUTE O1 = FUNCTION ORD(RAW(11:1)) - 1
           COMPUTE O2 = FUNCTION ORD(RAW(12:1)) - 1
           COMPUTE O3 = FUNCTION ORD(RAW(13:1)) - 1
           COMPUTE O4 = FUNCTION ORD(RAW(14:1)) - 1
           DISPLAY "N LEN=" L " IMG=" O1 " " O2 " " O3 " " O4
           MOVE FUNCTION BYTE-LENGTH(Z) TO L
           DISPLAY "Z LEN=" L " RAW=" RAW(15:3)
           MOVE FUNCTION BYTE-LENGTH(IX) TO L
           COMPUTE O1 = FUNCTION ORD(RAW(25:1)) - 1
           DISPLAY "IX LEN=" L " LAST=" O1 " TE=" TE(X1)
           COMPUTE K = B + C + PD
           DISPLAY "USE SUM=" K
           IF B = C
               DISPLAY "USE B=C"
           ELSE
               DISPLAY "USE B<>C"
           END-IF
           STOP RUN.
