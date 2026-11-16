      *>--- conformance-case
      *> id: g2-gu-bit-group-ws-and-cd
      *> rules: [2002-SR-13.16.27.2-1, 2002-SR-13.16.27.2-2,
      *>    2002-GR-13.16.27.3-1, 2002-GR-13.16.27.3-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> One record twice: in working-storage (W lines) and as a record
      *> description entry after an OUTPUT CD (C lines, §2002:A.1.1.2).
      *> §2002:13.16.27.2 SR1 - GROUP-USAGE on group items that are not
      *>   strongly typed (G and its subordinate group SG) is accepted.
      *> §2002:13.16.27.2 SR2 - "USAGE BIT is implied for the subject
      *>   of the entry": A and B carry no USAGE clause yet are bit items
      *>   (§2002:13.16.58.3 GR1), so G packs into 2 bytes; C is
      *>   explicitly USAGE BIT; SG is explicitly GROUP-USAGE BIT.
      *> §2002:13.16.27.3 GR1b - G is treated as an elementary PIC 1(12)
      *>   boolean item: B"101" pads with zeros on the right, a 14-bit
      *>   literal truncates on the right (§2002:14.5.8), G sends to and
      *>   receives from a PIC 1(12) item, and compares as one item.
      *> §2002:13.16.27.3 GR1a/GR1c - G is a bit group; A, B, C sit at
      *>   the next bit position (§2002:8.5.1.5.3), so A/B/C take bits
      *>   1-3, 4-8, 9-12 (SUB lines); LENGTH 12 in boolean positions
      *>   (§2002:15.39.3 RV1); BYTE-LENGTH 02 (§2002:15.12.3 RV4).
      *> §2002:13.16.27.3 GR3 - BG holds the same 12 bits with no
      *>   GROUP-USAGE: an alphanumeric group, so LENGTH counts
      *>   alphanumeric positions rounded up (§2002:15.39.3 RV3, RV6):
      *>   02, not 12.
      *> §2002:13.16.27.2 SR2 implicit leg - WSG2 is a group subordinate
      *>   to the bit group WG2 with no GROUP-USAGE clause, so GROUP-USAGE
      *>   BIT is implied for it (§2002:13.14.3 GR1): it is a bit group,
      *>   LENGTH 05 boolean positions (§2002:15.39.3 RV1); as a bit group
      *>   following the bit item WA2 at the same level it starts at the
      *>   next bit position (§2002:8.5.1.5.3), so WG2 is 3+5 = 8 bits,
      *>   BYTE-LENGTH 01 (§2002:15.12.3 RV1). An alphanumeric WSG2 would
      *>   give LEN-SG2=01 and BYTES-G2=02.
      *> Any leg wrong prints BAD or a different length.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2GU01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  T12  PIC 1(12) USAGE BIT.
       77  WLEN PIC 99.
       01  WREC.
           05 WG GROUP-USAGE BIT.
              10 WA PIC 1(3).
              10 WB PIC 1(5).
              10 WSG GROUP-USAGE BIT.
                 15 WC PIC 1(4) USAGE BIT.
           05 WBG USAGE BIT.
              10 WBA PIC 1(5).
              10 WBB PIC 1(7).
           05 WX PIC X(2).
           05 WG2 GROUP-USAGE BIT.
              10 WA2 PIC 1(3).
              10 WSG2.
                 15 WC2 PIC 1(5).
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CREC.
           05 CG GROUP-USAGE BIT.
              10 CA PIC 1(3).
              10 CB PIC 1(5).
              10 CSG GROUP-USAGE BIT.
                 15 CC PIC 1(4) USAGE BIT.
           05 CBG USAGE BIT.
              10 CBA PIC 1(5).
              10 CBB PIC 1(7).
           05 CX PIC X(2).
           05 CG2 GROUP-USAGE BIT.
              10 CA2 PIC 1(3).
              10 CSG2.
                 15 CC2 PIC 1(5).
       PROCEDURE DIVISION.
      *> working-storage record
           MOVE B"101" TO WG
           IF WG = B"101000000000" DISPLAY "W M1 PAD OK"
           ELSE DISPLAY "W M1 PAD BAD" END-IF
           IF WA = B"101" AND WB = B"00000" AND WC = B"0000"
              DISPLAY "W M1 SUB OK"
           ELSE DISPLAY "W M1 SUB BAD" END-IF
           MOVE B"11110000111101" TO WG
           IF WG = B"111100001111" DISPLAY "W M2 TRUNC OK"
           ELSE DISPLAY "W M2 TRUNC BAD" END-IF
           IF WA = B"111" AND WB = B"10000" AND WC = B"1111"
              DISPLAY "W M2 SUB OK"
           ELSE DISPLAY "W M2 SUB BAD" END-IF
           MOVE WG TO T12
           IF T12 = B"111100001111" DISPLAY "W M3 SEND OK"
           ELSE DISPLAY "W M3 SEND BAD" END-IF
           MOVE B"1" TO T12
           MOVE T12 TO WG
           IF WG = B"100000000000" DISPLAY "W M4 RECV OK"
           ELSE DISPLAY "W M4 RECV BAD" END-IF
           MOVE FUNCTION LENGTH(WG) TO WLEN
           DISPLAY "W LEN-G=" WLEN
           MOVE FUNCTION BYTE-LENGTH(WG) TO WLEN
           DISPLAY "W BYTES-G=" WLEN
           MOVE FUNCTION LENGTH(WBG) TO WLEN
           DISPLAY "W LEN-BG=" WLEN
           MOVE FUNCTION LENGTH(WSG2) TO WLEN
           DISPLAY "W LEN-SG2=" WLEN
           MOVE FUNCTION BYTE-LENGTH(WG2) TO WLEN
           DISPLAY "W BYTES-G2=" WLEN
      *> the same record under the CD
           MOVE B"101" TO CG
           IF CG = B"101000000000" DISPLAY "C M1 PAD OK"
           ELSE DISPLAY "C M1 PAD BAD" END-IF
           IF CA = B"101" AND CB = B"00000" AND CC = B"0000"
              DISPLAY "C M1 SUB OK"
           ELSE DISPLAY "C M1 SUB BAD" END-IF
           MOVE B"11110000111101" TO CG
           IF CG = B"111100001111" DISPLAY "C M2 TRUNC OK"
           ELSE DISPLAY "C M2 TRUNC BAD" END-IF
           IF CA = B"111" AND CB = B"10000" AND CC = B"1111"
              DISPLAY "C M2 SUB OK"
           ELSE DISPLAY "C M2 SUB BAD" END-IF
           MOVE CG TO T12
           IF T12 = B"111100001111" DISPLAY "C M3 SEND OK"
           ELSE DISPLAY "C M3 SEND BAD" END-IF
           MOVE B"1" TO T12
           MOVE T12 TO CG
           IF CG = B"100000000000" DISPLAY "C M4 RECV OK"
           ELSE DISPLAY "C M4 RECV BAD" END-IF
           MOVE FUNCTION LENGTH(CG) TO WLEN
           DISPLAY "C LEN-G=" WLEN
           MOVE FUNCTION BYTE-LENGTH(CG) TO WLEN
           DISPLAY "C BYTES-G=" WLEN
           MOVE FUNCTION LENGTH(CBG) TO WLEN
           DISPLAY "C LEN-BG=" WLEN
           MOVE FUNCTION LENGTH(CSG2) TO WLEN
           DISPLAY "C LEN-SG2=" WLEN
           MOVE FUNCTION BYTE-LENGTH(CG2) TO WLEN
           DISPLAY "C BYTES-G2=" WLEN
           STOP RUN.
