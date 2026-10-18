*>--- conformance-case
*> id: pb680_bit_record_area_file_io
*> rules: [GR-13.18.60.4-5, GR-13.18.33.4-3, GR-9.1.13.4-1]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB680 - a USAGE BIT item in a FILE's record area (13.18.60.4 GR5: "bits shall be used to
      *> represent a boolean data item"). The record area is "a storage area associated with a file in which
      *> logical records from that file are made accessible" (9.1.2), so a bit item there occupies BITS of that
      *> area, packed high-order bit first from its first bit position (8.5.1.6.3), and a record is as many
      *> characters wide as those bits fill: ceil(bits / 8), with the trailing filler bits zero ("as needed to
      *> increase the number of bits to fill an integral number of characters", 8.5.1.6.3). Eight bits to the
      *> character is the implementor's 8.1.2 choice (CONFORMANCE.md, USAGE BIT determination). Before PB327's
      *> record-area channel an elementary BIT record area sent its '0'/'1' value CARRIER (eight characters,
      *> truncated to the one-character record) instead of its packed byte; the fix had no golden.
      *>
      *> HOW EACH LEG CAN FAIL. Every bit item below is WRITTEN through a connector whose record is the bit item,
      *> then READ through a SECOND connector on the same physical file whose record is plain PIC X, so the
      *> expected number is the ORD (ordinal position, 15.67; 'A' is 66) of a byte computed from the bits by hand:
      *> a sender that put the carrier characters on the file would read back ORD 49 or 50 ('1' or '0'), and one
      *> that wrote the wrong width would fail the AT END that follows the last record (9.1.13.4 item 1).
      *> The bit connector then reads the file back and must show the bits it wrote, and the same bits must come
      *> back through RELATIVE and INDEXED organizations, whose physical layout is the implementor's, only by value.
      *>
      *> DERIVATIONS (high-order bit first):
      *>   S1  01000001 = 0x41 -> ORD 66      10100101 = 0xA5 -> ORD 166
      *>   S2  010000010101 + 0000 = 0x41 0x50 -> ORD 66,81      111100001111 + 0000 = 0xF0 0xF0 -> ORD 241,241
      *>   S3  10101 + 000 = 0xA8 -> ORD 169      01010 + 000 = 0x50 -> ORD 81
      *>   S4  "Z" = 0x5A (ORD 91) then 10100101 = 0xA5 (ORD 166); "a" = 0x61 (ORD 98) then 00000001 = 0x01 (ORD 2)
      *>   S5  "Z" then 101 + 00000 = 0xA0 -> ORD 91,161 (the trailing partial byte of an alphanumeric group record
      *>       is padded to an integral number of characters by 8.5.1.6.3's last placement rule)
      *>   S6  LINE SEQUENTIAL carries the byte of 01000010 = 0x42 = 'B' -> ORD 67
      *>   S7  SORT moves whole records: ascending on the alphanumeric key (a boolean item is not a legal key,
      *>       14.9.40.3 SR6 c) leaves A,B,C and each record's key byte equals its bit byte, so the ORDs are
      *>       66,66 / 67,67 / 68,68; the procedure form RELEASEs and RETURNs the same records, descending: C,B,A.
      *>       MERGE (14.9.24) interleaves two key-ordered inputs the same way: A,B,C,D -> 66,66 .. 69,69.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB680BITIO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FB ASSIGN TO "pb680_s1.dat" ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-B.
           SELECT FBX ASSIGN TO "pb680_s1.dat" ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-X.
           SELECT F12 ASSIGN TO "pb680_s2.dat" ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-B.
           SELECT F12X ASSIGN TO "pb680_s2.dat" ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-X.
           SELECT F5 ASSIGN TO "pb680_s3.dat" ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-B.
           SELECT F5X ASSIGN TO "pb680_s3.dat" ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-X.
           SELECT FG ASSIGN TO "pb680_s4.dat" ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-B.
           SELECT FGX ASSIGN TO "pb680_s4.dat" ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-X.
           SELECT FG3 ASSIGN TO "pb680_s5.dat" ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-B.
           SELECT FG3X ASSIGN TO "pb680_s5.dat" ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-X.
           SELECT FL ASSIGN TO "pb680_s6.dat" ORGANIZATION IS LINE SEQUENTIAL
               FILE STATUS IS ST-B.
           SELECT FLX ASSIGN TO "pb680_s6.dat" ORGANIZATION IS LINE SEQUENTIAL
               FILE STATUS IS ST-X.
           SELECT SI ASSIGN TO "pb680_s7i.dat" ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-B.
           SELECT SO ASSIGN TO "pb680_s7o.dat" ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-B.
           SELECT SOX ASSIGN TO "pb680_s7o.dat" ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-X.
           SELECT MA ASSIGN TO "pb680_s7a.dat" ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-B.
           SELECT MB ASSIGN TO "pb680_s7b.dat" ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-B.
           SELECT MO ASSIGN TO "pb680_s7m.dat" ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-B.
           SELECT MOX ASSIGN TO "pb680_s7m.dat" ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-X.
           SELECT SF ASSIGN TO "pb680_s7.wrk".
           SELECT FR ASSIGN TO "pb680_s8.dat" ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC RELATIVE KEY IS RK FILE STATUS IS ST-B.
           SELECT FI ASSIGN TO "pb680_s9.dat" ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC RECORD KEY IS IK FILE STATUS IS ST-B.
       DATA DIVISION.
       FILE SECTION.
       FD FB.
       01 BR PIC 1(8) USAGE BIT.
       FD FBX.
       01 BXR PIC X(1).
       FD F12.
       01 B12 PIC 1(12) USAGE BIT.
       FD F12X.
       01 B12X PIC X(2).
       FD F5.
       01 B5 PIC 1(5) USAGE BIT.
       FD F5X.
       01 B5X PIC X(1).
       FD FG.
       01 GR.
          05 GA PIC X.
          05 GB PIC 1(8) USAGE BIT.
       FD FGX.
       01 GXR PIC X(2).
       FD FG3.
       01 G3R.
          05 G3A PIC X.
          05 G3B PIC 1(3) USAGE BIT.
       FD FG3X.
       01 G3XR PIC X(2).
       FD FL.
       01 LR PIC 1(8) USAGE BIT.
       FD FLX.
       01 LXR PIC X(1).
       FD SI.
       01 SIR.
          05 SIK PIC X.
          05 SIB PIC 1(8) USAGE BIT.
       FD SO.
       01 SOR.
          05 SOK PIC X.
          05 SOB PIC 1(8) USAGE BIT.
       FD SOX.
       01 SOXR PIC X(2).
       FD MA.
       01 MAR.
          05 MAK PIC X.
          05 MAB PIC 1(8) USAGE BIT.
       FD MB.
       01 MBR.
          05 MBK PIC X.
          05 MBB PIC 1(8) USAGE BIT.
       FD MO.
       01 MOR.
          05 MOK PIC X.
          05 MOB PIC 1(8) USAGE BIT.
       FD MOX.
       01 MOXR PIC X(2).
       SD SF.
       01 SFR.
          05 SFK PIC X.
          05 SFB PIC 1(8) USAGE BIT.
       FD FR.
       01 RR PIC 1(12) USAGE BIT.
       FD FI.
       01 IR.
          05 IK PIC X(2).
          05 IB PIC 1(8) USAGE BIT.
       WORKING-STORAGE SECTION.
       01 ST-B PIC XX.
       01 ST-X PIC XX.
       01 RK PIC 9(2).
       01 WS5 PIC 1(5) USAGE BIT.
       01 WS12 PIC 1(12) USAGE BIT.
       01 WSG.
          05 WSK PIC X.
          05 WSB PIC 1(8) USAGE BIT.
       01 EOF-SW PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN SECTION.
      *> ===== S1 - an elementary 8-bit record =====
           OPEN OUTPUT FB.
           MOVE B"01000001" TO BR.
           WRITE BR.
           MOVE B"10100101" TO BR.
           WRITE BR.
           CLOSE FB.
           DISPLAY "S1-W=" ST-B.
           OPEN INPUT FBX.
           READ FBX.
           DISPLAY "S1-X1=" FUNCTION ORD(BXR).
           READ FBX.
           DISPLAY "S1-X2=" FUNCTION ORD(BXR).
           READ FBX AT END DISPLAY "S1-X3=" ST-X.
           CLOSE FBX.
           OPEN INPUT FB.
           READ FB.
           DISPLAY "S1-B1=[" BR "]".
           READ FB.
           DISPLAY "S1-B2=[" BR "]".
           CLOSE FB.
      *> ===== S2 - a 12-bit record: two characters, four zero filler bits =====
           OPEN OUTPUT F12.
           MOVE B"010000010101" TO B12.
           WRITE B12.
           MOVE B"111100001111" TO B12.
           WRITE B12.
           CLOSE F12.
           OPEN INPUT F12X.
           READ F12X.
           DISPLAY "S2-X1=" FUNCTION ORD(B12X(1:1)) "," FUNCTION ORD(B12X(2:1)).
           READ F12X.
           DISPLAY "S2-X2=" FUNCTION ORD(B12X(1:1)) "," FUNCTION ORD(B12X(2:1)).
           READ F12X AT END DISPLAY "S2-X3=" ST-X.
           CLOSE F12X.
           OPEN INPUT F12.
           READ F12.
           DISPLAY "S2-B1=[" B12 "]".
           READ F12.
           DISPLAY "S2-B2=[" B12 "]".
           CLOSE F12.
      *> ===== S3 - a 5-bit record sent WRITE FROM and received READ INTO =====
           OPEN OUTPUT F5.
           MOVE B"10101" TO WS5.
           WRITE B5 FROM WS5.
           MOVE B"01010" TO WS5.
           WRITE B5 FROM WS5.
           CLOSE F5.
           OPEN INPUT F5X.
           READ F5X.
           DISPLAY "S3-X1=" FUNCTION ORD(B5X).
           READ F5X.
           DISPLAY "S3-X2=" FUNCTION ORD(B5X).
           READ F5X AT END DISPLAY "S3-X3=" ST-X.
           CLOSE F5X.
           OPEN INPUT F5.
           MOVE B"00000" TO WS5.
           READ F5 INTO WS5.
           DISPLAY "S3-B1=[" WS5 "]".
           READ F5 INTO WS5.
           DISPLAY "S3-B2=[" WS5 "]".
           CLOSE F5.
      *> ===== S4 - an alphanumeric group with a bit leaf beside it =====
           OPEN OUTPUT FG.
           MOVE "Z" TO GA.
           MOVE B"10100101" TO GB.
           WRITE GR.
           MOVE "a" TO GA.
           MOVE B"00000001" TO GB.
           WRITE GR.
           CLOSE FG.
           OPEN INPUT FGX.
           READ FGX.
           DISPLAY "S4-X1=" FUNCTION ORD(GXR(1:1)) "," FUNCTION ORD(GXR(2:1)).
           READ FGX.
           DISPLAY "S4-X2=" FUNCTION ORD(GXR(1:1)) "," FUNCTION ORD(GXR(2:1)).
           READ FGX AT END DISPLAY "S4-X3=" ST-X.
           CLOSE FGX.
           OPEN INPUT FG.
           READ FG.
           DISPLAY "S4-B1=" GA "[" GB "]".
           READ FG.
           DISPLAY "S4-B2=" GA "[" GB "]".
           CLOSE FG.
      *> ===== S5 - the same group ending in a partial byte =====
           OPEN OUTPUT FG3.
           MOVE "Z" TO G3A.
           MOVE B"101" TO G3B.
           WRITE G3R.
           CLOSE FG3.
           OPEN INPUT FG3X.
           READ FG3X.
           DISPLAY "S5-X1=" FUNCTION ORD(G3XR(1:1)) "," FUNCTION ORD(G3XR(2:1)).
           READ FG3X AT END DISPLAY "S5-X2=" ST-X.
           CLOSE FG3X.
           OPEN INPUT FG3.
           READ FG3.
           DISPLAY "S5-B1=" G3A "[" G3B "]".
           CLOSE FG3.
      *> ===== S6 - LINE SEQUENTIAL =====
           OPEN OUTPUT FL.
           MOVE B"01000010" TO LR.
           WRITE LR.
           CLOSE FL.
           OPEN INPUT FLX.
           READ FLX.
           DISPLAY "S6-X1=" FUNCTION ORD(LXR).
           CLOSE FLX.
           OPEN INPUT FL.
           READ FL.
           DISPLAY "S6-B1=[" LR "]".
           CLOSE FL.
      *> ===== S7 - SORT: USING/GIVING, then INPUT/OUTPUT PROCEDURE =====
           OPEN OUTPUT SI.
           MOVE "C" TO SIK.
           MOVE B"01000011" TO SIB.
           WRITE SIR.
           MOVE "A" TO SIK.
           MOVE B"01000001" TO SIB.
           WRITE SIR.
           MOVE "B" TO SIK.
           MOVE B"01000010" TO SIB.
           WRITE SIR.
           CLOSE SI.
           SORT SF ON ASCENDING KEY SFK USING SI GIVING SO.
           OPEN INPUT SOX.
           PERFORM 3 TIMES
               READ SOX
               DISPLAY "S7-X=" FUNCTION ORD(SOXR(1:1)) ","
                   FUNCTION ORD(SOXR(2:1))
           END-PERFORM.
           READ SOX AT END DISPLAY "S7-X4=" ST-X.
           CLOSE SOX.
           SORT SF ON DESCENDING KEY SFK
               INPUT PROCEDURE IS S7-IN OUTPUT PROCEDURE IS S7-OUT.
      *> MERGE of two key-ordered inputs (A,C and B,D) into one: A,B,C,D, each with its own bit byte.
           OPEN OUTPUT MA.
           MOVE "A" TO MAK.
           MOVE B"01000001" TO MAB.
           WRITE MAR.
           MOVE "C" TO MAK.
           MOVE B"01000011" TO MAB.
           WRITE MAR.
           CLOSE MA.
           OPEN OUTPUT MB.
           MOVE "B" TO MBK.
           MOVE B"01000010" TO MBB.
           WRITE MBR.
           MOVE "D" TO MBK.
           MOVE B"01000100" TO MBB.
           WRITE MBR.
           CLOSE MB.
           MERGE SF ON ASCENDING KEY SFK USING MA MB GIVING MO.
           OPEN INPUT MOX.
           PERFORM 4 TIMES
               READ MOX
               DISPLAY "S7-M=" FUNCTION ORD(MOXR(1:1)) ","
                   FUNCTION ORD(MOXR(2:1))
           END-PERFORM.
           READ MOX AT END DISPLAY "S7-M5=" ST-X.
           CLOSE MOX.
           GO TO S8-REL.
       S7-IN SECTION.
           OPEN INPUT SI.
           PERFORM UNTIL EOF-SW = "Y"
               READ SI INTO WSG AT END MOVE "Y" TO EOF-SW
                   NOT AT END RELEASE SFR FROM WSG
               END-READ
           END-PERFORM.
           CLOSE SI.
       S7-OUT SECTION.
           MOVE "N" TO EOF-SW.
           PERFORM UNTIL EOF-SW = "Y"
               RETURN SF INTO WSG AT END MOVE "Y" TO EOF-SW
                   NOT AT END DISPLAY "S7-R=" WSK "[" WSB "]"
               END-RETURN
           END-PERFORM.
      *> ===== S8 - RELATIVE: write, random read, rewrite, sequential read =====
       S8-REL SECTION.
           OPEN OUTPUT FR.
           MOVE 1 TO RK.
           MOVE B"010000010101" TO WS12.
           WRITE RR FROM WS12.
           MOVE 3 TO RK.
           MOVE B"111100001111" TO RR.
           WRITE RR.
           DISPLAY "S8-W=" ST-B.
           CLOSE FR.
           OPEN I-O FR.
           MOVE 3 TO RK.
           READ FR.
           DISPLAY "S8-R3=" ST-B "[" RR "]".
           MOVE 1 TO RK.
           MOVE B"101010100101" TO RR.
           REWRITE RR.
           DISPLAY "S8-RW=" ST-B.
           MOVE 1 TO RK.
           MOVE ZERO TO WS12.
           READ FR INTO WS12.
           DISPLAY "S8-R1=" ST-B "[" WS12 "]".
           MOVE "N" TO EOF-SW.
           MOVE 1 TO RK.
           START FR KEY IS EQUAL TO RK.
           PERFORM UNTIL EOF-SW = "Y"
               READ FR NEXT AT END MOVE "Y" TO EOF-SW
                   NOT AT END DISPLAY "S8-N" RK "=[" RR "]"
               END-READ
           END-PERFORM.
           CLOSE FR.
      *> ===== S9 - INDEXED: write, random read, rewrite, key-order read =====
           OPEN OUTPUT FI.
           MOVE "AB" TO IK.
           MOVE B"01000011" TO IB.
           WRITE IR.
           MOVE "AA" TO IK.
           MOVE B"11110000" TO IB.
           WRITE IR.
           DISPLAY "S9-W=" ST-B.
           CLOSE FI.
           OPEN I-O FI.
           MOVE "AB" TO IK.
           READ FI.
           DISPLAY "S9-R=" ST-B "[" IB "]".
           MOVE B"00001111" TO IB.
           REWRITE IR.
           DISPLAY "S9-RW=" ST-B.
           MOVE "N" TO EOF-SW.
           MOVE LOW-VALUES TO IK.
           START FI KEY IS NOT LESS THAN IK.
           PERFORM UNTIL EOF-SW = "Y"
               READ FI NEXT AT END MOVE "Y" TO EOF-SW
                   NOT AT END DISPLAY "S9-N" IK "=[" IB "]"
               END-READ
           END-PERFORM.
           CLOSE FI.
           STOP RUN.
