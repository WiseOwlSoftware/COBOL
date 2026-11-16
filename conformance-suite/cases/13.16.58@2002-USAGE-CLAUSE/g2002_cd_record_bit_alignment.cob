      *>--- conformance-case
      *> id: g2002_cd_record_bit_alignment
      *> rules: [2002-GR-13.16.58.3-5]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.58.3 GR5 - "The USAGE BIT clause specifies that
      *> bits shall be used to represent a boolean data item. A data item
      *> described with USAGE BIT is a bit data item. The alignment of a
      *> data item described with USAGE BIT is specified in 8.5.1.5.3".
      *> CR is the record of an output CD (§2002:A.1.2.2 SR9: an
      *> ordinary record description); RAW, a second record of the same
      *> CD, implicitly redefines the same area and shows its bytes as
      *> FUNCTION ORD - 1 (native byte order, no PROGRAM COLLATING
      *> SEQUENCE).
      *> §2002:8.5.1.5.3 - an elementary bit item "immediately following
      *> an elementary bit data item ... of the same level" is aligned
      *> "at the next bit position": B2 follows B1 in the same byte, B3
      *> starts the next byte after them. X1 is not a bit item, so
      *> implicit filler bits follow B3 "as needed to advance alignment
      *> to a required natural boundary for the next item" - a byte.
      *> Bits are packed high-order bit first (D-USAGE-BIT).
      *>  RAW is first set to LOW-VALUES, then B1 = B"1010",
      *>  B2 = B"0101", B3 = B"111", X1 = "Z":
      *>   byte 1 = 1010 0101 = 165; byte 2 = 111 and five filler bits
      *>   still zero = 1110 0000 = 224; byte 3 = "Z" = 090.
      *>  LENGTH(CR) is 3 alphanumeric positions (§2002:15.39.3 RV3);
      *>  LENGTH(B1) is 4 boolean positions (RV1): B1 is a bit data item.
      *> The program fails if a bit item takes a character position per
      *> bit (LENGTH 12), if B2 starts a new byte, or if no filler
      *> precedes X1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2UA05.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  L   PIC 99.
       01  O1  PIC 999.
       01  O2  PIC 999.
       01  O3  PIC 999.
       COMMUNICATION SECTION.
       CD  CO FOR OUTPUT.
       01  CR.
           05 B1 PIC 1(4) USAGE BIT.
           05 B2 PIC 1(4) USAGE BIT.
           05 B3 PIC 1(3) USAGE BIT.
           05 X1 PIC X.
       01  RAW PIC X(3).
       PROCEDURE DIVISION.
       MAIN.
           MOVE LOW-VALUES TO RAW
           MOVE B"1010" TO B1
           MOVE B"0101" TO B2
           MOVE B"111" TO B3
           MOVE "Z" TO X1
           DISPLAY "BITS " B1 " " B2 " " B3 " " X1
           MOVE FUNCTION LENGTH(CR) TO L
           DISPLAY "CR LEN " L
           MOVE FUNCTION LENGTH(B1) TO L
           DISPLAY "B1 LEN " L
           COMPUTE O1 = FUNCTION ORD(RAW(1:1)) - 1
           COMPUTE O2 = FUNCTION ORD(RAW(2:1)) - 1
           COMPUTE O3 = FUNCTION ORD(RAW(3:1)) - 1
           DISPLAY "IMG " O1 " " O2 " " O3
           STOP RUN.
